using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Interface for extracting short-form video content
/// </summary>
public interface IShortsExtractor
{
    /// <summary>
    /// Extract a short video clip based on the plan
    /// </summary>
    Task<GeneratedShort> ExtractShortAsync(
        ShortsPlan plan,
        CancellationToken ct = default);
}

/// <summary>
/// Extracts short-form video clips from source videos
/// </summary>
public class ShortsExtractor : IShortsExtractor
{
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<ShortsExtractor> _logger;

    public ShortsExtractor(
        IFFmpegExecutor ffmpegExecutor,
        ILlmProvider llmProvider,
        ILogger<ShortsExtractor> logger)
    {
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GeneratedShort> ExtractShortAsync(
        ShortsPlan plan,
        CancellationToken ct = default)
    {
        var outputDir = Path.Combine(
            Path.GetTempPath(), "AuraVideoStudio", "Shorts", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        // Get start time from the scene at startSceneIndex
        var scenes = plan.SourceTimeline.Scenes.ToList();
        if (plan.StartSceneIndex >= scenes.Count || plan.EndSceneIndex >= scenes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), "Scene indices are out of range");
        }

        var startScene = scenes[plan.StartSceneIndex];
        var endScene = scenes[plan.EndSceneIndex];
        var startTime = startScene.Start;
        var endTime = endScene.Start + endScene.Duration;
        var duration = endTime - startTime;

        // Ensure we're within short-form limits (max 60 seconds)
        if (duration > TimeSpan.FromSeconds(60))
        {
            _logger.LogWarning("Short duration {Duration}s exceeds 60s, will be trimmed",
                duration.TotalSeconds);
            duration = TimeSpan.FromSeconds(60);
        }

        var outputPath = Path.Combine(outputDir, $"{SanitizeFileName(plan.Title)}.mp4");

        // Determine target aspect ratio based on platform
        var targetAspect = plan.Platform.ToLowerInvariant() switch
        {
            "youtube_shorts" => Aspect.Vertical9x16,
            "tiktok" => Aspect.Vertical9x16,
            "instagram_reels" => Aspect.Vertical9x16,
            _ => Aspect.Vertical9x16
        };

        // Extract and reformat the clip
        await ExtractAndReformatClipAsync(
            plan.SourceVideoPath,
            outputPath,
            startTime,
            duration,
            targetAspect,
            ct).ConfigureAwait(false);

        // Generate thumbnail
        var thumbnailPath = Path.Combine(outputDir, "thumbnail.jpg");
        await GenerateThumbnailAsync(outputPath, thumbnailPath, ct).ConfigureAwait(false);

        // Generate social metadata
        var metadata = await GenerateSocialMetadataAsync(plan, ct).ConfigureAwait(false);

        return new GeneratedShort(
            Id: Guid.NewGuid().ToString(),
            Title: plan.Title,
            OutputPath: outputPath,
            Duration: duration,
            Aspect: targetAspect,
            Platform: plan.Platform,
            ThumbnailPath: thumbnailPath,
            Metadata: metadata);
    }

    private async Task ExtractAndReformatClipAsync(
        string sourcePath,
        string outputPath,
        TimeSpan startTime,
        TimeSpan duration,
        Aspect targetAspect,
        CancellationToken ct)
    {
        var (width, height) = targetAspect switch
        {
            Aspect.Vertical9x16 => (1080, 1920),
            Aspect.Square1x1 => (1080, 1080),
            _ => (1080, 1920)
        };

        // Build crop filter based on target aspect ratio
        var cropFilter = targetAspect switch
        {
            Aspect.Vertical9x16 => $"crop=ih*9/16:ih,scale={width}:{height}",
            Aspect.Square1x1 => $"crop=min(iw\\,ih):min(iw\\,ih),scale={width}:{height}",
            _ => $"crop=ih*9/16:ih,scale={width}:{height}"
        };

        var builder = new FFmpegCommandBuilder()
            .AddInput(sourcePath)
            .SetStartTime(startTime)
            .SetDuration(duration)
            .AddFilter(cropFilter)
            .SetVideoCodec("libx264")
            .SetPreset("fast")
            .SetCRF(23)
            .SetAudioCodec("aac")
            .SetAudioBitrate(128)
            .SetOutput(outputPath)
            .SetOverwrite(true);

        var result = await _ffmpegExecutor.ExecuteCommandAsync(builder, cancellationToken: ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Short extraction failed: {result.ErrorMessage}");
        }
    }

