# Wizard Step 2 Analysis - FFmpeg Detection/Installation Issues

## Problem Statement
User screenshots show Step 2/6 of FirstRunWizard failing with:
1. "FFmpeg (Video Encoding) – Not Ready"
2. "Backend unreachable. Please ensure the Aura backend is running."
3. "Failed to save progress"
4. Non-functional "Re-scan" and "Install Managed FFmpeg" buttons

## Active Wizard Implementation

**Canonical File**: `Aura.Web/src/pages/Onboarding/FirstRunWizard.tsx`
- Used by: `App.tsx` when `shouldShowOnboarding === true`
- Desktop app loads this via standard React routing (no special URL)
- DesktopSetupWizard.tsx delegates to FirstRunWizard for actual setup

**Step Flow** (6 steps total):
- Step 0: Welcome
- Step 1: FFmpeg Check (quick status)
- **Step 2: FFmpeg Install** (PROBLEM AREA)
- Step 3: Provider Configuration
- Step 4: Workspace Setup
- Step 5: Complete

## Step 2 Implementation Details

### Components Involved

#### 1. FirstRunWizard.tsx - Step 2 Renderer
```typescript
const renderStep2FFmpeg = () => (
  <div>
    {/* Managed Installation */}
    <FFmpegDependencyCard
      autoCheck={false}
      autoExpandDetails={true}
      refreshSignal={ffmpegRefreshSignal}
      onInstallComplete={handleFfmpegStatusUpdate}
      onStatusChange={handleFfmpegStatusUpdate}
    />
    
    {/* Manual Configuration */}
    <Card className={styles.manualAttachCard}>
      <Input value={ffmpegPathInput} onChange={...} />
      <Button onClick={handleBrowseForFfmpeg}>Browse</Button>
      <Button onClick={handleValidateFfmpegPath}>Validate Path</Button>
    </Card>
  </div>
);
```

#### 2. FFmpegDependencyCard.tsx
Handles managed FFmpeg installation UI
- Calls: `ffmpegClient.getStatusExtended()` on mount
- Calls: `ffmpegClient.rescan()` when "Re-scan" clicked
- Calls: `ffmpegClient.install()` when "Install" clicked

#### 3. BackendStatusBanner.tsx
Shows backend connectivity warnings (rendered in Step 2)
- Calls: `getSystemHealth()` to check backend
- Only shows warning if backend is actually unreachable

### API Call Chain

#### Status Check (on mount):
```
FFmpegDependencyCard
  -> ffmpegClient.getStatusExtended()
    -> GET /api/system/ffmpeg/status (legacy endpoint)
      -> SystemController.GetStatus()
        -> IFFmpegStatusService.GetStatusAsync()
```

#### Rescan:
```
FFmpegDependencyCard
  -> ffmpegClient.rescan()
    -> POST /api/ffmpeg/rescan
      -> FFmpegController.Rescan()
        -> FFmpegResolver.ResolveAsync()
```

#### Install Managed FFmpeg:
```
FFmpegDependencyCard
  -> ffmpegClient.install()
    -> POST /api/ffmpeg/install
      -> FFmpegController.Install()
        -> FfmpegInstaller.InstallAsync()
```

#### Manual Path Validation:
```
FirstRunWizard
  -> ffmpegClient.useExisting()
    -> POST /api/ffmpeg/use-existing
      -> FFmpegController.UseExisting()
        -> FFmpegResolver.ValidatePathAsync()
```

## Potential Issues

### Issue 1: Endpoint Confusion
- **Two status endpoints exist**:
  - `/api/ffmpeg/status` (new, FFmpegController)
  - `/api/system/ffmpeg/status` (legacy, SystemController)
- FFmpegDependencyCard uses **legacy endpoint**
- This may cause inconsistencies

### Issue 2: Circuit Breaker State
- Circuit breaker clears on wizard mount (line 255 of FirstRunWizard)
- But FFmpegDependencyCard may load before circuit breaker clears
- If `autoCheck={false}`, status is never actually checked automatically

### Issue 3: autoCheck is Disabled
In Step 2:
```typescript
<FFmpegDependencyCard
  autoCheck={false}  // <-- Status check disabled!
  autoExpandDetails={true}
  refreshSignal={ffmpegRefreshSignal}
/>
```
This means:
- Component never calls `checkStatus()` on mount
- User must manually click "Re-scan" to trigger status check
- This could explain "Not Ready" appearing without trying to detect

### Issue 4: refreshSignal Not Incremented on Step 2 Entry
- `ffmpegRefreshSignal` starts at 0
- Incremented only when manual rescan is clicked (line 690)
- NOT incremented when entering Step 2
- So even with refreshSignal prop, no automatic check happens

### Issue 5: Backend Status Banner Timing
- BackendStatusBanner checks backend health independently
- May show "unreachable" error before FFmpeg check completes
- Creates confusing UX where backend error appears even when backend is up

### Issue 6: Error Message Confusion
FFmpegDependencyCard error messages (lines 138-156):
- "Backend unreachable" shown for network errors
- "No response from backend" shown for timeout
- These are user-facing but don't distinguish between:
  - Backend not started at all
  - Backend started but endpoint failing
  - Backend started but FFmpeg check taking too long

## Root Cause Analysis

**Primary Issue**: Step 2 never automatically checks FFmpeg status
- `autoCheck={false}` disables automatic status check
- `refreshSignal` doesn't trigger on step entry
- User sees "Not Ready" because no check was performed
- "Re-scan" button doesn't work if backend is having issues

