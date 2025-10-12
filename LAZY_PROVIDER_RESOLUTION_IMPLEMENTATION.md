# Lazy Provider Resolution Implementation

## Overview
This PR implements lazy provider resolution to ensure the application starts successfully even when provider initialization fails. Previously, the application would crash at startup if any provider factory method threw an exception.

## Problem Statement
Before this change:
- `ScriptOrchestrator` and `PlannerService` eagerly created all providers during DI registration in `Program.cs`
- If provider initialization failed (e.g., missing API keys, configuration issues), the entire application would fail to start
- This violated the principle of "fail gracefully" - the app should start even if optional features are misconfigured

## Solution
Implemented lazy provider resolution using factory delegates:

### 1. Modified Core Services
**ScriptOrchestrator** and **PlannerService** now support two constructor signatures:
- Legacy: Accepts pre-created `Dictionary<string, IProvider>` (for backward compatibility)
- New: Accepts `Func<Dictionary<string, IProvider>>` factory delegate

Providers are created lazily on first use via a thread-safe `GetProviders()` method.

### 2. Updated Program.cs
Changed from:
```csharp
// OLD - Eager resolution at startup
var providers = factory.CreateAvailableProviders(loggerFactory);
return new ScriptOrchestrator(logger, loggerFactory, mixer, providers);
```

To:
```csharp
// NEW - Lazy resolution with factory delegate
Func<Dictionary<string, ILlmProvider>> providerFactory = 
    () => factory.CreateAvailableProviders(loggerFactory);
return new ScriptOrchestrator(logger, loggerFactory, mixer, providerFactory);
```

### 3. Added ProviderWarmupService
Created a background hosted service that:
- Starts 2 seconds after application startup
- Attempts to warm up all provider factories
- Logs successes and failures without throwing exceptions
- Provides visibility into provider initialization status

## Benefits
✅ **Resilient Startup**: Application never crashes due to provider initialization failures
✅ **Lazy Loading**: Providers only created when actually needed
✅ **Early Visibility**: Warmup service logs provider status for debugging
✅ **Backward Compatible**: Existing tests continue to work without changes
✅ **Thread-Safe**: Provider initialization uses locking to prevent race conditions

## Testing
- **555 existing tests** pass without modification (backward compatibility)
- **5 new integration tests** verify lazy provider resolution behavior
- **Manual verification**: Application starts successfully with `dotnet run`

## Files Changed
1. `Aura.Core/Orchestrator/ScriptOrchestrator.cs` - Added lazy initialization constructor
2. `Aura.Core/Planner/PlannerService.cs` - Added lazy initialization constructor
3. `Aura.Api/Program.cs` - Updated DI registrations to use factory delegates
4. `Aura.Api/HostedServices/ProviderWarmupService.cs` - New warmup service
5. `Aura.Tests/LazyProviderResolutionTests.cs` - New integration tests

## Verification
Application startup logs now show:
```
[18:27:29 INF] Application started. Press Ctrl+C to shut down.
[18:27:31 INF] Starting provider warmup...
[18:27:31 INF] LLM providers warmed up: 2 providers available (RuleBased, Ollama)
[18:27:31 INF] Planner providers warmed up: 1 providers available (RuleBased)
[18:27:31 INF] Provider warmup completed
```

Notice the ~2 second delay between app startup and provider warmup, proving providers are not blocking startup.

## Commit Message
```
chore: avoid eager provider resolution on startup; add warmup service

- Modified ScriptOrchestrator and PlannerService to support lazy provider initialization
- Updated Program.cs to use factory delegates instead of eager provider creation
- Added ProviderWarmupService to warm up providers in background after startup
- All 555 existing tests pass + 5 new integration tests
- Application now starts successfully even with misconfigured providers
```
