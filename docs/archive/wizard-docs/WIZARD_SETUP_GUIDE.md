# Wizard Setup Guide

## Overview

Aura Video Studio uses a **single, streamlined 6-step setup wizard** called `FirstRunWizard`. This guide explains the wizard flow and how to troubleshoot common issues.

## The Setup Wizard

**Location**: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`

**This is the ONLY wizard that should be used.** Previous implementations (like `SetupWizard.tsx`) have been removed to avoid confusion.

### 6-Step Flow

1. **Welcome** - Introduction to Aura Video Studio
2. **FFmpeg Check** - Quick status check showing if FFmpeg is already installed
3. **FFmpeg Install** - Guided installation with managed FFmpeg or manual path configuration
4. **Provider Configuration** - Set up at least one LLM provider (or use offline mode)
5. **Workspace Setup** - Configure default save locations
6. **Complete** - Summary and "Start Creating Videos" button to finish setup

### Key Features

- **Circuit Breaker Clearing**: On mount, the wizard clears circuit breaker state to prevent false "backend not running" errors
- **Auto-Save Progress**: Your progress is saved automatically to both backend and localStorage
- **Resume Capability**: If you exit mid-setup, you'll see a dialog asking if you want to resume or start fresh
- **Backend Status Check**: Shows warnings only when backend is actually unreachable (not on initial load)
- **Automatic Status Check**: Step 2 automatically checks FFmpeg status when you enter the step
- **Clear Error Messages**: Network errors now include step-by-step instructions for starting the backend
- **Simplified UI**: Clean, focused interface with no duplicate buttons or redundant actions
- **Clear Loading States**: All actions show progress indicators and prevent double-clicks

### UI Design Principles (Latest Update - This PR)

- **Step 2 (FFmpeg Check)**: Shows simple status with single "Check Again" button
- **Step 3 (FFmpeg Install)**: 
  - Automatically checks FFmpeg status when you enter the step
  - One section for managed install (FFmpegDependencyCard)
  - One section for manual configuration
  - No duplicate Re-scan buttons
  - Clear error messages with backend startup instructions
- **Step 6 (Complete)**: Single "Start Creating Videos" button with loading spinner to prevent double-clicks
- **Backend Banner**: Shows once per step, dismissible, and automatically hides when backend is reachable

## Backend Requirement

**CRITICAL**: The Aura backend server MUST be running for Step 2 (FFmpeg Install) to work properly.

### How to Start the Backend

1. Open a terminal in the project root directory
2. Run: `dotnet run --project Aura.Api`
3. Wait for the message: **"Application started. Press Ctrl+C to shut down."**
4. Keep this terminal window open while using the wizard

The backend typically starts on `http://localhost:5005` (configurable in `appsettings.json`).

### What Happens Without Backend

If the backend is not running when you reach Step 2:
- Status check will fail with clear error message
- Error message includes step-by-step instructions to start backend
- "Re-scan" and "Install Managed FFmpeg" buttons will not work
- You must start the backend and then click "Check Again" or refresh

## Common Issues

### Issue 1: Step 2 shows "FFmpeg Not Ready" or "Backend unreachable" (NEW - Fixed in This PR)

**Symptoms**:
- Step 2 shows "FFmpeg (Video Encoding) – Not Ready"
- Error message: "Backend server is not running"
- "Re-scan" button doesn't work
- "Install Managed FFmpeg" button is disabled or doesn't work

**Root Cause** (Now Fixed):
- Previous versions didn't automatically check FFmpeg status when entering Step 2
- Users saw "Not Ready" because no check was performed
- Network errors didn't provide clear guidance on starting the backend

**Solution**:
1. **Ensure backend is running** (see "Backend Requirement" section above)
2. **Current version automatically checks** when you enter Step 2
3. **If you see an error**:
   - Read the error message carefully - it now includes step-by-step instructions
   - Follow the instructions to start the backend (if not running)
   - Click "Check Again" after starting the backend
   - If problem persists, check backend logs for errors

**What This PR Fixed**:
- ✅ Step 2 now automatically checks FFmpeg status on entry
- ✅ Error messages now include backend startup instructions
- ✅ Clear distinction between "backend not running" vs "FFmpeg not found"
- ✅ "Re-scan" and "Install" buttons work properly when backend is running

### Issue 2: "You have incomplete setup. Would you like to resume where you left off?"

**Cause**: You started the wizard but didn't complete it in a previous session.

**Solution**: 
- Click **"Resume Setup"** to continue from where you left off (restores state from backend)
- Click **"Start Fresh"** to begin the wizard from the beginning (clears both backend and localStorage state)
- This is normal behavior and helps you avoid losing progress

**What "Start Fresh" Does**:
1. Clears wizard progress from localStorage (`wizardProgress` key)
2. Calls backend API to reset wizard state in database (`/api/setup/wizard/reset`)
3. Resets to Step 0 (Welcome screen)
4. Preserves other app settings (API keys, workspace preferences are NOT deleted)

**Note**: If you need to completely reset the app including all settings, use the cleanup scripts instead:
- Windows: `Scripts/clean-desktop.ps1`
- Linux/Mac: See `DESKTOP_APP_GUIDE.md` for cleanup instructions

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
- [ ] "Start Fresh" clears all saved state (both localStorage and backend)
- [ ] Backend status banner only shows when backend is actually down
- [ ] Backend status banner is dismissible and doesn't reappear after dismissal
- [ ] Step 2 (FFmpeg Check) has single "Check Again" button (no Install button)
- [ ] Step 3 (FFmpeg Install) has no duplicate Re-scan buttons
- [ ] Step 6 (Complete) button shows loading spinner and is disabled during completion
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
