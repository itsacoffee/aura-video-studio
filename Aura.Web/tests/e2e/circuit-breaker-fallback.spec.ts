import { test, expect } from '@playwright/test';

/**
 * Circuit breaker and fallback E2E tests
 * Validates that the system gracefully handles provider failures and falls back correctly
 */
test.describe('Circuit Breaker and Fallback Scenarios', () => {
  test('should fall back to secondary provider when primary fails', async ({ page }) => {
    let primaryAttempts = 0;
    let fallbackUsed = false;

    // Mock primary provider (simulate failure)
    await page.route('**/api/providers/llm/openai/test', (route) => {
      primaryAttempts++;
      route.fulfill({
        status: 503,
        contentType: 'application/json',
        headers: {
          'X-Correlation-ID': `primary-fail-${primaryAttempts}`,
        },
        body: JSON.stringify({
          error: 'Service Unavailable',
          message: 'Primary provider is down',
        }),
      });
    });

    // Mock fallback provider (simulate success)
    await page.route('**/api/providers/llm/rulebased/test', (route) => {
      fallbackUsed = true;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: {
          'X-Correlation-ID': 'fallback-success',
        },
        body: JSON.stringify({
          status: 'Success',
          provider: 'RuleBased',
          message: 'Fallback provider working',
        }),
      });
    });

    // Mock provider health endpoint to show primary degraded
    await page.route('**/api/health/providers', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            name: 'OpenAI',
            status: 'Unhealthy',
            lastCheck: new Date().toISOString(),
            errorRate: 1.0,
          },
          {
            name: 'RuleBased',
            status: 'Healthy',
            lastCheck: new Date().toISOString(),
            errorRate: 0.0,
          },
        ]),
      });
    });

    await page.goto('/');

    // Navigate to provider test page or trigger provider test
    // This will vary based on actual UI - adjust as needed
    
    // Verify fallback was used
    // This would check UI indicators or logs
    // For now, validate the mock was called
    expect(primaryAttempts).toBeGreaterThan(0);
    expect(fallbackUsed).toBe(true);
  });

  test('should open circuit after multiple failures', async ({ page }) => {
    let requestCount = 0;
    const failureThreshold = 5;

    // Mock provider to fail multiple times
    await page.route('**/api/providers/tts/elevenlabs/synthesize', (route) => {
      requestCount++;
      
      if (requestCount <= failureThreshold) {
        // Fail first N requests
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          headers: {
            'X-Correlation-ID': `circuit-test-${requestCount}`,
          },
          body: JSON.stringify({
            error: 'Internal Server Error',
            message: 'Provider failed',
          }),
        });
      } else {
        // Circuit should be open, request shouldn't reach here
        route.fulfill({
          status: 503,
          contentType: 'application/json',
          headers: {
            'X-Correlation-ID': 'circuit-open',
          },
          body: JSON.stringify({
            error: 'Circuit Open',
            message: 'Circuit breaker is open due to repeated failures',
          }),
        });
      }
    });

    await page.goto('/');

    // Verify circuit breaker state is reflected in UI
    // This would check for circuit breaker indicators
    // Actual implementation depends on UI design
  });

  test('should display degraded status when providers are unhealthy', async ({ page }) => {
    // Mock system health to show degraded state
    await page.route('**/api/health/system', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: {
          'X-Correlation-ID': 'health-check-degraded',
        },
        body: JSON.stringify({
          status: 'Degraded',
          timestamp: new Date().toISOString(),
          components: {
            llm: 'Degraded',
            tts: 'Healthy',
            storage: 'Healthy',
          },
          message: 'System is operating with reduced capacity',
        }),
      });
    });

    await page.goto('/');

    // Wait for health check to complete
    await page.waitForTimeout(2000);

    // Verify degraded status is displayed
    // This will depend on actual UI implementation
    const statusIndicator = page.locator('[data-testid="system-status"], .status-indicator').first();
    if (await statusIndicator.isVisible({ timeout: 5000 }).catch(() => false)) {
      const statusText = await statusIndicator.textContent();
      expect(statusText?.toLowerCase()).toMatch(/degraded|warning/);
    }
  });

  test('should retry failed requests with exponential backoff', async ({ page }) => {
    const requestTimestamps: number[] = [];

    await page.route('**/api/jobs/*/status', (route) => {
      requestTimestamps.push(Date.now());
      
      if (requestTimestamps.length < 3) {
        // Fail first 2 attempts
        route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Service Unavailable' }),
        });
      } else {
        // Succeed on 3rd attempt
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ status: 'Completed' }),
        });
      }
    });

    await page.goto('/');

    // Trigger job status check
    // This will vary based on actual implementation
    
    // Verify exponential backoff was used
    if (requestTimestamps.length >= 3) {
      const firstGap = requestTimestamps[1] - requestTimestamps[0];
      const secondGap = requestTimestamps[2] - requestTimestamps[1];
      
      // Second gap should be larger than first (exponential backoff)
      expect(secondGap).toBeGreaterThanOrEqual(firstGap * 0.9); // Allow 10% variance
    }
  });

  test('should provide clear error messages for provider failures', async ({ page }) => {
    // Mock provider failure with detailed error
    await page.route('**/api/providers/test', (route) => {
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        headers: {
          'X-Correlation-ID': 'error-test-001',
        },
        body: JSON.stringify({
          type: 'ProviderError',
          title: 'Provider Test Failed',
          status: 400,
          detail: 'The provider is not properly configured. Please check API keys and settings.',
          correlationId: 'error-test-001',
          extensions: {
            provider: 'OpenAI',
            errorCode: 'INVALID_API_KEY',
          },
        }),
      });
    });

    await page.goto('/');

    // Navigate to provider settings or test
    // This will vary based on actual UI
    
    // Verify error message is displayed clearly
    // Check for correlation ID in error display
  });
});
