# Wizard End-to-End Test Guide

## Overview
This document provides comprehensive testing procedures for the video creation wizard. All paths, options, and edge cases are covered to ensure robust wizard functionality.

## Prerequisites
- Completed Quick Demo verification
- FFmpeg installed and verified
- Aura Video Studio running at http://127.0.0.1:5005

## Test Categories

### 1. Wizard Navigation Tests
### 2. Default Values Tests
### 3. Settings Persistence Tests
### 4. Profile Selection Tests
### 5. Preflight Check Tests
### 6. Validation Tests
### 7. Error Handling Tests
### 8. Configuration Save Tests

---

## 1. Wizard Navigation Tests

### Test 1.1: Forward Navigation

**Objective:** Verify user can navigate forward through all wizard steps

**Steps:**
1. Navigate to `/create`
2. Fill in Step 1 (Video Brief):
   - Topic: "Test Navigation"
   - Keep other defaults
3. Click "Next"
4. Verify Step 2 (Video Plan) appears
5. Click "Next"
6. Verify Step 3 (Generation Options) appears

**Expected Result:**
- ✅ Each step loads successfully
- ✅ Progress indicator shows current step
- ✅ Previous step data is retained
- ✅ No errors in console

**Pass Criteria:** Can navigate from Step 1 → Step 2 → Step 3 without errors

---

### Test 1.2: Backward Navigation

**Objective:** Verify user can navigate backward and data is preserved

**Steps:**
1. Start at Step 3 (from Test 1.1)
2. Click "Back" button
3. Verify Step 2 appears with previous values
4. Click "Back" again
5. Verify Step 1 appears with previous values
6. Modify Topic to "Test Back Navigation"
7. Click "Next" twice to return to Step 3
8. Verify modified topic is used in Step 3

**Expected Result:**
- ✅ Back navigation works from any step
- ✅ All field values preserved
- ✅ Changes in earlier steps reflected in later steps
- ✅ No data loss

**Pass Criteria:** Can navigate backward without losing data

---

### Test 1.3: Step Indicator Navigation

**Objective:** Verify clicking step indicators navigates correctly

**Steps:**
1. Start at Step 1
2. Fill in required fields (Topic)
3. Click on "Step 2" in progress indicator
4. Verify Step 2 appears
5. Click on "Step 1" in progress indicator
6. Verify Step 1 appears with data intact

**Expected Result:**
- ✅ Direct navigation to any step works
- ✅ Data preserved across jumps
- ✅ Current step highlighted in indicator

**Pass Criteria:** Can jump between steps using progress indicator

---

## 2. Default Values Tests

### Test 2.1: Step 1 Defaults

**Objective:** Verify all Step 1 fields have correct default values

**Steps:**
1. Clear browser localStorage: `localStorage.clear()`
2. Refresh page
3. Navigate to `/create`
4. Verify default values:

| Field | Expected Default |
|-------|------------------|
| Topic | (empty - required field) |
| Audience | "General" |
| Goal | "Inform" |
| Tone | "Informative" |
| Language | "en-US" |
| Aspect Ratio | "Widescreen16x9" |

**Expected Result:**
- ✅ All defaults match specification
- ✅ Topic field is empty
- ✅ Dropdowns show correct default selections

**Pass Criteria:** All Step 1 defaults are correct

---

### Test 2.2: Step 2 Defaults

**Objective:** Verify all Step 2 fields have correct default values

**Steps:**
1. From Step 1, enter topic and click "Next"
2. Verify Step 2 default values:

| Field | Expected Default |
|-------|------------------|
| Duration | 3.0 minutes |
| Pacing | "Conversational" |
| Density | "Balanced" |
| Style | "Standard" |
| Watermark Opacity | 0.7 (70%) |
| Captions Enabled | true |
| Caption Format | "srt" |
| Burn-in Captions | false |
| Pexels | enabled |
| Pixabay | enabled |
| Unsplash | enabled |
| Offline Mode | false |

