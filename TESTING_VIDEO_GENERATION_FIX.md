# Testing Video Generation Fix

## Overview
This document describes the changes made to fix video generation issues and provides testing instructions.

## Issues Fixed

### 1. Upload Provider Removed ✅
**Problem:** Preflight panel showed an "Upload Provider" dropdown that the user didn't need.

**Solution:** Removed the Upload Provider field from `Aura.Web/src/components/Wizard/ProviderSelection.tsx`.

**Test:** Navigate to Create Video → Step 3 (Preflight). Verify that only Script, TTS, and Visuals provider dropdowns are shown. Upload Provider should NOT appear.

### 2. Quick Demo Button Not Working 🔍
**Problem:** Clicking Quick Demo button resulted in "nothing happens."

**Root Cause Hypothesis:**
- Validation endpoint may have been failing silently
- API connectivity issues
- Error not being displayed to user

**Solution:**
- Added comprehensive debug logging
- Enhanced error handling to show toast notification on validation failures
- Moved console.log before state changes to ensure they always execute

**Test:**
1. Open browser console (F12)
2. Navigate to Create Video page (Step 1)
3. Click "Quick Demo (Safe)" button
4. Check console for: `[QUICK DEMO] Button clicked - starting demo generation`
5. If error occurs, verify toast notification appears with error details
6. Check for additional logs showing the failure point

### 3. Generate Video Button Not Working 🔍
**Problem:** Clicking Generate Video button resulted in "nothing happens" even when preflight showed "All systems ready."

**Root Cause Hypothesis:**
- Preflight `report.ok` field may not match what's displayed
- Button disabled logic may have bugs
- API endpoint connectivity issues

**Solution:**
- Added useEffect to log button state whenever it changes
- Enhanced preflight logging to show full report and OK status
- Added console.log at start of Generate handler

**Test:**
1. Open browser console (F12)
2. Navigate to Create Video → Step 3 (Preflight)
3. Click "Run Preflight Check"
4. Review console for `[PREFLIGHT]` logs:
   ```
   [PREFLIGHT] Starting preflight check with profile: Free-Only
   [PREFLIGHT] Preflight report received: {ok: true, ...}
   [PREFLIGHT] Report OK status: true
   ```
5. Check `[GENERATE BUTTON] State` log to see why button is enabled/disabled:
   ```
   {
     disabled: false,
     generating: false,
     hasReport: true,
     reportOk: true,
     overrideEnabled: false,
     preflightReport: {...}
   }
   ```
6. Click "Generate Video" button
7. Verify console shows: `[GENERATE VIDEO] Button clicked - starting generation`
8. If error occurs, verify toast notification appears

## Console Log Reference

### Expected Logs (Success Path)

**Quick Demo:**
```
[QUICK DEMO] Button clicked - starting demo generation
[QUICK DEMO] Validation passed
[QUICK DEMO] Demo job started: job_xxxxx
```

**Generate Video:**
```
[GENERATE VIDEO] Button clicked - starting generation
[GENERATE VIDEO] Creating job...
[GENERATE VIDEO] Job created successfully: job_xxxxx
```

**Preflight:**
```
[PREFLIGHT] Starting preflight check with profile: Free-Only
[PREFLIGHT] Preflight report received: {ok: true, stages: [...]}
[PREFLIGHT] Report OK status: true
[GENERATE BUTTON] State: {disabled: false, generating: false, hasReport: true, reportOk: true, ...}
```

### Error Logs (Failure Scenarios)

**Validation Endpoint Failed:**
```
[QUICK DEMO] Button clicked - starting demo generation
[QUICK DEMO] Validation request failed: 404 Not Found
Toast: "Validation Failed - Could not validate quick demo request. Server returned status 404"
```

**API Endpoint Not Found:**
```
[GENERATE VIDEO] Button clicked - starting generation
[GENERATE VIDEO] API request failed: {...}
Toast: "Failed to Start Generation - 404 Not Found"
```

**Preflight Failed:**
```
[PREFLIGHT] Starting preflight check with profile: Free-Only
[PREFLIGHT] Preflight check failed with status: 500
Alert: "Failed to run preflight check"
```

