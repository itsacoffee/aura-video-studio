using System;
using System.Collections.Generic;
using System.Text;
using Aura.Core.Models;

namespace Aura.Core.Rendering;

/// <summary>
/// Builds deterministic FFmpeg filtergraphs and command arguments for video rendering.
/// Maps spec requirements to encoder-specific parameters.
/// </summary>
public class FFmpegPlanBuilder
{
    /// <summary>
    /// Encoder types supported by the builder
    /// </summary>
    public enum EncoderType
    {
        X264,      // Software H.264
        NVENC_H264, // NVIDIA H.264
        NVENC_HEVC, // NVIDIA HEVC/H.265
        NVENC_AV1,  // NVIDIA AV1 (RTX 40/50 only)
        AMF_H264,   // AMD H.264
        AMF_HEVC,   // AMD HEVC
        QSV_H264,   // Intel QuickSync H.264
        QSV_HEVC    // Intel QuickSync HEVC
    }

    /// <summary>
    /// Quality vs Speed setting (0 = fastest/lower quality, 100 = slowest/highest quality)
    /// </summary>
    public class QualitySettings
    {
        public int QualityLevel { get; set; } = 75; // 0-100
        public int Fps { get; set; } = 30;
        public bool EnableSceneCut { get; set; } = true;
        public string Codec { get; set; } = "H264";
    }

    /// <summary>
    /// Builds FFmpeg command arguments based on render spec and quality settings
    /// </summary>
    public string BuildRenderCommand(
        RenderSpec spec,
        QualitySettings quality,
        EncoderType encoder,
        string inputVideo,
        string inputAudio,
        string outputPath)
    {
        var args = new StringBuilder();

        // Input files
        args.Append($"-i \"{inputVideo}\" ");
        args.Append($"-i \"{inputAudio}\" ");

        // Video encoding
        AppendVideoEncoderArgs(args, spec, quality, encoder);

        // Audio encoding
        AppendAudioEncoderArgs(args, spec);

        // Frame rate (CFR - Constant Frame Rate)
        args.Append($"-r {quality.Fps} ");

        // GOP (Group of Pictures) - 2x fps for standard keyframe interval
        int gopSize = quality.Fps * 2;
        args.Append($"-g {gopSize} ");

        // Scene-cut keyframes
        if (quality.EnableSceneCut)
        {
            args.Append("-sc_threshold 40 ");
        }

        // Pixel format
        args.Append("-pix_fmt yuv420p ");

        // Color space (BT.709 for HD)
        args.Append("-colorspace bt709 -color_trc bt709 -color_primaries bt709 ");

        // Overwrite output
        args.Append("-y ");

        // Output file
        args.Append($"\"{outputPath}\"");

        return args.ToString();
    }

    /// <summary>
    /// Builds a filtergraph for compositing video with overlays, text, and transitions
    /// </summary>
    public string BuildFilterGraph(
        Resolution resolution,
        bool addSubtitles = false,
        string? subtitlePath = null)
    {
        var filters = new List<string>();

        // Scale to target resolution with high-quality scaler
        filters.Add($"scale={resolution.Width}:{resolution.Height}:flags=lanczos");

        // Add subtle motion (Ken Burns effect) - optional
        // filters.Add("zoompan=z='min(zoom+0.0015,1.5)':d=125:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':s=1920x1080");

        // Add subtitles if requested
        if (addSubtitles && !string.IsNullOrEmpty(subtitlePath))
        {
            // Escape path for FFmpeg
            string escapedPath = subtitlePath.Replace("\\", "\\\\").Replace(":", "\\:");
            filters.Add($"subtitles='{escapedPath}':force_style='FontSize=24,PrimaryColour=&HFFFFFF&,OutlineColour=&H000000&,BorderStyle=3'");
        }

        return string.Join(",", filters);
    }

    private void AppendVideoEncoderArgs(StringBuilder args, RenderSpec spec, QualitySettings quality, EncoderType encoder)
    {
        switch (encoder)
        {
            case EncoderType.X264:
                AppendX264Args(args, spec, quality);
                break;

            case EncoderType.NVENC_H264:
            case EncoderType.NVENC_HEVC:
                AppendNvencArgs(args, spec, quality, encoder);
                break;

            case EncoderType.NVENC_AV1:
                AppendNvencAv1Args(args, spec, quality);
                break;

            case EncoderType.AMF_H264:
            case EncoderType.AMF_HEVC:
                AppendAmfArgs(args, spec, quality, encoder);
                break;

            case EncoderType.QSV_H264:
            case EncoderType.QSV_HEVC:
                AppendQsvArgs(args, spec, quality, encoder);
                break;

            default:
                // Fallback to x264
                AppendX264Args(args, spec, quality);
                break;
        }

        // Video bitrate
        args.Append($"-b:v {spec.VideoBitrateK}k ");
    }

