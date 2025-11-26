# Wizard Debug Quick Reference Card

## Console Patterns to Look For

### ✅ Successful Wizard Completion

```
[FirstRunWizard] 🚀 Starting onboarding completion...
[FirstRunWizard] ✅ Step 1/3 complete: Configuration validated
[Wizard Persistence] Completing wizard in backend (attempt 1/3)
[Wizard Persistence] ✅ Wizard completed successfully in backend
[FirstRunWizard] ✅ Step 2/3 complete: Backend wizard completion saved
[FirstRunWizard] ✅ Step 3/3 complete: Local first-run status updated
[FirstRunWizard] 🎉 ALL STEPS COMPLETE - Wizard finished successfully!
```

### ❌ Wizard Loop (Backend Failure)

```
[FirstRunWizard] Step 2/3: Marking wizard as complete in backend...
[Wizard Persistence] Completing wizard in backend (attempt 1/3)
[Wizard Persistence] ❌ Attempt 1/3 failed: Network Error
[Wizard Persistence] Retrying in 2000ms...
[Wizard Persistence] ❌ Attempt 3/3 failed: Network Error
[Wizard Persistence] ❌ All retry attempts exhausted
[FirstRunWizard] ❌ Step 2/3 FAILED: Backend completion failed
```

### ⚠️ Backend Slow Startup

```
[BackendStatusBanner] Auto-retrying backend health check (attempt 15/60)
[BackendStatusBanner] ⚠️ Backend health check failed (attempt 15/60)
[BackendStatusBanner] ✅ Backend health check passed (attempt 22/60, responseTime: 234ms)
```

---

## Quick Diagnosis Commands

### Check if wizard completed in database

```powershell
# Windows PowerShell
sqlite3 "$env:LOCALAPPDATA\aura-video-studio\aura.db" "SELECT user_id, completed, completed_at FROM user_setup;"
```

### Check localStorage status

```javascript
// Browser DevTools Console (F12)
console.log(
  "First run completed:",
  localStorage.getItem("hasCompletedFirstRun")
);
console.log("Seen onboarding:", localStorage.getItem("hasSeenOnboarding"));
```

### View recent backend logs

```powershell
Get-Content "$env:LOCALAPPDATA\aura-video-studio\logs\*.log" -Tail 50
```

### Reset wizard for testing

```powershell
# Full reset
Stop-Process -Name "Aura Video Studio" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\aura-video-studio\aura.db" -Force
# Then clear localStorage in browser DevTools
```

---

## Common Error Patterns

### Pattern 1: "Failed to mark wizard as complete in backend"

**Symptom**: Console shows `❌ All retry attempts exhausted`  
**Cause**: Backend not responding or crashed  
**Fix**:

1. Check if backend process is running
2. Check backend logs for errors
3. Verify database file permissions
4. Try restarting app

### Pattern 2: "Backend Server Not Reachable"

**Symptom**: Error banner shows immediately after launch  
**Cause**: Backend taking longer than 60s to start  
**Fix**:

1. Wait full 60 seconds
2. Check for firewall blocking localhost:5005
3. Verify .NET runtime is installed
4. Check if port 5005 is already in use

### Pattern 3: Wizard shows again after restart

**Symptom**: Completed wizard but it reappears  
**Cause**: Database didn't save completion status  
**Fix**:

1. Check database: `SELECT * FROM user_setup WHERE completed = 1`
2. Verify localStorage: `localStorage.getItem('hasCompletedFirstRun')`
3. Check backend logs for database errors
4. Verify database file write permissions

### Pattern 4: Settings lost after wizard

**Symptom**: API keys/paths not saved  
**Cause**: `wizard_state` field not populated  
**Fix**:

1. Check database: `SELECT wizard_state FROM user_setup`
2. Should see JSON with `apiKeys`, `workspacePreferences`
3. If empty, wizard completion API call failed
4. Check backend logs for serialization errors

---

## Health Check Decision Tree

```
Is backend responding to /healthz/simple?
├─ YES → Health check passes ✅
│   └─ Wizard can proceed normally
│
└─ NO → Check why
    ├─ Port 5005 not listening?
    │   └─ Backend process crashed or didn't start
    │       └─ Check backend logs
    │
    ├─ Firewall blocking?
    │   └─ Add firewall exception for Aura
    │
    ├─ Backend starting slowly?
    │   └─ Wait up to 60 seconds
    │       └─ If still failing after 60s, backend has issue
    │
    └─ Network adapter disabled?
        └─ Check Windows network settings
```

---

## Wizard State Machine

```
Step 0: Welcome
  ↓ (Click "Get Started")
Step 1: FFmpeg Check
  ↓ (Auto-check, then "Next")
Step 2: FFmpeg Install
  ↓ (Skip or install, then "Next")
Step 3: Provider Configuration
  ↓ (Configure 1+ provider, then "Next")
Step 4: Workspace Setup
  ↓ (Set paths, preview theme, then "Next")
Step 5: Complete
  ↓ (Click "Save")
  └─ Call setupApi.completeSetup()         [Step 1/3]
      ├─ SUCCESS → Continue
      └─ FAIL → Show error, stay on Step 5
  └─ Call completeWizardInBackend()        [Step 2/3]
      ├─ SUCCESS → Continue
      └─ FAIL (after 3 retries) → Show error, stay on Step 5
  └─ Call markFirstRunCompleted()          [Step 3/3]
      └─ SUCCESS → Navigate to dashboard
```

---

## Critical Files Reference

