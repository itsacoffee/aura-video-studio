> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---


# Service Initialization and Dependency Management

This document describes the service initialization order, dependency relationships, and startup behavior of the Aura Video Studio API.

## Overview

The application follows a strict service initialization order to ensure reliability, prevent race conditions, and enable graceful degradation when optional services are unavailable.

## Initialization Phases

### Phase 1: Configuration Validation
**Location**: `Program.cs` → `ConfigurationValidator`
**Timeout**: N/A (synchronous)
**Critical**: Yes

Validates all configuration settings before any services start:
- Database directory writability
- Required directory existence and permissions
- Port availability
- API key format (if provided)
- URL format validation
- Numeric configuration ranges

**Failure Behavior**: Application exits immediately with exit code 1

### Phase 2: Service Registration
**Location**: `Program.cs` → `builder.Services.Add*` calls
**Timeout**: N/A (synchronous)
**Critical**: Yes

Services are registered in dependency order:
1. **Foundational Services** (logging, configuration)
   - Serilog logger
   - Configuration providers
   - ProviderSettings

2. **Infrastructure Services** (database, caching, HTTP)
   - DbContext (SQLite)
   - Memory cache
   - HTTP clients

3. **Core Domain Services**
   - Hardware detection
   - FFmpeg locator
   - Provider factories
   - LLM providers
   - TTS providers
   - Image providers

4. **Application Services**
   - Orchestrators
   - Validators
   - Analytics
   - Asset management

5. **API Services**
   - Controllers
   - Middleware
   - Health checks

**Failure Behavior**: Application fails to start with DI container errors

### Phase 3: Startup Validation
**Location**: `Program.cs` → `StartupValidator`
**Timeout**: N/A
**Critical**: No (warnings only)

Performs file system and dependency checks:
- Directory permissions
- Available disk space
- Antivirus/firewall status

**Failure Behavior**: Logs warnings but allows application to start

### Phase 4: Database Migration
**Location**: `Program.cs` → `DbContext.Database.Migrate()`
**Timeout**: Default EF Core timeout
**Critical**: No (degraded mode)

Applies pending EF Core migrations to the SQLite database.

**Failure Behavior**: Logs error but allows application to start (database features degraded)

### Phase 5: Hosted Service Initialization
**Location**: `IHostedService` implementations
**Timeout**: Varies by service
**Critical**: Varies by service

Services start in registration order:

#### StartupInitializationService (Critical)
**Order**: 1st
**Timeout**: Per-step (10-30s)
**Critical Steps**:
1. Database Connectivity (30s) - **CRITICAL**
2. Required Directories (10s) - **CRITICAL**
3. FFmpeg Availability (10s) - Non-critical
4. AI Services (10s) - Non-critical

**Failure Behavior**:
- Critical step failure: Application exits with code 1
- Non-critical step failure: Continue with graceful degradation

#### ProviderWarmupService (Non-critical)
**Order**: 2nd
**Timeout**: Varies by provider
**Critical**: No

Warms up providers in the background:
- Never crashes the application
- Logs failures but continues
- Enables lazy loading fallback

**Failure Behavior**: Logs warnings, providers lazily loaded on first use

#### HealthCheckBackgroundService (Non-critical)
**Order**: 3rd
**Timeout**: Continuous
**Critical**: No

Runs periodic health checks on providers:
- Monitors provider availability
- Updates health metrics
- Never crashes application

**Failure Behavior**: Logs errors, restarts automatically after delay

### Phase 6: Application Start
**Location**: `app.Run()`
**Critical**: Yes

Application begins accepting HTTP requests.

## Service Dependency Graph

```
Configuration
    ├── ProviderSettings
    │   ├── Directories
    │   ├── FFmpegLocator
    │   └── Provider Factories
    ├── Database
    │   └── DbContext
    └── Caching
        └── MemoryCache

Hardware Detection
    ├── DiagnosticsHelper
    └── SystemProfile

Providers (Lazy)
    ├── LlmProviderFactory
    │   ├── OpenAI (optional)
    │   ├── Anthropic (optional)
    │   ├── Ollama (optional)
    │   └── RuleBased (fallback)
    ├── TtsProviderFactory
    │   ├── Azure TTS (optional)
    │   ├── ElevenLabs (optional)
    │   ├── Windows TTS (platform-specific)
    │   └── Null TTS (fallback)
    └── ImageProviderFactory
        ├── Stable Diffusion (optional)
        └── Stock Providers (optional)

Orchestration
    ├── ScriptOrchestrator
    ├── VideoOrchestrator
    └── JobRunner

API Layer
    ├── Controllers
    ├── Middleware
    └── Health Checks
```

## Graceful Degradation Strategy

