using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service for suggesting proxy presets based on hardware capabilities and media characteristics
/// </summary>
public interface IProxyPresetService
{
    /// <summary>
    /// Suggest optimal proxy quality based on hardware tier and media characteristics
    /// </summary>
    Task<ProxyQualitySuggestion> SuggestProxyQualityAsync(
        HardwareTier tier,
        MediaCharacteristics mediaCharacteristics,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Media characteristics for proxy preset suggestion
/// </summary>
public record MediaCharacteristics
{
    public int Width { get; init; }
    public int Height { get; init; }
    public double DurationSeconds { get; init; }
    public int BitrateKbps { get; init; }
    public double FrameRate { get; init; }
}

/// <summary>
/// Proxy quality suggestion with reasoning
/// </summary>
public record ProxyQualitySuggestion
{
    public ProxyQuality SuggestedQuality { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public ProxyQuality[] AlternativeQualities { get; init; } = Array.Empty<ProxyQuality>();
}

/// <summary>
/// Implementation of proxy preset suggestion service
/// </summary>
public class ProxyPresetService : IProxyPresetService
{
    private readonly ILogger<ProxyPresetService> _logger;

    public ProxyPresetService(ILogger<ProxyPresetService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ProxyQualitySuggestion> SuggestProxyQualityAsync(
        HardwareTier tier,
        MediaCharacteristics mediaCharacteristics,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting proxy quality for tier {Tier}, media {Width}x{Height}",
            tier, mediaCharacteristics.Width, mediaCharacteristics.Height);

        var suggestion = tier switch
        {
            HardwareTier.A => SuggestForTierA(mediaCharacteristics),
            HardwareTier.B => SuggestForTierB(mediaCharacteristics),
            HardwareTier.C => SuggestForTierC(mediaCharacteristics),
            HardwareTier.D => SuggestForTierD(mediaCharacteristics),
            _ => new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.Preview,
                Reasoning = "Default preview quality for unknown hardware tier",
                ConfidenceScore = 0.5,
                AlternativeQualities = new[] { ProxyQuality.Draft, ProxyQuality.High }
            }
        };

        _logger.LogInformation("Suggested {Quality} quality (confidence: {Confidence:P0}): {Reasoning}",
            suggestion.SuggestedQuality, suggestion.ConfidenceScore, suggestion.Reasoning);

        return Task.FromResult(suggestion);
    }

    private static ProxyQualitySuggestion SuggestForTierA(MediaCharacteristics media)
    {
        if (media.Width >= 3840)
        {
            return new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.High,
                Reasoning = "High-end system (Tier A) with 4K+ source. Using High quality (1080p) proxies for smooth editing while maintaining detail.",
                ConfidenceScore = 0.9,
                AlternativeQualities = new[] { ProxyQuality.Preview }
            };
        }

        if (media.Width >= 1920)
        {
            return new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.Preview,
                Reasoning = "High-end system (Tier A) with 1080p source. Preview quality optimal.",
                ConfidenceScore = 0.95,
                AlternativeQualities = new[] { ProxyQuality.High, ProxyQuality.Draft }
            };
        }

        return new ProxyQualitySuggestion
        {
            SuggestedQuality = ProxyQuality.Draft,
            Reasoning = "High-end system (Tier A). Draft quality provides best performance.",
            ConfidenceScore = 0.85,
            AlternativeQualities = new[] { ProxyQuality.Preview }
        };
    }

    private static ProxyQualitySuggestion SuggestForTierB(MediaCharacteristics media)
    {
        if (media.Width >= 3840)
        {
            return new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.Preview,
                Reasoning = "Mid-range system (Tier B) with 4K+ source. Preview quality recommended to maintain smooth playback.",
                ConfidenceScore = 0.85,
                AlternativeQualities = new[] { ProxyQuality.Draft, ProxyQuality.High }
            };
        }

        if (media.Width >= 1920)
        {
            return new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.Preview,
                Reasoning = "Mid-range system (Tier B) with 1080p source. Preview quality provides good balance.",
                ConfidenceScore = 0.9,
                AlternativeQualities = new[] { ProxyQuality.Draft }
            };
        }

        return new ProxyQualitySuggestion
        {
            SuggestedQuality = ProxyQuality.Draft,
            Reasoning = "Mid-range system (Tier B). Draft quality recommended for smooth editing.",
            ConfidenceScore = 0.85,
            AlternativeQualities = new[] { ProxyQuality.Preview }
        };
    }

    private static ProxyQualitySuggestion SuggestForTierC(MediaCharacteristics media)
    {
        if (media.Width >= 1920)
        {
            return new ProxyQualitySuggestion
            {
                SuggestedQuality = ProxyQuality.Draft,
                Reasoning = "Lower mid-range system (Tier C). Draft quality strongly recommended for any HD content.",
                ConfidenceScore = 0.9,
                AlternativeQualities = new[] { ProxyQuality.Preview }
            };
        }

        return new ProxyQualitySuggestion
        {
            SuggestedQuality = ProxyQuality.Draft,
            Reasoning = "Lower mid-range system (Tier C). Draft quality essential for smooth editing.",
            ConfidenceScore = 0.95,
            AlternativeQualities = Array.Empty<ProxyQuality>()
        };
    }

    private static ProxyQualitySuggestion SuggestForTierD(MediaCharacteristics media)
    {
        return new ProxyQualitySuggestion
        {
            SuggestedQuality = ProxyQuality.Draft,
            Reasoning = "Minimum spec system (Tier D). Draft quality required for any editing performance.",
            ConfidenceScore = 1.0,
            AlternativeQualities = Array.Empty<ProxyQuality>()
        };
    }
}
