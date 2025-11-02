import { test, expect, chromium } from '@playwright/test';

/**
 * Memory and performance regression tests
 * Validates that the application doesn't leak memory or degrade performance
 */
test.describe('Memory and Performance Regression', () => {
  test('should not leak memory during template pagination', async () => {
    const browser = await chromium.launch();
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
      // Navigate to templates page
      await page.goto('/templates');
      await page.waitForLoadState('networkidle');

      // Get initial memory usage
      const initialMetrics = await page.evaluate(() => {
        if (performance && (performance as any).memory) {
          return {
            usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
            totalJSHeapSize: (performance as any).memory.totalJSHeapSize,
          };
        }
        return null;
      });

      if (!initialMetrics) {
        test.skip();
        return;
      }

      // Scroll through multiple pages of templates
      for (let i = 0; i < 10; i++) {
        await page.evaluate(() => {
          window.scrollTo(0, document.body.scrollHeight);
        });
        await page.waitForTimeout(500);
      }

      // Scroll back to top to release items
      await page.evaluate(() => {
        window.scrollTo(0, 0);
      });
      await page.waitForTimeout(1000);

      // Force garbage collection if available
      await page.evaluate(() => {
        if ((window as any).gc) {
          (window as any).gc();
        }
      });

      // Get final memory usage
      const finalMetrics = await page.evaluate(() => {
        if (performance && (performance as any).memory) {
          return {
            usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
            totalJSHeapSize: (performance as any).memory.totalJSHeapSize,
          };
        }
        return null;
      });

      if (finalMetrics && initialMetrics) {
        const memoryGrowth = finalMetrics.usedJSHeapSize - initialMetrics.usedJSHeapSize;
        const growthPercentage = (memoryGrowth / initialMetrics.usedJSHeapSize) * 100;

        console.log(`Initial heap: ${(initialMetrics.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB`);
        console.log(`Final heap: ${(finalMetrics.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB`);
        console.log(`Growth: ${(memoryGrowth / 1024 / 1024).toFixed(2)} MB (${growthPercentage.toFixed(1)}%)`);

        // Memory should not grow by more than 50% during scrolling
        expect(growthPercentage).toBeLessThan(50);
      }
    } finally {
      await page.close();
      await context.close();
      await browser.close();
    }
  });

  test('should maintain performance with large template list', async ({ page }) => {
    // Mock a large list of templates
    await page.route('**/api/templates*', (route) => {
      const templates = Array.from({ length: 1000 }, (_, i) => ({
        id: `template-${i}`,
        name: `Template ${i}`,
        description: `Description for template ${i}`,
        thumbnail: `/assets/thumbnails/template-${i}.jpg`,
        category: ['Business', 'Education', 'Entertainment'][i % 3],
        duration: 30 + (i % 120),
      }));

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          templates,
          total: 1000,
          page: 1,
          pageSize: 1000,
        }),
      });
    });

    await page.goto('/templates');
    
    // Measure time to first render
    const startTime = Date.now();
    await page.waitForSelector('[data-testid="template-list"], .template-grid', {
      timeout: 10000,
    });
    const renderTime = Date.now() - startTime;

    console.log(`Templates rendered in ${renderTime}ms`);

    // Should render within 5 seconds even with 1000 items
    expect(renderTime).toBeLessThan(5000);

    // Verify virtualization is working (not all items rendered)
    const renderedItems = await page.locator('[data-testid="template-item"], .template-card').count();
    
    // With virtualization, should render far fewer than 1000 items
    expect(renderedItems).toBeLessThan(100);
    expect(renderedItems).toBeGreaterThan(0);
  });

  test('should not accumulate event listeners during navigation', async ({ page }) => {
    await page.goto('/');

    // Get initial listener count
    const initialListeners = await page.evaluate(() => {
      return (window as any).getEventListeners ? 
        Object.keys((window as any).getEventListeners(document)).length : 0;
    });

    // Navigate through multiple pages
    const routes = ['/', '/templates', '/settings', '/'];
    for (const route of routes) {
      await page.goto(route);
      await page.waitForLoadState('networkidle');
    }

    // Get final listener count
    const finalListeners = await page.evaluate(() => {
      return (window as any).getEventListeners ? 
        Object.keys((window as any).getEventListeners(document)).length : 0;
    });

    // Listener count should not grow significantly
    if (initialListeners > 0 && finalListeners > 0) {
      const growth = finalListeners - initialListeners;
      console.log(`Event listeners - Initial: ${initialListeners}, Final: ${finalListeners}, Growth: ${growth}`);
      
      // Allow some growth but not exponential
      expect(growth).toBeLessThan(initialListeners * 0.5);
    }
  });

  test('should clean up resources after job completion', async ({ page }) => {
    // Mock job lifecycle
    let jobStatus = 'Running';
    
    await page.route('**/api/jobs/test-job-001/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'test-job-001',
          status: jobStatus,
          progress: jobStatus === 'Running' ? 50 : 100,
        }),
      });
    });

    await page.route('**/api/jobs/test-job-001/events', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: 'data: {"status":"Running","progress":50}\n\n',
      });
    });

    await page.goto('/');

    // Get baseline metrics
    const beforeMetrics = await page.evaluate(() => ({
      heap: (performance as any).memory?.usedJSHeapSize || 0,
      connections: (performance as any).getEntriesByType?.('resource').length || 0,
    }));

    // Simulate job running
    await page.waitForTimeout(2000);

    // Complete the job
    jobStatus = 'Completed';
    await page.waitForTimeout(2000);

    // Force cleanup
    await page.evaluate(() => {
      if ((window as any).gc) {
        (window as any).gc();
      }
    });

    await page.waitForTimeout(1000);

    // Get after metrics
    const afterMetrics = await page.evaluate(() => ({
      heap: (performance as any).memory?.usedJSHeapSize || 0,
      connections: (performance as any).getEntriesByType?.('resource').length || 0,
    }));

    console.log('Before:', beforeMetrics);
    console.log('After:', afterMetrics);

    // Resources should be cleaned up (allowing for some variance)
    if (beforeMetrics.heap > 0 && afterMetrics.heap > 0) {
      const heapGrowth = (afterMetrics.heap - beforeMetrics.heap) / beforeMetrics.heap;
      expect(heapGrowth).toBeLessThan(0.3); // Less than 30% growth
    }
  });

  test('should handle rapid state updates without performance degradation', async ({ page }) => {
    await page.goto('/');

    const startTime = Date.now();
    const updates = 100;

    // Trigger rapid state updates (adjust based on actual UI)
    await page.evaluate((count) => {
      for (let i = 0; i < count; i++) {
        window.dispatchEvent(new CustomEvent('test-update', { detail: i }));
      }
    }, updates);

    const endTime = Date.now();
    const duration = endTime - startTime;

    console.log(`${updates} updates processed in ${duration}ms`);

    // Should handle updates without significant lag
    expect(duration).toBeLessThan(1000);
    
    // UI should remain responsive
    const button = await page.locator('button').first();
    if (await button.isVisible({ timeout: 1000 }).catch(() => false)) {
      await button.click();
      // Click should be processed quickly
    }
  });
});
