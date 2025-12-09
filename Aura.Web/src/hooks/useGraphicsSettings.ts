/**
 * useGraphicsSettings Hook
 * Provides reactive access to graphics settings with automatic CSS updates
 */

import { useEffect, useState, useCallback } from 'react';
import { graphicsSettingsService } from '../services/graphicsSettingsService';
import { applyGraphicsSettings } from '../styles/graphicsProvider';
import type { GraphicsSettings, PerformanceProfile } from '../types/graphicsSettings';
import { createDefaultGraphicsSettings } from '../types/graphicsSettings';

interface UseGraphicsSettingsReturn {
  settings: GraphicsSettings;
  loading: boolean;
  error: Error | null;

  // Convenience booleans
  animationsEnabled: boolean;
  blurEnabled: boolean;
  shadowsEnabled: boolean;
  gpuEnabled: boolean;
  reducedMotion: boolean;

  // Actions
  updateSettings: (updates: Partial<GraphicsSettings>) => Promise<void>;
  applyProfile: (profile: PerformanceProfile) => Promise<void>;
  resetToDefaults: () => Promise<void>;
  detectOptimal: () => Promise<void>;
}

export function useGraphicsSettings(): UseGraphicsSettingsReturn {
  const [settings, setSettings] = useState<GraphicsSettings>(createDefaultGraphicsSettings());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  // Load settings on mount
  useEffect(() => {
    let mounted = true;

    async function load() {
      try {
        const loaded = await graphicsSettingsService.loadSettings();
        if (mounted) {
          setSettings(loaded);
          applyGraphicsSettings(loaded);
          setError(null);
        }
      } catch (err: unknown) {
        if (mounted) {
          setError(err instanceof Error ? err : new Error('Failed to load settings'));
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    load();

    // Subscribe to external changes
    const unsubscribe = graphicsSettingsService.subscribe((newSettings) => {
      if (mounted) {
        setSettings(newSettings);
        applyGraphicsSettings(newSettings);
      }
    });

    return () => {
      mounted = false;
      unsubscribe();
    };
  }, []);

  // Apply settings whenever they change
  useEffect(() => {
    if (!loading) {
      applyGraphicsSettings(settings);
    }
  }, [settings, loading]);

  const updateSettings = useCallback(
    async (updates: Partial<GraphicsSettings>) => {
      const newSettings = { ...settings, ...updates };
      setSettings(newSettings);
      await graphicsSettingsService.saveSettings(newSettings);
    },
    [settings]
  );

  const applyProfile = useCallback(async (profile: PerformanceProfile) => {
    const newSettings = await graphicsSettingsService.applyProfile(profile);
    setSettings(newSettings);
  }, []);

  const resetToDefaults = useCallback(async () => {
    await graphicsSettingsService.resetToDefaults();
    const newSettings = await graphicsSettingsService.loadSettings();
    setSettings(newSettings);
  }, []);

  const detectOptimal = useCallback(async () => {
    const newSettings = await graphicsSettingsService.detectOptimalSettings();
    setSettings(newSettings);
  }, []);

  // Computed convenience values
  const animationsEnabled = settings.effects.animations && !settings.accessibility.reducedMotion;
  const blurEnabled = settings.effects.blurEffects && settings.gpuAccelerationEnabled;
  const shadowsEnabled = settings.effects.shadows;
  const gpuEnabled = settings.gpuAccelerationEnabled;
  const reducedMotion = settings.accessibility.reducedMotion;

  return {
    settings,
    loading,
    error,
    animationsEnabled,
    blurEnabled,
    shadowsEnabled,
    gpuEnabled,
    reducedMotion,
    updateSettings,
    applyProfile,
    resetToDefaults,
    detectOptimal,
  };
}
