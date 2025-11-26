# PR 001 Testing Guide

## Manual Testing for Health Endpoint and Deterministic Readiness Logging

### Backend Testing

#### 1. Start the Backend
```bash
cd Aura.Api
dotnet run
```

#### 2. Verify Deterministic Startup Message
**Expected output in console:**
```
Application started. Press Ctrl+C to shut down. Listening on: http://localhost:5005
```

#### 3. Test Health Endpoint
Open browser or use curl:

**Browser:** Navigate to `http://localhost:5005/healthz/simple`

**curl:**
```bash
curl http://localhost:5005/healthz/simple
```

**Expected JSON response:**
```json
{
  "status": "ok",
  "service": "Aura.Api",
  "version": "1.0.0.0",
  "timestampUtc": "2025-11-21T04:21:04.067Z"
}
```

### Frontend Testing

#### 1. Start the Frontend
```bash
cd Aura.Web
npm install
npm run dev
```

#### 2. Test Setup Wizard with Backend

**Scenario 1: Backend Running (Happy Path)**
1. Ensure backend is running on port 5005
2. Navigate to Setup Wizard (first run)
3. Proceed to Step 2 (FFmpeg Install)
4. **Expected:** Backend ping succeeds, FFmpeg check runs automatically
5. **Expected:** No "Backend Server Not Reachable" error

**Scenario 2: Backend Not Running (Error Path)**
1. Stop the backend
2. Navigate to Setup Wizard (first run)
3. Proceed to Step 2 (FFmpeg Install)
4. **Expected:** Backend ping retries 3 times with delays (1s, 2s, 3s)
5. **Expected:** After 3 failed attempts, appropriate error handling
6. Check browser console for logs:
   - `[FirstRunWizard] Backend ping attempt 1/3 failed`
   - `[FirstRunWizard] Backend ping attempt 2/3 failed`
   - `[FirstRunWizard] Backend ping attempt 3/3 failed`
   - `[FirstRunWizard] Backend not reachable after 3 attempts`

**Scenario 3: Backend Starts During Retry**
1. Navigate to Step 2 in wizard (backend stopped)
2. Wait for first ping attempt to fail
3. Start the backend before 3rd retry
4. **Expected:** FFmpeg check triggers once backend responds

#### 3. Test pingBackend API Method

Open browser console and test directly:
```javascript
// Import the setupApi (if not already imported)
import { setupApi } from './services/api/setupApi';

// Test with backend running
const result1 = await setupApi.pingBackend();
console.log(result1); // Expected: { ok: true, details: "OK" }

// Stop backend, then test again
const result2 = await setupApi.pingBackend();
console.log(result2); // Expected: { ok: false, details: "Network Error" or similar }
```

### Automated Tests

The test file `Aura.Tests/Integration/HealthEndpointTests.cs` contains 4 test cases:

1. **HealthzSimple_ReturnsOk** - Verifies endpoint returns 200 OK
2. **HealthzSimple_ReturnsExpectedStructure** - Validates JSON structure
3. **HealthzSimple_TimestampIsValidIsoFormat** - Verifies timestamp format
4. **HealthzSimple_ContentTypeIsJson** - Checks Content-Type header

**Run tests:**
```bash
cd Aura.Tests
dotnet test --filter "FullyQualifiedName~HealthEndpointTests"
```

**Note:** There are pre-existing test failures in the test suite unrelated to PR 001. These failures exist in `VideoOrchestratorValidationTests.cs` and are not introduced by this PR.

## Success Criteria

✅ Backend logs clear startup message with listening URLs  
✅ `/healthz/simple` endpoint returns valid JSON with expected fields  
✅ Frontend `pingBackend()` method returns correct status  
✅ Setup wizard retries backend ping with exponential backoff  
✅ No false "Backend Server Not Reachable" errors when backend is up  
✅ Clear console logs for debugging retry behavior  

## Known Issues

- Pre-existing test failures in `VideoOrchestratorValidationTests.cs` (unrelated to PR 001)
- Full frontend test suite requires `npm install` in CI environment