**Expected Result:**
- ✅ All defaults match specification
- ✅ Sliders at correct positions
- ✅ Switches in correct states
- ✅ Stock sources all enabled by default

**Pass Criteria:** All Step 2 defaults are correct

---

### Test 2.3: Brand Kit Defaults

**Objective:** Verify Brand Kit settings have correct defaults

**Steps:**
1. Navigate to Step 2
2. Expand "Brand Kit" section if collapsed
3. Verify defaults:

| Field | Expected Default |
|-------|------------------|
| Primary Color | (empty or system default) |
| Secondary Color | (empty) |
| Logo Path | (empty) |
| Watermark Path | (empty) |
| Watermark Opacity | 0.7 |

**Expected Result:**
- ✅ Brand Kit fields are empty initially
- ✅ Opacity slider at 70%
- ✅ File pickers show placeholder text

**Pass Criteria:** Brand Kit defaults are correct

---

## 3. Settings Persistence Tests

### Test 3.1: Persistence Across Page Refresh

**Objective:** Verify settings save to localStorage and restore

**Steps:**
1. Navigate to `/create`
2. Fill in unique values:
   - Topic: "Persistence Test Video"
   - Audience: "Professionals"
   - Duration: 5 minutes
   - Pacing: "Fast"
   - Primary Color: "#FF6B35"
3. Press F5 to refresh page
4. Navigate to `/create`
5. Verify all values restored:
   - Topic: "Persistence Test Video"
   - Audience: "Professionals"
   - Duration: 5 minutes
   - Pacing: "Fast"
   - Primary Color: "#FF6B35"

**Expected Result:**
- ✅ All custom values persist
- ✅ Settings restored on page load
- ✅ No data loss

**Pass Criteria:** Settings persist across refresh

---

### Test 3.2: Persistence Across Browser Restart

**Objective:** Verify settings persist after browser close

**Steps:**
1. Set custom values (as in Test 3.1)
2. Close browser completely
3. Reopen browser
4. Navigate to `http://127.0.0.1:5005/create`
5. Verify all values restored

**Expected Result:**
- ✅ Settings persist after browser restart
- ✅ localStorage data preserved

**Pass Criteria:** Settings survive browser restart

---

### Test 3.3: Independent Setting Storage

**Objective:** Verify settings don't interfere with other pages

**Steps:**
1. Set custom values in wizard
2. Navigate to Settings page
3. Verify Settings page has its own state
4. Return to `/create`
5. Verify wizard values still intact

**Expected Result:**
- ✅ Wizard state independent of other pages
- ✅ No cross-contamination of settings

**Pass Criteria:** Settings isolated correctly

---

## 4. Profile Selection Tests

### Test 4.1: Free-Only Profile

**Objective:** Complete wizard with Free-Only profile

**Steps:**
1. Navigate to `/create`
2. Step 1: Enter "Free Profile Test"
3. Step 2: Keep defaults
4. Step 3:
   - Select profile: "Free-Only"
   - Click "Run Preflight Check"
5. Verify preflight results:
   ```
   ✅ Script: Rule-based (Free)
   ✅ TTS: Windows SAPI (Free)
   ✅ Video: FFmpeg (Free)
   ⚠️  Visuals: Limited (text only)
   ```
6. Click "Generate Video"

**Expected Result:**
- ✅ Free-Only profile selected
- ✅ Preflight check passes
- ✅ Only free providers shown
- ✅ Generation starts without API keys

**Pass Criteria:** Free-Only workflow completes successfully

---

### Test 4.2: Pro-Basic Profile

**Objective:** Test Pro-Basic profile requirements

**Steps:**
1. Navigate to Step 3
2. Select profile: "Pro-Basic"
3. Click "Run Preflight Check"
4. Observe results (will vary based on API keys configured)

**Expected Results (with keys):**
```
✅ Script: GPT-4 Turbo
✅ TTS: ElevenLabs or Google Cloud
✅ Video: FFmpeg
✅ Visuals: Stock photos
```

**Expected Results (without keys):**
```
⚠️  Script: API key required for GPT-4
⚠️  TTS: API key required for ElevenLabs
❌ Cannot generate - missing API keys
```