    private void AppendX264Args(StringBuilder args, RenderSpec spec, QualitySettings quality)
    {
        args.Append("-c:v libx264 ");

        // CRF: 28 (fast/lower) -> 14 (slow/higher)
        int crf = 28 - (int)(quality.QualityLevel * 0.14);
        crf = Math.Clamp(crf, 14, 28);
        args.Append($"-crf {crf} ");

        // Preset: veryfast -> slow
        string preset = quality.QualityLevel switch
        {
            >= 90 => "slow",
            >= 75 => "medium",
            >= 50 => "fast",
            >= 25 => "faster",
            _ => "veryfast"
        };
        args.Append($"-preset {preset} ");

        // Tune for film
        args.Append("-tune film ");

        // Profile
        args.Append("-profile:v high ");
    }

    private void AppendNvencArgs(StringBuilder args, RenderSpec spec, QualitySettings quality, EncoderType encoder)
    {
        string codec = encoder == EncoderType.NVENC_H264 ? "h264_nvenc" : "hevc_nvenc";
        args.Append($"-c:v {codec} ");

        // Rate control: Constant Quality (CQ)
        args.Append("-rc cq ");

        // CQ value: 33 (fast/lower) -> 18 (slow/higher)
        int cq = 33 - (int)(quality.QualityLevel * 0.15);
        cq = Math.Clamp(cq, 18, 33);
        args.Append($"-cq {cq} ");

        // Preset: p5 (fast) -> p7 (slow)
        int preset = quality.QualityLevel >= 75 ? 7 : (quality.QualityLevel >= 50 ? 6 : 5);
        args.Append($"-preset p{preset} ");

        // Advanced options
        args.Append("-rc-lookahead 16 ");
        args.Append("-spatial-aq 1 ");
        args.Append("-temporal-aq 1 ");
        args.Append("-bf 3 "); // B-frames
    }

    private void AppendNvencAv1Args(StringBuilder args, RenderSpec spec, QualitySettings quality)
    {
        args.Append("-c:v av1_nvenc ");

        // Rate control: Constant Quality (CQ)
        args.Append("-rc cq ");

        // CQ value: 38 (fast/lower) -> 22 (slow/higher)
        int cq = 38 - (int)(quality.QualityLevel * 0.16);
        cq = Math.Clamp(cq, 22, 38);
        args.Append($"-cq {cq} ");

        // Preset: p5 (fast) -> p7 (slow)
        int preset = quality.QualityLevel >= 75 ? 7 : (quality.QualityLevel >= 50 ? 6 : 5);
        args.Append($"-preset p{preset} ");
    }

    private void AppendAmfArgs(StringBuilder args, RenderSpec spec, QualitySettings quality, EncoderType encoder)
    {
        string codec = encoder == EncoderType.AMF_H264 ? "h264_amf" : "hevc_amf";
        args.Append($"-c:v {codec} ");

        // Quality preset
        string preset = quality.QualityLevel >= 75 ? "quality" : "balanced";
        args.Append($"-quality {preset} ");

        // Rate control
        args.Append("-rc cqp ");
        int qp = 28 - (int)(quality.QualityLevel * 0.14);
        qp = Math.Clamp(qp, 14, 28);
        args.Append($"-qp_i {qp} -qp_p {qp} -qp_b {qp} ");
    }

    private void AppendQsvArgs(StringBuilder args, RenderSpec spec, QualitySettings quality, EncoderType encoder)
    {
        string codec = encoder == EncoderType.QSV_H264 ? "h264_qsv" : "hevc_qsv";
        args.Append($"-c:v {codec} ");

        // Quality preset
        string preset = quality.QualityLevel >= 75 ? "veryslow" : (quality.QualityLevel >= 50 ? "medium" : "fast");
        args.Append($"-preset {preset} ");

        // Global quality (lower is better)
        int globalQuality = 28 - (int)(quality.QualityLevel * 0.14);
        globalQuality = Math.Clamp(globalQuality, 14, 28);
        args.Append($"-global_quality {globalQuality} ");
    }

    private void AppendAudioEncoderArgs(StringBuilder args, RenderSpec spec)
    {
        // AAC codec (most compatible)
        args.Append("-c:a aac ");

        // Audio bitrate
        args.Append($"-b:a {spec.AudioBitrateK}k ");

        // Sample rate (48kHz standard for video)
        args.Append("-ar 48000 ");

        // Stereo channels
        args.Append("-ac 2 ");
    }

    /// <summary>
    /// Detects available encoders on the system
    /// </summary>
    public static List<EncoderType> DetectAvailableEncoders(string ffmpegOutput)
    {
        var available = new List<EncoderType>();

        if (ffmpegOutput.Contains("h264_nvenc"))
            available.Add(EncoderType.NVENC_H264);

        if (ffmpegOutput.Contains("hevc_nvenc"))
            available.Add(EncoderType.NVENC_HEVC);

        if (ffmpegOutput.Contains("av1_nvenc"))
            available.Add(EncoderType.NVENC_AV1);

        if (ffmpegOutput.Contains("h264_amf"))
            available.Add(EncoderType.AMF_H264);

        if (ffmpegOutput.Contains("hevc_amf"))
            available.Add(EncoderType.AMF_HEVC);

        if (ffmpegOutput.Contains("h264_qsv"))
            available.Add(EncoderType.QSV_H264);

        if (ffmpegOutput.Contains("hevc_qsv"))
            available.Add(EncoderType.QSV_HEVC);

        // x264 is always available as software fallback
        available.Add(EncoderType.X264);

        return available;
    }
}
