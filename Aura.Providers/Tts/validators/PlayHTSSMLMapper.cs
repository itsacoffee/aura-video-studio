using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;

namespace Aura.Providers.Tts.validators;

/// <summary>
/// SSML mapper for PlayHT TTS provider (similar to ElevenLabs)
/// </summary>
public class PlayHTSSMLMapper : ElevenLabsSSMLMapper
{
    public new VoiceProvider Provider => VoiceProvider.PlayHT;

    public new ProviderSSMLConstraints GetConstraints()
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
}
