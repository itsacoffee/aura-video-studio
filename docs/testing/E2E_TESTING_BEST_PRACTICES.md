# E2E Testing Best Practices

Guide for writing effective End-to-End tests with Playwright.

## Table of Contents

- [Test Structure](#test-structure)
- [Page Object Model](#page-object-model)
- [Selectors](#selectors)
- [Waiting Strategies](#waiting-strategies)
- [Visual Testing](#visual-testing)
- [Best Practices](#best-practices)

## Test Structure

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should perform action', async ({ page }) => {
    // Arrange
    await page.fill('input[name="field"]', 'value');

    // Act
    await page.click('button[type="submit"]');

    // Assert
    await expect(page.locator('.success')).toBeVisible();
  });
});
```

### Test Hooks

```typescript
test.describe('Suite with setup', () => {
  // Runs before all tests
  test.beforeAll(async ({ browser }) => {
    // Setup that runs once
  });

  // Runs before each test
  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard');
  });

  // Runs after each test
  test.afterEach(async ({ page }) => {
    // Cleanup
  });

  // Runs after all tests
  test.afterAll(async () => {
    // Final cleanup
  });
});
```

## Page Object Model

### Creating Page Objects

```typescript
// pages/ProjectsPage.ts
import { Page, Locator } from '@playwright/test';

export class ProjectsPage {
  readonly page: Page;
  readonly createButton: Locator;
  readonly projectList: Locator;
  readonly searchInput: Locator;

  constructor(page: Page) {
    this.page = page;
    this.createButton = page.locator('button[data-testid="create-project"]');
    this.projectList = page.locator('[data-testid="project-list"]');
    this.searchInput = page.locator('input[placeholder="Search projects"]');
  }

  async goto() {
    await this.page.goto('/projects');
    await this.page.waitForLoadState('networkidle');
  }

  async createProject(name: string, description: string) {
    await this.createButton.click();
    await this.page.fill('input[name="name"]', name);
    await this.page.fill('textarea[name="description"]', description);
    await this.page.click('button[type="submit"]');
  }

  async searchProject(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForTimeout(300); // Debounce
  }

  async getProjectByName(name: string): Promise<Locator> {
    return this.projectList.locator(`text="${name}"`);
  }

  async deleteProject(name: string) {
    const project = await this.getProjectByName(name);
    await project.hover();
    await project.locator('button[aria-label="Delete"]').click();
    await this.page.click('button:has-text("Confirm")');
  }
}
```

### Using Page Objects

```typescript
import { test, expect } from '@playwright/test';
import { ProjectsPage } from './pages/ProjectsPage';

test.describe('Projects', () => {
  let projectsPage: ProjectsPage;

  test.beforeEach(async ({ page }) => {
    projectsPage = new ProjectsPage(page);
    await projectsPage.goto();
  });

  test('should create new project', async () => {
    await projectsPage.createProject('Test Project', 'Description');
    
    const project = await projectsPage.getProjectByName('Test Project');
    await expect(project).toBeVisible();
  });

  test('should search projects', async () => {
    await projectsPage.searchProject('Test');
    
    const results = projectsPage.projectList.locator('.project-card');
    await expect(results).toHaveCount(1);
  });
});
```

## Selectors

### Selector Strategies (in priority order)

```typescript
// 1. Test IDs (Best)
await page.click('[data-testid="submit-button"]');

// 2. User-facing attributes
await page.click('button[name="submit"]');
await page.click('text="Submit"');
await page.click('[aria-label="Submit form"]');

// 3. Semantic selectors
await page.click('button[type="submit"]');
await page.click('role=button[name="Submit"]');

// 4. CSS selectors (Last resort)
await page.click('.submit-btn');
```

### Locator API

```typescript
// Get locator
const button = page.locator('button');

// Filter locators
const activeButton = page.locator('button').filter({ hasText: 'Active' });

// Chain locators
const deleteButton = page
  .locator('.project-card')
  .filter({ hasText: 'My Project' })
  .locator('button[aria-label="Delete"]');

// Multiple elements
const allButtons = page.locator('button');
const count = await allButtons.count();
const first = allButtons.first();
const last = allButtons.last();
const nth = allButtons.nth(2);

