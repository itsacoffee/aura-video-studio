using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Captions;

/// <summary>
/// Service for managing subtitle generation, export, and burn-in with RTL support.
/// </summary>
public class SubtitleService
{
    private readonly ILogger<SubtitleService> _logger;
    private readonly CaptionBuilder _captionBuilder;

    public SubtitleService(
        ILogger<SubtitleService> logger,
        CaptionBuilder captionBuilder)
    {
        _logger = logger;
        _captionBuilder = captionBuilder;
    }

    /// <summary>
    /// Generate subtitles with language-specific formatting and RTL support.
    /// </summary>
    public async Task<SubtitleGenerationResult> GenerateSubtitlesAsync(
        SubtitleGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating subtitles: Language={Language}, Format={Format}, RTL={IsRTL}",
            request.TargetLanguage, request.Format, request.IsRightToLeft);

        var lines = request.ScriptLines.ToList();

        if (!_captionBuilder.ValidateTimecodes(lines, out var validationMessage))
        {
            _logger.LogWarning("Subtitle validation warning: {Message}", validationMessage);
        }

        var content = request.Format switch
        {
            SubtitleExportFormat.SRT => _captionBuilder.GenerateSrt(lines),
            SubtitleExportFormat.VTT => _captionBuilder.GenerateVtt(lines),
            _ => throw new ArgumentException($"Unsupported subtitle format: {request.Format}")
        };

        string? exportedFilePath = null;
        if (request.ExportToFile && !string.IsNullOrEmpty(request.OutputDirectory))
        {
            exportedFilePath = await _captionBuilder.ExportSubtitlesToFileAsync(
                lines,
                request.Format,
                request.OutputDirectory,
                request.BaseFileName ?? $"subtitles_{request.TargetLanguage}",
                request.IsRightToLeft);
        }

        return new SubtitleGenerationResult
        {
            Content = content,
            Format = request.Format,
            LineCount = lines.Count,
            ExportedFilePath = exportedFilePath,
            IsRightToLeft = request.IsRightToLeft,
            TargetLanguage = request.TargetLanguage
        };
    }

    /// <summary>
    /// Generate burn-in filter for embedding subtitles with RTL-aware styling.
    /// </summary>
    public string GenerateBurnInFilter(
        string subtitleFilePath,
        BurnInOptions options)
    {
        _logger.LogInformation(
            "Generating burn-in filter: Font={Font}, RTL={IsRTL}",
            options.FontName, options.IsRightToLeft);

        var style = new CaptionRenderStyle(
            FontName: options.FontName,
            FontSize: options.FontSize,
            PrimaryColor: options.PrimaryColor,
            OutlineColor: options.OutlineColor,
            OutlineWidth: options.OutlineWidth,
            BorderStyle: options.BorderStyle,
            Alignment: options.Alignment,
            IsRightToLeft: options.IsRightToLeft,
            RtlFontFallback: options.RtlFontFallback);

        return _captionBuilder.BuildBurnInFilter(subtitleFilePath, style);
    }

    /// <summary>
    /// Get recommended subtitle styling for a language (RTL-aware).
    /// </summary>
    public CaptionRenderStyle GetRecommendedStyle(string languageCode)
    {
        var language = LanguageRegistry.GetLanguage(languageCode);
        
        if (language == null)
        {
            _logger.LogWarning("Unknown language: {Language}, using default style", languageCode);
            return new CaptionRenderStyle();
        }

        var isRTL = language.IsRightToLeft;
        
        _logger.LogDebug(
            "Recommended style for {Language}: RTL={IsRTL}",
            languageCode, isRTL);

        return new CaptionRenderStyle(
            FontName: "Arial",
            FontSize: 24,
            PrimaryColor: "FFFFFF",
            OutlineColor: "000000",
            OutlineWidth: 2,
            BorderStyle: 3,
            Alignment: 2,
            IsRightToLeft: isRTL,
            RtlFontFallback: isRTL ? GetRtlFontFallback(languageCode) : null);
    }

    /// <summary>
    /// Get appropriate font fallback for RTL languages.
    /// </summary>
    private string? GetRtlFontFallback(string languageCode)
    {
        var baseLanguage = languageCode.Split('-')[0].ToLowerInvariant();

        return baseLanguage switch
        {
            "ar" => "Arial Unicode MS",  // Arabic
            "he" => "Arial Unicode MS",  // Hebrew
            "fa" => "Arial Unicode MS",  // Persian
            "ur" => "Arial Unicode MS",  // Urdu
            _ => "Arial Unicode MS"
        };
    }

    /// <summary>
    /// Validate subtitle timing alignment with target durations (±2% tolerance).
    /// </summary>
    public SubtitleTimingValidationResult ValidateTimingAlignment(
        IEnumerable<ScriptLine> lines,
        double targetTotalDuration,
        double tolerancePercent = 0.02)
    {
        var linesList = lines.ToList();
        var actualTotalDuration = linesList.Sum(l => l.Duration.TotalSeconds);
        var deviation = Math.Abs(actualTotalDuration - targetTotalDuration) / targetTotalDuration;

        _logger.LogInformation(
            "Timing validation: Target={Target:F2}s, Actual={Actual:F2}s, Deviation={Deviation:P2}",
            targetTotalDuration, actualTotalDuration, deviation);

        var isWithinTolerance = deviation <= tolerancePercent;

        return new SubtitleTimingValidationResult
        {
            IsValid = isWithinTolerance,
            TargetDuration = targetTotalDuration,
            ActualDuration = actualTotalDuration,
            DeviationPercent = deviation * 100,
            TolerancePercent = tolerancePercent * 100,
            Message = isWithinTolerance
                ? "Subtitle timing within acceptable tolerance"
                : $"Subtitle timing deviation {deviation:P2} exceeds tolerance {tolerancePercent:P2}"
        };
    }
}

/// <summary>
/// Request for subtitle generation
/// </summary>
public record SubtitleGenerationRequest
{
    public required IReadOnlyList<ScriptLine> ScriptLines { get; init; }
    public required string TargetLanguage { get; init; }
    public SubtitleExportFormat Format { get; init; } = SubtitleExportFormat.SRT;
    public bool IsRightToLeft { get; init; }
    public bool ExportToFile { get; init; }
    public string? OutputDirectory { get; init; }
    public string? BaseFileName { get; init; }
}

/// <summary>
/// Result of subtitle generation
/// </summary>
public record SubtitleGenerationResult
{
    public required string Content { get; init; }
    public required SubtitleExportFormat Format { get; init; }
    public int LineCount { get; init; }
    public string? ExportedFilePath { get; init; }
    public bool IsRightToLeft { get; init; }
    public required string TargetLanguage { get; init; }
}

/// <summary>
/// Options for burning subtitles into video
/// </summary>
public record BurnInOptions
{
    public string FontName { get; init; } = "Arial";
    public int FontSize { get; init; } = 24;
    public string PrimaryColor { get; init; } = "FFFFFF";
    public string OutlineColor { get; init; } = "000000";
    public int OutlineWidth { get; init; } = 2;
    public int BorderStyle { get; init; } = 3;
    public int Alignment { get; init; } = 2;
    public bool IsRightToLeft { get; init; }
    public string? RtlFontFallback { get; init; }
}

/// <summary>
/// Result of subtitle timing validation
/// </summary>
public record SubtitleTimingValidationResult
{
    public bool IsValid { get; init; }
    public double TargetDuration { get; init; }
    public double ActualDuration { get; init; }
    public double DeviationPercent { get; init; }
    public double TolerancePercent { get; init; }
    public string Message { get; init; } = string.Empty;
}
