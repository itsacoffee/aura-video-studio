# Health Check Endpoints

Aura Video Studio provides comprehensive health check endpoints to monitor application readiness and liveness using ASP.NET Core Health Checks framework.

## Endpoints

### `/health/live`

**Liveness probe** - checks if the application process is running.

**Purpose:** Container orchestration liveness probe (Kubernetes, Docker)

**Response:** Always returns 200 OK if the process is alive. No dependency checks are performed.

```json
{
  "status": "healthy"
}
```

### `/health/ready`

**Readiness probe** - checks if the application is ready to serve requests by validating all critical dependencies.

**Purpose:**
- Container orchestration readiness probe (Kubernetes, Docker)
- Load balancer health checks
- Monitoring systems (Prometheus, Datadog, etc.)

**Response:**
- 200 OK when all critical checks pass (status: "healthy")
- 200 OK when some non-critical checks fail (status: "degraded")  
- 503 Service Unavailable when critical checks fail (status: "unhealthy")

```json
{
  "status": "healthy",
  "timestamp": "2025-10-28T04:00:00.000Z",
  "checks": [
    {
      "name": "Dependencies",
      "status": "healthy",
      "description": "All dependencies available",
      "data": {
        "ffmpeg_available": true,
        "ffmpeg_path": "/usr/bin/ffmpeg",
        "ffmpeg_version": "ffmpeg version 6.0",
        "platform": "Unix",
        "gpu_available": true,
        "nvenc_available": true,
        "gpu_vendor": "NVIDIA",
        "gpu_model": "GeForce RTX 3080",
        "gpu_vram_gb": 10,
        "tier": "High"
      }
    },
    {
      "name": "DiskSpace",
      "status": "healthy",
      "description": "Sufficient disk space: 125.50 GB free on C:\\",
      "data": {
        "free_gb": 125.50,
        "total_gb": 500.00,
        "threshold_gb": 1.0,
        "critical_gb": 0.5,
        "drive": "C:\\"
      }
    }
  ]
}
```

**Degraded Example:**
```json
{
  "status": "degraded",
  "timestamp": "2025-10-28T04:00:00.000Z",
  "checks": [
    {
      "name": "Dependencies",
      "status": "degraded",
      "description": "FFmpeg not available - video rendering disabled",
      "data": {
        "ffmpeg_available": false,
        "platform": "Unix",
        "gpu_available": false,
        "tier": "Low"
      }
    },
    {
      "name": "DiskSpace",
      "status": "healthy",
      "description": "Sufficient disk space: 10.25 GB free",
      "data": {
        "free_gb": 10.25,
        "total_gb": 500.00
      }
    }
  ]
}
```

## Health Checks

### Dependencies Check

Validates critical dependencies for video rendering:
- **FFmpeg availability** - Required for video composition
- **GPU detection** - Detects NVIDIA GPUs with NVENC support
- **System tier** - Determines hardware tier (Low/Medium/High)

**Critical:** FFmpeg is required for core functionality. Returns `degraded` if missing.

**Data provided:**
- `ffmpeg_available` - Whether FFmpeg is found
- `ffmpeg_path` - Path to FFmpeg executable (if available)
- `ffmpeg_version` - FFmpeg version string (if available)
- `platform` - Operating system platform
- `gpu_available` - Whether a GPU is detected
- `nvenc_available` - Whether NVIDIA NVENC is available
- `gpu_vendor` - GPU vendor (e.g., "NVIDIA", "AMD", "Intel")
- `gpu_model` - GPU model name
- `gpu_vram_gb` - GPU VRAM in GB
- `tier` - System hardware tier

### Disk Space Check

Validates available disk space on the drive where the application is running.

**Critical:** Returns `unhealthy` if below critical threshold (0.5 GB default)

**Thresholds:**
- **Warning** (degraded): < 1.0 GB free (configurable)
- **Critical** (unhealthy): < 0.5 GB free (configurable)

**Data provided:**
- `free_gb` - Free space in GB (rounded to 2 decimals)
- `total_gb` - Total disk size in GB
- `threshold_gb` - Warning threshold
- `critical_gb` - Critical threshold
- `drive` - Drive name/path

## Status Levels

### healthy
All checks passed. Application is fully operational and ready to serve requests.

### degraded
Some non-critical checks failed. Application can operate with reduced functionality.
- Example: FFmpeg not available, but API endpoints still work

### unhealthy  
Critical checks failed. Application cannot serve requests properly.
- Example: Disk space critically low (< 0.5 GB)
- Returns HTTP 503 Service Unavailable

## Configuration

### Health Check Settings

Configure disk space thresholds in `appsettings.json`:

