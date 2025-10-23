namespace Aura.Core.Models;

/// <summary>
/// Defines the pacing or tempo of video content.
/// </summary>
public enum Pacing
{
    /// <summary>
    /// Relaxed, slow-paced content with longer scene durations.
    /// </summary>
    Chill,
    
    /// <summary>
    /// Natural, conversational pacing suitable for most content.
    /// </summary>
    Conversational,
    
    /// <summary>
    /// Quick, dynamic pacing with rapid scene transitions.
    /// </summary>
    Fast
}

/// <summary>
/// Defines the visual density or information complexity of scenes.
/// </summary>
public enum Density
{
    /// <summary>
    /// Minimal visual elements, simple compositions.
    /// </summary>
    Sparse,
    
    /// <summary>
    /// Moderate visual complexity, balanced composition.
    /// </summary>
    Balanced,
    
    /// <summary>
    /// Rich visual detail, complex compositions.
    /// </summary>
    Dense
}

/// <summary>
/// Defines the aspect ratio of the video output.
/// </summary>
public enum Aspect
{
    /// <summary>
    /// Widescreen 16:9 aspect ratio (standard for YouTube, TV).
    /// </summary>
    Widescreen16x9,
    
    /// <summary>
    /// Vertical 9:16 aspect ratio (for mobile, TikTok, Instagram Reels).
    /// </summary>
    Vertical9x16,
    
    /// <summary>
    /// Square 1:1 aspect ratio (for Instagram posts).
    /// </summary>
    Square1x1
}

/// <summary>
/// Defines the style of pauses in narration.
/// </summary>
public enum PauseStyle
{
    /// <summary>
    /// Natural, speech-like pauses.
    /// </summary>
    Natural,
    
    /// <summary>
    /// Brief pauses for quick pacing.
    /// </summary>
    Short,
    
    /// <summary>
    /// Extended pauses for emphasis.
    /// </summary>
    Long,
    
    /// <summary>
    /// Dramatic, theatrical pauses.
    /// </summary>
    Dramatic
}

/// <summary>
/// Defines the provider pricing tier.
/// </summary>
public enum ProviderMode
{
    /// <summary>
    /// Free providers that don't require API keys or payment.
    /// </summary>
    Free,
    
    /// <summary>
    /// Premium providers that require API keys and may incur costs.
    /// </summary>
    Pro
}

/// <summary>
/// Defines hardware capability tiers based on GPU specifications.
/// </summary>
public enum HardwareTier
{
    /// <summary>
    /// High-end tier: 12GB+ VRAM or NVIDIA 40/50-series GPUs.
    /// </summary>
    A,
    
    /// <summary>
    /// Upper-mid tier: 8-12GB VRAM GPUs.
    /// </summary>
    B,
    
    /// <summary>
    /// Mid tier: 6-8GB VRAM GPUs.
    /// </summary>
    C,
    
    /// <summary>
    /// Entry tier: 4-6GB VRAM or no dedicated GPU.
    /// </summary>
    D
}