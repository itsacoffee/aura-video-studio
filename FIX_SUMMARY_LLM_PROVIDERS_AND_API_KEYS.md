# Fix Summary: LLM Provider Constructor Issues and Missing API Key Fields

## Problems Fixed

### 1. LLM Provider Constructor Errors
**Issue**: Script generation was failing with errors:
- `CreateLogger method not found on ILoggerFactory`
- `Constructor on type 'Aura.Providers.Llm.OpenAiLlmProvider' not found`
- `Constructor on type 'Aura.Providers.Llm.RuleBasedLlmProvider' not found`

**Root Cause**: The code was trying to use reflection to create typed loggers like `ILogger<RuleBasedLlmProvider>`, but couldn't reference the provider types directly from `Aura.Core` because it would create a circular dependency. The incorrect approach was:
- Using `GetMethod("CreateLogger", Array.Empty<Type>())` which doesn't find the generic extension method
- Passing non-typed loggers to constructors that expect `ILogger<T>`

**Solution**: Used `LoggerFactoryExtensions.CreateLogger<T>()` via reflection:
```csharp
var createLoggerMethod = typeof(LoggerFactoryExtensions)
    .GetMethods()
    .FirstOrDefault(m => m.Name == "CreateLogger" && m.IsGenericMethod && m.GetParameters().Length == 1);

var genericMethod = createLoggerMethod.MakeGenericMethod(type);
var logger = genericMethod.Invoke(null, new object[] { loggerFactory });
```

**Files Changed**:
- `Aura.Core/Orchestrator/LlmProviderFactory.cs` - Fixed all provider creation methods
- `Aura.Core/Orchestrator/ScriptOrchestrator.cs` - Fixed RuleBased fallback instantiation, added `ILoggerFactory` parameter
- `Aura.Api/Program.cs` - Updated ScriptOrchestrator registration to pass `ILoggerFactory`
- All test files in `Aura.Tests/` - Updated to pass `ILoggerFactory` to ScriptOrchestrator

### 2. Missing API Key Fields for Stock Sources
**Issue**: Pixabay and Unsplash stock sources required API keys but there was no place in the UI to enter them. The API key storage system only included:
- OpenAI
- ElevenLabs
- Pexels
- Stability AI

**Solution**: Added Pixabay and Unsplash API key fields throughout the stack:

**Backend Changes**:
- `Aura.Api/Models/ApiModels.V1/Dtos.cs` - Added `PixabayKey` and `UnsplashKey` to `ApiKeysRequest` record
- `Aura.Api/Program.cs` - Updated API key save/load endpoints to include pixabay and unsplash keys

**Frontend Changes**:
- `Aura.Web/src/pages/SettingsPage.tsx` - Added input fields for Pixabay and Unsplash API keys in the settings UI

## Testing

All tests pass successfully:
- 54 script-related tests pass
- The RuleBased "free" provider now works as a guaranteed fallback
- Script generation no longer fails with constructor errors
- API keys for all stock sources can now be saved and loaded

## Impact

### Before
- Script generation failed completely with "No LLM providers available" or constructor errors
- Even the "free" RuleBased provider couldn't be instantiated as a fallback
- Users could not enter API keys for Pixabay and Unsplash

### After
- Script generation works with all provider types (OpenAI, Ollama, Azure, Gemini, RuleBased)
- RuleBased provider successfully instantiates as a guaranteed free fallback
- Users can now enter and save API keys for all stock sources including Pixabay and Unsplash
- The system has proper fallback chains: Cloud → Local → RuleBased

## Files Modified

### Core Changes
1. `Aura.Core/Orchestrator/LlmProviderFactory.cs`
2. `Aura.Core/Orchestrator/ScriptOrchestrator.cs`
3. `Aura.Api/Program.cs`
4. `Aura.Api/Models/ApiModels.V1/Dtos.cs`
5. `Aura.Web/src/pages/SettingsPage.tsx`

### Test Updates
6. `Aura.Tests/ScriptApiTests.cs`
7. `Aura.Tests/ScriptEndpointE2ETests.cs`
8. `Aura.Tests/ScriptOrchestratorTests.cs`
9. `Aura.Tests/ProviderDowngradeTests.cs`

## Verification

To verify the fixes work:

1. **Test Script Generation**:
   ```bash
   dotnet test --filter "FullyQualifiedName~Script"
   ```
   Result: All 54 tests pass ✓

2. **Test RuleBased Provider**:
   ```bash
   dotnet test --filter "FullyQualifiedName~RuleBased"
   ```
   Result: All 17 tests pass ✓

3. **UI Verification**: 
   - Navigate to Settings → API Keys tab
   - Verify fields are present for: OpenAI, ElevenLabs, Pexels, Pixabay, Unsplash, Stability AI
   - Enter and save keys
   - Reload page and verify keys are persisted (masked)

4. **E2E Verification**:
   - Create a video with "Free" tier (no API keys)
   - Should use RuleBased provider successfully
   - Enable Pixabay/Unsplash in wizard
   - Should request API keys if not configured