**Pass Criteria:** Pro-Basic correctly identifies missing API keys

---

### Test 4.3: Pro-Max Profile

**Objective:** Test Pro-Max profile with all features

**Steps:**
1. Navigate to Step 3
2. Select profile: "Pro-Max"
3. Enable "Stable Diffusion" in Step 2
4. Enable "Offline Mode" if desired
5. Click "Run Preflight Check"
6. Observe comprehensive checks

**Expected Result:**
- ✅ All pro features listed in preflight
- ✅ Missing features clearly indicated
- ⚠️  Warning if required services not available

**Pass Criteria:** Pro-Max profile checks all features

---

### Test 4.4: Custom Profile

**Objective:** Verify custom profile configuration

**Steps:**
1. Navigate to Step 3
2. Select profile: "Custom"
3. Manually configure:
   - Script: "Rule-based" (Free)
   - TTS: "Windows SAPI" (Free)
   - Visuals: "Stock Photos"
4. Click "Run Preflight Check"
5. Verify custom configuration reflected

**Expected Result:**
- ✅ Custom selections saved
- ✅ Preflight uses custom settings
- ✅ Generation uses specified providers

**Pass Criteria:** Custom profile allows manual provider selection

---

## 5. Preflight Check Tests

### Test 5.1: Passing Preflight

**Objective:** Verify preflight passes with valid configuration

**Steps:**
1. Configure for Free-Only (known to work)
2. Click "Run Preflight Check"
3. Verify all checks pass:
   ```
   ✅ FFmpeg: Available at [path]
   ✅ Script Generator: Rule-based ready
   ✅ TTS: Windows SAPI ready
   ✅ Video Compositor: FFmpeg ready
   ```
4. Verify "Generate Video" button enabled

**Expected Result:**
- ✅ All preflight checks pass
- ✅ Green checkmarks shown
- ✅ Generate button enabled
- ✅ No warnings or errors

**Pass Criteria:** Preflight passes with valid config

---

### Test 5.2: Failing Preflight

**Objective:** Verify preflight fails gracefully

**Steps:**
1. Select Pro-Max profile (without API keys)
2. Click "Run Preflight Check"
3. Verify failures shown:
   ```
   ❌ API Key Required: OpenAI (for GPT-4)
   ❌ API Key Required: ElevenLabs (for TTS)
   ⚠️  Stable Diffusion: Not installed
   ```
4. Verify "Generate Video" button disabled
5. Verify helpful error messages with links to Settings

**Expected Result:**
- ✅ Failed checks clearly indicated
- ✅ Generate button disabled
- ✅ Error messages explain what's missing
- ✅ Links to fix issues provided

**Pass Criteria:** Failing preflight prevents generation and shows clear errors

---

### Test 5.3: Preflight Override

**Objective:** Verify user can override preflight warnings

**Steps:**
1. Trigger preflight warning (e.g., offline mode with stock photos)
2. Observe warning message
3. Check "Override preflight warnings" checkbox
4. Verify "Generate Video" button becomes enabled
5. Click "Generate Video"
6. Verify generation attempts despite warnings

**Expected Result:**
- ✅ Override checkbox appears with warnings
- ✅ Generate button enabled after override
- ✅ Generation proceeds with warning acknowledged

**Pass Criteria:** Override allows generation despite warnings

---

### Test 5.4: Preflight Re-run

**Objective:** Verify preflight can be re-run after changes

**Steps:**
1. Run preflight with incomplete config
2. Navigate to Settings
3. Add missing API keys
4. Return to wizard Step 3
5. Click "Run Preflight Check" again
6. Verify new preflight results reflect changes

**Expected Result:**
- ✅ Preflight can be re-run multiple times
- ✅ Results update based on current config
- ✅ No stale data shown

**Pass Criteria:** Preflight accurately reflects current system state

---

## 6. Validation Tests

### Test 6.1: Required Field Validation

**Objective:** Verify required fields are enforced

