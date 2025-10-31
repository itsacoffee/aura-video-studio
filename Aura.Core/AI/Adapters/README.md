# LLM Provider Adapters

## Overview

This directory contains provider-specific adapters that optimize prompts, parameters, and error handling for each LLM provider's characteristics. The adapters ensure maximum quality and reliability across OpenAI, Anthropic, Gemini, Azure OpenAI, and Ollama.

## Architecture

### Base Adapter (`LlmProviderAdapter`)

The abstract base class defines the contract for all provider adapters:
- **Prompt Optimization**: Adapts prompts to provider-specific formats and best practices
- **Parameter Calculation**: Determines optimal temperature, tokens, and other parameters per operation type
- **Token Management**: Truncates prompts that exceed token limits
- **Response Validation**: Validates responses before they propagate through the pipeline
- **Error Handling**: Provider-specific error recovery strategies with retry logic

### Provider Capabilities (`ProviderCapabilities`)

Each adapter exposes its capabilities:
- Max token limits
- Context window size
- Feature support (JSON mode, streaming, function calling)
- Typical latency characteristics

### Operation Types (`LlmOperationType`)

Adapters optimize parameters based on the operation:
- **Creative**: Script generation, storytelling (higher temperature)
- **Analytical**: Scene analysis, complexity assessment (lower temperature)
- **Extraction**: Structured data extraction (very low temperature)
- **ShortForm**: Transitions, prompts (moderate temperature, limited tokens)
- **LongForm**: Extended content generation (higher temperature, more tokens)

## Model Registry (`ModelRegistry`)

### Purpose

The `ModelRegistry` solves the problem of hardcoded model names that become outdated as providers release new models. Instead of hardcoding model capabilities, we:

1. **Maintain a central registry** of known models with their capabilities
2. **Support aliases** for models (e.g., "claude-3-5-sonnet" → "claude-3-5-sonnet-20241022")
3. **Pattern-based detection** for local models (Ollama) where model naming is flexible
4. **Fallback estimation** when a model isn't in the registry yet

### How It Works

```csharp
// Example: Finding a model
var modelInfo = ModelRegistry.FindModel("OpenAI", "gpt-4o-mini");
// Returns: ModelInfo with MaxTokens=128000, ContextWindow=128000

// Example: Using default model for a provider
var defaultModel = ModelRegistry.GetDefaultModel("Anthropic");
// Returns: "claude-3-5-sonnet-20241022"

// Example: Estimating unknown model capabilities
var (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities("gpt-5-turbo");
// Returns: Estimated values based on model name patterns
```

### Updating the Registry

When providers release new models:

1. Open `ModelRegistry.cs`
2. Add new `ModelInfo` entries to the `_models` list
3. Update default models in `GetDefaultModel()` if needed
4. Add deprecation info to old models if they're being retired

Example:
```csharp
new ModelInfo 
{ 
    Provider = "OpenAI", 
    ModelId = "gpt-5",  // New model
    MaxTokens = 256000,
    ContextWindow = 256000,
    Aliases = new[] { "gpt-5-latest" }
}
```

### Pattern Detection (Ollama)

For Ollama and other local models, we use pattern-based detection:

```csharp
// Model name: "llama3.2-vision:latest"
// Pattern detected: "llama"
// Capabilities: Determined by version detection in pattern
```

This allows flexibility for local models without requiring every variant to be registered.

## Provider-Specific Optimizations

### OpenAI (`OpenAiAdapter`)
- **Temperature**: 0.3 (analytical) to 0.7 (creative)
- **Top-p**: 0.7 to 0.9
- **Penalties**: Frequency (0.3) and presence (0.2) for creative work
- **Features**: System message priority, JSON mode, function calling
- **Error Handling**: Rate limit (429) → exponential backoff, 3 retries

### Anthropic (`AnthropicAdapter`)
- **Temperature**: 0.5 to 0.8 (Claude prefers slightly higher)
- **Constitutional AI**: Adds ethical principles to system prompts
- **Stop Sequences**: `\n\nHuman:`, `\n\nAssistant:` for cleaner outputs
- **Error Handling**: Overload (529) → longer backoff, 4 retries for rate limits

### Gemini (`GeminiAdapter`)
- **Temperature**: 0.4 to 0.9 (highest for creative work)
- **Top-K**: 10 to 40 for controlled sampling
- **Safety Filters**: Detects blocked responses and modifies prompts
- **Error Handling**: Safety blocks → prompt modification and retry

### Azure OpenAI (`AzureOpenAiAdapter`)
- **Regional Failover**: Supports multiple region endpoints
- **Deployment-Specific**: Uses deployment names instead of model names
- **Rate Limits**: Different limits than standard OpenAI
- **Error Handling**: 404 → deployment not found (permanent failure)

### Ollama (`OllamaAdapter`)
- **Local Optimization**: Model-specific temperature tuning
- **Context Awareness**: Smaller context windows for local models
- **Keep-Alive**: Sets keep_alive params for model persistence
- **Error Handling**: Connection refused → check service status (fallback immediately)

## Performance

All adapters are designed to have **< 5ms overhead**. The `ValidatePerformance()` method logs warnings if overhead exceeds this threshold.

## Error Recovery Strategies

Each adapter returns an `ErrorRecoveryStrategy` that includes:
- **ShouldRetry**: Whether to retry the request
- **RetryDelay**: How long to wait before retrying
- **ShouldFallback**: Whether to try a different provider
- **ModifiedPrompt**: Optional modified prompt for retry (e.g., safety filters)
- **UserMessage**: User-friendly error message
- **IsPermanentFailure**: Whether this error can't be recovered (e.g., invalid API key)

## Usage Example

```csharp
// Create adapter
var adapter = new OpenAiAdapter(logger, "gpt-4o-mini");

// Get capabilities
var capabilities = adapter.Capabilities;
Console.WriteLine($"Max tokens: {capabilities.MaxTokenLimit}");

// Optimize prompts
var systemPrompt = adapter.OptimizeSystemPrompt("You are a video creator");
var userPrompt = adapter.OptimizeUserPrompt("Create a script about AI", LlmOperationType.Creative);

// Calculate parameters
var parameters = adapter.CalculateParameters(LlmOperationType.Creative, estimatedInputTokens: 500);
Console.WriteLine($"Temperature: {parameters.Temperature}");
Console.WriteLine($"Max tokens: {parameters.MaxTokens}");

// Handle errors
try
{
    // API call
}
catch (Exception ex)
{
    var strategy = adapter.HandleError(ex, attemptNumber: 1);
    if (strategy.ShouldRetry)
    {
        await Task.Delay(strategy.RetryDelay ?? TimeSpan.Zero);
        // Retry
    }
    else if (strategy.ShouldFallback)
    {
        // Try different provider
    }
}
```

## Future Enhancements

- **Model Availability Checking**: Add HTTP checks to verify models exist before use
- **Dynamic Registry Updates**: Fetch model lists from provider APIs
- **Performance Metrics**: Track adapter overhead and optimization effectiveness
- **Custom Adapters**: Allow users to create custom adapters for new providers