## Troubleshooting Guide

### Problem: Quick Demo button does nothing
**Diagnosis Steps:**
1. Check if `[QUICK DEMO] Button clicked` appears in console
   - If NO: Button click handler not firing (check browser for JS errors)
   - If YES: Proceed to step 2

2. Check for validation logs
   - If validation failed: Check if `/api/validation/brief` endpoint is accessible
   - If no validation logs: Network request may be blocked (check Network tab)

3. Check for demo creation logs
   - If failed: Check if `/api/quick/demo` endpoint is accessible
   - Check Network tab for HTTP status codes

**Common Fixes:**
- Ensure backend API is running on correct port (5005)
- Check browser console for CORS errors
- Verify `/api/validation/brief` and `/api/quick/demo` endpoints exist and are accessible

### Problem: Generate Video button disabled even with "All systems ready"
**Diagnosis Steps:**
1. Check `[GENERATE BUTTON] State` log
2. Look at values:
   - `hasReport: false` → Preflight wasn't run
   - `reportOk: false` → Preflight failed despite UI showing success
   - `generating: true` → A previous operation is still running

**Common Fixes:**
- If `reportOk: false` but UI shows "All systems ready", there's a mismatch → Review full preflight report structure in console
- If `generating: true` stuck, refresh the page to reset state
- If `hasReport: false`, click "Run Preflight Check" first

### Problem: Generate Video button click has no effect
**Diagnosis Steps:**
1. Check if `[GENERATE VIDEO] Button clicked` appears in console
   - If NO: Button is disabled, check state log
   - If YES: Proceed to step 2

2. Check for job creation logs
   - If failed: Check if `/api/jobs` endpoint is accessible
   - Check Network tab for HTTP status codes and request payload

**Common Fixes:**
- Ensure backend API is running
- Check for CORS errors in console
- Verify `/api/jobs` endpoint exists and accepts POST requests
- Check request payload matches expected format (brief, planSpec, voiceSpec, renderSpec)

## API Endpoint Checklist

Verify these endpoints are accessible (replace `localhost:5005` with your API URL):

- [ ] `GET http://localhost:5005/api/healthz` - Health check
- [ ] `POST http://localhost:5005/api/validation/brief` - Brief validation
- [ ] `POST http://localhost:5005/api/quick/demo` - Quick demo generation
- [ ] `POST http://localhost:5005/api/jobs` - Full video generation
- [ ] `GET http://localhost:5005/api/preflight?profile=Free-Only` - Preflight check

### Testing API Endpoints with curl

```bash
# Health check
curl http://localhost:5005/api/healthz

# Preflight check
curl "http://localhost:5005/api/preflight?profile=Free-Only"

# Quick demo
curl -X POST http://localhost:5005/api/quick/demo \
  -H "Content-Type: application/json" \
  -d '{"topic": "Test Video"}'

# Validation
curl -X POST http://localhost:5005/api/validation/brief \
  -H "Content-Type: application/json" \
  -d '{"topic": "AI Video Generation Demo", "durationMinutes": 0.5}'
```

## Next Steps

If issues persist after reviewing console logs:

1. **Collect Information:**
   - Full console output (right-click in console → Save As)
   - Network tab showing failed requests
   - Browser and version
   - Steps to reproduce

2. **Check Backend:**
   - Is the API running? Check with `curl http://localhost:5005/api/healthz`
   - Are there errors in the API logs?
   - Are all dependencies installed?

3. **Clear State:**
   - Clear browser localStorage: `localStorage.clear()` in console
   - Clear browser cache
   - Refresh page

4. **Report Issue:**
   - Include console logs
   - Include network tab screenshots
   - Include any error toasts that appear
   - Describe exact steps taken

## Summary

The primary changes ensure that:
1. ✅ Upload Provider UI is removed
2. ✅ All button clicks are logged for debugging
3. ✅ All errors show toast notifications to the user
4. ✅ Preflight report data is fully logged for inspection
5. ✅ Button disabled state and reasons are logged

These changes don't fix the root cause (which requires diagnosing the actual error), but they make it much easier to see what's failing and why.