```json
{
  "HealthChecks": {
    "DiskSpaceThresholdGB": 1.0,
    "DiskSpaceCriticalGB": 0.5,
    "Timeout": "00:00:10"
  }
}
```

**Settings:**
- `DiskSpaceThresholdGB` - Warning threshold in GB (default: 1.0)
- `DiskSpaceCriticalGB` - Critical threshold in GB (default: 0.5)
- `Timeout` - Health check timeout (default: 10 seconds)

### Port Configuration

The API listens on `http://127.0.0.1:5005` by default.

Override with environment variables:
- `AURA_API_URL` - Full URL (e.g., `http://localhost:8080`)
- `ASPNETCORE_URLS` - ASP.NET Core standard (e.g., `http://0.0.0.0:8080`)

## Integration with Monitoring

### Kubernetes/Docker

Use health endpoints for container orchestration:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5005
  initialDelaySeconds: 10
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5005
  initialDelaySeconds: 5
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
  successThreshold: 1
```

**Liveness probe:**
- Detects if the application has crashed or is deadlocked
- Kubernetes will restart the pod if it fails
- Should be simple and fast (no dependency checks)

**Readiness probe:**
- Detects if the application is ready to serve traffic
- Kubernetes will remove pod from service endpoints if it fails
- Can include dependency checks

### Load Balancers

Configure health checks to use `/health/ready`:
- **Healthy threshold:** 2 consecutive successful checks
- **Unhealthy threshold:** 2 consecutive failed checks  
- **Timeout:** 5 seconds
- **Interval:** 10 seconds
- **Expected status:** 200 OK

### Monitoring Systems

#### Prometheus

Monitor health check status:
```promql
# Alert when service is not healthy
up{job="aura-api", health_status!="healthy"} == 1
```

#### Datadog

Configure synthetic monitoring:
```yaml
- name: "Aura API Health Check"
  request:
    url: "http://aura-api:5005/health/ready"
  assertions:
    - type: statusCode
      operator: is
      target: 200
    - type: body
      operator: contains
      target: '"status":"healthy"'
```

## Troubleshooting

### FFmpeg Not Found

**Symptom:** `/health/ready` returns `degraded` with "FFmpeg not available"

**Solution:**
1. Install FFmpeg using the Download Center in the web UI
2. Or manually install FFmpeg and ensure it's in your PATH
3. Or configure `FFmpeg:ExecutablePath` in appsettings.json

**Check attempted paths in response data to see where the application looked.**

### Disk Space Low

**Symptom:** `/health/ready` returns `degraded` or `unhealthy` for disk space

**Solution:**
1. Free up disk space by deleting old render outputs
2. Clean temporary files: `%TEMP%` (Windows) or `/tmp` (Linux/Mac)
3. Move the application to a drive with more space
4. Adjust thresholds in configuration if needed

### Health Check Timeout

**Symptom:** Health check takes too long or times out

**Solution:**
1. Check if FFmpeg detection is slow (network path, slow disk)
2. Increase timeout in configuration: `HealthChecks:Timeout`
3. Check system resources (CPU, disk I/O)

### Service Returns 503 on /health/ready

**Symptom:** Readiness probe returns 503 Service Unavailable

**Cause:** Critical health checks are failing (status: "unhealthy")

**Solution:**
1. Check the response body for which checks are failing
2. Address the failing checks (usually FFmpeg or disk space)
3. Service will automatically recover when checks pass

## Testing Health Checks

### Manual Testing

```bash
# Test liveness (should always return 200 if app is running)
curl http://localhost:5005/health/live

# Test readiness (returns status and details)
curl http://localhost:5005/health/ready | jq

# Test readiness with verbose output
curl -v http://localhost:5005/health/ready
```

### Expected Responses

**Healthy response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-28T04:00:00Z",
  "checks": [...]
}
```

**Degraded response (200 OK):**
```json
{
  "status": "degraded",
  "timestamp": "2025-10-28T04:00:00Z",
  "checks": [...]
}
```

**Unhealthy response (503 Service Unavailable):**
```json
{
  "status": "unhealthy",
  "timestamp": "2025-10-28T04:00:00Z",
  "checks": [...]
}
```

## Rate Limiting

**Note:** Health check endpoints are exempt from rate limiting to ensure monitoring systems can check health frequently without being blocked.

See [Rate Limiting Documentation](rate-limits.md) for details on API rate limits.

## Legacy Compatibility

### /api/health/live and /api/health/ready

The custom health check service endpoints at `/api/health/live` and `/api/health/ready` are still available for backward compatibility but may be deprecated in future versions. Use the new ASP.NET Core health check endpoints at `/health/live` and `/health/ready` instead.

### /healthz

The legacy `/healthz` endpoint is maintained for backward compatibility:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-28T04:00:00Z"
}
```

Use `/health/live` for new integrations.
