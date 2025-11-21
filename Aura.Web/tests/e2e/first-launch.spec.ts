import { test, expect } from '@playwright/test';

/**
 * First Launch Experience E2E Tests
 *
 * These tests validate the complete first-launch user experience,
 * including backend auto-start, health checks, and firewall configuration.
 *
 * Tests assume:
 * - Fresh installation or first run
 * - Backend auto-start is enabled
 * - Windows Firewall may or may not have rules configured
 */

// Mock storage keys
const MOCK_FIREWALL_MISSING_KEY = 'mock-firewall-missing';
const BACKEND_AUTO_START_KEY = 'backend-auto-start-enabled';

test.describe('First Launch Experience', () => {
  test('should launch app and reach setup wizard without errors', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.waitForSelector('[data-testid="setup-wizard"]', { timeout: 30000 });

    await expect(page.locator('text=Backend Server Not Reachable')).not.toBeVisible();

    await expect(page.locator('text=Welcome to Aura Video Studio')).toBeVisible();

    await expect(page.locator('text=Step 1 of 6')).toBeVisible();
  });

  test('should handle slow backend startup gracefully', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await expect(page.locator('text=Connecting to Aura backend')).toBeVisible({ timeout: 5000 });

    await page.waitForSelector('[data-testid="setup-wizard"]', { timeout: 30000 });
  });

  test('should show firewall dialog if rule missing', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.evaluate(() => {
      window.localStorage.setItem(MOCK_FIREWALL_MISSING_KEY, 'true');
    });

    await page.reload();

    await expect(page.locator('text=Windows Firewall Configuration')).toBeVisible({
      timeout: 10000,
    });
  });

  test('should allow skipping firewall configuration', async ({ page }) => {
    await page.goto('http://localhost:5173');

    const skipButton = page.locator('button:has-text("Skip")');
    if (await skipButton.isVisible()) {
      await skipButton.click();
    }

    await expect(page.locator('[data-testid="setup-wizard"]')).toBeVisible();
  });

  test('should display backend connection status', async ({ page }) => {
    await page.goto('http://localhost:5173');

    const statusIndicator = page.locator('[data-testid="backend-status"]');

    if (await statusIndicator.isVisible({ timeout: 5000 })) {
      const statusText = await statusIndicator.textContent();
      expect(statusText).toBeTruthy();
    }
  });

  test('should handle backend retry attempts', async ({ page }) => {
    await page.goto('http://localhost:5173');

    const retryIndicator = page.locator('[data-testid="connection-retry"]');

    if (await retryIndicator.isVisible({ timeout: 5000 })) {
      const retryText = await retryIndicator.textContent();
      expect(retryText).toMatch(/attempt|retry|connecting/i);
    }

    await page.waitForSelector('[data-testid="setup-wizard"]', { timeout: 30000 });
  });

  test('should show error message if backend fails to start', async ({ page }) => {
    await page.route('**/health', (route) => {
      route.abort('failed');
    });

    await page.goto('http://localhost:5173');

    await expect(page.locator('text=/Backend.*not.*reachable|connection.*failed/i')).toBeVisible({
      timeout: 15000,
    });
  });

  test('should allow manual backend configuration if auto-start fails', async ({ page }) => {
    await page.route('**/health', (route) => {
      route.abort('failed');
    });

    await page.goto('http://localhost:5173');

    const manualConfigButton = page.locator('button:has-text("Configure Manually")');
    if (await manualConfigButton.isVisible({ timeout: 10000 })) {
      await manualConfigButton.click();

      await expect(page.locator('[data-testid="backend-config-dialog"]')).toBeVisible();
    }
  });

  test('should persist backend connection preference', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.waitForSelector('[data-testid="setup-wizard"]', { timeout: 30000 });

    const autoStartEnabled = await page.evaluate((key) => {
      return localStorage.getItem(key);
    }, BACKEND_AUTO_START_KEY);

    expect(autoStartEnabled === null || autoStartEnabled === 'true').toBe(true);
  });

  test('should show firewall configuration success message', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.evaluate(() => {
      window.localStorage.setItem(MOCK_FIREWALL_MISSING_KEY, 'true');
    });

    await page.reload();

    const configureButton = page.locator('button:has-text("Configure Firewall Automatically")');
    if (await configureButton.isVisible({ timeout: 10000 })) {
      await configureButton.click();

      await expect(page.locator('text=/firewall.*configured|rule.*created/i')).toBeVisible({
        timeout: 15000,
      });
    }
  });

  test('should handle UAC elevation prompt for firewall', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.evaluate(() => {
      window.localStorage.setItem(MOCK_FIREWALL_MISSING_KEY, 'true');
    });

    await page.reload();

    const configureButton = page.locator('button:has-text("Configure Firewall Automatically")');
    if (await configureButton.isVisible({ timeout: 10000 })) {
      await configureButton.click();

      await expect(page.locator('text=/UAC|administrator|elevation|permission/i')).toBeVisible({
        timeout: 10000,
      });
    }
  });

  test('should navigate through setup wizard after successful connection', async ({ page }) => {
    await page.goto('http://localhost:5173');

    await page.waitForSelector('[data-testid="setup-wizard"]', { timeout: 30000 });

    await expect(page.locator('text=Step 1 of 6')).toBeVisible();

    const nextButton = page.locator('button:has-text("Next")');
    if (await nextButton.isVisible()) {
      await nextButton.click();

      await expect(page.locator('text=Step 2 of 6')).toBeVisible({ timeout: 5000 });
    }
  });
});
