# Shutdown Troubleshooting Guide

This document provides detailed troubleshooting steps for investigating and resolving issues where Aura Video Studio processes do not terminate properly when the application is closed.

## Table of Contents

- [Overview](#overview)
- [Quick Diagnostics](#quick-diagnostics)
- [Common Issues](#common-issues)
- [Advanced Diagnostics](#advanced-diagnostics)
- [Log Analysis](#log-analysis)
- [Process Investigation](#process-investigation)
- [Resolution Steps](#resolution-steps)

---

## Overview

### Normal Shutdown Flow

When you close Aura Video Studio, the following should happen:

1. **Electron Main Process** catches window close event
2. **Shutdown Orchestrator (Electron)** initiates shutdown sequence
3. **Backend API** receives shutdown request via `/api/system/shutdown`
4. **Shutdown Orchestrator (API)** terminates child processes and SSE connections
5. **IHostApplicationLifetime.StopApplication()** is called
6. **All BackgroundService instances** receive cancellation signal
7. **Each service** stops gracefully within timeout
8. **Host framework** disposes all services
9. **Process exits** cleanly

Total time: 1-3 seconds normally, up to 30 seconds maximum with active work.

### When Things Go Wrong

**Symptoms**:
- Multiple "AI-Powered Video Generation Studio" processes remain in Task Manager
- ".NET Host" processes visible after app close
- "Aura.Api.exe" or "dotnet.exe" processes don't exit
- FFmpeg processes remain running
- Port conflicts when trying to restart the app

**Root Causes**:
- Background services not responding to cancellation
- Deadlocks or blocking operations
- Long-running tasks not checking CancellationToken
- Timers or periodic tasks not disposed
- Child processes not tracked or terminated
- Hosted services not implementing StopAsync properly

---

## Quick Diagnostics

### Step 1: Check Task Manager

Open Task Manager (Ctrl+Shift+Esc) and look for:

**Expected Processes (While Running)**:
- `Aura Video Studio.exe` (Electron main)
- `Aura.Api.exe` or `dotnet.exe` (Backend API)
- Optionally: `ffmpeg.exe` (during renders)

**Problematic Processes (After Close)**:
- Any process above still visible after 30 seconds
- `.NET Host` processes
- Orphaned `ffmpeg.exe` processes

### Step 2: Check Logs

**Location**: `%APPDATA%\Aura\logs\`

**Files to check**:
- `aura-api-<date>.log` - Backend API logs
- `errors-<date>.log` - Error logs
- Electron logs in `%APPDATA%\aura-video-studio\logs\`

**What to look for**:
```powershell
# Search for shutdown events
Get-Content "$env:APPDATA\Aura\logs\aura-api-*.log" | Select-String "Shutdown"

# Check for errors during shutdown
Get-Content "$env:APPDATA\Aura\logs\errors-*.log" | Select-String "Shutdown" -Context 5,5

# Find process IDs
Get-Content "$env:APPDATA\Aura\logs\aura-api-*.log" | Select-String "PID:"
```

### Step 3: Verify Port Availability

If you get "port already in use" errors:

```powershell
# Check what's using port 5005 (default API port)
netstat -ano | findstr :5005

# Kill the process using the port (replace <PID> with actual PID)
taskkill /F /PID <PID>
```

---

## Common Issues

### Issue 1: Backend API Not Stopping

**Symptoms**:
- `Aura.Api.exe` remains in Task Manager
- Logs show shutdown initiated but never completed
- Port conflicts on restart

**Diagnosis**:
Look for these log patterns:
```
[INFO] Initiating graceful shutdown (Force: false, PID: 12345)
[INFO] Calling IHostApplicationLifetime.StopApplication()...
// Nothing after this - shutdown hung
```

**Possible Causes**:
1. Background service not responding to cancellation
2. Deadlock in service disposal
3. Long-running task blocking shutdown
4. Database connection not closing
5. External API call hanging

**Resolution**:
1. Check which service is hanging:
   ```powershell
   # Look for services that started StopAsync but never completed
   Get-Content "$env:APPDATA\Aura\logs\aura-api-*.log" | Select-String "StopAsync"
   ```

2. If a specific service is identified, check its implementation:
   - Does it properly handle CancellationToken?
   - Are there any blocking operations?
   - Is it waiting for a Task that never completes?

3. Increase timeout if legitimate work is being interrupted:
   - Edit `Aura.Api/Program.cs`, find `HostOptions.ShutdownTimeout`
   - Default is 30 seconds, can be increased if needed

### Issue 2: FFmpeg Processes Not Terminating

**Symptoms**:
- `ffmpeg.exe` processes remain after app close
- Multiple FFmpeg instances accumulate over time

**Diagnosis**:
```powershell
# Check for orphaned FFmpeg processes
Get-Process ffmpeg -ErrorAction SilentlyContinue

# Check if they're tracked in logs
Get-Content "$env:APPDATA\Aura\logs\aura-api-*.log" | Select-String "FFmpeg.*PID"
```

**Possible Causes**:
1. FFmpeg ProcessManager not registered
2. Processes spawned but not tracked
3. KillAllProcessesAsync not being called
4. Process.Kill failing silently

**Resolution**:
1. Verify FFmpeg ProcessManager is registered:
   ```csharp
   // In Program.cs, should have:
   builder.Services.AddSingleton<IProcessManager, ProcessManager>();
   ```

2. Check ShutdownOrchestrator has ProcessManager:
   ```csharp
   // In Program.cs:
   builder.Services.AddSingleton<ShutdownOrchestrator>(sp => {
       var logger = sp.GetRequiredService<ILogger<ShutdownOrchestrator>>();
       var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
       var processManager = sp.GetService<IProcessManager>();
       return new ShutdownOrchestrator(logger, lifetime, processManager);
   });
   ```

3. Verify processes are being registered when spawned:
   ```csharp
   // When spawning FFmpeg:
   _processManager?.RegisterProcess(process.Id, jobId);
   ```

### Issue 3: Electron Not Exiting

**Symptoms**:
- Main Electron window closes but process remains
- "Aura Video Studio.exe" in Task Manager

**Diagnosis**:
Check Electron logs for shutdown sequence.

**Possible Causes**:
1. Backend not responding to shutdown request
2. Electron waiting for backend indefinitely
3. Event listeners preventing exit
4. GPU process hanging

**Resolution**:
1. Check if backend shutdown is timing out:
   - Increase `ABSOLUTE_TIMEOUT_MS` in `main.js`
   - Currently 5 seconds, can increase to 10 seconds if needed

2. Verify failsafe is working:
   ```javascript
   // In main.js, should have:
   process.exit(0); // In finally block
   ```

### Issue 4: Timer Services Not Disposing

**Symptoms**:
- Process exits slowly (taking 10+ seconds)
- Memory not released properly

**Diagnosis**:
Check which services have timers:
```
- ProcessManager (FFmpeg)
- ScriptCacheService
- ResourceTracker
- OllamaDetectionService
- MemoryPressureManager
- EnhancedMemoryMonitor
- GenerationStateManager
```

**Resolution**:
Verify each service implements IDisposable and disposes timer:
```csharp
public void Dispose()
{
    if (_disposed) return;
    _timer?.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
}
```

---

## Advanced Diagnostics

### Using Process Explorer

Download Process Explorer from Microsoft Sysinternals.

**Steps**:
1. Run Process Explorer as Administrator
2. Close Aura Video Studio
3. Look for:
   - Orphaned processes
   - Process tree relationships
   - Open handles (files, ports, mutexes)
   - Thread states

**Key Information**:
- Check "Handles" tab for open files or ports
- Check "Threads" tab for blocked threads
- Check "Environment" tab for configuration

### Using WinDbg

For advanced debugging of hung processes:

```
# Attach to process
windbg -p <PID>

# Show all threads
~*k

# Look for blocked threads
!analyze -v

# Check for deadlocks
!dlk
```

### Enabling Debug Logging

**Backend API**:
Edit `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    }
  }
}
```

**Electron**:
Set environment variable:
```
set DEBUG=*
```

---

## Log Analysis

### Successful Shutdown Pattern

```
[12:34:56 INFO] Initiating graceful shutdown (Force: false, PID: 12345)
[12:34:56 INFO] Step 1/4 Complete: SSE Notification: No active connections (Elapsed: 5ms)
[12:34:56 INFO] Step 2/4 Complete: SSE Closure: No connections to close (Elapsed: 1ms)
[12:34:56 INFO] Step 3/4 Complete: FFmpeg Termination: No FFmpeg processes (Elapsed: 2ms)
[12:34:56 INFO] Step 4/4 Complete: Process Termination: No child processes (Elapsed: 1ms)
[12:34:56 INFO] Graceful shutdown completed successfully (Total: 235ms)
[12:34:56 INFO] Calling IHostApplicationLifetime.StopApplication()...
[12:34:56 INFO] === Application Shutdown Initiated ===
[12:34:56 INFO] BackgroundJobProcessorService StopAsync called
[12:34:57 INFO] BackgroundJobProcessorService stopping due to cancellation
[12:34:57 INFO] BackgroundJobProcessorService stopped
[12:34:57 INFO] BackgroundJobProcessorService StopAsync completed
[12:34:57 INFO] === Application Shutdown Complete (PID: 12345) ===
```

### Failed Shutdown Patterns

**Pattern 1: Service Not Responding**
```
[12:34:56 INFO] BackgroundJobProcessorService StopAsync called
// Never logs "StopAsync completed"
// Process hangs here
```
**Action**: Service is blocking in StopAsync or ExecuteAsync loop not checking cancellation.

**Pattern 2: FFmpeg Hung**
```
[12:34:56 INFO] Step 3/4: Terminating 2 FFmpeg processes (Force: false)
// Long delay (>5 seconds)
[12:35:02 WARN] Process 12346 did not exit gracefully, force killing
```
**Action**: FFmpeg not responding to SIGTERM, needs SIGKILL. Check what FFmpeg is doing.

**Pattern 3: Timeout**
```
[12:34:56 INFO] Initiating graceful shutdown
[12:35:26 ERROR] Shutdown hard timeout exceeded
[12:35:26 ERROR] Activating failsafe process termination...
```
**Action**: Shutdown took longer than 30 seconds. Check which step hung.

---

## Process Investigation

### Finding Parent-Child Relationships

```powershell
# Show process tree
Get-CimInstance Win32_Process | Where-Object {$_.Name -like "*Aura*" -or $_.Name -like "*dotnet*" -or $_.Name -like "*ffmpeg*"} | Select-Object ProcessId, ParentProcessId, Name, CommandLine | Format-Table

# Kill entire process tree
taskkill /PID <parent_pid> /T /F
```

### Checking Process Handles

```powershell
# Using handle.exe from Sysinternals
handle.exe -p <PID>

# Check what files are open
handle.exe -p <PID> | findstr /i "aura"

# Check what ports are open
handle.exe -p <PID> | findstr /i "port"
```

---

## Resolution Steps

### Immediate Fix (Manual Cleanup)

```powershell
# Kill all Aura-related processes
taskkill /F /IM "Aura.Api.exe" 2>$null
taskkill /F /IM "ffmpeg.exe" 2>$null
taskkill /F /IM "Aura Video Studio.exe" 2>$null

# Verify all gone
Get-Process | Where-Object {$_.Name -like "*Aura*" -or $_.Name -like "*ffmpeg*"}
```

### Permanent Fix (Code Changes)

1. **Identify the blocking service** from logs
2. **Review the service implementation**:
   - Does ExecuteAsync check `stoppingToken.IsCancellationRequested`?
   - Are all async operations using the cancellation token?
   - Is StopAsync calling base.StopAsync?
3. **Add timeout to blocking operations**:
   ```csharp
   await SomeLongOperation(stoppingToken).WaitAsync(TimeSpan.FromSeconds(5), stoppingToken);
   ```
4. **Add logging** to track shutdown progress:
   ```csharp
   _logger.LogInformation("MyService StopAsync called");
   await base.StopAsync(cancellationToken);
   _logger.LogInformation("MyService StopAsync completed");
   ```

### Testing the Fix

1. Build and run the application
2. Start a video generation job
3. Close the application while job is running
4. Verify in Task Manager: all processes gone within 30 seconds
5. Check logs: shutdown sequence completed
6. Restart application: no port conflicts

---

## Prevention

### Best Practices for Services

**Always check CancellationToken**:
```csharp
while (!stoppingToken.IsCancellationRequested)
{
    // Work
    await Task.Delay(_interval, stoppingToken);
}
```

**Implement proper disposal**:
```csharp
public void Dispose()
{
    _timer?.Dispose();
    _httpClient?.Dispose();
    // Dispose all resources
}
```

**Override StopAsync for logging**:
```csharp
public override async Task StopAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("MyService stopping...");
    await base.StopAsync(cancellationToken);
    _logger.LogInformation("MyService stopped");
}
```

**Use timeouts for external calls**:
```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(TimeSpan.FromSeconds(30));
await ExternalApiCall(cts.Token);
```

### Monitoring

**Add health checks** for shutdown:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("shutdown_ready", () => {
        // Check if app is ready to shutdown
        return HealthCheckResult.Healthy();
    });
```

**Track shutdown metrics**:
- Time to shutdown
- Services that timed out
- Processes left behind
- Port conflicts on restart

---

## Getting Help

If you've followed this guide and still have issues:

1. **Collect diagnostic bundle**:
   - All log files from `%APPDATA%\Aura\logs\`
   - Task Manager screenshot showing lingering processes
   - Process tree output
   - Steps to reproduce

2. **Create GitHub issue** with:
   - Clear title: "Shutdown Issue: [brief description]"
   - Environment: Windows version, .NET version, Node version
   - Logs attached
   - Steps to reproduce

3. **Include this information**:
   - When did it start happening?
   - Does it happen every time or intermittently?
   - Are there any active jobs when closing?
   - What's the last successful log entry?
   - What process(es) remain?

---

**Last Updated**: December 2024
**Version**: 1.0
**Related Docs**: 
- `test-shutdown-manually.md` - Manual testing procedures
- `PROCESS_MODEL.md` - Process architecture
- `DESKTOP_APP_GUIDE.md` - Desktop app documentation
