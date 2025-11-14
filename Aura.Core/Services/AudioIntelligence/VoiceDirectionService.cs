using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for generating TTS voice direction and optimization
/// </summary>
public class VoiceDirectionService
{
    private readonly ILogger<VoiceDirectionService> _logger;
    private readonly ILlmProvider _llmProvider;

    public VoiceDirectionService(
        ILogger<VoiceDirectionService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Generates voice direction for script lines
    /// </summary>
    public async Task<List<VoiceDirection>> GenerateVoiceDirectionAsync(
        string script,
        string? contentType = null,
        string? targetAudience = null,
        List<string>? keyMessages = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating voice direction for script (length: {Length})", script.Length);

        try
        {
            // Split script into lines
            var lines = SplitScriptIntoLines(script);
            var directions = new List<VoiceDirection>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var direction = AnalyzeLine(line, i, contentType, targetAudience, keyMessages);
                directions.Add(direction);
            }

            // Use AI for more sophisticated analysis if available
            if (_llmProvider != null)
            {
                directions = await EnhanceWithAIAsync(directions, lines, contentType, ct).ConfigureAwait(false);
            }

            return directions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating voice direction");
            throw;
        }
    }

    /// <summary>
    /// Analyzes a single line for voice direction
    /// </summary>
    private VoiceDirection AnalyzeLine(
        string line,
        int lineIndex,
        string? contentType,
        string? targetAudience,
        List<string>? keyMessages)
    {
        // Determine emotion based on content
        var emotion = DetermineEmotion(line);
        
        // Find emphasis words
        var emphasisWords = FindEmphasisWords(line, keyMessages);
        
        // Determine pacing
        var paceMultiplier = DeterminePacing(line);
        
        // Determine tone
        var tone = DetermineTone(line, contentType);
        
        // Find natural pause points
        var pauses = FindPausePoints(line);

        return new VoiceDirection(
            LineId: $"line_{lineIndex}",
            Emotion: emotion,
            EmphasisWords: emphasisWords,
            PaceMultiplier: paceMultiplier,
            Tone: tone,
            Pauses: pauses,
            PronunciationGuide: null
        );
    }

    /// <summary>
    /// Determines emotional delivery from content
    /// </summary>
    private EmotionalDelivery DetermineEmotion(string line)
    {
        var lowerLine = line.ToLowerInvariant();

        // Check for excitement indicators
        if (line.Contains('!') || lowerLine.Contains("amazing") || lowerLine.Contains("incredible") ||
            lowerLine.Contains("wow") || lowerLine.Contains("fantastic"))
        {
            return EmotionalDelivery.Excited;
        }

        // Check for urgency
        if (lowerLine.Contains("urgent") || lowerLine.Contains("immediately") || 
            lowerLine.Contains("now") || lowerLine.Contains("quick"))
        {
            return EmotionalDelivery.Urgent;
        }

        // Check for questions (curious/engaged)
        if (line.Contains('?'))
        {
            return EmotionalDelivery.Friendly;
        }

        // Check for serious/formal indicators
        if (lowerLine.Contains("important") || lowerLine.Contains("critical") || 
            lowerLine.Contains("must") || lowerLine.Contains("essential"))
        {
            return EmotionalDelivery.Serious;
        }

        // Check for warm/friendly indicators
        if (lowerLine.Contains("welcome") || lowerLine.Contains("hello") || 
            lowerLine.Contains("thank") || lowerLine.Contains("appreciate"))
        {
            return EmotionalDelivery.Warm;
        }

        // Check for enthusiasm
        if (lowerLine.Contains("excited") || lowerLine.Contains("love") || 
            lowerLine.Contains("great"))
        {
            return EmotionalDelivery.Enthusiastic;
        }

        return EmotionalDelivery.Neutral;
    }

