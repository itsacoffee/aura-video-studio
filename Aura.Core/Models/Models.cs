using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

public record Brief(string Topic, string? Audience, string? Goal, string Tone, string Language, Aspect Aspect);

public record PlanSpec(TimeSpan TargetDuration, Pacing Pacing, Density Density, string Style);

public record VoiceSpec(string VoiceName, double Rate, double Pitch, PauseStyle Pause);

public record Scene(int Index, string Heading, string Script, TimeSpan Start, TimeSpan Duration);

public record ScriptLine(int SceneIndex, string Text, TimeSpan Start, TimeSpan Duration);

public record Asset(string Kind, string PathOrUrl, string? License, string? Attribution);

public record Resolution(int Width, int Height);

public record RenderSpec(Resolution Res, string Container, int VideoBitrateK, int AudioBitrateK);

public record RenderProgress(float Percentage, TimeSpan Elapsed, TimeSpan Remaining, string CurrentStage);

public record SystemProfile
{
    public bool AutoDetect { get; init; } = true;
    public int LogicalCores { get; init; }
    public int PhysicalCores { get; init; }
    public int RamGB { get; init; }
    public GpuInfo? Gpu { get; init; }
    public HardwareTier Tier { get; init; }
    public bool EnableNVENC { get; init; }
    public bool EnableSD { get; init; }
    public bool OfflineOnly { get; init; }
}

public record GpuInfo(string Vendor, string Model, int VramGB, string? Series);