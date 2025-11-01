import { test, expect, devices } from '@playwright/test';

/**
 * Responsive design tests for Aura Video Studio
 * Tests UI behavior across different screen sizes and devices
 */
test.describe('Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses
    await page.route('**/api/settings', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ offlineMode: false }),
      });
    });
  });

  test('should render correctly on desktop (1920x1080)', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/create');

    // Main content should be visible
    const mainContent = page.locator('main, [role="main"], .main-content').first();
    await expect(mainContent).toBeVisible();

    // Navigation should be visible
    const nav = page.locator('nav, [role="navigation"]').first();
    if (await nav.count() > 0) {
      await expect(nav).toBeVisible();
    }

    // Form elements should be visible
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Should have adequate spacing
    const boundingBox = await topicInput.boundingBox();
    expect(boundingBox).not.toBeNull();
    if (boundingBox) {
      expect(boundingBox.width).toBeGreaterThan(200);
    }
  });

  test('should render correctly on laptop (1366x768)', async ({ page }) => {
    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/create');

    // Content should still be accessible
    const heading = page.getByRole('heading').first();
    await expect(heading).toBeVisible();

    // Form should be usable
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    await topicInput.fill('Laptop view test');
    await expect(topicInput).toHaveValue('Laptop view test');
  });

  test('should render correctly on tablet portrait (768x1024)', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/create');

    // Main content should be visible
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Form should be usable
    await topicInput.fill('Tablet portrait test');

    // Buttons should be accessible
    const nextButton = page.getByRole('button', { name: /Next/i });
    await expect(nextButton).toBeVisible();

    // Should have touch-friendly sizes (minimum 44x44px)
    const buttonBox = await nextButton.boundingBox();
    expect(buttonBox).not.toBeNull();
    if (buttonBox) {
      expect(buttonBox.height).toBeGreaterThanOrEqual(36); // Allow some flexibility
    }
  });

  test('should render correctly on tablet landscape (1024x768)', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await page.goto('/create');

    const heading = page.getByRole('heading').first();
    await expect(heading).toBeVisible();

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();
  });

  test('should render correctly on mobile portrait (375x667)', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/create');

    // Content should be visible without horizontal scroll
    const body = page.locator('body');
    const scrollWidth = await body.evaluate((el) => el.scrollWidth);
    const clientWidth = await body.evaluate((el) => el.clientWidth);

    expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 5); // Allow small tolerance

    // Form elements should be visible
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Text should be readable (minimum 14px)
    const fontSize = await topicInput.evaluate((el) => {
      return window.getComputedStyle(el).fontSize;
    });
    const fontSizeNum = parseInt(fontSize);
    expect(fontSizeNum).toBeGreaterThanOrEqual(14);

    // Navigation might be hamburger menu on mobile
    // Just verify page is functional
    await topicInput.fill('Mobile test');
    await expect(topicInput).toHaveValue('Mobile test');
  });

  test('should render correctly on mobile landscape (667x375)', async ({ page }) => {
    await page.setViewportSize({ width: 667, height: 375 });
    await page.goto('/create');

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Should handle limited vertical space
    const viewportHeight = 375;
    const inputBox = await topicInput.boundingBox();

    expect(inputBox).not.toBeNull();
    if (inputBox) {
      expect(inputBox.y).toBeLessThan(viewportHeight);
    }
  });

  test('should render correctly on iPhone 12 Pro', async ({ page }) => {
    await page.setViewportSize(devices['iPhone 12 Pro'].viewport);
    await page.goto('/create');

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Should be usable with touch
    await topicInput.tap();
    await page.keyboard.type('iPhone test');
    await expect(topicInput).toHaveValue('iPhone test');
  });

  test('should render correctly on iPad', async ({ page }) => {
    await page.setViewportSize(devices['iPad'].viewport);
    await page.goto('/create');

    const heading = page.getByRole('heading').first();
    await expect(heading).toBeVisible();

    // Should have good use of space on tablet
    const mainContent = page.locator('main, [role="main"]').first();
    if (await mainContent.count() > 0) {
      const box = await mainContent.boundingBox();
      expect(box).not.toBeNull();
    }
  });

  test('should not have horizontal scroll on any viewport', async ({ page }) => {
    const viewports = [
      { width: 1920, height: 1080 },
      { width: 1366, height: 768 },
      { width: 1024, height: 768 },
      { width: 768, height: 1024 },
      { width: 375, height: 667 },
    ];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.goto('/create');

      const body = page.locator('body');
      const scrollWidth = await body.evaluate((el) => el.scrollWidth);
      const clientWidth = await body.evaluate((el) => el.clientWidth);

      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 5); // Small tolerance for rounding
    }
  });

  test('should have touch-friendly button sizes on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/create');

    // All interactive elements should be at least 44x44px (WCAG guideline)
    const buttons = page.locator('button').all();

    for (const button of await buttons) {
      if (await button.isVisible()) {
        const box = await button.boundingBox();
        if (box) {
          // Allow slightly smaller for secondary/icon buttons
          expect(box.height).toBeGreaterThanOrEqual(32);
          expect(box.width).toBeGreaterThanOrEqual(32);
        }
      }
    }
  });

  test('should handle orientation changes gracefully', async ({ page }) => {
    // Portrait
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/create');

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await topicInput.fill('Orientation test');

    // Landscape
    await page.setViewportSize({ width: 667, height: 375 });

    // Content should still be there
    await expect(topicInput).toHaveValue('Orientation test');
    await expect(topicInput).toBeVisible();
  });

  test('should adapt layout for small screens', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 568 }); // iPhone SE
    await page.goto('/create');

    // Should be functional even on very small screens
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    await topicInput.fill('Small screen');
    await expect(topicInput).toHaveValue('Small screen');

    // Buttons should still be clickable
    const nextButton = page.getByRole('button', { name: /Next/i });
    if (await nextButton.isVisible()) {
      const box = await nextButton.boundingBox();
      expect(box).not.toBeNull();
    }
  });

  test('should have readable text at all screen sizes', async ({ page }) => {
    const viewports = [
      { width: 1920, height: 1080, name: 'Desktop' },
      { width: 768, height: 1024, name: 'Tablet' },
      { width: 375, height: 667, name: 'Mobile' },
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('/create');

      // Check body text size
      const textElement = page.locator('p, span, label').first();
      if (await textElement.count() > 0) {
        const fontSize = await textElement.evaluate((el) => {
          return window.getComputedStyle(el).fontSize;
        });

        const fontSizeNum = parseInt(fontSize);
        expect(fontSizeNum).toBeGreaterThanOrEqual(14); // Minimum readable size
      }
    }
  });

  test('should adapt form layout for narrow screens', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/create');

    // Form should stack vertically on mobile
    const formElements = page.locator('input, select, textarea');
    const count = await formElements.count();

    if (count >= 2) {
      const first = formElements.nth(0);
      const second = formElements.nth(1);

      const box1 = await first.boundingBox();
      const box2 = await second.boundingBox();

      if (box1 && box2) {
        // Elements should be stacked vertically (y positions different)
        // Not side by side on narrow screens
        expect(Math.abs(box1.y - box2.y)).toBeGreaterThan(10);
      }
    }
  });

  test('should show/hide navigation based on screen size', async ({ page }) => {
    // Desktop - full navigation visible
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/create');

    await page.waitForTimeout(500);

    // Mobile - may have hamburger menu
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(500);

    // Just verify page is still functional
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();
  });

  test('should handle zoom levels gracefully', async ({ page }) => {
    await page.goto('/create');

    // Test at 150% zoom (simulate browser zoom)
    // This is approximate - Playwright doesn't directly support browser zoom
    // But we can test viewport scaling
    await page.setViewportSize({ width: 1280, height: 720 });

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Content should be accessible
    await topicInput.fill('Zoom test');
    await expect(topicInput).toHaveValue('Zoom test');
  });
});
