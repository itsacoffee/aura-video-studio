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
/// SSML mapper for ElevenLabs TTS provider
/// </summary>
public class ElevenLabsSSMLMapper : BaseSSMLMapper
{
    public override VoiceProvider Provider => VoiceProvider.ElevenLabs;

    public override ProviderSSMLConstraints GetConstraints()
    {
        return new ProviderSSMLConstraints
        {
            SupportedTags = new HashSet<string> { "speak", "break", "emphasis", "prosody", "say-as" },
            SupportedProsodyAttributes = new HashSet<string> { "rate", "pitch", "volume" },
            RateRange = (0.5, 2.0),
            PitchRange = (-12.0, 12.0),
            VolumeRange = (0.0, 2.0),
            MaxPauseDurationMs = 5000,
            SupportsTimingMarkers = false,
            MaxTextLength = 5000
        };
    }

    public override string MapToSSML(string text, ProsodyAdjustments adjustments, VoiceSpec voiceSpec)
    {
        var sb = new StringBuilder();
        sb.Append("<speak>");

        var constraints = GetConstraints();
        var rate = Math.Clamp(adjustments.Rate, constraints.RateRange.Min, constraints.RateRange.Max);
        var pitch = Math.Clamp(adjustments.Pitch, constraints.PitchRange.Min, constraints.PitchRange.Max);
        var volume = Math.Clamp(adjustments.Volume, constraints.VolumeRange.Min, constraints.VolumeRange.Max);

        var hasProsody = Math.Abs(rate - 1.0) > 0.01 || 
                        Math.Abs(pitch) > 0.01 || 
                        Math.Abs(volume - 1.0) > 0.01;

        if (hasProsody)
        {
            sb.Append("<prosody");
            
            if (Math.Abs(rate - 1.0) > 0.01)
            {
                var ratePercent = (int)((rate - 1.0) * 100);
                sb.Append($" rate=\"{(ratePercent >= 0 ? "+" : "")}{ratePercent}%\"");
            }
            
            if (Math.Abs(pitch) > 0.01)
            {
                sb.Append($" pitch=\"{(pitch >= 0 ? "+" : "")}{pitch:F1}st\"");
            }
            
            if (Math.Abs(volume - 1.0) > 0.01)
            {
                var volumeDb = (int)((volume - 1.0) * 6);
                sb.Append($" volume=\"{(volumeDb >= 0 ? "+" : "")}{volumeDb}dB\"");
            }
            
            sb.Append('>');
        }

        var escapedText = EscapeXml(text);
        var textWithPauses = InsertPauses(escapedText, adjustments.Pauses, constraints.MaxPauseDurationMs);
        var textWithEmphasis = ApplyEmphasis(textWithPauses, adjustments.Emphasis);
        
        sb.Append(textWithEmphasis);

        if (hasProsody)
        {
            sb.Append("</prosody>");
        }

        sb.Append("</speak>");

        return sb.ToString();
    }

    public override Aura.Core.Models.Audio.SSMLValidationResult Validate(string ssml)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var suggestions = new List<SSMLRepairSuggestion>();

        if (string.IsNullOrWhiteSpace(ssml))
        {
            errors.Add("SSML is empty");
            return new Aura.Core.Models.Audio.SSMLValidationResult { IsValid = false, Errors = errors };
        }

        var speakOpenIndex = ssml.IndexOf("<speak", StringComparison.Ordinal);
        var speakCloseIndex = ssml.LastIndexOf("</speak>", StringComparison.Ordinal);
        
        if (speakOpenIndex < 0 || speakCloseIndex < 0)
        {
            errors.Add("SSML must be wrapped in <speak> tags");
            suggestions.Add(new SSMLRepairSuggestion(
                "Missing <speak> wrapper",
                "Wrap content in <speak></speak> tags",
                true));
        }
        else if (speakOpenIndex >= speakCloseIndex)
        {
            errors.Add("SSML has malformed <speak> tag structure (closing tag before opening tag)");
            suggestions.Add(new SSMLRepairSuggestion(
                "Malformed tag structure",
                "Ensure <speak> tag comes before </speak>",
                true));
        }

