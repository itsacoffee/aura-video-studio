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
/// SSML mapper for Piper TTS provider (limited SSML support)
/// </summary>
public class PiperSSMLMapper : BaseSSMLMapper
{
    public override VoiceProvider Provider => VoiceProvider.Piper;

    public override ProviderSSMLConstraints GetConstraints()
    {
        return new ProviderSSMLConstraints
        {
            SupportedTags = new HashSet<string> { "speak", "break", "phoneme" },
            SupportedProsodyAttributes = new HashSet<string>(),
            RateRange = (1.0, 1.0),
            PitchRange = (0.0, 0.0),
            VolumeRange = (1.0, 1.0),
            MaxPauseDurationMs = 5000,
            SupportsTimingMarkers = false,
            MaxTextLength = null
        };
    }

    public override string MapToSSML(string text, ProsodyAdjustments adjustments, VoiceSpec voiceSpec)
    {
        var sb = new StringBuilder();
        sb.Append("<speak>");

        var escapedText = EscapeXml(text);
        var textWithPauses = InsertPauses(escapedText, adjustments.Pauses);
        sb.Append(textWithPauses);

        sb.Append("</speak>");

        return sb.ToString();
    }

    public override Aura.Core.Models.Audio.SSMLValidationResult Validate(string ssml)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(ssml))
        {
            errors.Add("SSML is empty");
            return new Aura.Core.Models.Audio.SSMLValidationResult { IsValid = false, Errors = errors };
        }

        if (!ssml.Contains("<speak>"))
        {
            errors.Add("SSML must be wrapped in <speak> tags");
        }

        if (ssml.Contains("<prosody"))
        {
            warnings.Add("Piper does not support prosody tags; they will be ignored");
        }

        if (ssml.Contains("<emphasis"))
        {
            warnings.Add("Piper does not support emphasis tags; they will be ignored");
        }

        return new Aura.Core.Models.Audio.SSMLValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
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

        ssml = Regex.Replace(ssml, @"<prosody[^>]*>", "");
        ssml = Regex.Replace(ssml, @"</prosody>", "");
        ssml = Regex.Replace(ssml, @"<emphasis[^>]*>", "");
        ssml = Regex.Replace(ssml, @"</emphasis>", "");

        return ssml;
    }

    public override Task<int> EstimateDurationAsync(string ssml, VoiceSpec voiceSpec, CancellationToken ct = default)
    {
        var textContent = Regex.Replace(ssml, @"<[^>]+>", "");
        var wordCount = textContent.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        var baseRate = 130.0;
        var baseDurationMs = (int)((wordCount / baseRate) * 60000);
        
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
        
        return Task.FromResult(baseDurationMs + totalPauseMs);
    }

    private string InsertPauses(string text, IReadOnlyDictionary<int, int> pauses)
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
                var clampedPause = Math.Min(pause.Value, 5000);
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
}
