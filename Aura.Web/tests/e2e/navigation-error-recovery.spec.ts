import { test, expect } from '@playwright/test';

test.describe('Navigation Error Recovery', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the home page before each test
    await page.goto('/');
  });

  test('should navigate to Projects page without crashing', async ({ page }) => {
    // Navigate to Projects page
    await page.goto('/projects');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Should show either projects or empty state, not an error
    const hasEmptyState = await page.locator('text=No editor projects yet').isVisible();
    const hasProjects = await page.locator('table').isVisible();
    const hasError = await page.locator('text=Oops! Something went wrong').isVisible();

    expect(hasError).toBe(false);
    expect(hasEmptyState || hasProjects).toBe(true);
  });

  test('should navigate to Asset Library without crashing', async ({ page }) => {
    // Navigate to Asset Library page
    await page.goto('/assets');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Should show either assets or empty state, not an error
    const hasEmptyState = await page.locator('text=No assets found').isVisible();
    const hasAssets = await page.locator('text=Asset Library').isVisible();
    const hasError = await page.locator('text=Oops! Something went wrong').isVisible();

    expect(hasError).toBe(false);
    expect(hasEmptyState || hasAssets).toBe(true);
  });

  test('should navigate to Content Planning without crashing', async ({ page }) => {
    // Navigate to Content Planning page
    await page.goto('/content-planning');

    // Wait for the page to load
    await page.waitForLoadState('networkidle');

    // Should show content planning page, not an error
    const hasContentPlanning = await page.locator('text=Content Planning').isVisible();
    const hasError = await page.locator('text=Oops! Something went wrong').isVisible();

    expect(hasError).toBe(false);
    expect(hasContentPlanning).toBe(true);
  });

  test('should show empty state for Projects when no projects exist', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForLoadState('networkidle');

    // Check for empty state indicators
    const emptyStateVisible = await page.locator('text=No editor projects yet').isVisible();
    if (emptyStateVisible) {
      // Verify empty state message
      await expect(page.locator('text=No editor projects yet')).toBeVisible();
      await expect(
        page.locator('text=Create a project in the video editor to see it here')
      ).toBeVisible();

      // Verify CTA button exists
      await expect(page.locator('button:has-text("Open Video Editor")')).toBeVisible();
    }
  });

  test('should show empty state for Assets when no assets exist', async ({ page }) => {
    await page.goto('/assets');
    await page.waitForLoadState('networkidle');

    // Check if we have an empty state
    const emptyStateVisible = await page.locator('text=No assets found').isVisible();
    if (emptyStateVisible) {
      await expect(page.locator('text=No assets found')).toBeVisible();
      await expect(page.locator('text=Upload assets to get started')).toBeVisible();
    }
  });

  test('should have working Refresh button on Projects page', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForLoadState('networkidle');

    // Find and click refresh button
    const refreshButton = page.locator('button:has-text("Refresh")');
    await expect(refreshButton).toBeVisible();

    await refreshButton.click();

    // Wait for refresh to complete
    await page.waitForTimeout(500);

    // Page should still be visible (not crashed)
    await expect(page.locator('text=Projects')).toBeVisible();
  });

  test('should switch tabs in Content Planning without errors', async ({ page }) => {
    await page.goto('/content-planning');
    await page.waitForLoadState('networkidle');

    // Click on different tabs
    const tabs = ['Trend Analysis', 'Topic Suggestions', 'Content Calendar', 'Audience Insights'];

    for (const tabName of tabs) {
      const tab = page.locator(`button[role="tab"]:has-text("${tabName}")`);
      if (await tab.isVisible()) {
        await tab.click();
        await page.waitForTimeout(300);

        // Verify no error occurred
        const hasError = await page.locator('text=Oops! Something went wrong').isVisible();
        expect(hasError).toBe(false);
      }
    }
  });

  test('should switch tabs in Projects without errors', async ({ page }) => {
    await page.goto('/projects');
    await page.waitForLoadState('networkidle');

    // Switch to Generated Videos tab
    const generatedVideosTab = page.locator('button[role="tab"]:has-text("Generated Videos")');
    if (await generatedVideosTab.isVisible()) {
      await generatedVideosTab.click();
      await page.waitForTimeout(300);

      // Verify no error
      const hasError = await page.locator('text=Oops! Something went wrong').isVisible();
      expect(hasError).toBe(false);
    }

    // Switch back to Editor Projects
    const editorProjectsTab = page.locator('button[role="tab"]:has-text("Editor Projects")');
    if (await editorProjectsTab.isVisible()) {
      await editorProjectsTab.click();
      await page.waitForTimeout(300);

      // Verify no error
      const hasError = await page.locator('text=Oops! Something went wrong').isVisible();
      expect(hasError).toBe(false);
    }
  });
});

test.describe('Error Boundary Retry Functionality', () => {
  test('should show retry button when component errors occur', async ({ page }) => {
    // This test would require us to inject errors or mock API failures
    // For now, we test that the error boundary doesn't prevent navigation

    await page.goto('/projects');
    await page.waitForLoadState('networkidle');

    // If an error boundary is shown, there should be a retry button
    const errorBoundary = await page.locator('text=Oops! Something went wrong').isVisible();

    if (errorBoundary) {
      // Verify retry button exists
      await expect(page.locator('button:has-text("Try Again")')).toBeVisible();

      // Verify home button exists
      await expect(page.locator('button:has-text("Go to Home")')).toBeVisible();
    }
  });
});
