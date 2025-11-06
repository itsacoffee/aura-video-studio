import { test, expect } from '@playwright/test';

test.describe('First-Run Setup Wizard Gating', () => {
  test.beforeEach(async ({ page, context }) => {
    // Clear any existing first-run flags
    await context.clearCookies();
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  });

  test('should redirect to onboarding on first run', async ({ page }) => {
    // Mock first-run check to return false (not completed)
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: false }),
      });
    });

    // Navigate to home page
    await page.goto('/');

    // Should redirect to onboarding
    await expect(page).toHaveURL(/\/onboarding/);

    // Should show welcome screen
    await expect(page.locator('text=Welcome to Aura Video Studio')).toBeVisible();
    await expect(page.locator('text=Complete your setup to start generating videos')).toBeVisible();
  });

  test('should allow access to home after setup complete', async ({ page }) => {
    // Mock first-run check to return true (completed)
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          completed: true,
          completedAt: new Date().toISOString(),
          version: '1.0.0',
        }),
      });
    });

    // Mock settings validation to pass
    await page.route('**/api/downloads/ffmpeg/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ state: 'Installed', path: '/usr/bin/ffmpeg' }),
      });
    });

    // Navigate to home page
    await page.goto('/');

    // Should NOT redirect to onboarding
    await expect(page).not.toHaveURL(/\/onboarding/);
  });

  test('should block access to create page before setup', async ({ page }) => {
    // Set localStorage to indicate NOT completed
    await page.addInitScript(() => {
      localStorage.setItem('hasCompletedFirstRun', 'false');
    });

    // Mock first-run check
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: false }),
      });
    });

    // Try to navigate directly to create page
    await page.goto('/create');

    // Should redirect to onboarding
    await expect(page).toHaveURL(/\/onboarding/);
  });

  test('should allow access to settings without setup', async ({ page }) => {
    // Mock first-run check to return false
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: false }),
      });
    });

    // Navigate to settings (should be allowed)
    await page.goto('/settings');

    // Should NOT redirect
    await expect(page).toHaveURL(/\/settings/);
  });

  test('should allow access to health page without setup', async ({ page }) => {
    // Mock first-run check to return false
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: false }),
      });
    });

    // Mock health endpoint
    await page.route('**/api/health', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ status: 'healthy' }),
      });
    });

    // Navigate to health page (should be allowed)
    await page.goto('/health');

    // Should NOT redirect
    await expect(page).toHaveURL(/\/health/);
  });

  test('should show setup required banner if FFmpeg missing after setup', async ({ page }) => {
    // Mock first-run as complete
    await page.addInitScript(() => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
    });

    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: true }),
      });
    });

    // Mock FFmpeg as missing
    await page.route('**/api/downloads/ffmpeg/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ state: 'NotInstalled' }),
      });
    });

    // Navigate to create page
    await page.goto('/create');

    // Should show error banner about missing setup
    await expect(page.locator('text=Setup Required')).toBeVisible();
    await expect(page.locator('text=FFmpeg')).toBeVisible();
  });

  test('should have functional Browse button in workspace setup', async ({ page }) => {
    // Navigate to onboarding
    await page.goto('/onboarding');

    // Mock first-run check
    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ completed: false }),
      });
    });

    // Go through wizard steps to workspace setup
    await page.locator('button:has-text("Start Setup Wizard")').click();

    // Select free tier
    await page.locator('button:has-text("Free")').click();
    await page.locator('button:has-text("Next")').click();

    // Should reach workspace setup step
    await expect(page.locator('text=Workspace Preferences')).toBeVisible();

    // Default save location should not contain "YourName"
    const saveLocationInput = page.locator('input[placeholder*="Videos"]').first();
    const saveLocationValue = await saveLocationInput.inputValue();
    expect(saveLocationValue).not.toContain('YourName');
    expect(saveLocationValue).not.toContain('username');
  });
});
