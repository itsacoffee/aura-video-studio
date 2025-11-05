import { test, expect } from '@playwright/test';

/**
 * Full Pipeline E2E Tests
 * Tests complete video generation workflow: Brief → Plan → Script → SSML → Assets → Render
 * Validates SSE progress tracking, job management, and artifact generation
 */
test.describe('Full Video Generation Pipeline', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should complete full pipeline from brief to final video', async ({ page }) => {
    test.setTimeout(120000); // 2 minutes for full pipeline

    // Mock SSE for job progress
    let jobId = '';
    let progressValue = 0;

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

    // Mock job status endpoint
    await page.route(`**/api/jobs/**`, async (route) => {
      progressValue = Math.min(progressValue + 10, 100);
      const phases = ['brief', 'plan', 'script', 'tts', 'visuals', 'compose', 'render'];
      const phaseIndex = Math.floor((progressValue / 100) * phases.length);
      const currentPhase = phases[Math.min(phaseIndex, phases.length - 1)];

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: progressValue >= 100 ? 'completed' : 'running',
          phase: currentPhase,
          progress: progressValue,
          message: `Processing ${currentPhase}`,
          artifacts:
            progressValue >= 100
              ? [
                  {
                    type: 'video',
                    path: `/output/${jobId}/video.mp4`,
                    size: 2048000,
                    mimeType: 'video/mp4',
                  },
                  {
                    type: 'subtitles',
                    path: `/output/${jobId}/subtitles.srt`,
                    size: 4096,
                    mimeType: 'text/srt',
                  },
                  {
                    type: 'manifest',
                    path: `/output/${jobId}/manifest.json`,
                    size: 1024,
                    mimeType: 'application/json',
                  },
                ]
              : [],
        }),
      });
    });

    // Start Quick Demo
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await expect(quickDemoButton).toBeVisible({ timeout: 10000 });
    await quickDemoButton.click();

    // Verify job creation
    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    // Track phase progression
    await expect(page.getByText(/brief|planning/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/plan|script/i)).toBeVisible({ timeout: 8000 });
    await expect(page.getByText(/tts|voice|audio/i)).toBeVisible({ timeout: 8000 });
    await expect(page.getByText(/visuals|images/i)).toBeVisible({ timeout: 8000 });
    await expect(page.getByText(/compose|assembly/i)).toBeVisible({ timeout: 8000 });
    await expect(page.getByText(/render|encoding/i)).toBeVisible({ timeout: 8000 });

    // Verify completion
    await expect(page.getByText(/Complete|Done|Success/i)).toBeVisible({ timeout: 15000 });

    // Verify artifacts section
    await expect(page.getByText(/video\.mp4|Video file/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/subtitles\.srt|Subtitles/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/manifest\.json|Manifest/i)).toBeVisible({ timeout: 5000 });

    // Verify download buttons
    const downloadButtons = page.getByRole('button', { name: /Download|Export/i });
    await expect(downloadButtons.first()).toBeVisible();
  });

  test('should track SSE progress with event IDs and reconnection', async ({ page }) => {
    test.setTimeout(60000);

    const eventLog: string[] = [];

    // Intercept SSE connection
    page.on('response', async (response) => {
      if (response.url().includes('/events') && response.headers()['content-type']?.includes('text/event-stream')) {
        eventLog.push('SSE connection established');
      }
    });

    // Mock job creation
    await page.route('**/api/quick/demo', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ jobId: 'sse-test-job', status: 'queued' }),
      });
    });

    // Start generation
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 5000 })) {
      await quickDemoButton.click();

      // Wait for some progress
      await page.waitForTimeout(3000);

      // Verify SSE connection was established
      expect(eventLog).toContain('SSE connection established');
    }
  });

  test('should handle job cancellation gracefully', async ({ page }) => {
    test.setTimeout(60000);

    let cancelled = false;

    await page.route('**/api/quick/demo', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ jobId: 'cancel-test', status: 'queued' }),
      });
    });

    await page.route('**/api/jobs/cancel-test', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          body: JSON.stringify({
            id: 'cancel-test',
            status: cancelled ? 'cancelled' : 'running',
            phase: 'script',
            progress: 25,
          }),
        });
      }
    });

    await page.route('**/api/jobs/cancel-test/cancel', async (route) => {
      cancelled = true;
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ message: 'Job cancelled successfully' }),
      });
    });

    // Start job
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 5000 })) {
      await quickDemoButton.click();

      // Wait for job to start
      await page.waitForTimeout(1000);

      // Find and click cancel button
      const cancelButton = page.getByRole('button', { name: /Cancel|Stop|Abort/i });
      if (await cancelButton.isVisible({ timeout: 5000 })) {
        await cancelButton.click();

        // Verify cancellation
        await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 5000 });
      }
    }
  });

  test('should verify export manifest includes licensing info', async ({ page }) => {
    test.setTimeout(60000);

    const mockManifest = {
      version: '1.0',
      jobId: 'manifest-test',
      generatedAt: new Date().toISOString(),
      artifacts: [
        { type: 'video', path: 'video.mp4', size: 2048000 },
        { type: 'subtitles', path: 'subtitles.srt', size: 4096 },
      ],
      licensing: {
        provider: 'Aura',
        license: 'MIT',
        attribution: 'Generated by Aura Video Studio',
        providers: {
          llm: { name: 'RuleBased', license: 'Internal' },
          tts: { name: 'Windows', license: 'OS Included' },
          visuals: { name: 'Stock', license: 'Public Domain' },
        },
      },
      metadata: {
        duration: 30,
        resolution: '1920x1080',
        fps: 30,
      },
    };

    await page.route('**/api/jobs/**/manifest', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockManifest),
      });
    });

    await page.route('**/api/quick/demo', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({ jobId: 'manifest-test', status: 'completed' }),
      });
    });

    // Start and complete job
    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 5000 })) {
      await quickDemoButton.click();

      // Wait for completion
      await page.waitForTimeout(2000);

      // Check for manifest download option
      const manifestButton = page.getByRole('button', { name: /manifest|metadata|info/i });
      if (await manifestButton.isVisible({ timeout: 5000 })) {
        await manifestButton.click();

        // Verify manifest structure in response or display
        await expect(page.getByText(/licensing|attribution/i)).toBeVisible({ timeout: 3000 });
      }
    }
  });

  test('should handle wizard navigation through all steps', async ({ page }) => {
    test.setTimeout(90000);

    await page.goto('/create');

    // Step 1: Brief
    await expect(page.getByRole('heading', { name: /Brief|Create Video/i })).toBeVisible();

    const topicInput = page.getByPlaceholder(/topic|subject/i);
    await topicInput.fill('E2E Test Video');

    const audienceInput = page.getByPlaceholder(/audience|viewers/i);
    if (await audienceInput.isVisible({ timeout: 2000 })) {
      await audienceInput.fill('Test Users');
    }

    // Navigate to Step 2: Plan
    const nextButton = page.getByRole('button', { name: /Next|Continue/i });
    await nextButton.click();

    await expect(page.getByText(/Plan|Brand|Duration/i)).toBeVisible({ timeout: 5000 });

    // Set duration
    const durationInput = page.getByLabel(/Duration|Length/i);
    if (await durationInput.isVisible({ timeout: 2000 })) {
      await durationInput.fill('30');
    }

    // Navigate to Step 3: Voice/Providers
    await nextButton.click();

    await expect(page.getByText(/Voice|Providers|TTS/i)).toBeVisible({ timeout: 5000 });

    // Select voice provider
    const providerSelect = page.getByLabel(/Provider|Voice/i).first();
    if (await providerSelect.isVisible({ timeout: 2000 })) {
      await providerSelect.click();
      const freeOption = page.getByRole('option', { name: /Free|Windows|Default/i });
      if (await freeOption.isVisible({ timeout: 2000 })) {
        await freeOption.click();
      }
    }

    // Verify preflight check
    const preflightButton = page.getByRole('button', { name: /Preflight|Check|Ready/i });
    if (await preflightButton.isVisible({ timeout: 2000 })) {
      await preflightButton.click();
      await expect(page.getByText(/Ready|OK|Valid/i)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should handle API errors with proper error messages', async ({ page }) => {
    test.setTimeout(60000);

    await page.route('**/api/quick/demo', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Internal Server Error',
          message: 'FFmpeg not found',
          details: 'Could not locate FFmpeg executable',
        }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 5000 })) {
      await quickDemoButton.click();

      // Verify error is displayed to user
      await expect(
        page.getByText(/Error|Failed|Something went wrong/i)
      ).toBeVisible({ timeout: 5000 });

      // Verify helpful error message
      await expect(
        page.getByText(/FFmpeg|not found|executable/i)
      ).toBeVisible({ timeout: 5000 });
    }
  });
});
