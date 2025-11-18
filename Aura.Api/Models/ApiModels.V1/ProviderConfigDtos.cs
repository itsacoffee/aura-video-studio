namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// DTOs for unified provider configuration endpoints
/// These are separate from ProviderConfigurationDtos which handle provider profiles
/// </summary>

/// <summary>
/// Full provider configuration response
/// </summary>
public record ProviderConfigurationDto
{
    public OpenAiConfigDto OpenAi { get; set; } = new();
    public OllamaConfigDto Ollama { get; set; } = new();
    public StableDiffusionConfigDto StableDiffusion { get; set; } = new();
    public AnthropicConfigDto Anthropic { get; set; } = new();
    public GeminiConfigDto Gemini { get; set; } = new();
    public ElevenLabsConfigDto ElevenLabs { get; set; } = new();
}

/// <summary>
/// OpenAI configuration
/// </summary>
public record OpenAiConfigDto
{
    /// <summary>
    /// API key (null in GET responses for security)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// OpenAI API endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }
}

/// <summary>
/// Ollama configuration
/// </summary>
public record OllamaConfigDto
{
    /// <summary>
    /// Ollama base URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Currently selected model
    /// </summary>
    public string? Model { get; set; }
}

/// <summary>
/// Stable Diffusion WebUI configuration
/// </summary>
public record StableDiffusionConfigDto
{
    /// <summary>
    /// Stable Diffusion WebUI URL
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Anthropic configuration
/// </summary>
public record AnthropicConfigDto
{
    /// <summary>
    /// API key (null in GET responses for security)
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Google Gemini configuration
/// </summary>
public record GeminiConfigDto
{
    /// <summary>
    /// API key (null in GET responses for security)
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// ElevenLabs configuration
/// </summary>
public record ElevenLabsConfigDto
{
    /// <summary>
    /// API key (null in GET responses for security)
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Request to update provider configuration (non-secret fields)
/// </summary>
public record ProviderConfigurationUpdateDto
{
    public OpenAiConfigDto? OpenAi { get; set; }
    public OllamaConfigDto? Ollama { get; set; }
    public StableDiffusionConfigDto? StableDiffusion { get; set; }
}

/// <summary>
/// Request to update provider secrets (API keys)
/// </summary>
public record ProviderSecretsUpdateDto
{
    public string? OpenAiApiKey { get; set; }
    public string? AnthropicApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string? ElevenLabsApiKey { get; set; }
}
