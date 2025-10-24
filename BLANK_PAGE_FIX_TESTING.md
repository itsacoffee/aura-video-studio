# Blank Page Fix - Testing Guide

This document outlines the testing procedures to verify that the blank page issue fix is working correctly.

## Issue Summary

**Problem**: Users reported getting a blank white page when accessing `http://127.0.0.1:5005` after building and launching the portable distribution.

**Root Cause**: The web UI files were not being properly validated during the build process, resulting in missing or incomplete `wwwroot` folder in the portable distribution.

## Changes Made

### 1. Build Script Validation (`scripts/packaging/build-portable.ps1`)
- Validates `Aura.Web/dist` folder exists
- Validates `dist/index.html` exists
- Validates `dist/assets/` folder exists
- Validates files are successfully copied to `Api/wwwroot/`
- Build fails fast with clear error messages if validation fails

### 2. Launch Script Pre-flight Checks (`Launch.bat`)
- Checks `Api/` folder exists
- Checks `Api/Aura.Api.exe` exists
- Checks `Api/wwwroot/` folder exists
- Checks `Api/wwwroot/index.html` exists
- Shows clear error messages if any check fails
- Prevents starting API if files are missing

### 3. Runtime Error Handling (`Aura.Api/Program.cs`)
- Validates `index.html` exists in wwwroot before serving
- Logs comprehensive error messages if wwwroot is missing
- Provides step-by-step troubleshooting instructions
- Clearly indicates that blank page will occur without wwwroot

## Test Plan

### Test 1: Normal Build (Happy Path)

**Prerequisites**: Windows 11, .NET 8 SDK, Node.js 18+ installed

**Steps**:
1. Clone the repository
2. Run `scripts\packaging\build-portable.ps1` from PowerShell
3. Wait for build to complete
4. Extract the generated ZIP from `artifacts\portable\`
5. Run `Launch.bat`
6. Verify browser opens to `http://127.0.0.1:5005`
7. Verify the web UI loads (not a blank page)

**Expected Result**: 
- ✅ Build completes with no errors
- ✅ Launch.bat shows "Pre-flight checks passed!"
- ✅ API console shows "Serving static files from: ...\Api\wwwroot"
- ✅ Browser shows Aura Video Studio UI (not blank)

### Test 2: Frontend Build Failure

**Prerequisites**: Same as Test 1

**Steps**:
1. Clone the repository
2. Delete or rename `Aura.Web/dist` folder
3. Run `scripts\packaging\build-portable.ps1`

**Expected Result**:
- ❌ Build fails with error: "Web UI dist folder not found"
- ❌ Error message indicates frontend build may have failed
- ❌ No ZIP file is created

### Test 3: Missing wwwroot After Build

**Prerequisites**: Portable ZIP has been built

**Steps**:
1. Extract the portable ZIP
2. Delete `Api\wwwroot` folder
3. Run `Launch.bat`

**Expected Result**:
- ❌ Launch.bat shows error: "Web UI files not found at Api\wwwroot\"
- ❌ Error message tells user to re-extract ZIP
- ❌ API does not start
- ❌ Browser is not opened

### Test 4: Missing index.html

**Prerequisites**: Portable ZIP has been built

**Steps**:
1. Extract the portable ZIP
2. Delete `Api\wwwroot\index.html` (keep the folder)
3. Run `Launch.bat`

**Expected Result**:
- ❌ Launch.bat shows error: "index.html not found in Api\wwwroot\"
- ❌ Error message tells user to re-extract ZIP
- ❌ API does not start
- ❌ Browser is not opened

### Test 5: API Runtime Error (Bypass Launch.bat)

**Prerequisites**: Portable ZIP has been built

**Steps**:
1. Extract the portable ZIP
2. Delete `Api\wwwroot` folder
3. Navigate to `Api` folder in command prompt
4. Run `Aura.Api.exe` directly (bypassing Launch.bat)
5. Check the API console output
6. Try to access `http://127.0.0.1:5005` in browser

**Expected Result**:
- ⚠️ API starts but logs comprehensive error about missing wwwroot
- ⚠️ Error log includes:
  - "CRITICAL: wwwroot directory not found"
  - Explanation of possible causes
  - Step-by-step fix instructions
- ⚠️ Browser shows blank page or 404 (as expected)
- ⚠️ Logs clearly state this will happen

### Test 6: Incomplete ZIP Extraction

**Prerequisites**: Portable ZIP has been built

**Steps**:
1. Extract only the `Launch.bat` and `Api\Aura.Api.exe` from the ZIP (selective extraction)
2. Run `Launch.bat`

**Expected Result**:
- ❌ Launch.bat shows error: "Web UI files not found"
- ❌ Error message tells user to extract all files
- ❌ API does not start

### Test 7: Build Script Validation

**Prerequisites**: Source code repository, .NET 8 SDK, Node.js installed

**Steps**:
1. Clone repository
2. Run `npm run build` in `Aura.Web` folder
3. Delete `Aura.Web\dist\index.html` (keep dist folder)
4. Run `scripts\packaging\build-portable.ps1`

**Expected Result**:
- ❌ Build fails with error: "index.html not found in dist folder"
- ❌ Error message indicates frontend build is incomplete
- ❌ No ZIP file is created

## Automated Testing

Currently, these tests must be run manually on Windows. Future enhancements could include:

1. **PowerShell Pester Tests**: Automated tests for the build script validation logic
2. **E2E Tests**: Automated browser tests to verify UI loads correctly
3. **CI Integration**: GitHub Actions workflow to test portable build on Windows

## Regression Testing

When making future changes, ensure:

1. The build script still validates frontend build output
2. Launch.bat pre-flight checks still work
3. API error messages remain clear and actionable
4. Documentation (PORTABLE.md) stays up to date

## Success Criteria

The fix is considered successful if:

- ✅ Users never see a blank page when following documented instructions
- ✅ Build fails fast with clear messages if frontend build fails
- ✅ Launch.bat prevents starting with incomplete files
- ✅ API logs provide actionable troubleshooting steps
- ✅ All test cases pass as expected

## Known Limitations

1. Tests require Windows environment (cannot be automated on Linux CI)
2. PowerShell build script cannot be tested in bash/sh environments
3. Manual testing required for full portable distribution workflow

## Next Steps

- [ ] Run all test cases manually on Windows 11
- [ ] Verify error messages are clear to end users
- [ ] Update this document with any additional findings
- [ ] Consider adding automated PowerShell tests
