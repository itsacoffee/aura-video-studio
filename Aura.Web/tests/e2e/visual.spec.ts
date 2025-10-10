import { test, expect } from '@playwright/test';

/**
 * Visual regression tests for Aura.Web
 * Snapshots are stored per-branch in .playwright-snapshots/
 * See README.md for instructions on updating baselines
 */
test.describe('Visual Regression', () => {
  test('wizard step 1 - brief form', async ({ page }) => {
    await page.goto('/create');
    
    // Wait for page to load
    await expect(page.getByRole('heading', { name: 'Create Video' })).toBeVisible();
    
    // Take snapshot
    await expect(page).toHaveScreenshot('wizard-step1-brief.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('wizard step 1 - with topic filled', async ({ page }) => {
    await page.goto('/create');
    await page.getByPlaceholder('Enter your video topic').fill('Machine Learning Tutorial');
    
    // Take snapshot with content
    await expect(page).toHaveScreenshot('wizard-step1-filled.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('settings page - dark mode', async ({ page }) => {
    await page.goto('/settings');
    
    // Enable dark mode via localStorage
    await page.evaluate(() => {
      localStorage.setItem('darkMode', 'true');
    });
    await page.reload();
    
    // Wait for page to load
    await expect(page.getByRole('heading', { name: /Settings/i })).toBeVisible();
    
    // Take snapshot
    await expect(page).toHaveScreenshot('settings-dark-mode.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('settings page - light mode', async ({ page }) => {
    await page.goto('/settings');
    
    // Ensure light mode
    await page.evaluate(() => {
      localStorage.setItem('darkMode', 'false');
    });
    await page.reload();
    
    // Wait for page to load
    await expect(page.getByRole('heading', { name: /Settings/i })).toBeVisible();
    
    // Take snapshot
    await expect(page).toHaveScreenshot('settings-light-mode.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('dashboard page', async ({ page }) => {
    await page.goto('/dashboard');
    
    // Wait for page to load
    await expect(page.getByRole('heading', { name: /Dashboard/i })).toBeVisible();
    
    // Take snapshot
    await expect(page).toHaveScreenshot('dashboard.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });
});