    private async Task GenerateThumbnailAsync(
        string videoPath,
        string thumbnailPath,
        CancellationToken ct)
    {
        // Extract a frame from the middle of the video for thumbnail
        var builder = FFmpegCommandBuilder.CreateThumbnailCommand(
            videoPath,
            thumbnailPath,
            TimeSpan.FromSeconds(2),  // Get frame at 2 seconds
            720,
            1280);

        var result = await _ffmpegExecutor.ExecuteCommandAsync(builder, cancellationToken: ct).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning("Thumbnail generation failed: {Error}", result.ErrorMessage);
            // Create a placeholder thumbnail file
            await File.WriteAllTextAsync(thumbnailPath, "Thumbnail placeholder", ct).ConfigureAwait(false);
        }
    }

    private async Task<ShortMetadata> GenerateSocialMetadataAsync(
        ShortsPlan plan,
        CancellationToken ct)
    {
        var prompt = $@"Generate social media metadata for this short video clip.

Title: {plan.Title}
Hook: {plan.HookText}
Platform: {plan.Platform}

Generate:
1. An engaging caption (under 150 characters)
2. 5-10 relevant hashtags
3. A call-to-action suggestion

Respond with JSON only, no additional text:
{{
  ""caption"": ""..."",
  ""hashtags"": [""#tag1"", ""#tag2""],
  ""cta"": ""...""
}}";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            var parsed = ParseSocialMetadata(response);

            return new ShortMetadata(
                HookText: plan.HookText,
                ViralPotential: plan.ViralPotential,
                SuggestedCaption: parsed?.Caption ?? plan.Title,
                SuggestedHashtags: parsed?.Hashtags ?? Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate social metadata via LLM, using defaults");
            return new ShortMetadata(
                HookText: plan.HookText,
                ViralPotential: plan.ViralPotential,
                SuggestedCaption: plan.Title,
                SuggestedHashtags: Array.Empty<string>());
        }
    }

    private SocialMetadataResponse? ParseSocialMetadata(string response)
    {
        try
        {
            var jsonContent = ExtractJsonFromResponse(response);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return null;
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var hashtags = new System.Collections.Generic.List<string>();
            if (root.TryGetProperty("hashtags", out var hashtagsElement))
            {
                foreach (var tag in hashtagsElement.EnumerateArray())
                {
                    var tagStr = tag.GetString();
                    if (!string.IsNullOrEmpty(tagStr))
                    {
                        hashtags.Add(tagStr);
                    }
                }
            }

            return new SocialMetadataResponse(
                Caption: root.TryGetProperty("caption", out var cap) ? cap.GetString() : null,
                Hashtags: hashtags.ToArray(),
                Cta: root.TryGetProperty("cta", out var cta) ? cta.GetString() : null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(name.Where(c => !invalid.Contains(c)))
            .Replace(' ', '_');
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }

    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }

        var trimmed = response.Trim();

        // Handle markdown code blocks
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var endIndex = trimmed.IndexOf("```", 7, StringComparison.Ordinal);
            if (endIndex > 0)
            {
                return trimmed.Substring(7, endIndex - 7).Trim();
            }
        }

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var startIndex = trimmed.IndexOf('\n');
            if (startIndex > 0)
            {
                var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                }
            }
        }

        // If already looks like JSON, return as-is
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        return trimmed;
    }

    private record SocialMetadataResponse(
        string? Caption,
        string[] Hashtags,
        string? Cta);
}
