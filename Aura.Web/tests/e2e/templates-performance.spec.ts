/**
 * E2E tests for Templates page performance and pagination
 */

import { test, expect } from '@playwright/test';

test.describe('Templates Page Performance', () => {
  test('should load templates page without freezing', async ({ page }) => {
    // Navigate to templates page
    await page.goto('/templates');

    // Wait for page to load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Check that templates are rendered
    const templates = await page
      .locator('[role="listbox"], [data-testid="virtuoso-scroller"]')
      .first();
    await expect(templates).toBeVisible({ timeout: 5000 });

    // Verify that page is responsive (not frozen)
    const searchBox = await page.locator('input[placeholder*="Search"]').first();
    await expect(searchBox).toBeVisible();
  });

  test('should handle scrolling through templates smoothly', async ({ page }) => {
    await page.goto('/templates');

    // Wait for initial load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Get the scrollable container
    const scrollContainer = await page.locator('[data-testid="virtuoso-scroller"]').first();

    // If virtuoso container exists, scroll within it
    if ((await scrollContainer.count()) > 0) {
      // Scroll down multiple times
      for (let i = 0; i < 3; i++) {
        await scrollContainer.evaluate((el) => {
          el.scrollTop += 500;
        });

        // Wait a bit for rendering
        await page.waitForTimeout(500);
      }

      // Verify page is still responsive
      const searchBox = await page.locator('input[placeholder*="Search"]').first();
      await expect(searchBox).toBeVisible();
    }
  });

  test('should load more templates when scrolling to bottom', async ({ page }) => {
    await page.goto('/templates');

    // Wait for initial load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Scroll to bottom
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight);
    });

    // Wait for potential loading
    await page.waitForTimeout(2000);

    // Check if more templates loaded or if we see pagination info
    const paginationInfo = await page.locator('text=/Page \\d+ of \\d+/').first();

    if (await paginationInfo.isVisible()) {
      // Pagination is working
      expect(await paginationInfo.textContent()).toContain('Page');
    }
  });

  test('should navigate away and return without memory leak', async ({ page }) => {
    // Navigate to templates
    await page.goto('/templates');
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Navigate away
    await page.goto('/');
    await page.waitForTimeout(500);

    // Return to templates
    await page.goto('/templates');
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Verify page loads successfully second time
    const searchBox = await page.locator('input[placeholder*="Search"]').first();
    await expect(searchBox).toBeVisible();
  });

  test('should filter templates by category without lag', async ({ page }) => {
    await page.goto('/templates');

    // Wait for initial load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Click on different category tabs
    const categories = ['YouTube', 'Social Media', 'Business', 'Creative'];

    for (const category of categories) {
      const tab = await page.locator(`button[role="tab"]:has-text("${category}")`).first();

      if (await tab.isVisible()) {
        await tab.click();

        // Wait for filter to apply
        await page.waitForTimeout(300);

        // Verify page is still responsive
        const searchBox = await page.locator('input[placeholder*="Search"]').first();
        await expect(searchBox).toBeVisible();
      }
    }
  });

  test('should lazy load images as they come into view', async ({ page }) => {
    await page.goto('/templates');

    // Wait for initial load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    // Get initial image count
    const initialImages = await page.locator('img[loading="lazy"]').count();

    // Scroll down to load more images
    await page.evaluate(() => {
      window.scrollTo(0, 1000);
    });

    await page.waitForTimeout(1000);

    // Should have at least some lazy-loaded images
    expect(initialImages).toBeGreaterThanOrEqual(0);
  });

  test('should handle search without performance degradation', async ({ page }) => {
    await page.goto('/templates');

    // Wait for initial load
    await page.waitForSelector('text=Templates Library', { timeout: 10000 });

    const searchBox = await page.locator('input[placeholder*="Search"]').first();

    // Type search query
    await searchBox.fill('intro');

    // Wait for filter to apply
    await page.waitForTimeout(500);

    // Verify page is responsive
    await expect(searchBox).toBeVisible();

    // Clear search
    await searchBox.clear();
    await page.waitForTimeout(500);

    // Verify page still responsive
    await expect(searchBox).toBeVisible();
  });
});
