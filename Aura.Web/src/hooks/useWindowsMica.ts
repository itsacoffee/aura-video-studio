/**
 * useWindowsMica Hook
 * Access Windows 11 Mica functionality from React
 */

import { useEffect, useState, useCallback, useRef } from 'react';
import type { WindowMaterial, DisplayInfo } from '../types/electron.d';

interface MicaState {
  isSupported: boolean;
  isElectron: boolean;
  currentMaterial: WindowMaterial;
  accentColor: string | null;
  scaleFactor: number;
  displays: DisplayInfo[];
  isDarkMode: boolean;
}

interface UseWindowsMicaReturn extends MicaState {
  setMaterial: (material: WindowMaterial) => Promise<boolean>;
  syncWithGraphicsSettings: (settings: {
    transparency: boolean;
    blurEffects: boolean;
  }) => Promise<void>;
}

// Check if running in Electron with graphics API
function checkIsElectron(): boolean {
  return (
    typeof window !== 'undefined' && window.aura !== undefined && window.aura.graphics !== undefined
  );
}

export function useWindowsMica(): UseWindowsMicaReturn {
  const isElectron = useRef(checkIsElectron()).current;

  const [state, setState] = useState<MicaState>({
    isSupported: false,
    isElectron,
    currentMaterial: 'none',
    accentColor: null,
    scaleFactor: window.devicePixelRatio || 1,
    displays: [],
    isDarkMode: window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false,
  });

  useEffect(() => {
    if (!isElectron) return;

    let mounted = true;
    const cleanupFunctions: (() => void)[] = [];

    async function init() {
      try {
        const api = window.aura!.graphics;

        // Fetch initial state in parallel
        const [materialInfo, accentColor, dpiInfo, displays] = await Promise.all([
          api.getMaterial(),
          api.getAccentColor(),
          api.getDpiInfo(),
          api.getAllDisplays(),
        ]);

        if (!mounted) return;

        // Format accent color with # prefix if not already present
        const formatAccentColor = (color: string | null): string | null => {
          if (!color) return null;
          return color.startsWith('#') ? color : `#${color}`;
        };

        setState((prev) => ({
          ...prev,
          isSupported: materialInfo.supported,
          currentMaterial: materialInfo.current,
          accentColor: formatAccentColor(accentColor),
          scaleFactor: dpiInfo.scaleFactor,
          displays,
        }));

        // Subscribe to theme changes
        const unsubTheme = api.onThemeChange((data) => {
          if (mounted) {
            setState((prev) => ({ ...prev, isDarkMode: data.isDark }));
          }
        });
        cleanupFunctions.push(unsubTheme);

        // Subscribe to accent color changes
        const unsubAccent = api.onAccentColorChange((data) => {
          if (mounted) {
            // The color from accent color change events may already have #
            setState((prev) => ({ ...prev, accentColor: formatAccentColor(data.color) }));
          }
        });
        cleanupFunctions.push(unsubAccent);
      } catch (error) {
        console.warn('[Mica] Failed to initialize:', error);
      }
    }

    init();

    return () => {
      mounted = false;
      cleanupFunctions.forEach((fn) => fn());
    };
  }, [isElectron]);

  const setMaterial = useCallback(
    async (material: WindowMaterial): Promise<boolean> => {
      if (!isElectron) return false;

      try {
        const success = await window.aura!.graphics.setMaterial(material);

        if (success) {
          setState((prev) => ({ ...prev, currentMaterial: material }));
        }

        return success;
      } catch (error) {
        console.error('[Mica] Failed to set material:', error);
        return false;
      }
    },
    [isElectron]
  );

  const syncWithGraphicsSettings = useCallback(
    async (settings: { transparency: boolean; blurEffects: boolean }) => {
      if (!isElectron) return;

      try {
        const result = await window.aura!.graphics.applySettings(settings);

        if (result.success) {
          // Refresh material state
          const materialInfo = await window.aura!.graphics.getMaterial();
          setState((prev) => ({ ...prev, currentMaterial: materialInfo.current }));
        }
      } catch (error) {
        console.error('[Mica] Failed to sync settings:', error);
      }
    },
    [isElectron]
  );

  return {
    ...state,
    setMaterial,
    syncWithGraphicsSettings,
  };
}
