/**
 * Scaled UI Container Component
 *
 * Applies smart uniform scaling to maintain exact UI proportions
 * across all window sizes. The entire application scales uniformly
 * like a video in a media player.
 *
 * @module components/ScaledUIContainer
 */

import type { ReactNode } from 'react';
import { useUIScaleCSS } from '../hooks/useUIScale';

interface ScaledUIContainerProps {
  /** Child components to render with scaling applied */
  children: ReactNode;
  /** Base reference width for scaling calculations (default: 2208 - 15% larger than 1920) */
  baseWidth?: number;
  /** Base reference height for scaling calculations (default: 1242 - 15% larger than 1080) */
  baseHeight?: number;
  /** Whether to use contain (letterbox) or fill behavior (default: 'fill') */
  mode?: 'fill' | 'contain';
  /** Whether to enable scaling (default: true, set to false to disable) */
  enabled?: boolean;
}

/**
 * Container component that applies smart uniform UI scaling
 *
 * Wraps the entire application to provide automatic scaling based on window size.
 * Maintains exact layout proportions regardless of window dimensions.
 *
 * @example
 * ```tsx
 * <ScaledUIContainer>
 *   <App />
 * </ScaledUIContainer>
 * ```
 *
 * @example
 * ```tsx
 * // With custom configuration
 * <ScaledUIContainer baseWidth={2560} baseHeight={1440} mode="contain">
 *   <App />
 * </ScaledUIContainer>
 * ```
 */
export function ScaledUIContainer({
  children,
  baseWidth = 2208,
  baseHeight = 1242,
  mode = 'fill',
  enabled = true,
}: ScaledUIContainerProps) {
  // Apply UI scaling with CSS custom properties
  useUIScaleCSS({
    baseWidth,
    baseHeight,
    mode,
    debounceDelay: 150,
  });

  if (!enabled) {
    // If scaling is disabled, render children without wrapper
    return <>{children}</>;
  }

  return (
    <div
      className="scaled-ui-container"
      style={{
        transform: 'scale(var(--ui-scale))',
        transformOrigin: 'top left',
        width: 'var(--ui-scaled-width)',
        height: 'var(--ui-scaled-height)',
        overflow: 'hidden',
      }}
    >
      {children}
    </div>
  );
}
