# Service Initialization Order Documentation

This document describes the deterministic initialization and shutdown order for Aura Video Studio services.

## Overview

The application follows a phased initialization approach to ensure services start in the correct order and dependencies are resolved properly.

## Initialization Phases

### Phase 1: Service Registration (Builder Phase)
**Location**: `Aura.Api/Program.cs` lines 39-625

Services are registered with the DI container in dependency order:

1. **Core Infrastructure**
   - Serilog logging
   - Controllers, Swagger, CORS
   - JSON serialization with enum converters

2. **Hardware & System**
   - `HardwareDetector`
   - `DiagnosticsHelper`
   - `ProviderSettings`

3. **FFmpeg & Media**
   - `IFfmpegLocator`
   - `FfmpegInstaller`
   - `FfmpegLocator`
   - `HttpDownloader`

4. **LLM & Providers**
   - `LlmProviderFactory`
   - `ProviderMixer`
   - `ScriptOrchestrator`
   - Provider instances (Ollama, GPT, Gemini, etc.)

5. **TTS Providers**
   - `TtsProviderFactory`
   - `NullTtsProvider` (fallback)
   - `WindowsTtsProvider` (Windows only)
   - `AzureTtsProvider`
   - `AzureVoiceDiscovery`

6. **Image & Video Providers**
   - `ImageProviderFactory`
   - `IVideoComposer` (FfmpegVideoComposer)

7. **Validation Services**
   - `PreGenerationValidator`
   - `ScriptValidator`
   - `TtsOutputValidator`
   - `ImageOutputValidator`
   - `LlmOutputValidator`

8. **Pipeline & Health Services**
   - `ProviderHealthMonitor`
   - `ProviderRetryWrapper`
   - `ResourceCleanupManager`
   - `TemporaryFileCleanupService`
   - `DiskSpaceChecker`

9. **Orchestration Services**
   - `ResourceMonitor`
   - `StrategySelector`
   - `VideoGenerationOrchestrator`
   - `VideoOrchestrator`

10. **AI & Analytics Services**
    - Pacing services (RhythmDetector, RetentionOptimizer, etc.)
    - Performance analytics
    - Timeline editor services

11. **Context & Profile Management**
    - `ContextPersistence`
    - `ConversationContextManager`
    - `ProjectContextManager`
    - `ConversationalLlmService`
    - `ProfilePersistence`
    - `ProfileService`

12. **Learning & Ideation**
    - `LearningPersistence`
    - Learning analysis engines
    - `IdeationService`

13. **Content & Assets**
    - Content analyzers and enhancers
    - Asset library services
    - Stock image services

14. **Engine & Dependencies**
    - `DependencyManager`
    - `EngineManifestLoader`
    - `EngineInstaller`
    - `ModelInstaller`
    - `ExternalProcessManager`
    - `LocalEnginesRegistry`
    - `EngineLifecycleManager`
    - `EngineDetector`

15. **Setup & Health Check Services**
    - `HealthCheckService`
    - `StartupValidator`
    - `FirstRunDiagnostics`

16. **Background Services (Hosted Services)**
    - `ProviderWarmupService` (warms up providers in background)
    - `HealthCheckBackgroundService` (runs scheduled health checks)

### Phase 2: Startup Validation
**Location**: `Aura.Api/Program.cs` lines 639-651

After building the application but before starting the web server:

1. **Startup Validator** runs to check:
   - File system permissions
   - Required directories exist
   - Critical configuration is valid

2. **Result**: Logs warnings but continues even if validation fails (graceful degradation)

### Phase 3: Application Started Event
**Location**: `Aura.Api/Program.cs` lines 2453-2525

When the application starts (after web server is listening):

1. **Engine Lifecycle Manager** (First Priority)
   - Starts asynchronously via `Task.Run`
   - Manages local AI engine instances
   - Logs success/failure but doesn't block startup

2. **Provider Health Monitor** (Second Priority)
   - Waits 2 seconds for Engine Lifecycle Manager to initialize
   - Starts periodic health checks in background
   - Automatically restarts after errors with 1-minute delay

### Phase 4: Background Services Initialization
**Location**: Various hosted services

Hosted services registered in DI start automatically:

1. `ProviderWarmupService` - Pre-initializes providers
2. `HealthCheckBackgroundService` - Schedules health checks

## Shutdown Order (Deterministic Reverse Order)

**Location**: `Aura.Api/Program.cs` lines 2527-2548

### Shutdown Phase 1: Stop Background Services

Services stop in reverse order of startup:

1. **Provider Health Monitor**
   - Automatically stops via cancellation token
   - No explicit shutdown needed

2. **Engine Lifecycle Manager**
   - Explicitly stops all managed engines
   - Waits for clean shutdown
   - Logs any errors

### Shutdown Phase 2: Framework Cleanup

ASP.NET Core automatically:
- Stops hosted services
- Disposes singleton services
- Closes network connections

## Key Design Decisions

### 1. Graceful Degradation
- Services log errors but don't crash the application
- Background services can fail without affecting core functionality
- Startup validation warns but doesn't block

### 2. Deterministic Ordering
- Engine Lifecycle Manager starts first (manages dependencies)
- Provider Health Monitor starts second (depends on engines)
- 2-second delay ensures proper ordering

### 3. State Tracking
- Flags track which services started successfully
- Shutdown only stops services that actually started
- Prevents errors from attempting to stop non-started services

### 4. Comprehensive Logging
- Each phase is logged with clear labels
- Success and failure paths both log
- Helps diagnose initialization issues

## Troubleshooting

### Service Not Starting

Check logs for:
1. Phase 1 completion: "Service Registration Complete"
2. Phase 2 validation: "Startup validation completed successfully"
3. Phase 3 start: "Background services initialization started"
4. Individual service messages: "Engine Lifecycle Manager started successfully"

### Services Starting Out of Order

Look for:
- Missing 2-second delay between Engine and Health Monitor
- Task.Run not being used (synchronous initialization)
- Exceptions during earlier service initialization

### Shutdown Hangs

Check:
- Engine Lifecycle Manager is properly stopping
- Cancellation tokens are being respected
- No infinite loops in service cleanup

## Future Improvements

1. **Service Dependencies**: Could use a more explicit dependency graph
2. **Initialization Timeout**: Add timeouts for background service startup
3. **Health Checks**: Expose service health status via API
4. **Retry Logic**: More sophisticated retry for transient failures
