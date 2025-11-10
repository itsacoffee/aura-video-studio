using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aura.Core.Models.Voice;

/// <summary>
/// SSML parsing and generation utilities
/// </summary>
public static class SSMLParser
{
    /// <summary>
    /// Parses SSML text and extracts plain text content
    /// </summary>
    public static string ExtractPlainText(string ssml)
    {
        if (string.IsNullOrWhiteSpace(ssml))
        {
            return string.Empty;
        }

        // Remove XML declaration
        ssml = Regex.Replace(ssml, @"<\?xml[^>]*\?>", string.Empty, RegexOptions.IgnoreCase);

        // Remove all SSML tags but keep content
        ssml = Regex.Replace(ssml, @"<[^>]+>", string.Empty);

        // Decode XML entities
        ssml = System.Net.WebUtility.HtmlDecode(ssml);

        return ssml.Trim();
    }

    /// <summary>
    /// Wraps text in basic SSML structure
    /// </summary>
    public static string WrapInSSML(string text, string? voiceName = null, string language = "en-US")
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine($"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{language}\">");
        
        if (!string.IsNullOrEmpty(voiceName))
        {
            sb.AppendLine($"  <voice name=\"{System.Security.SecurityElement.Escape(voiceName)}\">");
            sb.AppendLine($"    {System.Security.SecurityElement.Escape(text)}");
            sb.AppendLine("  </voice>");
        }
        else
        {
            sb.AppendLine($"  {System.Security.SecurityElement.Escape(text)}");
        }
        