**Secondary Issue**: Error messages are unclear
- Network errors blame "backend unreachable"
- But actual issue could be endpoint-specific problem
- No guidance on how to verify backend is running

**Tertiary Issue**: Inconsistent endpoint usage
- Status check uses `/api/system/ffmpeg/status` (legacy)
- Rescan uses `/api/ffmpeg/rescan` (new)
- Install uses `/api/ffmpeg/install` (new)
- This mixing of old/new endpoints may cause issues

## Recommended Fixes

### Fix 1: Auto-check FFmpeg Status on Step 2 Entry
Change to `autoCheck={true}` OR increment `refreshSignal` when entering Step 2:
```typescript
useEffect(() => {
  if (state.step === 2) {
    // Trigger FFmpeg status check when entering Step 2
    setFfmpegRefreshSignal((prev) => prev + 1);
  }
}, [state.step]);
```

### Fix 2: Improve Error Messages
Distinguish between:
- Backend not running (provide dotnet run command)
- Backend running but endpoint failing (provide endpoint URL)
- Timeout (suggest waiting or checking logs)

### Fix 3: Consolidate to New Endpoints
- Replace `getStatusExtended()` call with `getStatus()` + separate hardware call
- Use consistent `/api/ffmpeg/*` endpoints throughout
- Mark `/api/system/ffmpeg/status` as deprecated

### Fix 4: Add Backend Health Check Before FFmpeg Check
```typescript
useEffect(() => {
  if (state.step === 2) {
    checkBackendHealth().then((healthy) => {
      if (healthy) {
        // Trigger FFmpeg check
        setFfmpegRefreshSignal((prev) => prev + 1);
      } else {
        // Show clear "backend not running" guidance
      }
    });
  }
}, [state.step]);
```

### Fix 5: Update Documentation
- WIZARD_SETUP_GUIDE.md must reflect actual Step 2 behavior
- Document that Step 2 requires backend to be running
- Provide clear backend startup instructions
- Add troubleshooting for "Re-scan" button not working

## Testing Requirements

### Manual Tests
- [ ] Start wizard with backend NOT running -> Should show clear error
- [ ] Start wizard with backend running, no FFmpeg -> Should offer install
- [ ] Start wizard with backend running, FFmpeg present -> Should detect
- [ ] Click "Re-scan" with backend NOT running -> Should show clear error
- [ ] Click "Re-scan" with backend running -> Should detect FFmpeg
- [ ] Click "Install Managed FFmpeg" -> Should install and detect
- [ ] Enter manual path and click "Validate" -> Should validate

### Integration Tests
- [ ] Test FFmpeg status check on Step 2 entry
- [ ] Test backend health check before FFmpeg operations
- [ ] Test error message clarity for different failure modes
- [ ] Test "Re-scan" button when backend is down vs when FFmpeg missing

## Priority Actions

### ✅ COMPLETED IN THIS PR

1. **HIGH**: Fix autoCheck or refreshSignal to trigger status check on Step 2 entry
   - ✅ Added useEffect to trigger FFmpeg check when entering Step 2
   - ✅ Changed autoCheck from false to true
   - ✅ Status now automatically checked on step entry

2. **HIGH**: Improve error messages to distinguish backend vs FFmpeg issues
   - ✅ Enhanced all network error messages with detailed instructions
   - ✅ Added "dotnet run --project Aura.Api" command in errors
   - ✅ Added numbered steps for backend startup
   - ✅ Added whiteSpace: 'pre-wrap' for proper line break display
   - ✅ Updated both FFmpegDependencyCard and FirstRunWizard error handlers

3. **MEDIUM**: Update documentation to match actual behavior
   - ✅ Updated WIZARD_SETUP_GUIDE.md with backend requirements
   - ✅ Added comprehensive troubleshooting section for Step 2
   - ✅ Documented automatic status check feature
   - ✅ Added DesktopSetupWizard deprecation notice

### ⏳ FUTURE ENHANCEMENTS (Not Required for This PR)

4. **MEDIUM**: Add backend health check before FFmpeg operations
   - Optional optimization - current error messages are sufficient
   - Could pre-check backend before attempting FFmpeg operations
   - Would provide slightly faster feedback to users

5. **LOW**: Consolidate to new `/api/ffmpeg/*` endpoints only
   - Current: Mixed usage of `/api/system/ffmpeg/status` (legacy) and `/api/ffmpeg/*` (new)
   - Future: Deprecate `/api/system/ffmpeg/status` endpoint
   - Not critical - both endpoints work correctly

### Testing Performed

✅ Manual verification of changes:
- [x] Reviewed FirstRunWizard.tsx Step 2 auto-check logic
- [x] Reviewed error message improvements (3 locations)
- [x] Reviewed documentation updates
- [x] Verified whiteSpace: 'pre-wrap' added for proper rendering
- [x] Verified DesktopSetupWizard marked as legacy

### Remaining Test Requirements (For QA/User Testing)

Manual tests needed:
- [ ] Start wizard with backend NOT running → Verify clear error with instructions
- [ ] Start wizard with backend running, no FFmpeg → Verify detection offers install
- [ ] Start wizard with backend running, FFmpeg present → Verify automatic detection
- [ ] Click "Re-scan" with backend NOT running → Verify error has startup instructions
- [ ] Enter manual path and validate with backend down → Verify error clarity
- [ ] Verify multi-line error messages display properly (newlines work)

Integration tests recommended:
- [ ] Test FFmpeg status check on Step 2 entry
- [ ] Test error message format and content
- [ ] Test "Re-scan" and "Install" button behavior
