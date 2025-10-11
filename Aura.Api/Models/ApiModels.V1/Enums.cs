namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// V1 API enum definitions - pinned and stable
/// </summary>

public enum Pacing
{
    Chill,
    Conversational,
    Fast
}

public enum Density
{
    Sparse,
    Balanced,
    Dense
}

public enum Aspect
{
    Widescreen16x9,
    Vertical9x16,
    Square1x1
}

public enum PauseStyle
{
    Natural,
    Short,
    Long,
    Dramatic
}

public enum ProviderMode
{
    Free,
    Pro
}

public enum HardwareTier
{
    A, // High (≥12GB VRAM or NVIDIA 40/50-series)
    B, // Upper-mid (8-8GB VRAM)
    C, // Mid (6-8GB VRAM)
    D  // Entry (≤4-6GB VRAM or no GPU)
}

/// <summary>
/// Helper methods to convert between API V1 enums and internal Core.Models enums
/// </summary>
public static class EnumMappings
{
    public static Aura.Core.Models.Aspect ToCore(this Aspect aspect) => aspect switch
    {
        Aspect.Widescreen16x9 => Aura.Core.Models.Aspect.Widescreen16x9,
        Aspect.Vertical9x16 => Aura.Core.Models.Aspect.Vertical9x16,
        Aspect.Square1x1 => Aura.Core.Models.Aspect.Square1x1,
        _ => throw new ArgumentOutOfRangeException(nameof(aspect))
    };

    public static Aura.Core.Models.Density ToCore(this Density density) => density switch
    {
        Density.Sparse => Aura.Core.Models.Density.Sparse,
        Density.Balanced => Aura.Core.Models.Density.Balanced,
        Density.Dense => Aura.Core.Models.Density.Dense,
        _ => throw new ArgumentOutOfRangeException(nameof(density))
    };

    public static Aura.Core.Models.Pacing ToCore(this Pacing pacing) => pacing switch
    {
        Pacing.Chill => Aura.Core.Models.Pacing.Chill,
        Pacing.Conversational => Aura.Core.Models.Pacing.Conversational,
        Pacing.Fast => Aura.Core.Models.Pacing.Fast,
        _ => throw new ArgumentOutOfRangeException(nameof(pacing))
    };

    public static Aura.Core.Models.PauseStyle ToCore(this PauseStyle pauseStyle) => pauseStyle switch
    {
        PauseStyle.Natural => Aura.Core.Models.PauseStyle.Natural,
        PauseStyle.Short => Aura.Core.Models.PauseStyle.Short,
        PauseStyle.Long => Aura.Core.Models.PauseStyle.Long,
        PauseStyle.Dramatic => Aura.Core.Models.PauseStyle.Dramatic,
        _ => throw new ArgumentOutOfRangeException(nameof(pauseStyle))
    };
}

