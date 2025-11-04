using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;

namespace Aura.Providers.Tts.validators;

/// <summary>
/// SSML mapper for Mimic3 TTS provider (similar to Piper, limited SSML)
/// </summary>
public class Mimic3SSMLMapper : PiperSSMLMapper
{
    public override VoiceProvider Provider => VoiceProvider.Mimic3;

    public override ProviderSSMLConstraints GetConstraints()
    {
        return new ProviderSSMLConstraints
        {
            SupportedTags = new HashSet<string> { "speak", "break", "phoneme", "say-as" },
            SupportedProsodyAttributes = new HashSet<string>(),
            RateRange = (1.0, 1.0),
            PitchRange = (0.0, 0.0),
            VolumeRange = (1.0, 1.0),
            MaxPauseDurationMs = 5000,
            SupportsTimingMarkers = false,
            MaxTextLength = null
        };
    }
}
