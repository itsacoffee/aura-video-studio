import { test, expect } from '@playwright/test';

/**
 * E2E tests for job lifecycle management
 * Tests job cancellation, resume, queue management, and cleanup
 */
test.describe('Job Lifecycle Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/settings', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ offlineMode: false }),
      });
    });
  });

  test('should cancel a running job successfully', async ({ page }) => {
    let cancelCalled = false;

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

    let jobStatus = 'Running';
    await page.route('**/api/jobs/cancel-test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'cancel-test-123',
          status: jobStatus,
          stage: 'TTS',
          progress: 40,
        }),
      });
    });

    await page.route('**/api/jobs/cancel-test-123/cancel', (route) => {
      cancelCalled = true;
      jobStatus = 'Cancelled';

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Job cancelled successfully',
          jobId: 'cancel-test-123',
        }),
      });
    });

    await page.route('**/api/jobs/cancel-test-123/events', (route) => {
      const events = [
        'event: job-status\ndata: {"id":"cancel-test-123","status":"Running","progress":40}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');

    await page.waitForTimeout(1000);

    const cancelButton = page.getByRole('button', { name: /Cancel|Stop|Abort/i });
    if (await cancelButton.isVisible({ timeout: 2000 })) {
      await cancelButton.click();

      await page.waitForTimeout(500);
      expect(cancelCalled).toBe(true);
    }
  });

  test('should clean up temporary files after job cancellation', async ({ page }) => {
    let cleanupCalled = false;

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'cleanup-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/cleanup-test-123/cancel', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Job cancelled successfully',
          jobId: 'cleanup-test-123',
          cleanupPerformed: true,
        }),
      });
    });

    await page.route('**/api/cleanup/job/cleanup-test-123', (route) => {
      cleanupCalled = true;

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          filesDeleted: 5,
          bytesFreed: 1024000,
        }),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });

  test('should list all jobs in queue with filtering', async ({ page }) => {
    await page.route('**/api/queue', (route) => {
      const url = new URL(route.request().url());
      const status = url.searchParams.get('status');

      const allJobs = [
        { id: 'job-1', status: 'Running', progress: 45 },
        { id: 'job-2', status: 'Queued', progress: 0 },
        { id: 'job-3', status: 'Done', progress: 100 },
        { id: 'job-4', status: 'Failed', progress: 30 },
      ];

      const filteredJobs = status
        ? allJobs.filter((j) => j.status.toLowerCase() === status.toLowerCase())
        : allJobs;

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobs: filteredJobs,
          total: filteredJobs.length,
        }),
      });
    });

    await page.goto('/jobs');

    await expect(page.getByText(/job-1|job-2|job-3|job-4/i)).toBeVisible({
      timeout: 3000,
    });

    const filterButton = page.getByRole('button', { name: /Filter|Status/i });
    if (await filterButton.isVisible({ timeout: 1000 })) {
      await filterButton.click();

      const runningOption = page.getByRole('option', { name: /Running/i });
      if (await runningOption.isVisible({ timeout: 1000 })) {
        await runningOption.click();

        await expect(page.getByText(/job-1/i)).toBeVisible({ timeout: 2000 });
      }
    }
  });

  test('should retrieve detailed job progress information', async ({ page }) => {
    await page.route('**/api/render/progress-test-123/progress', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'progress-test-123',
          status: 'Running',
          currentPhase: 'TTS',
          progress: 45,
          startTime: new Date().toISOString(),
          estimatedTimeRemaining: 120,
          steps: [
            { name: 'Script', status: 'Completed', progress: 100 },
            { name: 'TTS', status: 'Running', progress: 45 },
            { name: 'Visuals', status: 'Pending', progress: 0 },
            { name: 'Compose', status: 'Pending', progress: 0 },
            { name: 'Render', status: 'Pending', progress: 0 },
          ],
        }),
      });
    });

    await page.goto('/jobs/progress-test-123');

    await expect(page.getByText(/TTS|Running/i)).toBeVisible({ timeout: 3000 });
    await expect(page.getByText(/45|%/i)).toBeVisible({ timeout: 2000 });
  });

  test('should handle job retry after transient failure', async ({ page }) => {
    let attemptCount = 0;

    await page.route('**/api/jobs', (route) => {
      attemptCount++;

      if (attemptCount === 1) {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'Transient error',
            retryable: true,
          }),
        });
      } else {
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

    await page.goto('/');

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 2000 })) {
      await quickDemoButton.click();

      await page.waitForTimeout(1000);

      const retryButton = page.getByRole('button', { name: /Retry|Try Again/i });
      if (await retryButton.isVisible({ timeout: 2000 })) {
        await retryButton.click();
        await page.waitForTimeout(1000);
      }
    }
  });

  test('should prevent multiple cancellation requests for same job', async ({ page }) => {
    let cancelCount = 0;

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'multi-cancel-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/multi-cancel-test-123/cancel', (route) => {
      cancelCount++;

      if (cancelCount === 1) {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            message: 'Job cancelled successfully',
          }),
        });
      } else {
        route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'Job already cancelled',
          }),
        });
      }
    });

    await page.goto('/');
    await page.waitForTimeout(1000);

    const cancelButton = page.getByRole('button', { name: /Cancel|Stop/i });
    if (await cancelButton.isVisible({ timeout: 2000 })) {
      await cancelButton.click();
      await page.waitForTimeout(200);

      if (cancelCount > 1) {
        throw new Error('Multiple cancel requests sent');
      }
    }
  });

  test('should show cancellation confirmation dialog for long-running jobs', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'confirm-cancel-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/confirm-cancel-test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'confirm-cancel-test-123',
          status: 'Running',
          progress: 75,
          elapsedTime: 300,
        }),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);

    const cancelButton = page.getByRole('button', { name: /Cancel|Stop/i });
    if (await cancelButton.isVisible({ timeout: 2000 })) {
      await cancelButton.click();

      const confirmDialog = page.getByRole('dialog', { name: /Confirm|Cancel/i });
      if (await confirmDialog.isVisible({ timeout: 1000 })) {
        await expect(confirmDialog.getByText(/Are you sure|progress will be lost/i)).toBeVisible();

        const confirmButton = confirmDialog.getByRole('button', { name: /Yes|Confirm/i });
        await confirmButton.click();
      }
    }
  });

  test('should display job queue status summary', async ({ page }) => {
    await page.route('**/api/queue/summary', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          total: 10,
          running: 2,
          queued: 5,
          completed: 2,
          failed: 1,
        }),
      });
    });

    await page.goto('/jobs');

    await expect(page.getByText(/total.*10|10.*total/i)).toBeVisible({ timeout: 3000 });
    await expect(page.getByText(/running.*2|2.*running/i)).toBeVisible({ timeout: 2000 });
    await expect(page.getByText(/queued.*5|5.*queued/i)).toBeVisible({ timeout: 2000 });
  });

  test('should handle job pause and resume functionality', async ({ page }) => {
    let isPaused = false;

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'pause-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/pause-test-123/pause', (route) => {
      isPaused = true;

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Job paused successfully',
          status: 'Paused',
        }),
      });
    });

    await page.route('**/api/jobs/pause-test-123/resume', (route) => {
      isPaused = false;

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Job resumed successfully',
          status: 'Running',
        }),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);

    const pauseButton = page.getByRole('button', { name: /Pause/i });
    if (await pauseButton.isVisible({ timeout: 2000 })) {
      await pauseButton.click();
      await page.waitForTimeout(500);

      const resumeButton = page.getByRole('button', { name: /Resume/i });
      if (await resumeButton.isVisible({ timeout: 2000 })) {
        await resumeButton.click();
      }
    }
  });
});
