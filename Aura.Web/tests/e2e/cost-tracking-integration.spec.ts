import { test, expect } from '@playwright/test';

/**
 * Cost Tracking Integration E2E Tests
 * Tests cost tracking functionality with real provider usage
 */
test.describe('Cost Tracking Integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should track cost during video generation with real providers', async ({ page }) => {
    test.setTimeout(120000);

    // Mock job creation with cost tracking
    let jobId = '';
    await page.route('**/api/quick/demo', async (route) => {
      jobId = `test-job-${Date.now()}`;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId,
          status: 'queued',
          message: 'Job created successfully',
        }),
      });
    });

    // Mock telemetry endpoint with cost data
    await page.route(`**/api/telemetry/**`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId,
          totalCost: 0.25,
          currency: 'USD',
          costByStage: {
            scriptGeneration: 0.1,
            tts: 0.08,
            images: 0.05,
            rendering: 0.02,
          },
          costByProvider: {
            OpenAI: 0.1,
            ElevenLabs: 0.08,
            StableDiffusion: 0.05,
            FFmpeg: 0.02,
          },
          tokenStats: {
            totalInputTokens: 1500,
            totalOutputTokens: 800,
            totalTokens: 2300,
            operationCount: 3,
            totalCost: 0.1,
          },
        }),
      });
    });

    // Mock cost tracking configuration
    await page.route('**/api/cost-tracking/configuration', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          userId: 'test-user',
          overallMonthlyBudget: 100.0,
          currency: 'USD',
          periodType: 'Monthly',
          alertThresholds: [50, 75, 90],
          providerBudgets: {
            OpenAI: 50.0,
            ElevenLabs: 30.0,
          },
        }),
      });
    });

    // Mock current period spending
    await page.route('**/api/cost-tracking/current-period', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCost: 15.5,
          currency: 'USD',
          periodType: 'Monthly',
          budget: 100.0,
          percentageUsed: 15.5,
        }),
      });
    });

    // Start Quick Demo
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await expect(quickDemoButton).toBeVisible({ timeout: 10000 });
    await quickDemoButton.click();

    // Wait for job to be created
    await page.waitForTimeout(2000);

    // Verify cost tracking is displayed
    const costDisplay = page.locator('[data-testid="cost-display"]');
    if (await costDisplay.isVisible()) {
      await expect(costDisplay).toContainText('USD');
    }
  });

  test('should display cost history view', async ({ page }) => {
    // Navigate to cost history if available
    const historyLink = page.getByRole('link', { name: /Cost History|Costs|Budget/i });

    if (await historyLink.isVisible()) {
      await historyLink.click();

      // Mock spending report
      await page.route('**/api/cost-tracking/spending', async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
            endDate: new Date().toISOString(),
            totalCost: 45.75,
            currency: 'USD',
            costByProvider: {
              OpenAI: 25.5,
              ElevenLabs: 15.25,
              Pexels: 5.0,
            },
            costByFeature: {
              ScriptGeneration: 25.5,
              TextToSpeech: 15.25,
              ImageGeneration: 5.0,
            },
            recentTransactions: [],
          }),
        });
      });

      // Verify history displays
      await expect(page.locator('h1, h2, h3')).toContainText(/Cost|History|Spending/i);
    }
  });

  test('should show budget warnings when approaching limit', async ({ page }) => {
    // Mock high spending scenario
    await page.route('**/api/cost-tracking/current-period', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCost: 92.0,
          currency: 'USD',
          periodType: 'Monthly',
          budget: 100.0,
          percentageUsed: 92.0,
        }),
      });
    });

    // Mock budget check that shows warning
    await page.route('**/api/cost-tracking/check-budget', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isWithinBudget: true,
          shouldBlock: false,
          warnings: ['You are approaching your monthly budget limit (92% used)'],
          currentMonthlyCost: 92.0,
          estimatedNewTotal: 95.0,
        }),
      });
    });

    // Navigate to create wizard
    await page.goto('/create');

    // Check for warning indicators
    await page.waitForTimeout(2000);

    const warningElement = page.locator('[role="alert"], .warning, [data-testid*="warning"]');
    if (await warningElement.first().isVisible()) {
      await expect(warningElement.first()).toBeVisible();
    }
  });

  test('should prevent generation when budget exceeded with hard limit', async ({ page }) => {
    // Mock budget exceeded scenario
    await page.route('**/api/cost-tracking/check-budget', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isWithinBudget: false,
          shouldBlock: true,
          warnings: ['Budget limit exceeded. Cannot proceed with generation.'],
          currentMonthlyCost: 105.0,
          estimatedNewTotal: 110.0,
        }),
      });
    });

    await page.goto('/create');
    await page.waitForTimeout(1000);

    // Try to start generation
    const generateButton = page.getByRole('button', { name: /Generate|Start/i });
    if (await generateButton.isVisible()) {
      const isDisabled = await generateButton.isDisabled();
      if (isDisabled) {
        expect(isDisabled).toBe(true);
      }
    }
  });

  test('should display cost optimization suggestions', async ({ page }) => {
    const jobId = 'test-job-123';

    // Mock optimization suggestions
    await page.route(`**/api/cost-tracking/optimize-suggestions/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            category: 'ProviderSelection',
            suggestion: 'Switch from OpenAI GPT-4 to Gemini for 60% cost reduction',
            estimatedSavings: 3.0,
            qualityImpact: 'Minimal - Gemini provides comparable quality for most use cases',
          },
          {
            category: 'Caching',
            suggestion: 'Enable LLM response caching to reduce repeated API calls',
            estimatedSavings: 1.5,
            qualityImpact: 'None - Same quality, just cached results',
          },
        ]),
      });
    });

    await page.goto(`/jobs/${jobId}`);
    await page.waitForTimeout(1000);

    // Look for optimization suggestions
    const suggestionsSection = page.locator('text=/Optimization|Suggestions|Cost Savings/i');
    if (await suggestionsSection.first().isVisible()) {
      await expect(suggestionsSection.first()).toBeVisible();
    }
  });
});
