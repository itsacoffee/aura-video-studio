# Wizard Setup Guide

## Overview

Aura Video Studio uses a **single, streamlined 6-step setup wizard** called `FirstRunWizard`. This guide explains the wizard flow and how to troubleshoot common issues.

## The Setup Wizard

**Location**: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

**This is the ONLY wizard that should be used.** Previous implementations (like `SetupWizard.tsx`) have been removed to avoid confusion.

### 6-Step Flow

1. **Welcome** - Introduction to Aura Video Studio
2. **FFmpeg Check** - Quick detection of existing FFmpeg installation
3. **FFmpeg Install** - Guided installation or manual configuration
4. **Provider Configuration** - Set up at least one LLM provider (or use offline mode)
5. **Workspace Setup** - Configure default save locations
6. **Complete** - Summary and transition to main app

### Key Features

- **Circuit Breaker Clearing**: On mount, the wizard clears circuit breaker state to prevent false "backend not running" errors
- **Auto-Save Progress**: Your progress is saved automatically to both backend and localStorage
- **Resume Capability**: If you exit mid-setup, you'll see a dialog asking if you want to resume or start fresh
- **Backend Status Check**: Shows warnings only when backend is actually unreachable (not on initial load)
- **Process Cleanup**: All FFmpeg processes are properly terminated when the app exits

## Common Issues

### Issue 1: "You have incomplete setup. Would you like to resume where you left off?"

**Cause**: You started the wizard but didn't complete it in a previous session.

**Solution**: 
- Click **"Resume Setup"** to continue from where you left off
- Click **"Start Fresh"** to begin the wizard from the beginning
- This is normal behavior and helps you avoid losing progress

### Issue 2: "Backend Server Not Running" Error

**Cause**: The Aura backend API is not running or not reachable.

**Solution**:
1. Open a terminal in the project root
2. Run: `dotnet run --project Aura.Api`
3. Wait for the message "Application started. Press Ctrl+C to shut down."
4. Click "Retry" in the wizard or refresh the page

**Note**: The wizard now only shows this error after confirming the backend is actually unreachable, not on initial load.

### Issue 3: Multiple ".NET Host" or "AI-Powered Video Generation Studio" Processes in Task Manager

**Cause**: Previous backend processes weren't properly cleaned up on shutdown.

**Solution**: 
- **Fixed in this PR** - The backend now properly terminates all FFmpeg and child processes on shutdown
- To clean up existing orphaned processes:
  1. Open Task Manager (Ctrl+Shift+Esc)
  2. Look for ".NET Host", "ffmpeg", or "AI-Powered Video Generation Studio" processes
  3. Right-click and select "End Task" for each one
  4. Restart the application - it will now clean up properly on exit

### Issue 4: Confused About Which Wizard to Use

**Cause**: Multiple wizard files existed in the codebase (SetupWizard.tsx, FirstRunWizard.tsx).

**Solution**: 
- **Fixed in this PR** - `SetupWizard.tsx` has been removed
- `FirstRunWizard.tsx` is the ONLY wizard
- It's used in:
  - `App.tsx` (for first-run experience)
  - `WelcomePage.tsx` via `ConfigurationModal` (for reconfiguration)
  - `DesktopSetupWizard.tsx` (wrapper for Electron desktop app)

## How the Wizard is Invoked

### First Run Experience
When you launch Aura for the first time:
1. `App.tsx` checks if first-run is complete (`hasCompletedFirstRun()`)
2. If not complete, it renders `FirstRunWizard` full-screen
3. On completion, it marks first-run as complete and shows the main app

### Reconfiguration
After initial setup, you can reconfigure from the Welcome page:
1. Click the "Setup Wizard" or "Reconfigure" button
2. Opens `ConfigurationModal` which wraps `FirstRunWizard`
3. Shows in a modal dialog instead of full-screen

## Backend Process Management

### How It Works
- FFmpeg processes are tracked via `ProcessManager` in `Aura.Core`
- On application shutdown, `Program.cs` calls `ProcessManager.KillAllProcessesAsync()`
- This ensures all FFmpeg child processes are terminated
- Shutdown timeout is set to 30 seconds to allow graceful cleanup

### Logging
Shutdown process is logged in detail:
```
[Info] === Application Shutdown Initiated ===
[Info] Shutdown Phase 1a: Terminating all FFmpeg processes
[Warn] Found 2 tracked FFmpeg processes to terminate
[Info] All FFmpeg processes terminated successfully
[Info] Shutdown Phase 2: Background services stopped and processes cleaned up (Elapsed: 1234ms)
```

## Developer Notes

### Circuit Breaker State
The wizard clears circuit breaker state on mount:
```typescript
PersistentCircuitBreaker.clearState();
resetCircuitBreaker();
```
This prevents false "service unavailable" errors from persisted circuit breaker state.

### Resume State Management
- Progress is saved to both backend (via `setupApi`) and localStorage
- Backend is the primary source of truth
- localStorage is used as fallback if backend is unreachable

### Wizard State Flow
```
Check saved progress → Show resume dialog (if applicable) → Run wizard → Complete → Mark first-run complete
```

## Testing the Wizard

### Manual Test Checklist
- [ ] Fresh install shows wizard on first launch
- [ ] Can complete all 6 steps successfully
- [ ] Progress is saved and resumable
- [ ] "Start Fresh" clears all saved state
- [ ] Backend status banner only shows when backend is actually down
- [ ] FFmpeg processes are cleaned up on app exit
- [ ] No orphaned processes remain in Task Manager after exit

### Testing Backend Cleanup
1. Start the backend: `dotnet run --project Aura.Api`
2. Generate a video (which spawns FFmpeg processes)
3. Stop the backend: Press Ctrl+C
4. Check Task Manager - no FFmpeg or .NET Host processes should remain

## Troubleshooting Backend Issues

If you're still seeing backend errors after these fixes:

1. **Check if backend is actually running**:
   ```bash
   curl http://localhost:5005/api/healthz
   ```
   Should return: `{"status":"Healthy",...}`

2. **Check backend logs**:
   ```bash
   # Logs are in: %LOCALAPPDATA%\Aura\logs\
   # On Windows: C:\Users\YourName\AppData\Local\Aura\logs\
   # Recent errors are in: errors-YYYYMMDD.log
   ```

3. **Clear circuit breaker state manually**:
   - Open browser DevTools (F12)
   - Go to Application → Local Storage
   - Delete any keys starting with `circuit-breaker-`
   - Refresh the page

4. **Reset wizard state completely**:
   - Open browser DevTools (F12)
   - Go to Application → Local Storage
   - Delete keys: `hasCompletedFirstRun`, `wizard-state`, `wizard-progress`
   - Refresh the page to restart wizard from step 0

## Need Help?

If you're still experiencing issues:
1. Check the [Installation Guide](INSTALLATION.md)
2. Review the [Troubleshooting Guide](TROUBLESHOOTING.md)
3. Check backend logs in `%LOCALAPPDATA%\Aura\logs\`
4. Open an issue on GitHub with:
   - Steps to reproduce
   - Error messages
   - Relevant log excerpts
   - Screenshot of the issue
