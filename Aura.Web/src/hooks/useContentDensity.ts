/**
 * Content Density Hook
 *
 * Provides intelligent content density management based on display environment,
 * allowing users to override automatic density detection.
 */

import { useState, useMemo, useCallback, useEffect } from 'react';
import { useDisplayEnvironment } from './useDisplayEnvironment';

/**
 * Content density levels
 */
export type ContentDensity = 'compact' | 'comfortable' | 'spacious';

/**
 * Content density settings and controls
 */
export interface ContentDensityState {
  /** Current active density (manual or auto) */
  density: ContentDensity;
  /** Set manual density override */
  setDensity: (density: ContentDensity | 'auto') => void;
  /** Automatically calculated density based on screen */
  autoDensity: ContentDensity;
  /** Whether auto-density is being used */
  isAuto: boolean;
  /** Reset to auto-density */
  resetToAuto: () => void;
  /** CSS class name for the current density */
  densityClass: string;
  /** Spacing multiplier based on density */
  spacingMultiplier: number;
  /** Font size adjustment based on density */
  fontSizeAdjustment: number;
}

/**
 * LocalStorage key for persisting density preference
 */
const DENSITY_STORAGE_KEY = 'aura-content-density';

/**
 * Calculate automatic density based on display environment
 */
function calculateAutoDensity(viewportWidth: number, viewportHeight: number): ContentDensity {
  // Compact: Small screens or limited vertical space
  if (viewportHeight < 800 || viewportWidth < 1024) {
    return 'compact';
  }

  // Spacious: Large screens with ample space
  if (viewportWidth >= 1920 && viewportHeight >= 1080) {
    return 'spacious';
  }

  // Comfortable: Standard desktop screens
  return 'comfortable';
}

/**
 * Get spacing multiplier for density
 */
function getSpacingMultiplier(density: ContentDensity): number {
  switch (density) {
    case 'compact':
      return 0.75;
    case 'comfortable':
      return 1;
    case 'spacious':
      return 1.25;
  }
}

/**
 * Get font size adjustment for density (in pixels)
 */
function getFontSizeAdjustment(density: ContentDensity): number {
  switch (density) {
    case 'compact':
      return -1;
    case 'comfortable':
      return 0;
    case 'spacious':
      return 1;
  }
}

/**
 * Custom hook for intelligent content density management
 *
 * Provides automatic density detection based on display environment,
 * with user override capability and persistence.
 *
 * @returns ContentDensityState object with density value and controls
 *
 * @example
 * ```tsx
 * const { density, setDensity, isAuto } = useContentDensity();
 *
 * // Apply density-based styling
 * <div style={{ padding: `${density === 'compact' ? '8px' : '16px'}` }}>
 *   ...
 * </div>
 *
 * // Density selector
 * <Select value={isAuto ? 'auto' : density} onChange={setDensity}>
 *   <option value="auto">Auto</option>
 *   <option value="compact">Compact</option>
 *   <option value="comfortable">Comfortable</option>
 *   <option value="spacious">Spacious</option>
 * </Select>
 * ```
 */
export function useContentDensity(): ContentDensityState {
  const { viewportWidth, viewportHeight } = useDisplayEnvironment();

  // Initialize from localStorage or default to 'auto'
  const [manualDensity, setManualDensity] = useState<ContentDensity | 'auto'>(() => {
    try {
      const saved = localStorage.getItem(DENSITY_STORAGE_KEY);
      if (saved && ['auto', 'compact', 'comfortable', 'spacious'].includes(saved)) {
        return saved as ContentDensity | 'auto';
      }
    } catch {
      // localStorage not available
    }
    return 'auto';
  });

  // Calculate auto density based on display environment
  const autoDensity = useMemo(
    () => calculateAutoDensity(viewportWidth, viewportHeight),
    [viewportWidth, viewportHeight]
  );

  // Current effective density
  const density = useMemo(
    () => (manualDensity === 'auto' ? autoDensity : manualDensity),
    [manualDensity, autoDensity]
  );

  // Persist preference to localStorage
  useEffect(() => {
    try {
      localStorage.setItem(DENSITY_STORAGE_KEY, manualDensity);
    } catch {
      // localStorage not available
    }
  }, [manualDensity]);

  // Apply density data attribute to document root
  useEffect(() => {
    document.documentElement.setAttribute('data-content-density', density);
    document.documentElement.style.setProperty(
      '--content-density-multiplier',
      String(getSpacingMultiplier(density))
    );
  }, [density]);

  const setDensity = useCallback((newDensity: ContentDensity | 'auto') => {
    setManualDensity(newDensity);
  }, []);

  const resetToAuto = useCallback(() => {
    setManualDensity('auto');
  }, []);

  return {
    density,
    setDensity,
    autoDensity,
    isAuto: manualDensity === 'auto',
    resetToAuto,
    densityClass: `density-${density}`,
    spacingMultiplier: getSpacingMultiplier(density),
    fontSizeAdjustment: getFontSizeAdjustment(density),
  };
}

/**
 * Apply content density CSS classes and properties
 */
export function applyContentDensityProperties(density: ContentDensity): void {
  const root = document.documentElement;

  // Remove existing density classes
  root.classList.remove('density-compact', 'density-comfortable', 'density-spacious');

  // Add current density class
  root.classList.add(`density-${density}`);

  // Set CSS custom properties
  root.style.setProperty('--content-density', density);
  root.style.setProperty('--density-spacing-multiplier', String(getSpacingMultiplier(density)));
  root.style.setProperty('--density-font-adjustment', `${getFontSizeAdjustment(density)}px`);
}
