# Manual Test Verification for PR 372 Continuation

## Purpose
Verify the fixes from PR 372 continuation addressing:
1. Circular dependency bug in Program.cs
2. Cancelled token bug in SseService.cs
3. Shutdown behavior with active SSE connections
4. Shutdown sequence logging integrity
5. Rapid restart scenarios

## Prerequisites
- Aura Video Studio built in Release mode
- .NET 8 SDK installed
- Windows 11 (or compatible OS)

## Test Scenarios

### Test 1: Normal Shutdown with No Active Connections

**Steps:**
1. Start the Aura.Api application
2. Wait for application to fully start (check logs for "Application started successfully")
3. Close the application (Ctrl+C in console or close window)

**Expected Results:**
- ✅ Application stops within 2 seconds
- ✅ Log shows: "Application stopping - shutting down gracefully"
- ✅ Log shows: "Graceful shutdown completed successfully"
- ✅ Log shows: "Application host stopped, flushing logs..."
- ✅ No error logs during shutdown
- ✅ Process terminates completely (verify in Task Manager)

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 2: Shutdown with Active SSE Connection

**Steps:**
1. Start the Aura.Api application
2. Open a SSE connection (e.g., via browser or curl to `/api/jobs/{id}/events`)
3. While SSE connection is active, close the application

**Expected Results:**
- ✅ Application detects active SSE connection
- ✅ Shutdown sequence includes: "Notifying N active SSE connections of shutdown"
- ✅ SSE connection receives error notification (if client is monitoring)
- ✅ Application stops within 3 seconds despite active connection
- ✅ Log shows: "Closed N connections"
- ✅ No hanging processes in Task Manager

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 3: Shutdown with Multiple Active SSE Connections

**Steps:**
1. Start the Aura.Api application
2. Open 5 simultaneous SSE connections
3. Close the application while all connections are active

**Expected Results:**
- ✅ Log shows: "Notifying 5 active SSE connections of shutdown"
- ✅ All connections receive shutdown notification
- ✅ Application stops within 3 seconds
- ✅ Log shows: "Closed 5 connections"
- ✅ No orphaned processes

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 4: Rapid Restart Scenario

**Steps:**
1. Start the Aura.Api application
2. Wait 2 seconds for startup
3. Close the application (Ctrl+C)
4. Immediately start the application again (within 1 second of closing)
5. Repeat steps 3-4 three more times

**Expected Results:**
- ✅ Each restart succeeds without "port in use" errors
- ✅ Each shutdown completes in under 2 seconds
- ✅ No lingering processes between restarts
- ✅ No memory leaks (check Task Manager between restarts)
- ✅ Logs show consistent shutdown/startup sequence
- ✅ Final application starts successfully

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 5: Shutdown During Dependency Scan

**Steps:**
1. Start the Aura.Api application
2. Immediately close the application (within 5 seconds, during startup scan)

**Expected Results:**
- ✅ Application responds to shutdown signal immediately
- ✅ Log shows: "Dependency scan cancelled due to application shutdown"
- ✅ No exceptions or errors related to cancelled scan
- ✅ Application stops within 2 seconds
- ✅ Clean shutdown sequence completes

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 6: Shutdown Sequence Logging Verification

**Steps:**
1. Start the Aura.Api application with logging set to Information level
2. Perform normal shutdown

**Expected Results:**
- ✅ Log contains in order:
  1. "Application stopping - shutting down gracefully"
  2. "Initiating graceful shutdown (Force: false, PID: XXXX)"
  3. "Step 1/4 Complete: Notified N connections"
  4. "Step 2/4 Complete: Closed N connections"
  5. "Step 3/4 Complete: Terminated N FFmpeg process(es)"
  6. "Step 4/4 Complete: Terminated N process(es)"
  7. "Graceful shutdown completed successfully (Total: XXXXms)"
  8. "Calling IHostApplicationLifetime.StopApplication()..."
  9. "StopApplication() called - host shutdown initiated"
  10. "Application host stopped, flushing logs..."
  11. "Application stopped successfully"
- ✅ All shutdown timing logs show < 2000ms total

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 7: Force Shutdown Mode

**Steps:**
1. Start the Aura.Api application
2. Trigger force shutdown (if available via API or gracefully shutdown with force flag)

**Expected Results:**
- ✅ Application stops in under 500ms
- ✅ Log shows: "Initiating graceful shutdown (Force: true, PID: XXXX)"
- ✅ Minimal delays in shutdown sequence
- ✅ Process terminates immediately

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 8: Shutdown with Background Tasks

**Steps:**
1. Start the Aura.Api application
2. Start a video generation job (if available)
3. While job is running, close the application

**Expected Results:**
- ✅ Application detects running job/task
- ✅ Graceful cancellation of background work
- ✅ FFmpeg processes are terminated cleanly
- ✅ Application stops within 3 seconds
- ✅ No zombie processes left behind

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 9: Verify No Circular Dependency Issues

**Steps:**
1. Start the Aura.Api application
2. Let it run for 30 seconds
3. Perform graceful shutdown

**Expected Results:**
- ✅ No premature shutdown triggers
- ✅ Application runs normally until shutdown requested
- ✅ Shutdown completes successfully
- ✅ No unexpected OperationCanceledException in logs
- ✅ ApplicationStopping token only fires during shutdown

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

### Test 10: SSE Error Notification Delivery

**Steps:**
1. Start the Aura.Api application
2. Connect SSE client to job progress endpoint
3. Trigger operation cancellation (user-initiated or timeout)
4. Monitor SSE client for error event

**Expected Results:**
- ✅ SSE client receives "event: error" message
- ✅ Error message contains "Operation cancelled"
- ✅ Error is received before connection closes
- ✅ Client can parse and display error message
- ✅ No exceptions in server logs about sending error

**Pass/Fail:** ___________

**Notes:**
_____________________________________

---

## Summary

**Total Tests:** 10  
**Passed:** _____ / 10  
**Failed:** _____ / 10

**Overall Assessment:** ___________

**Critical Issues Found:**
_____________________________________

**Recommendations:**
_____________________________________

**Tested By:** _____________________  
**Date:** _____________________  
**Environment:** _____________________

---

## Automated Test Results Reference

For automated test coverage, see:
- `Aura.Tests/Services/SseServiceShutdownTests.cs` - 8 tests
- `Aura.Tests/Services/ShutdownWithSseTests.cs` - 10 tests  
- `Aura.Tests/Services/RapidRestartTests.cs` - 8 tests
- `Aura.Tests/ShutdownOrchestratorTests.cs` - 11 existing tests

**All 37 automated tests passing ✅**
