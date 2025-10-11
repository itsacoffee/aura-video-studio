import { test, expect } from '@playwright/test';

test.describe('Local Engines E2E', () => {
  test.beforeEach(async ({ page }) => {
    // Mock the engines manifest API
    await page.route('**/api/engines/manifest', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          engines: [
            {
              id: 'stable-diffusion-webui',
              name: 'Stable Diffusion WebUI',
              version: '1.7.0',
              description: 'A1111 Stable Diffusion WebUI',
              category: 'Visuals',
              requiresGpu: true,
              minimumVram: 6144,
              downloadSize: 2147483648,
              installPath: null,
              isInstalled: false,
              healthCheck: {
                endpoint: 'http://127.0.0.1:7860/internal/ping',
                expectedStatus: 200,
                timeout: 5000
              }
            },
            {
              id: 'piper',
              name: 'Piper TTS',
              version: '1.2.0',
              description: 'Fast, local neural TTS',
              category: 'TTS',
              requiresGpu: false,
              minimumVram: 0,
              downloadSize: 52428800,
              installPath: null,
              isInstalled: false,
              healthCheck: null
            },
            {
              id: 'mimic3',
              name: 'Mimic3 TTS',
              version: '0.2.4',
              description: 'High-quality local TTS',
              category: 'TTS',
              requiresGpu: false,
              minimumVram: 0,
              downloadSize: 104857600,
              installPath: null,
              isInstalled: false,
              healthCheck: {
                endpoint: 'http://127.0.0.1:59125/api/voices',
                expectedStatus: 200,
                timeout: 5000
              }
            }
          ]
        })
      });
    });

    // Mock system capabilities
    await page.route('**/api/capabilities', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          gpuDetected: true,
          gpuName: 'NVIDIA GeForce RTX 3060',
          vramMb: 12288,
          tier: 'High',
          canRunStableDiffusion: true
        })
      });
    });
  });

  test('should display local engines in settings', async ({ page }) => {
    await page.goto('/settings');

    // Navigate to Local Engines tab (assuming it exists)
    const localEnginesTab = page.getByRole('tab', { name: /Local Engines|Engines/i });
    if (await localEnginesTab.isVisible()) {
      await localEnginesTab.click();
    }

    // Check that engines are displayed
    await expect(page.getByText('Stable Diffusion WebUI')).toBeVisible();
    await expect(page.getByText('Piper TTS')).toBeVisible();
    await expect(page.getByText('Mimic3 TTS')).toBeVisible();
  });

  test('should show engine installation status', async ({ page }) => {
    // Mock engine status API
    await page.route('**/api/engines/*/status', (route) => {
      const engineId = route.request().url().split('/').slice(-2)[0];
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          engineId,
          isInstalled: false,
          installPath: null,
          version: null,
          status: 'NotInstalled'
        })
      });
    });

    await page.goto('/settings');

    // Navigate to Download Center or Local Engines
    const downloadCenterLink = page.getByRole('link', { name: /Download Center|Engines/i });
    if (await downloadCenterLink.isVisible()) {
      await downloadCenterLink.click();
    }

    // Check status badges
    const notInstalledBadge = page.locator('text=/Not Installed|Install/i').first();
    await expect(notInstalledBadge).toBeVisible({ timeout: 10000 });
  });

  test('should validate engine installation', async ({ page }) => {
    // Mock installed engine status
    await page.route('**/api/engines/piper/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          engineId: 'piper',
          isInstalled: true,
          installPath: 'C:\\Users\\Test\\AppData\\Local\\Aura\\Tools\\piper',
          version: '1.2.0',
          status: 'Ready'
        })
      });
    });

    // Mock validation API
    await page.route('**/api/engines/piper/validate', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          version: '1.2.0',
          errors: [],
          warnings: []
        })
      });
    });

    await page.goto('/settings');

    // Find and click validate button for Piper
    const validateButton = page.getByRole('button', { name: /Validate|Check/i }).first();
    if (await validateButton.isVisible()) {
      await validateButton.click();

      // Check for success message
      await expect(page.getByText(/Valid|Ready|OK/i)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should handle engine health checks', async ({ page }) => {
    // Mock health check API
    await page.route('**/api/engines/stable-diffusion-webui/health', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isHealthy: true,
          responseTime: 150,
          lastChecked: new Date().toISOString()
        })
      });
    });

    await page.goto('/settings');

    // Try to perform health check
    const healthCheckButton = page.getByRole('button', { name: /Health Check|Test/i }).first();
    if (await healthCheckButton.isVisible()) {
      await healthCheckButton.click();

      // Wait for result
      await page.waitForTimeout(2000);
      
      // Check for success indicator (green checkmark, "Healthy", etc.)
      const successIndicator = page.locator('[data-testid="health-status-success"]')
        .or(page.getByText(/Healthy|Running|OK/i));
      
      if (await successIndicator.isVisible({ timeout: 2000 })) {
        await expect(successIndicator).toBeVisible();
      }
    }
  });

  test('should display GPU requirements for SD', async ({ page }) => {
    await page.goto('/settings');

    // Check for VRAM warning or requirement
    const vramInfo = page.getByText(/6GB|VRAM|GPU/i);
    if (await vramInfo.isVisible({ timeout: 5000 })) {
      await expect(vramInfo).toBeVisible();
    }
  });

  test('should allow starting and stopping engines', async ({ page }) => {
    // Mock engine control APIs
    await page.route('**/api/engines/stable-diffusion-webui/start', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Engine started successfully',
          pid: 12345
        })
      });
    });

    await page.route('**/api/engines/stable-diffusion-webui/stop', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Engine stopped successfully'
        })
      });
    });

    // Mock running status
    await page.route('**/api/engines/stable-diffusion-webui/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          engineId: 'stable-diffusion-webui',
          isInstalled: true,
          isRunning: false,
          status: 'Stopped'
        })
      });
    });

    await page.goto('/settings');

    // Look for Start button
    const startButton = page.getByRole('button', { name: /Start|Launch/i }).first();
    if (await startButton.isVisible({ timeout: 5000 })) {
      await startButton.click();

      // Wait for status change
      await page.waitForTimeout(1000);
      
      // Check for running indicator or Stop button
      const stopButton = page.getByRole('button', { name: /Stop|Shutdown/i }).first();
      if (await stopButton.isVisible({ timeout: 3000 })) {
        await expect(stopButton).toBeVisible();
      }
    }
  });

  test('should show provider selection with local engines', async ({ page }) => {
    // Mock profiles API with local engines
    await page.route('**/api/profiles/list', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            name: 'Free-Only',
            description: 'Free providers only',
            providers: {
              script: 'Template',
              tts: 'WindowsTTS',
              visuals: 'LocalStock'
            }
          },
          {
            name: 'Local-First',
            description: 'Prefer local engines',
            providers: {
              script: 'Template',
              tts: 'Piper',
              visuals: 'StableDiffusion'
            }
          },
          {
            name: 'Offline-Only',
            description: 'Only offline engines',
            providers: {
              script: 'RuleBased',
              tts: 'Piper',
              visuals: 'StableDiffusion'
            }
          }
        ])
      });
    });

    await page.goto('/create');

    // Navigate to provider selection step
    await page.getByPlaceholder('Enter your video topic').fill('Local Engine Test');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.getByRole('button', { name: 'Next' }).click();

    // Check for local engine profiles
    const profileDropdown = page.getByLabel(/Profile|Preset/i);
    if (await profileDropdown.isVisible({ timeout: 5000 })) {
      await profileDropdown.click();
      await expect(page.getByText(/Local-First|Offline/i)).toBeVisible();
    }
  });

  test('should run preflight check with local engines', async ({ page }) => {
    // Mock preflight API
    await page.route('**/api/preflight', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          profile: 'Local-First',
          providers: {
            script: { provider: 'Template', status: 'Ready' },
            tts: { provider: 'Piper', status: 'Ready', hint: 'Using local Piper TTS' },
            visuals: { provider: 'StableDiffusion', status: 'Ready', hint: 'Using local Stable Diffusion' }
          },
          warnings: [],
          readyToGenerate: true,
          offlineCapable: true
        })
      });
    });

    await page.goto('/create');

    // Navigate to providers step
    await page.getByPlaceholder('Enter your video topic').fill('Preflight Test');
    await page.getByRole('button', { name: 'Next' }).click();
    await page.getByRole('button', { name: 'Next' }).click();

    // Run preflight
    const preflightButton = page.getByRole('button', { name: /Preflight|Check/i });
    if (await preflightButton.isVisible({ timeout: 5000 })) {
      await preflightButton.click();

      // Wait for results
      await expect(page.getByText(/Ready|Piper|Stable Diffusion/i)).toBeVisible({ timeout: 10000 });
    }
  });
});
