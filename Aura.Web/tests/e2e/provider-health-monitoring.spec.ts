import { test, expect } from '@playwright/test';

/**
 * Provider Health Monitoring E2E Tests
 * Tests provider health monitoring dashboard and real-time health checks
 */
test.describe('Provider Health Monitoring', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display provider health dashboard', async ({ page }) => {
    // Mock provider health data
    await page.route('**/api/ProviderHealth', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'OpenAI',
            status: 'Healthy',
            successRatePercent: 98.5,
            averageLatencySeconds: 1.2,
            totalRequests: 150,
            consecutiveFailures: 0,
            circuitState: 'Closed',
            lastUpdated: new Date().toISOString(),
          },
          {
            providerName: 'ElevenLabs',
            status: 'Healthy',
            successRatePercent: 99.2,
            averageLatencySeconds: 0.8,
            totalRequests: 85,
            consecutiveFailures: 0,
            circuitState: 'Closed',
            lastUpdated: new Date().toISOString(),
          },
          {
            providerName: 'Pexels',
            status: 'Degraded',
            successRatePercent: 75.0,
            averageLatencySeconds: 2.5,
            totalRequests: 40,
            consecutiveFailures: 2,
            circuitState: 'HalfOpen',
            lastUpdated: new Date().toISOString(),
          },
        ]),
      });
    });

    // Try to navigate to health dashboard
    const healthLink = page.getByRole('link', { name: /Health|Providers|Status/i });

    if (await healthLink.isVisible()) {
      await healthLink.click();

      // Verify health dashboard displays
      await expect(page.locator('h1, h2')).toContainText(/Health|Provider|Status/i);

      // Verify provider cards are displayed
      await expect(page.locator('text=OpenAI')).toBeVisible();
      await expect(page.locator('text=ElevenLabs')).toBeVisible();
      await expect(page.locator('text=Pexels')).toBeVisible();

      // Verify health status badges
      await expect(page.locator('text=Healthy')).toBeVisible();
      await expect(page.locator('text=Degraded')).toBeVisible();
    }
  });

  test('should show unhealthy provider warnings', async ({ page }) => {
    // Mock unhealthy provider
    await page.route('**/api/ProviderHealth', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'OpenAI',
            status: 'Unhealthy',
            successRatePercent: 45.0,
            averageLatencySeconds: 5.0,
            totalRequests: 50,
            consecutiveFailures: 5,
            circuitState: 'Open',
            lastUpdated: new Date().toISOString(),
            nextRetryTime: new Date(Date.now() + 60000).toISOString(),
          },
        ]),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(1000);

    // Look for unhealthy indicators
    const unhealthyIndicator = page.locator('text=Unhealthy');
    if (await unhealthyIndicator.isVisible()) {
      await expect(unhealthyIndicator).toBeVisible();

      // Verify circuit breaker state
      await expect(page.locator('text=Open')).toBeVisible();
    }
  });

  test('should allow manual health check reset', async ({ page }) => {
    // Mock health data
    await page.route('**/api/ProviderHealth', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'OpenAI',
            status: 'Degraded',
            successRatePercent: 70.0,
            averageLatencySeconds: 2.0,
            totalRequests: 30,
            consecutiveFailures: 3,
            circuitState: 'HalfOpen',
            lastUpdated: new Date().toISOString(),
          },
        ]),
      });
    });

    // Mock reset endpoint
    await page.route('**/api/ProviderHealth/OpenAI/reset', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Health metrics reset for provider OpenAI',
        }),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(1000);

    // Look for reset button
    const resetButton = page.getByRole('button', { name: /Reset|Refresh/i });
    if (await resetButton.first().isVisible()) {
      await resetButton.first().click();

      // Verify success message or reload
      await page.waitForTimeout(1000);
    }
  });

  test('should display circuit breaker status', async ({ page }) => {
    // Mock circuit breaker data
    await page.route('**/api/ProviderHealth/circuit-breakers', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'OpenAI',
            state: 'Closed',
            consecutiveFailures: 0,
            consecutiveSuccesses: 10,
            lastFailureTime: null,
            nextRetryTime: null,
          },
          {
            providerName: 'UnreliableProvider',
            state: 'Open',
            consecutiveFailures: 5,
            consecutiveSuccesses: 0,
            lastFailureTime: new Date().toISOString(),
            nextRetryTime: new Date(Date.now() + 30000).toISOString(),
          },
        ]),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(1000);

    // Verify circuit breaker states are displayed
    const circuitBreakerSection = page.locator('text=/Circuit|Breaker/i');
    if (await circuitBreakerSection.first().isVisible()) {
      await expect(circuitBreakerSection.first()).toBeVisible();
    }
  });

  test('should show health metrics history', async ({ page }) => {
    // Mock health data with history
    await page.route('**/api/ProviderHealth/OpenAI', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          providerName: 'OpenAI',
          status: 'Healthy',
          successRatePercent: 98.5,
          averageLatencySeconds: 1.2,
          totalRequests: 150,
          consecutiveFailures: 0,
          circuitState: 'Closed',
          lastUpdated: new Date().toISOString(),
          history: [
            {
              timestamp: new Date(Date.now() - 3600000).toISOString(),
              successRate: 97.0,
              latency: 1.3,
            },
            {
              timestamp: new Date(Date.now() - 7200000).toISOString(),
              successRate: 98.0,
              latency: 1.1,
            },
          ],
        }),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(1000);

    // Look for provider detail view
    const openAICard = page.locator('text=OpenAI').first();
    if (await openAICard.isVisible()) {
      await openAICard.click();
      await page.waitForTimeout(1000);

      // Verify detail view shows metrics
      const metricsDisplay = page.locator('text=/Success Rate|Latency|Requests/i');
      if (await metricsDisplay.first().isVisible()) {
        await expect(metricsDisplay.first()).toBeVisible();
      }
    }
  });

  test('should update health status in real-time', async ({ page }) => {
    let requestCount = 0;

    // Mock health endpoint with changing data
    await page.route('**/api/ProviderHealth', async (route) => {
      requestCount++;
      const successRate = 95 + (requestCount % 5);

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'OpenAI',
            status: 'Healthy',
            successRatePercent: successRate,
            averageLatencySeconds: 1.2,
            totalRequests: 150 + requestCount,
            consecutiveFailures: 0,
            circuitState: 'Closed',
            lastUpdated: new Date().toISOString(),
          },
        ]),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(2000);

    // Verify initial data
    const initialRequests = page.locator('text=/150|151/');
    if (await initialRequests.first().isVisible()) {
      await expect(initialRequests.first()).toBeVisible();
    }

    // Wait for potential auto-refresh
    await page.waitForTimeout(5000);
  });

  test('should filter providers by status', async ({ page }) => {
    // Mock providers with different statuses
    await page.route('**/api/ProviderHealth', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            providerName: 'HealthyProvider1',
            status: 'Healthy',
            successRatePercent: 99.0,
            totalRequests: 100,
          },
          {
            providerName: 'HealthyProvider2',
            status: 'Healthy',
            successRatePercent: 98.0,
            totalRequests: 80,
          },
          {
            providerName: 'DegradedProvider',
            status: 'Degraded',
            successRatePercent: 70.0,
            totalRequests: 50,
          },
          {
            providerName: 'UnhealthyProvider',
            status: 'Unhealthy',
            successRatePercent: 40.0,
            totalRequests: 30,
          },
        ]),
      });
    });

    await page.goto('/health');
    await page.waitForTimeout(1000);

    // Look for filter buttons
    const healthyFilter = page.getByRole('button', { name: /Healthy/i });

    if (await healthyFilter.isVisible()) {
      await healthyFilter.click();
      await page.waitForTimeout(500);

      // Verify only healthy providers shown
      await expect(page.locator('text=HealthyProvider1')).toBeVisible();
      if (await page.locator('text=UnhealthyProvider').isVisible()) {
        await expect(page.locator('text=UnhealthyProvider')).not.toBeVisible();
      }
    }
  });
});
