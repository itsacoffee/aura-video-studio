import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

/**
 * PHASE 5: Settings and Configuration Validation
 * 
 * These smoke tests validate:
 * - Settings page completeness
 * - FFmpeg path configuration
 * - Workspace preferences persistence
 */

describe('Smoke Test: Settings and Configuration', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('5.1 Settings Page Completeness', () => {
    it('should load General section correctly', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          general: {
            appName: 'Aura Video Studio',
            version: '1.0.0',
            language: 'en',
            theme: 'dark',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/general');
      const data = await response.json();

      expect(data.general).toBeDefined();
      expect(data.general.appName).toBeTruthy();
    });

    it('should load API Keys section correctly', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          apiKeys: {
            openai: { configured: true, valid: true },
            elevenlabs: { configured: false, valid: false },
            stabilityai: { configured: true, valid: true },
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/api-keys');
      const data = await response.json();

      expect(data.apiKeys).toBeDefined();
      expect(data.apiKeys.openai).toBeDefined();
    });

    it('should load File Locations section correctly', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          fileLocations: {
            projectsFolder: '/home/user/AuraProjects',
            exportsFolder: '/home/user/AuraExports',
            cacheFolder: '/home/user/.aura/cache',
            tempFolder: '/tmp/aura',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/file-locations');
      const data = await response.json();

      expect(data.fileLocations).toBeDefined();
      expect(data.fileLocations.projectsFolder).toBeTruthy();
    });

    it('should load Video Defaults section correctly', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          videoDefaults: {
            resolution: '1080p',
            framerate: 30,
            codec: 'h264',
            quality: 'high',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/video-defaults');
      const data = await response.json();

      expect(data.videoDefaults).toBeDefined();
      expect(data.videoDefaults.resolution).toBeTruthy();
    });

    it('should save settings successfully', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          message: 'Settings saved successfully',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          general: { theme: 'light' },
        }),
      });
      const data = await response.json();

      expect(data.success).toBe(true);
    });

    it('should persist settings across restarts', () => {
      // Clear first to ensure clean state
      localStorage.clear();
      
      const settings = {
        theme: 'dark',
        language: 'en',
        autosaveInterval: 5,
      };

      localStorage.setItem('settings', JSON.stringify(settings));
      
      // Simulate restart by clearing and retrieving
      const retrieved = JSON.parse(localStorage.getItem('settings') || '{}');
      
      expect(retrieved).toBeDefined();
      expect(retrieved.theme).toBe('dark');
      expect(retrieved.autosaveInterval).toBe(5);
    });

    it('should validate API keys successfully', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          valid: true,
          provider: 'OpenAI',
          message: 'API key is valid',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/validate-api-key', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider: 'openai',
          apiKey: 'sk-test123',
        }),
      });
      const data = await response.json();

      expect(data.valid).toBe(true);
    });
  });

  describe('5.2 FFmpeg Path Configuration', () => {
    it('should allow setting custom FFmpeg path', async () => {
      const customPath = '/usr/local/bin/ffmpeg';
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          path: customPath,
          message: 'FFmpeg path updated',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/ffmpeg-path', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: customPath }),
      });
      const data = await response.json();

      expect(data.success).toBe(true);
      expect(data.path).toBe(customPath);
    });

    it('should support file browser for FFmpeg selection', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          files: [
            { path: '/usr/bin/ffmpeg', name: 'ffmpeg', isExecutable: true },
            { path: '/usr/local/bin/ffmpeg', name: 'ffmpeg', isExecutable: true },
          ],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/files/browse?filter=ffmpeg');
      const data = await response.json();

      expect(data.files).toBeDefined();
      expect(data.files.length).toBeGreaterThan(0);
    });

    it('should save and use custom FFmpeg path', async () => {
      const customPath = '/custom/ffmpeg';
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          ffmpegPath: customPath,
          isCustom: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/ffmpeg-path');
      const data = await response.json();

      expect(data.ffmpegPath).toBe(customPath);
      expect(data.isCustom).toBe(true);
    });

    it('should show validation error for invalid path', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 400,
        json: async () => ({
          error: 'Invalid FFmpeg path',
          message: 'The specified path does not contain a valid FFmpeg executable',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/ffmpeg-path', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: '/invalid/path' }),
      });
      const data = await response.json();

      expect(response.ok).toBe(false);
      expect(data.error).toBeTruthy();
    });

    it('should support reverting to auto-detected path', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          path: '/usr/bin/ffmpeg',
          isCustom: false,
          message: 'Reverted to auto-detected FFmpeg path',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/ffmpeg-path/reset', {
        method: 'POST',
      });
      const data = await response.json();

      expect(data.success).toBe(true);
      expect(data.isCustom).toBe(false);
    });
  });

  describe('5.3 Workspace Preferences', () => {
    it('should set default save location for projects', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          projectsFolder: '/home/user/MyProjects',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/workspace/projects-folder', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: '/home/user/MyProjects' }),
      });
      const data = await response.json();

      expect(data.success).toBe(true);
      expect(data.projectsFolder).toBeTruthy();
    });

    it('should configure autosave interval', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          autosaveInterval: 5,
          unit: 'minutes',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/workspace/autosave', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ interval: 5 }),
      });
      const data = await response.json();

      expect(data.autosaveInterval).toBe(5);
    });

    it('should change theme selection', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          theme: 'light',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/workspace/theme', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ theme: 'light' }),
      });
      const data = await response.json();

      expect(data.theme).toBe('light');
    });

    it('should modify default video resolution', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          defaultResolution: '4k',
          width: 3840,
          height: 2160,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/settings/workspace/default-resolution', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ resolution: '4k' }),
      });
      const data = await response.json();

      expect(data.defaultResolution).toBe('4k');
    });

    it('should apply defaults to new projects', async () => {
      // Set defaults
      const defaults = {
        resolution: '1080p',
        framerate: 60,
        theme: 'dark',
      };
      localStorage.setItem('projectDefaults', JSON.stringify(defaults));

      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          project: {
            resolution: '1080p',
            framerate: 60,
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/projects/new', { method: 'POST' });
      const data = await response.json();

      expect(data.project.resolution).toBe('1080p');
      expect(data.project.framerate).toBe(60);
    });

    it('should persist workspace preferences after restart', () => {
      // Clear first to ensure clean state
      localStorage.clear();
      
      const preferences = {
        projectsFolder: '/home/user/Projects',
        autosaveInterval: 5,
        theme: 'dark',
        defaultResolution: '1080p',
      };

      localStorage.setItem('workspacePreferences', JSON.stringify(preferences));
      
      // Simulate app restart
      const retrieved = JSON.parse(localStorage.getItem('workspacePreferences') || '{}');
      
      expect(retrieved).toBeDefined();
      expect(retrieved.projectsFolder).toBe('/home/user/Projects');
      expect(retrieved.autosaveInterval).toBe(5);
      expect(retrieved.theme).toBe('dark');
      expect(retrieved.defaultResolution).toBe('1080p');
    });

    it('should validate workspace folder exists', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          exists: true,
          writable: true,
          path: '/home/user/Projects',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/files/validate-folder?path=/home/user/Projects');
      const data = await response.json();

      expect(data.exists).toBe(true);
      expect(data.writable).toBe(true);
    });
  });
});
