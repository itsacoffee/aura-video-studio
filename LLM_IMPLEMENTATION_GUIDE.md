# LLM Provider Implementation Completion Guide

## Overview

This guide provides step-by-step instructions to complete the remaining LLM provider implementations for **Gemini** and **Azure OpenAI** providers to achieve full feature parity with OpenAI and Anthropic providers.

## Current Status

### ✅ Completed Providers (7/7 methods)
1. **OpenAI** - Reference implementation, fully complete
2. **Anthropic** - Newly implemented, fully complete
3. **Ollama** - Newly completed, fully functional
4. **RuleBased** - Fallback provider, fully complete

### ⚠️ Incomplete Providers (2/7 methods)
5. **Gemini** - Missing 5 methods (71% incomplete)
6. **Azure OpenAI** - Missing 5 methods (71% incomplete)

## Missing Methods by Provider

### Gemini LLM Provider

**File**: `Aura.Providers/Llm/GeminiLlmProvider.cs`

**Status**: 2/7 methods implemented

**Implemented** ✅:
- `DraftScriptAsync`
- `AnalyzeSceneImportanceAsync`

**Missing** ❌:
- `GenerateVisualPromptAsync`
- `AnalyzeContentComplexityAsync`
- `AnalyzeSceneCoherenceAsync`
- `ValidateNarrativeArcAsync`
- `GenerateTransitionTextAsync`

### Azure OpenAI LLM Provider

**File**: `Aura.Providers/Llm/AzureOpenAiLlmProvider.cs`

**Status**: 2/7 methods implemented

**Implemented** ✅:
- `DraftScriptAsync`
- `AnalyzeSceneImportanceAsync`

**Missing** ❌:
- `GenerateVisualPromptAsync`
- `AnalyzeContentComplexityAsync`
- `AnalyzeSceneCoherenceAsync`
- `ValidateNarrativeArcAsync`
- `GenerateTransitionTextAsync`

## Implementation Instructions

### Step 1: Complete Gemini Provider

#### Method 1: GenerateVisualPromptAsync

**Copy from**: OpenAI or Anthropic provider (lines with visual prompt implementation)

**Changes needed**:
1. Replace `messages` structure with Gemini's `contents` format
2. Replace `message.content` extraction with `content[0].parts[0].text`
3. Add markdown cleanup (Gemini sometimes returns ```json...``` blocks)
4. Use these parameters:
   ```csharp
   temperature = 0.7,
   maxOutputTokens = 2048,
   topP = 0.9
   ```

**Template**:
```csharp
public async Task<VisualPromptResult?> GenerateVisualPromptAsync(
    string sceneText,
    string? previousSceneText,
    string videoTone,
    VisualStyle targetStyle,
    CancellationToken ct)
{
    _logger.LogInformation("Generating visual prompt with Gemini");

    try
    {
        var systemPrompt = "You are a professional cinematographer...";
        var userPrompt = $@"Create a detailed visual prompt...";
        var prompt = $"{systemPrompt}\n\n{userPrompt}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048,
                topP = 0.9
            }
        };

        // ... rest of HTTP call and response parsing
        // Clean markdown: analysisText.Replace("```json", "").Replace("```", "").Trim()
    }
}
```

#### Method 2-5: Other Analysis Methods

**Follow same pattern** for:
- `AnalyzeContentComplexityAsync` (maxOutputTokens: 1024, temperature: 0.3)
- `AnalyzeSceneCoherenceAsync` (maxOutputTokens: 1024, temperature: 0.5)
- `ValidateNarrativeArcAsync` (maxOutputTokens: 2048, temperature: 0.5)
- `GenerateTransitionTextAsync` (maxOutputTokens: 256, temperature: 0.7)

**Key differences for Gemini**:
1. Always use `contents` array instead of `messages`
2. Always clean markdown from responses: `text.Replace("```json", "").Replace("```", "").Trim()`
3. Extract text from `content[0].parts[0].text` instead of `message.content`
4. Use `topP` (camelCase) instead of `top_p`
5. Use `maxOutputTokens` instead of `max_tokens`

