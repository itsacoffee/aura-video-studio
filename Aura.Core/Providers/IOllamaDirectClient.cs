using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Providers;

/// <summary>
/// Direct client for Ollama API without going through the LLM provider abstraction.
/// This interface provides a clean, dependency-injection-friendly way to call Ollama
/// without using reflection to access private fields of OllamaLlmProvider.
/// 
/// ARCHITECTURAL DECISION: Use this interface instead of reflection-based access.
/// Reflection is fragile and breaks when provider implementation changes.
/// </summary>
public interface IOllamaDirectClient
{
    /// <summary>
    /// Generate text using Ollama with specific options.
    /// </summary>
    /// <param name="model">The model to use (e.g., "llama3.1:8b-q4_k_m")</param>
    /// <param name="prompt">The prompt to send to the model</param>
    /// <param name="systemPrompt">Optional system prompt for context</param>
    /// <param name="options">Ollama-specific generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text response</returns>
    Task<string> GenerateAsync(
        string model, 
        string prompt, 
        string? systemPrompt = null,
        OllamaGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if Ollama is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if Ollama is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// List available models on the Ollama instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available model names</returns>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for Ollama generation requests.
/// Provides fine-grained control over the generation process.
/// </summary>
public class OllamaGenerationOptions
{
    /// <summary>Temperature for randomness (0.0 = deterministic, higher = more random)</summary>
    public double? Temperature { get; set; }
    
    /// <summary>Top-p sampling threshold</summary>
    public double? TopP { get; set; }
    
    /// <summary>Top-k sampling parameter</summary>
    public int? TopK { get; set; }
    
    /// <summary>Maximum tokens to generate</summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>Number of times to repeat the prompt for better results</summary>
    public int? RepeatPenalty { get; set; }
    
    /// <summary>Stop sequences to end generation</summary>
    public List<string>? Stop { get; set; }

    /// <summary>Number of GPUs to use (-1 = all available, 0 = CPU only)</summary>
    public int? NumGpu { get; set; }

    /// <summary>Context window size</summary>
    public int? NumCtx { get; set; }
}
