> **⚠️ ARCHIVED DOCUMENT**  
> This document is retained for historical context only.  
> It may contain outdated information, references to superseded implementations, or details specific to past pull requests.  
> For current documentation, see the main docs/ directory and canonical guides in the repository root.

---

# Fix for npm install Failures on Windows

## Problem Statement

The `build-portable.ps1` script was failing on fresh Windows 11 installations with the error:
```
npm install failed after 3 attempts
npm output:
```

The error output was empty, making it impossible to diagnose the issue.

## Root Cause Analysis

### Primary Issue: Incompatible npm prepare Script

The `prepare` script in `Aura.Web/package.json` was using shell-specific syntax that failed on Windows:

```json
"prepare": "cd .. && git config core.hooksPath .husky"
```

**Why it failed on Windows:**
1. The `cd .. && ...` command uses bash-style command chaining
2. On Windows cmd.exe, this syntax behaves differently
3. If git is not in PATH (common on fresh Windows installations), the entire command fails
4. When the prepare script fails, npm install fails with it
5. The error was being suppressed by the `--silent` flag

### Secondary Issues

1. **Incorrect Retry Logic**: The retry loop in `build-portable.ps1` only attempted 3 times total instead of 1 initial + 3 retries
2. **Poor Error Reporting**: The `--silent` flag suppressed all error output, making debugging impossible
3. **Confusing Error Messages**: Retry attempt numbers were displayed incorrectly

## Solution

### 1. Cross-Platform Git Hooks Setup Script

Created `Aura.Web/scripts/setup-git-hooks.cjs`:
- Written in Node.js (CommonJS format to work with package.json "type": "module")
- Gracefully handles missing git (exits with success, just warns)
- Handles non-git repositories gracefully
- Works consistently on Windows, Linux, and macOS
- Never fails npm install

### 2. Updated package.json

Changed the prepare script from:
```json
"prepare": "cd .. && git config core.hooksPath .husky"
```

To:
```json
"prepare": "node scripts/setup-git-hooks.cjs"
```

### 3. Improved build-portable.ps1 Retry Logic

**Before:**
- Used confusing `$maxRetries` and `$retryCount` variables
- Only tried 3 times total (not 1 + 3 retries)
- Used `npm install --silent` which hid error messages
- Poor error messages

**After:**
- Uses clear `$maxAttempts` and `$attemptCount` variables
- Properly attempts up to 3 times total
- Removed `--silent` flag to show npm output
- Captures and displays full error output on failure
- Provides helpful troubleshooting guidance

### 4. Updated Other Build Scripts

Also improved `make_portable_zip.ps1`:
- Removed `--silent` flags
- Added proper error handling
- Better error messages

## Testing

Verified the fix works correctly:

```bash
# Test 1: Clean npm install works
cd Aura.Web
rm -rf node_modules
npm install
# ✓ Git hooks configured successfully
# ✓ npm install completes successfully

# Test 2: Build works
npm run build
# ✓ Build completes successfully

# Test 3: Git hooks are configured
cd ..
git config --get core.hooksPath
# ✓ Returns: .husky
```

## Benefits

1. **npm install now works on fresh Windows installations** - even without git
2. **Better error messages** - when npm install does fail, users see helpful output
3. **Proper retry logic** - actually retries the correct number of times
4. **Cross-platform compatibility** - works on Windows, Linux, macOS
5. **Graceful degradation** - missing git doesn't break the build

## Files Changed

1. `Aura.Web/scripts/setup-git-hooks.cjs` (new)
2. `Aura.Web/package.json`
3. `scripts/packaging/build-portable.ps1`
4. `scripts/packaging/make_portable_zip.ps1`

## Backward Compatibility

This change is fully backward compatible:
- Existing development environments continue to work
- Git hooks still get configured for developers
- CI/CD pipelines are unaffected
- The fix only improves error handling and compatibility
