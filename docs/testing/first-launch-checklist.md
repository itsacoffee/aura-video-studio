# First Launch Manual Testing Checklist

## Prerequisites
- [ ] Fresh Windows 11 machine (or VM)
- [ ] No .NET SDK installed
- [ ] Windows Firewall enabled
- [ ] No previous Aura installation

## Installer Tests

### Test 1: Clean Install
1. [ ] Download Aura Video Studio installer (`.exe`)
2. [ ] Run installer as normal user (not admin)
3. [ ] Complete installation
4. [ ] Launch Aura Video Studio from Start Menu
5. [ ] **Expected**: App launches, backend starts automatically, setup wizard appears within 10 seconds
6. [ ] **Expected**: No "Backend Server Not Reachable" error

**Notes:**
- Record startup time from launch to wizard display
- Check Task Manager for `Aura.Api.exe` process
- Verify no console windows appear

### Test 2: Firewall Rule Installation
1. [ ] Check Windows Firewall with Advanced Security
2. [ ] **Expected**: Inbound rule "Aura Video Studio Backend" exists
3. [ ] **Expected**: Rule points to `C:\Program Files\Aura Video Studio\resources\backend\Aura.Api.exe`

**Verification Steps:**
1. Open Windows Defender Firewall with Advanced Security
2. Navigate to "Inbound Rules"
3. Look for "Aura Video Studio Backend" rule
4. Verify rule properties:
   - Protocol: TCP
   - Local Port: 5000
   - Action: Allow
   - Program: `C:\Program Files\Aura Video Studio\resources\backend\Aura.Api.exe`

### Test 3: Backend Process
1. [ ] Open Task Manager while app is running
2. [ ] **Expected**: Process `Aura.Api.exe` is running
3. [ ] Close Aura Video Studio
4. [ ] **Expected**: `Aura.Api.exe` process terminates within 5 seconds

**Additional Checks:**
- Verify memory usage of `Aura.Api.exe` (should be < 200MB at idle)
- Check CPU usage (should be < 5% at idle)
- Verify listening on port 5000 (use `netstat -ano | findstr :5000`)

## Portable Build Tests

### Test 4: Portable Execution
1. [ ] Download Aura Video Studio Portable (`.exe`)
2. [ ] Run portable executable from Downloads folder
3. [ ] **Expected**: App launches without installation
4. [ ] **Expected**: Backend starts automatically
5. [ ] **Expected**: Setup wizard appears within 10 seconds

**Notes:**
- Verify no files are created in Program Files
- Check that settings are stored in portable directory
- Confirm backend executable is extracted to portable location

### Test 5: Portable Firewall Detection
1. [ ] Run portable build on machine without firewall rule
2. [ ] **Expected**: Firewall configuration dialog appears
3. [ ] Click "Configure Firewall Automatically"
4. [ ] **Expected**: UAC prompt appears
5. [ ] Accept UAC prompt
6. [ ] **Expected**: Firewall rule is created
7. [ ] **Expected**: App continues to setup wizard

**Verification:**
- Check firewall rules for portable executable path
- Verify rule points to `<PortableDir>\resources\backend\Aura.Api.exe`

### Test 6: Portable Skip Firewall
1. [ ] Run portable build on machine without firewall rule
2. [ ] **Expected**: Firewall configuration dialog appears
3. [ ] Click "Skip"
4. [ ] **Expected**: App continues to setup wizard
5. [ ] **Expected**: Warning about network connectivity may appear

## Error Handling Tests

### Test 7: Backend Fails to Start
**Scenario**: Simulate backend failure by blocking port 5000

1. [ ] Start another application on port 5000 (e.g., `python -m http.server 5000`)
2. [ ] Launch Aura Video Studio
3. [ ] **Expected**: Error message "Backend failed to start (port in use)"
4. [ ] **Expected**: Option to retry or configure manually
5. [ ] Stop the blocking application
6. [ ] Click "Retry"
7. [ ] **Expected**: Backend starts successfully

### Test 8: Slow Network Connection
**Scenario**: Simulate slow backend startup

