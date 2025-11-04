using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;
using Aura.Providers.Tts.validators;
using Xunit;

namespace Aura.Tests;

public class SSMLMapperTests
{
    [Theory]
    [InlineData(typeof(ElevenLabsSSMLMapper), VoiceProvider.ElevenLabs)]
    [InlineData(typeof(WindowsSSMLMapper), VoiceProvider.WindowsSAPI)]
    [InlineData(typeof(PlayHTSSMLMapper), VoiceProvider.PlayHT)]
    [InlineData(typeof(PiperSSMLMapper), VoiceProvider.Piper)]
    [InlineData(typeof(Mimic3SSMLMapper), VoiceProvider.Mimic3)]
    public void Mapper_Provider_ReturnsCorrectProvider(Type mapperType, VoiceProvider expectedProvider)
    {
        var mapper = (ISSMLMapper)Activator.CreateInstance(mapperType)!;
        Assert.Equal(expectedProvider, mapper.Provider);
    }

    [Fact]
    public void ElevenLabsMapper_GetConstraints_ReturnsValidConstraints()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var constraints = mapper.GetConstraints();

        Assert.NotEmpty(constraints.SupportedTags);
        Assert.Contains("speak", constraints.SupportedTags);
        Assert.Contains("prosody", constraints.SupportedTags);
        Assert.Contains("break", constraints.SupportedTags);
        Assert.True(constraints.MaxTextLength > 0);
    }

    [Fact]
    public void WindowsMapper_GetConstraints_SupportsTimingMarkers()
    {
        var mapper = new WindowsSSMLMapper();
        var constraints = mapper.GetConstraints();

        Assert.True(constraints.SupportsTimingMarkers);
        Assert.Contains("speak", constraints.SupportedTags);
        Assert.Contains("phoneme", constraints.SupportedTags);
    }

    [Fact]
    public void PiperMapper_GetConstraints_HasLimitedSupport()
    {
        var mapper = new PiperSSMLMapper();
        var constraints = mapper.GetConstraints();

        Assert.DoesNotContain("prosody", constraints.SupportedTags);
        Assert.DoesNotContain("emphasis", constraints.SupportedTags);
        Assert.Equal(1.0, constraints.RateRange.Min);
        Assert.Equal(1.0, constraints.RateRange.Max);
    }

    [Fact]
    public void ElevenLabsMapper_MapToSSML_BasicText_GeneratesValidSSML()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var adjustments = new ProsodyAdjustments
        {
            Rate = 1.0,
            Pitch = 0.0,
            Volume = 1.0
        };
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Hello world", adjustments, voiceSpec);

        Assert.StartsWith("<speak>", ssml);
        Assert.EndsWith("</speak>", ssml);
        Assert.Contains("Hello world", ssml);
    }

    [Fact]
    public void ElevenLabsMapper_MapToSSML_WithRateAdjustment_IncludesProsody()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var adjustments = new ProsodyAdjustments
        {
            Rate = 1.2,
            Pitch = 0.0,
            Volume = 1.0
        };
        var voiceSpec = new VoiceSpec("Test", 1.2, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Faster speech", adjustments, voiceSpec);

        Assert.Contains("<prosody", ssml);
        Assert.Contains("rate=", ssml);
        Assert.Contains("</prosody>", ssml);
    }

    [Fact]
    public void ElevenLabsMapper_MapToSSML_WithPauses_InsertsPausesTags()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var pauses = new Dictionary<int, int> { { 5, 500 } };
        var adjustments = new ProsodyAdjustments
        {
            Rate = 1.0,
            Pauses = pauses
        };
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Hello world", adjustments, voiceSpec);

        Assert.Contains("<break", ssml);
        Assert.Contains("500ms", ssml);
    }

    [Fact]
    public void ElevenLabsMapper_MapToSSML_WithEmphasis_AddsEmphasisTags()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var emphasis = new List<EmphasisSpan>
        {
            new EmphasisSpan(0, 5, Aura.Core.Models.Voice.EmphasisLevel.Strong)
        };
        var adjustments = new ProsodyAdjustments
        {
            Rate = 1.0,
            Emphasis = emphasis
        };
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Hello world", adjustments, voiceSpec);

        Assert.Contains("<emphasis", ssml);
        Assert.Contains("strong", ssml);
        Assert.Contains("</emphasis>", ssml);
    }

    [Fact]
    public void WindowsMapper_MapToSSML_BasicText_IncludesNamespace()
    {
        var mapper = new WindowsSSMLMapper();
        var adjustments = new ProsodyAdjustments();
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Hello world", adjustments, voiceSpec);

        Assert.Contains("xmlns=", ssml);
        Assert.Contains("http://www.w3.org/2001/10/synthesis", ssml);
    }

    [Fact]
    public void WindowsMapper_MapToSSML_SlowRate_UsesDescriptiveValue()
    {
        var mapper = new WindowsSSMLMapper();
        var adjustments = new ProsodyAdjustments { Rate = 0.6 };
        var voiceSpec = new VoiceSpec("Test", 0.6, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Slow speech", adjustments, voiceSpec);

        Assert.Contains("<prosody", ssml);
        Assert.Contains("slow", ssml);
    }

    [Fact]
    public void PiperMapper_MapToSSML_WithProsody_OmitsProsodyTag()
    {
        var mapper = new PiperSSMLMapper();
        var adjustments = new ProsodyAdjustments { Rate = 1.5 };
        var voiceSpec = new VoiceSpec("Test", 1.5, 0.0, PauseStyle.Natural);

        var ssml = mapper.MapToSSML("Test text", adjustments, voiceSpec);

        Assert.DoesNotContain("<prosody", ssml);
    }

    [Fact]
    public void ElevenLabsMapper_Validate_ValidSSML_ReturnsValid()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak>Hello world</speak>";

        var result = mapper.Validate(ssml);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ElevenLabsMapper_Validate_MissingSpeakTag_ReturnsInvalid()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "Hello world";

        var result = mapper.Validate(ssml);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ElevenLabsMapper_Validate_UnsupportedTag_ReturnsError()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak><unsupported>Hello</unsupported></speak>";

        var result = mapper.Validate(ssml);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unsupported tags"));
    }

    [Fact]
    public void ElevenLabsMapper_AutoRepair_MissingSpeakTag_AddsSpeakTag()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "Hello world";

        var repaired = mapper.AutoRepair(ssml);

        Assert.StartsWith("<speak>", repaired);
        Assert.EndsWith("</speak>", repaired);
    }

    [Fact]
    public void ElevenLabsMapper_AutoRepair_UnsupportedTags_RemovesTags()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak><unsupported>Hello</unsupported> world</speak>";

        var repaired = mapper.AutoRepair(ssml);

        Assert.DoesNotContain("<unsupported>", repaired);
        Assert.DoesNotContain("</unsupported>", repaired);
    }

    [Fact]
    public void ElevenLabsMapper_AutoRepair_ExcessivePause_ClampsPauseDuration()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var constraints = mapper.GetConstraints();
        var ssml = $"<speak>Hello<break time=\"{constraints.MaxPauseDurationMs + 1000}ms\"/> world</speak>";

        var repaired = mapper.AutoRepair(ssml);

        Assert.Contains($"{constraints.MaxPauseDurationMs}ms", repaired);
    }

    [Fact]
    public async Task ElevenLabsMapper_EstimateDuration_ShortText_ReturnsReasonableEstimate()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak>Hello world</speak>";
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var durationMs = await mapper.EstimateDurationAsync(ssml, voiceSpec, CancellationToken.None);

        Assert.True(durationMs > 0);
        Assert.True(durationMs < 5000);
    }

    [Fact]
    public async Task ElevenLabsMapper_EstimateDuration_WithPauses_IncludesPauseDuration()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak>Hello<break time=\"1000ms\"/> world</speak>";
        var voiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);

        var durationMs = await mapper.EstimateDurationAsync(ssml, voiceSpec, CancellationToken.None);

        Assert.True(durationMs >= 1000);
    }

    [Fact]
    public async Task ElevenLabsMapper_EstimateDuration_FasterRate_ReducesDuration()
    {
        var mapper = new ElevenLabsSSMLMapper();
        var ssml = "<speak>This is a longer sentence with more words to test rate adjustment.</speak>";
        
        var normalVoiceSpec = new VoiceSpec("Test", 1.0, 0.0, PauseStyle.Natural);
        var fastVoiceSpec = new VoiceSpec("Test", 1.5, 0.0, PauseStyle.Natural);

        var normalDuration = await mapper.EstimateDurationAsync(ssml, normalVoiceSpec, CancellationToken.None);
        var fastDuration = await mapper.EstimateDurationAsync(ssml, fastVoiceSpec, CancellationToken.None);

        Assert.True(fastDuration < normalDuration);
    }

    [Fact]
    public void PiperMapper_AutoRepair_RemovesProsodyTags()
    {
        var mapper = new PiperSSMLMapper();
        var ssml = "<speak><prosody rate=\"fast\">Hello</prosody> world</speak>";

        var repaired = mapper.AutoRepair(ssml);

        Assert.DoesNotContain("<prosody", repaired);
        Assert.DoesNotContain("</prosody>", repaired);
        Assert.Contains("Hello", repaired);
    }

    [Fact]
    public void WindowsMapper_AutoRepair_AddsProperNamespace()
    {
        var mapper = new WindowsSSMLMapper();
        var ssml = "Hello world";

        var repaired = mapper.AutoRepair(ssml);

        Assert.Contains("xmlns=", repaired);
        Assert.Contains("http://www.w3.org/2001/10/synthesis", repaired);
    }
}
