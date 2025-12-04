using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Assigns voices to characters based on dialogue analysis.
/// </summary>
public class VoiceAssignmentService : IVoiceAssignmentService
{
    private readonly TtsProviderFactory _ttsFactory;
    private readonly ILogger<VoiceAssignmentService> _logger;

    public VoiceAssignmentService(
        TtsProviderFactory ttsFactory,
        ILogger<VoiceAssignmentService> logger)
    {
        _ttsFactory = ttsFactory ?? throw new ArgumentNullException(nameof(ttsFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<VoiceAssignment> AssignVoicesAsync(
        DialogueAnalysis dialogue,
        VoiceAssignmentSettings settings,
        CancellationToken ct = default)
    {
        if (dialogue == null)
        {
            throw new ArgumentNullException(nameof(dialogue));
        }

        settings ??= new VoiceAssignmentSettings();

        _logger.LogInformation(
            "Assigning voices to {CharacterCount} characters with {LineCount} lines",
            dialogue.Characters.Count,
            dialogue.Lines.Count);

        var assignments = new Dictionary<string, VoiceDescriptor>();

        // Use explicit assignments first
        if (settings.ExplicitAssignments != null)
        {
            foreach (var (character, voice) in settings.ExplicitAssignments)
            {
                assignments[character] = voice;
                _logger.LogDebug("Explicit assignment: {Character} -> {Voice}", character, voice.Name);
            }
        }

        // Auto-assign remaining characters from pool
        if (settings.AutoAssignFromPool)
        {
            var pool = settings.VoicePool ?? await GetDefaultVoicePoolAsync(ct).ConfigureAwait(false);
            var usedVoices = new HashSet<string>(assignments.Values.Select(v => v.Id));

            foreach (var character in dialogue.Characters)
            {
                if (assignments.ContainsKey(character.Name))
                {
                    continue;
                }

                var matchingVoice = FindMatchingVoice(character.SuggestedVoiceType, pool, usedVoices);

                if (matchingVoice != null)
                {
                    assignments[character.Name] = matchingVoice;
                    usedVoices.Add(matchingVoice.Id);
                    _logger.LogDebug(
                        "Auto-assigned: {Character} -> {Voice} (suggested: {SuggestedType})",
                        character.Name,
                        matchingVoice.Name,
                        character.SuggestedVoiceType);
                }
            }
        }

        // Build voiced lines
        var defaultNarrator = settings.NarratorVoice ?? GetDefaultNarratorVoice();
        var voicedLines = dialogue.Lines.Select(line =>
        {
            var characterName = line.CharacterName ?? "Narrator";
            var voice = assignments.GetValueOrDefault(characterName, defaultNarrator);
            var spec = BuildVoiceSpec(voice, line.Emotion);
            return new VoicedLine(line, voice, spec);
        }).ToList();

        _logger.LogInformation(
            "Voice assignment complete: {AssignmentCount} characters assigned",
            assignments.Count);

        return new VoiceAssignment(assignments, voicedLines);
    }

    private async Task<IReadOnlyList<VoiceDescriptor>> GetDefaultVoicePoolAsync(CancellationToken ct)
    {
        var voices = new List<VoiceDescriptor>();

        try
        {
            var providers = _ttsFactory.CreateAvailableProviders();
            
            foreach (var (providerName, provider) in providers)
            {
                try
                {
                    var voiceNames = await provider.GetAvailableVoicesAsync().ConfigureAwait(false);
                    var voiceProvider = MapProviderName(providerName);

                    foreach (var name in voiceNames.Take(5)) // Limit to 5 per provider
                    {
                        voices.Add(new VoiceDescriptor
                        {
                            Id = $"{providerName}_{name}",
                            Name = name,
                            Provider = voiceProvider,
                            Locale = "en-US",
                            Gender = InferGenderFromName(name),
                            VoiceType = VoiceType.Neural,
                            SupportedFeatures = VoiceFeatures.Basic
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get voices from provider {Provider}", providerName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build voice pool from providers");
        }

        // Always include a default fallback
        if (voices.Count == 0)
        {
            voices.Add(GetDefaultNarratorVoice());
        }

        return voices;
    }

    private static VoiceProvider MapProviderName(string providerName)
    {
        return providerName.ToUpperInvariant() switch
        {
            "ELEVENLABS" => VoiceProvider.ElevenLabs,
            "AZURE" => VoiceProvider.Azure,
            "PLAYHT" => VoiceProvider.PlayHT,
            "PIPER" => VoiceProvider.Piper,
            "MIMIC3" => VoiceProvider.Mimic3,
            "WINDOWS" => VoiceProvider.WindowsSAPI,
            _ => VoiceProvider.Mock
        };
    }

    private static VoiceGender InferGenderFromName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("female") || lower.Contains("woman") || 
            lower.Contains("jenny") || lower.Contains("aria") || lower.Contains("sarah"))
        {
            return VoiceGender.Female;
        }
        if (lower.Contains("male") || lower.Contains("man") || 
            lower.Contains("adam") || lower.Contains("sam") || lower.Contains("josh"))
        {
            return VoiceGender.Male;
        }
        return VoiceGender.Neutral;
    }

    private VoiceDescriptor? FindMatchingVoice(
        string suggestedType,
        IReadOnlyList<VoiceDescriptor> pool,
        HashSet<string> usedVoices)
    {
        if (pool.Count == 0)
        {
            return null;
        }

        // Parse suggested type (e.g., "male-young", "female-mature")
        var parts = suggestedType.Split('-');
        var genderHint = parts.Length > 0 ? parts[0].ToLowerInvariant() : null;

        VoiceGender? preferredGender = genderHint switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            _ => null
        };

        // Try to find a matching voice not already used
        var available = pool.Where(v => !usedVoices.Contains(v.Id)).ToList();

        if (available.Count == 0)
        {
            // All voices used, pick any from pool
            available = pool.ToList();
        }

        // Prefer matching gender
        if (preferredGender.HasValue)
        {
            var genderMatch = available.FirstOrDefault(v => v.Gender == preferredGender.Value);
            if (genderMatch != null)
            {
                return genderMatch;
            }
        }

        // Return first available
        return available.FirstOrDefault();
    }

    private static VoiceDescriptor GetDefaultNarratorVoice()
    {
        return new VoiceDescriptor
        {
            Id = "default_narrator",
            Name = "Default Narrator",
            Provider = VoiceProvider.Mock,
            Locale = "en-US",
            Gender = VoiceGender.Neutral,
            VoiceType = VoiceType.Neural,
            SupportedFeatures = VoiceFeatures.Basic
        };
    }

    private static VoiceSpec BuildVoiceSpec(VoiceDescriptor voice, EmotionHint? emotion)
    {
        var baseRate = 1.0;
        var basePitch = 0.0;

        if (emotion.HasValue)
        {
            // Adjust rate and pitch based on emotion
            (baseRate, basePitch) = emotion.Value switch
            {
                EmotionHint.Excited => (1.15, 0.1),
                EmotionHint.Sad => (0.9, -0.05),
                EmotionHint.Angry => (1.1, 0.05),
                EmotionHint.Curious => (1.0, 0.05),
                EmotionHint.Calm => (0.95, -0.02),
                _ => (1.0, 0.0)
            };
        }

        return new VoiceSpec(voice.Name, baseRate, basePitch, PauseStyle.Natural);
    }
}
