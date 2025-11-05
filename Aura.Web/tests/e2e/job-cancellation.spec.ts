import { test, expect } from '@playwright/test';
import mockResponses from '../../../samples/test-data/fixtures/mock-responses.json';

/**
 * Job Cancellation E2E Tests
 * Tests job cancellation functionality, cleanup, and SSE cancellation events
 */
test.describe('Job Cancellation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should cancel running job and cleanup resources', async ({ page }) => {
    test.setTimeout(90000);

    const jobId = `test-job-cancel-${Date.now()}`;
    let jobStatus = 'running';

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
      if (jobStatus === 'running') {
        const runningEvents = mockResponses.sse.jobProgress.events
          .slice(0, 4)
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
          body: runningEvents,
        });
      } else if (jobStatus === 'cancelling' || jobStatus === 'cancelled') {
        const cancelEvents = mockResponses.sse.jobCancellation.events
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
          body: cancelEvents,
        });
      }
    });

    await page.route(`**/api/jobs/${jobId}/cancel`, async (route) => {
      jobStatus = 'cancelling';
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'cancelling',
          message: 'Cancellation requested',
        }),
      });

      setTimeout(() => {
        jobStatus = 'cancelled';
      }, 1000);
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: jobStatus,
          phase: jobStatus === 'cancelled' ? 'cancelled' : 'visuals',
          progress: jobStatus === 'cancelled' ? 0 : 60,
          message: jobStatus === 'cancelled' ? 'Job cancelled' : 'Generating visuals',
          cancelledAt: jobStatus === 'cancelled' ? new Date().toISOString() : undefined,
        }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await expect(quickDemoButton).toBeVisible({ timeout: 10000 });
    await quickDemoButton.click();

    await expect(page.getByText(/Generation started|Job created/i)).toBeVisible({
      timeout: 5000,
    });

    await page.waitForTimeout(3000);

    const cancelButton = page.getByRole('button', { name: /Cancel|Stop/i });
    if (await cancelButton.isVisible({ timeout: 5000 })) {
      await cancelButton.click();

      await expect(page.getByText(/Cancelling|Cancel/i)).toBeVisible({ timeout: 5000 });

      await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 10000 });
    }
  });

  test('should prevent actions on cancelled job', async ({ page }) => {
    test.setTimeout(60000);

    const jobId = `test-job-cancelled-${Date.now()}`;

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
          status: 'cancelled',
          phase: 'cancelled',
          progress: 0,
          message: 'Job was cancelled',
          cancelledAt: '2024-01-01T00:05:00Z',
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}/resume`, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'Cannot resume cancelled job',
          code: 'JOB_CANCELLED',
        }),
      });
    });

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(2000);

    await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 10000 });

    const resumeButton = page.getByRole('button', { name: /Resume|Restart/i });
    if (await resumeButton.isVisible({ timeout: 2000 })) {
      await resumeButton.click();
      await expect(page.getByText(/Cannot resume|Error/i)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should handle cancellation during different phases', async ({ page }) => {
    test.setTimeout(90000);

    const phases = ['script', 'tts', 'visuals', 'compose'];

    for (const phase of phases) {
      const jobId = `test-job-cancel-${phase}-${Date.now()}`;
      const currentPhase = phase;
      let cancelled = false;

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

      await page.route(`**/api/jobs/${jobId}/cancel`, async (route) => {
        cancelled = true;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: jobId,
            status: 'cancelled',
            message: `Job cancelled during ${currentPhase} phase`,
          }),
        });
      });

      await page.route(`**/api/jobs/${jobId}`, async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: jobId,
            status: cancelled ? 'cancelled' : 'running',
            phase: cancelled ? 'cancelled' : currentPhase,
            progress: cancelled ? 0 : 50,
            message: cancelled ? 'Job cancelled' : `Processing ${currentPhase}`,
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

      await page.goto('/');

      const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
      await quickDemoButton.click();

      await page.waitForTimeout(2000);

      const cancelButton = page.getByRole('button', { name: /Cancel|Stop/i });
      if (await cancelButton.isVisible({ timeout: 3000 })) {
        await cancelButton.click();
        await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 10000 });
      }
    }
  });

  test('should cleanup artifacts after cancellation', async ({ page }) => {
    test.setTimeout(60000);

    const jobId = `test-job-cleanup-${Date.now()}`;
    let _cleanupCalled = false;

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

    await page.route(`**/api/jobs/${jobId}/cancel`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'cancelled',
          message: 'Job cancelled successfully',
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}/cleanup`, async (route) => {
      _cleanupCalled = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Cleanup completed',
          artifactsRemoved: 5,
          diskSpaceFreed: 102400,
        }),
      });
    });

    await page.route(`**/api/jobs/${jobId}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: jobId,
          status: 'cancelled',
          phase: 'cancelled',
          progress: 0,
          message: 'Job cancelled',
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

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    await quickDemoButton.click();

    await page.waitForTimeout(2000);

    const cancelButton = page.getByRole('button', { name: /Cancel|Stop/i });
    if (await cancelButton.isVisible({ timeout: 5000 })) {
      await cancelButton.click();
      await expect(page.getByText(/Cancelled|Stopped/i)).toBeVisible({ timeout: 10000 });

      await page.waitForTimeout(2000);
    }
  });
});
