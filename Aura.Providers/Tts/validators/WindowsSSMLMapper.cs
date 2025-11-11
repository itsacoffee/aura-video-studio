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
/// SSML mapper for Windows SAPI TTS provider
/// </summary>
public class WindowsSSMLMapper : BaseSSMLMapper
{
    public override VoiceProvider Provider => VoiceProvider.WindowsSAPI;

    public override ProviderSSMLConstraints GetConstraints()
    {
        return new ProviderSSMLConstraints
        {
            SupportedTags = new HashSet<string> { "speak", "break", "emphasis", "prosody", "voice", "phoneme", "say-as" },
            SupportedProsodyAttributes = new HashSet<string> { "rate", "pitch", "volume" },
            RateRange = (0.5, 3.0),
            PitchRange = (-10.0, 10.0),
            VolumeRange = (0.0, 2.0),
            MaxPauseDurationMs = 60000,
            SupportsTimingMarkers = true,
            MaxTextLength = null
        };
    }

    public override string MapToSSML(string text, ProsodyAdjustments adjustments, VoiceSpec voiceSpec)
    {
        var sb = new StringBuilder();
        sb.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">");

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
                var rateValue = rate switch
                {
                    < 0.7 => "x-slow",
                    < 0.9 => "slow",
                    > 1.3 => "x-fast",
                    > 1.1 => "fast",
                    _ => "medium"
                };
                sb.Append($" rate=\"{rateValue}\"");
            }
            
            if (Math.Abs(pitch) > 0.01)
            {
                var pitchValue = pitch switch
                {
                    < -5 => "x-low",
                    < -2 => "low",
                    > 5 => "x-high",
                    > 2 => "high",
                    _ => "medium"
                };
                sb.Append($" pitch=\"{pitchValue}\"");
            }
            
            if (Math.Abs(volume - 1.0) > 0.01)
            {
                var volumeValue = volume switch
                {
                    < 0.3 => "silent",
                    < 0.6 => "soft",
                    > 1.5 => "x-loud",
                    > 1.2 => "loud",
                    _ => "medium"
                };
                sb.Append($" volume=\"{volumeValue}\"");
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

        if (!ssml.Contains("<speak"))
        {
            errors.Add("SSML must start with <speak> tag");
            suggestions.Add(new SSMLRepairSuggestion(
                "Missing <speak> tag",
                "Add proper <speak> tag with namespace",
                true));
        }

        var constraints = GetConstraints();
        var unsupportedTags = FindUnsupportedTags(ssml, constraints.SupportedTags);
        if (unsupportedTags.Any())
        {
            warnings.Add($"Unsupported tags (will be ignored): {string.Join(", ", unsupportedTags)}");
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
            return "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"></speak>";
        }

        ssml = ssml.Trim();

        if (!ssml.StartsWith("<speak"))
        {
            ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" + ssml;
        }

        if (!ssml.EndsWith("</speak>"))
        {
            ssml += "</speak>";
        }

        return ssml;
    }

    public override Task<int> EstimateDurationAsync(string ssml, VoiceSpec voiceSpec, CancellationToken ct = default)
    {
        var textContent = Regex.Replace(ssml, @"<[^>]+>", "");
        var wordCount = textContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        var baseRate = 140.0;
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
        totalPauseMs += sentencePauseCount * 400;
        
        var commaPauseCount = Regex.Matches(textContent, @"[,;:]").Count;
        totalPauseMs += commaPauseCount * 200;
        
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
}
