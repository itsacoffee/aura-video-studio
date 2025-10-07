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
}
