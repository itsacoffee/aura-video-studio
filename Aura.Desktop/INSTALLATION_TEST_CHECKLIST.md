# Windows Installer Testing Checklist

Use this checklist when testing the Windows installer on a clean Windows 11 VM.

## Pre-Installation Phase

### Environment Setup
- [ ] Clean Windows 11 VM (22H2 or later)
- [ ] Windows fully updated
- [ ] Create VM snapshot "Clean State"
- [ ] 8GB+ RAM allocated
- [ ] 20GB+ free disk space
- [ ] Administrator account available

### Pre-Installation Checks
- [ ] Run: `.\scripts\test-installation-e2e.ps1 -InstallerPath "<path>" -Silent`
- [ ] No existing installation detected
- [ ] Windows version compatible
- [ ] Installer file size 200-400MB
- [ ] SHA256 checksum recorded

## Installation Phase

### Installer Launch
- [ ] Right-click installer → Run as Administrator
- [ ] UAC prompt appears and is accepted
- [ ] Installer window opens without errors

### License Agreement
- [ ] License text displays correctly
- [ ] MIT License visible
- [ ] "I accept" checkbox functions
- [ ] "Next" button enables after accepting
- [ ] "Cancel" button exits installer

### Installation Directory
- [ ] Default path: `C:\Program Files\Aura Video Studio`
- [ ] "Browse" button works
- [ ] Path validation prevents invalid characters
- [ ] Disk space check passes (requires ~500MB)
- [ ] Warning appears if path exists

### .NET 8 Runtime Check
- [ ] Installer detects .NET 8 presence/absence
- [ ] If missing: Dialog prompts to download
- [ ] Download link opens: https://dotnet.microsoft.com/download/dotnet/8.0
- [ ] Installation can continue without .NET (with warning)
- [ ] If present: Check passes silently

### Visual C++ Redistributable
- [ ] Installer checks for VC++ 2015-2022
- [ ] If missing: Prompts to download
- [ ] Download link works
- [ ] Installation can continue without it

### Installation Progress
- [ ] Progress bar updates smoothly
- [ ] Step indicators show progress:
  - [ ] Checking .NET 8 Runtime
  - [ ] Configuring Windows registry
  - [ ] Registering file associations
  - [ ] Configuring Windows Firewall
  - [ ] Adding Windows Defender exclusion
  - [ ] Creating user data directories
  - [ ] Copying application files
  - [ ] Creating shortcuts
  - [ ] Refreshing shell
- [ ] No errors during installation
- [ ] Estimated time reasonable (2-5 minutes)
- [ ] Installation completes successfully

### Completion Screen
- [ ] Success message displays
- [ ] "Launch Aura Video Studio" checkbox available
- [ ] "Finish" button works
- [ ] "Visit website" link works (optional)

## Post-Installation Phase

### File System Validation
- [ ] Run: `.\scripts\validate-installation.ps1`
- [ ] Installation directory exists: `C:\Program Files\Aura Video Studio`
- [ ] Main executable exists: `Aura Video Studio.exe`
- [ ] Backend exists: `resources\backend\win-x64\Aura.Api.exe`
- [ ] Frontend exists: `resources\frontend\index.html`
- [ ] FFmpeg exists: `resources\ffmpeg\win-x64\bin\ffmpeg.exe`
- [ ] ASAR bundle exists: `resources\app.asar`
- [ ] All required DLLs present

### Registry Validation
- [ ] Uninstall key exists:
  - Path: `HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\f6c6e9f0-8b1a-4e5a-9c2d-1e8f4a6b7c8d`
  - DisplayName: "Aura Video Studio"
  - DisplayVersion: "1.0.0"
  - Publisher: "Coffee285"
  - InstallLocation: Correct path
  - UninstallString: Points to uninstaller

### File Associations
- [ ] `.aura` extension registered
- [ ] `.avsproj` extension registered
- [ ] Registry keys exist in HKLM\Software\Classes
- [ ] Icon associated with file types
- [ ] Double-clicking .aura file launches app (if app installed)

### Shortcuts
- [ ] Desktop shortcut created: `Desktop\Aura Video Studio.lnk`
- [ ] Desktop shortcut icon correct
- [ ] Desktop shortcut launches app
- [ ] Start Menu shortcut: `Start Menu\Programs\Aura Video Studio.lnk`
- [ ] Start Menu search finds "Aura Video Studio"
- [ ] Start Menu shortcut launches app

### Windows Integration
- [ ] Windows Firewall rule created: "Aura Video Studio"
- [ ] Firewall rule allows inbound traffic
- [ ] Firewall rule is enabled
- [ ] Windows Defender exclusion added (if applicable)

### AppData Directories
- [ ] AppData directory exists: `%LOCALAPPDATA%\aura-video-studio`
- [ ] Logs directory created: `%LOCALAPPDATA%\aura-video-studio\logs`
- [ ] Cache directory created: `%LOCALAPPDATA%\aura-video-studio\cache`

## Application Testing Phase

### First Launch
- [ ] Launch from Start Menu
- [ ] Splash screen appears
- [ ] Backend process starts
- [ ] No error dialogs
- [ ] Main window appears
- [ ] First-run wizard displays
- [ ] UI is responsive

### Backend Verification
- [ ] Backend API accessible: http://localhost:5005
- [ ] Health check endpoint works: /health/live
- [ ] No crashes in backend logs
- [ ] Process visible in Task Manager

### Frontend Verification
- [ ] React app loads correctly
- [ ] No console errors (F12 Developer Tools)
- [ ] Navigation works
- [ ] UI elements display properly
- [ ] Settings accessible

