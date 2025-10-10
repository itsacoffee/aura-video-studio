using System;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Rendering;
using Xunit;

namespace Aura.Tests;

public class FFmpegPlanBuilderTests
{
    [Fact]
    public void BuildRenderCommand_Should_IncludeBasicParameters()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 12000,
            AudioBitrateK: 256
        );
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = 75,
            Fps = 30
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
        Assert.Contains("-i \"input.mp4\"", command);
        Assert.Contains("-i \"audio.wav\"", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:v 12000k", command);
        Assert.Contains("-b:a 256k", command);
        Assert.Contains("-r 30", command);
        Assert.Contains("\"output.mp4\"", command);
    }

    [Fact]
    public void BuildRenderCommand_Should_UseNvencWhenSpecified()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = 80,
            Fps = 30
        };

        // Act
        string command = builder.BuildRenderCommand(
            spec,
            quality,
            FFmpegPlanBuilder.EncoderType.NVENC_H264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Contains("-c:v h264_nvenc", command);
        Assert.Contains("-rc cq", command);
        Assert.Contains("-preset p", command);
        Assert.Contains("-spatial-aq", command);
        Assert.Contains("-temporal-aq", command);
    }

    [Theory]
    [InlineData(100, "-crf 14")]  // Highest quality -> lowest CRF
    [InlineData(50, "-crf 21")]   // Medium quality
    [InlineData(0, "-crf 28")]    // Lowest quality -> highest CRF
    public void BuildRenderCommand_Should_MapQualityToCrf(int qualityLevel, string expectedCrf)
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = qualityLevel,
            Fps = 30
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
        Assert.Contains(expectedCrf, command);
    }

    [Fact]
    public void BuildRenderCommand_Should_SetGopSizeToTwiceFramerate()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings
        {
            QualityLevel = 75,
            Fps = 30
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

        // Assert - GOP should be 60 (2x fps)
        Assert.Contains("-g 60", command);
    }

    [Fact]
    public void BuildRenderCommand_Should_EnableSceneCutKeyframes()
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
        Assert.Contains("-sc_threshold", command);
    }

    [Fact]
    public void BuildFilterGraph_Should_IncludeScale()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);

        // Act
        string filterGraph = builder.BuildFilterGraph(resolution);

        // Assert
        Assert.Contains("scale=1920:1080", filterGraph);
        Assert.Contains("lanczos", filterGraph);
    }

    [Fact]
    public void BuildFilterGraph_Should_IncludeSubtitlesWhenRequested()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        string subtitlePath = "subtitles.srt";

        // Act
        string filterGraph = builder.BuildFilterGraph(resolution, addSubtitles: true, subtitlePath);

        // Assert
        Assert.Contains("subtitles=", filterGraph);
        Assert.Contains("subtitles.srt", filterGraph);
    }

    [Fact]
    public void DetectAvailableEncoders_Should_FindNvenc()
    {
        // Arrange
        string ffmpegOutput = @"
        Encoders:
         V....D h264_nvenc           NVIDIA NVENC H.264 encoder
         V....D hevc_nvenc           NVIDIA NVENC hevc encoder
         V..... libx264              libx264 H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10
        ";

        // Act
        var encoders = FFmpegPlanBuilder.DetectAvailableEncoders(ffmpegOutput);

        // Assert
        Assert.Contains(FFmpegPlanBuilder.EncoderType.NVENC_H264, encoders);
        Assert.Contains(FFmpegPlanBuilder.EncoderType.NVENC_HEVC, encoders);
        Assert.Contains(FFmpegPlanBuilder.EncoderType.X264, encoders);
    }

    [Fact]
    public void DetectAvailableEncoders_Should_FindAmf()
    {
        // Arrange
        string ffmpegOutput = @"
        Encoders:
         V....D h264_amf             AMD AMF H.264 Encoder
         V....D hevc_amf             AMD AMF HEVC encoder
        ";

        // Act
        var encoders = FFmpegPlanBuilder.DetectAvailableEncoders(ffmpegOutput);

        // Assert
        Assert.Contains(FFmpegPlanBuilder.EncoderType.AMF_H264, encoders);
        Assert.Contains(FFmpegPlanBuilder.EncoderType.AMF_HEVC, encoders);
    }

    [Fact]
    public void DetectAvailableEncoders_Should_AlwaysIncludeX264Fallback()
    {
        // Arrange
        string ffmpegOutput = "No hardware encoders available";

        // Act
        var encoders = FFmpegPlanBuilder.DetectAvailableEncoders(ffmpegOutput);

        // Assert
        Assert.Contains(FFmpegPlanBuilder.EncoderType.X264, encoders);
        Assert.Single(encoders); // Only x264 should be available
    }

    [Fact]
    public void BuildRenderCommand_Should_UseCorrectColorSpace()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings { QualityLevel = 75, Fps = 30 };

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
        Assert.Contains("-colorspace bt709", command);
        Assert.Contains("-color_trc bt709", command);
        Assert.Contains("-color_primaries bt709", command);
    }

    [Fact]
    public void BuildRenderCommand_Should_UseAacWithCorrectSettings()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;
        var quality = new FFmpegPlanBuilder.QualitySettings { QualityLevel = 75, Fps = 30 };

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
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:a 256k", command);
        Assert.Contains("-ar 48000", command); // 48kHz sample rate
        Assert.Contains("-ac 2", command);     // Stereo
    }

    [Fact]
    public void BuildFilterGraph_Should_IncludeKenBurnsEffect()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);

        // Act
        string filterGraph = builder.BuildFilterGraph(
            resolution,
            addSubtitles: false,
            subtitlePath: null,
            brandKit: null,
            enableKenBurns: true);

        // Assert
        Assert.Contains("zoompan", filterGraph);
        Assert.Contains("min(zoom+0.0015,1.1)", filterGraph); // Subtle zoom to 1.1x
    }

    [Fact]
    public void BuildFilterGraph_Should_IncludeWatermark()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        var brandKit = new BrandKit(
            WatermarkPath: "logo.png",
            WatermarkPosition: "bottom-right",
            WatermarkOpacity: 0.8f,
            BrandColor: null,
            AccentColor: null);

        // Act
        string filterGraph = builder.BuildFilterGraph(
            resolution,
            addSubtitles: false,
            subtitlePath: null,
            brandKit: brandKit,
            enableKenBurns: false);

        // Assert
        Assert.Contains("movie='logo.png'", filterGraph);
        Assert.Contains("overlay=", filterGraph);
        Assert.Contains("x=W-w-10:y=H-h-10", filterGraph); // Bottom-right position
        Assert.Contains("alpha=0.80", filterGraph);
    }

    [Fact]
    public void BuildFilterGraph_Should_IncludeBrandColorOverlay()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        var brandKit = new BrandKit(
            WatermarkPath: null,
            WatermarkPosition: null,
            WatermarkOpacity: 0.7f,
            BrandColor: "#FF6B35",
            AccentColor: null);

        // Act
        string filterGraph = builder.BuildFilterGraph(
            resolution,
            addSubtitles: false,
            subtitlePath: null,
            brandKit: brandKit,
            enableKenBurns: false);

        // Assert
        Assert.Contains("drawbox", filterGraph);
        Assert.Contains("FF6B35", filterGraph); // Brand color without #
        Assert.Contains("@0.05", filterGraph);  // 5% opacity overlay
    }

    [Fact]
    public void BuildFilterGraph_Should_SupportDifferentWatermarkPositions()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);

        // Test all positions
        var positions = new[]
        {
            ("top-left", "x=10:y=10"),
            ("top-right", "x=W-w-10:y=10"),
            ("bottom-left", "x=10:y=H-h-10"),
            ("bottom-right", "x=W-w-10:y=H-h-10"),
            ("center", "x=(W-w)/2:y=(H-h)/2")
        };

        foreach (var (position, expectedOverlay) in positions)
        {
            var brandKit = new BrandKit(
                WatermarkPath: "logo.png",
                WatermarkPosition: position,
                WatermarkOpacity: 0.7f,
                BrandColor: null,
                AccentColor: null);

            // Act
            string filterGraph = builder.BuildFilterGraph(
                resolution,
                addSubtitles: false,
                subtitlePath: null,
                brandKit: brandKit,
                enableKenBurns: false);

            // Assert
            Assert.Contains(expectedOverlay, filterGraph);
        }
    }

    [Fact]
    public void BuildFilterGraph_Should_CombineAllFeatures()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        var brandKit = new BrandKit(
            WatermarkPath: "logo.png",
            WatermarkPosition: "bottom-right",
            WatermarkOpacity: 0.8f,
            BrandColor: "#FF6B35",
            AccentColor: "#00D9FF");

        // Act
        string filterGraph = builder.BuildFilterGraph(
            resolution,
            addSubtitles: true,
            subtitlePath: "subs.srt",
            brandKit: brandKit,
            enableKenBurns: true);

        // Assert
        Assert.Contains("scale=1920:1080", filterGraph);
        Assert.Contains("zoompan", filterGraph);           // Ken Burns
        Assert.Contains("drawbox", filterGraph);           // Brand color
        Assert.Contains("movie='logo.png'", filterGraph);  // Watermark
        Assert.Contains("subtitles", filterGraph);         // Subtitles
    }

    [Fact]
    public void BuildRenderCommand_WithRenderSpec_Should_UseSpecSettings()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 12000,
            AudioBitrateK: 256,
            Fps: 60,
            Codec: "H264",
            QualityLevel: 90,
            EnableSceneCut: false
        );

        // Act
        string command = builder.BuildRenderCommand(
            spec,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Contains("-r 60", command);
        Assert.Contains("-g 120", command); // GOP = 2x FPS
        Assert.DoesNotContain("-sc_threshold", command); // Scene-cut disabled
        Assert.Contains("-crf", command);
    }

    [Theory]
    [InlineData("YouTube 1080p", 1920, 1080, 30)]
    [InlineData("YouTube 4K", 3840, 2160, 30)]
    [InlineData("YouTube Shorts", 1080, 1920, 30)]
    public void Presets_Should_MapCorrectly(string presetName, int width, int height, int fps)
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var preset = RenderPresets.GetPresetByName(presetName);

        // Act
        Assert.NotNull(preset);
        string command = builder.BuildRenderCommand(
            preset!,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Equal(width, preset.Res.Width);
        Assert.Equal(height, preset.Res.Height);
        Assert.Equal(fps, preset.Fps);
        Assert.Contains($"-r {fps}", command);
    }

    [Fact]
    public void BuildRenderCommand_Should_IncludeGopAndSceneCut()
    {
        // Arrange
        var builder = new FFmpegPlanBuilder();
        var spec = RenderPresets.YouTube1080p;

        // Act
        string command = builder.BuildRenderCommand(
            spec,
            FFmpegPlanBuilder.EncoderType.X264,
            "input.mp4",
            "audio.wav",
            "output.mp4"
        );

        // Assert
        Assert.Contains("-g 60", command); // GOP = 2x FPS (30 * 2)
        Assert.Contains("-sc_threshold 40", command); // Scene-cut enabled
        Assert.Contains("-pix_fmt yuv420p", command); // CFR
    }
}
