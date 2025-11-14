# Orchestration and Startup Diagnostics Runbook

## Overview
This runbook provides operational guidance for diagnosing, troubleshooting, and monitoring the Aura Video Studio startup orchestration and service initialization process.

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Service Initialization Order](#service-initialization-order)
- [Health Checks](#health-checks)
- [Diagnostic Procedures](#diagnostic-procedures)
- [Common Issues](#common-issues)
- [Log Analysis](#log-analysis)
- [Performance Metrics](#performance-metrics)

---

## Architecture Overview

### Startup Flow
```
Application Start
    ↓
Logging Configuration
    ↓
Configuration Loading
    ↓
Database Connection
    ↓
Required Directories Setup
    ↓
Dependency Validation (FFmpeg, Python, etc.)
    ↓
AI Services Initialization
    ↓
Job/Worker Registration
    ↓
API Binding (Kestrel/HTTP Server)
    ↓
Application Ready (/health/ready → 200 OK)
```

### Service Dependencies
- **Database** depends on: Logging
- **FFmpeg Validator** depends on: File System
- **Video Services** depend on: Database, FFmpeg
- **AI Services** depend on: Configuration, Network
- **Export Pipeline** depends on: Video Services, FFmpeg

---

## Service Initialization Order

### Phase 1: Core Infrastructure (Critical)
1. **Logging Service** - First to initialize, all others depend on it
   - Timeout: 5 seconds
   - Failure Mode: Fatal - application exits
   
2. **Database Connectivity** - Required for persistence
   - Timeout: 30 seconds
   - Failure Mode: Fatal - application exits
   - Retry: 3 attempts with exponential backoff
   
3. **Required Directories** - File system structure
   - Timeout: 10 seconds
   - Failure Mode: Fatal - application exits
   - Creates: Data, Output, Projects, Logs directories

### Phase 2: External Dependencies (Conditional)
4. **FFmpeg Availability** - Video processing capability
   - Timeout: 10 seconds
   - Failure Mode: Graceful degradation - app continues
   - Impact: Video export and preview disabled until FFmpeg configured
   
5. **Python Detection** - AI capabilities
   - Timeout: 10 seconds
   - Failure Mode: Graceful degradation
   - Impact: Local AI features disabled, cloud APIs still available

### Phase 3: Application Services (Non-Critical)
6. **AI Services** - Optional enhancements
   - Timeout: 10 seconds per service
   - Failure Mode: Graceful degradation
   - Impact: Specific AI features unavailable based on which service fails

7. **Background Workers** - Async job processing
   - Timeout: 5 seconds
   - Failure Mode: Graceful degradation
   - Impact: Background jobs queued until workers available

### Total Expected Startup Time
- **Optimal**: 3-5 seconds (all dependencies present)
- **Degraded**: 5-10 seconds (some dependencies missing)
- **Failure**: >10 seconds (critical dependency failed)

---

## Health Checks

### Liveness Probe
**Endpoint**: `GET /health/live`

**Purpose**: Verify the application process is running

**Expected Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-10-27T22:00:00Z"
}
```

**Response Codes**:
- `200 OK`: Application is alive
- `503 Service Unavailable`: Application is starting or shutting down

**When to Use**: Container orchestration liveness checks, uptime monitoring

### Readiness Probe
**Endpoint**: `GET /health/ready`

**Purpose**: Verify the application is ready to serve traffic

**Expected Response**:
```json
{
  "status": "Ready",
  "timestamp": "2025-10-27T22:00:00Z",
  "services": {
    "database": "healthy",
    "ffmpeg": "healthy",
    "aiServices": "degraded"
  }
}
```

**Response Codes**:
- `200 OK`: Application is ready to serve requests
- `503 Service Unavailable`: Still initializing or critical service failed

**Degraded Mode**: Returns 200 but indicates degraded services in response body

**When to Use**: Load balancer health checks, pre-deployment validation

### Startup Diagnostics Endpoint
**Endpoint**: `GET /api/diagnostics/initialization-order`

**Purpose**: View detailed initialization sequence and timing

**Expected Response**:
```json
{
  "initializationOrder": [
    {
      "service": "logging",
      "timestamp": 1000,
      "duration": 45,
      "status": "initialized"
    },
    {
      "service": "database",
      "timestamp": 1045,
      "duration": 234,
      "status": "initialized"
    },
    {
      "service": "ffmpeg",
      "timestamp": 1279,
      "duration": 156,
      "status": "initialized"
    }
  ],
  "totalDuration": 1435,
  "criticalFailures": 0,
  "warnings": 1
}
```

---

## Diagnostic Procedures

### Procedure 1: Verify Application is Starting
```bash
# Check if process is running
ps aux | grep Aura.Api

# Check if port is listening (default 5000/5001)
netstat -tuln | grep 5000

# Check health endpoints
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

### Procedure 2: Analyze Startup Logs
```bash
# View real-time startup logs
tail -f ~/.local/share/aura/logs/startup.log

# Search for errors
grep "ERROR" ~/.local/share/aura/logs/startup.log

# Search for initialization failures
grep "failed to initialize" ~/.local/share/aura/logs/startup.log

# View structured initialization sequence
grep "Initializing:" ~/.local/share/aura/logs/startup.log
```

### Procedure 3: Check Dependency Status
```bash
# Run dependency check script
./scripts/check-deps.sh

# Check via API
curl http://localhost:5000/api/dependencies/check | jq

# Force rescan
curl -X POST http://localhost:5000/api/dependencies/rescan
```

### Procedure 4: Investigate Slow Startup
```bash
# View initialization timing
curl http://localhost:5000/api/diagnostics/initialization-order | jq '.initializationOrder[] | select(.duration > 1000)'

# Check database connectivity
curl http://localhost:5000/api/diagnostics/database-status

# Check filesystem permissions
ls -la ~/.local/share/aura/
```

### Procedure 5: Diagnose Failed Startup
```bash
# View last 100 lines of startup log
tail -n 100 ~/.local/share/aura/logs/startup.log

# Search for critical failures
grep "CRITICAL" ~/.local/share/aura/logs/startup.log

# Check if database is reachable
# (connection string from appsettings.json)

# Verify required directories exist
ls -la ~/.local/share/aura/
ls -la ~/Documents/Aura\ Projects/
ls -la ~/Videos/Aura/
```

---

## Common Issues

### Issue 1: Application Stuck in "Starting" State

**Symptoms**:
- `/health/ready` returns 503 for >30 seconds
- Logs show initialization steps but never complete

**Diagnosis**:
```bash
# Check initialization order
curl http://localhost:5000/api/diagnostics/initialization-order

# Look for hanging step
tail -f ~/.local/share/aura/logs/startup.log | grep "Initializing:"
```

**Common Causes**:
1. Database connection timeout
2. FFmpeg detection hanging
3. Network timeout for AI service check

**Resolution**:
```bash
# For database: Verify connection string
# For FFmpeg: Kill any hung ffmpeg processes
pkill ffmpeg

# For network: Check firewall/proxy settings
# Restart application
```

### Issue 2: FFmpeg Not Detected

**Symptoms**:
- First-run wizard shows FFmpeg as "Not Installed"
- Video export fails with "FFmpeg not found"

**Diagnosis**:
```bash
# Check if FFmpeg is in PATH
which ffmpeg
ffmpeg -version

# Check application's FFmpeg detection
curl http://localhost:5000/api/dependencies/check | jq '.ffmpeg'
```

**Resolution**:
1. Install FFmpeg via auto-installer in Downloads/Engines
2. Or manually add to PATH
3. Or use "Attach Existing" to specify custom path
4. Rescan dependencies: `POST /api/dependencies/rescan`

### Issue 3: Database Connection Failure

**Symptoms**:
- Application exits immediately after start
- Logs show "Database Connectivity failed"

**Diagnosis**:
```bash
# Check logs
grep "Database Connectivity" ~/.local/share/aura/logs/startup.log

# Verify SQLite database file
ls -la ~/.local/share/aura/aura.db
```

**Resolution**:
1. Ensure data directory exists and is writable
2. Check disk space availability
3. Verify SQLite runtime is available
4. Delete corrupted database (will recreate on next start)

### Issue 4: Startup Takes >30 Seconds

**Symptoms**:
- Application eventually starts but very slowly
- Users experience long wait times

**Diagnosis**:
```bash
# Check initialization timing
curl http://localhost:5000/api/diagnostics/initialization-order | jq '.initializationOrder[] | {service, duration}'
```

**Common Causes**:
1. Network timeout trying to reach AI services
2. Large number of projects/media causing database query slowness
3. Disk I/O bottleneck

**Resolution**:
1. Increase timeouts in configuration
2. Optimize database indexes
3. Consider async initialization for non-critical services

### Issue 5: Services in Degraded Mode

**Symptoms**:
- `/health/ready` returns 200 but with degraded services
- Some features unavailable

**Diagnosis**:
```bash
# Check health details
curl http://localhost:5000/health/ready | jq

# Check specific service status
curl http://localhost:5000/api/dependencies/check | jq
```

**Resolution**:
- This is expected behavior when optional dependencies are missing
- Review which services are degraded and install missing dependencies
- Or accept degraded mode and use available features only

---

## Log Analysis

### Log Format
Structured JSON logs for each initialization step:
```json
{
  "timestamp": "2025-10-27T22:00:01.234Z",
  "level": "Information",
  "service": "ffmpeg",
  "step": "detection",
  "status": "success",
  "duration": 156,
  "details": {
    "version": "6.0",
    "path": "/usr/bin/ffmpeg"
  }
}
```

### Key Log Patterns

#### Successful Initialization
```
=== Service Initialization Starting ===
Initializing: Database Connectivity (Critical: True, Timeout: 30s)
✓ Database Connectivity initialized successfully in 234ms
Initializing: Required Directories (Critical: True, Timeout: 10s)
✓ Required Directories initialized successfully in 45ms
...
=== Service Initialization COMPLETE ===
Total time: 1435ms, Successful: 4/4
```

#### Critical Failure
```
Initializing: Database Connectivity (Critical: True, Timeout: 30s)
✗ CRITICAL: Database Connectivity failed to initialize (took 30001ms)
=== Service Initialization FAILED ===
Critical services failed to initialize. Application cannot start.
```

#### Graceful Degradation
```
Initializing: AI Services (Critical: False, Timeout: 10s)
⚠ AI Services failed to initialize - continuing with graceful degradation (took 10002ms)
=== Service Initialization COMPLETE ===
Some non-critical services failed. Application running in degraded mode.
```

### Log Locations

| Platform | Path |
|----------|------|
| Windows | `%LOCALAPPDATA%\Aura\logs\startup.log` |
| macOS | `~/Library/Application Support/Aura/logs/startup.log` |
| Linux | `~/.local/share/aura/logs/startup.log` |

---

## Performance Metrics

### Initialization Time Targets

| Phase | Target (ms) | Warning (ms) | Critical (ms) |
|-------|-------------|--------------|---------------|
| Logging | <50 | >100 | >500 |
| Database | <300 | >1000 | >5000 |
| Directories | <100 | >500 | >2000 |
| FFmpeg | <200 | >1000 | >5000 |
| AI Services | <500 | >2000 | >10000 |
| **Total** | **<2000** | **>5000** | **>15000** |

### Health Check Response Times

| Endpoint | Target (ms) | Warning (ms) |
|----------|-------------|--------------|
| /health/live | <10 | >50 |
| /health/ready | <50 | >200 |
| /api/diagnostics/initialization-order | <100 | >500 |

### Monitoring Recommendations

1. **Alert on**:
   - Startup time >10 seconds
   - Health check failures >3 consecutive
   - Critical service initialization failure
   - Readiness probe down >60 seconds

2. **Track Metrics**:
   - Average startup time (by platform)
   - FFmpeg detection success rate
   - Database connection success rate
   - Degraded mode frequency

3. **Dashboard Views**:
   - Initialization timeline visualization
   - Service health status matrix
   - Dependency availability trends

---

## Emergency Procedures

### Emergency Restart
```bash
# Stop application
pkill -f Aura.Api

# Clear temp files (optional)
rm -rf ~/.local/share/aura/temp/*

# Start application
./Aura.Api
```

### Reset to Default State
```bash
# Backup current state
cp -r ~/.local/share/aura ~/.local/share/aura.backup

# Clear all application data (will trigger first-run wizard)
rm -rf ~/.local/share/aura/*

# Restart application
./Aura.Api
```

### Collect Diagnostic Bundle
```bash
# Create diagnostic bundle for support
mkdir -p /tmp/aura-diagnostics
cp ~/.local/share/aura/logs/*.log /tmp/aura-diagnostics/
curl http://localhost:5000/api/diagnostics/initialization-order > /tmp/aura-diagnostics/init-order.json
curl http://localhost:5000/api/dependencies/check > /tmp/aura-diagnostics/dependencies.json
curl http://localhost:5000/health/ready > /tmp/aura-diagnostics/health.json
tar -czf aura-diagnostics-$(date +%Y%m%d-%H%M%S).tar.gz /tmp/aura-diagnostics/
```

---

## Appendix: Configuration Reference

### Timeout Configuration
Located in `appsettings.json`:
```json
{
  "Orchestration": {
    "Timeouts": {
      "DatabaseConnection": 30,
      "FFmpegDetection": 10,
      "AIServices": 10,
      "DirectorySetup": 10
    },
    "Retries": {
      "DatabaseConnection": 3,
      "ExponentialBackoffMs": 1000
    }
  }
}
```

### Feature Flags
```json
{
  "Features": {
    "EnableAIServices": true,
    "RequireFFmpegOnStartup": false,
    "AllowDegradedMode": true
  }
}
```

---

## Contact and Escalation

For issues not covered in this runbook:
1. Check [Troubleshooting Guide](archive/docs-old/TROUBLESHOOTING_INTEGRATION_TESTS.md)
2. Review [Production Readiness Checklist](../PRODUCTION_READINESS_CHECKLIST.md)
3. Collect diagnostic bundle (see Emergency Procedures)
4. File issue on GitHub with diagnostic bundle attached
