# Smoke Test Scripts

This directory contains smoke test scripts for Aura Video Studio that perform critical sanity checks to ensure the application starts cleanly.

## Overview

The smoke test scripts perform the following checks:

1. **Build the solution** - Ensures code compiles without errors
2. **Run core tests** - Validates unit/integration tests pass
3. **Start Aura.Api** - Launches the backend API in background
4. **Probe endpoints** - Tests critical API endpoints:
   - `GET /api/healthz` - Health check
   - `GET /api/capabilities` - Hardware detection
   - `GET /api/queue` - Jobs/artifacts endpoint
5. **Monitor logs** - Watches logs for exceptions over 30 seconds

The scripts exit with code 0 on success and non-zero on failure, making them suitable for CI/CD pipelines.

## Scripts

### Windows: `start_and_probe.ps1`

PowerShell script for Windows environments.

**Usage:**
```powershell
# Run full smoke test
.\scripts\smoke\start_and_probe.ps1

# Skip build (if already built)
.\scripts\smoke\start_and_probe.ps1 -SkipBuild

# Skip tests (faster checks)
.\scripts\smoke\start_and_probe.ps1 -SkipTests

# Custom timeouts
.\scripts\smoke\start_and_probe.ps1 -TestTimeout 180 -ProbeTimeout 60
```

**Options:**
- `-SkipBuild` - Skip the build step
- `-SkipTests` - Skip running tests
- `-TestTimeout N` - Timeout for test execution in seconds (default: 120)
- `-ProbeTimeout N` - Timeout for log monitoring in seconds (default: 30)

### Linux/macOS: `start_and_probe.sh`

Bash script for Linux and macOS environments.

**Usage:**
```bash
# Run full smoke test
./scripts/smoke/start_and_probe.sh

# Skip build (if already built)
./scripts/smoke/start_and_probe.sh --skip-build

# Skip tests (faster checks)
./scripts/smoke/start_and_probe.sh --skip-tests

# Custom timeouts
./scripts/smoke/start_and_probe.sh --test-timeout 180 --probe-timeout 60
```

**Options:**
- `--skip-build` - Skip the build step
- `--skip-tests` - Skip running tests
- `--test-timeout N` - Timeout for test execution in seconds (default: 120)
- `--probe-timeout N` - Timeout for log monitoring in seconds (default: 30)
- `-h, --help` - Show help message

## Prerequisites

- .NET 8 SDK installed
- Repository must be built at least once (unless using `--skip-build`)
- Port 5005 should be available (or API already running on that port)

## Output

The scripts provide colored output indicating:
- ✓ Success (green)
- → Info (cyan)
- ⚠ Warning (yellow)
- ✗ Error (red)

Example output:
```
╔═══════════════════════════════════════════════════════╗
║   Aura Video Studio - Startup Smoke Check            ║
╚═══════════════════════════════════════════════════════╝

→ [1/5] Building solution...
✓ Build completed successfully
→ [2/5] Running core tests (Aura.Tests)...
✓ Tests passed: 609 tests
→ [3/5] Starting Aura.Api in background...
✓ Aura.Api started (PID: 12345)
→ Waiting for API to become ready...
✓ API is ready after 3 seconds
→ [4/5] Probing critical endpoints...
✓ GET /api/healthz - OK
  Server time: 2025-10-12T17:27:50.090Z
✓ GET /api/capabilities - OK
  Hardware Tier: D
  CPU Cores: 4
✓ GET /api/queue - OK
  Jobs in queue: 0
→ [5/5] Monitoring logs for 30 seconds...
✓ Monitoring complete
  Duration: 30 seconds
  Exceptions found: 0
  Errors found: 0
  Correlation IDs seen: 5
    - abc123...
    - def456...
✓ No critical exceptions found
→ Stopping Aura.Api (PID: 12345)...

╔═══════════════════════════════════════════════════════╗
✓ ║   Smoke test PASSED - Application is healthy         ║
╚═══════════════════════════════════════════════════════╝
```

## Troubleshooting

**Port already in use:**
The scripts will warn if port 5005 is already in use but will attempt to continue. Stop any existing Aura.Api processes before running.

**API doesn't start:**
Check the smoke logs in `logs/smoke-stdout.log` and `logs/smoke-stderr.log` for startup errors.

**Tests fail:**
Run `dotnet test` directly to see detailed test output.

**Build fails:**
Run `dotnet build` directly to see detailed build errors.

## Integration with CI/CD

These scripts are designed to be used in CI/CD pipelines:

```yaml
# Example GitHub Actions usage
- name: Run smoke tests
  run: ./scripts/smoke/start_and_probe.sh
  
# Example with custom timeouts
- name: Run smoke tests (extended)
  run: ./scripts/smoke/start_and_probe.sh --probe-timeout 60
```

The scripts automatically clean up the API process on exit, interrupt, or termination signals.
