/**
 * Apple-level Display Environment Detection Hook
 *
 * Provides comprehensive display information for adaptive layouts,
 * including semantic size classes, density classification, and
 * practical layout metrics.
 */

import { useState, useEffect, useCallback, useMemo } from 'react';

/**
 * Semantic size class following Apple's Human Interface Guidelines
 */
export type SizeClass = 'compact' | 'regular' | 'expanded';

/**
 * Display density classification
 */
export type DensityClass = 'low' | 'standard' | 'high' | 'ultra';

/**
 * Aspect ratio classification
 */
export type AspectRatioClass = 'portrait' | 'square' | 'landscape' | 'ultrawide';

/**
 * Recommended panel layout based on available space
 */
export type PanelLayout = 'stacked' | 'side-by-side' | 'three-panel';

/**
 * Comprehensive display environment information
 */
export interface DisplayEnvironment {
  // Physical display information
  screenWidth: number;
  screenHeight: number;
  viewportWidth: number;
  viewportHeight: number;
  devicePixelRatio: number;

  // Computed classifications
  sizeClass: SizeClass;
  densityClass: DensityClass;
  aspectRatio: AspectRatioClass;

  // Practical metrics
  effectiveWidth: number;
  contentColumns: 1 | 2 | 3 | 4;
  panelLayout: PanelLayout;

  // Derived spacing scale
  baseSpacing: number;
  baseFontSize: number;

  // Feature flags
  canShowSecondaryPanels: boolean;
  canShowDetailInspector: boolean;
  preferCompactControls: boolean;
  enableTouchOptimizations: boolean;
}

/**
 * Breakpoints for size class determination
 */
const SIZE_CLASS_BREAKPOINTS = {
  compact: 1024, // Below this is compact
  regular: 1920, // Below this is regular, at or above is expanded
} as const;

/**
 * Breakpoints for column count determination
 */
const COLUMN_BREAKPOINTS = {
  one: 640, // Below this is 1 column
  two: 1024, // Below this is 2 columns
  three: 1920, // Below this is 3 columns
  // 4 columns at 1920+
} as const;

/**
 * Calculate size class based on effective viewport width
 */
function calculateSizeClass(effectiveWidth: number): SizeClass {
  if (effectiveWidth < SIZE_CLASS_BREAKPOINTS.compact) {
    return 'compact';
  }
  if (effectiveWidth < SIZE_CLASS_BREAKPOINTS.regular) {
    return 'regular';
  }
  return 'expanded';
}

/**
 * Calculate density class based on DPI and effective resolution
 */
function calculateDensityClass(dpr: number, effectiveWidth: number): DensityClass {
  // Ultra: 4K+ at 100% scale or 1440p+ at 150%+
  if (effectiveWidth >= 2560 || (effectiveWidth >= 1920 && dpr >= 2)) {
    return 'ultra';
  }
  // High: 1440p at 100% or 1080p at 150%+
  if (effectiveWidth >= 1920 || (effectiveWidth >= 1280 && dpr >= 1.5)) {
    return 'high';
  }
  // Standard: 1080p at 100%
  if (effectiveWidth >= 1280) {
    return 'standard';
  }
  // Low: Below 1280 effective width
  return 'low';
}

/**
 * Calculate aspect ratio classification
 */
function calculateAspectRatio(width: number, height: number): AspectRatioClass {
  const ratio = width / height;

  if (ratio < 0.75) {
    return 'portrait';
  }
  if (ratio < 1.2) {
    return 'square';
  }
  if (ratio < 2.1) {
    return 'landscape';
  }
  return 'ultrawide';
}

/**
 * Calculate recommended content columns
 */
function calculateContentColumns(effectiveWidth: number): 1 | 2 | 3 | 4 {
  if (effectiveWidth < COLUMN_BREAKPOINTS.one) {
    return 1;
  }
  if (effectiveWidth < COLUMN_BREAKPOINTS.two) {
    return 1;
  }
  if (effectiveWidth < COLUMN_BREAKPOINTS.three) {
    return 2;
  }
  if (effectiveWidth < 2560) {
    return 3;
  }
  return 4;
}

/**
 * Calculate recommended panel layout
 */
function calculatePanelLayout(sizeClass: SizeClass, aspectRatio: AspectRatioClass): PanelLayout {
  if (sizeClass === 'compact') {
    return 'stacked';
  }
  if (sizeClass === 'regular') {
    return 'side-by-side';
  }
  // Expanded mode
  if (aspectRatio === 'ultrawide') {
    return 'three-panel';
  }
  return 'side-by-side';
}

/**
 * Calculate base spacing unit (in pixels)
 */
function calculateBaseSpacing(densityClass: DensityClass): number {
  switch (densityClass) {
    case 'ultra':
      return 8;
    case 'high':
      return 6;
    case 'standard':
      return 5;
    case 'low':
      return 4;
  }
}

/**
 * Calculate base font size (in pixels)
 */
function calculateBaseFontSize(densityClass: DensityClass, viewportHeight: number): number {
  // Start with density-based sizing
  let baseSize: number;
  switch (densityClass) {
    case 'ultra':
      baseSize = 16;
      break;
    case 'high':
      baseSize = 15;
      break;
    case 'standard':
      baseSize = 14;
      break;
    case 'low':
      baseSize = 13;
      break;
  }

  // Adjust for very small viewport heights
  if (viewportHeight < 720) {
    baseSize = Math.max(12, baseSize - 1);
  }

  return baseSize;
}

