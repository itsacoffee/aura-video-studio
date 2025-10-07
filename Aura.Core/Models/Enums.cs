namespace Aura.Core.Models;

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
    B, // Upper-mid (8-12GB VRAM)
    C, // Mid (6-8GB VRAM)
    D  // Entry (≤4-6GB VRAM or no GPU)
}