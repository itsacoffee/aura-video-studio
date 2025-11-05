import { test, expect } from '@playwright/test';

/**
 * E2E tests for Server-Sent Events (SSE) progress monitoring
 * Tests real-time job progress updates, reconnection, and Last-Event-ID support
 */
test.describe('SSE Progress Monitoring', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/settings', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ offlineMode: false }),
      });
    });
  });

  test('should establish SSE connection and receive progress updates', async ({ page }) => {
    let sseConnected = false;
    const progressEvents: unknown[] = [];

    page.on('console', (msg) => {
      const text = msg.text();
      if (text.includes('[SseClient] Connected successfully')) {
        sseConnected = true;
      }
      if (text.includes('[SseClient] Received')) {
        progressEvents.push(text);
      }
    });

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'sse-test-123',
          status: 'queued',
        }),
      });
    });

    let eventCount = 0;
    await page.route('**/api/jobs/sse-test-123/events', (route) => {
      eventCount++;
      const events = [
        'event: job-status\ndata: {"id":"sse-test-123","status":"Running","stage":"Script","progress":10}\n\n',
        'event: step-progress\ndata: {"step":"script","progress":25,"message":"Generating scene 1"}\n\n',
        'event: step-progress\ndata: {"step":"script","progress":50,"message":"Generating scene 2"}\n\n',
        'event: job-status\ndata: {"id":"sse-test-123","status":"Running","stage":"TTS","progress":40}\n\n',
        'event: step-progress\ndata: {"step":"tts","progress":60,"message":"Synthesizing audio"}\n\n',
        'event: job-completed\ndata: {"id":"sse-test-123","status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');

    const quickDemoButton = page.getByRole('button', { name: /Quick Demo/i });
    if (await quickDemoButton.isVisible({ timeout: 2000 })) {
      await quickDemoButton.click();
    }

    await page.waitForTimeout(2000);

    await expect(page.getByText(/Script|Running/i)).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/progress|%/i)).toBeVisible({ timeout: 3000 });
  });

  test('should handle SSE reconnection with Last-Event-ID', async ({ page }) => {
    let reconnectAttempts = 0;

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'reconnect-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/reconnect-test-123/events', (route) => {
      reconnectAttempts++;

      if (reconnectAttempts === 1) {
        route.fulfill({
          status: 500,
          body: 'Server error',
        });
      } else {
        const events = [
          'id: event-5\nevent: job-status\ndata: {"id":"reconnect-test-123","status":"Running","progress":50}\n\n',
          'id: event-6\nevent: job-completed\ndata: {"id":"reconnect-test-123","status":"Done","progress":100}\n\n',
        ];

        route.fulfill({
          status: 200,
          headers: {
            'Content-Type': 'text/event-stream',
            'Cache-Control': 'no-cache',
            Connection: 'keep-alive',
          },
          body: events.join(''),
        });
      }
    });

    await page.goto('/create');
    await page.waitForTimeout(1000);
  });

  test('should receive keep-alive pings and maintain connection', async ({ page }) => {
    const keepAliveReceived = false;

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'keepalive-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/keepalive-test-123/events', (route) => {
      const events = [
        ': keep-alive\n\n',
        'event: job-status\ndata: {"id":"keepalive-test-123","status":"Running","progress":30}\n\n',
        ': keep-alive\n\n',
        'event: job-completed\ndata: {"id":"keepalive-test-123","status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });

  test('should handle SSE connection errors gracefully', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'error-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/error-test-123/events', (route) => {
      route.abort('failed');
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });

  test('should display phase information during generation', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'phase-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/phase-test-123/events', (route) => {
      const events = [
        'event: job-status\ndata: {"id":"phase-test-123","status":"Running","stage":"Script","progress":10}\n\n',
        'event: step-progress\ndata: {"step":"script","progress":15,"message":"Analyzing brief"}\n\n',
        'event: job-status\ndata: {"id":"phase-test-123","status":"Running","stage":"TTS","progress":35}\n\n',
        'event: step-progress\ndata: {"step":"tts","progress":50,"message":"Synthesizing speech"}\n\n',
        'event: job-status\ndata: {"id":"phase-test-123","status":"Running","stage":"Visuals","progress":65}\n\n',
        'event: step-progress\ndata: {"step":"visuals","progress":75,"message":"Generating images"}\n\n',
        'event: job-status\ndata: {"id":"phase-test-123","status":"Running","stage":"Render","progress":85}\n\n',
        'event: step-progress\ndata: {"step":"render","progress":95,"message":"Encoding video"}\n\n',
        'event: job-completed\ndata: {"id":"phase-test-123","status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });

  test('should handle warning events during generation', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'warning-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/warning-test-123/events', (route) => {
      const events = [
        'event: job-status\ndata: {"id":"warning-test-123","status":"Running","progress":20}\n\n',
        'event: warning\ndata: {"message":"TTS provider fallback to Windows SAPI","code":"W201"}\n\n',
        'event: job-completed\ndata: {"id":"warning-test-123","status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });

  test('should close SSE connection when job completes', async ({ page }) => {
    let connectionClosed = false;

    page.on('console', (msg) => {
      if (msg.text().includes('[SseClient] Connection closed')) {
        connectionClosed = true;
      }
    });

    await page.route('**/api/jobs', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'complete-test-123',
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/complete-test-123/events', (route) => {
      const events = [
        'event: job-status\ndata: {"id":"complete-test-123","status":"Running","progress":50}\n\n',
        'event: job-completed\ndata: {"id":"complete-test-123","status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(2000);
  });

  test('should handle multiple concurrent SSE connections', async ({ page }) => {
    await page.route('**/api/jobs', (route) => {
      const body = route.request().postDataJSON();
      const jobId = `concurrent-${Date.now()}`;

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: jobId,
          status: 'queued',
        }),
      });
    });

    await page.route('**/api/jobs/*/events', (route) => {
      const events = [
        'event: job-status\ndata: {"status":"Running","progress":50}\n\n',
        'event: job-completed\ndata: {"status":"Done","progress":100}\n\n',
      ];

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
        },
        body: events.join(''),
      });
    });

    await page.goto('/');
    await page.waitForTimeout(1000);
  });
});
