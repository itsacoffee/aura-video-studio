using System;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Rendering;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// End-to-end integration tests for Aura Video Studio
/// These tests validate the complete pipeline from brief to final video
/// </summary>
public class VideoGenerationE2ETests
{
    [Fact]
    public async Task HardwareDetection_Should_DetectSystem()
    {
        // Arrange
        var detector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);

        // Act
        var profile = await detector.DetectSystemAsync().ConfigureAwait(false);

        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.LogicalCores > 0, "Should detect CPU cores");
        Assert.True(profile.RamGB > 0, "Should detect RAM");
        Assert.True(Enum.IsDefined(typeof(HardwareTier), profile.Tier), "Should assign valid tier");
    }

    [Fact]
    public async Task RuleBasedLlm_Should_GenerateScript()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "Introduction to Machine Learning",
            Audience: "Beginners",
            Goal: "Educate",
            Tone: "Educational",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec, default).ConfigureAwait(false);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Machine Learning", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("##", script); // Should have scene headings
    }

    [Fact]
    public void ProviderMixing_Should_SelectCorrectProvider()
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            ActiveProfile = "Free-Only",
            AutoFallback = true,
            LogProviderSelection = false
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "Free");

        // Assert
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
    }

    [Fact]
    public void FFmpegPlanBuilder_Should_BuildValidCommand()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = 75,
            Fps = 30,
            EnableSceneCut = true
        };

        // Act
        string command = builder.BuildRenderCommand(
            spec,
            quality,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.NotEmpty(command);
        Assert.Contains("-i \"input.mp4\"", command);
        Assert.Contains("-i \"audio.wav\"", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-r 30", command);
        Assert.Contains("output.mp4", command);
    }

    [Fact]
    public void RenderPresets_Should_ProvideStandardPresets()
    {
        // Act & Assert
        var youtube1080p = RenderPresets.YouTube1080p;
        Assert.Equal(1920, youtube1080p.Res.Width);
        Assert.Equal(1080, youtube1080p.Res.Height);
        Assert.Equal("mp4", youtube1080p.Container);

        var shorts = RenderPresets.YouTubeShorts;
        Assert.Equal(1080, shorts.Res.Width);
        Assert.Equal(1920, shorts.Res.Height); // Vertical format

        var youtube4k = RenderPresets.YouTube4K;
        Assert.Equal(3840, youtube4k.Res.Width);
        Assert.Equal(2160, youtube4k.Res.Height);
    }

    [Fact]
    public void ProviderProfiles_Should_DefineCorrectStages()
    {
        // Act & Assert
        var freeOnly = ProviderProfile.FreeOnly;
        Assert.Equal("Free", freeOnly.Stages["Script"]);
        Assert.Equal("Windows", freeOnly.Stages["TTS"]);
        Assert.Equal("Stock", freeOnly.Stages["Visuals"]);

        var balancedMix = ProviderProfile.BalancedMix;
        Assert.Equal("ProIfAvailable", balancedMix.Stages["Script"]);
        Assert.Equal("StockOrLocal", balancedMix.Stages["Visuals"]);

        var proMax = ProviderProfile.ProMax;
        Assert.Equal("Pro", proMax.Stages["Script"]);
        Assert.Equal("Pro", proMax.Stages["TTS"]);
        Assert.Equal("Pro", proMax.Stages["Visuals"]);
    }

    [Fact]
    public async Task HardwareProbes_Should_Complete()
    {
        // Arrange
        var detector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);

        // Act & Assert - should not throw
        await detector.RunHardwareProbeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates a minimal free-path video generation workflow
    /// This validates that all components work together
    /// </summary>
    [Fact]
    public async Task FreePath_Should_GenerateScriptAndSelectProviders()
    {
        // Arrange - Hardware detection
        var hardwareDetector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);
        var systemProfile = await hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
        Assert.NotNull(systemProfile);

        // Arrange - Provider setup
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providerConfig = new ProviderMixingConfig
        {
            ActiveProfile = "Free-Only",
            AutoFallback = true
        };
        var providerMixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, providerConfig);

        // Arrange - Brief and spec
        var brief = new Brief(
            Topic: "Getting Started with Video Creation",
            Audience: "Content Creators",
            Goal: "Teach basics",
            Tone: "Informative",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(15), // Short test video
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Educational"
        );

        // Act - Generate script
        var script = await llmProvider.DraftScriptAsync(brief, planSpec, default).ConfigureAwait(false);

        // Assert - Script generation
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Video Creation", script, StringComparison.OrdinalIgnoreCase);

        // Act - Provider selection
        var llmProviders = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };
        var llmSelection = providerMixer.SelectLlmProvider(llmProviders, "Free");

        // Assert - Provider selection
        Assert.Equal("RuleBased", llmSelection.SelectedProvider);
        Assert.Equal("Script", llmSelection.Stage);

        // Act - Render spec
        var renderSpec = RenderPresets.YouTube1080p;
        var ffmpegBuilder = new FFmpegPlanBuilder();
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = systemProfile.Tier == HardwareTier.D ? 50 : 75,
            Fps = 30
        };

        string renderCommand = ffmpegBuilder.BuildRenderCommand(
            renderSpec,
            quality,
            FFmpegPlanBuilder.EncoderType.X264, // Free path uses software encoder
            "placeholder_video.mp4",
            "placeholder_audio.wav",
            "output.mp4"
        );

        // Assert - Render command
        Assert.NotEmpty(renderCommand);
        Assert.Contains("libx264", renderCommand);
    }

    [Theory]
    [InlineData("YouTube 1080p", 1920, 1080, 30, "H264")]
    [InlineData("YouTube Shorts", 1080, 1920, 30, "H264")]
    [InlineData("YouTube 4K", 3840, 2160, 30, "H264")]
    public void RenderPresets_Should_MapCorrectlyToFFmpegCommand(
        string presetName,
        int expectedWidth,
        int expectedHeight,
        int expectedFps,
        string expectedCodec)
    {
        // Arrange
        var preset = RenderPresets.GetPresetByName(presetName);
        Assert.NotNull(preset);

        var builder = new FFmpegPlanBuilder();

        // Act
        string command = builder.BuildRenderCommand(
            preset!,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Equal(expectedWidth, preset.Res.Width);
        Assert.Equal(expectedHeight, preset.Res.Height);
        Assert.Equal(expectedFps, preset.Fps);
        Assert.Equal(expectedCodec, preset.Codec);
        
        // Verify command includes correct settings
        Assert.Contains($"-r {expectedFps}", command);
        Assert.Contains("-g ", command); // GOP setting
        Assert.Contains("-sc_threshold", command); // Scene-cut keyframes
        Assert.Contains("-pix_fmt yuv420p", command); // CFR default
    }

    [Fact]
    public void RenderSpec_WithCustomSettings_Should_BuildCorrectCommand()
    {
        // Arrange
        var customSpec = new RenderSpec(
            Res: new Resolution(2560, 1440),
            Container: "mkv",
            VideoBitrateK: 20000,
            AudioBitrateK: 320,
            Fps: 60,
            Codec: "H264",
            QualityLevel: 90,
            EnableSceneCut: false
        );

        var builder = new FFmpegPlanBuilder();

        // Act
        string command = builder.BuildRenderCommand(
            customSpec,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mkv"
        );

        // Assert
        Assert.Contains("-r 60", command);
        Assert.Contains("-g 120", command); // GOP = 2x FPS
        Assert.DoesNotContain("-sc_threshold", command); // Scene-cut disabled
        Assert.Contains("-b:v 20000k", command);
        Assert.Contains("-b:a 320k", command);
        Assert.Contains("output.mkv", command);
    }

    [Theory]
    [InlineData(FFmpegPlanBuilder.EncoderType.X264, "-c:v libx264", "-crf")]
    [InlineData(FFmpegPlanBuilder.EncoderType.NVENC_H264, "-c:v h264_nvenc", "-cq")]
    [InlineData(FFmpegPlanBuilder.EncoderType.NVENC_AV1, "-c:v av1_nvenc", "-cq")]
    public void RenderCommand_Should_UseCorrectEncoderSettings(
        FFmpegPlanBuilder.EncoderType encoder,
        string expectedCodecArg,
        string expectedQualityArg)
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;

        // Act
        string command = builder.BuildRenderCommand(
            spec,
            encoder,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Contains(expectedCodecArg, command);
        Assert.Contains(expectedQualityArg, command);
    }
}