        sb.Append("</speak>");
        return sb.ToString();
    }

    /// <summary>
    /// Adds prosody controls to text
    /// </summary>
    public static string AddProsody(
        string text,
        double? rate = null,
        double? pitch = null,
        double? volume = null)
    {
        if (!rate.HasValue && !pitch.HasValue && !volume.HasValue)
        {
            return text;
        }

        var attributes = new List<string>();

        if (rate.HasValue)
        {
            var rateValue = rate.Value switch
            {
                <= 0.5 => "x-slow",
                <= 0.75 => "slow",
                <= 1.25 => "medium",
                <= 1.5 => "fast",
                _ => "x-fast"
            };
            attributes.Add($"rate=\"{rateValue}\"");
        }

        if (pitch.HasValue)
        {
            var pitchStr = pitch.Value >= 0 ? $"+{pitch.Value}st" : $"{pitch.Value}st";
            attributes.Add($"pitch=\"{pitchStr}\"");
        }

        if (volume.HasValue)
        {
            var volumeValue = volume.Value switch
            {
                <= 0.3 => "soft",
                <= 0.7 => "medium",
                <= 1.3 => "loud",
                _ => "x-loud"
            };
            attributes.Add($"volume=\"{volumeValue}\"");
        }

        return $"<prosody {string.Join(" ", attributes)}>{System.Security.SecurityElement.Escape(text)}</prosody>";
    }

    /// <summary>
    /// Adds a break/pause to SSML
    /// </summary>
    public static string AddBreak(PauseStyle style)
    {
        return style switch
        {
            PauseStyle.Short => "<break strength=\"weak\"/>",
            PauseStyle.Natural => "<break strength=\"medium\"/>",
            PauseStyle.Long => "<break strength=\"strong\"/>",
            PauseStyle.Dramatic => "<break time=\"1000ms\"/>",
            _ => "<break strength=\"medium\"/>"
        };
    }

    /// <summary>
    /// Adds a timed break to SSML
    /// </summary>
    public static string AddBreak(TimeSpan duration)
    {
        return $"<break time=\"{duration.TotalMilliseconds}ms\"/>";
    }

    /// <summary>
    /// Adds emphasis to text
    /// </summary>
    public static string AddEmphasis(string text, EmphasisLevel level = EmphasisLevel.Moderate)
    {
        var levelStr = level switch
        {
            EmphasisLevel.Strong => "strong",
            EmphasisLevel.Moderate => "moderate",
            EmphasisLevel.Reduced => "reduced",
            _ => "moderate"
        };

        return $"<emphasis level=\"{levelStr}\">{System.Security.SecurityElement.Escape(text)}</emphasis>";
    }

    /// <summary>
    /// Marks text with a specific interpretation
    /// </summary>
    public static string AddSayAs(string text, SayAsInterpret interpret, string? format = null)
    {
        var interpretStr = interpret.ToString().ToLowerInvariant();
        var formatAttr = !string.IsNullOrEmpty(format) ? $" format=\"{format}\"" : string.Empty;
        
        return $"<say-as interpret-as=\"{interpretStr}\"{formatAttr}>{System.Security.SecurityElement.Escape(text)}</say-as>";
    }

    /// <summary>
    /// Adds a phoneme pronunciation
    /// </summary>
    public static string AddPhoneme(string text, string phoneme, string alphabet = "ipa")
    {
        return $"<phoneme alphabet=\"{alphabet}\" ph=\"{phoneme}\">{System.Security.SecurityElement.Escape(text)}</phoneme>";
    }

    /// <summary>
    /// Validates SSML syntax
    /// </summary>
    public static SSMLValidationResult Validate(string ssml)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(ssml))
        {
            errors.Add("SSML content is empty");
            return new SSMLValidationResult
            {
                IsValid = false,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        // Check for XML declaration
        if (!ssml.Contains("<?xml"))
        {
            warnings.Add("Missing XML declaration");
        }

        // Check for speak element
        if (!ssml.Contains("<speak"))
        {
            errors.Add("Missing <speak> root element");
        }

        // Check for balanced tags
        var openTags = Regex.Matches(ssml, @"<(\w+)(?:\s|>)").Count;
        var closeTags = Regex.Matches(ssml, @"</(\w+)>").Count;
        var selfClosingTags = Regex.Matches(ssml, @"<\w+[^>]*/\s*>").Count;

        if (openTags - selfClosingTags != closeTags)
        {
            errors.Add($"Unbalanced tags: {openTags - selfClosingTags} opening tags, {closeTags} closing tags");
        }

        // Check for unsupported tags (common mistakes)
        var unsupportedTags = new[] { "b", "i", "u", "span", "div", "p" };
        foreach (var tag in unsupportedTags)
        {
            if (Regex.IsMatch(ssml, $@"<{tag}[\s>]", RegexOptions.IgnoreCase))
            {
                warnings.Add($"Tag <{tag}> is not part of SSML standard");
            }
        }

        // Check for invalid prosody rates
        var prosodyRates = Regex.Matches(ssml, @"rate=""([^""]+)""");
        foreach (Match match in prosodyRates)
        {
            var rate = match.Groups[1].Value;
            if (!IsValidProsodyRate(rate))
            {
                warnings.Add($"Invalid prosody rate: {rate}");
            }
        }

        return new SSMLValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.ToArray(),
            Warnings = warnings.ToArray()
        };
    }

    private static bool IsValidProsodyRate(string rate)
    {
        var validRates = new[] { "x-slow", "slow", "medium", "fast", "x-fast" };
        
        if (validRates.Contains(rate, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for percentage format (e.g., "80%", "150%")
        if (Regex.IsMatch(rate, @"^\d+%$"))
        {
            return true;
        }

        // Check for numeric format (e.g., "0.8", "1.5")
        if (double.TryParse(rate, out var numRate) && numRate > 0)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// SSML validation result
/// </summary>
public record SSMLValidationResult
{
    public required bool IsValid { get; init; }
    public required string[] Errors { get; init; }
    public required string[] Warnings { get; init; }

    public string GetSummary()
    {
        if (IsValid && Warnings.Length == 0)
        {
            return "SSML is valid";
        }

        var parts = new List<string>();
        
        if (Errors.Length > 0)
        {
            parts.Add($"{Errors.Length} error(s)");
        }
        
        if (Warnings.Length > 0)
        {
            parts.Add($"{Warnings.Length} warning(s)");
        }

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Emphasis level for text
/// </summary>
public enum EmphasisLevel
{
    Strong,
    Moderate,
    Reduced
}

/// <summary>
/// Say-as interpretation types
/// </summary>
public enum SayAsInterpret
{
    Cardinal,     // 123 -> "one hundred twenty-three"
    Ordinal,      // 123 -> "one hundred twenty-third"
    Characters,   // ABC -> "A B C"
    Fraction,     // 1/2 -> "one half"
    Date,         // 2024-01-01
    Time,         // 12:30
    Telephone,    // 555-1234
    Currency,     // $100.00
    Measure,      // 5kg
    Address,      // Street address
    Expletive     // Bleep out content
}

/// <summary>
/// SSML document builder for complex structures
/// </summary>
public class SSMLBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly Stack<string> _tagStack = new();

    public SSMLBuilder(string language = "en-US")
    {
        _builder.AppendLine("<?xml version=\"1.0\"?>");
        _builder.AppendLine($"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{language}\">");
        _tagStack.Push("speak");
    }

    public SSMLBuilder AddVoice(string voiceName)
    {
        _builder.AppendLine($"  <voice name=\"{System.Security.SecurityElement.Escape(voiceName)}\">");
        _tagStack.Push("voice");
        return this;
    }

    public SSMLBuilder EndVoice()
    {
        if (_tagStack.Count > 1 && _tagStack.Peek() == "voice")
        {
            _tagStack.Pop();
            _builder.AppendLine("  </voice>");
        }
        return this;
    }

    public SSMLBuilder AddText(string text)
    {
        var indent = new string(' ', (_tagStack.Count - 1) * 2);
        _builder.AppendLine($"{indent}  {System.Security.SecurityElement.Escape(text)}");
        return this;
    }

    public SSMLBuilder AddProsody(string text, double? rate = null, double? pitch = null)
    {
        var prosodyText = SSMLParser.AddProsody(text, rate, pitch);
        var indent = new string(' ', (_tagStack.Count - 1) * 2);
        _builder.AppendLine($"{indent}  {prosodyText}");
        return this;
    }

    public SSMLBuilder AddBreak(PauseStyle style)
    {
        var indent = new string(' ', (_tagStack.Count - 1) * 2);
        _builder.AppendLine($"{indent}  {SSMLParser.AddBreak(style)}");
        return this;
    }

    public SSMLBuilder AddBreak(TimeSpan duration)
    {
        var indent = new string(' ', (_tagStack.Count - 1) * 2);
        _builder.AppendLine($"{indent}  {SSMLParser.AddBreak(duration)}");
        return this;
    }

    public SSMLBuilder AddEmphasis(string text, EmphasisLevel level = EmphasisLevel.Moderate)
    {
        var indent = new string(' ', (_tagStack.Count - 1) * 2);
        _builder.AppendLine($"{indent}  {SSMLParser.AddEmphasis(text, level)}");
        return this;
    }

    public string Build()
    {
        // Close any open tags
        while (_tagStack.Count > 1)
        {
            var tag = _tagStack.Pop();
            _builder.AppendLine($"</{tag}>");
        }

        _builder.AppendLine("</speak>");
        return _builder.ToString();
    }
}