#### Helper Methods to Add

After the last method, add these helpers (copy from Anthropic or Ollama):

```csharp
private static string[] ParseStringArray(JsonElement root, string propertyName)
{
    // Copy from Anthropic provider
}

private static string BuildComplexityAnalysisPrompt(string sceneText, string? previousSceneText, string videoGoal)
{
    // Copy from Anthropic provider
}

private static double GetDoubleProperty(JsonElement root, string propertyName, double defaultValue)
{
    // Copy from Anthropic provider
}

private static int GetIntProperty(JsonElement root, string propertyName, int defaultValue)
{
    // Copy from Anthropic provider
}

private static string GetStringProperty(JsonElement root, string propertyName, string defaultValue)
{
    // Copy from Anthropic provider
}
```

### Step 2: Complete Azure OpenAI Provider

**Good news**: Azure OpenAI uses the exact same API format as OpenAI, just different endpoint.

#### Quick Implementation Strategy

The **fastest approach** is to copy implementations from `OpenAiLlmProvider.cs`:

1. Open both files side-by-side
2. For each missing method in Azure OpenAI:
   - Copy entire method from OpenAI provider
   - Replace `_logger.LogInformation("... with OpenAI")` with `"... with Azure OpenAI"`
   - Replace error messages to say "Azure OpenAI" instead of "OpenAI"
   - Keep same parameters (temperature, max_tokens, etc.)

#### Methods to Copy

1. `GenerateVisualPromptAsync` (lines ~387-509 in OpenAI)
2. `AnalyzeContentComplexityAsync` (lines ~529-606 in OpenAI)
3. `AnalyzeSceneCoherenceAsync` (lines ~678-784 in OpenAI)
4. `ValidateNarrativeArcAsync` (lines ~786-921 in OpenAI)
5. `GenerateTransitionTextAsync` (lines ~923-998 in OpenAI)

#### Helper Methods to Copy

After the last method, add these from OpenAI provider:
- `ParseStringArray`
- `BuildComplexityAnalysisPrompt`
- `GetDoubleProperty`
- `GetIntProperty`
- `GetStringProperty`

**Note**: The HTTP client setup is already correct (uses `api-key` header and Azure-specific URL format).

### Step 3: Add Explicit top_p to OpenAI Provider

In `OpenAiLlmProvider.cs`, find all `requestBody` objects and add `top_p = 0.9` parameter:

**Locations to update**:
1. `DraftScriptAsync` (line ~133): Add `top_p = 0.9`
2. `AnalyzeSceneImportanceAsync` (line ~307): Add `top_p = 0.9`
3. `GenerateVisualPromptAsync` (line ~432): Add `top_p = 0.95`
4. `AnalyzeContentComplexityAsync` (line ~552): Add `top_p = 0.9`
5. `AnalyzeSceneCoherenceAsync` (line ~716): Add `top_p = 0.9`
6. `ValidateNarrativeArcResult` (line ~838): Add `top_p = 0.9`
7. `GenerateTransitionTextAsync` (line ~956): Add `top_p = 0.95`

**Example**:
```csharp
var requestBody = new
{
    model = _model,
    messages = new[] { /* ... */ },
    temperature = 0.7,
    max_tokens = 2048,
    top_p = 0.9  // Add this line
};
```

## Testing Strategy

### Manual Testing Checklist

After implementing each provider:

1. **Build Test**:
   ```bash
   cd /home/runner/work/aura-video-studio/aura-video-studio
   dotnet build Aura.Providers/Aura.Providers.csproj --no-restore
   ```

2. **Integration Test** (if you have API keys):
   ```bash
   # Set environment variables
   export GEMINI_API_KEY="your-key"
   export AZURE_OPENAI_ENDPOINT="your-endpoint"
   export AZURE_OPENAI_KEY="your-key"
   
   # Run integration tests
   dotnet test Aura.Tests/ --filter "FullyQualifiedName~LLMProvider"
   ```