### Critical Services (Must Initialize)
1. **Database**: Application cannot function without data persistence
2. **Required Directories**: Application needs writable file system access

### Non-Critical Services (Optional)
1. **FFmpeg**: Many operations work without it, video operations gracefully degrade
2. **AI Services**: Application falls back to rule-based alternatives
3. **External APIs**: Application continues with limited functionality

### Degraded Mode Indicators
- Health endpoint reports "degraded" status
- Missing features return 503 Service Unavailable
- UI shows warnings about unavailable features
- Logs indicate which services failed to initialize

## Startup Timeout Configuration

| Service | Timeout | Configurable | Default |
|---------|---------|--------------|---------|
| Database Connection | 30s | Yes (EF Core) | 30s |
| Directory Creation | 10s | No | 10s |
| FFmpeg Detection | 10s | No | 10s |
| AI Service Check | 10s | No | 10s |
| Configuration Validation | N/A | No | Immediate |

## Service Lifetime Scopes

### Singleton Services
- **When**: Stateless services, shared across all requests
- **Examples**: Hardware detectors, provider factories, orchestrators
- **Risk**: Memory leaks if holds request-specific data

### Scoped Services
- **When**: Request-bound services, new instance per HTTP request
- **Examples**: DbContext, template service
- **Risk**: Incorrect sharing across requests

### Transient Services
- **When**: Lightweight, short-lived operations
- **Examples**: Validators, factories
- **Risk**: Performance overhead if heavy

## Monitoring and Diagnostics

### Startup Logs
All initialization steps are logged with:
- Timestamp
- Step name
- Success/failure status
- Duration in milliseconds
- Critical/non-critical designation

Example:
```
[18:00:01.234 INF] === Service Initialization Starting ===
[18:00:01.235 INF] Initializing: Database Connectivity (Critical: True, Timeout: 30s)
[18:00:01.456 INF] ✓ Database Connectivity initialized successfully in 221ms
[18:00:01.457 INF] Initializing: Required Directories (Critical: True, Timeout: 10s)
[18:00:01.467 INF] ✓ Required Directories initialized successfully in 10ms
[18:00:01.468 INF] Initializing: FFmpeg Availability (Critical: False, Timeout: 10s)
[18:00:01.789 WRN] ⚠ FFmpeg Availability failed to initialize - continuing with graceful degradation (took 321ms)
[18:00:01.790 INF] === Service Initialization COMPLETE ===
[18:00:01.791 INF] Total time: 557ms, Successful: 2/3
[18:00:01.792 WRN] Some non-critical services failed. Application running in degraded mode.
```

### Health Endpoints
- `/api/health/live`: Liveness probe (always returns 200 if app is running)
- `/api/health/ready`: Readiness probe (returns service status and dependencies)
- `/api/health/first-run`: Comprehensive diagnostics with actionable guidance

### Troubleshooting

#### Application won't start
1. Check startup logs for "CRITICAL" failures
2. Verify database directory is writable
3. Ensure port 5005 (or configured port) is available
4. Review configuration validation errors

#### Application starts but features missing
1. Check for "degraded" status in health endpoint
2. Review non-critical service failures in logs
3. Verify optional dependencies (FFmpeg, API keys)
4. Check file permissions and disk space

#### Slow startup
1. Review initialization timing in logs
2. Check for network timeouts (AI services, external APIs)
3. Verify database performance
4. Consider disabling optional services

## Best Practices

### Adding New Services
1. **Determine criticality**: Will app function without it?
2. **Add to correct phase**: Based on dependencies
3. **Set appropriate timeout**: Balance reliability vs. startup time
4. **Add to dependency graph**: Document relationships
5. **Implement graceful degradation**: Handle initialization failure
6. **Add initialization logging**: Include timing and status

### Modifying Service Order
1. **Understand dependencies**: What does it depend on?
2. **Test thoroughly**: Race conditions may be subtle
3. **Update documentation**: Keep dependency graph current
4. **Consider startup time**: Later services delay app readiness

### Configuration Changes
1. **Add validation**: ConfigurationValidator should catch errors
2. **Provide defaults**: Minimize required configuration
3. **Document requirements**: What values are valid?
4. **Handle missing config**: Graceful degradation or clear error

## Future Improvements

### Planned Enhancements
- [ ] Parallel initialization of independent services
- [ ] Configurable timeout values via appsettings
- [ ] Retry logic for transient failures
- [ ] Health check dashboard in UI
- [ ] Startup profiling and optimization
- [ ] Service dependency visualization tool

### Known Limitations
- Sequential initialization increases startup time
- No automatic rollback of partial initialization
- Limited retry logic for transient failures
- No circuit breaker for external dependencies
