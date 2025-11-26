# Clean Script Enhancements - PR-002

## Overview
This document describes the enhancements made to the `clean-desktop.ps1` script as part of PR-002.

## Changes Summary

### 1. Additional Cleanup Locations (Step 3.5)
Added comprehensive cleanup for configuration locations that were previously not thoroughly cleaned:

**New directories cleaned:**
- `$env:LOCALAPPDATA\Aura\dependencies` - FFmpeg managed installs
- `$env:LOCALAPPDATA\Aura\Logs` - Log files
- `$env:LOCALAPPDATA\Aura\Cache` - Cache directory
- `$env:LOCALAPPDATA\AuraVideoStudio` - Alternative app data location
- `$env:APPDATA\AuraVideoStudio` - Roaming app data
- `$env:USERPROFILE\Documents\AuraVideoStudio` - User projects (conditional)

**Special Features:**
- Documents directory is skipped by default unless `-IncludeUserContent` flag is used
- All paths are properly expanded using environment variables
- Each path removal is tracked in cleanup statistics

### 2. Windows Registry Cleanup
Added cleanup for Windows Registry entries (PowerShell 5+ only):
- `HKCU:\Software\Aura`
- `HKCU:\Software\AuraVideoStudio`

**Features:**
- Only runs on Windows with PowerShell 5 or higher
- Respects `-DryRun` flag (shows what would be removed without actually removing)
- Proper error handling with warnings if registry keys cannot be removed
- Tracks successful removals in cleanup statistics

### 3. Cleanup Verification Report
Added a comprehensive verification report that displays after cleanup:

**Report includes:**
- Visual indicators (✓ for cleaned, ⚠️ for still exists)
- Verification of all key paths:
  - `$env:LOCALAPPDATA\Aura`
  - `$env:APPDATA\Aura`
  - `$env:LOCALAPPDATA\AuraVideoStudio`
  - `$env:APPDATA\AuraVideoStudio`
  - `$env:LOCALAPPDATA\aura-video-studio`
  - `$env:APPDATA\aura-video-studio`

### 4. NPM Script Integration
Updated `package.json` with new convenience scripts:

```json
{
  "scripts": {
    "clean": "pwsh -File ./clean-desktop.ps1",
    "clean:full": "pwsh -File ./clean-desktop.ps1 -IncludeUserContent",
    "prebuild": "npm run clean"
  }
}
```

**Usage:**
- `npm run clean` - Standard cleanup (preserves user content)
- `npm run clean:full` - Full cleanup including user content
- `prebuild` - Automatically runs before build to ensure clean state

## Usage Examples

### Standard Cleanup
```powershell
# Using PowerShell directly
.\clean-desktop.ps1

# Using npm script
npm run clean
```

### Dry Run (Preview)
```powershell
# See what would be removed without actually removing
.\clean-desktop.ps1 -DryRun
```

### Full Cleanup (Including User Content)
```powershell
# Using PowerShell directly
.\clean-desktop.ps1 -IncludeUserContent

# Using npm script
npm run clean:full
```

### Combined with Build Pipeline
```bash
# Clean before building
npm run clean && npm run build
```

## Output Example

```
========================================
Aura Video Studio - Desktop Cleanup
========================================

[INFO] Step 3.5: Cleaning additional configuration locations...

Additional cleanup locations:
[SUCCESS] Removed: $env:LOCALAPPDATA\Aura\dependencies
[SUCCESS] Removed: $env:LOCALAPPDATA\Aura\Logs
[SUCCESS] Removed: $env:LOCALAPPDATA\Aura\Cache
[SUCCESS] Removed: $env:LOCALAPPDATA\AuraVideoStudio
[SUCCESS] Removed: $env:APPDATA\AuraVideoStudio
  Skipping user content: $env:USERPROFILE\Documents\AuraVideoStudio (use -IncludeUserContent to remove)

Cleaning Windows Registry entries...
[SUCCESS] Removed registry key: HKCU:\Software\Aura
[SUCCESS] Removed registry key: HKCU:\Software\AuraVideoStudio

========================================
Cleanup Verification Report
========================================
  ✓ Cleaned: C:\Users\Username\AppData\Local\Aura
  ✓ Cleaned: C:\Users\Username\AppData\Roaming\Aura
  ✓ Cleaned: C:\Users\Username\AppData\Local\AuraVideoStudio
  ✓ Cleaned: C:\Users\Username\AppData\Roaming\AuraVideoStudio
  ✓ Cleaned: C:\Users\Username\AppData\Local\aura-video-studio
  ✓ Cleaned: C:\Users\Username\AppData\Roaming\aura-video-studio
```

## Benefits

1. **More Thorough Cleanup**: Removes all configuration and cache locations
2. **Better Visibility**: Verification report shows exactly what was cleaned
3. **User-Friendly**: Documents directory protected by default
4. **Registry Cleanup**: Removes Windows registry entries for complete cleanup
5. **Integration Ready**: NPM scripts allow easy integration into build pipeline
6. **Safe Testing**: Dry-run mode allows preview before actual cleanup

## Testing Checklist

- [x] Script removes all LocalAppData\Aura subdirectories
- [x] Script removes both Aura and AuraVideoStudio folders
- [x] Registry entries cleaned (Windows with PowerShell 5+)
- [x] User documents preserved by default
- [x] -IncludeUserContent flag removes project files
- [x] Verification report shows all paths cleaned
- [x] Dry-run mode accurately previews actions
- [x] NPM scripts work correctly
- [x] PowerShell syntax is valid

## Acceptance Criteria Met

✅ 1. Script thoroughly cleans all configuration locations found in codebase
✅ 2. Verification report confirms cleanup completeness
✅ 3. User content preserved unless explicitly requested
✅ 4. Script can be integrated into build pipeline via npm scripts
✅ 5. Dry-run mode accurately previews actions

## Related Files

- `Aura.Desktop/clean-desktop.ps1` - Main cleanup script
- `Aura.Desktop/package.json` - NPM scripts configuration
