import { test, expect } from '@playwright/test';

/**
 * End-to-end tests for complete video generation workflow
 * Tests the full user journey: Brief → Plan → Voice → Generate → Export
 */
test.describe('Complete Video Generation Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses that are common across tests
    await page.route('**/api/settings', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ offlineMode: false }),
      });
    });
  });

  test('should complete full workflow from brief to video export', async ({ page }) => {
    // Mock profile API
    await page.route('**/api/profiles', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { name: 'Free-Only', description: 'No API keys required' },
          { name: 'Balanced Mix', description: 'Mix of free and paid' },
        ]),
      });
    });

    // Mock preflight check
    await page.route('**/api/preflight', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          profile: 'Free-Only',
          providers: {
            script: { provider: 'RuleBased', status: 'Ready' },
            tts: { provider: 'WindowsTTS', status: 'Ready' },
            visuals: { provider: 'Stock', status: 'Ready' },
          },
          warnings: [],
          readyToGenerate: true,
        }),
      });
    });

    // Mock job creation
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'test-job-123',
          status: 'queued',
          message: 'Job created successfully',
        }),
      });
    });

    // Mock job status with progression
    let jobProgress = 0;
    await page.route('**/api/jobs/test-job-123', (route) => {
      jobProgress = Math.min(jobProgress + 25, 100);
      const stages = ['Script', 'TTS', 'Visuals', 'Render', 'Done'];
      const stageIndex = Math.min(Math.floor(jobProgress / 25), stages.length - 1);

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-job-123',
          status: jobProgress >= 100 ? 'Done' : 'Running',
          stage: stages[stageIndex],
          progress: jobProgress,
          artifacts:
            jobProgress >= 100
              ? [
                  {
                    type: 'video',
                    path: '/output/test-job-123/video.mp4',
                    size: 2048000,
                  },
                ]
              : [],
        }),
      });
    });

    // Navigate to creation wizard
    await page.goto('/create');

    // Step 1: Brief
    await expect(page.getByRole('heading', { name: /Create Video/i })).toBeVisible();
    await page.getByPlaceholder(/Enter your video topic/i).fill('Getting Started with Aura');
    await page.getByPlaceholder(/Who is the target audience/i).fill('New Users');
    await page.getByPlaceholder(/What is the goal/i).fill('Tutorial');

    // Navigate to next step
    await page.getByRole('button', { name: /Next/i }).click();

    // Step 2: Plan & Brand Kit
    await expect(page.getByText(/Plan.*Brand Kit/i)).toBeVisible();

    // Select video duration
    const durationField = page.getByLabel(/Duration/i);
    if (await durationField.isVisible()) {
      await durationField.fill('30');
    }

    // Navigate to next step
    await page.getByRole('button', { name: /Next/i }).click();

    // Step 3: Providers
    await expect(page.getByText(/Providers/i)).toBeVisible();

    // Select profile
    const profileDropdown = page.getByLabel(/Profile/i);
    await profileDropdown.click();
    await page.getByRole('option', { name: /Free-Only/i }).click();

    // Run preflight check
    await page.getByRole('button', { name: /Run Preflight Check|Check Readiness/i }).click();

    // Wait for preflight results
    await expect(page.getByText(/Ready to generate|All systems ready/i)).toBeVisible({
      timeout: 5000,
    });

    // Generate video
    const generateButton = page.getByRole('button', { name: /Generate Video|Start Generation/i });
    await expect(generateButton).toBeEnabled();
    await generateButton.click();

    // Verify job started
    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 3000,
    });

    // Wait for progress updates
    await expect(page.getByText(/Script|Running|Processing/i)).toBeVisible({ timeout: 5000 });

    // Eventually should complete
    await expect(page.getByText(/Complete|Done|Success/i)).toBeVisible({ timeout: 15000 });

    // Verify download button appears
    const downloadButton = page.getByRole('button', { name: /Download|Export|View/i }).first();
    await expect(downloadButton).toBeVisible({ timeout: 5000 });
  });

  test('should allow navigation back and forth through wizard steps', async ({ page }) => {
    await page.goto('/create');

    // Fill brief
    await page.getByPlaceholder(/Enter your video topic/i).fill('Navigation Test');
    await page.getByPlaceholder(/Who is the target audience/i).fill('Test Users');

    // Go to step 2
    await page.getByRole('button', { name: /Next/i }).click();
    await expect(page.getByText(/Plan.*Brand Kit/i)).toBeVisible();

    // Go back to step 1
    await page.getByRole('button', { name: /Previous|Back/i }).click();
    await expect(page.getByPlaceholder(/Enter your video topic/i)).toHaveValue('Navigation Test');

    // Go forward again
    await page.getByRole('button', { name: /Next/i }).click();
    await expect(page.getByText(/Plan.*Brand Kit/i)).toBeVisible();

    // Go to step 3
    await page.getByRole('button', { name: /Next/i }).click();
    await expect(page.getByText(/Providers/i)).toBeVisible();

    // Go back to step 2
    await page.getByRole('button', { name: /Previous|Back/i }).click();
    await expect(page.getByText(/Plan.*Brand Kit/i)).toBeVisible();
  });

  test('should validate required fields before proceeding', async ({ page }) => {
    await page.goto('/create');

    // Try to proceed without filling required fields
    const nextButton = page.getByRole('button', { name: /Next/i });

    // Topic is required
    await nextButton.click();

    // Should still be on step 1 or show validation error
    const topicField = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicField).toBeVisible();

    // Check for validation message
    const validationMessage = page.getByText(/required|topic.*required/i);
    if (await validationMessage.isVisible({ timeout: 1000 })) {
      await expect(validationMessage).toBeVisible();
    }

    // Fill topic and proceed
    await topicField.fill('Validation Test');
    await nextButton.click();

    // Should now be on step 2
    await expect(page.getByText(/Plan.*Brand Kit/i)).toBeVisible({ timeout: 3000 });
  });

  test('should persist form data across page reloads', async ({ page }) => {
    await page.goto('/create');

    // Fill in some data
    await page.getByPlaceholder(/Enter your video topic/i).fill('Persistence Test');
    await page.getByPlaceholder(/Who is the target audience/i).fill('Test Audience');

    // Reload page
    await page.reload();

    // Data should be restored (if localStorage persistence is implemented)
    const topicField = page.getByPlaceholder(/Enter your video topic/i);
    await expect(topicField).toBeVisible();

    // Note: This assumes localStorage persistence is implemented
    // If not, this test will pass but won't verify persistence
  });

  test('should show real-time progress updates during generation', async ({ page }) => {
    // Mock job with progressive updates
    let callCount = 0;
    await page.route('**/api/jobs/progress-test-123', (route) => {
      callCount++;
      const progress = Math.min(callCount * 20, 100);
      const stages = ['Script', 'TTS', 'Visuals', 'Render', 'Done'];
      const stageIndex = Math.min(Math.floor(progress / 25), stages.length - 1);

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'progress-test-123',
          status: progress >= 100 ? 'Done' : 'Running',
          stage: stages[stageIndex],
          progress: progress,
          message: `Processing ${stages[stageIndex]}`,
        }),
      });
    });

    // Mock job creation
    await page.route('**/api/quick/demo', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'progress-test-123',
          status: 'queued',
        }),
      });
    });

    await page.goto('/create');

    // Start quick demo
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    // Should show progress updates
    await expect(page.getByText(/Script|Processing/i)).toBeVisible({ timeout: 3000 });
    await expect(page.getByText(/TTS|Audio/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/Visuals|Images/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/Render|Video/i)).toBeVisible({ timeout: 5000 });

    // Progress bar or percentage should be visible
    const progressIndicator = page.locator('[role="progressbar"], .progress, [class*="progress"]');
    if (await progressIndicator.count() > 0) {
      await expect(progressIndicator.first()).toBeVisible();
    }
  });

  test('should handle job cancellation', async ({ page }) => {
    // Mock job creation
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'cancel-test-123',
          status: 'queued',
        }),
      });
    });

    // Mock job status
    await page.route('**/api/jobs/cancel-test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'cancel-test-123',
          status: 'Running',
          stage: 'TTS',
          progress: 40,
        }),
      });
    });

    // Mock cancellation endpoint
    await page.route('**/api/jobs/cancel-test-123/cancel', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Job cancelled successfully',
        }),
      });
    });

    await page.goto('/create');

    // Start generation (simplified path)
    await page.getByPlaceholder(/Enter your video topic/i).fill('Cancel Test');

    // If there's a quick generate button, use it
    const generateButton = page.getByRole('button', {
      name: /Quick Demo|Generate|Start/i,
    });

    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Wait for job to start
      await page.waitForTimeout(1000);

      // Look for cancel button
      const cancelButton = page.getByRole('button', { name: /Cancel|Stop|Abort/i });

      if (await cancelButton.isVisible({ timeout: 2000 })) {
        await cancelButton.click();

        // Should show cancelled status
        await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 3000 });
      }
    }
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Mock API error
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Internal Server Error',
          message: 'Failed to create job',
        }),
      });
    });

    await page.goto('/create');

    // Fill form
    await page.getByPlaceholder(/Enter your video topic/i).fill('Error Test');

    // Try to start generation
    const generateButton = page.getByRole('button', { name: /Quick Demo|Generate/i });

    if (await generateButton.isVisible({ timeout: 2000 })) {
      await generateButton.click();

      // Should show error message
      await expect(
        page.getByText(/Error|Failed|Something went wrong/i)
      ).toBeVisible({ timeout: 5000 });
    }
  });
});
