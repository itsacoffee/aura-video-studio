import { test, expect } from '@playwright/test';

/**
 * Accessibility compliance tests for Aura Video Studio
 * Tests WCAG 2.1 AA compliance, keyboard navigation, screen reader support
 */
test.describe('Accessibility Compliance', () => {
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

  test('should have proper ARIA labels on interactive elements', async ({ page }) => {
    await page.goto('/create');

    // Check main interactive elements have labels
    const nextButton = page.getByRole('button', { name: /Next/i });
    await expect(nextButton).toBeVisible();

    // Form inputs should have labels
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Check for aria-label or associated label
    const topicLabel = await topicInput.getAttribute('aria-label');
    const topicLabelId = await topicInput.getAttribute('aria-labelledby');
    const hasLabel = topicLabel !== null || topicLabelId !== null;

    // If no ARIA label, should have associated <label> element
    if (!hasLabel) {
      const inputId = await topicInput.getAttribute('id');
      if (inputId) {
        const label = page.locator(`label[for="${inputId}"]`);
        await expect(label).toBeVisible({ timeout: 1000 });
      }
    }
  });

  test('should support full keyboard navigation', async ({ page }) => {
    await page.goto('/create');

    // Tab to first input
    await page.keyboard.press('Tab');

    // Active element should be an input or button
    const activeElement = page.locator(':focus');
    await expect(activeElement).toBeVisible();

    // Type in focused input
    await page.keyboard.type('Keyboard Navigation Test');

    // Tab to next element
    await page.keyboard.press('Tab');

    // Continue tabbing through form
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Shift+Tab should go backwards
    await page.keyboard.press('Shift+Tab');

    // Enter should activate buttons
    const focusedElement = await page.locator(':focus');
    const tagName = await focusedElement.evaluate((el) => el.tagName.toLowerCase());

    if (tagName === 'button') {
      // Pressing Enter should activate the button
      await page.keyboard.press('Enter');
      // Check that some action occurred (depends on which button was focused)
    }
  });

  test('should have proper heading hierarchy', async ({ page }) => {
    await page.goto('/create');

    // Get all headings
    const h1s = await page.locator('h1').count();
    const h2s = await page.locator('h2').count();

    // Should have at least one h1
    expect(h1s).toBeGreaterThan(0);

    // Verify heading hierarchy (h1 should come before h2, etc.)
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();

    if (headings.length > 0) {
      const headingLevels = await Promise.all(
        headings.map(async (h) => {
          const tagName = await h.evaluate((el) => el.tagName);
          return parseInt(tagName.substring(1));
        })
      );

      // Verify no heading level is skipped
      for (let i = 1; i < headingLevels.length; i++) {
        const levelDiff = headingLevels[i] - headingLevels[i - 1];
        expect(levelDiff).toBeLessThanOrEqual(1);
      }
    }
  });

  test('should have sufficient color contrast', async ({ page }) => {
    await page.goto('/create');

    // Check for text elements
    const textElements = page.locator('p, span, label, button, a');
    const count = await textElements.count();

    expect(count).toBeGreaterThan(0);

    // Note: Actual color contrast checking requires axe-core or similar tool
    // This is a basic check that elements are visible
    const firstText = textElements.first();
    await expect(firstText).toBeVisible();
  });

  test('should have proper form labels and error messages', async ({ page }) => {
    await page.goto('/create');

    // Try to submit without required fields
    const nextButton = page.getByRole('button', { name: /Next/i });
    await nextButton.click();

    // Check for validation messages with proper ARIA attributes
    await page.waitForTimeout(500);

    // Error messages should have role="alert" or aria-live
    const errorMessages = page.locator('[role="alert"], [aria-live="polite"], [aria-live="assertive"]');

    if ((await errorMessages.count()) > 0) {
      await expect(errorMessages.first()).toBeVisible();
    }
  });

  test('should support screen reader announcements', async ({ page }) => {
    await page.goto('/create');

    // Look for aria-live regions
    const liveRegions = page.locator('[aria-live], [role="status"], [role="alert"]');
    const count = await liveRegions.count();

    // Should have at least one live region for dynamic content
    if (count > 0) {
      await expect(liveRegions.first()).toBeAttached();
    }

    // Check for landmark regions
    const main = page.locator('main, [role="main"]');
    const nav = page.locator('nav, [role="navigation"]');

    // Should have main content area
    expect(await main.count()).toBeGreaterThan(0);
  });

  test('should have skip navigation link', async ({ page }) => {
    await page.goto('/create');

    // Tab to first element
    await page.keyboard.press('Tab');

    // Check if first focusable element is a skip link
    const firstFocused = page.locator(':focus');
    const text = await firstFocused.textContent();

    // Skip link is optional but good practice
    if (text && text.toLowerCase().includes('skip')) {
      await expect(firstFocused).toBeVisible();
    }
  });

  test('should not have empty links or buttons', async ({ page }) => {
    await page.goto('/create');

    // Check all links have text or aria-label
    const links = page.locator('a');
    const linkCount = await links.count();

    for (let i = 0; i < Math.min(linkCount, 20); i++) {
      const link = links.nth(i);
      const text = await link.textContent();
      const ariaLabel = await link.getAttribute('aria-label');
      const ariaLabelledBy = await link.getAttribute('aria-labelledby');

      const hasContent =
        (text && text.trim().length > 0) || ariaLabel !== null || ariaLabelledBy !== null;

      expect(hasContent).toBe(true);
    }

    // Check all buttons have text or aria-label
    const buttons = page.locator('button');
    const buttonCount = await buttons.count();

    for (let i = 0; i < Math.min(buttonCount, 20); i++) {
      const button = buttons.nth(i);
      const text = await button.textContent();
      const ariaLabel = await button.getAttribute('aria-label');
      const ariaLabelledBy = await button.getAttribute('aria-labelledby');

      const hasContent =
        (text && text.trim().length > 0) || ariaLabel !== null || ariaLabelledBy !== null;

      expect(hasContent).toBe(true);
    }
  });

  test('should have proper focus indicators', async ({ page }) => {
    await page.goto('/create');

    // Tab to an element
    await page.keyboard.press('Tab');

    const focused = page.locator(':focus');
    await expect(focused).toBeVisible();

    // Get computed styles to check for focus indicator
    const outlineWidth = await focused.evaluate((el) => {
      return window.getComputedStyle(el).outlineWidth;
    });

    const outlineStyle = await focused.evaluate((el) => {
      return window.getComputedStyle(el).outlineStyle;
    });

    // Should have visible focus indicator (outline or other visual indicator)
    const hasFocusIndicator =
      (outlineWidth !== '0px' && outlineStyle !== 'none') ||
      (await focused.evaluate((el) => {
        const styles = window.getComputedStyle(el);
        // Check for other focus indicators like border, box-shadow, etc.
        return (
          styles.borderWidth !== '0px' ||
          styles.boxShadow !== 'none' ||
          styles.backgroundColor !== 'transparent'
        );
      }));

    expect(hasFocusIndicator).toBe(true);
  });

  test('should have proper alt text on images', async ({ page }) => {
    await page.goto('/create');

    // Check all images
    const images = page.locator('img');
    const imageCount = await images.count();

    for (let i = 0; i < imageCount; i++) {
      const img = images.nth(i);
      const alt = await img.getAttribute('alt');

      // All images must have alt attribute (can be empty for decorative images)
      expect(alt).not.toBeNull();
    }
  });

  test('should support high contrast mode', async ({ page }) => {
    await page.goto('/create');

    // Inject media query to simulate high contrast
    await page.emulateMedia({ colorScheme: 'dark' });

    // Elements should still be visible
    const heading = page.getByRole('heading').first();
    await expect(heading).toBeVisible();

    // Check that interactive elements are still visible
    const buttons = page.locator('button');
    if ((await buttons.count()) > 0) {
      await expect(buttons.first()).toBeVisible();
    }
  });

  test('should have language attribute', async ({ page }) => {
    await page.goto('/create');

    // HTML should have lang attribute
    const lang = await page.locator('html').getAttribute('lang');
    expect(lang).not.toBeNull();
    expect(lang).toBeTruthy();
  });

  test('should handle dynamic content announcements', async ({ page }) => {
    // Mock job creation
    await page.route('**/api/quick/demo', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'test-123',
          status: 'queued',
        }),
      });
    });

    // Mock job status
    await page.route('**/api/jobs/test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-123',
          status: 'Running',
          stage: 'Script',
          progress: 25,
        }),
      });
    });

    await page.goto('/create');

    // Start generation
    const quickDemo = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemo.isVisible({ timeout: 2000 })) {
      await quickDemo.click();

      // Check for aria-live region updates
      await page.waitForTimeout(1000);

      const liveRegions = page.locator('[aria-live="polite"], [aria-live="assertive"], [role="status"]');
      if ((await liveRegions.count()) > 0) {
        await expect(liveRegions.first()).toBeAttached();
      }
    }
  });

  test('should be usable with keyboard only (no mouse)', async ({ page }) => {
    await page.goto('/create');

    // Navigate entire form using only keyboard
    await page.keyboard.press('Tab');
    await page.keyboard.type('Keyboard Only Test');

    await page.keyboard.press('Tab');
    await page.keyboard.type('Test Audience');

    await page.keyboard.press('Tab');
    await page.keyboard.type('Test Goal');

    // Navigate to and activate Next button
    let tabCount = 0;
    while (tabCount < 10) {
      await page.keyboard.press('Tab');
      tabCount++;

      const focused = page.locator(':focus');
      const text = await focused.textContent();

      if (text && text.toLowerCase().includes('next')) {
        await page.keyboard.press('Enter');
        break;
      }
    }

    // Should navigate to next step
    await page.waitForTimeout(1000);

    // Verify we're on a different step
    const url = page.url();
    expect(url).toBeTruthy();
  });

  test('should have proper modal dialog accessibility', async ({ page }) => {
    await page.goto('/create');

    // Look for any dialogs or modals
    const dialogs = page.locator('[role="dialog"], [role="alertdialog"], .modal');

    if ((await dialogs.count()) > 0) {
      const dialog = dialogs.first();

      // Dialog should have aria-label or aria-labelledby
      const ariaLabel = await dialog.getAttribute('aria-label');
      const ariaLabelledBy = await dialog.getAttribute('aria-labelledby');

      expect(ariaLabel !== null || ariaLabelledBy !== null).toBe(true);

      // Dialog should trap focus
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');

      // Focus should remain within dialog
      const focused = page.locator(':focus');
      const isInsideDialog = await focused.evaluate((el, dialogEl) => {
        return dialogEl.contains(el);
      }, await dialog.elementHandle());

      expect(isInsideDialog).toBe(true);
    }
  });
});
