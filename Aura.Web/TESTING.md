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

### Test Statistics
- **Total Test Files**: 45
- **Total Tests**: 541 passing
- **Coverage Target**: 70% for new code
- **Coverage Provider**: v8 with line, branch, and statement coverage

### Test Organization

#### Unit Tests (`src/test/` and `src/**/__tests__/`)
- ✅ **Utilities** (3 files, 40+ tests)
  - `formatters.test.ts` - File size, duration, relative time formatting
  - `enumNormalizer.test.ts` - Enum normalization and validation
  - `formValidation.test.ts` - Form validation rules
  - `mediaProcessing.test.ts` - Media file processing

- ✅ **Services** (3 files, 40+ tests)
  - `loggingService.test.ts` - Application logging
  - `commandHistory.test.ts` - Command undo/redo
  - `performanceMonitor.test.ts` - Performance tracking

- ✅ **State Management** (4 files, 75+ tests)
  - `jobState.test.ts` - Job status management
  - `jobs.test.ts` - Job queue operations
  - `engines.test.ts` - Engine state management
  - `onboarding.test.ts` - Onboarding flow state

- ✅ **Components** (3 files, 50+ tests)
  - `GlobalStatusFooter.test.tsx` - Status footer component
  - `Loading.test.tsx` - Loading states
  - `ValidatedInput.test.tsx` - Form input validation

- ✅ **Hooks** (1 file, 10+ tests)
  - `useLoadingState.test.ts` - Loading state hook

- ✅ **Commands** (1 file, 25 tests)
  - `clipCommands.test.ts` - Timeline clip commands

- ✅ **Integration Tests** (10 files, 150+ tests)
  - API client integration
  - Clipboard service
  - Keyboard shortcuts
  - Timeline operations
  - Pacing analysis
  - Engine workflows
  - Quality dashboard

- ✅ **Test Data Factories** (1 file, 13 tests)
  - `factories.test.ts` - Test data factory functions
  - Timeline factories (clips, tracks, markers, overlays)
  - Project factories (brief, plan spec, voice spec)
  - System factories (hardware, render jobs, profiles)

- ✅ **Accessibility Tests** (1 file, 10 tests)
  - `accessibility.test.tsx` - ARIA attributes and keyboard navigation
  - Focus management
  - Screen reader support
  - Form accessibility
  - Color contrast

### E2E Tests (`tests/e2e/`)
- ✅ **Wizard Flows** (10 E2E test files)
  - `wizard.spec.ts` - Complete wizard workflow
  - `first-run-wizard.spec.ts` - First-run experience
  - `onboarding-path-pickers.spec.ts` - Path selection
  - `dependency-download.spec.ts` - Dependency installation
  - `engine-diagnostics.spec.ts` - Engine health checks
  - `local-engines.spec.ts` - Local engine management
  - `notifications.spec.ts` - Toast notifications
  - `error-ux-toasts.spec.ts` - Error handling UX
  - `logviewer.spec.ts` - Log viewing
  - `visual.spec.ts` - Visual regression tests

## Test Data Factories

To ensure consistency across tests, we provide factory functions for creating test data:

```typescript
import { 
  createMockTimelineClip, 
  createMockTrack,
  createMockBrief,
  createMockHardwareCapabilities 
} from '@/test/factories';

// Create a clip with defaults
const clip = createMockTimelineClip();

// Create with overrides
const customClip = createMockTimelineClip({ 
  id: 'custom-id',
  timelineStart: 30 
});

// Create multiple items
const clips = createMockClips(5, { trackId: 'track-1' });
```

See `src/test/factories/` for all available factories.

## Coverage Goals

### Current Coverage
- **Utility Functions**: 80%+ coverage
- **Critical Services**: 70%+ coverage
- **State Management**: 75%+ coverage
- **Components**: 60%+ coverage (focusing on critical components)

### Coverage Configuration
```json
{
  "thresholds": {
    "lines": 70,
    "branches": 70,
    "statements": 70,
    "perFile": true
  }
}
```

### Approach
- **Unit tests**: Focus on critical business logic, utilities, and services
- **Component tests**: Cover primary use cases and user interactions
- **Integration tests**: Test API integration and complex workflows
- **E2E tests**: Cover critical user journeys end-to-end
- **Accessibility tests**: Ensure ARIA compliance and keyboard navigation

## Writing Tests

### Using Test Data Factories

For consistent test data, use the factory functions:

```typescript
import { describe, it, expect } from 'vitest';
import { createMockTimelineClip, createMockTrack } from '@/test/factories';

describe('Timeline Operations', () => {
  it('should add clip to track', () => {
    const track = createMockTrack();
    const clip = createMockTimelineClip({ trackId: track.id });
    
    track.clips.push(clip);
    expect(track.clips).toHaveLength(1);
  });
  
  it('should create multiple clips', () => {
    const clips = createMockClips(3);
    expect(clips).toHaveLength(3);
    expect(clips[0].timelineStart).toBe(0);
    expect(clips[1].timelineStart).toBe(10);
  });
});
```

### Component Test Example

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MyComponent } from './MyComponent';

describe('MyComponent', () => {
  it('should render with props', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <MyComponent title="Test Title" />
      </FluentProvider>
    );
    
    expect(screen.getByText('Test Title')).toBeDefined();
  });
});
```

### Accessibility Test Example

```typescript
import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';

describe('Accessibility', () => {
  it('should have proper ARIA attributes', () => {
    const { container } = render(
      <button aria-label="Close dialog">X</button>
    );
    
    const button = container.querySelector('button');
    expect(button?.getAttribute('aria-label')).toBe('Close dialog');
    expect(button?.tabIndex).toBeGreaterThanOrEqual(0);
  });
});
```

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
7. **Use test data factories**: Use `createMock*` functions for consistent test data
8. **Test accessibility**: Include ARIA attributes and keyboard navigation tests
9. **Test error cases**: Don't just test the happy path
10. **Keep tests independent**: Tests should not depend on each other's state

## Accessibility Testing

All new components should include accessibility tests:

```typescript
describe('Accessibility', () => {
  it('should be keyboard navigable', () => {
    const { container } = render(<MyComponent />);
    const focusableElements = container.querySelectorAll('button, input, a');
    focusableElements.forEach(el => {
      expect(el.tabIndex).toBeGreaterThanOrEqual(0);
    });
  });
  
  it('should have proper labels', () => {
    render(<MyInput label="Username" />);
    expect(screen.getByLabelText('Username')).toBeDefined();
  });
  
  it('should announce errors to screen readers', () => {
    const { container } = render(
      <ErrorMessage role="alert">Error occurred</ErrorMessage>
    );
    expect(container.querySelector('[role="alert"]')).toBeDefined();
  });
});
```

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
