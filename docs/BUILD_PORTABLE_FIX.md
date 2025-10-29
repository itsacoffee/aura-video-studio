# build-portable.ps1 Fix Summary

## What You Experienced

When running `.\build-portable.ps1` on a fresh Windows 11 installation, you saw:

```
[3/6] Building web UI...
      Installing npm dependencies...
      Retry attempt 1 of 3...
      Retry attempt 2 of 3...
      ✗ ERROR: npm install failed after 3 attempts
npm output:

========================================
 Build Failed!
========================================
```

The error output was completely empty, making it impossible to diagnose.

## Root Cause

The issue was NOT in the PowerShell script itself, but in the **npm prepare script** that runs automatically after `npm install`.

The `Aura.Web/package.json` had this prepare script:
```json
"prepare": "cd .. && git config core.hooksPath .husky"
```

This failed on Windows because:
- Windows cmd.exe doesn't support the `&&` operator the same way as Unix shells (it exists but behaves differently in PowerShell contexts)
- The directory context and command chaining can fail silently
- If git is not in PATH (common on fresh Windows installs), the command fails
- When the prepare script fails, npm install fails with it

The error was also hidden by the `--silent` flag in the original script.

## Fix Applied

### 1. Fixed the npm prepare script (PRIMARY FIX)

Created `Aura.Web/scripts/setup-git-hooks.cjs`:
- Cross-platform Node.js script
- Gracefully handles missing git (warns but doesn't fail)
- Never causes npm install to fail

Updated `Aura.Web/package.json`:
```json
"prepare": "node scripts/setup-git-hooks.cjs"
```

### 2. Improved build-portable.ps1 error reporting

**Before:**
```powershell
$npmOutput = npm install --silent 2>&1
```

**After:**
```powershell
$npmOutput = npm install 2>&1  # Removed --silent
```

Now you'll see:
- Actual npm error messages
- What package failed to install
- Network error details
- Helpful troubleshooting suggestions

### 3. Fixed retry logic

**Before:** Only tried 3 times total (confusing count)

**After:** Tries up to 3 times total (1 initial + 2 retries if needed):
- Attempt 1: "Installing npm dependencies..."
- Attempt 2 (if needed): "Retry attempt 1 of 2..."
- Attempt 3 (if needed): "Retry attempt 2 of 2..."

## What Changed for You

When you now run `.\build-portable.ps1` on Windows:

1. **npm install will work** even if git is not in PATH
2. **If npm install does fail**, you'll see actual error messages explaining why
3. **Better retry logic** with clear attempt numbers
4. **Helpful error messages** suggesting what to check

## Testing

The fix has been tested and verified:
- ✅ npm install works with new prepare script
- ✅ Build completes successfully
- ✅ Error output is now visible when failures occur
- ✅ Retry logic works correctly

## Next Time You Build

Just run your normal command:
```powershell
.\scripts\packaging\build-portable.ps1
```

If npm install fails, you'll now see:
1. Actual npm error output (not empty)
2. Clear indication of which attempt failed
3. Helpful suggestions (check internet, npm config, Node version, disk space)

The most common issue on fresh Windows installs (the git hooks setup failing) should now be resolved.
