# Provider Validation and Fallback Implementation

## Overview

This implementation adds comprehensive provider validation, graceful fallback, and downgrade tracking to the Aura Video Studio pipeline. The system ensures that video generation continues even when preferred providers fail, automatically falling back to alternative providers while logging the downgrade information.

## Architecture

### Core Components

#### 1. ProviderValidator Base Class
**Location:** `Aura.Core/Providers/ProviderValidator.cs`

Abstract base class that defines the contract for validating provider availability:
- `ValidateAsync()` - Checks if a provider is available and ready to use
- Returns `ProviderValidationResult` with availability status, details, and error messages

#### 2. ResultEnvelope
**Location:** `Aura.Core/Orchestration/ResultEnvelope.cs`

Generic result wrapper that tracks provider selection and downgrade information:
- `SourceProvider` - The originally requested provider
- `ActualProvider` - The provider that actually produced the result
- `WasDowngraded` - Flag indicating if fallback occurred
- `DowngradeReason` - Explanation of why fallback was necessary
- `Timestamp` - When the operation completed

#### 3. Enhanced ScriptResult
**Location:** `Aura.Core/Orchestrator/ScriptOrchestrator.cs`

Extended to include downgrade metadata:
- `RequestedProvider` - Original provider requested
- `DowngradeReason` - Why fallback occurred
- Existing fields: `Success`, `ErrorCode`, `ErrorMessage`, `Script`, `ProviderUsed`, `IsFallback`

### Provider Validators

#### LLM Providers
- **OpenAiLlmValidator** - Validates OpenAI API connectivity and authentication
- **OllamaLlmValidator** - Checks if Ollama is running and has models installed
- **RuleBasedLlmValidator** - Always available (built-in fallback)

#### TTS Providers
- **ElevenLabsTtsValidator** - Validates ElevenLabs API connectivity
- **WindowsTtsValidator** - Checks Windows SAPI availability (Windows only)

#### Image Providers
- **StableDiffusionImageValidator** - Validates SD WebUI availability and models
- **StockImageValidator** - Always available (built-in fallback)

## Fallback Chain

### Script Generation (LLM)
1. **Primary**: User-selected provider (e.g., OpenAI)
2. **Secondary**: Ollama (if available)
3. **Tertiary**: RuleBased (always available)

### Voice Synthesis (TTS)
1. **Primary**: Pro TTS (e.g., ElevenLabs)
2. **Secondary**: Windows SAPI (if on Windows)
3. **Tertiary**: Mock TTS (development fallback)

### Visual Generation
1. **Primary**: Pro services (e.g., Runway - planned)
2. **Secondary**: Local Stable Diffusion (if available)
3. **Tertiary**: Stock images (always available)

## Runtime Behavior

### Successful Provider Execution
```
1. Provider selected based on user tier preference
2. Provider executes successfully
3. Result includes:
   - ProviderUsed = selected provider
   - IsFallback = false
   - RequestedProvider = null
   - DowngradeReason = null
```

### Provider Failure with Fallback
```
1. Primary provider selected
2. Primary provider fails (network, API key, etc.)
3. System logs warning: "Primary provider {Provider} failed, attempting fallback"
4. Secondary provider attempted
5. If successful:
   - ProviderUsed = fallback provider name
   - IsFallback = true
   - RequestedProvider = original provider name
   - DowngradeReason = "Primary provider {X} failed: {reason}"
6. System logs: "Successfully downgraded from {Requested} to {Actual}"
```

### All Providers Failed
```
1. All providers in fallback chain fail
2. Result includes:
   - Success = false
   - ErrorCode = "E300"
   - ErrorMessage = "All LLM providers failed to generate script"
```

## Logging

The implementation provides comprehensive logging:

### Info Level
- Provider selection: "[Script] Provider: OpenAI - Pro provider available and preferred"
- Successful generation: "Successfully generated script with OpenAI (1234 chars)"
- Fallback success: "Successfully downgraded from OpenAI to RuleBased"

### Warning Level
- Primary failure: "Primary provider OpenAI failed, attempting fallback: Connection timeout"
- Downgrade: "[Script] Provider: RuleBased (FALLBACK from OpenAI) - Final fallback"

### Error Level
- Validation failures: "Failed to validate OpenAI LLM provider: Invalid API key"

## Testing

### Unit Tests

#### ProviderDowngradeTests (6 tests)
- `ProLlmFails_ShouldFallbackToFreeProvider` - Validates Pro→Free fallback
- `LocalSdDisabled_ShouldFallbackToStockVisuals` - Validates SD→Stock fallback
- `DowngradeEnvelope_ShouldContainMetadata` - Verifies metadata tracking
- `AllProvidersFail_ShouldReturnFailure` - Validates total failure handling
- `PrimarySucceeds_ShouldNotFallback` - Ensures no unnecessary fallback
- `SecondProviderSucceeds_ShouldSkipFurtherFallbacks` - Validates chain stops after success

#### ProviderValidatorTests (10 tests)
- Validates each provider validator's behavior
- Tests availability checks with various scenarios
- Verifies error handling and messaging

### Integration
All existing ScriptOrchestrator tests continue to pass, demonstrating backward compatibility.

## Usage Example

```csharp
// In an orchestrator
var result = await scriptOrchestrator.GenerateScriptAsync(
    brief, 
    spec, 
    preferredTier: "Pro",
    offlineOnly: false,
    cancellationToken);

if (result.Success)
{
    Console.WriteLine($"Script generated by: {result.ProviderUsed}");
    
    if (result.IsFallback)
    {
        Console.WriteLine($"Note: Originally requested {result.RequestedProvider}");
        Console.WriteLine($"Downgrade reason: {result.DowngradeReason}");
    }
}
```

## UI Integration

The downgrade information can be surfaced to users via:
1. Toast notifications showing which provider was used
2. Warning messages when fallback occurs
3. Detailed logs in settings/diagnostics panel
4. API response metadata for web/CLI clients

## Future Enhancements

1. **Preflight Validation**: Run validators before attempting generation
2. **Provider Health Monitoring**: Track success rates and latency
3. **Smart Selection**: Choose providers based on historical performance
4. **User Preferences**: Allow users to customize fallback chains
5. **TTS and Visual Orchestrators**: Apply same pattern to other stages

## Error Codes

- **E300**: General provider failure
- **E301**: Request timeout or cancellation
- **E302**: Provider returned empty/invalid result
- **E305**: Provider not available/not registered
- **E307**: Offline mode restriction (Pro providers blocked)

## Acceptance Criteria Met

✅ Runs do not abort on first provider failure; fallback is attempted
✅ UI and logs clearly show when and why a fallback occurred
✅ ProviderValidator abstraction created with per-provider implementations
✅ ResultEnvelope tracks source and downgraded provider fields
✅ Unit tests validate fallback scenarios
✅ Downgrade info logged via Serilog (through ILogger)
