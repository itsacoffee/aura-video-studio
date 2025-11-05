# E2E Testing Guide

Comprehensive guide for end-to-end testing in Aura Video Studio, including test scenarios, flake control, and CI integration.

## Table of Contents

- [Overview](#overview)
- [Test Infrastructure](#test-infrastructure)
- [Running Tests](#running-tests)
- [Test Categories](#test-categories)
- [Flaky Test Management](#flaky-test-management)
- [Hermetic Testing](#hermetic-testing)
- [CI Integration](#ci-integration)
- [Writing New Tests](#writing-new-tests)
- [Troubleshooting](#troubleshooting)

## Overview

Aura Video Studio's E2E testing infrastructure provides comprehensive coverage of critical user flows with support for:

- **Windows + Linux execution** - Cross-platform test coverage
- **Flaky test quarantine** - Isolation and tracking of unstable tests
- **Artifact retention** - 30-90 day retention for test reports and videos
- **Retry strategies** - Provider-specific retry logic with exponential backoff
- **Hermetic testing** - Reproducible tests with no external dependencies
- **CI gates** - Red/green gates for critical flows

## Test Infrastructure

### Frontend (Playwright)

**Location**: `Aura.Web/tests/e2e/`

**Framework**: Playwright with TypeScript

**Key Features**:
- Multiple browser projects (chromium, chromium-headless)
- Quarantine support for flaky tests
- Video recording and trace capture on failure
- JSON, HTML, and JUnit reporting

### Backend (xUnit)

**Location**: `Aura.E2E/`

**Framework**: xUnit with FluentAssertions

**Key Features**:
- Complete pipeline integration tests
- Provider validation tests
- Hardware detection tests
- Smoke tests

### Test Helpers

**Location**: `Aura.Web/tests/e2e/helpers/`

**Utilities**:
- `syntheticDataGenerator.ts` - Generate test briefs, scripts, and edge cases
- `retryStrategies.ts` - Retry logic, circuit breaker, rate limiter

## Running Tests

### Quick Start

```bash
# Install dependencies (first time only)
cd Aura.Web
npm ci
npm run playwright:install

# Run all E2E tests
npm run playwright

# Run tests in UI mode (interactive)
npm run playwright:ui
```

### Specific Test Suites

```bash
# Complete workflow tests
npx playwright test tests/e2e/complete-workflow.spec.ts

# SSE progress monitoring
npx playwright test tests/e2e/sse-progress-monitoring.spec.ts

# Job lifecycle management
npx playwright test tests/e2e/job-lifecycle-management.spec.ts

# Export manifest and licensing
npx playwright test tests/e2e/export-manifest-licensing.spec.ts
```

### Platform-Specific Tests

```bash
# Run on Windows with headed browser
npx playwright test --project=chromium

# Run on Linux with headless browser
npx playwright test --project=chromium-headless

# Run quarantined tests
npx playwright test --project=quarantine
```

### Backend E2E Tests

```bash
# From repository root
dotnet test Aura.E2E/Aura.E2E.csproj

# Run specific test
dotnet test Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~CompleteWorkflow"
```

## Test Categories

### 1. Complete Workflow Tests

**File**: `complete-workflow.spec.ts`

**Coverage**:
- Full user journey from brief creation to video export
- Wizard navigation (forward/backward)
- Form validation
- Data persistence across page reloads
- Real-time progress updates
- Job cancellation

**Example**:
```typescript
test('should complete full workflow from brief to video export', async ({ page }) => {
  await page.goto('/create');
  await page.getByPlaceholder(/Enter your video topic/i).fill('Getting Started');
  await page.getByRole('button', { name: /Next/i }).click();
  // ... complete flow
});
```

### 2. SSE Progress Monitoring Tests

**File**: `sse-progress-monitoring.spec.ts`

**Coverage**:
- SSE connection establishment
- Reconnection with Last-Event-ID
- Keep-alive pings
- Progress updates for all phases
- Warning event handling
- Connection cleanup

**Example**:
```typescript
test('should establish SSE connection and receive progress updates', async ({ page }) => {
  await page.route('**/api/jobs/*/events', (route) => {
    const events = generateSSEEventStream([
      { event: 'job-status', data: { status: 'Running', progress: 50 } },
      { event: 'job-completed', data: { status: 'Done', progress: 100 } }
    ]);
    route.fulfill({ body: events });
  });
});
```

### 3. Job Lifecycle Management Tests

**File**: `job-lifecycle-management.spec.ts`

**Coverage**:
- Job cancellation with confirmation
- Cleanup of temporary files
- Queue listing and filtering
- Job retry after transient failure
- Pause and resume functionality
- Concurrent job handling

**Example**:
```typescript
test('should cancel a running job successfully', async ({ page }) => {
  await page.route('**/api/jobs/*/cancel', (route) => {
    route.fulfill({ body: JSON.stringify({ message: 'Job cancelled' }) });
  });
  
  await page.getByRole('button', { name: /Cancel/i }).click();
  await expect(page.getByText(/Cancelled/i)).toBeVisible();
});
```

### 4. Export Manifest and Licensing Tests

**File**: `export-manifest-licensing.spec.ts`

**Coverage**:
- Complete manifest generation
- Licensing information for all components
- Attribution notices for third-party content
- Subtitle file format validation
- ZIP archive creation
- Checksum verification

**Example**:
```typescript
test('should generate complete export manifest', async ({ page }) => {
  await page.route('**/api/export/*/manifest', (route) => {
    route.fulfill({
      body: JSON.stringify({
        artifacts: { video: 'video.mp4', subtitles: ['srt', 'vtt'] },
        licenses: [{ component: 'FFmpeg', license: 'LGPL' }]
      })
    });
  });
});
```

## Flaky Test Management

### Quarantine System

Tests that exhibit flaky behavior should be quarantined:

1. **Rename test file**: `mytest.spec.ts` → `mytest.quarantine.spec.ts`
2. **Tests run separately**: Quarantined tests run with higher retries
3. **Track over time**: Monitor quarantine tests in CI

### Running Quarantined Tests

```bash
# Run only quarantined tests
npx playwright test --project=quarantine

# Run with extra debugging
npx playwright test --project=quarantine --trace on --video on
```

### Flake Detection Workflow

The `e2e-comprehensive.yml` workflow includes a flake detection job that:

- Runs on schedule (nightly) or manual trigger
- Executes each test 10 times
- Analyzes pass/fail rates
- Generates flake report with problematic tests

**View flake reports**:
1. Go to GitHub Actions
2. Find "E2E Comprehensive Tests" workflow
3. Look for "flake-detection-report" artifact

### Addressing Flaky Tests

**Common causes**:
- Race conditions (use explicit waits)
- Timing dependencies (add retries)
- External API dependencies (use mocks)
- Animation/transition timing (disable animations)

**Solutions**:
```typescript
// ❌ BAD - Race condition
await page.click('button');
expect(page.locator('.result')).toBeVisible();

// ✅ GOOD - Explicit wait
await page.click('button');
await expect(page.locator('.result')).toBeVisible({ timeout: 5000 });

// ✅ GOOD - Wait for network idle
await page.goto('/page', { waitUntil: 'networkidle' });
```

## Hermetic Testing

### Test Configurations

**Location**: `samples/e2e-test-configs/`

Hermetic test configurations provide reproducible test scenarios without external dependencies:

```json
{
  "name": "Basic Tutorial Test Config",
  "brief": {
    "topic": "Getting Started",
    "audience": "New Users",
    "goal": "Tutorial"
  },
  "providers": {
    "script": { "name": "RuleBased", "offline": true },
    "tts": { "name": "WindowsTTS", "offline": true },
    "visuals": { "name": "Stock", "offline": true }
  }
}
```

### Using Test Configs

```typescript
import * as config from '../../samples/e2e-test-configs/basic-tutorial.json';

test('should generate video from config', async ({ page }) => {
  await page.route('**/api/jobs', (route) => {
    route.fulfill({
      body: JSON.stringify({
        jobId: 'test-123',
        brief: config.brief
      })
    });
  });
});
```

### Synthetic Data Generation

Use `syntheticDataGenerator.ts` for test data:

```typescript
import { 
  generateSyntheticBrief, 
  generateEdgeCaseScenarios,
  generateBatchJobs 
} from './helpers/syntheticDataGenerator';

// Generate random brief with seed for reproducibility
const brief = generateSyntheticBrief(42);

// Get edge cases
const edgeCases = generateEdgeCaseScenarios();

// Generate batch for concurrent testing
const jobs = generateBatchJobs(10, 42);
```

## CI Integration

### Workflows

**E2E Comprehensive Tests** (`.github/workflows/e2e-comprehensive.yml`):

**Jobs**:
1. **e2e-windows** - Windows runners with headed browser
2. **e2e-linux** - Linux runners with headless browser
3. **flake-detection** - Runs tests 10 times to detect flakes
4. **test-summary** - Aggregates results and posts PR comment

**Triggers**:
- Push to main/develop
- Pull requests to main/develop
- Nightly schedule (4 AM UTC)
- Manual workflow dispatch

**Artifacts**:
- Playwright reports (30 days)
- Test results (30 days)
- API logs (7 days)
- Flake detection report (90 days)

### CI Gates

Tests must pass on **both Windows and Linux** before PR can be merged:

- ✅ Complete workflow tests
- ✅ SSE progress monitoring
- ✅ Job lifecycle management
- ✅ Export manifest tests
- ✅ All other E2E scenarios

### Artifact Inspection

After CI runs:

1. Go to workflow run in GitHub Actions
2. Scroll to "Artifacts" section
3. Download relevant artifacts:
   - `playwright-report-windows/linux` - HTML test report
   - `test-results-windows/linux` - Raw test results
   - `api-logs-windows/linux` - Backend logs
   - `flake-detection-report` - Flake analysis

## Writing New Tests

### Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    // Setup common mocks
    await page.route('**/api/settings', (route) => {
      route.fulfill({ body: JSON.stringify({ offlineMode: false }) });
    });
  });

  test('should do something specific', async ({ page }) => {
    // Arrange: Setup mocks and state
    await page.route('**/api/endpoint', (route) => {
      route.fulfill({ body: JSON.stringify({ data: 'test' }) });
    });

    // Act: Perform user actions
    await page.goto('/page');
    await page.click('button');

    // Assert: Verify expected outcome
    await expect(page.locator('.result')).toHaveText('Expected');
  });
});
```

### Best Practices

1. **Use descriptive test names**
   ```typescript
   // ✅ GOOD
   test('should cancel job and clean up temporary files when cancel button clicked')
   
   // ❌ BAD
   test('cancel test')
   ```

2. **Mock external dependencies**
   ```typescript
   // Always mock API calls
   await page.route('**/api/**', (route) => {
     route.fulfill({ body: JSON.stringify({ /* mock data */ }) });
   });
   ```

3. **Use explicit waits**
   ```typescript
   // Wait for specific conditions
   await expect(page.locator('.result')).toBeVisible({ timeout: 5000 });
   await page.waitForLoadState('networkidle');
   ```

4. **Clean up after tests**
   ```typescript
   test.afterEach(async ({ page }) => {
     // Close any open dialogs, clear state, etc.
   });
   ```

5. **Use retry strategies for known issues**
   ```typescript
   import { retryProviderCall } from './helpers/retryStrategies';
   
   const result = await retryProviderCall('openai', async () => {
     return await callOpenAI();
   });
   ```

### Adding to CI

New test files are automatically picked up by CI. To quarantine a flaky test:

1. Rename to `*.quarantine.spec.ts`
2. Test runs in quarantine project with higher retries
3. Fix underlying issue
4. Rename back to `*.spec.ts`

## Troubleshooting

### Tests Failing Locally

**Check prerequisites**:
```bash
# Verify Node version
node --version  # Should be 18.0.0+

# Verify Playwright browsers installed
npx playwright --version
```

**Re-install browsers**:
```bash
npm run playwright:install
```

**Clear test artifacts**:
```bash
rm -rf test-results/ playwright-report/
```

### Tests Passing Locally, Failing in CI

**Common causes**:
- Timing differences (CI is slower)
- Missing environment variables
- Platform-specific behavior

**Solutions**:
- Increase timeouts for CI
- Use headless mode locally to match CI
- Check CI logs for specific errors

### SSE Tests Not Working

**Check backend is running**:
```bash
# Terminal 1: Start backend
cd Aura.Api
dotnet run

# Terminal 2: Run tests
cd Aura.Web
npm run playwright
```

**Verify SSE endpoint**:
```bash
curl -N http://localhost:5005/api/jobs/test-123/events
```

### Flaky Tests

**Diagnose with trace viewer**:
```bash
# Run test with trace
npx playwright test --trace on

# Open trace viewer
npx playwright show-trace test-results/trace.zip
```

**Common fixes**:
- Add explicit waits
- Disable animations
- Mock time-sensitive operations
- Use retry strategies

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [xUnit Documentation](https://xunit.net/)
- [Sample Test Configurations](samples/e2e-test-configs/README.md)
- [SSE Integration Testing Guide](SSE_INTEGRATION_TESTING_GUIDE.md)
- [Production Readiness Checklist](PRODUCTION_READINESS_CHECKLIST.md)

## Support

For issues with E2E tests:

1. Check [Troubleshooting](#troubleshooting) section
2. Review CI logs and artifacts
3. Search existing GitHub issues
4. Create new issue with:
   - Test name and file
   - Platform (Windows/Linux)
   - Error message
   - CI run link (if applicable)