// Within element
const form = page.locator('form');
const submitButton = form.locator('button[type="submit"]');
```

## Waiting Strategies

### Auto-waiting

Playwright automatically waits for elements to be:
- Attached to DOM
- Visible
- Stable (not animating)
- Enabled
- Editable (for inputs)

```typescript
// No explicit wait needed
await page.click('button'); // Automatically waits
await page.fill('input', 'text'); // Automatically waits
```

### Explicit Waits

```typescript
// Wait for selector
await page.waitForSelector('.loading', { state: 'hidden' });

// Wait for load state
await page.waitForLoadState('networkidle');

// Wait for URL
await page.waitForURL('**/projects/**');

// Wait for response
await page.waitForResponse('**/api/projects');

// Wait for request
await page.waitForRequest('**/api/projects');

// Wait for timeout (use sparingly!)
await page.waitForTimeout(1000);

// Wait for function
await page.waitForFunction(() => window.appReady === true);
```

### Best Waiting Practices

```typescript
// ✅ Good: Wait for specific condition
await page.waitForSelector('[data-testid="project-loaded"]');

// ❌ Bad: Fixed timeout
await page.waitForTimeout(5000);

// ✅ Good: Wait for network to settle
await page.goto('/projects');
await page.waitForLoadState('networkidle');

// ✅ Good: Wait for specific API call
const response = await page.waitForResponse('**/api/projects');
expect(response.status()).toBe(200);
```

## Visual Testing

### Screenshot Comparison

```typescript
test('should match visual snapshot', async ({ page }) => {
  await page.goto('/dashboard');
  
  // Full page screenshot
  await expect(page).toHaveScreenshot('dashboard.png', {
    fullPage: true,
    animations: 'disabled',
  });

  // Element screenshot
  const header = page.locator('header');
  await expect(header).toHaveScreenshot('header.png');
});
```

### Visual Regression Configuration

```typescript
// playwright.config.ts
export default defineConfig({
  expect: {
    toHaveScreenshot: {
      maxDiffPixels: 100, // Allow small differences
      threshold: 0.2, // 20% pixel difference threshold
      animations: 'disabled',
    },
  },
});
```

### Updating Snapshots

```bash
# Update all snapshots
npx playwright test --update-snapshots

# Update specific test
npx playwright test dashboard.spec.ts --update-snapshots

# Update in specific browser
npx playwright test --update-snapshots --project=chromium
```

## Best Practices

### 1. Use Test IDs

```typescript
// ❌ Bad: Fragile selector
await page.click('.btn.btn-primary.submit');

// ✅ Good: Stable test ID
await page.click('[data-testid="submit-button"]');
```

### 2. Isolate Tests

```typescript
// ✅ Good: Each test is independent
test.describe('Projects', () => {
  test.beforeEach(async ({ page }) => {
    // Fresh state for each test
    await page.goto('/projects');
  });

  test('test 1', async ({ page }) => {
    // Test doesn't depend on test 2
  });

  test('test 2', async ({ page }) => {
    // Test doesn't depend on test 1
  });
});
```

### 3. Use Parallel Execution

```typescript
// playwright.config.ts
export default defineConfig({
  workers: process.env.CI ? 2 : undefined,
  fullyParallel: true,
});

// Mark tests as independent
test.describe.configure({ mode: 'parallel' });
```

### 4. Handle Dynamic Content

```typescript
// ✅ Good: Wait for content
await page.waitForSelector('[data-testid="data-loaded"]');
await expect(page.locator('.project')).toHaveCount(3);

// ✅ Good: Retry assertions
await expect(async () => {
  const count = await page.locator('.project').count();
  expect(count).toBeGreaterThan(0);
}).toPass({
  intervals: [1000, 2000, 5000],
  timeout: 10000,
});
```

### 5. Test User Flows, Not Implementation

```typescript
// ❌ Bad: Testing implementation
test('should update state on button click', async ({ page }) => {
  await page.click('button');
  const state = await page.evaluate(() => window.__APP_STATE__);
  expect(state.clicked).toBe(true);
});

