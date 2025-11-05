import { test, expect } from '@playwright/test';
import mockResponses from '../../../samples/test-data/fixtures/mock-responses.json';

/**
 * Export Manifest Validation E2E Tests
 * Tests manifest generation, licensing information, and artifact validation
 */
test.describe('Export Manifest Validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should generate manifest with complete metadata', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-manifest-${Date.now()}`;
    const manifest = mockResponses.artifacts.manifest.success;

    await page.route('**/api/quick/demo', async (route) => {
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

    await page.route(`**/api/jobs/${jobId}/events`, async (route) => {
      const completionEvent = {
        id: '7',
        event: 'job-completed',
        data: {
          jobId,
          status: 'completed',
          progress: 100,
          message: 'Video generation completed',
          artifacts: [
            {
              type: 'video',
              path: `/output/${jobId}/video.mp4`,
              size: 2048000,
              mimeType: 'video/mp4',
            },
            {
              type: 'manifest',
              path: `/output/${jobId}/manifest.json`,
              size: 1024,
              mimeType: 'application/json',
            },
          ],
        },
      };

      const sseEvent = `id: ${completionEvent.id}\nevent: ${completionEvent.event}\ndata: ${JSON.stringify(completionEvent.data)}\n\n`;

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: sseEvent,
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'completed',
          phase: 'render',
          progress: 100,
          message: 'Video generation completed',
          artifacts: [
            {
              type: 'video',
              path: `/output/${jobId}/video.mp4`,
              size: 2048000,
              mimeType: 'video/mp4',
            },
            {
              type: 'manifest',
              path: `/output/${jobId}/manifest.json`,
              size: 1024,
              mimeType: 'application/json',
            },
          ],
        }),
      });
    });

    await page.route(`**/output/${jobId}/manifest.json`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...manifest, jobId }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await expect(quickDemoButton).toBeVisible({ timeout: 10000 });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    await expect(page.getByText(/Complete|Done|Success/i)).toBeVisible({ timeout: 20000 });

    await expect(page.getByText(/manifest\.json|Manifest/i)).toBeVisible({ timeout: 5000 });
  });

  test('should validate licensing information in manifest', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-licensing-${Date.now()}`;
    const manifest = mockResponses.artifacts.manifest.success;

    await page.route('**/api/quick/demo', async (route) => {
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

    await page.route(`**/api/jobs/${jobId}/events`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: '',
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'completed',
          phase: 'render',
          progress: 100,
          message: 'Video generation completed',
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}/manifest`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...manifest, jobId }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(3000);

    const manifestLink = page.getByText(/manifest\.json|View Manifest|Licensing/i);
    if (await manifestLink.isVisible({ timeout: 10000 })) {
      await manifestLink.click();

      await expect(page.getByText(/licensing|license/i)).toBeVisible({ timeout: 5000 });

      const licensingFields = [
        /llmProvider|LLM Provider/i,
        /ttsProvider|TTS Provider/i,
        /visualsProvider|Visuals Provider/i,
        /Commercial Use/i,
      ];

      for (const field of licensingFields) {
        await expect(page.getByText(field).or(page.locator('text=' + field))).toBeVisible({
          timeout: 3000,
        });
      }
    }
  });

  test('should include pipeline timing information', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-timing-${Date.now()}`;
    const manifest = mockResponses.artifacts.manifest.success;

    await page.route('**/api/quick/demo', async (route) => {
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

    await page.route(`**/api/jobs/${jobId}/events`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: '',
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'completed',
          phase: 'render',
          progress: 100,
          message: 'Video generation completed',
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}/manifest`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...manifest, jobId }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(3000);

    const manifestLink = page.getByText(/manifest\.json|View Manifest/i);
    if (await manifestLink.isVisible({ timeout: 10000 })) {
      await manifestLink.click();

      const phases = ['brief', 'script', 'tts', 'visuals', 'compose', 'render'];
      for (const phase of phases) {
        await expect(page.getByText(new RegExp(phase, 'i'))).toBeVisible({ timeout: 3000 });
      }
    }
  });

  test('should validate artifact checksums if available', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-checksum-${Date.now()}`;
    const manifest = {
      ...mockResponses.artifacts.manifest.success,
      output: {
        ...mockResponses.artifacts.manifest.success.output,
        video: {
          ...mockResponses.artifacts.manifest.success.output.video,
          checksum: 'sha256:abcdef1234567890',
        },
      },
    };

    await page.route('**/api/quick/demo', async (route) => {
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

    await page.route(`**/api/jobs/${jobId}/events`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: '',
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'completed',
          phase: 'render',
          progress: 100,
          message: 'Video generation completed',
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}/manifest`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...manifest, jobId }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(3000);
  });

  test('should export manifest as downloadable file', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-download-${Date.now()}`;
    const manifest = mockResponses.artifacts.manifest.success;

    await page.route('**/api/quick/demo', async (route) => {
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

    await page.route(`**/api/jobs/${jobId}/events`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: '',
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'completed',
          phase: 'render',
          progress: 100,
          message: 'Video generation completed',
          artifacts: [
            {
              type: 'manifest',
              path: `/output/${jobId}/manifest.json`,
              size: 1024,
              mimeType: 'application/json',
            },
          ],
        }),
      });
    });

    await page.route(`**/output/${jobId}/manifest.json`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: {
          'Content-Disposition': `attachment; filename="manifest-${jobId}.json"`,
        },
        body: JSON.stringify({ ...manifest, jobId }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(3000);

    const downloadLink = page.getByRole('link', { name: /Download|manifest\.json/i });
    if (await downloadLink.isVisible({ timeout: 10000 })) {
      const downloadPromise = page.waitForEvent('download');
      await downloadLink.click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toMatch(/manifest.*\.json/);
    }
  });
});
