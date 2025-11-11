import { useEffect, useState, useCallback } from 'react';
import {
  isWindows,
  isWindows11,
  getSystemThemePreference,
  onSystemThemeChange,
  getDPIScaleInfo,
  supportsSnapLayouts,
  type DPIScaleInfo,
} from '../utils/windowsUtils';

/**
 * Custom hook for Windows-specific UI features and integrations
 */
export function useWindowsNativeUI() {
  const [dpiInfo, setDpiInfo] = useState<DPIScaleInfo>(() => getDPIScaleInfo());
  const [systemTheme, setSystemTheme] = useState<'light' | 'dark'>(() =>
    getSystemThemePreference()
  );
  const [windowsVersion] = useState({
    isWindows: isWindows(),
    isWindows11: isWindows11(),
  });

  // Listen for DPI changes (when user changes display scaling)
  useEffect(() => {
    const handleResize = () => {
      const newDpiInfo = getDPIScaleInfo();
      setDpiInfo(newDpiInfo);
    };

    // Monitor for window resize which can indicate DPI change
    window.addEventListener('resize', handleResize);

    // Also listen for orientationchange (mobile/tablet)
    window.addEventListener('orientationchange', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('orientationchange', handleResize);
    };
  }, []);

  // Listen for system theme changes
  useEffect(() => {
    return onSystemThemeChange((theme) => {
      setSystemTheme(theme);
    });
  }, []);

  // Apply Windows 11 specific styles
  useEffect(() => {
    if (windowsVersion.isWindows11) {
      document.body.classList.add('windows-11');
    } else if (windowsVersion.isWindows) {
      document.body.classList.add('windows');
    }

    return () => {
      document.body.classList.remove('windows-11', 'windows');
    };
  }, [windowsVersion]);

  // Apply DPI scale class for responsive adjustments
  useEffect(() => {
    const classList = document.body.classList;

    // Remove existing DPI classes
    classList.remove('dpi-normal', 'dpi-medium', 'dpi-high', 'dpi-very-high');

    // Add current DPI class
    classList.add(`dpi-${dpiInfo.scaleCategory}`);

    return () => {
      classList.remove('dpi-normal', 'dpi-medium', 'dpi-high', 'dpi-very-high');
    };
  }, [dpiInfo.scaleCategory]);

  // Configure viewport for DPI awareness
  useEffect(() => {
    const viewport = document.querySelector('meta[name="viewport"]');
    if (viewport) {
      const currentContent = viewport.getAttribute('content') || '';

      // Ensure viewport is DPI-aware
      if (!currentContent.includes('width=device-width')) {
        viewport.setAttribute(
          'content',
          'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes'
        );
      }
    }
  }, []);

  const syncThemeWithSystem = useCallback(
    (onThemeChange: (theme: 'light' | 'dark') => void) => {
      onThemeChange(systemTheme);
    },
    [systemTheme]
  );

  return {
    isWindows: windowsVersion.isWindows,
    isWindows11: windowsVersion.isWindows11,
    dpiInfo,
    systemTheme,
    supportsSnapLayouts: supportsSnapLayouts(),
    syncThemeWithSystem,
  };
}
