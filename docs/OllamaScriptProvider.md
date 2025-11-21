# OllamaScriptProvider - Streaming Support

## Overview

`OllamaScriptProvider` extends `BaseLlmScriptProvider` to provide streaming script generation support using Ollama's local LLM service.

## Usage

### Basic Script Generation

```csharp
var logger = loggerFactory.CreateLogger<OllamaScriptProvider>();
var httpClient = new HttpClient();

var provider = new OllamaScriptProvider(
    logger,
    httpClient,
    baseUrl: "http://127.0.0.1:11434",
    model: "llama3.1:8b-q4_k_m",
    maxRetries: 3,
    timeoutSeconds: 120
);

var request = new ScriptGenerationRequest
{
    Brief = new Brief
    {
        Topic = "Introduction to Machine Learning",
        Audience = "Beginners",
        Goal = "Educate",
        Tone = "Friendly"
    },
    PlanSpec = new PlanSpec
    {
        TargetDuration = TimeSpan.FromSeconds(60),
        Style = "Educational",
        Pacing = "Medium"
    },
    CorrelationId = Guid.NewGuid().ToString()
};

var script = await provider.GenerateScriptAsync(request, cancellationToken);
```

### Streaming Script Generation

```csharp
await foreach (var progress in provider.StreamGenerateAsync(request, cancellationToken))
{
    Console.WriteLine($"[{progress.PercentComplete}%] {progress.Stage}: {progress.Message}");
    Console.WriteLine($"Partial script: {progress.PartialScript}");
    
    if (progress.PercentComplete == 100)
    {
        Console.WriteLine("Generation complete!");
        Console.WriteLine($"Final script: {progress.PartialScript}");
    }
}
```

### Progress Updates

The `StreamGenerateAsync` method yields `ScriptGenerationProgress` objects with:

- `Stage`: Current generation stage (e.g., "Generating")
- `PercentComplete`: 0-100 progress percentage
- `PartialScript`: Accumulated script content so far
- `Message`: Human-readable status message with token count

Progress is calculated based on:
- Token-by-token generation tracking
- Maximum expected tokens (2048)
- Final chunk completion signal from Ollama

## Configuration

### Prerequisites

1. **Install Ollama**: Download from [ollama.com](https://ollama.com)
2. **Start Ollama service**: Run `ollama serve` in terminal
3. **Pull a model**: Run `ollama pull llama3.1:8b-q4_k_m`

### Validation

Check if the provider is properly configured:

```csharp
var validationResult = await provider.ValidateConfigurationAsync(cancellationToken);

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

foreach (var warning in validationResult.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

### Check Availability

```csharp
var isAvailable = await provider.IsAvailableAsync(cancellationToken);
if (!isAvailable)
{
    Console.WriteLine("Ollama service is not running. Please start it with: ollama serve");
}
```

### List Available Models

```csharp
var models = await provider.GetAvailableModelsAsync(cancellationToken);
foreach (var model in models)
{
    Console.WriteLine($"Available model: {model}");
}
```

## Error Handling

The provider handles several error scenarios:

1. **Service Unavailable**: Throws `InvalidOperationException` if Ollama is not running
2. **Model Not Found**: Throws `InvalidOperationException` with instructions to pull the model
3. **Timeout**: Respects the configured timeout (default 120 seconds)
4. **Cancellation**: Properly handles `CancellationToken` for graceful shutdown

## Features

- ✅ **Streaming Support**: Real-time token-by-token generation
- ✅ **Local Execution**: No internet required, runs offline
- ✅ **Free**: No API costs or usage limits
- ✅ **Privacy**: All processing happens locally
- ✅ **Cancellable**: Full cancellation token support
- ✅ **Progress Tracking**: Real-time progress updates
- ✅ **Error Recovery**: Automatic retries with exponential backoff

## Provider Metadata

```csharp
var metadata = provider.GetProviderMetadata();

// Name: "Ollama"
// Tier: ProviderTier.Free
// RequiresInternet: false
// RequiresApiKey: false
// Capabilities: ["streaming", "local-execution", "offline"]
// EstimatedCostPer1KTokens: $0.00
```

## Integration with BaseLlmScriptProvider

This provider extends `BaseLlmScriptProvider`, which means it:

- Inherits retry logic with exponential backoff
- Provides consistent error handling
- Implements the `IScriptLlmProvider` interface
- Includes script parsing utilities
- Supports model selection and validation

## Differences from OllamaLlmProvider

`OllamaScriptProvider` is designed for script-centric workflows, while `OllamaLlmProvider` implements the broader `ILlmProvider` interface:

| Feature | OllamaScriptProvider | OllamaLlmProvider |
|---------|---------------------|-------------------|
| Base Class | BaseLlmScriptProvider | ILlmProvider |
| Primary Use | Script generation workflows | General LLM tasks |
| Streaming Output | ScriptGenerationProgress | OllamaStreamResponse |
| Script Parsing | ✅ Built-in | Manual |
| Retry Logic | ✅ Inherited | ✅ Custom |
| Scene Analysis | ❌ | ✅ |
| Visual Prompts | ❌ | ✅ |
| Tool Calling | ❌ | ✅ |

Choose `OllamaScriptProvider` when:
- You need structured script generation
- You want progress tracking during generation
- You're using `BaseLlmScriptProvider`-based workflows

Choose `OllamaLlmProvider` when:
- You need scene analysis or visual prompt generation
- You want tool calling support
- You need direct access to Ollama's streaming format

## Example: CLI Application

```csharp
var provider = new OllamaScriptProvider(
    logger,
    httpClient,
    baseUrl: "http://127.0.0.1:11434",
    model: "llama3.1:8b-q4_k_m"
);

Console.WriteLine("Generating script with streaming...");

await foreach (var progress in provider.StreamGenerateAsync(request, cts.Token))
{
    // Clear current line and show progress
    Console.Write($"\r[{progress.PercentComplete}%] {progress.Message}");
    
    if (progress.PercentComplete == 100)
    {
        Console.WriteLine("\n\nFinal Script:");
        Console.WriteLine("=" + new string('=', 79));
        Console.WriteLine(progress.PartialScript);
        Console.WriteLine("=" + new string('=', 79));
    }
}
```

## Testing

The provider includes comprehensive unit tests in `OllamaScriptProviderTests.cs`:

```bash
# Run all OllamaScriptProvider tests
dotnet test --filter "FullyQualifiedName~OllamaScriptProviderTests"

# Run streaming-specific tests
dotnet test --filter "FullyQualifiedName~OllamaScriptProviderTests.StreamGenerateAsync"
```

## Troubleshooting

### "Cannot connect to Ollama"

1. Ensure Ollama is installed and running: `ollama serve`
2. Check the base URL matches your Ollama instance (default: `http://127.0.0.1:11434`)
3. Verify firewall settings allow localhost connections

### "Model not found"

1. Pull the model: `ollama pull llama3.1:8b-q4_k_m`
2. List available models: `ollama list`
3. Use a model that's already pulled

### Slow Generation

1. Check your hardware (Ollama performs better with GPU)
2. Use a smaller model (e.g., `llama3.1:7b` instead of `13b`)
3. Increase timeout if model is loading: `timeoutSeconds: 300`

## See Also

- [Ollama Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
- [BaseLlmScriptProvider](../BaseLlmScriptProvider.cs)
- [IScriptLlmProvider Interface](../../Aura.Core/Interfaces/IScriptLlmProvider.cs)
