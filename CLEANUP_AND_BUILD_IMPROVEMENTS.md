# Clean Desktop and Build Scripts Improvements

## Overview
This document describes improvements made to the cleanup and build scripts for Aura Video Studio to ensure complete application removal and reliable builds after cleanup.

## Changes Summary

### 1. Enhanced AppData Cleanup (clean-desktop.ps1 & clean-desktop.sh)

#### Problem
The original `clean-desktop.ps1` script only cleaned the Local AppData directory (`%LOCALAPPDATA%\aura-video-studio`), leaving behind configuration and cache files in the Roaming AppData directory (`%APPDATA%\aura-video-studio`). This resulted in incomplete cleanup where some application data persisted after running the script.

#### Solution
Extended cleanup to include **all** AppData locations:

**New cleanup locations:**
- `%LOCALAPPDATA%\aura-video-studio` (existing)
- `%LOCALAPPDATA%\Aura Video Studio` (existing, case variation)
- `%APPDATA%\aura-video-studio` (**NEW** - Roaming)
- `%APPDATA%\Aura Video Studio` (**NEW** - Roaming, case variation)

**Why this matters:**
- Electron applications can store configuration in both Local and Roaming AppData
- The `electron-store` package may use Roaming AppData
- Windows system caches may persist in Roaming
- Roaming profiles sync across machines, so incomplete cleanup could cause issues

#### Updated Help Text
The help output now clearly shows both Local and Roaming cleanup:
```
What gets cleaned:
  • AppData configuration and cache:
    - %LOCALAPPDATA%\aura-video-studio
    - %APPDATA%\aura-video-studio (Roaming)
  • First-run wizard state (database and localStorage)
  • ...
```

### 2. Improved Build Dependency Verification (build-desktop.ps1 & build-desktop.sh)

#### Problem
After running `clean-desktop.ps1` or in a fresh repository clone, the build scripts could fail if:
- `node_modules` existed but was incomplete or corrupted
- Critical build dependencies were missing
- Package installations were interrupted previously

The original scripts only checked if `node_modules` directory existed, assuming all dependencies were present if it did.

#### Solution
Implemented **critical dependency verification** before build:

**Frontend critical dependencies checked:**
- `vite` - Build tool
- `react` - UI framework
- `typescript` - Type system

**Electron/Desktop critical dependencies checked:**
- `electron` - Desktop framework
- `electron-builder` - Installer builder
- `electron-store` - Configuration storage

**New behavior:**
1. If `node_modules` doesn't exist → Run `npm install`
2. If `node_modules` exists → Verify critical packages
3. If any critical package missing → Run `npm install` automatically
4. If all critical packages present → Continue with build
5. If `npm install` fails → Exit with clear error message

#### Benefits
- **Automatic recovery** from incomplete installations
- **Prevents build failures** due to missing dependencies
- **Works reliably** after running clean-desktop script
- **Better error messages** when installation fails
- **No manual intervention** required for missing dependencies

### 3. Cross-Platform Consistency

Both PowerShell (`.ps1`) and Bash (`.sh`) scripts received the same improvements, ensuring consistent behavior across:
- Windows (PowerShell native)
- Linux/macOS (via PowerShell Core or Bash)
- Development containers

## Testing

All changes have been validated:
- ✅ PowerShell syntax validation
- ✅ Bash syntax validation  
- ✅ Help text displays correctly
- ✅ Dry run executes without errors
- ✅ Dependency verification logic works
- ✅ Script wrappers function correctly

## Usage Examples

### Clean Everything (Including Roaming AppData)
```powershell
# Windows PowerShell
.\Aura.Desktop\clean-desktop.ps1

# Linux/macOS
./Aura.Desktop/clean-desktop.sh
```

### Preview What Will Be Cleaned
```powershell
# Dry run mode - shows what would be removed
.\Aura.Desktop\clean-desktop.ps1 -DryRun
```

### Build After Cleanup
```powershell
# The build script will automatically install any missing dependencies
.\Aura.Desktop\build-desktop.ps1

# Or with Bash
./Aura.Desktop/build-desktop.sh
```

## Implementation Details

### Code Changes

**clean-desktop.ps1** (lines 176-203):
```powershell
# Clean LocalAppData directories
$appDataPath = "$env:LOCALAPPDATA\aura-video-studio"
if (Remove-PathSafely $appDataPath "Main application data (LocalAppData)") {
    $cleanupStats.Removed++
}

# Clean Roaming AppData directories (NEW)
$roamingAppDataPath = "$env:APPDATA\aura-video-studio"
if (Remove-PathSafely $roamingAppDataPath "Application data (Roaming)") {
    $cleanupStats.Removed++
}
```

**build-desktop.ps1** (lines 150-178):
```powershell
if (-not (Test-Path "node_modules")) {
    npm install
} else {
    # Verify critical dependencies exist (NEW)
    $criticalPackages = @("vite", "react", "typescript")
    $missingPackages = @()
    
    foreach ($package in $criticalPackages) {
        if (-not (Test-Path "node_modules\$package")) {
            $missingPackages += $package
        }
    }
    
    if ($missingPackages.Count -gt 0) {
        Write-Info "Critical dependencies missing, reinstalling..."
        npm install
    }
}
```

## Related Files Modified
- `Aura.Desktop/clean-desktop.ps1`
- `Aura.Desktop/clean-desktop.sh` (wrapper, uses .ps1)
- `Aura.Desktop/build-desktop.ps1`
- `Aura.Desktop/build-desktop.sh`

## Impact

### For Users
- **Complete cleanup**: No leftover configuration files
- **Reliable builds**: Builds work after cleanup without manual intervention
- **Better experience**: Less troubleshooting needed

### For Developers
- **Cleaner testing**: True "first run" experience after cleanup
- **Fewer build issues**: Automatic dependency recovery
- **Time saved**: No manual `npm install` after cleanup

## Verification

To verify the improvements work correctly:

1. **Test cleanup includes Roaming:**
   ```powershell
   .\clean-desktop.ps1 -DryRun
   # Look for "Application data (Roaming)" in output
   ```

2. **Test dependency verification:**
   ```powershell
   # Create incomplete node_modules
   mkdir Aura.Desktop\node_modules
   
   # Run build - should detect missing packages and install
   .\build-desktop.ps1 -SkipBackend -SkipInstaller
   # Look for "Critical dependencies missing" message
   ```

## Conclusion

These improvements ensure that:
1. ✅ **Complete cleanup**: All AppData locations (Local and Roaming) are cleaned
2. ✅ **Reliable builds**: Dependencies are automatically verified and installed
3. ✅ **Better UX**: Users get clear feedback and automatic recovery

Both requirements from the issue have been fully addressed.
