using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class AzureVoiceDiscoveryTests
{
    [Fact]
    public void AzureVoiceDiscovery_Should_Initialize()
    {
        // Arrange
        var httpClient = new HttpClient();
        
        // Act
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-api-key");

        // Assert
        Assert.NotNull(discovery);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_ReturnEmpty_OnHttpError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "invalid-key");

        // Act
        var voices = await discovery.GetVoicesAsync();

        // Assert
        Assert.NotNull(voices);
        Assert.Empty(voices);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_CacheResults()
    {
        // Arrange
        int callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"[
                        {
                            ""Name"": ""Microsoft Server Speech Text to Speech Voice (en-US, JennyNeural)"",
                            ""ShortName"": ""en-US-JennyNeural"",
                            ""DisplayName"": ""Jenny"",
                            ""LocalName"": ""Jenny"",
                            ""Gender"": ""Female"",
                            ""Locale"": ""en-US"",
                            ""StyleList"": [""cheerful"", ""sad""],
                            ""RolePlayList"": [""Girl"", ""Boy""],
                            ""VoiceType"": ""Neural""
                        }
                    ]")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act - call twice
        var voices1 = await discovery.GetVoicesAsync();
        var voices2 = await discovery.GetVoicesAsync();

        // Assert - should only call HTTP once due to caching
        Assert.Equal(1, callCount);
        Assert.Single(voices1);
        Assert.Single(voices2);
        Assert.Equal("en-US-JennyNeural", voices1[0].Id);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_FilterByLocale()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""VoiceType"": ""Neural""
                    },
                    {
                        ""ShortName"": ""es-ES-ElviraNeural"",
                        ""DisplayName"": ""Elvira"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""es-ES"",
                        ""VoiceType"": ""Neural""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voices = await discovery.GetVoicesAsync(locale: "en-US");

        // Assert
        Assert.Single(voices);
        Assert.Equal("en-US-JennyNeural", voices[0].Id);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_FilterByGender()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""VoiceType"": ""Neural""
                    },
                    {
                        ""ShortName"": ""en-US-GuyNeural"",
                        ""DisplayName"": ""Guy"",
                        ""Gender"": ""Male"",
                        ""Locale"": ""en-US"",
                        ""VoiceType"": ""Neural""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voices = await discovery.GetVoicesAsync(gender: VoiceGender.Female);

        // Assert
        Assert.Single(voices);
        Assert.Equal("en-US-JennyNeural", voices[0].Id);
        Assert.Equal(VoiceGender.Female, voices[0].Gender);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_FilterByVoiceType()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""VoiceType"": ""Neural""
                    },
                    {
                        ""ShortName"": ""en-US-BenjaminStandard"",
                        ""DisplayName"": ""Benjamin"",
                        ""Gender"": ""Male"",
                        ""Locale"": ""en-US"",
                        ""VoiceType"": ""Standard""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voices = await discovery.GetVoicesAsync(voiceType: VoiceType.Neural);

        // Assert
        Assert.Single(voices);
        Assert.Equal("en-US-JennyNeural", voices[0].Id);
        Assert.Equal(VoiceType.Neural, voices[0].VoiceType);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_ParseVoiceCapabilities()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""LocalName"": ""Jenny (US English)"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""StyleList"": [""cheerful"", ""sad"", ""angry""],
                        ""RolePlayList"": [""Girl"", ""Boy""],
                        ""VoiceType"": ""Neural""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voices = await discovery.GetVoicesAsync();

        // Assert
        Assert.Single(voices);
        var voice = voices[0];
        Assert.Equal("en-US-JennyNeural", voice.Id);
        Assert.Equal("Jenny", voice.Name);
        Assert.Equal("Jenny (US English)", voice.LocalName);
        Assert.Equal(VoiceGender.Female, voice.Gender);
        Assert.Equal("en-US", voice.Locale);
        Assert.Equal(3, voice.AvailableStyles.Length);
        Assert.Contains("cheerful", voice.AvailableStyles);
        Assert.Contains("sad", voice.AvailableStyles);
        Assert.Contains("angry", voice.AvailableStyles);
        Assert.Equal(2, voice.AvailableRoles.Length);
        Assert.Contains("Girl", voice.AvailableRoles);
        Assert.Contains("Boy", voice.AvailableRoles);
        Assert.Equal(VoiceType.Neural, voice.VoiceType);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_DetectFeatures()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""StyleList"": [""cheerful""],
                        ""RolePlayList"": [""Girl""],
                        ""VoiceType"": ""Neural""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voices = await discovery.GetVoicesAsync();

        // Assert
        var voice = voices[0];
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Rate));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Pitch));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Volume));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Prosody));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.AudioEffects));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Styles));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Roles));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.Phonemes));
        Assert.True(voice.SupportedFeatures.HasFlag(VoiceFeatures.SayAs));
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_GetVoiceCapabilitiesById()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[
                    {
                        ""ShortName"": ""en-US-JennyNeural"",
                        ""DisplayName"": ""Jenny"",
                        ""Gender"": ""Female"",
                        ""Locale"": ""en-US"",
                        ""StyleList"": [""cheerful""],
                        ""VoiceType"": ""Neural""
                    }
                ]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voice = await discovery.GetVoiceCapabilitiesAsync("en-US-JennyNeural");

        // Assert
        Assert.NotNull(voice);
        Assert.Equal("en-US-JennyNeural", voice.Id);
        Assert.Equal("Jenny", voice.Name);
    }

    [Fact]
    public async Task AzureVoiceDiscovery_Should_ReturnNull_ForNonExistentVoice()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act
        var voice = await discovery.GetVoiceCapabilitiesAsync("non-existent-voice");

        // Assert
        Assert.Null(voice);
    }

    [Fact]
    public void AzureVoiceDiscovery_Should_ClearCache()
    {
        // Arrange
        var httpClient = new HttpClient();
        var discovery = new AzureVoiceDiscovery(
            NullLogger<AzureVoiceDiscovery>.Instance,
            httpClient,
            "eastus",
            "fake-key");

        // Act & Assert - should not throw
        discovery.ClearCache();
    }
}
