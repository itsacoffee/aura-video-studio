import { test, expect } from '@playwright/test';

test.describe('Dependency Download E2E', () => {
  test('should complete FFmpeg download successfully', async ({ page }) => {
    let downloadProgress = 0;
    
    // Mock dependency manifest API
    await page.route('**/api/dependencies/manifest', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          components: [
            {
              name: 'FFmpeg',
              version: '6.0',
              isRequired: true,
              installPath: 'ffmpeg',
              files: [
                {
                  filename: 'ffmpeg.exe',
                  url: 'https://example.com/ffmpeg.exe',
                  sha256: 'abc123',
                  sizeBytes: 104857600, // 100MB
                },
              ],
            },
          ],
        }),
      });
    });

    // Mock component status - initially not installed
    await page.route('**/api/dependencies/FFmpeg/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          componentName: 'FFmpeg',
          isInstalled: downloadProgress >= 100,
          isValid: downloadProgress >= 100,
          missingFiles: downloadProgress >= 100 ? [] : ['ffmpeg.exe'],
        }),
      });
    });

    // Mock download API with progress updates
    await page.route('**/api/dependencies/FFmpeg/download', async (route) => {
      // Simulate download progress
      downloadProgress = 0;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Download started',
        }),
      });
    });

    // Mock download progress API
    await page.route('**/api/dependencies/FFmpeg/progress', (route) => {
      downloadProgress = Math.min(downloadProgress + 25, 100);
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          componentName: 'FFmpeg',
          status: downloadProgress >= 100 ? 'completed' : 'downloading',
          progress: downloadProgress,
          downloadedBytes: (104857600 * downloadProgress) / 100,
          totalBytes: 104857600,
        }),
      });
    });

    // Navigate to dependencies/download page (assuming it exists)
    await page.goto('/settings');
    
    // Look for Dependencies tab
    const dependenciesTab = page.getByRole('tab', { name: /Dependencies|Components/i });
    if (await dependenciesTab.isVisible({ timeout: 5000 })) {
      await dependenciesTab.click();
    } else {
      // Try navigating directly
      await page.goto('/dependencies');
    }

    // Should show FFmpeg as required but not installed
    await expect(page.getByText('FFmpeg')).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/Required|Not Installed/i)).toBeVisible();

    // Click download button
    const downloadButton = page.getByRole('button', { name: /Download|Install/i }).first();
    await downloadButton.click();

    // Should show download in progress
    await expect(page.getByText(/Downloading|Download started/i)).toBeVisible({ timeout: 5000 });

    // Wait for completion (with mocked progress)
    await expect(page.getByText(/Complete|Installed|Success/i)).toBeVisible({ timeout: 10000 });
  });

  test('should handle network failure and allow repair', async ({ page }) => {
    let downloadAttempt = 0;
    let isCorrupted = false;

    // Mock dependency manifest API
    await page.route('**/api/dependencies/manifest', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          components: [
            {
              name: 'FFmpeg',
              version: '6.0',
              isRequired: true,
              installPath: 'ffmpeg',
              files: [
                {
                  filename: 'ffmpeg.exe',
                  url: 'https://example.com/ffmpeg.exe',
                  sha256: 'abc123',
                  sizeBytes: 104857600,
                },
              ],
            },
          ],
        }),
      });
    });

    // Mock download API - fail on first attempt, succeed on retry
    await page.route('**/api/dependencies/FFmpeg/download', (route) => {
      downloadAttempt++;
      
      if (downloadAttempt === 1) {
        // First attempt: Network failure
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({
            success: false,
            error: 'Network timeout',
            message: 'Failed to download FFmpeg: Network connection lost',
          }),
        });
      } else {
        // Retry: Success
        isCorrupted = false;
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            success: true,
            message: 'Download completed successfully',
          }),
        });
      }
    });

    // Mock component status
    await page.route('**/api/dependencies/FFmpeg/status', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          componentName: 'FFmpeg',
          isInstalled: downloadAttempt > 1 && !isCorrupted,
          isValid: downloadAttempt > 1 && !isCorrupted,
          missingFiles: downloadAttempt > 1 ? [] : ['ffmpeg.exe'],
        }),
      });
    });

    // Mock verify/repair API
    await page.route('**/api/dependencies/FFmpeg/verify', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          componentName: 'FFmpeg',
          isValid: !isCorrupted,
          missingFiles: isCorrupted ? ['ffmpeg.exe'] : [],
          corruptedFiles: [],
        }),
      });
    });

    await page.route('**/api/dependencies/FFmpeg/repair', (route) => {
      isCorrupted = false;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          message: 'Repair completed successfully',
        }),
      });
    });

    // Navigate to dependencies page
    await page.goto('/settings');
    const dependenciesTab = page.getByRole('tab', { name: /Dependencies|Components/i });
    if (await dependenciesTab.isVisible({ timeout: 5000 })) {
      await dependenciesTab.click();
    } else {
      await page.goto('/dependencies');
    }

    // Should show FFmpeg
    await expect(page.getByText('FFmpeg')).toBeVisible({ timeout: 5000 });

    // Click download button - should fail
    const downloadButton = page.getByRole('button', { name: /Download|Install/i }).first();
    await downloadButton.click();

    // Should show error message
    await expect(page.getByText(/Failed|Error|Network/i)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Network connection lost|timeout/i)).toBeVisible();

    // Should show retry/repair button
    const retryButton = page.getByRole('button', { name: /Retry|Repair|Try Again/i }).first();
    await expect(retryButton).toBeVisible();
    await retryButton.click();

    // Should succeed on retry
    await expect(page.getByText(/Success|Complete|Installed/i)).toBeVisible({ timeout: 10000 });
  });

  test('should allow manual install with offline instructions', async ({ page }) => {
    // Mock dependency manifest API
    await page.route('**/api/dependencies/manifest', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          components: [
            {
              name: 'FFmpeg',
              version: '6.0',
              isRequired: true,
              installPath: 'ffmpeg',
              files: [
                {
                  filename: 'ffmpeg.exe',
                  url: 'https://example.com/ffmpeg.exe',
                  sha256: 'abc123def456',
                  sizeBytes: 104857600,
                },
              ],
            },
          ],
        }),
      });
    });

    // Mock manual install instructions API
    await page.route('**/api/dependencies/FFmpeg/manual-instructions', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          componentName: 'FFmpeg',
          version: '6.0',
          installPath: 'C:\\Users\\User\\AppData\\Local\\Aura\\ffmpeg',
          steps: [
            'Download FFmpeg 6.0 from https://example.com/ffmpeg.exe',
            'Verify SHA-256: abc123def456',
            'Place file in: C:\\Users\\User\\AppData\\Local\\Aura\\ffmpeg',
            'Restart application',
          ],
        }),
      });
    });

    // Navigate to dependencies page
    await page.goto('/settings');
    const dependenciesTab = page.getByRole('tab', { name: /Dependencies|Components/i });
    if (await dependenciesTab.isVisible({ timeout: 5000 })) {
      await dependenciesTab.click();
    } else {
      await page.goto('/dependencies');
    }

    // Should show FFmpeg
    await expect(page.getByText('FFmpeg')).toBeVisible({ timeout: 5000 });

    // Look for manual install option
    const manualButton = page.getByRole('button', { name: /Manual|Offline|Instructions/i }).first();
    if (await manualButton.isVisible({ timeout: 2000 })) {
      await manualButton.click();

      // Should show manual instructions
      await expect(page.getByText(/Download FFmpeg/i)).toBeVisible();
      await expect(page.getByText(/SHA-256/i)).toBeVisible();
      await expect(page.getByText(/abc123def456/)).toBeVisible();
    }
  });
});
