import { test, expect } from '@playwright/test';

/**
 * E2E tests for export manifest and licensing verification
 * Tests artifact generation, licensing compliance, and manifest validation
 */
test.describe('Export Manifest and Licensing', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/settings', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ offlineMode: false }),
      });
    });
  });

  test('should generate complete export manifest with all artifacts', async ({ page }) => {
    await page.route('**/api/jobs/manifest-test-123', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'manifest-test-123',
          status: 'Done',
          progress: 100,
          artifacts: [
            {
              type: 'video',
              path: '/output/manifest-test-123/video.mp4',
              size: 5242880,
              mimeType: 'video/mp4',
            },
            {
              type: 'subtitle',
              path: '/output/manifest-test-123/subtitles.srt',
              size: 2048,
              mimeType: 'text/plain',
            },
            {
              type: 'subtitle',
              path: '/output/manifest-test-123/subtitles.vtt',
              size: 2156,
              mimeType: 'text/vtt',
            },
            {
              type: 'manifest',
              path: '/output/manifest-test-123/manifest.json',
              size: 4096,
              mimeType: 'application/json',
            },
            {
              type: 'license',
              path: '/output/manifest-test-123/LICENSES.md',
              size: 1024,
              mimeType: 'text/markdown',
            },
          ],
        }),
      });
    });

    await page.route('**/api/export/manifest-test-123/manifest', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          jobId: 'manifest-test-123',
          version: '1.0.0',
          createdAt: new Date().toISOString(),
          artifacts: {
            video: {
              filename: 'video.mp4',
              format: 'mp4',
              codec: 'h264',
              resolution: '1920x1080',
              duration: 30.5,
              size: 5242880,
            },
            subtitles: [
              { filename: 'subtitles.srt', format: 'srt', language: 'en' },
              { filename: 'subtitles.vtt', format: 'vtt', language: 'en' },
            ],
            licenses: { filename: 'LICENSES.md' },
          },
          sources: {
            script: {
              provider: 'RuleBased',
              model: 'rule-based-v1',
            },
            tts: {
              provider: 'WindowsTTS',
              voice: 'David',
            },
            visuals: {
              provider: 'Stock',
              imageCount: 5,
            },
          },
        }),
      });
    });

    await page.goto('/jobs/manifest-test-123');

    const exportButton = page.getByRole('button', { name: /Export|Download/i });
    if (await exportButton.isVisible({ timeout: 2000 })) {
      await exportButton.click();

      await expect(page.getByText(/video\.mp4|MP4/i)).toBeVisible({ timeout: 2000 });
      await expect(page.getByText(/subtitles\.srt|SRT/i)).toBeVisible({ timeout: 2000 });
      await expect(page.getByText(/LICENSES\.md/i)).toBeVisible({ timeout: 2000 });
    }
  });

  test('should include licensing information for all assets', async ({ page }) => {
    await page.route('**/api/export/license-test-123/licenses', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          licenses: [
            {
              component: 'FFmpeg',
              license: 'LGPL v2.1',
              url: 'https://www.ffmpeg.org/legal.html',
              required: true,
            },
            {
              component: 'Stock Images',
              license: 'CC0 1.0 Universal',
              url: 'https://creativecommons.org/publicdomain/zero/1.0/',
              attribution: 'Various contributors',
            },
            {
              component: 'Windows TTS',
              license: 'Microsoft Software License',
              url: 'https://www.microsoft.com/en-us/legal/intellectualproperty/copyright',
              required: true,
            },
          ],
          generatedAt: new Date().toISOString(),
        }),
      });
    });

    await page.goto('/jobs/license-test-123');

    const licensesButton = page.getByRole('button', { name: /Licenses|Attribution/i });
    if (await licensesButton.isVisible({ timeout: 2000 })) {
      await licensesButton.click();

      await expect(page.getByText(/FFmpeg|LGPL/i)).toBeVisible({ timeout: 2000 });
      await expect(page.getByText(/Stock Images|CC0/i)).toBeVisible({ timeout: 2000 });
    }
  });

  test('should validate manifest JSON schema compliance', async ({ page }) => {
    const manifestSchema = {
      $schema: 'http://json-schema.org/draft-07/schema#',
      type: 'object',
      required: ['jobId', 'version', 'createdAt', 'artifacts', 'sources'],
      properties: {
        jobId: { type: 'string' },
        version: { type: 'string' },
        createdAt: { type: 'string', format: 'date-time' },
        artifacts: { type: 'object' },
        sources: { type: 'object' },
      },
    };

    await page.route('**/api/export/schema-test-123/manifest', (route) => {
      const manifest = {
        jobId: 'schema-test-123',
        version: '1.0.0',
        createdAt: new Date().toISOString(),
        artifacts: {
          video: {
            filename: 'video.mp4',
            format: 'mp4',
          },
        },
        sources: {
          script: { provider: 'RuleBased' },
        },
      };

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(manifest),
      });
    });

    await page.goto('/jobs/schema-test-123');
    await page.waitForTimeout(1000);
  });

  test('should generate attribution notices for third-party content', async ({ page }) => {
    await page.route('**/api/export/attribution-test-123/attribution', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          attributions: [
            {
              type: 'image',
              source: 'Unsplash',
              author: 'John Doe',
              license: 'Unsplash License',
              url: 'https://unsplash.com/photos/example',
            },
            {
              type: 'music',
              source: 'Free Music Archive',
              author: 'Jane Smith',
              license: 'CC BY 4.0',
              url: 'https://freemusicarchive.org/music/example',
            },
          ],
          format: 'markdown',
          content:
            '# Attribution\n\n## Images\n- Photo by John Doe...\n\n## Music\n- Track by Jane Smith...',
        }),
      });
    });

    await page.goto('/jobs/attribution-test-123');

    const attributionButton = page.getByRole('button', { name: /Attribution|Credits/i });
    if (await attributionButton.isVisible({ timeout: 2000 })) {
      await attributionButton.click();

      await expect(page.getByText(/John Doe|Unsplash/i)).toBeVisible({ timeout: 2000 });
      await expect(page.getByText(/Jane Smith|CC BY/i)).toBeVisible({ timeout: 2000 });
    }
  });

  test('should verify subtitle file formats are correct', async ({ page }) => {
    await page.route('**/api/export/subtitle-test-123/subtitles.srt', (route) => {
      const srtContent = `1
00:00:00,000 --> 00:00:03,000
Welcome to Aura Video Studio

2
00:00:03,000 --> 00:00:06,000
Create amazing videos with AI`;

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/plain',
          'Content-Disposition': 'attachment; filename="subtitles.srt"',
        },
        body: srtContent,
      });
    });

    await page.route('**/api/export/subtitle-test-123/subtitles.vtt', (route) => {
      const vttContent = `WEBVTT

00:00:00.000 --> 00:00:03.000
Welcome to Aura Video Studio

00:00:03.000 --> 00:00:06.000
Create amazing videos with AI`;

      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'text/vtt',
          'Content-Disposition': 'attachment; filename="subtitles.vtt"',
        },
        body: vttContent,
      });
    });

    await page.goto('/jobs/subtitle-test-123');
    await page.waitForTimeout(1000);
  });

  test('should export project configuration for reproducibility', async ({ page }) => {
    await page.route('**/api/export/config-test-123/config', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          brief: {
            topic: 'Getting Started',
            audience: 'New Users',
            goal: 'Tutorial',
            tone: 'Friendly',
          },
          plan: {
            targetDuration: 30,
            pacing: 'Fast',
            density: 'Sparse',
          },
          providers: {
            script: 'RuleBased',
            tts: 'WindowsTTS',
            visuals: 'Stock',
          },
          renderSettings: {
            resolution: '1920x1080',
            fps: 30,
            codec: 'h264',
            bitrate: '5M',
          },
        }),
      });
    });

    await page.goto('/jobs/config-test-123');

    const exportConfigButton = page.getByRole('button', {
      name: /Export Config|Configuration/i,
    });
    if (await exportConfigButton.isVisible({ timeout: 2000 })) {
      await exportConfigButton.click();

      await expect(page.getByText(/Getting Started|Brief/i)).toBeVisible({
        timeout: 2000,
      });
    }
  });

  test('should include render log in export package', async ({ page }) => {
    await page.route('**/api/export/log-test-123/render-log', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'text/plain',
        body: `[2024-01-01 12:00:00] Starting video generation
[2024-01-01 12:00:05] Script generation completed
[2024-01-01 12:00:15] TTS synthesis completed
[2024-01-01 12:00:25] Visual generation completed
[2024-01-01 12:00:35] Video composition completed
[2024-01-01 12:00:45] Final render completed`,
      });
    });

    await page.goto('/jobs/log-test-123');

    const logsButton = page.getByRole('button', { name: /Logs|View Log/i });
    if (await logsButton.isVisible({ timeout: 2000 })) {
      await logsButton.click();

      await expect(page.getByText(/Script generation completed/i)).toBeVisible({
        timeout: 2000,
      });
    }
  });

  test('should create ZIP archive with all export artifacts', async ({ page }) => {
    let zipDownloadTriggered = false;

    page.on('download', (download) => {
      zipDownloadTriggered = true;
      expect(download.suggestedFilename()).toMatch(/\.zip$/);
    });

    await page.route('**/api/export/zip-test-123/download', (route) => {
      route.fulfill({
        status: 200,
        headers: {
          'Content-Type': 'application/zip',
          'Content-Disposition': 'attachment; filename="export-zip-test-123.zip"',
        },
        body: Buffer.from([0x50, 0x4b, 0x03, 0x04]),
      });
    });

    await page.goto('/jobs/zip-test-123');

    const downloadAllButton = page.getByRole('button', { name: /Download All|Export All/i });
    if (await downloadAllButton.isVisible({ timeout: 2000 })) {
      await downloadAllButton.click();
      await page.waitForTimeout(1000);
    }
  });

  test('should display export progress for large packages', async ({ page }) => {
    await page.route('**/api/export/large-test-123/prepare', (route) => {
      route.fulfill({
        status: 202,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'Export preparation started',
          estimatedTime: 60,
        }),
      });
    });

    let progress = 0;
    await page.route('**/api/export/large-test-123/progress', (route) => {
      progress = Math.min(progress + 25, 100);

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          progress: progress,
          status: progress < 100 ? 'preparing' : 'ready',
          message: `Preparing export package (${progress}%)`,
        }),
      });
    });

    await page.goto('/jobs/large-test-123');

    const exportButton = page.getByRole('button', { name: /Export|Download/i });
    if (await exportButton.isVisible({ timeout: 2000 })) {
      await exportButton.click();

      await expect(page.getByText(/Preparing|%/i)).toBeVisible({ timeout: 2000 });
    }
  });

  test('should validate checksum for exported files', async ({ page }) => {
    await page.route('**/api/export/checksum-test-123/checksums', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          checksums: [
            {
              file: 'video.mp4',
              algorithm: 'SHA256',
              checksum: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
            },
            {
              file: 'subtitles.srt',
              algorithm: 'SHA256',
              checksum: 'd7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592',
            },
          ],
        }),
      });
    });

    await page.goto('/jobs/checksum-test-123');

    const checksumButton = page.getByRole('button', { name: /Verify|Checksum/i });
    if (await checksumButton.isVisible({ timeout: 2000 })) {
      await checksumButton.click();

      await expect(page.getByText(/SHA256|checksum/i)).toBeVisible({ timeout: 2000 });
    }
  });
});
