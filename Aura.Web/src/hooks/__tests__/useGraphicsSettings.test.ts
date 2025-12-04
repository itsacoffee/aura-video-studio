/**
 * Tests for useGraphicsSettings hook
 */

import { renderHook, waitFor, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { graphicsSettingsService } from '../../services/graphicsSettingsService';
import type { GraphicsSettings } from '../../types/graphicsSettings';
import { useGraphicsSettings } from '../useGraphicsSettings';

// Mock the graphicsSettingsService
vi.mock('../../services/graphicsSettingsService', () => ({
  graphicsSettingsService: {
    loadSettings: vi.fn(),
    saveSettings: vi.fn(),
    applyProfile: vi.fn(),
    resetToDefaults: vi.fn(),
    detectOptimalSettings: vi.fn(),
    subscribe: vi.fn(),
  },
}));

// Mock applyGraphicsSettings
vi.mock('../../styles/graphicsProvider', () => ({
  applyGraphicsSettings: vi.fn(),
}));

const defaultSettings: GraphicsSettings = {
  profile: 'maximum',
  gpuAccelerationEnabled: true,
  detectedGpuName: null,
  detectedGpuVendor: null,
  detectedVramMB: 0,
  effects: {
    animations: true,
    blurEffects: true,
    shadows: true,
    transparency: true,
    smoothScrolling: true,
    springPhysics: true,
    parallaxEffects: true,
    glowEffects: true,
    microInteractions: true,
    staggeredAnimations: true,
  },
  scaling: {
    mode: 'system',
    manualScaleFactor: 1.0,
    perMonitorDpiAware: true,
    subpixelRendering: true,
  },
  accessibility: {
    reducedMotion: false,
    highContrast: false,
    largeText: false,
    focusIndicators: true,
  },
  lastModified: new Date().toISOString(),
  settingsVersion: 1,
};

describe('useGraphicsSettings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(graphicsSettingsService.loadSettings).mockResolvedValue(defaultSettings);
    vi.mocked(graphicsSettingsService.saveSettings).mockResolvedValue(true);
    vi.mocked(graphicsSettingsService.subscribe).mockReturnValue(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('initialization', () => {
    it('should start with loading state', () => {
      const { result } = renderHook(() => useGraphicsSettings());

      expect(result.current.loading).toBe(true);
    });

    it('should load settings on mount', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(graphicsSettingsService.loadSettings).toHaveBeenCalled();
    });

    it('should set error state on load failure', async () => {
      vi.mocked(graphicsSettingsService.loadSettings).mockRejectedValue(
        new Error('Failed to load')
      );

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.error).toBeInstanceOf(Error);
      expect(result.current.error?.message).toBe('Failed to load');
    });
  });

  describe('convenience booleans', () => {
    it('should compute animationsEnabled correctly', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // With default settings (animations: true, reducedMotion: false)
      expect(result.current.animationsEnabled).toBe(true);
    });

    it('should disable animations when reducedMotion is true', async () => {
      vi.mocked(graphicsSettingsService.loadSettings).mockResolvedValue({
        ...defaultSettings,
        accessibility: {
          ...defaultSettings.accessibility,
          reducedMotion: true,
        },
      });

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.animationsEnabled).toBe(false);
    });

    it('should disable blur when GPU acceleration is off', async () => {
      vi.mocked(graphicsSettingsService.loadSettings).mockResolvedValue({
        ...defaultSettings,
        gpuAccelerationEnabled: false,
      });

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.blurEnabled).toBe(false);
    });

    it('should compute shadowsEnabled correctly', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.shadowsEnabled).toBe(true);
    });

    it('should compute gpuEnabled correctly', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.gpuEnabled).toBe(true);
    });

    it('should compute reducedMotion correctly', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      expect(result.current.reducedMotion).toBe(false);
    });
  });

  describe('actions', () => {
    it('should update settings', async () => {
      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      await act(async () => {
        await result.current.updateSettings({
          gpuAccelerationEnabled: false,
        });
      });

      expect(graphicsSettingsService.saveSettings).toHaveBeenCalled();
      expect(result.current.settings.gpuAccelerationEnabled).toBe(false);
    });

    it('should apply profile', async () => {
      const balancedSettings = {
        ...defaultSettings,
        profile: 'balanced' as const,
        effects: {
          ...defaultSettings.effects,
          blurEffects: false,
          springPhysics: false,
        },
      };
      vi.mocked(graphicsSettingsService.applyProfile).mockResolvedValue(balancedSettings);

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      await act(async () => {
        await result.current.applyProfile('balanced');
      });

      expect(graphicsSettingsService.applyProfile).toHaveBeenCalledWith('balanced');
      expect(result.current.settings.profile).toBe('balanced');
    });

    it('should reset to defaults', async () => {
      vi.mocked(graphicsSettingsService.resetToDefaults).mockResolvedValue(defaultSettings);

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      await act(async () => {
        await result.current.resetToDefaults();
      });

      expect(graphicsSettingsService.resetToDefaults).toHaveBeenCalled();
    });

    it('should detect optimal settings', async () => {
      const optimalSettings = {
        ...defaultSettings,
        profile: 'balanced' as const,
      };
      vi.mocked(graphicsSettingsService.detectOptimalSettings).mockResolvedValue(optimalSettings);

      const { result } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      await act(async () => {
        await result.current.detectOptimal();
      });

      expect(graphicsSettingsService.detectOptimalSettings).toHaveBeenCalled();
    });
  });

  describe('subscription', () => {
    it('should subscribe to settings changes on mount', async () => {
      renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(graphicsSettingsService.subscribe).toHaveBeenCalled();
      });
    });

    it('should unsubscribe on unmount', async () => {
      const unsubscribe = vi.fn();
      vi.mocked(graphicsSettingsService.subscribe).mockReturnValue(unsubscribe);

      const { unmount } = renderHook(() => useGraphicsSettings());

      await waitFor(() => {
        expect(graphicsSettingsService.subscribe).toHaveBeenCalled();
      });

      unmount();

      expect(unsubscribe).toHaveBeenCalled();
    });
  });
});
