/**
 * Custom hook for smart uniform UI scaling
 *
 * Provides automatic scaling of the entire UI to maintain proportions
 * across different window sizes. The UI scales uniformly like a video
 * in a media player, maintaining exact layout proportions.
 *
 * @module hooks/useUIScale
 */

import { useEffect, useState, useCallback } from 'react';

interface UIScaleConfig {
  /** Base reference width for scaling calculations */
  baseWidth: number;
  /** Base reference height for scaling calculations */
  baseHeight: number;
  /** Whether to use contain (letterbox/pillarbox) or fill behavior */
  mode: 'fill' | 'contain';
  /** Debounce delay for resize events in milliseconds */
  debounceDelay: number;
}

interface UIScaleResult {
  /** Current scale factor applied to the UI */
  scale: number;
  /** Scaled width after applying transform */
  scaledWidth: number;
  /** Scaled height after applying transform */
  scaledHeight: number;
  /** Current window width */
  windowWidth: number;
  /** Current window height */
  windowHeight: number;
}

const DEFAULT_CONFIG: UIScaleConfig = {
  baseWidth: 2208,
  baseHeight: 1242,
  mode: 'fill',
  debounceDelay: 150,
};

/**
 * Hook to manage smart uniform UI scaling
 *
 * Automatically calculates and applies scale factor based on window dimensions
 * relative to a base reference resolution.
 *
 * @param config - Optional configuration for scaling behavior
 * @returns Object containing scale factor and dimension information
 *
 * @example
 * ```tsx
 * function App() {
 *   const { scale, scaledWidth, scaledHeight } = useUIScale();
 *
 *   return (
 *     <div style={{
 *       transform: `scale(${scale})`,
 *       transformOrigin: 'top left',
 *       width: `${scaledWidth}px`,
 *       height: `${scaledHeight}px`
 *     }}>
 *       {children}
 *     </div>
 *   );
 * }
 * ```
 */
export function useUIScale(config: Partial<UIScaleConfig> = {}): UIScaleResult {
  const finalConfig = { ...DEFAULT_CONFIG, ...config };

  const calculateScale = useCallback((): UIScaleResult => {
    const windowWidth = window.innerWidth;
    const windowHeight = window.innerHeight;

    // Calculate scale factors for width and height
    const scaleX = windowWidth / finalConfig.baseWidth;
    const scaleY = windowHeight / finalConfig.baseHeight;

    // Use min for contain (letterbox/pillarbox) or max for fill behavior
    const scale =
      finalConfig.mode === 'contain' ? Math.min(scaleX, scaleY) : Math.max(scaleX, scaleY);

    // Calculate the scaled dimensions (inverse of scale to maintain base dimensions)
    const scaledWidth = windowWidth / scale;
    const scaledHeight = windowHeight / scale;

    return {
      scale,
      scaledWidth,
      scaledHeight,
      windowWidth,
      windowHeight,
    };
  }, [finalConfig.baseWidth, finalConfig.baseHeight, finalConfig.mode]);

  const [scaleInfo, setScaleInfo] = useState<UIScaleResult>(calculateScale);

  useEffect(() => {
    let timeoutId: number | undefined;

    const handleResize = () => {
      // Clear any pending timeout
      if (timeoutId !== undefined) {
        window.clearTimeout(timeoutId);
      }

      // Debounce the scale calculation
      timeoutId = window.setTimeout(() => {
        const newScaleInfo = calculateScale();
        setScaleInfo(newScaleInfo);
      }, finalConfig.debounceDelay);
    };

    // Add resize listener
    window.addEventListener('resize', handleResize);

    // Calculate initial scale
    const initialScale = calculateScale();
    setScaleInfo(initialScale);

    // Cleanup
    return () => {
      window.removeEventListener('resize', handleResize);
      if (timeoutId !== undefined) {
        window.clearTimeout(timeoutId);
      }
    };
  }, [calculateScale, finalConfig.debounceDelay]);

  return scaleInfo;
}

/**
 * Hook to apply UI scaling directly to CSS custom properties
 *
 * Sets --ui-scale, --ui-scaled-width, and --ui-scaled-height CSS variables
 * on the document root for use in stylesheets.
 *
 * @param config - Optional configuration for scaling behavior
 * @returns Object containing scale factor and dimension information
 *
 * @example
 * ```tsx
 * function App() {
 *   useUIScaleCSS();
 *
 *   // CSS can now use:
 *   // transform: scale(var(--ui-scale));
 *   // width: calc(100% / var(--ui-scale));
 * }
 * ```
 */
export function useUIScaleCSS(config: Partial<UIScaleConfig> = {}): UIScaleResult {
  const scaleInfo = useUIScale(config);

  useEffect(() => {
    const root = document.documentElement;

    // Set CSS custom properties
    root.style.setProperty('--ui-scale', scaleInfo.scale.toString());
    root.style.setProperty('--ui-scaled-width', `${scaleInfo.scaledWidth}px`);
    root.style.setProperty('--ui-scaled-height', `${scaleInfo.scaledHeight}px`);

    return () => {
      // Cleanup: remove custom properties
      root.style.removeProperty('--ui-scale');
      root.style.removeProperty('--ui-scaled-width');
      root.style.removeProperty('--ui-scaled-height');
    };
  }, [scaleInfo.scale, scaleInfo.scaledWidth, scaleInfo.scaledHeight]);

  return scaleInfo;
}