3. **Verify All Methods Implemented**:
   ```bash
   # Check Gemini provider
   grep -c "public.*Task<.*>" Aura.Providers/Llm/GeminiLlmProvider.cs
   # Should return 7 (one per interface method)
   
   # Check Azure OpenAI provider
   grep -c "public.*Task<.*>" Aura.Providers/Llm/AzureOpenAiLlmProvider.cs
   # Should return 7
   ```

### Unit Test Template

Create `Aura.Tests/Providers/Llm/GeminiLlmProviderTests.cs`:

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Aura.Providers.Llm;

namespace Aura.Tests.Providers.Llm;

public class GeminiLlmProviderTests
{
    [Fact]
    public void Constructor_InvalidApiKey_ThrowsArgumentException()
    {
        // Arrange
        var logger = new Mock<ILogger<GeminiLlmProvider>>();
        var httpClient = new HttpClient();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GeminiLlmProvider(logger.Object, httpClient, ""));
    }
    
    [Fact]
    public void Constructor_ShortApiKey_ThrowsArgumentException()
    {
        // Arrange
        var logger = new Mock<ILogger<GeminiLlmProvider>>();
        var httpClient = new HttpClient();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GeminiLlmProvider(logger.Object, httpClient, "short"));
    }
    
    // Add more tests for each method...
}
```

## Quality Checklist

Before marking complete, verify:

- [ ] All 7 interface methods implemented in Gemini provider
- [ ] All 7 interface methods implemented in Azure OpenAI provider
- [ ] Explicit `top_p` added to OpenAI provider
- [ ] All providers build without errors
- [ ] Error handling in place (try-catch, logging)
- [ ] Retry logic with exponential backoff
- [ ] Appropriate timeouts (30s for analysis, 120s for script)
- [ ] JSON response validation
- [ ] Null return on failures (for graceful degradation)
- [ ] Structured logging with appropriate levels
- [ ] Helper methods added (ParseStringArray, Get*Property, etc.)

## Configuration Recommendations

### Gemini Provider

**Best Models**:
- `gemini-1.5-pro` - Best for complex analysis (1M context window)
- `gemini-pro` - Good balance (32k context window)
- `gemini-1.5-flash` - Fastest, lower cost (1M context window)

**Optimal Settings**:
```csharp
Creative:     temperature=0.7, topP=0.9, maxTokens=2048
Analytical:   temperature=0.3, topP=0.9, maxTokens=1024
Short-form:   temperature=0.6, topP=0.9, maxTokens=256
```

### Azure OpenAI Provider

**Best Models**:
- `gpt-4o` - Latest, fastest GPT-4 (128k context)
- `gpt-4-turbo` - Previous generation (128k context)
- `gpt-35-turbo` - Cost-effective (16k context)

**Optimal Settings** (same as OpenAI):
```csharp
Creative:     temperature=0.7, top_p=0.9, max_tokens=2048
Analytical:   temperature=0.3, top_p=0.9, max_tokens=512
Short-form:   temperature=0.7, top_p=0.9, max_tokens=128
```

## Common Issues and Solutions

### Issue 1: Gemini Returns Markdown-Wrapped JSON

**Symptom**: `JsonException` when parsing Gemini responses

**Solution**: Add markdown cleanup before parsing:
```csharp
var analysisText = textProp.GetString() ?? string.Empty;

// Clean up markdown code blocks
analysisText = analysisText.Trim();
if (analysisText.StartsWith("```json"))
{
    analysisText = analysisText.Substring(7);
}
if (analysisText.StartsWith("```"))
{
    analysisText = analysisText.Substring(3);
}
if (analysisText.EndsWith("```"))
{
    analysisText = analysisText.Substring(0, analysisText.Length - 3);
}
analysisText = analysisText.Trim();