// ✅ Good: Testing user-visible behavior
test('should show success message after submit', async ({ page }) => {
  await page.click('button[type="submit"]');
  await expect(page.locator('.success-message')).toBeVisible();
});
```

### 6. Mock External Services

```typescript
test('should handle API error', async ({ page }) => {
  // Mock API failure
  await page.route('**/api/projects', (route) => {
    route.fulfill({
      status: 500,
      body: JSON.stringify({ error: 'Server error' }),
    });
  });

  await page.goto('/projects');
  await expect(page.locator('.error-message')).toBeVisible();
});
```

### 7. Use Fixtures

```typescript
// fixtures.ts
import { test as base } from '@playwright/test';

type Fixtures = {
  authenticatedPage: Page;
};

export const test = base.extend<Fixtures>({
  authenticatedPage: async ({ page }, use) => {
    // Setup: Login
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'password');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/dashboard');

    // Use the authenticated page
    await use(page);

    // Teardown: Logout
    await page.click('[data-testid="logout"]');
  },
});

// Usage
test('should access protected route', async ({ authenticatedPage }) => {
  await authenticatedPage.goto('/protected');
  await expect(authenticatedPage).toHaveURL('**/protected');
});
```

### 8. Organize Tests by User Journey

```typescript
test.describe('Video Creation Journey', () => {
  test('step 1: navigate to create page', async ({ page }) => {
    // ...
  });

  test('step 2: fill in video details', async ({ page }) => {
    // ...
  });

  test('step 3: submit and verify creation', async ({ page }) => {
    // ...
  });
});
```

### 9. Use Soft Assertions for Multiple Checks

```typescript
test('should validate form fields', async ({ page }) => {
  // Continue test even if some assertions fail
  await expect.soft(page.locator('#name')).toBeVisible();
  await expect.soft(page.locator('#email')).toBeVisible();
  await expect.soft(page.locator('#submit')).toBeDisabled();
  
  // Test continues and reports all failures
});
```

### 10. Debug Tests Effectively

```typescript
// Add debugging helpers
test('debug test', async ({ page }) => {
  // Pause test
  await page.pause();

  // Take screenshot
  await page.screenshot({ path: 'debug.png' });

  // Log element info
  const button = page.locator('button');
  console.log(await button.textContent());
  console.log(await button.isVisible());

  // Video recording (enabled in config)
  // Video automatically saved on failure
});
```

## Common Patterns

### Authentication

```typescript
// Save auth state
test('login', async ({ page }) => {
  await page.goto('/login');
  await page.fill('[name="email"]', 'test@example.com');
  await page.fill('[name="password"]', 'password');
  await page.click('button[type="submit"]');
  
  await page.context().storageState({ path: 'auth.json' });
});

// Use auth state
test.use({ storageState: 'auth.json' });
```

### File Upload

```typescript
test('should upload file', async ({ page }) => {
  await page.setInputFiles('input[type="file"]', 'path/to/file.png');
  
  // Or with buffer
  await page.setInputFiles('input[type="file"]', {
    name: 'file.txt',
    mimeType: 'text/plain',
    buffer: Buffer.from('file content'),
  });
});
```

### Downloads

```typescript
test('should download file', async ({ page }) => {
  const downloadPromise = page.waitForEvent('download');
  await page.click('a[download]');
  const download = await downloadPromise;
  
  await download.saveAs('/path/to/save.pdf');
});
```

### Multiple Tabs/Windows

```typescript
test('should handle new window', async ({ page, context }) => {
  const pagePromise = context.waitForEvent('page');
  await page.click('a[target="_blank"]');
  const newPage = await pagePromise;
  
  await expect(newPage).toHaveTitle(/New Window/);
});
```

## Troubleshooting

### Flaky Tests

```typescript
// Increase timeout for slow operations
test('slow test', async ({ page }) => {
  test.setTimeout(60000);
  await page.goto('/slow-page');
});

// Use retry
test('flaky test', async ({ page }) => {
  test.fixme(); // Skip until fixed
  // or
  test.fail(); // Expected to fail
});

// Stabilize with proper waits
await page.waitForLoadState('networkidle');
await expect(element).toBeVisible({ timeout: 10000 });
```

### Debugging

```bash
# Run in headed mode
npx playwright test --headed

# Run with browser devtools
npx playwright test --debug

# Generate trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```
