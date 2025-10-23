# Health Check Endpoints

Aura Video Studio provides comprehensive health check endpoints to monitor application readiness and liveness.

## Endpoints

### `/api/health/live`

**Liveness probe** - checks if the application is running.

**Response:** Always returns 200 OK if the process is alive.

```json
{
  "status": "healthy",
  "checks": [
    {
      "name": "Application",
      "status": "healthy",
      "message": "Application is running",
      "details": null
    }
  ],
  "errors": []
}
```

### `/api/health/ready`

**Readiness probe** - checks if the application is ready to serve requests.

**Response:** 
- 200 OK when all critical checks pass (status: "healthy")
- 200 OK when some non-critical checks fail (status: "degraded")
- 503 Service Unavailable when critical checks fail (status: "unhealthy")

```json
{
  "status": "unhealthy",
  "checks": [
    {
      "name": "FFmpeg",
      "status": "unhealthy",
      "message": "FFmpeg not found in any of 3 candidate locations",
      "details": {
        "attemptedPaths": [
          "/path/to/search/location1",
          "/path/to/search/location2",
          "/path/to/search/location3"
        ]
      }
    },
    {
      "name": "TempDirectory",
      "status": "healthy",
      "message": "Temp directory is writable: /tmp/",
      "details": {
        "path": "/tmp/"
      }
    },
    {
      "name": "ProviderRegistry",
      "status": "healthy",
      "message": "Provider registry initialized",
      "details": {
        "toolsDirectory": "/path/to/tools",
        "auraDataDirectory": "/path/to/aura/data"
      }
    },
    {
      "name": "PortAvailability",
      "status": "healthy",
      "message": "Port 5005 is available or being used by this instance",
      "details": {
        "port": 5005,
        "inUse": false
      }
    }
  ],
  "errors": [
    "FFmpeg not found in any of 3 candidate locations"
  ]
}
```

## JSON Contract

### HealthCheckResponse

```typescript
interface HealthCheckResponse {
  status: "healthy" | "degraded" | "unhealthy";
  checks: SubCheckResult[];
  errors: string[];
}
```

### SubCheckResult

```typescript
interface SubCheckResult {
  name: string;
  status: "healthy" | "degraded" | "unhealthy";
  message?: string;
  details?: { [key: string]: any };
}
```

## Sub-Checks

### FFmpeg

Checks if FFmpeg is installed and accessible.

**Critical:** Yes - video rendering cannot work without FFmpeg

**Details provided:**
- `path` - FFmpeg executable path
- `version` - FFmpeg version string
- `attemptedPaths` - Paths checked for FFmpeg (on failure)

### TempDirectory

Checks if the system temp directory is writable.

**Critical:** Yes - application needs temp directory for intermediate files

**Details provided:**
- `path` - Temp directory path

### ProviderRegistry

Checks if provider registry directories exist and are accessible.

**Critical:** No - can be degraded if directories don't exist but will be created on demand

**Details provided:**
- `toolsDirectory` - Tools installation directory
- `auraDataDirectory` - Application data directory
- `exists` - Whether directory exists (on failure)

### PortAvailability

Checks if the configured port is available.

**Critical:** No - port may be in use by this instance

**Details provided:**
- `port` - Port number being checked
- `inUse` - Whether port is in use

## Status Levels

### healthy
All checks passed. Application is fully operational.

### degraded
Some non-critical checks failed. Application can operate with reduced functionality.

### unhealthy
Critical checks failed. Application cannot serve requests properly.

## Configuration

### Port Configuration

The API listens on `http://127.0.0.1:5005` by default.

Override with environment variables:
- `AURA_API_URL` - Full URL (e.g., `http://localhost:8080`)
- `ASPNETCORE_URLS` - ASP.NET Core standard (e.g., `http://0.0.0.0:8080`)

### Startup Validation

The application performs fail-fast validation on startup. If critical checks fail:
1. Validation errors are logged clearly
2. Application exits with code 1
3. No partial startup or degraded service

Validated on startup:
- Critical directories can be created
- Temp directory is writable
- Port configuration is valid

## Troubleshooting

### FFmpeg Not Found

**Symptom:** `/api/health/ready` returns 503 with "FFmpeg not found"

**Solution:**
1. Install FFmpeg using the Download Center in the web UI
2. Or manually install FFmpeg and ensure it's in your PATH
3. Or place FFmpeg in the Tools directory

**Details:** Check the `attemptedPaths` array in the response to see where the application looked for FFmpeg.

### Temp Directory Not Writable

**Symptom:** Startup fails with "Temp directory is not writable"

**Solution:**
1. Check temp directory permissions: `/tmp` (Linux/Mac) or `%TEMP%` (Windows)
2. Ensure disk is not full
3. Check user permissions

### Port Already In Use

**Symptom:** Startup fails with "Address already in use"

**Solution:**
1. Check if another instance of Aura is running
2. Use a different port via `AURA_API_URL` environment variable
3. Stop the conflicting application

### Provider Registry Degraded

**Symptom:** `/api/health/ready` shows ProviderRegistry as degraded

**Solution:**
This is usually not critical. Directories will be created on first use. If persistent:
1. Check directory permissions
2. Ensure parent directory is writable
3. Check disk space

## Integration with Monitoring

### Kubernetes/Docker

Use health endpoints for container orchestration:

```yaml
livenessProbe:
  httpGet:
    path: /api/health/live
    port: 5005
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 5005
  initialDelaySeconds: 5
  periodSeconds: 10
```

### Load Balancers

Configure health checks to use `/api/health/ready`:
- Healthy threshold: 2 consecutive successful checks
- Unhealthy threshold: 2 consecutive failed checks
- Timeout: 5 seconds
- Interval: 10 seconds

## Development

### Adding New Sub-Checks

1. Add check logic to `HealthCheckService.cs`
2. Return a `SubCheckResult` with appropriate status
3. Add the check to `CheckReadinessAsync` method
4. Write unit tests for the new check
5. Update this documentation

### Testing Health Checks

```bash
# Test liveness
curl http://localhost:5005/api/health/live

# Test readiness
curl http://localhost:5005/api/health/ready

# Test with verbose output
curl -v http://localhost:5005/api/health/ready
```
