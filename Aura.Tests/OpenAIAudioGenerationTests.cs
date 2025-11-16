using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Llm;
using Aura.Core.Models.OpenAI;

namespace Aura.Tests;

public class OpenAIAudioGenerationTests
{
    private const string ValidApiKey = "sk-test-validkey1234567890abcdefghijklmnopqrstuvwxyz";

    [Fact]
    public async Task GenerateWithAudioAsync_ReturnsAudioResponse()
    {
        // Arrange
        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test transcript",
                        audio = new
                        {
                            data = Convert.ToBase64String(Encoding.UTF8.GetBytes("fake audio data")),
                            transcript = "Test transcript"
                        }
                    }
                }
            }
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-4o"
        );

        var audioConfig = new AudioConfig
        {
            Voice = AudioVoice.Alloy,
            Format = AudioFormat.Wav,
            Modalities = new System.Collections.Generic.List<string> { "text", "audio" }
        };

        // Act
        var result = await provider.GenerateWithAudioAsync("Hello, world!", audioConfig);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AudioData);
        Assert.Equal("Test transcript", result.Transcript);
        Assert.Equal("wav", result.Format);
        Assert.Equal("alloy", result.Voice);
    }

    [Fact]
    public async Task GenerateWithAudioAsync_WithNonGPT4oModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-3.5-turbo" // Not a GPT-4o model
        );

        var audioConfig = new AudioConfig
        {
            Voice = AudioVoice.Echo,
            Format = AudioFormat.Mp3
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await provider.GenerateWithAudioAsync("Test", audioConfig));
    }

    [Fact]
    public async Task GenerateWithAudioAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-4o"
        );

        var audioConfig = new AudioConfig();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await provider.GenerateWithAudioAsync("", audioConfig));
    }

    [Fact]
    public async Task GenerateWithAudioAsync_WithUnauthorized_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(HttpStatusCode.Unauthorized, "Unauthorized");
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-4o"
        );

        var audioConfig = new AudioConfig();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await provider.GenerateWithAudioAsync("Test", audioConfig));
        
        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateWithAudioAsync_WithNoAudioInResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test transcript"
                        // No audio property
                    }
                }
            }
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-4o"
        );

        var audioConfig = new AudioConfig();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await provider.GenerateWithAudioAsync("Test", audioConfig));
    }

    [Theory]
    [InlineData(AudioVoice.Alloy, "alloy")]
    [InlineData(AudioVoice.Echo, "echo")]
    [InlineData(AudioVoice.Fable, "fable")]
    [InlineData(AudioVoice.Onyx, "onyx")]
    [InlineData(AudioVoice.Nova, "nova")]
    [InlineData(AudioVoice.Shimmer, "shimmer")]
    public async Task GenerateWithAudioAsync_SetsVoiceCorrectly(AudioVoice voice, string expectedVoice)
    {
        // Arrange
        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Test",
                        audio = new
                        {
                            data = Convert.ToBase64String(Encoding.UTF8.GetBytes("audio")),
                            transcript = "Test"
                        }
                    }
                }
            }
        };

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(mockResponse));
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidApiKey,
            "gpt-4o"
        );

        var audioConfig = new AudioConfig { Voice = voice };

        // Act
        var result = await provider.GenerateWithAudioAsync("Test", audioConfig);

        // Assert
        Assert.Equal(expectedVoice, result.Voice);
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });

        return new HttpClient(mockHandler.Object);
    }
}