### Core Features
- [ ] Can create new project
- [ ] Script generation works
- [ ] TTS synthesis available
- [ ] Voice selection works
- [ ] Video rendering accessible
- [ ] File save/load functions
- [ ] Settings persist after restart

### Performance
- [ ] Application starts in <10 seconds
- [ ] UI is responsive
- [ ] No memory leaks during extended use
- [ ] GPU detection works (if available)
- [ ] FFmpeg processes correctly

## Uninstallation Phase

### Uninstaller Launch
- [ ] Open Control Panel → Programs and Features
- [ ] "Aura Video Studio" appears in list
- [ ] Right-click → Uninstall
- [ ] Or: Run `Uninstall.exe` from installation directory
- [ ] UAC prompt (if applicable)
- [ ] Uninstaller window opens

### Uninstallation Process
- [ ] Warning about removing application
- [ ] Prompt: "Remove user data, settings, and cached files?"
- [ ] Option to keep or remove AppData
- [ ] Progress bar displays
- [ ] Uninstallation completes without errors
- [ ] Success message displays

### Cleanup Validation
- [ ] Run: `.\scripts\validate-uninstallation.ps1`
- [ ] Installation directory removed
- [ ] Registry keys removed (uninstall, file associations)
- [ ] Desktop shortcut removed
- [ ] Start Menu shortcut removed
- [ ] Windows Firewall rule removed
- [ ] Windows Defender exclusion removed
- [ ] Temporary files removed: `%TEMP%\aura-video-studio`

### User Data Preservation
- [ ] If "Remove data" selected:
  - [ ] AppData directory removed: `%LOCALAPPDATA%\aura-video-studio`
- [ ] If "Keep data" selected:
  - [ ] AppData directory preserved
- [ ] User documents preserved: `%USERPROFILE%\Documents\Aura Video Studio`

## Edge Cases & Error Scenarios

### Installation Failures
- [ ] Test install to path with spaces
- [ ] Test install to non-default drive (D:\, E:\)
- [ ] Test install with insufficient disk space
- [ ] Test install without admin rights (should fail gracefully)
- [ ] Test install over existing installation

### Missing Dependencies
- [ ] Test without .NET 8 Runtime (should prompt)
- [ ] Test without VC++ Redistributable (should prompt)
- [ ] Test with blocked internet (downloads fail, install continues)

### Upgrade Scenarios
- [ ] Install version 1.0.0
- [ ] Install version 1.0.1 over it (if available)
- [ ] Settings preserved
- [ ] User data preserved

### Concurrent Operations
- [ ] Cannot install while app is running
- [ ] Cannot uninstall while app is running
- [ ] Multiple install attempts (should fail gracefully)

## Security Testing

### Code Signing
- [ ] Installer is signed (if certificate available)
- [ ] Signature valid and trusted
- [ ] No security warnings from SmartScreen
- [ ] Certificate chain verifies
- [ ] Or: Warning appears if unsigned (expected)

### Permissions
- [ ] Installer requires admin (elevation prompt)
- [ ] Application runs with standard user rights
- [ ] No unnecessary privilege escalation
- [ ] Files installed to protected directory (Program Files)

### Antivirus Compatibility
- [ ] Windows Defender allows installation
- [ ] No false positive detections
- [ ] Application runs after Windows Defender scan
- [ ] Exclusion works if configured

## Automation Testing

### CI/CD Pipeline
- [ ] GitHub Actions workflow runs: `.github/workflows/build-windows-installer.yml`
- [ ] All build steps succeed
- [ ] Artifacts uploaded
- [ ] Checksums generated
- [ ] Release created (if tag pushed)

### Automated Tests
- [ ] Run: `.\scripts\test-installation-e2e.ps1 -InstallerPath "<path>" -Silent`
- [ ] All automated checks pass
- [ ] Exit code 0 (success)
- [ ] Log output shows no failures

## Documentation Review

### User-Facing Documentation
- [ ] BUILD_INSTRUCTIONS.md accurate
- [ ] WINDOWS_11_TESTING_GUIDE.md comprehensive
- [ ] INSTALLER_VALIDATION_REPORT.md complete
- [ ] Installation instructions clear

### Developer Documentation
- [ ] package.json configuration documented
- [ ] installer.nsh script commented
- [ ] Build scripts have help text
- [ ] Validation scripts documented

## Final Validation

### Production Readiness
- [ ] All critical checks passed
- [ ] No high-priority issues
- [ ] Documentation complete
- [ ] Validation scripts work
- [ ] CI/CD pipeline stable

### Sign-Off
- [ ] Lead developer approval: ___________
- [ ] QA approval: ___________
- [ ] Date tested: ___________
- [ ] Windows version: ___________
- [ ] Installer version: ___________
- [ ] Notes: ___________

---

## Quick Test Commands

```powershell
# Validate build configuration
cd Aura.Desktop
node scripts/validate-build-config.js

# Build installer
.\build-desktop.ps1

# Test installation (automated)
.\scripts\test-installation-e2e.ps1 -InstallerPath "dist\Aura-Video-Studio-Setup-1.0.0.exe" -Silent

# Validate installation
.\scripts\validate-installation.ps1

# Validate uninstallation
.\scripts\validate-uninstallation.ps1
```

## Pass Criteria

**Minimum requirements for release:**
- ✅ All installation steps complete without errors
- ✅ All required files present after installation
- ✅ Application launches successfully
- ✅ Core features functional
- ✅ Uninstallation removes all files
- ✅ No leftover registry entries
- ✅ Validation scripts pass

**Nice to have:**
- Code signing certificate applied
- Windows Defender exclusion works
- Hardware acceleration detected
- Update mechanism functional