| File                      | Purpose                    | Key Function                          |
| ------------------------- | -------------------------- | ------------------------------------- |
| `FirstRunWizard.tsx`      | Main wizard UI             | `completeOnboarding()`                |
| `onboarding.ts`           | Wizard state & persistence | `completeWizardInBackend()`           |
| `setupApi.ts`             | Backend API client         | `completeSetup()`, `completeWizard()` |
| `BackendStatusBanner.tsx` | Health check UI            | `checkBackend()`                      |
| `backendHealthService.ts` | Health check logic         | `checkHealth()`                       |
| `SetupController.cs`      | Backend wizard API         | `CompleteWizard()` endpoint           |

---

## Timeout Values Reference

| Component               | Timeout         | Purpose                               |
| ----------------------- | --------------- | ------------------------------------- |
| Backend startup         | 60s             | Max time for backend process to start |
| Health check auto-retry | 60s             | Match backend startup time            |
| Individual health check | 5s              | Single HTTP request timeout           |
| App first-run check     | 60s             | Wait before showing timeout error     |
| Wizard completion retry | 3 attempts × 2s | Network error recovery                |
| Auto-save debounce      | 5s              | Prevent spam saves                    |

---

## Database Schema Quick Reference

```sql
-- User setup completion tracking
CREATE TABLE user_setup (
  id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,              -- Always "default" for now
  completed INTEGER NOT NULL,         -- 0 = incomplete, 1 = complete
  completed_at TEXT,                  -- ISO 8601 timestamp
  version TEXT,                       -- App version (e.g., "1.0.0")
  last_step INTEGER NOT NULL,         -- Last completed step (0-5)
  wizard_state TEXT,                  -- JSON with wizard data
  selected_tier TEXT,                 -- "free", "pro", etc.
  updated_at TEXT NOT NULL
);

-- Example wizard_state JSON:
{
  "mode": "free",
  "apiKeys": {
    "openai": "sk-...",
    "ollama": ""
  },
  "workspacePreferences": {
    "defaultSaveLocation": "C:\\Users\\...",
    "cacheLocation": "C:\\Users\\...",
    "autosaveInterval": 5,
    "theme": "dark"
  }
}
```

---

## Emergency Procedures

### User Trapped in Wizard Loop

```powershell
# 1. Force-complete wizard in database
$dbPath = "$env:LOCALAPPDATA\aura-video-studio\aura.db"
sqlite3 $dbPath "UPDATE user_setup SET completed = 1, completed_at = datetime('now') WHERE user_id = 'default';"

# 2. Set localStorage flag
# Run in browser DevTools:
localStorage.setItem('hasCompletedFirstRun', 'true');

# 3. Restart app
```

### Backend Won't Start

```powershell
# 1. Check if already running
Get-Process -Name "Aura.Api" -ErrorAction SilentlyContinue

# 2. Check port availability
netstat -ano | findstr :5005

# 3. Manual backend start (for debugging)
cd C:\github\aura-video-studio
dotnet run --project Aura.Api

# 4. Check logs
Get-Content "$env:LOCALAPPDATA\aura-video-studio\logs\backend-*.log" -Tail 100
```

### Complete Nuclear Reset

```powershell
# WARNING: This deletes ALL user data
Stop-Process -Name "Aura Video Studio" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\aura-video-studio" -Recurse -Force
# Clear localStorage in browser DevTools
# Restart app for fresh wizard
```

---

## Support Information Checklist

When reporting wizard issues, collect:

- [ ] Console logs (F12 → Console tab → Save as file)
- [ ] Backend logs (`%LOCALAPPDATA%\aura-video-studio\logs\*.log`)
- [ ] Database query results (`SELECT * FROM user_setup`)
- [ ] localStorage values (`hasCompletedFirstRun`, `hasSeenOnboarding`)
- [ ] App version (Help → About)
- [ ] Windows version
- [ ] Network adapter status
- [ ] Firewall/antivirus software installed
- [ ] Steps to reproduce

---

## Quick Test Procedure

```powershell
# 1. Reset
Stop-Process -Name "Aura Video Studio" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\aura-video-studio\aura.db" -Force
# Clear localStorage in DevTools

# 2. Launch
& "C:\Path\To\Aura Video Studio.exe"

# 3. Monitor console (F12)
# Look for patterns listed above

# 4. Complete wizard
# All 6 steps, click "Save"

# 5. Verify
sqlite3 "$env:LOCALAPPDATA\aura-video-studio\aura.db" "SELECT completed FROM user_setup;"
# Should return: 1

# 6. Restart
# Wizard should NOT reappear
```

---

## Performance Benchmarks

| Metric               | Fast Machine | Slow Machine  | Acceptable Range |
| -------------------- | ------------ | ------------- | ---------------- |
| Backend startup      | 5-10s        | 30-45s        | < 60s            |
| Health check success | Attempt 1-2  | Attempt 10-20 | < 60 attempts    |
| Wizard completion    | 2-3 min      | 5-8 min       | < 10 min         |
| Database write       | < 100ms      | < 500ms       | < 1s             |
| Auto-save cycle      | 5s           | 5s            | 5s (fixed)       |

---

## Version History

| Date       | Version | Changes                                                |
| ---------- | ------- | ------------------------------------------------------ |
| 2024-11-22 | 1.1.0   | Added retry logic, enhanced logging, database fallback |
| 2024-11-01 | 1.0.0   | Initial wizard implementation                          |

---

## Contact

For wizard-specific issues:

1. Check this guide first
2. Review console logs
3. Check backend logs
4. Query database
5. File issue with collected information
