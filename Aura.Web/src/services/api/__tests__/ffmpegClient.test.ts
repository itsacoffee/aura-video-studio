/**
 * Unit tests for ffmpegClient enhancements
 * Tests the FFmpeg client methods added/updated in PR 336
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient, { resetCircuitBreaker } from '../apiClient';
import { ffmpegClient } from '../ffmpegClient';

// Mock the apiClient
vi.mock('../apiClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
  resetCircuitBreaker: vi.fn(),
}));

describe('ffmpegClient', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getStatus', () => {
    it('should get basic FFmpeg status', async () => {
      const mockStatus = {
        installed: true,
        valid: true,
        version: 'ffmpeg version 6.0',
        path: '/usr/bin/ffmpeg',
        source: 'System PATH',
        error: null,
        correlationId: 'abc-123',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockStatus });

      const result = await ffmpegClient.getStatus();

      expect(apiClient.get).toHaveBeenCalledWith('/api/ffmpeg/status', {
        _skipCircuitBreaker: true,
      });
      expect(result).toEqual(mockStatus);
    });

    it('should reset circuit breaker on successful status check', async () => {
      const mockStatus = {
        installed: true,
        valid: true,
        version: 'ffmpeg version 6.0',
        path: '/usr/bin/ffmpeg',
        source: 'System PATH',
        error: null,
        correlationId: 'def-456',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockStatus });

      await ffmpegClient.getStatus();

      expect(resetCircuitBreaker).toHaveBeenCalled();
    });

    it('should not reset circuit breaker if FFmpeg is not valid', async () => {
      const mockStatus = {
        installed: false,
        valid: false,
        version: null,
        path: null,
        source: 'Not found',
        error: 'FFmpeg not found on system',
        correlationId: 'ghi-789',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockStatus });

      await ffmpegClient.getStatus();

      expect(resetCircuitBreaker).not.toHaveBeenCalled();
    });
  });

  describe('getStatusExtended', () => {
    it('should get extended FFmpeg status with hardware acceleration details', async () => {
      const mockExtendedStatus = {
        installed: true,
        valid: true,
        version: 'ffmpeg version 6.0',
        path: '/usr/bin/ffmpeg',
        source: 'System PATH',
        error: null,
        errorCode: null,
        errorMessage: null,
        attemptedPaths: ['/usr/bin/ffmpeg', '/usr/local/bin/ffmpeg'],
        versionMeetsRequirement: true,
        minimumVersion: '4.0',
        hardwareAcceleration: {
          nvencSupported: true,
          amfSupported: false,
          quickSyncSupported: false,
          videoToolboxSupported: false,
          availableEncoders: ['h264_nvenc', 'hevc_nvenc'],
        },
        correlationId: 'jkl-012',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockExtendedStatus });

      const result = await ffmpegClient.getStatusExtended();

      expect(apiClient.get).toHaveBeenCalledWith('/api/system/ffmpeg/status', {
        _skipCircuitBreaker: true,
      });
      expect(result.hardwareAcceleration.nvencSupported).toBe(true);
      expect(result.hardwareAcceleration.availableEncoders).toHaveLength(2);
    });
  });

  describe('install', () => {
    it('should install FFmpeg with specified version', async () => {
      const mockResponse = {
        success: true,
        message: 'FFmpeg installed successfully',
        path: 'C:\\Program Files\\FFmpeg\\bin\\ffmpeg.exe',
        version: 'ffmpeg version 6.0',
        installedAt: '2024-01-15T10:30:00Z',
        correlationId: 'mno-345',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.install({ version: '6.0' });

      expect(apiClient.post).toHaveBeenCalledWith(
        '/api/ffmpeg/install',
        { version: '6.0' },
        { _skipCircuitBreaker: true }
      );
      expect(result.success).toBe(true);
      expect(result.path).toBeDefined();
    });

    it('should handle installation failure with detailed error codes', async () => {
      const mockResponse = {
        success: false,
        message: 'Download timed out',
        errorCode: 'E348',
        title: 'Download Timeout',
        detail: 'FFmpeg download timed out due to slow network',
        howToFix: [
          'Check your internet connection speed',
          'Try again later when network conditions improve',
          'Download FFmpeg manually',
        ],
        type: 'https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/ffmpeg-errors.md#e348',
        correlationId: 'pqr-678',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.install();

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe('E348');
      expect(result.howToFix).toBeDefined();
      expect(result.howToFix?.length).toBeGreaterThan(0);
    });

    it('should reset circuit breaker on successful installation', async () => {
      const mockResponse = {
        success: true,
        message: 'FFmpeg installed successfully',
        path: '/usr/local/bin/ffmpeg',
        version: 'ffmpeg version 6.0',
        correlationId: 'stu-901',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      await ffmpegClient.install();

      expect(resetCircuitBreaker).toHaveBeenCalled();
    });
  });

  describe('rescan', () => {
    it('should rescan system for FFmpeg installations', async () => {
      const mockResponse = {
        success: true,
        installed: true,
        version: 'ffmpeg version 6.0',
        path: '/opt/ffmpeg/bin/ffmpeg',
        source: 'Custom Directory',
        valid: true,
        error: null,
        message: 'FFmpeg found at /opt/ffmpeg/bin/ffmpeg',
        correlationId: 'vwx-234',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.rescan();

      expect(apiClient.post).toHaveBeenCalledWith('/api/ffmpeg/rescan', undefined, {
        _skipCircuitBreaker: true,
      });
      expect(result.success).toBe(true);
      expect(result.installed).toBe(true);
    });
  });

  describe('useExisting', () => {
    it('should validate and use existing FFmpeg installation', async () => {
      const mockResponse = {
        success: true,
        message: 'FFmpeg validated successfully',
        installed: true,
        valid: true,
        path: '/custom/path/ffmpeg.exe',
        version: 'ffmpeg version 5.1',
        source: 'Configured',
        correlationId: 'yza-567',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.useExisting({ path: '/custom/path/ffmpeg.exe' });

      expect(apiClient.post).toHaveBeenCalledWith(
        '/api/ffmpeg/use-existing',
        { path: '/custom/path/ffmpeg.exe' },
        { _skipCircuitBreaker: true }
      );
      expect(result.success).toBe(true);
      expect(result.valid).toBe(true);
    });

    it('should return detailed error for invalid FFmpeg path', async () => {
      const mockResponse = {
        success: false,
        message: 'The specified path does not contain a valid FFmpeg executable',
        installed: false,
        valid: false,
        path: null,
        version: null,
        source: 'None',
        title: 'Invalid FFmpeg',
        detail: 'The file at the specified path is not a valid FFmpeg executable',
        howToFix: [
          'Ensure the path points to ffmpeg.exe',
          'Verify FFmpeg is properly installed',
          'Try running "ffmpeg -version" manually to test',
        ],
        correlationId: 'bcd-890',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.useExisting({ path: '/invalid/path' });

      expect(result.success).toBe(false);
      expect(result.howToFix).toBeDefined();
      expect(result.howToFix?.length).toBeGreaterThan(0);
    });
  });

  describe('directCheck', () => {
    it('should fetch detailed diagnostics from debug endpoint', async () => {
      const mockResponse = {
        candidates: [
          {
            label: 'EnvVar',
            path: '/custom/ffmpeg',
            exists: true,
            executionAttempted: true,
            exitCode: 0,
            timedOut: false,
            rawVersionOutput: 'ffmpeg version 6.0',
            versionParsed: '6.0',
            valid: true,
            error: null,
          },
        ],
        overall: {
          installed: true,
          valid: true,
          source: 'EnvVar',
          chosenPath: '/custom/ffmpeg',
          version: '6.0',
        },
        correlationId: 'diag-001',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockResponse });

      const result = await ffmpegClient.directCheck();

      expect(apiClient.get).toHaveBeenCalledWith('/api/debug/ffmpeg/direct-check', {
        _skipCircuitBreaker: true,
      });
      expect(result.overall.installed).toBe(true);
      expect(result.candidates[0].label).toBe('EnvVar');
    });
  });
});
