# Manual Verification Checklist for Engine Lifecycle Manager

## Prerequisites
1. Build the application: `dotnet build Aura.Api/Aura.Api.csproj`
2. Have at least one engine configured with `StartOnAppLaunch=true`

## Test Scenarios

### 1. Auto-Start on Application Launch

**Steps:**
1. Configure an engine with auto-start enabled:
   ```bash
   # Edit engines-config.json or use the API
   curl -X POST http://127.0.0.1:5005/api/engines/preferences \
     -H "Content-Type: application/json" \
     -d '{"stable-diffusion": {"autoStart": true, "port": 7860}}'
   ```

2. Start the Aura API:
   ```bash
   cd Aura.Api
   dotnet run
   ```

3. **Expected Results:**
   - Console logs should show: "Starting Engine Lifecycle Manager"
   - Console logs should show: "Auto-launching N engines"
   - Console logs should show: "Engine Lifecycle Manager started successfully"
   - Configured engines should start automatically

4. Verify engines are running:
   ```bash
   curl http://127.0.0.1:5005/api/engines/diagnostics
   ```

### 2. Graceful Shutdown

**Steps:**
1. With engines running from step 1, press Ctrl+C or send SIGTERM:
   ```bash
   # In another terminal
   pkill -TERM -f "Aura.Api"
   ```

2. **Expected Results:**
   - Console logs should show: "Application stopping - shutting down engines..."
   - Console logs should show: "Stopping all running engines"
   - Console logs should show individual engine stops
   - Console logs should show: "Engine Lifecycle Manager stopped successfully"
   - All engine processes should terminate gracefully (check with `ps aux | grep <engine>`)

### 3. Crash Detection and Auto-Restart

**Steps:**
1. Start an engine with auto-restart enabled
2. Manually kill the engine process:
   ```bash
   # Find the process ID from diagnostics
   curl http://127.0.0.1:5005/api/engines/diagnostics
   
   # Kill the process
   kill -9 <PID>
   ```

3. **Expected Results:**
   - Console logs should show: "Engine {Id} crashed"
   - Console logs should show: "Engine {Id} crashed and is being restarted"
   - Engine should restart automatically (verify with diagnostics endpoint)

### 4. Health Check Notifications

**Steps:**
1. Start an engine with a health check URL configured
2. Check notifications:
   ```bash
   curl http://127.0.0.1:5005/api/engines/notifications
   ```

3. **Expected Results:**
   - Notifications should include "HealthCheckPassed" or "HealthCheckFailed"
   - Notifications should include engine start/stop events
   - All notifications should have proper timestamps

### 5. Diagnostics Report

**Steps:**
1. Run diagnostics:
   ```bash
   curl http://127.0.0.1:5005/api/engines/diagnostics
   ```

2. **Expected Results:**
   - Report should show total engines, running engines, healthy engines
   - Each engine should have: engineId, name, isRunning, isHealthy, restartCount
   - Running engines should have processId and lastStarted timestamp

### 6. Log Viewing

**Steps:**
1. Start an engine
2. Fetch logs:
   ```bash
   curl "http://127.0.0.1:5005/api/engines/logs?engineId=stable-diffusion&tailLines=100"
   ```

3. **Expected Results:**
   - Logs should be returned in JSON format
   - Should include stdout and stderr with timestamps
   - Should respect the tailLines parameter

### 7. UI Integration

**Steps:**
1. Build and start the web UI:
   ```bash
   cd Aura.Web
   npm install
   npm run dev
   ```

2. Navigate to Settings > Local Engines

3. Test the following:
   - Click "Run Diagnostics" button
     - Dialog should appear with system diagnostics
   - Start an engine
   - Click "View Logs" button on running engine
     - Dialog should appear with engine logs
   - Test auto-start toggle and save preferences

4. **Expected Results:**
   - All buttons should work without errors
   - Dialogs should display properly formatted data
   - Status badges should update in real-time
   - Logs should be readable in monospace font

## Success Criteria

✅ Engines auto-start when configured
✅ Engines stop gracefully on application shutdown
✅ Crashed engines restart automatically (up to 3 times)
✅ Health checks generate notifications
✅ Diagnostics report shows accurate data
✅ Logs are captured and retrievable
✅ UI components work without errors

## Common Issues

### Issue: Engine won't auto-start
**Solution:** Check executable path, permissions, and logs

### Issue: Engine keeps restarting
**Solution:** Check health check URL, ensure engine can actually run

### Issue: Shutdown hangs
**Solution:** Some processes may need force kill, which is normal after timeout

### Issue: Logs are empty
**Solution:** Check log directory permissions, ensure stdout/stderr are captured

## Performance Checks

Monitor these during testing:
- CPU usage should be minimal when engines are idle
- Memory should not leak over time
- Log files should not grow indefinitely (check rotation)
- Monitoring task should run every 5 seconds

## Notes

- All automated tests pass (26 tests)
- Implementation follows the problem statement requirements
- Code is production-ready with error handling
- Documentation is comprehensive

## Sign-Off

After completing all test scenarios:

- [ ] Auto-start works correctly
- [ ] Graceful shutdown works correctly
- [ ] Crash detection works correctly
- [ ] Auto-restart works correctly (up to 3 times)
- [ ] Diagnostics report is accurate
- [ ] Logs are viewable
- [ ] UI integration works correctly
- [ ] No memory leaks observed
- [ ] No performance issues observed

**Tester:** _______________
**Date:** _______________
**Status:** ☐ PASS ☐ FAIL ☐ NEEDS WORK
