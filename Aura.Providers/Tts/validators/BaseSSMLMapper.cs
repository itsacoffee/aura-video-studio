using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;

namespace Aura.Providers.Tts.validators;

/// <summary>
/// Base class for SSML mappers with common functionality
/// </summary>
public abstract class BaseSSMLMapper : ISSMLMapper
{
    public abstract VoiceProvider Provider { get; }
    
    public abstract ProviderSSMLConstraints GetConstraints();

    public abstract string MapToSSML(string text, ProsodyAdjustments adjustments, VoiceSpec voiceSpec);

    public virtual Aura.Core.Models.Audio.SSMLValidationResult Validate(string ssml)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var suggestions = new List<SSMLRepairSuggestion>();

        if (string.IsNullOrWhiteSpace(ssml))
        {
            errors.Add("SSML is empty");
            return new Aura.Core.Models.Audio.SSMLValidationResult { IsValid = false, Errors = errors };
        }

        if (!ssml.Contains("<speak>") && !ssml.Contains("<speak "))
        {
            errors.Add("SSML must be wrapped in <speak> tags");
            suggestions.Add(new SSMLRepairSuggestion(
                "Missing <speak> wrapper",
                "Wrap content in <speak></speak> tags",
                true));
        }

        return new Aura.Core.Models.Audio.SSMLValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            RepairSuggestions = suggestions
        };
    }

    public virtual string AutoRepair(string ssml)
    {
        if (string.IsNullOrWhiteSpace(ssml))
        {
            return "<speak></speak>";
        }

        ssml = ssml.Trim();

        if (!ssml.StartsWith("<speak"))
        {
            ssml = "<speak>" + ssml;
        }

        if (!ssml.EndsWith("</speak>"))
        {
            ssml += "</speak>";
        }

        return ssml;
    }

    public virtual Task<int> EstimateDurationAsync(string ssml, VoiceSpec voiceSpec, CancellationToken ct = default)
    {
        var textContent = Regex.Replace(ssml, @"<[^>]+>", "");
        var wordCount = textContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        var baseRate = 150.0;
        var rate = voiceSpec.Rate > 0 ? voiceSpec.Rate : 1.0;
        var wordsPerMinute = baseRate * rate;
        
        var baseDurationMs = (int)((wordCount / wordsPerMinute) * 60000);
        
        var pauseMatches = Regex.Matches(ssml, @"<break\s+time=""(\d+)ms""\s*/>");
        var totalPauseMs = 0;
        foreach (Match match in pauseMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var pauseMs))
            {
                totalPauseMs += pauseMs;
            }
        }
        
        var sentencePauseCount = Regex.Matches(textContent, @"[.!?]").Count;
        totalPauseMs += sentencePauseCount * 300;
        
        var commaPauseCount = Regex.Matches(textContent, @"[,;:]").Count;
        totalPauseMs += commaPauseCount * 150;
        
        return Task.FromResult(baseDurationMs + totalPauseMs);
    }

    protected string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + (text.Length / 10));
        
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            if (c == '&')
            {
                if (i + 1 < text.Length && IsStartOfXmlEntity(text, i))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append("&amp;");
                }
            }
            else if (c == '<')
            {
                sb.Append("&lt;");
            }
            else if (c == '>')
            {
                sb.Append("&gt;");
            }
            else if (c == '"')
            {
                sb.Append("&quot;");
            }
            else if (c == '\'')
            {
                sb.Append("&apos;");
            }
            else
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString();
    }

    private static bool IsStartOfXmlEntity(string text, int ampersandIndex)
    {
        var remainingLength = text.Length - ampersandIndex;
        if (remainingLength < 4)
        {
            return false;
        }

        var slice = text.Substring(ampersandIndex, Math.Min(10, remainingLength));
        
        return slice.StartsWith("&amp;", StringComparison.Ordinal) ||
               slice.StartsWith("&lt;", StringComparison.Ordinal) ||
               slice.StartsWith("&gt;", StringComparison.Ordinal) ||
               slice.StartsWith("&quot;", StringComparison.Ordinal) ||
               slice.StartsWith("&apos;", StringComparison.Ordinal);
    }

    protected string InsertPauses(string text, IReadOnlyDictionary<int, int> pauses, int maxPauseMs)
    {
        if (pauses.Count == 0)
        {
            return text;
        }

        var sb = new StringBuilder();
        var lastPos = 0;

        foreach (var pause in pauses.OrderBy(p => p.Key))
        {
            if (pause.Key > lastPos && pause.Key <= text.Length)
            {
                sb.Append(text[lastPos..pause.Key]);
                var clampedPause = Math.Min(pause.Value, maxPauseMs);
                sb.Append($"<break time=\"{clampedPause}ms\"/>");
                lastPos = pause.Key;
            }
        }

        if (lastPos < text.Length)
        {
            sb.Append(text[lastPos..]);
        }

        return sb.ToString();
    }

    protected string ApplyEmphasis(string text, IReadOnlyList<EmphasisSpan> emphasisSpans)
    {
        if (emphasisSpans.Count == 0)
        {
            return text;
        }

        var sb = new StringBuilder();
        var lastPos = 0;

        foreach (var span in emphasisSpans.OrderBy(e => e.StartPosition))
        {
            if (span.StartPosition >= lastPos && span.StartPosition + span.Length <= text.Length)
            {
                sb.Append(text[lastPos..span.StartPosition]);
                
                var level = span.Level switch
                {
                    Aura.Core.Models.Voice.EmphasisLevel.Strong => "strong",
                    Aura.Core.Models.Voice.EmphasisLevel.Moderate => "moderate",
                    Aura.Core.Models.Voice.EmphasisLevel.Reduced => "reduced",
                    _ => "none"
                };

                if (level != "none")
                {
                    sb.Append($"<emphasis level=\"{level}\">");
                    sb.Append(text.Substring(span.StartPosition, span.Length));
                    sb.Append("</emphasis>");
                }
                else
                {
                    sb.Append(text.Substring(span.StartPosition, span.Length));
                }

                lastPos = span.StartPosition + span.Length;
            }
        }

        if (lastPos < text.Length)
        {
            sb.Append(text[lastPos..]);
        }

        return sb.ToString();
    }
}
