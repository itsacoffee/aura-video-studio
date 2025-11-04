using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// Service for filtering stock media content based on safety guidelines
/// </summary>
public class ContentSafetyFilterService
{
    private readonly ILogger<ContentSafetyFilterService> _logger;
    private readonly ContentSafetyFilters _defaultFilters;

    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "explicit", "nude", "nsfw", "violence", "blood", "gore", "weapon", "gun",
        "drug", "alcohol", "smoking", "cigarette", "controversial", "political"
    };

    public ContentSafetyFilterService(
        ILogger<ContentSafetyFilterService> logger,
        ContentSafetyFilters? filters = null)
    {
        _logger = logger;
        _defaultFilters = filters ?? new ContentSafetyFilters
        {
            EnabledFilters = true,
            BlockExplicitContent = true,
            BlockViolentContent = true,
            BlockSensitiveTopics = true,
            SafetyLevel = 5
        };
    }

    /// <summary>
    /// Checks if content is safe based on text analysis
    /// </summary>
    public Task<bool> IsContentSafeAsync(string text, CancellationToken ct)
    {
        if (!_defaultFilters.EnabledFilters)
        {
            return Task.FromResult(true);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(true);
        }

        var lowerText = text.ToLowerInvariant();

        if (ContainsBlockedKeywords(lowerText))
        {
            _logger.LogDebug("Content blocked due to sensitive keyword");
            return Task.FromResult(false);
        }

        if (ContainsSensitiveContent(lowerText))
        {
            _logger.LogDebug("Content blocked due to sensitive content detection");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Sanitizes query to remove potentially unsafe terms
    /// </summary>
    public string SanitizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var terms = query.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var safeTerms = terms.Where(term => !SensitiveKeywords.Contains(term)).ToList();

        return string.Join(" ", safeTerms);
    }

    /// <summary>
    /// Validates if query is appropriate for stock media search
    /// </summary>
    public (bool IsValid, string? Reason) ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (false, "Query cannot be empty");
        }

        if (query.Length < 2)
        {
            return (false, "Query too short");
        }

        if (query.Length > 200)
        {
            return (false, "Query too long (max 200 characters)");
        }

        var lowerQuery = query.ToLowerInvariant();

        if (_defaultFilters.BlockedKeywords.Any(kw => lowerQuery.Contains(kw.ToLowerInvariant())))
        {
            return (false, "Query contains blocked keywords");
        }

        if (ContainsExplicitContent(lowerQuery) && _defaultFilters.BlockExplicitContent)
        {
            return (false, "Query contains explicit content");
        }

        if (ContainsViolentContent(lowerQuery) && _defaultFilters.BlockViolentContent)
        {
            return (false, "Query contains violent content");
        }

        return (true, null);
    }

    private bool ContainsBlockedKeywords(string text)
    {
        return _defaultFilters.BlockedKeywords.Any(keyword =>
            text.Contains(keyword.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsSensitiveContent(string text)
    {
        if (!_defaultFilters.BlockSensitiveTopics)
        {
            return false;
        }

        return SensitiveKeywords.Any(keyword => text.Contains(keyword));
    }

    private bool ContainsExplicitContent(string text)
    {
        var explicitTerms = new[] { "explicit", "nude", "nsfw", "adult", "xxx" };
        return explicitTerms.Any(term => text.Contains(term));
    }

    private bool ContainsViolentContent(string text)
    {
        var violentTerms = new[] { "violence", "blood", "gore", "weapon", "gun", "kill" };
        return violentTerms.Any(term => text.Contains(term));
    }
}
