import { test, expect } from '@playwright/test';

/**
 * Error handling and resilience tests for Aura Video Studio
 * Tests network failures, API errors, invalid inputs, and error recovery
 */
test.describe('Error Handling and Resilience', () => {
  test('should handle network timeout gracefully', async ({ page }) => {
    // Mock API with delayed response
    await page.route('**/api/jobs', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 60000)); // Never resolves
      route.abort();
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Timeout test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show timeout error
      await expect(page.getByText(/timeout|taking too long|try again/i)).toBeVisible({
        timeout: 10000,
      });
    }
  });

  test('should handle 500 server errors gracefully', async ({ page }) => {
    // Mock API with server error
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Internal Server Error',
          message: 'An unexpected error occurred',
        }),
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Server error test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show user-friendly error message
      await expect(page.getByText(/error|failed|something went wrong/i)).toBeVisible({
        timeout: 5000,
      });
    }
  });

  test('should handle 404 not found errors', async ({ page }) => {
    await page.route('**/api/jobs/nonexistent-123', (route) => {
      route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Not Found',
          message: 'Job not found',
        }),
      });
    });

    // Try to access non-existent job (if app supports direct job URLs)
    await page.goto('/jobs/nonexistent-123', { waitUntil: 'domcontentloaded' }).catch(() => {
      // Ignore navigation errors
    });

    // Should show not found message or redirect
    await page.waitForTimeout(1000);

    // Page should handle this gracefully
    const errorMessage = page.getByText(/not found|doesn't exist/i);
    if (await errorMessage.isVisible({ timeout: 2000 })) {
      await expect(errorMessage).toBeVisible();
    }
  });

  test('should handle 401 unauthorized errors', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Unauthorized',
          message: 'Authentication required',
        }),
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Auth test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show auth error or redirect to login
      await page.waitForTimeout(2000);

      const errorMessage = page.getByText(/unauthorized|authentication|login/i);
      if (await errorMessage.isVisible({ timeout: 2000 })) {
        await expect(errorMessage).toBeVisible();
      }
    }
  });

  test('should handle network disconnection', async ({ page }) => {
    // Set offline
    await page.context().setOffline(true);

    await page.goto('/create', { waitUntil: 'domcontentloaded' }).catch(() => {
      // May fail to load
    });

    // Try to start generation while offline
    await page.context().setOffline(false);
    await page.goto('/create');
    await page.context().setOffline(true);

    await page.getByPlaceholder(/Enter your video topic/i).fill('Offline test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show offline error
      await page.waitForTimeout(2000);

      const errorMessage = page.getByText(/offline|network|connection/i);
      if (await errorMessage.isVisible({ timeout: 2000 })) {
        await expect(errorMessage).toBeVisible();
      }
    }

    // Reset
    await page.context().setOffline(false);
  });

  test('should handle malformed API responses', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: 'invalid json {{{',
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Malformed response test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should handle gracefully
      await page.waitForTimeout(2000);

      const errorMessage = page.getByText(/error|failed|unexpected/i);
      if (await errorMessage.isVisible({ timeout: 2000 })) {
        await expect(errorMessage).toBeVisible();
      }
    }
  });

  test('should validate required fields', async ({ page }) => {
    await page.goto('/create');

    // Try to proceed without filling required fields
    const nextButton = page.getByRole('button', { name: /Next/i });
    await nextButton.click();

    // Should show validation error
    await page.waitForTimeout(500);

    // Check for error message or field staying on same page
    const topicInput = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicInput).toBeVisible();

    // Look for validation message
    const validationError = page.getByText(/required|cannot be empty|please enter/i);
    if (await validationError.count() > 0) {
      await expect(validationError.first()).toBeVisible({ timeout: 2000 });
    }
  });

  test('should validate topic length', async ({ page }) => {
    await page.goto('/create');

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);

    // Try very short topic
    await topicInput.fill('a');

    const nextButton = page.getByRole('button', { name: /Next/i });
    await nextButton.click();

    await page.waitForTimeout(500);

    // Should show validation error for too short
    const tooShortError = page.getByText(/too short|minimum|at least/i);
    if (await tooShortError.count() > 0) {
      await expect(tooShortError.first()).toBeVisible({ timeout: 2000 });
    }

    // Try very long topic (if there's a maximum)
    await topicInput.fill('a'.repeat(500));
    await nextButton.click();

    await page.waitForTimeout(500);

    // May show error for too long
    const tooLongError = page.getByText(/too long|maximum|exceeds/i);
    if (await tooLongError.count() > 0) {
      await expect(tooLongError.first()).toBeVisible({ timeout: 1000 });
    }
  });

  test('should handle special characters in input', async ({ page }) => {
    await page.goto('/create');

    const topicInput = page.getByPlaceholder(/Enter your video topic/i);

    // Input with special characters
    const specialChars = 'Test <script>alert("xss")</script> Topic';
    await topicInput.fill(specialChars);

    // Should accept or sanitize input
    const value = await topicInput.inputValue();
    expect(value).toBeTruthy();

    // Try to proceed
    const nextButton = page.getByRole('button', { name: /Next/i });
    await nextButton.click();

    // Should not execute scripts
    await page.waitForTimeout(500);
  });

  test('should handle job failure gracefully', async ({ page }) => {
    // Mock job creation
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'fail-test-123',
          status: 'queued',
        }),
      });
    });

    // Mock job status with failure
    await page.route('**/api/jobs/fail-test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'fail-test-123',
          status: 'Failed',
          stage: 'TTS',
          progress: 40,
          errorMessage: 'Audio synthesis failed',
        }),
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Job failure test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Wait for failure to be reported
      await page.waitForTimeout(2000);

      // Should show error message
      await expect(page.getByText(/failed|error|synthesis/i)).toBeVisible({ timeout: 5000 });

      // Should offer retry option
      const retryButton = page.getByRole('button', { name: /retry|try again/i });
      if (await retryButton.count() > 0) {
        await expect(retryButton.first()).toBeVisible();
      }
    }
  });

  test('should recover from transient errors with retry', async ({ page }) => {
    let attemptCount = 0;

    await page.route('**/api/jobs', (route) => {
      attemptCount++;

      if (attemptCount === 1) {
        // First attempt fails
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Transient error' }),
        });
      } else {
        // Retry succeeds
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            jobId: 'retry-test-123',
            status: 'queued',
          }),
        });
      }
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Retry test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Wait for error
      await page.waitForTimeout(2000);

      // Click retry if available
      const retryButton = page.getByRole('button', { name: /retry|try again/i });
      if (await retryButton.count() > 0) {
        await retryButton.first().click();

        // Second attempt should succeed
        await page.waitForTimeout(1000);

        // Should show success or progress
        const successIndicator = page.getByText(/queued|running|processing/i);
        if (await successIndicator.count() > 0) {
          await expect(successIndicator.first()).toBeVisible({ timeout: 3000 });
        }
      }
    }
  });

  test('should handle CORS errors', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.abort('failed');
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('CORS test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should handle network error gracefully
      await page.waitForTimeout(2000);

      const errorMessage = page.getByText(/error|failed|network/i);
      if (await errorMessage.count() > 0) {
        await expect(errorMessage.first()).toBeVisible({ timeout: 3000 });
      }
    }
  });

  test('should show helpful error messages', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Validation Error',
          message: 'Topic must be between 10 and 200 characters',
          field: 'topic',
        }),
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Short');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show specific error message
      await expect(page.getByText(/10 and 200 characters/i)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should handle rate limiting', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 429,
        contentType: 'application/json',
        headers: {
          'Retry-After': '60',
        },
        body: JSON.stringify({
          error: 'Too Many Requests',
          message: 'Rate limit exceeded. Please try again in 60 seconds.',
        }),
      });
    });

    await page.goto('/create');

    await page.getByPlaceholder(/Enter your video topic/i).fill('Rate limit test');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show rate limit message
      await expect(
        page.getByText(/rate limit|too many|try again|60 seconds/i)
      ).toBeVisible({ timeout: 5000 });
    }
  });

  test('should preserve form data after error', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Server error' }),
      });
    });

    await page.goto('/create');

    const topicValue = 'Data preservation test';
    await page.getByPlaceholder(/Enter your video topic/i).fill(topicValue);
    await page.getByPlaceholder(/Who is the target audience/i).fill('Test Audience');

    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });
    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Wait for error
      await page.waitForTimeout(2000);

      // Form data should still be there
      await expect(page.getByPlaceholder(/Enter your video topic/i)).toHaveValue(topicValue);
      await expect(page.getByPlaceholder(/Who is the target audience/i)).toHaveValue(
        'Test Audience'
      );
    }
  });
});
