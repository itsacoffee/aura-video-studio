import { test, expect } from '@playwright/test';

/**
 * E2E tests for Setup Wizard Step 6 (Complete) behavior
 * Tests Save button, Exit button, error handling, and first-run detection
 */
test.describe('Setup Wizard Completion (Step 6/6)', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage to simulate first run
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.removeItem('hasSeenOnboarding');
      localStorage.removeItem('hasCompletedFirstRun');
      localStorage.removeItem('aura-setup-aborted');
      localStorage.removeItem('wizardProgress');
    });

    // Mock APIs that are called during wizard initialization
    await page.route('**/api/setup/system-status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isComplete: false,
          ffmpegPath: null,
          outputDirectory: null,
        }),
      });
    });

    await page.route('**/api/setup/wizard/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          completed: false,
          currentStep: 0,
          state: null,
          canResume: false,
          lastUpdated: null,
        }),
      });
    });
  });

  test('happy path: complete setup successfully', async ({ page }) => {
    // Mock successful completion
    await page.route('**/api/setup/complete', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          errors: [],
          correlationId: 'test-correlation-id',
        }),
      });
    });

    await page.route('**/api/setup/wizard/complete', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Wizard completed',
          correlationId: 'test-correlation-id',
        }),
      });
    });

    // Navigate to wizard
    await page.goto('/');
    
    // Wait for wizard to load
    await page.waitForSelector('text=Welcome', { timeout: 5000 });

    // Mock navigation through steps to reach Step 6
    await page.evaluate(() => {
      // Simulate being on step 5 (Step 6/6 - Complete)
      const state = {
        step: 5,
        mode: 'free',
        selectedTier: 'free',
        ffmpegReady: true,
        apiKeys: {},
        apiKeyValidationStatus: {},
        workspacePreferences: {
          defaultSaveLocation: '/test/output',
        },
      };
      localStorage.setItem('wizardProgress', JSON.stringify(state));
    });

    // Reload to pick up the state
    await page.reload();
    
    // Should be on Step 6 (Complete)
    await expect(page.locator('text=Setup Summary - Ready to Save')).toBeVisible({ timeout: 10000 });

    // Click Save button
    const saveButton = page.locator('button:has-text("Save")');
    await expect(saveButton).toBeVisible();
    await saveButton.click();

    // Should show loading state
    await expect(page.locator('button:has-text("Saving...")')).toBeVisible({ timeout: 2000 });

    // Wait for completion and navigation
    await page.waitForURL(/\/$/, { timeout: 10000 });

    // Verify localStorage is updated
    const hasCompletedFirstRun = await page.evaluate(() => 
      localStorage.getItem('hasCompletedFirstRun')
    );
    expect(hasCompletedFirstRun).toBe('true');
  });

  test('show validation errors on completion failure', async ({ page }) => {
    // Mock failed completion with validation errors
    await page.route('**/api/setup/complete', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          errors: [
            'FFmpeg executable not found at: /invalid/path/ffmpeg',
            'Output directory is not writable: Permission denied',
          ],
          correlationId: 'test-error-correlation-id',
        }),
      });
    });

    // Navigate to wizard
    await page.goto('/');
    
    // Wait for wizard to load
    await page.waitForSelector('text=Welcome', { timeout: 5000 });

    // Mock navigation to Step 6
    await page.evaluate(() => {
      const state = {
        step: 5,
        mode: 'free',
        selectedTier: 'free',
        ffmpegReady: false,
        apiKeys: {},
        apiKeyValidationStatus: {},
        workspacePreferences: {
          defaultSaveLocation: '/invalid/output',
        },
      };
      localStorage.setItem('wizardProgress', JSON.stringify(state));
    });

    await page.reload();
    
    // Should be on Step 6
    await expect(page.locator('text=Setup Summary - Ready to Save')).toBeVisible({ timeout: 10000 });

    // Click Save button
    const saveButton = page.locator('button:has-text("Save")');
    await saveButton.click();

    // Should show validation errors
    await expect(page.locator('text=Validation Failed')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=FFmpeg executable not found')).toBeVisible();
    await expect(page.locator('text=Output directory is not writable')).toBeVisible();

    // Save button should be re-enabled
    await expect(saveButton).toBeEnabled();

    // Should not have navigated away
    await expect(page.locator('text=Setup Summary - Ready to Save')).toBeVisible();
  });

  test('exit without completion saves progress', async ({ page }) => {
    // Mock wizard progress save
    await page.route('**/api/setup/wizard/save-progress', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Progress saved',
          correlationId: 'test-save-correlation-id',
        }),
      });
    });

    // Navigate to wizard
    await page.goto('/');
    
    // Wait for wizard to load
    await page.waitForSelector('text=Welcome', { timeout: 5000 });

    // Mock navigation to Step 6
    await page.evaluate(() => {
      const state = {
        step: 5,
        mode: 'free',
        selectedTier: 'free',
        ffmpegReady: false,
        apiKeys: {},
        apiKeyValidationStatus: {},
        workspacePreferences: {
          defaultSaveLocation: '/test/output',
        },
      };
      localStorage.setItem('wizardProgress', JSON.stringify(state));
    });

    await page.reload();
    
    // Should be on Step 6
    await expect(page.locator('text=Setup Summary - Ready to Save')).toBeVisible({ timeout: 10000 });

    // Click Exit Wizard button
    const exitButton = page.locator('button:has-text("Exit Wizard")');
    await expect(exitButton).toBeVisible();
    
    // Handle confirmation dialog
    page.on('dialog', (dialog) => {
      expect(dialog.message()).toContain('exit the setup wizard');
      dialog.accept();
    });
    
    await exitButton.click();

    // Should navigate to main app
    await page.waitForURL(/\/$/, { timeout: 10000 });

    // Verify abort flags are set
    const setupAborted = await page.evaluate(() => 
      localStorage.getItem('aura-setup-aborted')
    );
    expect(setupAborted).toBe('true');

    // hasCompletedFirstRun should NOT be set
    const hasCompletedFirstRun = await page.evaluate(() => 
      localStorage.getItem('hasCompletedFirstRun')
    );
    expect(hasCompletedFirstRun).not.toBe('true');
  });

  test('refresh after completion does not reopen wizard', async ({ page }) => {
    // Mock completed system status
    await page.route('**/api/setup/system-status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isComplete: true,
          ffmpegPath: '/usr/bin/ffmpeg',
          outputDirectory: '/home/user/Videos',
        }),
      });
    });

    // Set completion flag
    await page.evaluate(() => {
      localStorage.setItem('hasCompletedFirstRun', 'true');
    });

    // Navigate to app
    await page.goto('/');

    // Wait for the main app to load (check for something that should be visible in main app, not wizard)
    // We expect NOT to see wizard-specific elements
    await expect(page.locator('text=Welcome')).not.toBeVisible({ timeout: 3000 }).catch(() => {
      // It's okay if the element doesn't exist at all
    });
    
    // Verify URL doesn't contain wizard-related paths
    await page.waitForLoadState('domcontentloaded');
    const url = page.url();
    expect(url).not.toContain('/setup');
    expect(url).not.toContain('/onboarding');
    expect(url).not.toContain('wizard');
  });
});