/**
 * Debounce utility for resize events
 */
function debounce<T extends (...args: unknown[]) => unknown>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout>;
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

/**
 * Custom hook for Apple-level display environment detection
 *
 * Provides comprehensive display information including semantic size classes,
 * density classification, and practical layout metrics that update on resize.
 *
 * @returns DisplayEnvironment object with all computed display metrics
 *
 * @example
 * ```tsx
 * const display = useDisplayEnvironment();
 *
 * // Conditionally render based on size class
 * if (display.sizeClass === 'compact') {
 *   return <MobileLayout />;
 * }
 *
 * // Use column count for grids
 * <Grid columns={display.contentColumns}>...</Grid>
 * ```
 */
export function useDisplayEnvironment(): DisplayEnvironment {
  const calculateEnvironment = useCallback((): DisplayEnvironment => {
    const screenWidth = window.screen.width;
    const screenHeight = window.screen.height;
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const devicePixelRatio = window.devicePixelRatio || 1;

    // Effective width considers DPI scaling
    const effectiveWidth = Math.round(viewportWidth);

    const sizeClass = calculateSizeClass(effectiveWidth);
    const densityClass = calculateDensityClass(devicePixelRatio, effectiveWidth);
    const aspectRatio = calculateAspectRatio(viewportWidth, viewportHeight);
    const contentColumns = calculateContentColumns(effectiveWidth);
    const panelLayout = calculatePanelLayout(sizeClass, aspectRatio);
    const baseSpacing = calculateBaseSpacing(densityClass);
    const baseFontSize = calculateBaseFontSize(densityClass, viewportHeight);

    return {
      // Physical display information
      screenWidth,
      screenHeight,
      viewportWidth,
      viewportHeight,
      devicePixelRatio,

      // Computed classifications
      sizeClass,
      densityClass,
      aspectRatio,

      // Practical metrics
      effectiveWidth,
      contentColumns,
      panelLayout,

      // Derived spacing scale
      baseSpacing,
      baseFontSize,

      // Feature flags
      canShowSecondaryPanels: sizeClass !== 'compact',
      canShowDetailInspector:
        sizeClass === 'expanded' || (sizeClass === 'regular' && effectiveWidth >= 1440),
      preferCompactControls: sizeClass === 'compact' || viewportHeight < 800,
      enableTouchOptimizations:
        'ontouchstart' in window || navigator.maxTouchPoints > 0 || sizeClass === 'compact',
    };
  }, []);

  const [environment, setEnvironment] = useState<DisplayEnvironment>(calculateEnvironment);

  useEffect(() => {
    const handleResize = debounce(() => {
      setEnvironment(calculateEnvironment());
    }, 100); // 100ms debounce for smooth performance

    window.addEventListener('resize', handleResize);

    // Also listen for orientation changes (mobile/tablet)
    window.addEventListener('orientationchange', handleResize);

    // Handle DPI changes (display settings change)
    // Check if matchMedia is available (not available in JSDOM/test environments)
    let mediaQuery: MediaQueryList | null = null;
    const handleDpiChange = () => {
      setEnvironment(calculateEnvironment());
    };

    if (typeof window.matchMedia === 'function') {
      try {
        mediaQuery = window.matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`);
        if (mediaQuery.addEventListener) {
          mediaQuery.addEventListener('change', handleDpiChange);
        }
      } catch {
        // matchMedia not supported or query invalid - gracefully degrade
      }
    }

    return () => {
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('orientationchange', handleResize);
      if (mediaQuery && mediaQuery.removeEventListener) {
        mediaQuery.removeEventListener('change', handleDpiChange);
      }
    };
  }, [calculateEnvironment]);

  return environment;
}

/**
 * Apply display environment CSS custom properties to the document root
 */
export function applyDisplayEnvironmentProperties(env: DisplayEnvironment): void {
  const root = document.documentElement;

  // Size class
  root.setAttribute('data-size-class', env.sizeClass);
  root.setAttribute('data-density-class', env.densityClass);
  root.setAttribute('data-aspect-ratio', env.aspectRatio);

  // CSS custom properties for adaptive layouts
  root.style.setProperty('--adaptive-base-spacing', `${env.baseSpacing}px`);
  root.style.setProperty('--adaptive-base-font-size', `${env.baseFontSize}px`);
  root.style.setProperty('--adaptive-content-columns', String(env.contentColumns));
  root.style.setProperty('--adaptive-viewport-width', `${env.viewportWidth}px`);
  root.style.setProperty('--adaptive-viewport-height', `${env.viewportHeight}px`);
  root.style.setProperty('--adaptive-dpr', String(env.devicePixelRatio));
}

/**
 * Hook that applies display environment properties to the document
 */
export function useApplyDisplayEnvironment(): DisplayEnvironment {
  const environment = useDisplayEnvironment();

  useEffect(() => {
    applyDisplayEnvironmentProperties(environment);
  }, [environment]);

  return environment;
}

/**
 * Utility hook to get computed values based on display environment
 */
export function useAdaptiveValue<T>(compactValue: T, regularValue: T, expandedValue: T): T {
  const { sizeClass } = useDisplayEnvironment();

  return useMemo(() => {
    switch (sizeClass) {
      case 'compact':
        return compactValue;
      case 'regular':
        return regularValue;
      case 'expanded':
        return expandedValue;
    }
  }, [sizeClass, compactValue, regularValue, expandedValue]);
}
