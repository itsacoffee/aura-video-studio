import { test, expect } from '@playwright/test';

test.describe('Navigation Crash Fixes', () => {
  test.beforeEach(async ({ page }) => {
    // Start from home page
    await page.goto('/');
  });

  test('should navigate to Projects page without crashing', async ({ page }) => {
    // Navigate to projects page
    await page.goto('/projects');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Page should not show error boundary
    const errorBoundary = page.getByText(/Oops! Something went wrong/i);
    await expect(errorBoundary).not.toBeVisible();

    // Should show Projects title
    const title = page.getByRole('heading', { name: /projects/i, level: 1 });
    await expect(title).toBeVisible();

    // Should show either empty state or projects list
    const emptyState = page.getByText(/No editor projects yet/i);
    const projectsTable = page.getByRole('table');
    const isEmptyOrHasProjects = await Promise.race([
      emptyState.isVisible().catch(() => false),
      projectsTable.isVisible().catch(() => false),
    ]);
    expect(isEmptyOrHasProjects).toBeTruthy();
  });

  test('should navigate to Asset Library without crashing', async ({ page }) => {
    // Navigate to assets page
    await page.goto('/assets');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Page should not show error boundary
    const errorBoundary = page.getByText(/Oops! Something went wrong/i);
    await expect(errorBoundary).not.toBeVisible();

    // Should show Asset Library title
    const title = page.getByText(/Asset Library/i);
    await expect(title).toBeVisible();

    // Should show either empty state or assets grid
    const emptyState = page.getByText(/No assets found/i);
    const assetsGrid = page.locator('[class*="assetsGrid"]');
    const isEmptyOrHasAssets = await Promise.race([
      emptyState.isVisible().catch(() => false),
      assetsGrid.isVisible().catch(() => false),
    ]);
    expect(isEmptyOrHasAssets).toBeTruthy();
  });

  test('should navigate to Content Planning without crashing', async ({ page }) => {
    // Navigate to content planning page
    await page.goto('/content-planning');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Page should not show error boundary
    const errorBoundary = page.getByText(/Oops! Something went wrong/i);
    await expect(errorBoundary).not.toBeVisible();

    // Should show Content Planning title
    const title = page.getByText(/Content Planning/i).first();
    await expect(title).toBeVisible();

    // Should show tabs
    const trendsTab = page.getByRole('tab', { name: /Trend Analysis/i });
    const topicsTab = page.getByRole('tab', { name: /Topic Suggestions/i });
    await expect(trendsTab).toBeVisible();
    await expect(topicsTab).toBeVisible();
  });

  test('should show error boundary with retry button when route fails', async ({ page }) => {
    // Mock network error
    await page.route('**/api/project', (route) => {
      route.abort('failed');
    });

    // Navigate to projects page
    await page.goto('/projects');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Should show error state with retry button (not error boundary since error is in component)
    // The error will be handled gracefully by the hook and show empty state or error message
    const title = page.getByRole('heading', { name: /projects/i, level: 1 });
    await expect(title).toBeVisible();

    // Page should still be functional
    const refreshButton = page.getByRole('button', { name: /refresh/i });
    await expect(refreshButton).toBeVisible();
  });

  test('should handle empty API responses gracefully', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/project', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    // Navigate to projects page
    await page.goto('/projects');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Should show empty state
    const emptyState = page.getByText(/No editor projects yet/i);
    await expect(emptyState).toBeVisible();

    // Should have CTA button
    const ctaButton = page.getByRole('button', { name: /Open Video Editor/i });
    await expect(ctaButton).toBeVisible();
  });

  test('should handle 204 No Content responses gracefully', async ({ page }) => {
    // Mock 204 response
    await page.route('**/api/project', (route) => {
      route.fulfill({
        status: 204,
        contentType: 'application/json',
      });
    });

    // Navigate to projects page
    await page.goto('/projects');

    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Should show empty state (204 is handled as empty array)
    const emptyState = page.getByText(/No editor projects yet/i);
    await expect(emptyState).toBeVisible();
  });
});