**Steps:**
1. Navigate to Step 1
2. Leave "Topic" field empty
3. Click "Next"
4. Verify:
   - Error message appears: "Topic is required"
   - Cannot proceed to next step
   - Field highlighted in red

**Expected Result:**
- ✅ Validation error shown
- ✅ Navigation blocked
- ✅ Clear error message

**Pass Criteria:** Required fields enforced

---

### Test 6.2: Field Format Validation

**Objective:** Verify field formats are validated

**Steps:**
1. Navigate to Step 2
2. Enter invalid duration: 0 minutes
3. Click "Next"
4. Verify error: "Duration must be at least 0.5 minutes"

5. Enter invalid duration: 1000 minutes
6. Verify error: "Duration cannot exceed 60 minutes"

7. Test color picker with invalid hex:
   - Enter "#GGGGGG" in Primary Color
   - Verify error or automatic correction

**Expected Result:**
- ✅ Minimum/maximum values enforced
- ✅ Invalid formats rejected
- ✅ Helpful error messages

**Pass Criteria:** Format validation works correctly

---

### Test 6.3: Conditional Validation

**Objective:** Verify conditional fields validated correctly

**Steps:**
1. Enable "Stable Diffusion" in Step 2
2. Leave SD URL empty
3. Proceed to Step 3
4. Run preflight
5. Verify warning about missing SD URL

**Expected Result:**
- ✅ Conditional validation based on enabled features
- ✅ Warnings shown for incomplete optional features
- ✅ Can override if desired

**Pass Criteria:** Conditional validation works

---

### Test 6.4: Real-time Validation

**Objective:** Verify validation happens as user types

**Steps:**
1. Focus on Duration field
2. Type "0.1"
3. Observe validation error appears immediately
4. Type "3.0"
5. Observe error clears immediately

**Expected Result:**
- ✅ Validation updates in real-time
- ✅ No need to submit to see errors
- ✅ Errors clear when fixed

**Pass Criteria:** Real-time validation responsive

---

## 7. Error Handling Tests

### Test 7.1: Network Error During Generation

**Objective:** Verify graceful handling of network errors

**Steps:**
1. Configure Pro-Max profile (requires API calls)
2. Start video generation
3. Simulate network error:
   - Disconnect internet mid-generation
   - Or block API endpoint in browser DevTools
4. Observe error handling

**Expected Result:**
- ✅ Error message displayed
- ✅ Generation stops gracefully
- ✅ Option to retry
- ✅ No crash or hanging

**Pass Criteria:** Network errors handled gracefully

---

### Test 7.2: API Error Response

**Objective:** Verify API error responses handled correctly

**Steps:**
1. Configure with invalid API key
2. Start generation
3. Observe API error response
4. Verify:
   - Error message shown
   - Suggests checking API key in Settings
   - Option to return to wizard
   - No stack trace shown to user

**Expected Result:**
- ✅ User-friendly error message
- ✅ Helpful suggestions provided
- ✅ Technical details hidden
- ✅ Can recover without restart

**Pass Criteria:** API errors handled gracefully

---

### Test 7.3: Insufficient Disk Space

**Objective:** Verify disk space check before generation

**Steps:**
1. Configure normal generation
2. If possible, simulate low disk space
3. Start generation
4. Verify warning or error about disk space

**Expected Result:**
- ✅ Disk space checked before generation
- ⚠️  Warning if space low
- ❌ Error if insufficient space

**Pass Criteria:** Disk space validated

---

### Test 7.4: Timeout Handling

**Objective:** Verify long-running operations don't hang

**Steps:**
1. Configure complex video (5+ minutes, Pro features)
2. Start generation
3. Observe progress updates continue
4. If generation exceeds expected time, verify timeout handling

**Expected Result:**
- ✅ Progress updates shown throughout
- ✅ Timeout configured reasonably
- ✅ User can cancel if needed
- ✅ No indefinite waiting

**Pass Criteria:** Long operations handle timeouts

---

## 8. Configuration Save Tests

### Test 8.1: Save to Profile

