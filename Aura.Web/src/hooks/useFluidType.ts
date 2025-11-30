/**
 * useFluidType Hook
 *
 * A React hook that provides fluid typography configuration based on
 * display environment. Uses the modular scale system with viewport-responsive
 * adjustments for optimal readability across devices.
 *
 * Features:
 * - Dynamic scale ratio based on viewport width
 * - Base size adjustment based on display density
 * - Helper function for generating fluid clamp() values
 * - Integration with useDisplayEnvironment hook
 *
 * @example
 * ```tsx
 * const { scale, config, getFluidSize } = useFluidType();
 *
 * // Use scale values
 * console.log(scale.md); // "16px"
 *
 * // Generate fluid size
 * const customSize = getFluidSize(14, 18);
 * // Returns: "clamp(14px, 12.5714px + 0.3571vw, 18px)"
 * ```
 */

import { useMemo } from 'react';
import { useDisplayEnvironment } from './useDisplayEnvironment';

/**
 * Configuration for the fluid type system
 */
export interface FluidTypeConfig {
  /** Base font size in pixels */
  baseSize: number;
  /** Modular scale ratio */
  scaleRatio: number;
  /** Minimum viewport for scaling (in pixels) */
  minViewport: number;
  /** Maximum viewport for scaling (in pixels) */
  maxViewport: number;
}

/**
 * Type scale sizes in pixel strings
 */
export interface FluidTypeScale {
  /** Extra small - captions, metadata */
  xs: string;
  /** Small - secondary content, labels */
  sm: string;
  /** Medium - base body text */
  md: string;
  /** Large - emphasized text */
  lg: string;
  /** Extra large - small headings */
  xl: string;
  /** 2XL - medium headings */
  '2xl': string;
  /** 3XL - large headings */
  '3xl': string;
  /** 4XL - display/hero text */
  '4xl': string;
}

/**
 * Return type for the useFluidType hook
 */
export interface UseFluidTypeReturn {
  /** Calculated type scale with pixel values */
  scale: FluidTypeScale;
  /** Current configuration values */
  config: FluidTypeConfig;
  /** Function to generate fluid clamp() CSS value */
  getFluidSize: (minPx: number, maxPx: number) => string;
}

/**
 * Custom hook for fluid typography based on display environment.
 *
 * Provides a dynamic type scale and configuration that adapts to viewport
 * size and display density. Uses a modular scale approach where each size
 * is a mathematical ratio of the previous.
 *
 * @returns Object containing scale, config, and getFluidSize helper
 */
export function useFluidType(): UseFluidTypeReturn {
  const display = useDisplayEnvironment();

  const config = useMemo((): FluidTypeConfig => {
    // Adjust scale ratio based on viewport
    let scaleRatio = 1.2; // Minor Third (default)

    if (display.viewportWidth >= 2560) {
      scaleRatio = 1.333; // Perfect Fourth
    } else if (display.viewportWidth >= 1920) {
      scaleRatio = 1.25; // Major Third
    } else if (display.viewportWidth < 768) {
      scaleRatio = 1.125; // Major Second
    }

    // Adjust base size based on display density
    let baseSize = 16;
    if (display.densityClass === 'low') {
      baseSize = 14;
    } else if (display.densityClass === 'ultra') {
      baseSize = 18;
    }

    return {
      baseSize,
      scaleRatio,
      minViewport: 375,
      maxViewport: 2560,
    };
  }, [display.viewportWidth, display.densityClass]);

  const scale = useMemo((): FluidTypeScale => {
    const { baseSize, scaleRatio } = config;

    return {
      xs: `${(baseSize / scaleRatio).toFixed(2)}px`,
      sm: `${(baseSize / 1.1).toFixed(2)}px`,
      md: `${baseSize.toFixed(2)}px`,
      lg: `${(baseSize * scaleRatio).toFixed(2)}px`,
      xl: `${(baseSize * Math.pow(scaleRatio, 2)).toFixed(2)}px`,
      '2xl': `${(baseSize * Math.pow(scaleRatio, 3)).toFixed(2)}px`,
      '3xl': `${(baseSize * Math.pow(scaleRatio, 4)).toFixed(2)}px`,
      '4xl': `${(baseSize * Math.pow(scaleRatio, 5)).toFixed(2)}px`,
    };
  }, [config]);

  /**
   * Generate a fluid size using CSS clamp()
   *
   * @param minPx - Minimum size in pixels
   * @param maxPx - Maximum size in pixels
   * @returns CSS clamp() function string for fluid sizing
   *
   * @example
   * ```ts
   * const size = getFluidSize(14, 18);
   * // Returns: "clamp(14px, 12.5714px + 0.3571vw, 18px)"
   * ```
   */
  const getFluidSize = (minPx: number, maxPx: number): string => {
    const { minViewport, maxViewport } = config;
    const slope = (maxPx - minPx) / (maxViewport - minViewport);
    const yAxisIntersection = minPx - slope * minViewport;

    return `clamp(${minPx}px, ${yAxisIntersection.toFixed(4)}px + ${(slope * 100).toFixed(4)}vw, ${maxPx}px)`;
  };

  return { scale, config, getFluidSize };
}

export default useFluidType;
