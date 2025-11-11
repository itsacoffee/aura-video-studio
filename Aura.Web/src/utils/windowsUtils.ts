/**
 * Windows-specific utility functions for native integration
 */

/**
 * Detects if the application is running on Windows
 */
export function isWindows(): boolean {
  return navigator.platform.toLowerCase().includes('win');
}

/**
 * Detects Windows 11 specifically (requires User-Agent parsing)
 * Note: Windows 11 identifies as Windows 10 in User-Agent, so this is approximate
 */
export function isWindows11(): boolean {
  if (!isWindows()) return false;

  const ua = navigator.userAgent.toLowerCase();
  // Windows 11 still reports as Windows NT 10.0, but typically has higher build numbers
  // This is a best-effort detection
  return ua.includes('windows nt 10.0') || ua.includes('windows nt 11.0');
}

/**
 * Gets the current device pixel ratio for DPI scaling
 */
export function getDevicePixelRatio(): number {
  return window.devicePixelRatio || 1;
}

/**
 * Checks if the device is using high-DPI (typically 150% scaling or higher)
 */
export function isHighDPI(): boolean {
  return getDevicePixelRatio() >= 1.5;
}

/**
 * Converts CSS pixels to physical pixels based on DPI
 */
export function cssToPhysicalPixels(cssPixels: number): number {
  return Math.round(cssPixels * getDevicePixelRatio());
}

/**
 * Converts physical pixels to CSS pixels based on DPI
 */
export function physicalToCSSPixels(physicalPixels: number): number {
  return Math.round(physicalPixels / getDevicePixelRatio());
}

/**
 * Detects the system theme preference (light or dark)
 */
export function getSystemThemePreference(): 'light' | 'dark' {
  if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    return 'dark';
  }
  return 'light';
}

/**
 * Listens for system theme changes
 */
export function onSystemThemeChange(callback: (theme: 'light' | 'dark') => void): () => void {
  if (!window.matchMedia) {
    return () => {}; // No-op cleanup
  }

  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  const handler = (e: MediaQueryListEvent) => {
    callback(e.matches ? 'dark' : 'light');
  };

  // Modern browsers
  if (mediaQuery.addEventListener) {
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }

  // Fallback for older browsers
  mediaQuery.addListener(handler);
  return () => mediaQuery.removeListener(handler);
}

/**
 * Gets the current DPI scaling percentage
 */
export function getDPIScalingPercentage(): number {
  const dpr = getDevicePixelRatio();
  return Math.round(dpr * 100);
}

/**
 * Applies DPI-aware sizing to an element
 */
export function applyDPIAwareSizing(element: HTMLElement, baseSizePx: number): void {
  const scaledSize = cssToPhysicalPixels(baseSizePx);
  element.style.width = `${scaledSize}px`;
  element.style.height = `${scaledSize}px`;
}

/**
 * Detects if Windows snap layouts are supported
 * This checks for the presence of the maximize button handler
 */
export function supportsSnapLayouts(): boolean {
  if (!isWindows11()) return false;

  return 'chrome' in window;
}

/**
 * Interface for DPI scale information
 */
export interface DPIScaleInfo {
  ratio: number;
  percentage: number;
  isHighDPI: boolean;
  scaleCategory: 'normal' | 'medium' | 'high' | 'very-high';
}

/**
 * Gets comprehensive DPI scaling information
 */
export function getDPIScaleInfo(): DPIScaleInfo {
  const ratio = getDevicePixelRatio();
  const percentage = Math.round(ratio * 100);

  let scaleCategory: DPIScaleInfo['scaleCategory'] = 'normal';
  if (ratio >= 3) {
    scaleCategory = 'very-high';
  } else if (ratio >= 2) {
    scaleCategory = 'high';
  } else if (ratio >= 1.5) {
    scaleCategory = 'medium';
  }

  return {
    ratio,
    percentage,
    isHighDPI: ratio >= 1.5,
    scaleCategory,
  };
}

/**
 * Logs Windows environment information for debugging
 */
export function logWindowsEnvironment(): void {
  const dpiInfo = getDPIScaleInfo();
  const theme = getSystemThemePreference();

  console.log('[Windows Environment]', {
    platform: navigator.platform,
    userAgent: navigator.userAgent,
    isWindows: isWindows(),
    isWindows11: isWindows11(),
    dpiRatio: dpiInfo.ratio,
    dpiPercentage: `${dpiInfo.percentage}%`,
    dpiCategory: dpiInfo.scaleCategory,
    isHighDPI: dpiInfo.isHighDPI,
    systemTheme: theme,
    supportsSnapLayouts: supportsSnapLayouts(),
    screenSize: {
      width: window.screen.width,
      height: window.screen.height,
      availWidth: window.screen.availWidth,
      availHeight: window.screen.availHeight,
    },
    viewport: {
      width: window.innerWidth,
      height: window.innerHeight,
    },
  });
}
