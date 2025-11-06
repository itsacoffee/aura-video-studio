using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Registry for voice providers and their available voices
/// Provides voice validation and fallback suggestions
/// </summary>
public class VoiceProviderRegistry
{
    private readonly ILogger<VoiceProviderRegistry> _logger;
    private readonly Dictionary<VoiceProvider, Dictionary<string, List<VoiceInfo>>> _voiceDatabase;

    public VoiceProviderRegistry(ILogger<VoiceProviderRegistry> logger)
    {
        _logger = logger;
        _voiceDatabase = InitializeVoiceDatabase();
    }

    /// <summary>
    /// Validate if a voice exists for the given provider and language
    /// </summary>
    public VoiceValidationResult ValidateVoice(VoiceProvider provider, string language, string voiceName)
    {
        var normalizedLanguage = NormalizeLanguageCode(language);
        var availableVoices = GetAvailableVoices(provider, normalizedLanguage);

        if (availableVoices.Count == 0)
        {
            return new VoiceValidationResult
            {
                IsValid = false,
                ErrorMessage = $"No voices available for provider {provider} and language {language}",
                AvailableVoices = new List<VoiceInfo>()
            };
        }

        var matchedVoice = availableVoices.FirstOrDefault(v =>
            v.VoiceName.Equals(voiceName, StringComparison.OrdinalIgnoreCase));

        if (matchedVoice != null)
        {
            return new VoiceValidationResult
            {
                IsValid = true,
                MatchedVoice = matchedVoice,
                AvailableVoices = availableVoices
            };
        }

        var fallback = GetFallbackVoice(provider, normalizedLanguage);
        return new VoiceValidationResult
        {
            IsValid = false,
            ErrorMessage = $"Voice '{voiceName}' not found for provider {provider} and language {language}",
            FallbackSuggestion = fallback,
            AvailableVoices = availableVoices
        };
    }

    /// <summary>
    /// Get all available voices for a provider and language
    /// </summary>
    public List<VoiceInfo> GetAvailableVoices(VoiceProvider provider, string language)
    {
        var normalizedLanguage = NormalizeLanguageCode(language);

        if (_voiceDatabase.TryGetValue(provider, out var providerVoices))
        {
            if (providerVoices.TryGetValue(normalizedLanguage, out var voices))
            {
                return new List<VoiceInfo>(voices);
            }
        }

        return new List<VoiceInfo>();
    }

    /// <summary>
    /// Get fallback voice (typically the highest quality voice for the language)
    /// </summary>
    public VoiceInfo? GetFallbackVoice(VoiceProvider provider, string language)
    {
        var voices = GetAvailableVoices(provider, language);
        return voices.FirstOrDefault();
    }