    /// <summary>
    /// Finds words that should be emphasized
    /// </summary>
    private List<string> FindEmphasisWords(string line, List<string>? keyMessages)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add key message words if provided
        if (keyMessages != null)
        {
            foreach (var message in keyMessages)
            {
                var messageWords = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in messageWords)
                {
                    if (line.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        words.Add(word);
                    }
                }
            }
        }

        // Find words in ALL CAPS (manual emphasis)
        var capsWords = Regex.Matches(line, @"\b[A-Z]{2,}\b")
            .Select(m => m.Value);
        foreach (var word in capsWords)
        {
            words.Add(word);
        }

        // Find emphasized words (importance indicators)
        var emphasisIndicators = new[] 
        { 
            "important", "key", "critical", "essential", "must", "never", "always",
            "first", "best", "only", "unique", "exclusive", "revolutionary", "breakthrough"
        };

        var lineWords = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in lineWords)
        {
            var cleanWord = Regex.Replace(word, @"[^\w]", "");
            if (emphasisIndicators.Contains(cleanWord, StringComparer.OrdinalIgnoreCase))
            {
                words.Add(cleanWord);
            }
        }

        // Find numbers (usually important)
        var numbers = Regex.Matches(line, @"\b\d+[%]?\b")
            .Select(m => m.Value);
        foreach (var num in numbers)
        {
            words.Add(num);
        }

        return words.ToList();
    }

    /// <summary>
    /// Determines speaking pace
    /// </summary>
    private double DeterminePacing(string line)
    {
        var lowerLine = line.ToLowerInvariant();

        // Slow down for important/serious content
        if (lowerLine.Contains("important") || lowerLine.Contains("remember") ||
            lowerLine.Contains("critical") || lowerLine.Contains("careful"))
        {
            return 0.85; // 15% slower
        }

        // Speed up for exciting content
        if (line.Contains('!') || lowerLine.Contains("quick") || lowerLine.Contains("fast"))
        {
            return 1.15; // 15% faster
        }

        // Complex sentences should be slower
        if (line.Split(' ').Length > 25)
        {
            return 0.9; // 10% slower for long sentences
        }

        return 1.0; // Normal pace
    }

    /// <summary>
    /// Determines overall tone
    /// </summary>
    private string DetermineTone(string line, string? contentType)
    {
        if (contentType?.ToLowerInvariant() == "educational")
        {
            return "informative";
        }

        if (contentType?.ToLowerInvariant() == "corporate")
        {
            return "professional";
        }

        if (line.Contains('?'))
        {
            return "curious";
        }

        if (line.Contains('!'))
        {
            return "enthusiastic";
        }

        return "conversational";
    }

    /// <summary>
    /// Finds natural pause points
    /// </summary>
    private List<PausePoint> FindPausePoints(string line)
    {
        var pauses = new List<PausePoint>();

        // Pause at commas (short pause)
        var commaMatches = Regex.Matches(line, ",");
        foreach (Match match in commaMatches)
        {
            pauses.Add(new PausePoint(match.Index, TimeSpan.FromMilliseconds(300)));
        }

        // Pause at periods (longer pause)
        var periodMatches = Regex.Matches(line, @"\.");
        foreach (Match match in periodMatches)
        {
            pauses.Add(new PausePoint(match.Index, TimeSpan.FromMilliseconds(500)));
        }

        // Pause at colons (medium pause)
        var colonMatches = Regex.Matches(line, ":");
        foreach (Match match in colonMatches)
        {
            pauses.Add(new PausePoint(match.Index, TimeSpan.FromMilliseconds(400)));
        }

        // Pause after em dashes (medium pause)
        var dashMatches = Regex.Matches(line, "—|--");
        foreach (Match match in dashMatches)
        {
            pauses.Add(new PausePoint(match.Index, TimeSpan.FromMilliseconds(350)));
        }

        return pauses.OrderBy(p => p.CharacterPosition).ToList();
    }

    /// <summary>
    /// Enhances voice direction with AI analysis
    /// </summary>
    private async Task<List<VoiceDirection>> EnhanceWithAIAsync(
        List<VoiceDirection> initialDirections,
        List<string> lines,
        string? contentType,
        CancellationToken ct)
    {
        // In a full implementation, this would use the LLM to refine the voice direction
        // For now, return the initial directions
        await Task.CompletedTask.ConfigureAwait(false);
        return initialDirections;
    }

    /// <summary>
    /// Splits script into manageable lines
    /// </summary>
    private List<string> SplitScriptIntoLines(string script)
    {
        return script
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }
}
