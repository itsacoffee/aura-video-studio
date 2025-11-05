# E2E Testing Guide

## Overview

This guide covers end-to-end testing for Aura Video Studio, including test scenarios, CI gates, flake control, and best practices.

## Table of Contents

- [Test Architecture](#test-architecture)
- [Running Tests](#running-tests)
- [Test Scenarios](#test-scenarios)
- [Flake Control](#flake-control)
- [CI Gates](#ci-gates)
- [Writing New Tests](#writing-new-tests)
- [Troubleshooting](#troubleshooting)

## Test Architecture

### Test Types

1. **Frontend E2E Tests** (Playwright)
   - Full workflow tests (Brief → Plan → Script → SSML → Assets → Render)
   - SSE progress tracking
   - Job management (creation, cancellation, monitoring)
   - Error handling and recovery
   
2. **Backend Integration Tests** (.NET)
   - Complete workflow validation
   - Provider integration
   - Pipeline execution
   - SSE event streams

3. **CLI Integration Tests**
   - Cross-platform command validation
   - Quick generation
   - Preflight checks

### Test Data

Test data is organized under `samples/test-data/`:

```
samples/test-data/
├── briefs/
│   └── synthetic-briefs.json      # LLM-generated test scenarios with edge cases
├── configs/
│   └── hermetic-test-config.json  # Isolated test configuration
└── fixtures/
    └── mock-responses.json        # Provider mock responses, SSE events, artifacts
```

**Synthetic Briefs** (`briefs/synthetic-briefs.json`):
- 18 test scenarios covering various content types and edge cases
- Edge cases: Unicode/emoji, special characters, extreme durations, line breaks
- Reliability tests: Provider retry, job cancellation, SSE reconnection
- Each brief includes expected complexity and scene count

**Mock Responses** (`fixtures/mock-responses.json`):
- Provider responses (LLM, TTS, visuals) with success and fallback scenarios
- SSE event streams for job progress and cancellation
- Export manifest with licensing information
- Error responses for testing retry logic

**Hermetic Config** (`configs/hermetic-test-config.json`):
- Offline-first provider configuration
- Mock FFmpeg with placeholder output
- Accelerated time for faster tests
- Retry policy for transient failures

## Running Tests

### Local Development

#### Frontend E2E Tests

```bash
cd Aura.Web

# Run all E2E tests
npm run playwright

# Run specific test file
npx playwright test tests/e2e/full-pipeline.spec.ts

# Run in UI mode (interactive)
npm run playwright:ui

# Run with specific browser
npx playwright test --project=chromium

# Debug mode
npx playwright test --debug
```

#### Backend Integration Tests

```bash
# Run all E2E tests
dotnet test Aura.E2E/Aura.E2E.csproj

# Run specific test class
dotnet test Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~CompleteWorkflow"

# With detailed output
dotnet test Aura.E2E/Aura.E2E.csproj --logger "console;verbosity=detailed"
```

#### CLI Tests

```bash
# Quick command test
dotnet run --project Aura.Cli -- quick -t "Test Video" -d 0.5 --dry-run -v

# Preflight check
dotnet run --project Aura.Cli -- preflight -v
```

### CI/CD Execution

E2E tests run automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Scheduled nightly runs (3 AM UTC)
- Manual workflow dispatch

## Test Scenarios

### Full Pipeline Test

**Location**: `Aura.Web/tests/e2e/full-pipeline.spec.ts`

**Coverage**:
- Complete video generation workflow
- Phase progression (Brief → Plan → Script → TTS → Visuals → Compose → Render)
- Artifact generation (video, subtitles, manifest)
- SSE progress tracking
- Job cancellation
- Error handling

**Expected Duration**: 60-120 seconds

**Success Criteria**:
- All phases complete in order
- Progress updates received via SSE
- Final artifacts generated
- Manifest includes licensing information

### Complete Workflow Test

**Location**: `Aura.Web/tests/e2e/complete-workflow.spec.ts`

**Coverage**:
- Wizard navigation
- Form validation
- Provider selection
- Preflight checks
- Generation execution

**Expected Duration**: 30-60 seconds

### SSE Progress Tracking Test

**Location**: `Aura.Web/tests/e2e/sse-progress-tracking.spec.ts`

**Coverage**:
- Real-time job progress via SSE events
- SSE reconnection with Last-Event-ID after network interruptions
- Connection error handling and retry
- Progress percentage accuracy validation

**Expected Duration**: 60-90 seconds

**Success Criteria**:
- All phase transitions tracked correctly
- Reconnection works with Last-Event-ID header
- Error handling graceful with retry logic
- Progress updates accurate (0-100%)

### Job Cancellation Test

**Location**: `Aura.Web/tests/e2e/job-cancellation.spec.ts`

**Coverage**:
- Job cancellation during execution
- Resource cleanup after cancellation
- Prevention of actions on cancelled jobs
- Phase-specific cancellation handling

**Expected Duration**: 60-90 seconds

**Success Criteria**:
- Jobs can be cancelled at any phase
- Cleanup removes temporary files
- Cancelled jobs cannot be resumed (validation error)
- Status updates correctly to "cancelled"

### Export Manifest Validation Test

**Location**: `Aura.Web/tests/e2e/export-manifest-validation.spec.ts`

**Coverage**:
- Manifest generation with complete metadata
- Licensing information validation
- Pipeline timing and phase duration tracking
- Artifact checksum validation
- Manifest download functionality

**Expected Duration**: 60-90 seconds

**Success Criteria**:
- Manifest includes all required metadata
- Licensing information present (LLM, TTS, visuals providers)
- Pipeline phases tracked with timing
- Commercial use rights documented
- Manifest downloadable as JSON file

### Backend Integration Tests

**Location**: `Aura.E2E/CompleteWorkflowTests.cs`

**Coverage**:
- Offline workflow with local providers
- Provider fallback and selection
- Hardware detection
- Script generation

**Expected Duration**: 5-15 seconds per test

## Flake Control

### Flake Tracking System

The flake tracker automatically monitors test stability and quarantines flaky tests.

**Location**: `Aura.Web/tests/utils/flake-tracker.ts`

#### Features

1. **Automatic Detection**
   - Tracks pass/fail rates per test
   - Calculates flake rate
   - Auto-quarantines tests with >30% failure rate (after 5 runs)

2. **Quarantine Mechanism**
   - Known flaky tests are marked and skipped
   - Quarantined tests tracked separately
   - Manual unquarantine available

3. **Reporting**
   - Generates flake reports after each run
   - Uploaded as CI artifacts
   - Tracked over time for trends

#### Using the Flake Tracker

```typescript
import { flakeTracker } from '../utils/flake-tracker';

test.afterEach(async ({}, testInfo) => {
  flakeTracker.recordTestResult(
    testInfo.title,
    testInfo.file,
    testInfo.status === 'passed'
  );
});

test('my test', async () => {
  // Skip if quarantined
  if (flakeTracker.isQuarantined('my test', 'test-file.spec.ts')) {
    test.skip();
  }
  
  // Test implementation
});
```

### Retry Strategies

#### Playwright Retry Configuration

```typescript
// playwright.config.ts
export default defineConfig({
  retries: process.env.CI ? 2 : 0,  // 2 retries in CI, 0 locally
  timeout: 60 * 1000,                // 60 seconds per test
  expect: {
    timeout: 10 * 1000,              // 10 seconds for assertions
  },
});
```

#### Handling Transient Failures

For known transient issues (provider timeouts, network glitches):

```typescript
test('test with retry logic', async ({ page }) => {
  // Use retry with exponential backoff
  await test.step('with retry', async () => {
    let attempts = 0;
    const maxAttempts = 3;
    
    while (attempts < maxAttempts) {
      try {
        await page.click('button');
        break;
      } catch (error) {
        attempts++;
        if (attempts >= maxAttempts) throw error;
        await page.waitForTimeout(1000 * attempts);
      }
    }
  });
});
```

## CI Gates

### Required Gates

All E2E tests must pass before merge:

1. **Windows E2E Tests** ✅
   - Full pipeline with real FFmpeg
   - Complete workflow validation
   - SSE and job management

2. **Linux E2E Tests (Headless)** ✅
   - Cross-platform validation
   - Headless browser testing
   - CLI integration

3. **Backend Integration Tests** ✅
   - Provider integration
   - Pipeline execution
   - SSE event streams

4. **CLI Integration Tests** ✅
   - Windows and Linux
   - Quick generation
   - Preflight validation

### Gate Configuration

**Location**: `.github/workflows/e2e-pipeline.yml`

**Fail Criteria**:
- Any critical test failure
- Flake rate above 50% on any test (warning, tracked but doesn't fail build)
- More than 10 quarantined tests (warning)
- Test timeout (45 minutes max for Windows, 30 minutes for Linux)

**Artifact Retention**:
- Test results: 30 days
- Screenshots/videos: 7 days (failures only)
- Flake reports: 90 days
- CLI artifacts: 7 days

**Flake Rate Thresholds**:
- **30%**: Auto-quarantine test after 5 runs
- **20%**: High flake warning (reported but not quarantined)
- **50%**: Critical flake warning (serious reliability issue)

**Test Retry Policy**:
- CI: 2 retries on failure
- Local: 0 retries (fail fast for quick feedback)
- Timeout: 60 seconds per test (configurable per test)

## Writing New Tests

### Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup: navigation, mocks, etc.
    await page.goto('/');
  });

  test('should do something', async ({ page }) => {
    // Set reasonable timeout
    test.setTimeout(60000);

    // Mock external dependencies
    await page.route('**/api/endpoint', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ data: 'mock' }),
      });
    });

    // Test implementation
    await page.click('button');
    await expect(page.getByText('Success')).toBeVisible();
  });
});
```

### Best Practices

1. **Use Hermetic Test Data**
   - Load from `samples/test-data/`
   - Mock external dependencies
   - Avoid real API calls

2. **Set Explicit Timeouts**
   - Default: 60 seconds
   - Adjust per test complexity
   - Use `test.setTimeout()`

3. **Mock Appropriately**
   - Mock external providers
   - Mock slow operations
   - Use realistic mock data

4. **Handle Asynchrony**
   - Use `await` consistently
   - Wait for network idle when needed
   - Use explicit waits over hardcoded delays

5. **Clean Up Resources**
   - Use `test.afterEach()` for cleanup
   - Remove test artifacts
   - Reset state

### Test Data Generation

Use synthetic briefs for comprehensive coverage:

```typescript
import syntheticBriefs from '../../../samples/test-data/briefs/synthetic-briefs.json';

