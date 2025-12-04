using System.Collections.Generic;
using Aura.Core.Models;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Complete voice assignment mapping characters to voices.
/// </summary>
public record VoiceAssignment(
    IReadOnlyDictionary<string, VoiceDescriptor> CharacterVoices,
    IReadOnlyList<VoicedLine> VoicedLines);

/// <summary>
/// A dialogue line with its assigned voice and synthesis specification.
/// </summary>
public record VoicedLine(
    DialogueLine Line,
    VoiceDescriptor AssignedVoice,
    VoiceSpec SynthesisSpec);

/// <summary>
/// Settings for automatic voice assignment.
/// </summary>
public record VoiceAssignmentSettings(
    VoiceDescriptor? NarratorVoice = null,
    IReadOnlyDictionary<string, VoiceDescriptor>? ExplicitAssignments = null,
    bool AutoAssignFromPool = true,
    IReadOnlyList<VoiceDescriptor>? VoicePool = null);

/// <summary>
/// Progress information for multi-voice synthesis.
/// </summary>
public record SynthesisProgress(
    double Percentage,
    string CurrentStep);
