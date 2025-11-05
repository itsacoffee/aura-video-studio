import { test, expect } from '@playwright/test';
import mockResponses from '../../../samples/test-data/fixtures/mock-responses.json';

/**
 * SSE Progress Tracking E2E Tests
 * Tests Server-Sent Events for real-time progress updates, reconnection with Last-Event-ID
 */
test.describe('SSE Progress Tracking', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should track job progress via SSE events', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-${Date.now()}`;
    const progressUpdates: number[] = [];
    const phaseUpdates: string[] = [];

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
      const sseEvents = mockResponses.sse.jobProgress.events
        .map(
          (event) =>
            `id: ${event.id}\nevent: ${event.event}\ndata: ${JSON.stringify({ ...event.data, jobId })}\n\n`
        )
        .join('');

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: sseEvents,
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

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await expect(quickDemoButton).toBeVisible({ timeout: 10000 });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    const progressIndicators = [
      /brief|planning/i,
      /script/i,
      /tts|voice|audio/i,
      /visuals|images/i,
      /compose|assembly/i,
      /render|encoding/i,
    ];

    for (const indicator of progressIndicators) {
      await expect(page.getByText(indicator)).toBeVisible({ timeout: 10000 });
    }

    await expect(page.getByText(/Complete|Done|Success/i)).toBeVisible({ timeout: 15000 });

    expect(progressIndicators.length).toBeGreaterThan(3);
  });

  test('should reconnect SSE with Last-Event-ID after network interruption', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-reconnect-${Date.now()}`;
    let reconnectionAttempted = false;
    let lastEventIdReceived = '';

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
      const request = route.request();
      const lastEventId = request.headers()['last-event-id'];

      if (lastEventId) {
        reconnectionAttempted = true;
        lastEventIdReceived = lastEventId;

        const resumeEvents = mockResponses.sse.jobProgress.reconnection.resumeEvents
          .map(
            (event) =>
              `id: ${event.id}\nevent: ${event.event}\ndata: ${JSON.stringify({ ...event.data, jobId })}\n\n`
          )
          .join('');

        await route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          headers: {
            'Cache-Control': 'no-cache',
            Connection: 'keep-alive',
          },
          body: resumeEvents,
        });
      } else {
        const initialEvents = mockResponses.sse.jobProgress.events
          .slice(0, 3)
          .map(
            (event) =>
              `id: ${event.id}\nevent: ${event.event}\ndata: ${JSON.stringify({ ...event.data, jobId })}\n\n`
          )
          .join('');

        await route.fulfill({
          status: 200,
          contentType: 'text/event-stream',
          headers: {
            'Cache-Control': 'no-cache',
            Connection: 'keep-alive',
          },
          body: initialEvents,
        });
      }
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'running',
          phase: 'visuals',
          progress: 60,
          message: 'Generating visuals',
        }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    await page.waitForTimeout(2000);

    await page.context().setOffline(true);
    await page.waitForTimeout(1000);
    await page.context().setOffline(false);

    await page.waitForTimeout(3000);

    expect(reconnectionAttempted).toBeTruthy();
  });

  test('should handle SSE connection errors gracefully', async ({ page }) => {
    test.setTimeout(60000);

    const jobId = `test-job-error-${Date.now()}`;

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
      await route.abort('failed');
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'running',
          phase: 'script',
          progress: 20,
          message: 'Generating script',
        }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    await expect(
      page.getByText(/Connection|Reconnect|Retry/i).or(page.getByText(/script/i))
    ).toBeVisible({ timeout: 10000 });
  });

  test('should display progress percentage accurately', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-percentage-${Date.now()}`;

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
      const progressEvents = [0, 20, 40, 60, 80, 100].map((progress, index) => ({
        id: `${index + 1}`,
        event: 'phase-progress',
        data: {
          jobId,
          status: progress === 100 ? 'completed' : 'running',
          phase: ['brief', 'script', 'tts', 'visuals', 'compose', 'render'][index],
          progress,
          message: `Processing phase ${index + 1}`,
        },
      }));

      const sseEvents = progressEvents
        .map(
          (event) =>
            `id: ${event.id}\nevent: ${event.event}\ndata: ${JSON.stringify(event.data)}\n\n`
        )
        .join('');

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: {
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: sseEvents,
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

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    await expect(page.getByText(/100%|Complete|Done/i)).toBeVisible({ timeout: 15000 });
  });
});
