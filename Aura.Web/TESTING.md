# Testing Guide for Aura.Web

This document describes the testing infrastructure and how to run tests for the Aura.Web frontend.

## Test Infrastructure

### Vitest (Unit & Integration Tests)
- **Framework**: Vitest with jsdom environment
- **Location**: `src/**/*.test.ts(x)`
- **Setup**: `src/test/setup.ts`
- **Coverage**: v8 provider with HTML/JSON/text reports

### Playwright (E2E Tests)
- **Framework**: Playwright Test
- **Location**: `tests/e2e/**/*.spec.ts`
- **Config**: `playwright.config.ts`
- **Browser**: Chromium (headless by default)

## Running Tests

### Unit Tests (Vitest)

```bash
# Run tests once
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with UI
npm run test:ui

# Run with coverage
npm run test:coverage
```

### E2E Tests (Playwright)

```bash
# Install Playwright browsers (first time only)
npm run playwright:install

# Run E2E tests
npm run playwright

# Run in UI mode (interactive)
npm run playwright:ui

# Update visual snapshots
npm run playwright -- --update-snapshots
```

## Current Test Coverage

### Unit Tests (`src/test/`)
- ✅ `wizard-defaults.test.ts` - Tests default values for wizard settings
  - Brief settings defaults
  - Plan settings defaults
  - Brand kit defaults
  - Captions defaults
  - Stock sources defaults

### E2E Tests (`tests/e2e/`)
- ✅ `wizard.spec.ts` - Complete wizard workflow
  - Free profile workflow end-to-end
  - Navigation between wizard steps
  - Settings persistence to localStorage

- ✅ `visual.spec.ts` - Visual regression tests
  - Wizard step 1 (empty)
  - Wizard step 1 (with content)
  - Settings page (dark mode)
  - Settings page (light mode)
  - Dashboard page

## Coverage Goals

The coverage infrastructure is configured but thresholds are intentionally relaxed for the initial implementation. Coverage can be tracked per-PR for changed files.

### Current Approach
- **Unit tests**: Focus on critical business logic and defaults
- **E2E tests**: Cover happy paths and common workflows
- **Visual tests**: Ensure UI consistency across changes

### Future Improvements
- Add more unit tests for utility functions
- Add component-level tests for complex UI components
- Add integration tests for state management
- Increase coverage thresholds as test suite grows

## Writing Tests

### Unit Test Example

```typescript
import { describe, it, expect } from 'vitest';

describe('My Feature', () => {
  it('should do something', () => {
    const result = myFunction();
    expect(result).toBe(expected);
  });
});
```

### E2E Test Example

```typescript
import { test, expect } from '@playwright/test';

test('should complete workflow', async ({ page }) => {
  await page.goto('/create');
  await page.fill('input[name="topic"]', 'Test');
  await page.click('button[type="submit"]');
  await expect(page.locator('.success')).toBeVisible();
});
```

### Visual Regression Test Example

```typescript
import { test, expect } from '@playwright/test';

test('should match snapshot', async ({ page }) => {
  await page.goto('/dashboard');
  await expect(page).toHaveScreenshot('dashboard.png', {
    fullPage: true,
    animations: 'disabled',
  });
});
```

## CI Integration

All tests run automatically on every PR via GitHub Actions:

1. **Unit Tests**: Run on every commit
2. **E2E Tests**: Run on every PR
3. **Visual Tests**: Compare screenshots against baselines
4. **Coverage Reports**: Uploaded as artifacts

See `.github/workflows/ci.yml` for CI configuration.

## Debugging Tests

### Vitest
```bash
# Run specific test file
npm test -- wizard-defaults.test.ts

# Run with debugging
npm test -- --inspect-brk
```

### Playwright
```bash
# Run in headed mode (see browser)
npm run playwright -- --headed

# Run specific test
npm run playwright -- wizard.spec.ts

# Debug mode
npm run playwright -- --debug
```

## Test Best Practices

1. **Keep tests focused**: Each test should verify one behavior
2. **Use descriptive names**: Test names should explain what is being tested
3. **Mock external dependencies**: Don't rely on actual APIs in tests
4. **Avoid hardcoded waits**: Use `waitFor` and explicit assertions
5. **Keep snapshots minimal**: Only snapshot what's necessary
6. **Update snapshots carefully**: Review diffs before accepting changes

## Troubleshooting

### Vitest Issues

**Error: Cannot find module**
- Ensure all imports use correct paths
- Check `tsconfig.json` path mappings

**Tests timing out**
- Increase timeout in test or globally
- Check for unresolved promises

### Playwright Issues

**Browser not installed**
- Run `npm run playwright:install`

**Tests failing due to snapshots**
- Review diff images in test report
- Update snapshots if changes are intentional: `npm run playwright -- --update-snapshots`

**Flaky tests**
- Add explicit waits: `await page.waitForLoadState('networkidle')`
- Use `toBeVisible({ timeout: 5000 })` for dynamic content
- Disable animations: `animations: 'disabled'` in screenshot options

## Resources

- [Vitest Documentation](https://vitest.dev/)
- [Playwright Documentation](https://playwright.dev/)
- [Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
- [jest-dom Matchers](https://github.com/testing-library/jest-dom)