        var constraints = GetConstraints();
        var unsupportedTags = FindUnsupportedTags(ssml, constraints.SupportedTags);
        if (unsupportedTags.Count != 0)
        {
            errors.Add($"Unsupported tags: {string.Join(", ", unsupportedTags)}");
            suggestions.Add(new SSMLRepairSuggestion(
                $"Unsupported tags: {string.Join(", ", unsupportedTags)}",
                "Remove or replace with supported tags",
                true));
        }

        var rateMatches = Regex.Matches(ssml, @"rate=""([^""]+)""");
        foreach (Match match in rateMatches)
        {
            var rateValue = match.Groups[1].Value;
            if (!IsValidRateValue(rateValue, constraints.RateRange))
            {
                warnings.Add($"Rate value '{rateValue}' may be out of range ({constraints.RateRange.Min}-{constraints.RateRange.Max})");
            }
        }

        var textContent = Regex.Replace(ssml, @"<[^>]+>", "");
        if (constraints.MaxTextLength.HasValue && textContent.Length > constraints.MaxTextLength.Value)
        {
            warnings.Add($"Text length {textContent.Length} exceeds recommended maximum {constraints.MaxTextLength}");
        }

        return new Aura.Core.Models.Audio.SSMLValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            RepairSuggestions = suggestions
        };
    }

    public override string AutoRepair(string ssml)
    {
        if (string.IsNullOrWhiteSpace(ssml))
        {
            return "<speak></speak>";
        }

        ssml = ssml.Trim();

        if (!ssml.StartsWith("<speak>"))
        {
            ssml = "<speak>" + ssml;
        }

        if (!ssml.EndsWith("</speak>"))
        {
            ssml += "</speak>";
        }

        var constraints = GetConstraints();
        var unsupportedTags = FindUnsupportedTags(ssml, constraints.SupportedTags);
        foreach (var tag in unsupportedTags)
        {
            ssml = Regex.Replace(ssml, $"<{tag}[^>]*>", "");
            ssml = Regex.Replace(ssml, $"</{tag}>", "");
        }

        ssml = Regex.Replace(ssml, @"<break\s+time=""(\d+)ms""\s*/>", match =>
        {
            if (int.TryParse(match.Groups[1].Value, out var ms))
            {
                var clamped = Math.Min(ms, constraints.MaxPauseDurationMs);
                return $"<break time=\"{clamped}ms\"/>";
            }
            return match.Value;
        });

        return ssml;
    }

    public override Task<int> EstimateDurationAsync(string ssml, VoiceSpec voiceSpec, CancellationToken ct = default)
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

    private string InsertPauses(string text, IReadOnlyDictionary<int, int> pauses, int maxPauseMs)
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

    private string ApplyEmphasis(string text, IReadOnlyList<EmphasisSpan> emphasisSpans)
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

    private List<string> FindUnsupportedTags(string ssml, IReadOnlySet<string> supportedTags)
    {
        var unsupported = new List<string>();
        var tagMatches = Regex.Matches(ssml, @"</?(\w+)");
        
        foreach (Match match in tagMatches)
        {
            var tag = match.Groups[1].Value;
            if (!supportedTags.Contains(tag) && !unsupported.Contains(tag))
            {
                unsupported.Add(tag);
            }
        }

        return unsupported;
    }

    private bool IsValidRateValue(string rateValue, (double Min, double Max) range)
    {
        if (rateValue.EndsWith('%'))
        {
            if (double.TryParse(rateValue.TrimEnd('%'), out var percent))
            {
                var rate = 1.0 + (percent / 100.0);
                return rate >= range.Min && rate <= range.Max;
            }
        }
        else if (double.TryParse(rateValue, out var rate))
        {
            return rate >= range.Min && rate <= range.Max;
        }

        return true;
    }
}
