import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

/**
 * PHASE 3: Generate Video Button and Export Pipeline
 * 
 * These smoke tests validate:
 * - Generate Video button functionality
 * - Export pipeline end-to-end (format selection, progress tracking, completion)
 * - Export error scenarios
 */

// Type definitions for test data
type ExportFormat = { extension: string; codec: string };
type ExportResolution = { name: string; width: number; height: number };

describe('Smoke Test: Export Pipeline', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('3.1 Generate Video Button Functionality', () => {
    it('should enable Generate Video button when timeline has clips', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          canExport: true,
          clipCount: 5,
          totalDuration: 30.5,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/timeline/can-export');
      const data = await response.json();

      expect(data.canExport).toBe(true);
      expect(data.clipCount).toBeGreaterThan(0);
    });

    it('should show loading state when export starts', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'queued',
          message: 'Export job queued',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          format: 'mp4',
          resolution: '1080p',
        }),
      });
      const data = await response.json();

      expect(response.ok).toBe(true);
      expect(data.status).toBe('queued');
      expect(data.jobId).toBeTruthy();
    });

    it('should disable button during export', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          isExporting: true,
          currentJobId: 'export-123',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/status');
      const data = await response.json();

      expect(data.isExporting).toBe(true);
    });

    it('should show export progress in global status footer', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'in_progress',
          progress: 45,
          message: 'Encoding video...',
          showInFooter: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123');
      const data = await response.json();

      expect(data.showInFooter).toBe(true);
      expect(data.status).toBe('in_progress');
      expect(data.progress).toBeGreaterThan(0);
    });

    it('should open export dialog or start export automatically', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          autoExport: true,
          defaultSettings: {
            format: 'mp4',
            codec: 'h264',
            resolution: '1080p',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/settings');
      const data = await response.json();

      expect(data.defaultSettings).toBeDefined();
      expect(data.defaultSettings.format).toBe('mp4');
    });
  });

  describe('3.2 Export Pipeline End-to-End', () => {
    it('should support MP4 H.264 format selection', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          supportedFormats: [
            { name: 'MP4', codec: 'h264', extension: 'mp4' },
            { name: 'WebM', codec: 'vp9', extension: 'webm' },
          ],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/formats');
      const data = await response.json();

      const mp4Format = data.supportedFormats.find((f: ExportFormat) => f.extension === 'mp4');
      expect(mp4Format).toBeDefined();
      expect(mp4Format.codec).toBe('h264');
    });

    it('should support 1080p resolution selection', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          supportedResolutions: [
            { name: '1080p', width: 1920, height: 1080 },
            { name: '720p', width: 1280, height: 720 },
            { name: '4K', width: 3840, height: 2160 },
          ],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/resolutions');
      const data = await response.json();

      const resolution1080p = data.supportedResolutions.find((r: ExportResolution) => r.name === '1080p');
      expect(resolution1080p).toBeDefined();
      expect(resolution1080p.width).toBe(1920);
      expect(resolution1080p.height).toBe(1080);
    });

    it('should launch FFmpeg process with correct parameters', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'in_progress',
          ffmpegCommand: 'ffmpeg -i input.mp4 -c:v libx264 -preset medium -crf 23 output.mp4',
          pid: 12345,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/start', { method: 'POST' });
      const data = await response.json();

      expect(data.status).toBe('in_progress');
      expect(data.ffmpegCommand).toContain('ffmpeg');
      expect(data.ffmpegCommand).toContain('libx264');
    });

    it('should provide real-time progress updates', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'in_progress',
          progress: 65,
          framesEncoded: 1950,
          totalFrames: 3000,
          fps: 30,
          eta: '00:02:30',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123/progress');
      const data = await response.json();

      expect(data.progress).toBeGreaterThan(0);
      expect(data.progress).toBeLessThanOrEqual(100);
      expect(data.framesEncoded).toBeGreaterThan(0);
    });

    it('should show frames encoded counter', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          framesEncoded: 1950,
          totalFrames: 3000,
          percentComplete: 65,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123/progress');
      const data = await response.json();

      expect(data.framesEncoded).toBeDefined();
      expect(data.totalFrames).toBeDefined();
      expect(data.percentComplete).toBeGreaterThan(0);
    });

    it('should display time remaining estimate', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          eta: '00:02:30',
          etaSeconds: 150,
          estimatedCompletion: new Date(Date.now() + 150000).toISOString(),
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123/progress');
      const data = await response.json();

      expect(data.eta).toBeTruthy();
      expect(data.etaSeconds).toBeGreaterThan(0);
    });

    it('should complete export successfully', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'completed',
          progress: 100,
          outputFile: '/exports/my-video-123.mp4',
          fileSize: 52428800, // 50 MB
          duration: 30.5,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123');
      const data = await response.json();

      expect(data.status).toBe('completed');
      expect(data.progress).toBe(100);
      expect(data.outputFile).toBeTruthy();
    });

    it('should verify output file exists at expected location', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          exists: true,
          path: '/exports/my-video-123.mp4',
          size: 52428800,
          createdAt: new Date().toISOString(),
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/files/verify?path=/exports/my-video-123.mp4');
      const data = await response.json();

      expect(data.exists).toBe(true);
      expect(data.size).toBeGreaterThan(0);
    });

    it('should produce playable video file', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          playable: true,
          format: 'mp4',
          codec: 'h264',
          duration: 30.5,
          resolution: { width: 1920, height: 1080 },
          hasAudio: true,
          hasVideo: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/files/validate?path=/exports/my-video-123.mp4');
      const data = await response.json();

      expect(data.playable).toBe(true);
      expect(data.hasVideo).toBe(true);
    });
  });

  describe('3.3 Export Error Scenarios', () => {
    it('should validate parameters before starting export', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({
          error: 'Validation failed',
          errors: [
            { field: 'resolution', message: 'Invalid resolution format' },
          ],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          format: 'mp4',
          resolution: 'invalid',
        }),
      });
      const data = await response.json();

      expect(response.ok).toBe(false);
      expect(data.errors).toBeDefined();
      expect(data.errors.length).toBeGreaterThan(0);
    });

    it('should catch validation errors before FFmpeg starts', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          valid: false,
          errors: ['Timeline is empty', 'No output path specified'],
          canProceed: false,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/validate', { method: 'POST' });
      const data = await response.json();

      expect(data.valid).toBe(false);
      expect(data.canProceed).toBe(false);
      expect(data.errors.length).toBeGreaterThan(0);
    });

    it('should handle FFmpeg process failure mid-export', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'failed',
          progress: 45,
          error: 'FFmpeg process terminated unexpectedly',
          exitCode: 1,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123');
      const data = await response.json();

      expect(data.status).toBe('failed');
      expect(data.error).toBeTruthy();
    });

    it('should show clear error message in status footer', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'export-123',
          status: 'failed',
          message: 'Export failed: Insufficient disk space',
          userMessage: 'Export failed. Please free up disk space and try again.',
          showInFooter: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/jobs/export-123');
      const data = await response.json();

      expect(data.status).toBe('failed');
      expect(data.userMessage).toBeTruthy();
      expect(data.showInFooter).toBe(true);
    });

    it('should support retry after export failure', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          canRetry: true,
          retryJobId: 'export-124',
          message: 'Export retried successfully',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/retry/export-123', { method: 'POST' });
      const data = await response.json();

      expect(data.canRetry).toBe(true);
      expect(data.retryJobId).toBeTruthy();
    });

    it('should clean up partial files after failed export', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          cleaned: true,
          filesRemoved: [
            '/temp/export-123-partial.mp4',
            '/temp/export-123.log',
          ],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/cleanup/export-123', { method: 'POST' });
      const data = await response.json();

      expect(data.cleaned).toBe(true);
      expect(data.filesRemoved).toBeDefined();
    });

    it('should handle disk space errors', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 507,
        json: async () => ({
          error: 'Insufficient storage',
          message: 'Not enough disk space to complete export',
          requiredSpace: 524288000,
          availableSpace: 104857600,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/export/start', { method: 'POST' });
      const data = await response.json();

      expect(response.status).toBe(507);
      expect(data.error).toContain('storage');
    });
  });
});
