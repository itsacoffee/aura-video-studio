using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Manages keyword-based content filtering
/// </summary>
public class KeywordListManager
{
    private readonly ILogger<KeywordListManager> _logger;

    public KeywordListManager(ILogger<KeywordListManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Find matches for a keyword rule in content
    /// </summary>
    public Task<List<KeywordMatch>> FindMatchesAsync(
        string content,
        KeywordRule rule,
        CancellationToken ct = default)
    {
        var matches = new List<KeywordMatch>();

        try
        {
            if (rule.IsRegex)
            {
                matches.AddRange(FindRegexMatches(content, rule));
            }
            else if (rule.MatchType == KeywordMatchType.WholeWord)
            {
                matches.AddRange(FindWholeWordMatches(content, rule));
            }
            else
            {
                matches.AddRange(FindSubstringMatches(content, rule));
            }

            return Task.FromResult(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding matches for keyword '{Keyword}'", rule.Keyword);
            return Task.FromResult(matches);
        }
    }

    /// <summary>
    /// Find regex pattern matches
    /// </summary>
    private List<KeywordMatch> FindRegexMatches(string content, KeywordRule rule)
    {
        var matches = new List<KeywordMatch>();
        var options = rule.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

        try
        {
            var regex = new Regex(rule.Keyword, options);
            var regexMatches = regex.Matches(content);

            foreach (Match match in regexMatches)
            {
                matches.Add(new KeywordMatch
                {
                    MatchedText = match.Value,
                    Position = match.Index,
                    Length = match.Length
                });
            }
        }
        catch (RegexParseException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", rule.Keyword);
        }

        return matches;
    }

    /// <summary>
    /// Find whole word matches
    /// </summary>
    private List<KeywordMatch> FindWholeWordMatches(string content, KeywordRule rule)
    {
        var matches = new List<KeywordMatch>();
        var comparison = rule.IsCaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        var pattern = $@"\b{Regex.Escape(rule.Keyword)}\b";
        var options = rule.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

        try
        {
            var regex = new Regex(pattern, options);
            var regexMatches = regex.Matches(content);

            foreach (Match match in regexMatches)
            {
                matches.Add(new KeywordMatch
                {
                    MatchedText = match.Value,
                    Position = match.Index,
                    Length = match.Length
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in whole word matching for: {Keyword}", rule.Keyword);
        }

        return matches;
    }

    /// <summary>
    /// Find substring matches
    /// </summary>
    private List<KeywordMatch> FindSubstringMatches(string content, KeywordRule rule)
    {
        var matches = new List<KeywordMatch>();
        var comparison = rule.IsCaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        var index = 0;
        while (index < content.Length)
        {
            index = content.IndexOf(rule.Keyword, index, comparison);
            if (index == -1)
                break;

            matches.Add(new KeywordMatch
            {
                MatchedText = content.Substring(index, rule.Keyword.Length),
                Position = index,
                Length = rule.Keyword.Length
            });

            index += rule.Keyword.Length;
        }

        return matches;
    }

    /// <summary>
    /// Import keywords from text (one per line)
    /// </summary>
    public List<KeywordRule> ImportFromText(
        string text,
        SafetyAction defaultAction = SafetyAction.Warn)
    {
        var rules = new List<KeywordRule>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("//"))
                continue;

            rules.Add(new KeywordRule
            {
                Keyword = line,
                Action = defaultAction,
                MatchType = KeywordMatchType.WholeWord
            });
        }

        _logger.LogInformation("Imported {Count} keyword rules", rules.Count);
        return rules;
    }

    /// <summary>
    /// Get curated starter lists
    /// </summary>
    public Dictionary<string, List<string>> GetStarterLists()
    {
        return new Dictionary<string, List<string>>
        {
            ["CommonProfanity"] = new List<string> 
            { 
                "damn", "hell", "crap", "dang"
            },
            ["StrongProfanity"] = new List<string> 
            { 
                "explicit", "vulgar", "obscene"
            },
            ["ViolenceTerms"] = new List<string> 
            { 
                "kill", "murder", "assault", "weapon", "gun", "knife"
            },
            ["HateSpeech"] = new List<string> 
            { 
                "slur", "racist", "bigot", "discriminate"
            }
        };
    }
}

/// <summary>
/// Represents a matched keyword in content
/// </summary>
public class KeywordMatch
{
    public string MatchedText { get; set; } = string.Empty;
    public int Position { get; set; }
    public int Length { get; set; }
}