test('should handle edge case brief', async ({ page }) => {
  const edgeCaseBrief = syntheticBriefs.briefs.find(
    b => b.tags?.includes('edge-case')
  );
  
  // Use in test
  await fillBriefForm(page, edgeCaseBrief);
});
```

## Troubleshooting

### Common Issues

#### Tests Timing Out

**Symptoms**: Tests fail with "Timeout of 60000ms exceeded"

**Solutions**:
- Increase test timeout: `test.setTimeout(120000)`
- Check if backend is running and responsive
- Review browser console for errors
- Check network requests in test artifacts

#### Flaky Tests

**Symptoms**: Tests pass sometimes, fail other times

**Solutions**:
- Add explicit waits: `await page.waitForSelector()`
- Increase assertion timeouts
- Mock time-sensitive operations
- Check flake tracker report for patterns

#### Mock Not Working

**Symptoms**: Real API calls happening instead of mocks

**Solutions**:
- Verify route pattern matches actual URL
- Check route is registered before navigation
- Use `route.continue()` for passthrough
- Check network tab in Playwright trace

#### Headless vs Headed Differences

**Symptoms**: Tests pass locally (headed) but fail in CI (headless)

**Solutions**:
- Run locally in headless: `npx playwright test --headed=false`
- Check for timing issues (headless is faster)
- Verify fonts and rendering differences
- Use `page.waitForLoadState('networkidle')`

### Debug Tools

#### Playwright Inspector

```bash
# Run with inspector
npx playwright test --debug

# Pause at specific point
await page.pause();
```

#### Trace Viewer

```bash
# Generate trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```

#### Video Recording

```bash
# Enable video
npx playwright test --video on

# Videos saved to test-results/
```

### Getting Help

1. Check test artifacts in CI
2. Review flake tracker reports
3. Check existing issues in GitHub
4. Consult [SSE Integration Testing Guide](SSE_INTEGRATION_TESTING_GUIDE.md)
5. Review [Production Readiness Checklist](PRODUCTION_READINESS_CHECKLIST.md)

## Continuous Improvement

### Monitoring Test Health

1. **Flake Rate**: Track weekly in CI artifacts
2. **Test Duration**: Monitor for performance regression
3. **Coverage**: Ensure new features have E2E tests
4. **Quarantine Queue**: Review monthly and fix/remove

### Adding New Scenarios

When adding new features:

1. Add E2E test covering happy path
2. Add edge case tests
3. Update synthetic briefs if needed
4. Document in this guide
5. Ensure CI gates pass

---

**Last Updated**: 2025-11-05  
**Maintainer**: Aura Development Team
