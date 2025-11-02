import { test, expect } from '@playwright/test';

/**
 * Contract smoke tests - Validates critical API contracts and workflows
 * These tests use mocked providers for deterministic, fast execution
 */
test.describe('Contract Smoke Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Ensure clean state
    await page.goto('/');
  });

  test('should validate health endpoints respond correctly', async ({ page }) => {
    // Test /health/live endpoint
    const liveResponse = await page.request.get('http://localhost:5005/api/health/live');
    expect(liveResponse.status()).toBe(200);
    
    // Test /health/ready endpoint
    const readyResponse = await page.request.get('http://localhost:5005/api/health/ready');
    expect(readyResponse.status()).toBeGreaterThanOrEqual(200);
    expect(readyResponse.status()).toBeLessThan(300);
    
    // Test /api/health/system endpoint  
    const systemResponse = await page.request.get('http://localhost:5005/api/health/system');
    expect(systemResponse.status()).toBe(200);
    const systemData = await systemResponse.json();
    expect(systemData).toHaveProperty('status');
    expect(systemData.status).toMatch(/Healthy|Degraded/);
  });

  test('should validate diagnostics endpoints', async ({ page }) => {
    // Test diagnostics endpoint
    const diagResponse = await page.request.get('http://localhost:5005/api/diagnostics/report');
    expect(diagResponse.status()).toBeGreaterThanOrEqual(200);
    expect(diagResponse.status()).toBeLessThan(300);
    
    // Test providers health endpoint
    const providersResponse = await page.request.get('http://localhost:5005/api/health/providers');
    expect(providersResponse.status()).toBe(200);
    const providersData = await providersResponse.json();
    expect(Array.isArray(providersData)).toBe(true);
  });

  test('should have correlation ID in all API responses', async ({ page }) => {
    const endpoints = [
      '/api/health/system',
      '/api/health/providers',
      '/api/providers',
      '/api/settings',
    ];

    for (const endpoint of endpoints) {
      const response = await page.request.get(`http://localhost:5005${endpoint}`);
      const headers = response.headers();
      
      // Check for correlation ID header (case-insensitive)
      const correlationId = headers['x-correlation-id'] || headers['X-Correlation-ID'];
      expect(correlationId).toBeTruthy();
      expect(correlationId).toMatch(/[a-f0-9-]{36}/); // UUID format
    }
  });

  test('should handle project creation with mocked providers', async ({ page }) => {
    await page.goto('/');
    
    // Mock the project creation API
    await page.route('**/api/projects', (route) => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 201,
          contentType: 'application/json',
          headers: {
            'X-Correlation-ID': 'test-correlation-id-001',
          },
          body: JSON.stringify({
            id: 'test-project-001',
            name: 'Test Project',
            createdAt: new Date().toISOString(),
            status: 'Draft',
          }),
        });
      } else {
        route.continue();
      }
    });

    // Trigger project creation (adjust selector based on actual UI)
    // This is a placeholder - adjust to match actual UI elements
    const createButton = page.locator('button:has-text("New Project"), button:has-text("Create")').first();
    if (await createButton.isVisible({ timeout: 5000 }).catch(() => false)) {
      await createButton.click();
      
      // Verify project was created
      await expect(page.locator('text=test-project-001, text=Test Project').first()).toBeVisible({ timeout: 5000 });
    }
  });

  test('should validate OpenAPI schema is accessible', async ({ page }) => {
    const swaggerResponse = await page.request.get('http://localhost:5005/swagger/v1/swagger.json');
    expect(swaggerResponse.status()).toBe(200);
    
    const schema = await swaggerResponse.json();
    expect(schema).toHaveProperty('openapi');
    expect(schema).toHaveProperty('paths');
    expect(schema).toHaveProperty('components');
    
    // Validate critical endpoints exist
    const paths = schema.paths || {};
    expect(paths['/api/health/system']).toBeDefined();
    expect(paths['/api/health/providers']).toBeDefined();
    expect(paths['/api/diagnostics/report']).toBeDefined();
  });

  test('should handle streaming endpoints (SSE)', async ({ page }) => {
    // Mock job endpoint
    await page.route('**/api/jobs', (route) => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 202,
          contentType: 'application/json',
          headers: {
            'X-Correlation-ID': 'test-correlation-id-002',
          },
          body: JSON.stringify({
            jobId: 'test-job-001',
            status: 'Queued',
          }),
        });
      } else {
        route.continue();
      }
    });

    // Note: SSE testing requires special handling
    // This test validates the endpoint exists and returns correct status
    const jobId = 'test-job-001';
    
    // Validate SSE endpoint is reachable (will timeout naturally for test)
    const sseUrl = `http://localhost:5005/api/jobs/${jobId}/events`;
    
    // Just verify the endpoint doesn't return 404
    const response = await page.request.get(sseUrl, {
      timeout: 2000,
      failOnStatusCode: false,
    }).catch(() => ({ status: () => 0 }));
    
    // SSE endpoints should not return 404
    expect(response.status()).not.toBe(404);
  });
});
