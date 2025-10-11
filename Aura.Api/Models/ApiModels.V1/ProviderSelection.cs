namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Per-stage provider selection for dynamic provider routing
/// </summary>
public record ProviderSelectionDto
{
    /// <summary>
    /// Script/LLM provider: RuleBased | Ollama | OpenAI | AzureOpenAI | Gemini
    /// </summary>
    public string? Script { get; init; }

    /// <summary>
    /// TTS provider: Windows | ElevenLabs | PlayHT
    /// </summary>
    public string? Tts { get; init; }

    /// <summary>
    /// Visual provider: Stock | LocalSD | StableDiffusion | CloudPro | Stability | Runway
    /// </summary>
    public string? Visuals { get; init; }

    /// <summary>
    /// Upload provider: Off | YouTube
    /// </summary>
    public string? Upload { get; init; }
}
