using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Hardware and performance-related settings
/// </summary>
public class HardwarePerformanceSettings
{
    /// <summary>
    /// Enable hardware acceleration for encoding
    /// </summary>
    public bool HardwareAccelerationEnabled { get; set; } = true;

    /// <summary>
    /// Preferred hardware encoder (auto, nvenc, amf, qsv, software)
    /// </summary>
    public string PreferredEncoder { get; set; } = "auto";

    /// <summary>
    /// Selected GPU device ID for multi-GPU systems
    /// </summary>
    public string SelectedGpuId { get; set; } = "auto";

    /// <summary>
    /// RAM allocation for rendering in MB (0 = auto)
    /// </summary>
    public int RamAllocationMB { get; set; } = 0;

    /// <summary>
    /// Maximum number of concurrent rendering threads (0 = auto)
    /// </summary>
    public int MaxRenderingThreads { get; set; } = 0;

    /// <summary>
    /// Preview quality setting (low, medium, high, ultra)
    /// </summary>
    public string PreviewQuality { get; set; } = "medium";

    /// <summary>
    /// Enable background rendering
    /// </summary>
    public bool BackgroundRenderingEnabled { get; set; } = false;

    /// <summary>
    /// Maximum cache size in MB (0 = unlimited)
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 5000;

    /// <summary>
    /// Enable GPU memory monitoring
    /// </summary>
    public bool EnableGpuMemoryMonitoring { get; set; } = true;

    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}

/// <summary>
/// Provider configuration settings
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// OpenAI provider settings
    /// </summary>
    public OpenAIProviderSettings OpenAI { get; set; } = new();

    /// <summary>
    /// Ollama provider settings
    /// </summary>
    public OllamaProviderSettings Ollama { get; set; } = new();

    /// <summary>
    /// Anthropic provider settings
    /// </summary>
    public AnthropicProviderSettings Anthropic { get; set; } = new();

    /// <summary>
    /// Azure OpenAI provider settings
    /// </summary>
    public AzureOpenAIProviderSettings AzureOpenAI { get; set; } = new();

    /// <summary>
    /// Google Gemini provider settings
    /// </summary>
    public GeminiProviderSettings Gemini { get; set; } = new();

    /// <summary>
    /// ElevenLabs provider settings
    /// </summary>
    public ElevenLabsProviderSettings ElevenLabs { get; set; } = new();

    /// <summary>
    /// Stable Diffusion provider settings
    /// </summary>
    public StableDiffusionProviderSettings StableDiffusion { get; set; } = new();

    /// <summary>
    /// Provider priority order for fallback
    /// </summary>
    public List<string> ProviderPriorityOrder { get; set; } = new();
}

/// <summary>
/// OpenAI provider settings
/// </summary>
public class OpenAIProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string OrganizationId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Ollama provider settings
/// </summary>
public class OllamaProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "llama3.1:8b-q4_k_m";
    public string ExecutablePath { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
    public bool AutoStart { get; set; } = false;
}

/// <summary>
/// Anthropic provider settings
/// </summary>
public class AnthropicProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Azure OpenAI provider settings
/// </summary>
public class AzureOpenAIProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-01";
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Google Gemini provider settings
/// </summary>
public class GeminiProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-pro";
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// ElevenLabs provider settings
/// </summary>
public class ElevenLabsProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultVoiceId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Stable Diffusion provider settings
/// </summary>
public class StableDiffusionProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "http://127.0.0.1:7860";
    public int TimeoutSeconds { get; set; } = 120;
    public bool AutoStart { get; set; } = false;
}
