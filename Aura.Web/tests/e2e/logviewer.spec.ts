import { test, expect } from '@playwright/test';

test.describe('Log Viewer E2E', () => {
  test('should open log viewer page', async ({ page }) => {
    // Mock the logs API
    await page.route('**/api/logs*', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'INF',
              correlationId: 'test-correlation-123',
              message: 'Application started successfully',
              rawLine: '[2025-10-10 22:00:00.000 +00:00] [INF] [test-correlation-123] Application started successfully'
            },
            {
              timestamp: '2025-10-10 22:00:01.000 +00:00',
              level: 'WRN',
              correlationId: 'test-correlation-456',
              message: 'Warning: Configuration not found',
              rawLine: '[2025-10-10 22:00:01.000 +00:00] [WRN] [test-correlation-456] Warning: Configuration not found'
            },
            {
              timestamp: '2025-10-10 22:00:02.000 +00:00',
              level: 'ERR',
              correlationId: 'test-correlation-789',
              message: 'Error: Failed to connect to service',
              rawLine: '[2025-10-10 22:00:02.000 +00:00] [ERR] [test-correlation-789] Error: Failed to connect to service'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 1500
        })
      });
    });

    // Navigate to log viewer
    await page.goto('/logs');

    // Check page title
    await expect(page.getByRole('heading', { name: 'Log Viewer' })).toBeVisible();

    // Wait for logs to load
    await expect(page.getByText('Application started successfully')).toBeVisible();
    await expect(page.getByText('Warning: Configuration not found')).toBeVisible();
    await expect(page.getByText('Error: Failed to connect to service')).toBeVisible();

    // Verify stats are displayed
    await expect(page.getByText('aura-api-20251010.log')).toBeVisible();
    await expect(page.getByText('1500')).toBeVisible(); // Total lines
    await expect(page.getByText('3')).toBeVisible(); // Filtered lines
  });

  test('should filter logs by error level', async ({ page }) => {
    // Mock the logs API for initial load
    await page.route('**/api/logs?lines=500', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'INF',
              correlationId: 'test-123',
              message: 'Info message',
              rawLine: '[2025-10-10 22:00:00.000 +00:00] [INF] [test-123] Info message'
            },
            {
              timestamp: '2025-10-10 22:00:01.000 +00:00',
              level: 'ERR',
              correlationId: 'test-456',
              message: 'Error message',
              rawLine: '[2025-10-10 22:00:01.000 +00:00] [ERR] [test-456] Error message'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 1500
        })
      });
    });

    // Mock the logs API for error filter
    await page.route('**/api/logs?level=ERR&lines=500', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:01.000 +00:00',
              level: 'ERR',
              correlationId: 'test-456',
              message: 'Error message',
              rawLine: '[2025-10-10 22:00:01.000 +00:00] [ERR] [test-456] Error message'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 1500
        })
      });
    });

    await page.goto('/logs');

    // Wait for initial logs to load
    await expect(page.getByText('Info message')).toBeVisible();
    await expect(page.getByText('Error message')).toBeVisible();

    // Select error level filter
    await page.selectOption('select', 'ERR');
    
    // Click Apply Filters button
    await page.getByRole('button', { name: 'Apply Filters' }).click();

    // Wait for filtered results
    await page.waitForTimeout(500);

    // Verify only error message is shown
    await expect(page.getByText('Error message')).toBeVisible();
  });

  test('should copy log details to clipboard', async ({ page, context }) => {
    // Grant clipboard permissions
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);

    // Mock the logs API
    await page.route('**/api/logs*', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'ERR',
              correlationId: 'test-correlation-123',
              message: 'Test error message',
              rawLine: '[2025-10-10 22:00:00.000 +00:00] [ERR] [test-correlation-123] Test error message'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 100
        })
      });
    });

    await page.goto('/logs');

    // Wait for log to be visible
    await expect(page.getByText('Test error message')).toBeVisible();

    // Click on the log entry to copy it
    await page.getByText('Test error message').click();

    // Wait for copy operation
    await page.waitForTimeout(500);

    // Verify "Copied!" message appears
    await expect(page.getByText('Copied!')).toBeVisible();

    // Verify clipboard content
    const clipboardText = await page.evaluate(() => navigator.clipboard.readText());
    expect(clipboardText).toContain('test-correlation-123');
    expect(clipboardText).toContain('Test error message');
    expect(clipboardText).toContain('ERR');
  });

  test('should filter logs by correlation ID', async ({ page }) => {
    // Mock the logs API for initial load
    await page.route('**/api/logs?lines=500', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'INF',
              correlationId: 'abc-123',
              message: 'Message 1',
              rawLine: '[2025-10-10 22:00:00.000 +00:00] [INF] [abc-123] Message 1'
            },
            {
              timestamp: '2025-10-10 22:00:01.000 +00:00',
              level: 'INF',
              correlationId: 'def-456',
              message: 'Message 2',
              rawLine: '[2025-10-10 22:00:01.000 +00:00] [INF] [def-456] Message 2'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 1500
        })
      });
    });

    // Mock the logs API for correlation ID filter
    await page.route('**/api/logs?correlationId=abc-123&lines=500', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'INF',
              correlationId: 'abc-123',
              message: 'Message 1',
              rawLine: '[2025-10-10 22:00:00.000 +00:00] [INF] [abc-123] Message 1'
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: 1500
        })
      });
    });

    await page.goto('/logs');

    // Wait for initial logs to load
    await expect(page.getByText('Message 1')).toBeVisible();
    await expect(page.getByText('Message 2')).toBeVisible();

    // Enter correlation ID filter
    await page.getByPlaceholder('Filter by Correlation ID').fill('abc-123');
    
    // Click Apply Filters button
    await page.getByRole('button', { name: 'Apply Filters' }).click();

    // Wait for filtered results
    await page.waitForTimeout(500);

    // Verify only Message 1 is shown
    await expect(page.getByText('Message 1')).toBeVisible();
  });

  test('should refresh logs', async ({ page }) => {
    let requestCount = 0;

    // Mock the logs API with incrementing data
    await page.route('**/api/logs*', (route) => {
      requestCount++;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          logs: [
            {
              timestamp: '2025-10-10 22:00:00.000 +00:00',
              level: 'INF',
              correlationId: 'test-123',
              message: `Log entry ${requestCount}`,
              rawLine: `[2025-10-10 22:00:00.000 +00:00] [INF] [test-123] Log entry ${requestCount}`
            }
          ],
          file: 'aura-api-20251010.log',
          totalLines: requestCount * 100
        })
      });
    });

    await page.goto('/logs');

    // Wait for initial load
    await expect(page.getByText('Log entry 1')).toBeVisible();

    // Click refresh button
    await page.getByRole('button', { name: 'Refresh' }).click();

    // Wait for refresh
    await page.waitForTimeout(500);

    // Verify new data is loaded
    await expect(page.getByText('Log entry 2')).toBeVisible();
  });
});
