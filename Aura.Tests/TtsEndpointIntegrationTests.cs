using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class TtsEndpointIntegrationTests
{
    [Fact]
    public async Task TtsEndpoint_Should_GenerateNarrationWithMockProvider()
    {
        // Arrange - Simulating the /tts endpoint
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Welcome to Aura Video Studio", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test narration", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)),
            new ScriptLine(2, "Thank you for watching", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2))
        };

        var voiceSpec = new VoiceSpec(
            VoiceName: "Mock Voice 1",
            Rate: 1.0,
            Pitch: 0.0,
            Pause: PauseStyle.Natural
        );

        // Act
        var audioPath = await provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(audioPath);
        Assert.True(File.Exists(audioPath));
        
        var fileInfo = new FileInfo(audioPath);
        Assert.True(fileInfo.Length > 0);
        
        // Cleanup
        File.Delete(audioPath);
    }

    [Fact]
    public async Task TtsEndpoint_Should_SupportDifferentVoiceSettings()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Testing voice settings", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };

        var voiceSpecFast = new VoiceSpec(
            VoiceName: "Mock Voice 2",
            Rate: 1.5,
            Pitch: 2.0,
            Pause: PauseStyle.Short
        );

        // Act
        var audioPath = await provider.SynthesizeAsync(lines, voiceSpecFast, CancellationToken.None);

        // Assert
        Assert.NotNull(audioPath);
        Assert.True(File.Exists(audioPath), $"Expected audio file to exist at {audioPath}");
        
        // Cleanup
        if (File.Exists(audioPath))
        {
            File.Delete(audioPath);
        }
    }

    [Fact]
    public async Task TtsWithCaptions_Should_ProduceLinkedTimeline()
    {
        // Arrange - Full integration test: TTS + Captions
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var ttsProvider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        var audioProcessor = new Aura.Core.Audio.AudioProcessor(
            NullLogger<Aura.Core.Audio.AudioProcessor>.Instance);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Scene 1: Introduction", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "Scene 2: Main content", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4)),
            new ScriptLine(2, "Scene 3: Conclusion", TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(2))
        };

        var voiceSpec = new VoiceSpec("Mock Voice 1", 1.0, 0.0, PauseStyle.Natural);

        // Act - Generate narration
        var narrationPath = await ttsProvider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Act - Generate captions
        var srtCaptions = audioProcessor.GenerateSrtSubtitles(lines);
        var vttCaptions = audioProcessor.GenerateVttSubtitles(lines);

        // Act - Create timeline
        var scenes = lines.Select(line => new Scene(
            Index: line.SceneIndex,
            Heading: $"Scene {line.SceneIndex + 1}",
            Script: line.Text,
            Start: line.Start,
            Duration: line.Duration
        )).ToList();

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: narrationPath,
            MusicPath: "",
            SubtitlesPath: "/tmp/test-captions.srt"
        );

        // Assert
        Assert.NotNull(narrationPath);
        Assert.True(File.Exists(narrationPath), $"Expected narration file to exist at {narrationPath}");
        Assert.NotNull(srtCaptions);
        Assert.NotEmpty(srtCaptions);
        Assert.NotNull(vttCaptions);
        Assert.NotEmpty(vttCaptions);
        Assert.Equal(narrationPath, timeline.NarrationPath);
        Assert.NotNull(timeline.SubtitlesPath);
        
        // Verify captions match line timings
        Assert.Contains("00:00:00,000 --> 00:00:03,000", srtCaptions);
        Assert.Contains("Scene 1: Introduction", srtCaptions);
        Assert.Contains("00:00:03,000 --> 00:00:07,000", srtCaptions);
        Assert.Contains("Scene 2: Main content", srtCaptions);
        
        // Cleanup
        if (File.Exists(narrationPath))
        {
            File.Delete(narrationPath);
        }
    }

    [Fact]
    public async Task TtsEndpoint_Should_HandleCancellation()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This should be cancelled", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };
        var voiceSpec = new VoiceSpec("Mock Voice 1", 1.0, 0.0, PauseStyle.Natural);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await provider.SynthesizeAsync(lines, voiceSpec, cts.Token);
        });
    }

    [Fact]
    public void CaptionsEndpoint_Should_SupportBothFormats()
    {
        // Arrange - Simulating /captions/generate endpoint
        var audioProcessor = new Aura.Core.Audio.AudioProcessor(
            NullLogger<Aura.Core.Audio.AudioProcessor>.Instance);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Caption line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Caption line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
        };

        // Act - Generate SRT
        var srtCaptions = audioProcessor.GenerateSrtSubtitles(lines);
        
        // Act - Generate VTT
        var vttCaptions = audioProcessor.GenerateVttSubtitles(lines);

        // Assert - Both formats should be valid
        Assert.NotNull(srtCaptions);
        Assert.NotEmpty(srtCaptions);
        Assert.Contains("1", srtCaptions);
        Assert.Contains("00:00:00,000 --> 00:00:02,000", srtCaptions);
        
        Assert.NotNull(vttCaptions);
        Assert.NotEmpty(vttCaptions);
        Assert.StartsWith("WEBVTT", vttCaptions);
        Assert.Contains("00:00:00.000 --> 00:00:02.000", vttCaptions);
    }
}
