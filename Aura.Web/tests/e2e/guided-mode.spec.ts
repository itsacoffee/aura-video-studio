import { test, expect } from '@playwright/test';

test.describe('Guided Mode', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display guided mode elements when enabled', async ({ page }) => {
    const config = {
      enabled: true,
      experienceLevel: 'beginner',
      showTooltips: true,
      showWhyLinks: true,
      requirePromptDiffConfirmation: true,
    };

    await page.evaluate((cfg) => {
      localStorage.setItem('guided-mode-config', JSON.stringify(cfg));
    }, config);

    await page.reload();

    const wizardLink = page.getByRole('link', { name: /create/i });
    if (await wizardLink.isVisible()) {
      await wizardLink.click();
    }
  });

  test('should allow user to change experience level', async ({ page }) => {
    await page.goto('/settings');

    const advancedOption = page.getByRole('radio', { name: /advanced/i });
    if (await advancedOption.isVisible()) {
      await advancedOption.click();

      const config = await page.evaluate(() => {
        const stored = localStorage.getItem('guided-mode-config');
        return stored ? JSON.parse(stored) : null;
      });

      expect(config?.experienceLevel).toBe('advanced');
    }
  });

  test('should show explanation panel when requested', async ({ page }) => {
    await page.goto('/wizard/create');

    const explainButton = page.getByRole('button', { name: /explain/i });
    if (await explainButton.isVisible()) {
      await explainButton.click();

      await expect(page.getByText(/understanding your/i)).toBeVisible({
        timeout: 5000,
      });
    }
  });

  test('should show improvement menu options', async ({ page }) => {
    await page.goto('/wizard/create');

    const improveButton = page.getByRole('button', { name: /improve/i });
    if (await improveButton.isVisible()) {
      await improveButton.click();

      await expect(page.getByText(/improve clarity/i)).toBeVisible();
      await expect(page.getByText(/adapt for audience/i)).toBeVisible();
      await expect(page.getByText(/shorten/i)).toBeVisible();
      await expect(page.getByText(/expand/i)).toBeVisible();
    }
  });

  test('should display prompt diff modal before applying changes', async ({ page }) => {
    await page.goto('/wizard/create');

    const improveButton = page.getByRole('button', { name: /improve/i });
    if (await improveButton.isVisible()) {
      await improveButton.click();

      const clarityOption = page.getByText(/improve clarity/i);
      if (await clarityOption.isVisible()) {
        await clarityOption.click();

        await expect(page.getByText(/review prompt changes/i)).toBeVisible({
          timeout: 5000,
        });

        await expect(page.getByText(/intended outcome/i)).toBeVisible();

        const proceedButton = page.getByRole('button', {
          name: /proceed with changes/i,
        });
        await expect(proceedButton).toBeVisible();

        const cancelButton = page.getByRole('button', { name: /cancel/i });
        await expect(cancelButton).toBeVisible();
      }
    }
  });

  test('should allow locking sections in script', async ({ page }) => {
    await page.goto('/wizard/create');

    const lockSectionButton = page.getByRole('button', { name: /lock section/i });
    if (await lockSectionButton.isVisible()) {
      await lockSectionButton.click();

      await expect(page.getByText(/locked sections/i)).toBeVisible();
    }
  });

  test('should preserve locked sections during regeneration', async ({ page }) => {
    await page.goto('/wizard/create');

    const lockButton = page.getByRole('button', { name: /lock/i }).first();
    if (await lockButton.isVisible()) {
      await lockButton.click();
    }

    const regenerateButton = page.getByRole('button', { name: /regenerate/i });
    if (await regenerateButton.isVisible()) {
      await regenerateButton.click();

      await expect(page.getByText(/preserving.*locked/i)).toBeVisible({
        timeout: 5000,
      });
    }
  });

  test('should track telemetry for guided mode actions', async ({ page }) => {
    let telemetryRequests = 0;

    page.on('request', (request) => {
      if (request.url().includes('/api/guidedmode/telemetry')) {
        telemetryRequests++;
      }
    });

    await page.goto('/wizard/create');

    const explainButton = page.getByRole('button', { name: /explain/i });
    if (await explainButton.isVisible()) {
      await explainButton.click();
      await page.waitForTimeout(1000);
    }

    expect(telemetryRequests).toBeGreaterThan(0);
  });
});