**Objective:** Verify settings can be saved as named profile

**Steps:**
1. Configure custom wizard settings
2. Click "Save as Profile"
3. Enter profile name: "My Test Profile"
4. Save profile
5. Reset wizard to defaults
6. Load "My Test Profile"
7. Verify all settings restored

**Expected Result:**
- ✅ Profile saves successfully
- ✅ Profile appears in profile list
- ✅ Loading profile restores all settings
- ✅ Can save multiple profiles

**Pass Criteria:** Profile save/load works

---

### Test 8.2: Export Configuration

**Objective:** Verify settings can be exported to JSON

**Steps:**
1. Configure wizard settings
2. Click "Export Configuration"
3. Save JSON file
4. Verify JSON contains all settings
5. Reset wizard
6. Click "Import Configuration"
7. Load saved JSON
8. Verify settings restored

**Expected Result:**
- ✅ Export creates valid JSON
- ✅ All settings included in export
- ✅ Import restores settings correctly
- ✅ Can share configs between users

**Pass Criteria:** Export/import works

---

### Test 8.3: Settings Migration

**Objective:** Verify old settings migrate to new format

**Steps:**
1. Manually create old format in localStorage
2. Refresh page
3. Verify settings automatically migrate
4. Verify new format used going forward

**Expected Result:**
- ✅ Old settings detected
- ✅ Automatic migration to new format
- ✅ No data loss
- ✅ Warning shown if applicable

**Pass Criteria:** Settings migration works

---

## Test Execution Checklist

Run all tests and mark as Pass/Fail:

### Navigation Tests
- [ ] Test 1.1: Forward Navigation
- [ ] Test 1.2: Backward Navigation
- [ ] Test 1.3: Step Indicator Navigation

### Default Values Tests
- [ ] Test 2.1: Step 1 Defaults
- [ ] Test 2.2: Step 2 Defaults
- [ ] Test 2.3: Brand Kit Defaults

### Persistence Tests
- [ ] Test 3.1: Persistence Across Refresh
- [ ] Test 3.2: Persistence Across Browser Restart
- [ ] Test 3.3: Independent Setting Storage

### Profile Selection Tests
- [ ] Test 4.1: Free-Only Profile
- [ ] Test 4.2: Pro-Basic Profile
- [ ] Test 4.3: Pro-Max Profile
- [ ] Test 4.4: Custom Profile

### Preflight Check Tests
- [ ] Test 5.1: Passing Preflight
- [ ] Test 5.2: Failing Preflight
- [ ] Test 5.3: Preflight Override
- [ ] Test 5.4: Preflight Re-run

### Validation Tests
- [ ] Test 6.1: Required Field Validation
- [ ] Test 6.2: Field Format Validation
- [ ] Test 6.3: Conditional Validation
- [ ] Test 6.4: Real-time Validation

### Error Handling Tests
- [ ] Test 7.1: Network Error During Generation
- [ ] Test 7.2: API Error Response
- [ ] Test 7.3: Insufficient Disk Space
- [ ] Test 7.4: Timeout Handling

### Configuration Save Tests
- [ ] Test 8.1: Save to Profile
- [ ] Test 8.2: Export Configuration
- [ ] Test 8.3: Settings Migration

## Test Report Template

```
=== Wizard End-to-End Test Report ===

Date: [DATE]
Tester: [NAME]
Build Version: [VERSION]

Navigation Tests: [X/3 passed]
Default Values Tests: [X/3 passed]
Persistence Tests: [X/3 passed]
Profile Selection Tests: [X/4 passed]
Preflight Check Tests: [X/4 passed]
Validation Tests: [X/4 passed]
Error Handling Tests: [X/4 passed]
Configuration Save Tests: [X/3 passed]

Total: [X/28 tests passed]

Critical Failures:
1. [Description if any]

Minor Issues:
1. [Description if any]

Overall Result: [PASS/FAIL]
```

---

**Document Version:** 1.0  
**Last Updated:** 2025-10-20  
**Maintained By:** Aura Video Studio Team
