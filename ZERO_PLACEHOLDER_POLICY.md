# Zero-Placeholder Policy

## Policy Statement

**All code committed to this repository must be production-ready with no placeholder markers.**

This is the **#1 code quality rule** in the Aura Video Studio project. Violations will cause automated build failures.

## What is Prohibited

### Forbidden Comment Patterns

The following patterns are **strictly forbidden** in all code files (case-insensitive):

```typescript
// TODO: anything
// FIXME: anything  
// HACK: anything
// XXX: anything
// WIP: anything
/* TODO */ or /* FIXME */ or /* HACK */
```

Also forbidden in comments:
- "not implemented"
- "coming soon"
- "placeholder"

### Forbidden Commit Messages

The following keywords are **not allowed** in commit messages:
- TODO
- WIP
- FIXME
- "temp commit"
- "temporary"

## What is Allowed

### Documentation Files

All `.md` (Markdown) files can contain TODO lists and planning notes. These are documentation, not code.

### Test Fixtures

Test files that intentionally test date/time logic may have "future" in variable names.

### UI Placeholder Text

Actual rendered placeholders for user forms are allowed:
```typescript
<input placeholder="Enter your name" />
```

### Intentional Logging

Console statements for diagnostics are allowed if marked with `eslint-disable`:
```typescript
// eslint-disable-next-line no-console
console.log('Hardware acceleration available');
```

## Enforcement Mechanisms

### 1. Pre-commit Hook

**Location:** `.husky/pre-commit`

**Action:**
- Runs `scripts/audit/find-placeholders.js` on staged files
- Blocks commit if any placeholders found
- Can be bypassed with `--no-verify` (but CI will catch it)

### 2. Commit Message Hook

**Location:** `.husky/commit-msg`

**Action:**
- Rejects commit messages containing forbidden keywords
- Ensures professional commit messages

### 3. CI Workflows

**Primary:** `.github/workflows/no-placeholders.yml`  
**Secondary:** `.github/workflows/build-validation.yml` (Job 4)

**Action:**
- Runs placeholder scanner on all code files
- Fails build if any placeholders found
- Blocks PR merge

### 4. ESLint Rules

**Frontend:** ESLint configured with strict rules to catch placeholder-like patterns

## What to Do Instead

### If Feature is Incomplete

**❌ Don't:**
```typescript
// TODO: Implement hardware acceleration
async function render() {
  // Temporary implementation
  return null;
}
```

**✅ Do:**
- Finish the feature before committing, OR
- Remove the incomplete code from the commit, OR
- Create a GitHub Issue and reference it in a descriptive comment

### If Work is Deferred

**❌ Don't:**
```typescript
// FIXME: This breaks on large files
// Need to optimize later
```

**✅ Do:**
- Create a GitHub Issue: "Optimize large file handling"
- Reference the issue in code if needed:
```typescript
// Large file handling optimization tracked in issue #123
// Current implementation handles files up to 100MB
```

### If Explaining Current Behavior

**❌ Don't:**
```typescript
// HACK: Temporary workaround for bug
const result = data || defaultValue;
```

**✅ Do:**
```typescript
// Uses fallback approach for edge case handling
// See issue #456 for enhancement discussion
const result = data || defaultValue;
```

## Examples

### Before (Forbidden)

```typescript
// TODO: Add error handling
async function fetchData() {
  // FIXME: This needs proper validation
  const response = await fetch(url);
  // WIP: Parse response
  return response.json();
}
```

### After (Correct)

```typescript
// Fetches data from API with error handling and validation
// Enhancement tracked in issue #789: Add retry logic
async function fetchData() {
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    return await response.json();
  } catch (error: unknown) {
    const errorObj = error instanceof Error ? error : new Error(String(error));
    logger.error('Failed to fetch data:', errorObj);
    throw errorObj;
  }
}
```

## Philosophy

**All code must be production-ready when committed.**

No half-finished features, no deferred work in code comments. Create GitHub Issues for future enhancements, not code comments. This keeps the codebase clean, professional, and maintainable.

### Benefits

1. **Forces completion** - Features are either done or not included
2. **Clear issue tracking** - Future work tracked in GitHub Issues, not scattered comments
3. **Professional codebase** - No "work in progress" markers visible to users
4. **Better collaboration** - Team members can trust all committed code is complete
5. **Prevents technical debt** - No accumulation of deferred work

## Bypassing (Not Recommended)

### For Local Development

You can bypass pre-commit hooks locally:
```bash
git commit --no-verify
```

**Warning:** CI will still catch and reject the commit in the PR.

### For Emergency Hotfixes

If you absolutely must commit incomplete code:
1. Create a GitHub Issue documenting the incomplete work
2. Use descriptive comments (not TODO/FIXME)
3. Plan immediate follow-up to complete the work
4. Clearly mark in PR description

## Audit Results

As of November 1, 2025:

```
Total files scanned: 1,778
Files checked: 1,270
Placeholder markers found: 0

✅ Repository is clean
✅ Policy is enforced
✅ Automated checks are working
```

## Verification

To verify your code complies with the policy:

```bash
# Run placeholder scanner
node scripts/audit/find-placeholders.js

# Run full validation
cd Aura.Web && npm run validate:full
```

## References

- **Placeholder Scanner:** `scripts/audit/find-placeholders.js`
- **Pre-commit Hook:** `.husky/pre-commit`
- **Commit Message Hook:** `.husky/commit-msg`
- **CI Workflow:** `.github/workflows/no-placeholders.yml`
- **Project Guidelines:** `.github/copilot-instructions.md` (Section 3)

---

**Policy Established:** PR #144 (Build Validation)  
**Last Audit:** November 1, 2025  
**Status:** ✅ **ENFORCED**
