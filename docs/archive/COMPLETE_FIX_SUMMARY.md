# Complete Fix: LLM Provider and API Key Issues

## Problem Statement

The application was failing with critical errors preventing script generation:

1. **Constructor Errors**: 
   - "CreateLogger method not found on ILoggerFactory"
   - "Constructor on type 'Aura.Providers.Llm.OpenAiLlmProvider' not found"
   - "Constructor on type 'Aura.Providers.Llm.RuleBasedLlmProvider' not found"

2. **Missing API Key Fields**: 
   - Pixabay and Unsplash stock sources required API keys but had no UI to enter them
   - System stored only 4 API keys (OpenAI, ElevenLabs, Pexels, Stability AI)

3. **Script Generation Failure**: 
   - Even with "free" failback options, script generation failed
   - RuleBased provider couldn't be instantiated
   - Resulted in "E300 - All LLM providers failed to generate script"

## Root Causes

### 1. Incorrect Logger Creation
The code used reflection to create provider instances but failed to create typed loggers correctly:
- Used `GetMethod("CreateLogger", Array.Empty<Type>())` which couldn't find the generic extension method
- Tried to pass `ILogger` instead of `ILogger<T>` to constructors
- Couldn't reference provider types directly from `Aura.Core` (circular dependency)

### 2. Incomplete API Key Storage
The API key save/load endpoints only supported 4 providers, not all 6 stock sources that required API keys.

## Solutions Implemented

### 1. Fixed Logger Creation via Reflection

**File**: `Aura.Core/Orchestrator/LlmProviderFactory.cs`

Used `LoggerFactoryExtensions.CreateLogger<T>()` via reflection:

```csharp
private ILlmProvider CreateRuleBasedProvider(ILoggerFactory loggerFactory)
{
    var type = Type.GetType("Aura.Providers.Llm.RuleBasedLlmProvider, Aura.Providers");
    
    // Get the CreateLogger<T> extension method
    var createLoggerMethod = typeof(LoggerFactoryExtensions)
        .GetMethods()
        .FirstOrDefault(m => m.Name == "CreateLogger" && 
                            m.IsGenericMethod && 
                            m.GetParameters().Length == 1);
    
    // Make it generic for the specific provider type
    var genericMethod = createLoggerMethod.MakeGenericMethod(type);
    
    // Invoke to get ILogger<ProviderType>
    var logger = genericMethod.Invoke(null, new object[] { loggerFactory });
    
    return (ILlmProvider)Activator.CreateInstance(type, logger)!;
}
```

Applied this pattern to all provider creation methods:
- `CreateRuleBasedProvider()`
- `CreateOllamaProvider()`
- `CreateOpenAiProvider()`
- `CreateAzureOpenAiProvider()`
- `CreateGeminiProvider()`

### 2. Fixed ScriptOrchestrator Fallback

**File**: `Aura.Core/Orchestrator/ScriptOrchestrator.cs`

- Added `ILoggerFactory` parameter to constructor
- Updated fallback instantiation to use same reflection pattern
- Ensures RuleBased provider can always be instantiated as last resort

### 3. Added Missing API Key Fields

**Backend Files**:
- `Aura.Api/Models/ApiModels.V1/Dtos.cs` - Added PixabayKey and UnsplashKey to record
- `Aura.Api/Program.cs` - Updated save/load endpoints to handle 6 keys

**Frontend File**:
- `Aura.Web/src/pages/SettingsPage.tsx` - Added UI input fields for Pixabay and Unsplash

### 4. Updated All Tests

Updated test files to pass `ILoggerFactory` to `ScriptOrchestrator`:
- `Aura.Tests/ScriptApiTests.cs`
- `Aura.Tests/ScriptEndpointE2ETests.cs`
- `Aura.Tests/ScriptOrchestratorTests.cs`
- `Aura.Tests/ProviderDowngradeTests.cs`

## Verification Results

### Build Status
✅ **Build succeeds** - 0 errors, warnings only

### Test Results
✅ **432 unit tests pass** (Aura.Tests)
✅ **29 E2E tests pass** (Aura.E2E)
✅ **54 script-related tests pass**
✅ **17 RuleBased provider tests pass**

### Key Test Validation
- RuleBased provider instantiates successfully as guaranteed fallback
- All provider types can be created with correct logger types
- Script generation works end-to-end
- API keys can be saved and loaded for all 6 stock sources

## Impact

### Before
❌ Script generation completely broken
❌ Constructor errors prevented any provider from working
❌ Even "free" RuleBased fallback failed
❌ No way to configure Pixabay/Unsplash API keys

### After
✅ Script generation works with all provider types
✅ RuleBased provider serves as guaranteed free fallback
✅ All LLM providers (OpenAI, Ollama, Azure, Gemini, RuleBased) work correctly
✅ All 6 stock sources can be configured with API keys
✅ Proper fallback chain: Cloud → Local → RuleBased

## Files Modified

### Core Changes (9 files)
1. `Aura.Core/Orchestrator/LlmProviderFactory.cs` - Fixed all provider creation methods
2. `Aura.Core/Orchestrator/ScriptOrchestrator.cs` - Added ILoggerFactory, fixed fallback
3. `Aura.Api/Program.cs` - Updated ScriptOrchestrator registration and API key storage
4. `Aura.Api/Models/ApiModels.V1/Dtos.cs` - Added Pixabay and Unsplash to API keys
5. `Aura.Web/src/pages/SettingsPage.tsx` - Added UI fields for new API keys
6. `Aura.Tests/ScriptApiTests.cs` - Updated test, fixed expectations
7. `Aura.Tests/ScriptEndpointE2ETests.cs` - Updated constructor calls
8. `Aura.Tests/ScriptOrchestratorTests.cs` - Updated constructor calls
9. `Aura.Tests/ProviderDowngradeTests.cs` - Updated constructor calls

### Documentation (2 files)
10. `FIX_SUMMARY_LLM_PROVIDERS_AND_API_KEYS.md` - Technical details
11. `UI_CHANGES_API_KEYS.md` - UI changes documentation

## Testing Instructions

### 1. Verify Script Generation
```bash
cd /home/runner/work/aura-video-studio/aura-video-studio
dotnet test --filter "FullyQualifiedName~Script"
```
Expected: All 54 tests pass

### 2. Verify RuleBased Provider
```bash
dotnet test --filter "FullyQualifiedName~RuleBased"
```
Expected: All 17 tests pass

### 3. Verify API Key Storage
- Launch application
- Navigate to Settings → API Keys tab
- Verify 6 input fields are present:
  - OpenAI
  - ElevenLabs
  - Pexels
  - **Pixabay** (NEW)
  - **Unsplash** (NEW)
  - Stability AI
- Enter test keys and save
- Reload page and verify keys are persisted

### 4. End-to-End Script Generation
- Create a new video with "Free" tier (no API keys required)
- Should use RuleBased provider
- Should generate script successfully
- No constructor errors should occur

## Security Notes

- API keys stored in `%LOCALAPPDATA%\Aura\apikeys.json`
- Keys are masked in UI (password fields)
- Only first 8 characters shown when loading saved keys
- TODO: Implement DPAPI encryption (noted in code comments)

## Future Improvements

1. Add validation for Pixabay and Unsplash API keys (similar to OpenAI, ElevenLabs)
2. Implement DPAPI encryption for stored API keys
3. Add better error messages when API keys are invalid
4. Consider moving to secure key storage (Windows Credential Manager, etc.)
