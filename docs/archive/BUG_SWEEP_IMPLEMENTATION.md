# Bug Sweep + Regression Tests Implementation Summary

## Overview

This implementation adds comprehensive regression tests to prevent common bugs from returning and enforces CI-level checks for placeholder markers (TODO, FIXME, etc.) in the codebase.

## Test Coverage Added

### Playwright E2E Tests (3 new scenarios)

#### 1. First-Run Wizard: Invalid → Fix → Validate → Success
**File:** `Aura.Web/tests/e2e/first-run-wizard.spec.ts`

Tests the complete flow when a user encounters validation failures and retries:
- Initial validation fails (e.g., missing API key)
- User sees fix actions
- User retries validation
- Validation succeeds
- User can proceed to next step

This prevents the "stuck wizard" bug where users couldn't retry after validation failures.

#### 2. Dependency Download with Network Failure and Repair
**File:** `Aura.Web/tests/e2e/dependency-download.spec.ts` (NEW)

Three comprehensive scenarios:
- **Success Path**: Download FFmpeg successfully with progress tracking
- **Network Failure → Repair**: Download fails → Show error → User clicks Retry → Success
- **Manual Installation**: Show offline instructions with SHA-256 checksums

This prevents download center bugs where network failures left users stuck.

#### 3. Quick Demo Full Lifecycle
**File:** `Aura.Web/tests/e2e/wizard.spec.ts`

Tests the complete Quick Demo flow:
- Start → Queued → Running → Progress updates → Complete → Artifacts available
- Simulates real job state transitions
- Validates progress tracking and success states

This prevents quick demo bugs where jobs would hang or not update properly.

### Vitest Unit Tests (19 new tests)

#### 4. Wizard State Machine Edge Cases (4 tests)
**File:** `Aura.Web/src/state/__tests__/onboarding.test.ts`

Edge cases covered:
- Retry after validation failure (errors cleared, status reset)
- State preservation across step changes
- Install failure recovery (returns to idle state)
- Multiple validation attempts with correlationId tracking

These prevent state machine bugs that could leave the wizard in invalid states.

#### 5. Engine State Management (15 tests)
**File:** `Aura.Web/src/state/__tests__/engines.test.ts` (NEW)

Complete state machine testing for engine lifecycle:
- **Starting**: idle → starting → running transitions
- **Progress Tracking**: Multiple progress updates, reset on stop
- **Stopping**: running → stopping → stopped transitions
- **Error Handling**: Errors from any state, recovery after error
- **Complete Lifecycle**: Full happy path and error recovery paths
- **Edge Cases**: Ignore actions for different engine IDs, rapid state changes

This prevents engine state bugs where engines would report incorrect status or get stuck.

### .NET Unit Tests (13 new tests)

#### 6. ProviderMixer Never-Throw Guarantees (6 tests)
**File:** `Aura.Tests/ProviderMixerTests.cs`

Tests that ensure ProviderMixer never throws exceptions:
- Empty provider dictionary → Returns guaranteed fallback (RuleBased/Windows/Slideshow)
- All profile types handled without throwing
- Recovery from errors in provider dictionary
- Fallback chains work correctly

This prevents runtime exceptions when providers are unavailable.

#### 7. EngineInstaller Verify/Repair Workflows (7 tests)
**File:** `Aura.Tests/EngineInstallerTests.cs`

Installation workflow tests:
- Detect corrupted files gracefully
- Repair missing files (reinstall workflow)
- Resume partial installations
- Handle multiple missing files
- Verify after successful install
- Verify after removal

This prevents installer bugs where partial/corrupted installations couldn't be recovered.

## CI/CD Integration

### Windows CI (`ci-windows.yml`)

Added explicit grep step that fails if any of these patterns are found in source code:
- `TODO`
- `FIXME`
- `FUTURE`
- `NEXT STEPS`
- `OPTIONAL ENHANCEMENTS`

Uses PowerShell script that scans all `.cs`, `.ts`, `.tsx`, `.js`, `.jsx` files.

### Linux CI (`ci-linux.yml`)

Same grep check using bash, ensuring consistent enforcement across platforms.

## Code Cleanup

Removed the only TODO found in the codebase:
- **File:** `Aura.Web/src/components/Generation/GenerationPanel.tsx`
- **Change:** Replaced TODO comment with descriptive comment explaining current behavior

## Test Results

### Before This PR
- Vitest: 109 tests
- Aura.Tests: 498 tests
- Total: 607 tests

### After This PR
- Vitest: **128 tests** (+19 new)
- Aura.Tests: **511 tests** (+13 new)
- Total: **639 tests** (+32 new)
- **Pass Rate: 100%**

## Bug Prevention

These tests prevent the following bug categories from returning:

1. **Wizard State Machine Bugs**
   - Stuck buttons after validation failure
   - Lost state when navigating between steps
   - Invalid state transitions

2. **Engine State Bugs**
   - Incorrect progress reporting
   - Failed error recovery
   - Engine status stuck in transient states

3. **Provider Fallback Bugs**
   - Runtime exceptions when providers unavailable
   - Missing guaranteed fallbacks
   - Incorrect fallback chains

4. **Installer Bugs**
   - Corrupted installations not detected
   - Failed repairs leaving system in bad state
   - Unable to resume partial downloads

5. **Download Center Bugs**
   - Network failures without retry option
   - Missing repair/verify functionality
   - No offline installation path

6. **Quick Demo Bugs**
   - Jobs hanging in intermediate states
   - Missing progress updates
   - No completion notification

## How to Run Tests Locally

### Vitest Tests
```bash
cd Aura.Web
npm test
```

### .NET Tests
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --configuration Release
```

### Playwright E2E Tests
```bash
cd Aura.Web
npm run playwright
```

### Check for Placeholders
```bash
# Linux/Mac
grep -rn "TODO\|FIXME\|FUTURE\|NEXT STEPS\|OPTIONAL ENHANCEMENTS" \
  --include="*.cs" --include="*.ts" --include="*.tsx" \
  --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj .

# Windows (PowerShell)
pwsh -File scripts/audit/no_future_text.ps1
```

## CI Integration

Both CI workflows now have TWO checks for placeholders:
1. **Audit Script** (`no_future_text.ps1`) - Comprehensive scan with allowlists
2. **Explicit Grep** - Simple, fast check that fails immediately on any placeholder

This dual approach ensures:
- Fast feedback (grep step runs early)
- Comprehensive coverage (audit script checks patterns and context)
- Zero tolerance for new placeholders

## Migration Notes

### Breaking Changes
None - all changes are additive.

### New Dependencies
None - uses existing test infrastructure.

### Configuration Changes
None - CI workflows updated but no new configuration required.

## Future Maintenance

When adding new features, ensure:
1. Add regression tests for any bug fixes
2. Never commit TODO/FIXME markers in production code
3. Run full test suite before submitting PR
4. CI will automatically enforce both rules

## Summary

This PR implements a comprehensive testing strategy that:
- ✅ Prevents 6 categories of bugs from returning
- ✅ Adds 32 new automated tests (100% passing)
- ✅ Enforces zero-tolerance policy for placeholders
- ✅ Provides clear error messages when CI fails
- ✅ Requires minimal maintenance overhead

The repository is now significantly more resilient to regressions, and the CI pipeline will catch issues before they reach production.
