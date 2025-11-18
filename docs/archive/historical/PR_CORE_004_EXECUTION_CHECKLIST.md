# PR-CORE-004: Windows Compatibility Execution Checklist

## 📋 Pre-Execution Setup

### Environment Requirements
- [ ] Windows 10 (version 1607 or later) or Windows 11
- [ ] .NET 8.0 SDK installed
- [ ] Visual Studio 2022 or Visual Studio Code (optional)
- [ ] Administrator privileges (for some manual tests)
- [ ] At least 10GB free disk space

### Verify Installation
```powershell
# Check .NET version
dotnet --version  # Should show 8.0.x or later

# Check available disk space
Get-PSDrive C | Select-Object Used,Free

# Check Windows version
[System.Environment]::OSVersion.Version
```

---

## 🧪 Automated Test Execution

### Step 1: Run Complete Test Suite
```bash
cd /workspace
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~WindowsDatabaseStorageCompatibilityTests" \
  --logger "console;verbosity=detailed" \
  --results-directory ./TestResults
```

**Expected Result**: All tests pass ✅

**If tests fail**:
1. Check the test output for specific failure messages
2. Verify environment meets requirements
3. Check Windows event logs for system-level issues
4. Review `TestResults` directory for detailed logs

---

### Step 2: Run Tests by Category

#### Database Initialization Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~DatabaseInitialization" \
  --logger "console;verbosity=detailed"
```

**Tests**: 5
- [ ] DatabaseInitialization_OnWindows_CreatesDatabase
- [ ] DatabaseInitialization_WithWindowsPathWithSpaces_Succeeds
- [ ] SQLiteWALMode_OnWindows_EnabledSuccessfully
- [ ] DatabaseIntegrityCheck_OnWindows_Passes
- [ ] ConcurrentDatabaseAccess_OnWindows_HandlesMultipleConnections

---

#### File Path Handling Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~FilePathHandling" \
  --logger "console;verbosity=detailed"
```

**Tests**: 5
- [ ] FilePathHandling_WindowsPathWithBackslashes_NormalizesCorrectly
- [ ] FilePathHandling_UNCPath_HandledCorrectly
- [ ] FilePathHandling_LongPath_HandlesCorrectly
- [ ] FilePathHandling_SpecialCharacters_HandledCorrectly
- [ ] FilePathHandling_RelativePaths_ConvertToAbsolute

---

#### Project Save/Load Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~ProjectSaveLoad" \
  --logger "console;verbosity=detailed"
```

**Tests**: 3
- [ ] ProjectSaveLoad_OnWindows_WorksWithLocalPaths
- [ ] ProjectWithAssets_OnWindows_HandlesWindowsPaths
- [ ] ProjectPackage_OnWindows_CreatesValidZipFile

---

#### File Locking Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~FileLocking" \
  --logger "console;verbosity=detailed"
```

**Tests**: 3
- [ ] FileLocking_OnWindows_DetectsLockedFiles
- [ ] SQLiteDatabase_OnWindows_HandlesFileLocking
- [ ] ConcurrentWrites_OnWindows_HandledCorrectly

---

#### Temporary File Cleanup Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj \
  --filter "FullyQualifiedName~TemporaryFileCleanup" \
  --logger "console;verbosity=detailed"
