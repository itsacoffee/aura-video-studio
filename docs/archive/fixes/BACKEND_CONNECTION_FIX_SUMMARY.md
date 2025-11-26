# Backend Connection Fix Summary

## Problem
Fresh builds of the portable app on Windows 11 were getting "Backend Server Not Reachable" errors even though the backend was configured to auto-start.

## Root Causes Identified

### 1. **Timing Issue in Runtime Bridge State**
- The preload script (`preload.js`) was calling `runtime:getBootstrap` synchronously at startup
- At that early stage, the backend service hadn't been fully initialized yet
- The frontend received an error state instead of the backend URL
- This caused all API calls to fail with network errors

### 2. **Health Endpoint Mismatch**
- Frontend's `backendHealthService.ts` was using `/healthz/simple` endpoint
- Electron's network contract specified `/health/live` as the health endpoint
- This inconsistency could cause health checks to fail even when the backend was running

### 3. **Insufficient Retry Timeout**
- Frontend was only retrying for 20 seconds (20 attempts × 1 second)
- Portable apps on Windows 11 can take longer on first run due to:
  - Windows Defender scanning
  - .NET JIT compilation  
  - Database initialization
  - Slow storage (especially on USB/external drives)
- Backend service has a 90-second timeout, but frontend gave up at 20 seconds

### 4. **Missing Backend URL Fallback**
- When backend contract wasn't available, the preload script returned an error object
- This error object didn't include a `backend.baseUrl` field
- The frontend couldn't connect because it had no URL to try

### 5. **Poor Logging for Portable App Scenarios**
- Limited diagnostic information for troubleshooting startup issues
- No indication of why backend might be taking longer to start

## Fixes Applied

### 1. **Early Backend URL Provision** (`main.js`)
```javascript
// BEFORE: Returned error with no backend URL
if (!backendContract) {
  return { error: "backend-contract-unavailable", ... };
}

// AFTER: Always provide backend URL from contract
if (!state && backendContract) {
  state = {
    backend: {
      baseUrl: backendContract.baseUrl,
      port: backendContract.port,
      ready: false, // Not ready yet, but URL is available
    },
    ...
  };
}
```

**Impact**: Frontend can now configure axios immediately with the correct URL, even if backend isn't started yet.

### 2. **Health Endpoint Consistency** (`backendHealthService.ts`)
```typescript
// BEFORE: Used /healthz/simple
this.healthEndpoint = '/healthz/simple';

// AFTER: Use /health/live (matches Electron network contract)
this.healthEndpoint = '/health/live';
```

**Impact**: Health checks now use the correct endpoint that the backend is guaranteed to have ready first.

### 3. **Extended Retry Timeouts** (`BackendStatusBanner.tsx`)
```typescript
// BEFORE: 20 attempts × 1 second = 20 seconds
const maxAutoRetries = 20;

// AFTER: 90 attempts × 1 second = 90 seconds (matches backend timeout)
const maxAutoRetries = 90;
```

```typescript
// BEFORE: 5000ms timeout, 500ms retry delay
timeout: 5000,
retryDelay: 500,

// AFTER: 8000ms timeout, 1000ms retry delay
timeout: 8000,
retryDelay: 1000,
```

**Impact**: Gives slower systems enough time to start, with clearer progress feedback.

### 4. **Better User Feedback** (`BackendStatusBanner.tsx`)
```typescript
// Added contextual messages based on retry count
{retryCountRef.current > 30 && (
  <Text>
    <strong>This is taking longer than usual.</strong> On first launch, Windows Defender
    scanning and .NET compilation may slow startup. Please wait...
  </Text>
)}
```

**Impact**: Users understand why startup might be slow on first launch.

### 5. **Enhanced Logging** (`backend-service.js`)
```javascript
console.log("[BackendService] =".repeat(30));
console.log("[BackendService] BACKEND STARTUP CONFIGURATION");
console.log("[BackendService]   URL:", this.baseUrl);
console.log("[BackendService]   Port:", this.port);
console.log("[BackendService]   Frontend should connect to:", `${this.baseUrl}/health/live`);
// ... more diagnostic info
```

**Impact**: Easier troubleshooting with comprehensive startup logs.

## Testing Instructions

### Prerequisite: Rebuild the Desktop App

```powershell
# Clean build (recommended)
Remove-Item -Recurse -Force .\Aura.Desktop\dist\, .\Aura.Desktop\out\ -ErrorAction SilentlyContinue
cd Aura.Desktop
npm run build:prod

# Or quick rebuild
cd Aura.Desktop
npm run build
```

### Test 1: Fresh Install Scenario

