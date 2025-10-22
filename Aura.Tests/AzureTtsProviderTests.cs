using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Voice;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class AzureTtsProviderTests
{
    [Fact]
    public void AzureTtsProvider_Should_Initialize_WithValidCredentials()
    {
        // Arrange & Act
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            "fake-api-key",
            "eastus",
            offlineOnly: false);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void AzureTtsProvider_Should_Initialize_InOfflineMode()
    {
        // Arrange & Act
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            null,
            "eastus",
            offlineOnly: true);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task AzureTtsProvider_Should_ReturnEmptyVoices_InOfflineMode()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            null,
            "eastus",
            offlineOnly: true);

        // Act
        var voices = await provider.GetAvailableVoicesAsync();

        // Assert
        Assert.NotNull(voices);
        Assert.Empty(voices);
    }

    [Fact]
    public async Task AzureTtsProvider_Should_ThrowException_WhenSynthesizing_InOfflineMode()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            null,
            "eastus",
            offlineOnly: true);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };
        var voiceSpec = new VoiceSpec("en-US-JennyNeural", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
    }

    [Fact]
    public async Task AzureTtsProvider_Should_ThrowException_WithoutApiKey()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            null,
            "eastus",
            offlineOnly: false);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };
        var voiceSpec = new VoiceSpec("en-US-JennyNeural", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
    }

    [Fact]
    public async Task AzureTtsProvider_Should_ThrowException_WhenSynthesizingWithOptions_InOfflineMode()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            null,
            "eastus",
            offlineOnly: true);

        var options = new AzureTtsOptions
        {
            Rate = 0.0,
            Pitch = 0.0,
            Volume = 1.0,
            Style = "cheerful"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeWithOptionsAsync(
                "Hello world", 
                "en-US-JennyNeural", 
                options, 
                CancellationToken.None));
    }

    [Fact]
    public void AzureTtsOptions_Should_HaveDefaultValues()
    {
        // Arrange & Act
        var options = new AzureTtsOptions();

        // Assert
        Assert.Equal(0.0, options.Rate);
        Assert.Equal(0.0, options.Pitch);
        Assert.Equal(1.0, options.Volume);
        Assert.Equal(1.0, options.StyleDegree);
        Assert.Equal(AzureAudioEffect.None, options.AudioEffect);
        Assert.Equal(EmphasisLevel.None, options.Emphasis);
        Assert.Null(options.Style);
        Assert.Null(options.Role);
    }

    [Fact]
    public void AzureTtsOptions_Should_AcceptValidRanges()
    {
        // Arrange & Act
        var options = new AzureTtsOptions
        {
            Rate = 1.5,  // -1.0 to 2.0
            Pitch = 0.3,  // -0.5 to 0.5
            Volume = 1.8,  // 0.0 to 2.0
            StyleDegree = 1.8,  // 0.01 to 2.0
            Style = "cheerful",
            Role = "Girl",
            AudioEffect = AzureAudioEffect.Reverb,
            Emphasis = EmphasisLevel.Strong
        };

        // Assert
        Assert.Equal(1.5, options.Rate);
        Assert.Equal(0.3, options.Pitch);
        Assert.Equal(1.8, options.Volume);
        Assert.Equal(1.8, options.StyleDegree);
        Assert.Equal("cheerful", options.Style);
        Assert.Equal("Girl", options.Role);
        Assert.Equal(AzureAudioEffect.Reverb, options.AudioEffect);
        Assert.Equal(EmphasisLevel.Strong, options.Emphasis);
    }

    [Fact]
    public void AzureTtsOptions_Should_SupportCustomBreaks()
    {
        // Arrange & Act
        var options = new AzureTtsOptions
        {
            CustomBreaks = new List<BreakPoint>
            {
                new BreakPoint { Position = 10, DurationMs = 500 },
                new BreakPoint { Position = 20, Strength = BreakStrength.Strong }
            }
        };

        // Assert
        Assert.NotNull(options.CustomBreaks);
        Assert.Equal(2, options.CustomBreaks.Count);
        Assert.Equal(10, options.CustomBreaks[0].Position);
        Assert.Equal(500, options.CustomBreaks[0].DurationMs);
        Assert.Equal(BreakStrength.Strong, options.CustomBreaks[1].Strength);
    }

    [Fact]
    public void AzureTtsOptions_Should_SupportProsodyContour()
    {
        // Arrange & Act
        var options = new AzureTtsOptions
        {
            ProsodyContour = "(0%,+20Hz) (10%,+30Hz) (40%,+10Hz)"
        };

        // Assert
        Assert.Equal("(0%,+20Hz) (10%,+30Hz) (40%,+10Hz)", options.ProsodyContour);
    }

    [Fact]
    public void AzureTtsOptions_Should_SupportPhonemes()
    {
        // Arrange & Act
        var options = new AzureTtsOptions
        {
            Phonemes = new Dictionary<string, string>
            {
                { "tomato", "təˈmeɪtoʊ" },
                { "either", "ˈaɪðər" }
            }
        };

        // Assert
        Assert.NotNull(options.Phonemes);
        Assert.Equal(2, options.Phonemes.Count);
        Assert.Equal("təˈmeɪtoʊ", options.Phonemes["tomato"]);
    }

    [Fact]
    public void AzureTtsOptions_Should_SupportSayAsHints()
    {
        // Arrange & Act
        var options = new AzureTtsOptions
        {
            SayAsHints = new Dictionary<string, SayAsInterpretation>
            {
                { 
                    "12/31/2024", 
                    new SayAsInterpretation 
                    { 
                        Text = "12/31/2024", 
                        InterpretAs = SayAsType.Date,
                        Format = "mdy"
                    } 
                }
            }
        };

        // Assert
        Assert.NotNull(options.SayAsHints);
        Assert.Single(options.SayAsHints);
        Assert.Equal(SayAsType.Date, options.SayAsHints["12/31/2024"].InterpretAs);
    }

    [Fact]
    public void AzureAudioEffect_Should_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)AzureAudioEffect.None);
        Assert.Equal(1, (int)AzureAudioEffect.EqTelecom);
        Assert.Equal(2, (int)AzureAudioEffect.EqCar);
        Assert.Equal(3, (int)AzureAudioEffect.Reverb);
    }

    [Fact]
    public void EmphasisLevel_Should_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)EmphasisLevel.None);
        Assert.Equal(1, (int)EmphasisLevel.Reduced);
        Assert.Equal(2, (int)EmphasisLevel.Moderate);
        Assert.Equal(3, (int)EmphasisLevel.Strong);
    }

    [Fact]
    public void BreakStrength_Should_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)BreakStrength.None);
        Assert.Equal(1, (int)BreakStrength.XWeak);
        Assert.Equal(2, (int)BreakStrength.Weak);
        Assert.Equal(3, (int)BreakStrength.Medium);
        Assert.Equal(4, (int)BreakStrength.Strong);
        Assert.Equal(5, (int)BreakStrength.XStrong);
    }

    [Fact]
    public void SayAsType_Should_HaveAllExpectedValues()
    {
        // Assert - verify all say-as types exist
        var types = Enum.GetValues<SayAsType>();
        Assert.Contains(SayAsType.Date, types);
        Assert.Contains(SayAsType.Time, types);
        Assert.Contains(SayAsType.Telephone, types);
        Assert.Contains(SayAsType.Cardinal, types);
        Assert.Contains(SayAsType.Ordinal, types);
        Assert.Contains(SayAsType.Digits, types);
        Assert.Contains(SayAsType.Fraction, types);
        Assert.Contains(SayAsType.Unit, types);
        Assert.Contains(SayAsType.Address, types);
        Assert.Contains(SayAsType.SpellOut, types);
    }

    [Fact]
    public void AzureTtsProvider_Should_Dispose_Properly()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            "fake-api-key",
            "eastus",
            offlineOnly: false);

        // Act & Assert - should not throw
        provider.Dispose();
        provider.Dispose(); // Second dispose should be safe
    }
}