```

**Tests**: 3
- [ ] TemporaryFileCleanup_OnWindows_CleansUpOldFiles
- [ ] TemporaryFileCleanup_OnWindows_PreservesLockedFiles
- [ ] TemporaryFileCleanup_OnWindows_RemovesEmptyDirectories

---

## 🔧 Manual Testing Checklist

### Database Tests

#### Test 1: Database Creation in Standard Location
1. [ ] Delete any existing `aura.db` file
2. [ ] Launch Aura application
3. [ ] Verify database created in expected location
4. [ ] Check for WAL files (`aura.db-wal`, `aura.db-shm`)
5. [ ] Verify no errors in application logs

**Expected Location**: `{AppDirectory}\aura.db`

---

#### Test 2: Database Creation in Path with Spaces
1. [ ] Install Aura to `C:\Program Files\Aura Video Studio\`
2. [ ] Launch application
3. [ ] Verify database initializes correctly
4. [ ] Check application logs for any path-related errors

---

#### Test 3: Concurrent Database Access
1. [ ] Launch Aura application
2. [ ] Start a video rendering job
3. [ ] Open Aura in a second window (if supported)
4. [ ] Verify both instances work without "database locked" errors
5. [ ] Check for any data corruption

---

### File Path Tests

#### Test 4: Project on Different Drive
1. [ ] Create project on D:\ drive (if available)
2. [ ] Save project as `D:\AuraProjects\TestProject.aura`
3. [ ] Close and reopen project
4. [ ] Verify all paths resolve correctly

---

#### Test 5: Project with Spaces and Special Characters
1. [ ] Create folder: `C:\Users\{Username}\My Videos (2024) [Test]\`
2. [ ] Create project in this folder
3. [ ] Add media assets with special characters in names
4. [ ] Save, close, and reopen project
5. [ ] Verify all assets load correctly

---

#### Test 6: UNC Path (Network Share)
**Prerequisites**: Access to network share
1. [ ] Map network drive or use UNC path: `\\server\share\AuraProjects\`
2. [ ] Create project on network location
3. [ ] Add local media assets
4. [ ] Verify project saves and loads correctly
5. [ ] Check for performance issues

---

#### Test 7: Long Path Support
1. [ ] Create deeply nested folder structure (>200 characters)
2. [ ] Create project in deepest folder
3. [ ] Verify project creation succeeds or fails gracefully
4. [ ] Document any path length limitations

**Example Path**:
```
C:\Users\{Username}\Documents\Videos\Projects\2024\Q4\November\Week2\Day10\Morning\Session1\Project1\SubProject\Assets\Media\VideoClips\...
```

---

### Project Operation Tests

#### Test 8: Project with Mixed Asset Sources
1. [ ] Create new project
2. [ ] Add assets from:
   - [ ] C:\ drive
   - [ ] D:\ drive (if available)
   - [ ] Network share (if available)
   - [ ] External USB drive (if available)
3. [ ] Save project
4. [ ] Disconnect external/network drives
5. [ ] Reopen project
6. [ ] Verify missing assets detected correctly
7. [ ] Reconnect drives
8. [ ] Verify assets can be relinked

---

#### Test 9: Project Package Export/Import
1. [ ] Create project with multiple assets
2. [ ] Export as `.aurapack` file
3. [ ] Delete original project
4. [ ] Import `.aurapack` file
5. [ ] Verify all assets included and paths correct
6. [ ] Test package on different Windows machine (if available)

---

#### Test 10: Project Consolidation
1. [ ] Create project with external assets (not in project folder)
2. [ ] Run project consolidation
3. [ ] Verify all assets copied to project folder
4. [ ] Check that paths updated correctly
5. [ ] Verify original files not deleted (unless specified)

---

### File Locking Tests

#### Test 11: Database Access During Render
1. [ ] Start long-running render job
2. [ ] While rendering, try to access database:
   - [ ] Create new project
   - [ ] Save existing project
   - [ ] Export project
3. [ ] Verify no "database locked" errors
4. [ ] Verify render completes successfully

---

#### Test 12: Cleanup with Active Files
1. [ ] Start rendering video
2. [ ] Note location of temporary files
3. [ ] While rendering, manually trigger cleanup (if possible)
4. [ ] Verify render temp files not deleted
5. [ ] Verify render completes successfully
6. [ ] After render, verify temp files cleaned up

---

### Cleanup Tests

#### Test 13: Old Temp File Cleanup
1. [ ] Locate temp directory: `%LOCALAPPDATA%\Aura\Temp\`
2. [ ] Manually create old test files (date 2+ days ago)
3. [ ] Wait for cleanup cycle or trigger manually
4. [ ] Verify old files removed
5. [ ] Verify recent files preserved

---

#### Test 14: Empty Directory Cleanup
1. [ ] Create empty subdirectories in temp folder
2. [ ] Run cleanup
3. [ ] Verify empty directories removed
4. [ ] Verify directories with files preserved

---

## 🔍 Performance Validation

### Test 15: Database Performance Under Load
```bash
# Use provided performance test script if available
dotnet run --project Aura.Tests -- \
  --test-type performance \
  --duration 60 \
  --concurrent-users 5