1. **Extract portable app to a fresh location** (simulate first-time user)
   ```powershell
   # If you have a portable build ZIP:
   Expand-Archive Aura-Video-Studio-Portable.zip -DestinationPath C:\Temp\AuraTest
   cd C:\Temp\AuraTest
   .\Aura-Video-Studio.exe
   ```

2. **Watch for:**
   - Splash screen should show "Starting backend server..." message
   - Check console logs (F12 in DevTools if enabled) for:
     - `[Preload] ✓ Backend URL confirmed: http://127.0.0.1:5005`
     - `[BackendStatusBanner] ✅ Backend health check passed`
   - Banner should show retry progress if backend takes time to start
   - App should eventually load without "Backend Server Not Reachable" error

### Test 2: Diagnostic Script

Run the diagnostic script to check connectivity:

```powershell
node test-backend-connectivity.js
```

**Expected Output:**
```
PORT CONNECTIVITY:    ✓ PASS
HTTP HEALTH CHECK:    ✓ PASS  
BACKEND EXECUTABLE:   ✓ FOUND
.NET RUNTIME:         ✓ OK
```

### Test 3: Manual Backend Testing

```powershell
# Start backend manually to verify it works
cd Aura.Api
dotnet run

# In another terminal, test health endpoint
curl http://127.0.0.1:5005/health/live

# Expected response:
# {"status":"Healthy","totalDuration":"00:00:00.0001234"}
```

### Test 4: Slow System Simulation

Test on a slow system or under load:

1. Open Task Manager
2. Create artificial CPU load (optional: run CPU stress test)
3. Launch Aura
4. Verify it waits up to 90 seconds and shows helpful messages

### Test 5: DevTools Console Verification

Open DevTools (Ctrl+Shift+I or F12) and check for these logs:

```
✓ [Preload] Runtime bootstrap received: { backend: { baseUrl: "http://127.0.0.1:5005", port: 5005, ready: false } }
✓ [Preload] ✓ Backend URL confirmed: http://127.0.0.1:5005
✓ [BackendStatusBanner] ✅ Backend health check passed
```

**Red flags (these should NOT appear):**
```
✗ [Preload] ERROR: Runtime bootstrap missing backend URL!
✗ [BackendStatusBanner] ❌ Backend health check threw exception
✗ Circuit breaker is open - service unavailable
```

## Verification Checklist

- [ ] Portable app launches without "Backend Server Not Reachable" error
- [ ] First launch completes within 90 seconds on slow systems
- [ ] Helpful progress messages appear during long startups
- [ ] Backend logs show clear startup configuration
- [ ] DevTools console shows backend URL is received
- [ ] Health checks use `/health/live` endpoint
- [ ] Diagnostic script passes all tests

## Troubleshooting

### If backend still doesn't connect:

1. **Run diagnostic script:**
   ```powershell
   node test-backend-connectivity.js
   ```

2. **Check Electron logs:**
   - Windows: `%APPDATA%\Aura Video Studio\logs\`
   - Look for `startup-*.log` files

3. **Verify .NET is installed:**
   ```powershell
   dotnet --version
   # Should show 8.0 or higher
   ```

4. **Check Windows Firewall:**
   - The app should prompt for firewall access on first run
   - If blocked, manually allow in Windows Defender Firewall settings

5. **Check port availability:**
   ```powershell
   netstat -ano | findstr :5005
   ```
   - Should be empty before starting Aura
   - Should show LISTENING after Aura starts

## Performance Expectations

- **First Launch (Cold Start):** 30-90 seconds on slow systems
- **Subsequent Launches:** 5-15 seconds
- **Fast Systems/SSD:** 3-10 seconds

Factors affecting startup time:
- Windows Defender real-time scanning
- .NET JIT compilation (first run)
- Database creation (first run)
- Storage speed (HDD vs SSD)
- System resources (CPU, RAM)

## Related Files Changed

1. `Aura.Desktop/electron/main.js` - Runtime bridge state initialization
2. `Aura.Web/src/services/backendHealthService.ts` - Health endpoint
3. `Aura.Web/src/components/Onboarding/BackendStatusBanner.tsx` - Retry logic & UI
4. `Aura.Desktop/electron/backend-service.js` - Logging improvements
5. `test-backend-connectivity.js` - New diagnostic script

## Additional Notes

- The fixes maintain backward compatibility with existing installations
- No breaking changes to API contracts
- Frontend gracefully handles slow backend starts
- Better observability through enhanced logging

## Next Steps

If issues persist after these fixes:
1. Run the diagnostic script and share output
2. Check `%APPDATA%\Aura Video Studio\logs\startup-*.log`
3. Verify Windows Firewall isn't blocking the app
4. Ensure .NET 8.0 SDK/Runtime is installed correctly
5. Try running backend manually: `cd Aura.Api && dotnet run`