    /// <summary>
    /// Normalize language codes (e.g., "en-US" → "en", "en_GB" → "en")
    /// </summary>
    private static string NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return "en";
        }

        var normalized = languageCode.Replace('_', '-');
        var separatorIndex = normalized.IndexOf('-');
        return separatorIndex >= 0 ? normalized.Substring(0, separatorIndex) : normalized;
    }

    /// <summary>
    /// Initialize the voice database with available voices for each provider
    /// </summary>
    private Dictionary<VoiceProvider, Dictionary<string, List<VoiceInfo>>> InitializeVoiceDatabase()
    {
        var database = new Dictionary<VoiceProvider, Dictionary<string, List<VoiceInfo>>>();

        database[VoiceProvider.ElevenLabs] = InitializeElevenLabsVoices();
        database[VoiceProvider.PlayHT] = InitializePlayHTVoices();
        database[VoiceProvider.WindowsSAPI] = InitializeWindowsSAPIVoices();
        database[VoiceProvider.Piper] = InitializePiperVoices();
        database[VoiceProvider.Mimic3] = InitializeMimic3Voices();
        database[VoiceProvider.Azure] = InitializeAzureVoices();

        return database;
    }

    private Dictionary<string, List<VoiceInfo>> InitializeElevenLabsVoices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("Rachel", "rachel_voice_id", "Female", "Narration", "Premium"),
                new("Josh", "josh_voice_id", "Male", "Conversational", "Premium"),
                new("Emily", "emily_voice_id", "Female", "Professional", "Premium"),
                new("Adam", "adam_voice_id", "Male", "Deep", "Premium")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("Diego", "diego_voice_id", "Male", "Professional", "Premium"),
                new("Sofia", "sofia_voice_id", "Female", "Warm", "Premium"),
                new("Matias", "matias_voice_id", "Male", "Conversational", "Premium")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("Antoine", "antoine_voice_id", "Male", "Professional", "Premium"),
                new("Charlotte", "charlotte_voice_id", "Female", "Elegant", "Premium"),
                new("Thomas", "thomas_voice_id", "Male", "Friendly", "Premium")
            },
            ["de"] = new List<VoiceInfo>
            {
                new("Hans", "hans_voice_id", "Male", "Authoritative", "Premium"),
                new("Greta", "greta_voice_id", "Female", "Professional", "Premium"),
                new("Klaus", "klaus_voice_id", "Male", "Conversational", "Premium")
            },
            ["ja"] = new List<VoiceInfo>
            {
                new("Akira", "akira_voice_id", "Male", "Professional", "Premium"),
                new("Sakura", "sakura_voice_id", "Female", "Gentle", "Premium"),
                new("Takeshi", "takeshi_voice_id", "Male", "Dynamic", "Premium")
            },
            ["zh"] = new List<VoiceInfo>
            {
                new("Li Wei", "li_wei_voice_id", "Male", "Professional", "Premium"),
                new("Mei Lin", "mei_lin_voice_id", "Female", "Warm", "Premium"),
                new("Zhang Ming", "zhang_ming_voice_id", "Male", "Authoritative", "Premium")
            },
            ["ar"] = new List<VoiceInfo>
            {
                new("Ahmed", "ahmed_voice_id", "Male", "Professional", "Premium"),
                new("Fatima", "fatima_voice_id", "Female", "Warm", "Premium"),
                new("Omar", "omar_voice_id", "Male", "Authoritative", "Premium")
            },
            ["he"] = new List<VoiceInfo>
            {
                new("David", "david_voice_id", "Male", "Professional", "Premium"),
                new("Sarah", "sarah_voice_id", "Female", "Warm", "Premium")
            }
        };
    }

    private Dictionary<string, List<VoiceInfo>> InitializePlayHTVoices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("PlayHT English", "playht_en_id", "Neutral", "Adaptive", "Premium"),
                new("PlayHT Female", "playht_en_female_id", "Female", "Natural", "Premium"),
                new("PlayHT Male", "playht_en_male_id", "Male", "Natural", "Premium")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("PlayHT Spanish", "playht_es_id", "Neutral", "Adaptive", "Premium")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("PlayHT French", "playht_fr_id", "Neutral", "Adaptive", "Premium")
            },
            ["de"] = new List<VoiceInfo>
            {
                new("PlayHT German", "playht_de_id", "Neutral", "Adaptive", "Premium")
            }
        };
    }

    private Dictionary<string, List<VoiceInfo>> InitializeWindowsSAPIVoices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("Microsoft David", "ms_david", "Male", "Standard", "Free"),
                new("Microsoft Zira", "ms_zira", "Female", "Standard", "Free")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("Microsoft Helena", "ms_helena", "Female", "Standard", "Free")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("Microsoft Hortense", "ms_hortense", "Female", "Standard", "Free")
            },
            ["de"] = new List<VoiceInfo>
            {
                new("Microsoft Stefan", "ms_stefan", "Male", "Standard", "Free")
            }
        };
    }

    private Dictionary<string, List<VoiceInfo>> InitializePiperVoices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("Piper English", "piper_en_us", "Neutral", "Neural", "Free"),
                new("Piper Female", "piper_en_us_female", "Female", "Neural", "Free"),
                new("Piper Male", "piper_en_us_male", "Male", "Neural", "Free")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("Piper Spanish", "piper_es_es", "Neutral", "Neural", "Free")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("Piper French", "piper_fr_fr", "Neutral", "Neural", "Free")
            },
            ["de"] = new List<VoiceInfo>
            {
                new("Piper German", "piper_de_de", "Neutral", "Neural", "Free")
            }
        };
    }

    private Dictionary<string, List<VoiceInfo>> InitializeMimic3Voices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("Mimic English", "mimic3_en_us", "Neutral", "Neural", "Free"),
                new("Mimic Female", "mimic3_en_us_female", "Female", "Neural", "Free"),
                new("Mimic Male", "mimic3_en_us_male", "Male", "Neural", "Free")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("Mimic Spanish", "mimic3_es_es", "Neutral", "Neural", "Free")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("Mimic French", "mimic3_fr_fr", "Neutral", "Neural", "Free")
            }
        };
    }

    private Dictionary<string, List<VoiceInfo>> InitializeAzureVoices()
    {
        return new Dictionary<string, List<VoiceInfo>>
        {
            ["en"] = new List<VoiceInfo>
            {
                new("Azure Jenny", "en-US-JennyNeural", "Female", "Professional", "Premium"),
                new("Azure Guy", "en-US-GuyNeural", "Male", "Professional", "Premium"),
                new("Azure Aria", "en-US-AriaNeural", "Female", "Conversational", "Premium")
            },
            ["es"] = new List<VoiceInfo>
            {
                new("Azure Elvira", "es-ES-ElviraNeural", "Female", "Professional", "Premium"),
                new("Azure Alvaro", "es-ES-AlvaroNeural", "Male", "Professional", "Premium")
            },
            ["fr"] = new List<VoiceInfo>
            {
                new("Azure Denise", "fr-FR-DeniseNeural", "Female", "Professional", "Premium"),
                new("Azure Henri", "fr-FR-HenriNeural", "Male", "Professional", "Premium")
            },
            ["de"] = new List<VoiceInfo>
            {
                new("Azure Katja", "de-DE-KatjaNeural", "Female", "Professional", "Premium"),
                new("Azure Conrad", "de-DE-ConradNeural", "Male", "Professional", "Premium")
            }
        };
    }
}

/// <summary>
/// Voice information record
/// </summary>
public record VoiceInfo(
    string VoiceName,
    string VoiceId,
    string Gender,
    string Style,
    string Quality);

/// <summary>
/// Voice validation result
/// </summary>
public record VoiceValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public VoiceInfo? MatchedVoice { get; init; }
    public VoiceInfo? FallbackSuggestion { get; init; }
    public List<VoiceInfo> AvailableVoices { get; init; } = new();
}