```

**Metrics to Capture**:
- [ ] Average query time: < 50ms
- [ ] Peak query time: < 200ms
- [ ] No database lock timeout errors
- [ ] Memory usage stable

---

### Test 16: Large Project Load Time
1. [ ] Create project with 100+ assets
2. [ ] Measure time to:
   - [ ] Save project: < 2 seconds
   - [ ] Load project: < 5 seconds
   - [ ] Detect missing assets: < 10 seconds
3. [ ] Document results

---

## 🛡️ Security & Permissions Tests

### Test 17: Standard User Account
1. [ ] Log in as standard user (non-admin)
2. [ ] Launch Aura application
3. [ ] Verify all operations work without admin rights
4. [ ] Check no UAC prompts appear

---

### Test 18: Read-Only Folder
1. [ ] Create folder and mark as read-only
2. [ ] Try to create project in read-only folder
3. [ ] Verify graceful error handling
4. [ ] Check appropriate error message shown

---

### Test 19: Antivirus Compatibility
**Prerequisites**: Windows Defender or other AV active
1. [ ] Enable real-time protection
2. [ ] Run all automated tests
3. [ ] Perform manual tests
4. [ ] Monitor for AV interference
5. [ ] Check AV logs for false positives

---

## 📊 Results Documentation

### Test Summary Template
```
Date: _______________
Tester: _______________
Windows Version: _______________
.NET Version: _______________

Automated Tests:
- Database Tests: PASS / FAIL
- Path Tests: PASS / FAIL
- Project Tests: PASS / FAIL
- Locking Tests: PASS / FAIL
- Cleanup Tests: PASS / FAIL

Manual Tests: X / 19 Passed

Issues Found: (list any issues)

Performance Results:
- Database avg query time: ___ ms
- Large project load time: ___ seconds
- Memory usage: ___ MB peak

Notes:
_______________________________________________
_______________________________________________
```

---

## ✅ Sign-Off Criteria

**Required for approval**:
- [ ] All automated tests pass (25/25)
- [ ] All critical manual tests pass (tests 1-10)
- [ ] No database corruption observed
- [ ] No data loss observed
- [ ] Performance within acceptable limits
- [ ] No security vulnerabilities found
- [ ] Documentation updated

**Optional but recommended**:
- [ ] All manual tests completed (19/19)
- [ ] Tested on multiple Windows versions
- [ ] Tested with different antivirus software
- [ ] Tested on different drive configurations
- [ ] Stress testing completed

---

## 🐛 Issue Reporting Template

If issues are found, use this template:

```markdown
### Issue #X: [Brief Description]

**Severity**: Critical / High / Medium / Low

**Test**: [Test number and name]

**Environment**:
- Windows Version: 
- .NET Version: 
- Antivirus: 
- Drive Configuration: 

**Steps to Reproduce**:
1. 
2. 
3. 

**Expected Behavior**:


**Actual Behavior**:


**Logs/Screenshots**:
[Attach relevant logs]

**Workaround** (if any):


**Impact Assessment**:


**Recommended Fix**:
```

---

## 📞 Support & Escalation

**For issues during testing**:
1. Check detailed documentation: `PR_CORE_004_WINDOWS_DATABASE_STORAGE_COMPATIBILITY.md`
2. Review test code: `Aura.Tests/Windows/WindowsDatabaseStorageCompatibilityTests.cs`
3. Check Windows Event Viewer for system-level issues
4. Review application logs in `%LOCALAPPDATA%\Aura\Logs\`

**Escalation Path**:
1. Development Team Lead
2. Architecture Review Board
3. QA Manager

---

## 📝 Completion Certificate

Upon successful completion of all tests:

```
PR-CORE-004: WINDOWS COMPATIBILITY VERIFICATION

I certify that all required tests have been executed and passed.
The Aura Video Studio database and storage layer is ready for
Windows production deployment.

Tester Name: _______________
Date: _______________
Signature: _______________

Reviewer Name: _______________
Date: _______________
Signature: _______________

Approved for Release: YES / NO
```

---

**Document Version**: 1.0
**Last Updated**: 2025-11-11
**Status**: Ready for Execution