1. [ ] Configure Windows Defender to scan `Aura.Api.exe` on execution
2. [ ] Launch Aura Video Studio
3. [ ] **Expected**: Loading indicator with message "Connecting to backend"
4. [ ] **Expected**: Progress indication or retry counter
5. [ ] **Expected**: Eventually connects within 30 seconds

### Test 9: Firewall Rule Creation Fails
**Scenario**: UAC denial or insufficient permissions

1. [ ] Launch portable build without admin rights
2. [ ] Click "Configure Firewall Automatically"
3. [ ] Deny UAC prompt
4. [ ] **Expected**: Error message explaining failure
5. [ ] **Expected**: Option to retry or skip
6. [ ] **Expected**: App continues to function (may have connectivity warnings)

## Recovery Tests

### Test 10: Backend Crash Recovery
**Scenario**: Backend crashes during first run

1. [ ] Launch Aura Video Studio
2. [ ] Wait for setup wizard to appear
3. [ ] Terminate `Aura.Api.exe` process manually via Task Manager
4. [ ] **Expected**: App detects crash within 5 seconds
5. [ ] **Expected**: Automatic restart attempt
6. [ ] **Expected**: Connection restored within 10 seconds

### Test 11: Firewall Rule Removal
**Scenario**: User removes firewall rule manually

1. [ ] Complete first launch with firewall rule
2. [ ] Manually delete firewall rule via Windows Firewall settings
3. [ ] Restart Aura Video Studio
4. [ ] **Expected**: Firewall dialog appears again
5. [ ] **Expected**: Option to recreate rule

## Performance Tests

### Test 12: Startup Performance
**Metrics to Record:**
- [ ] Time from launch to app window visible: ______ seconds
- [ ] Time from launch to backend ready: ______ seconds
- [ ] Time from launch to setup wizard: ______ seconds

**Targets:**
- App window visible: < 3 seconds
- Backend ready: < 8 seconds
- Setup wizard: < 10 seconds

### Test 13: Resource Usage
**Metrics to Record:**
- [ ] Frontend memory usage at idle: ______ MB
- [ ] Backend memory usage at idle: ______ MB
- [ ] Frontend CPU usage at idle: ______ %
- [ ] Backend CPU usage at idle: ______ %

**Targets:**
- Frontend memory: < 300MB
- Backend memory: < 200MB
- Frontend CPU: < 10%
- Backend CPU: < 5%

## Accessibility Tests

### Test 14: Keyboard Navigation
1. [ ] Launch app without mouse
2. [ ] Navigate using Tab key
3. [ ] **Expected**: Focus visible on all interactive elements
4. [ ] **Expected**: Can reach setup wizard using keyboard only

### Test 15: Screen Reader Compatibility
1. [ ] Launch NVDA or Narrator
2. [ ] Launch Aura Video Studio
3. [ ] **Expected**: Loading messages are announced
4. [ ] **Expected**: Error messages are announced
5. [ ] **Expected**: Setup wizard is navigable with screen reader

## Integration Tests

### Test 16: Windows Security Integration
1. [ ] Launch with Windows Security real-time protection enabled
2. [ ] **Expected**: No security warnings for trusted publisher
3. [ ] **Expected**: Backend executable not flagged as threat

### Test 17: Multiple Instances
1. [ ] Launch first instance of Aura Video Studio
2. [ ] Attempt to launch second instance
3. [ ] **Expected**: Second instance detects first is running
4. [ ] **Expected**: Second instance either:
   - Focuses first instance, OR
   - Shows message "Already running"

### Test 18: Network Configuration
**Test various network scenarios:**
- [ ] Corporate network with proxy
- [ ] Network with restrictive firewall
- [ ] No network connection
- [ ] VPN connection

**Expected**: App functions in offline mode if backend connectivity works

## Sign-off

**Tester Name:** ___________________

**Test Date:** ___________________

**Build Version:** ___________________

**Test Environment:**
- OS Version: ___________________
- RAM: ___________________
- Antivirus: ___________________

**Overall Result:** [ ] PASS [ ] FAIL [ ] PASS WITH ISSUES

**Issues Found:**
1. ___________________
2. ___________________
3. ___________________

**Additional Notes:**
___________________
___________________
___________________