var analysisDoc = JsonDocument.Parse(analysisText);
```

### Issue 2: Azure OpenAI 404 Deployment Not Found

**Symptom**: HTTP 404 error when calling Azure OpenAI

**Solution**: Verify deployment name matches Azure portal:
- Deployment name is **not** the same as model name
- Check in Azure Portal → OpenAI → Deployments
- Use exact deployment name from portal

### Issue 3: Rate Limiting on Multiple Providers

**Symptom**: 429 errors during testing

**Solution**: Implement provider rotation:
1. Try primary provider (OpenAI/Azure)
2. On 429, fallback to Anthropic
3. On 429, fallback to Gemini
4. On 429, fallback to Ollama (local)
5. Final fallback: RuleBased

## Performance Optimization

### Context Window Management

Add truncation logic for long prompts:

```csharp
private (string prompt, bool wasTruncated) TruncatePrompt(string prompt, int maxTokens)
{
    // Rough estimate: 4 characters per token
    var estimatedTokens = prompt.Length / 4;
    
    if (estimatedTokens <= maxTokens)
    {
        return (prompt, false);
    }
    
    var targetLength = maxTokens * 4;
    var truncated = prompt[..Math.Min(targetLength, prompt.Length)];
    truncated += "\n\n[Note: Content truncated due to length]";
    
    return (truncated, true);
}
```

### Parallel Provider Calls

For non-dependent operations, call multiple providers in parallel:

```csharp
var tasks = new[]
{
    provider1.AnalyzeSceneImportanceAsync(scene, previous, goal, ct),
    provider2.GenerateVisualPromptAsync(scene, previous, tone, style, ct)
};

var results = await Task.WhenAll(tasks);
```

## Next Steps After Completion

1. **Add Token Usage Tracking**:
   - Create `LlmUsageMetrics` record
   - Track input/output tokens per request
   - Calculate costs based on provider pricing

2. **Implement Streaming**:
   - Add `IAsyncEnumerable<string> DraftScriptStreamAsync(...)`
   - Support real-time script generation feedback

3. **Add Provider Health Monitoring**:
   - Track success/failure rates
   - Monitor response times
   - Implement circuit breaker pattern

4. **Create Comprehensive Tests**:
   - Unit tests for each provider
   - Integration tests with real APIs
   - Mock provider for CI/CD

## Estimated Time to Complete

- **Gemini Provider**: 2-3 hours
  - Copy/adapt from Ollama: 1.5 hours
  - Test and debug: 0.5-1 hour
  - Add markdown cleanup: 0.5 hour

- **Azure OpenAI Provider**: 1-2 hours
  - Copy from OpenAI: 1 hour
  - Test and verify: 0.5-1 hour

- **Add top_p to OpenAI**: 30 minutes
  - Find and update 7 locations: 20 minutes
  - Test: 10 minutes

**Total**: 3.5-5.5 hours to achieve 100% feature parity across all providers.

## Success Criteria

✅ All 6 providers implement all 7 ILlmProvider interface methods
✅ Solution builds without errors
✅ All providers have consistent error handling
✅ All providers use optimal parameters
✅ Proper logging at appropriate levels
✅ Graceful degradation (return null on failures)
✅ Ready for production use

---

## Quick Reference: Method Signatures

```csharp
public interface ILlmProvider
{
    Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct);
    
    Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct);
    
    Task<VisualPromptResult?> GenerateVisualPromptAsync(
        string sceneText, string? previousSceneText, string videoTone, 
        VisualStyle targetStyle, CancellationToken ct);
    
    Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(
        string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct);
    
    Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(
        string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct);
    
    Task<NarrativeArcResult?> ValidateNarrativeArcAsync(
        IReadOnlyList<string> sceneTexts, string videoGoal, string videoType, CancellationToken ct);
    
    Task<string?> GenerateTransitionTextAsync(
        string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct);
}
```

---

**Last Updated**: After completing Anthropic and Ollama providers
**Status**: Ready for Gemini and Azure OpenAI completion
