/**
 * Density Context
 *
 * Provides intelligent density management based on display environment,
 * allowing users to override automatic density detection.
 */

import { createContext, useContext, useState, useEffect, useMemo, type ReactNode } from 'react';
import { useDisplayEnvironment } from '../hooks/useDisplayEnvironment';

/**
 * Available density modes
 */
export type Density = 'compact' | 'comfortable' | 'spacious';

/**
 * Context value providing density state and controls
 */
export interface DensityContextValue {
  /** Current active density (manual or auto-calculated) */
  density: Density;
  /** Set manual density override or 'auto' for automatic detection */
  setDensity: (density: Density | 'auto') => void;
  /** Whether automatic density detection is being used */
  isAuto: boolean;
  /** The automatically calculated density based on screen size */
  autoDensity: Density;
  /** Spacing scale multiplier based on current density */
  spacingScale: number;
}

const DensityContext = createContext<DensityContextValue | null>(null);

/**
 * LocalStorage key for persisting density preference
 */
const DENSITY_STORAGE_KEY = 'aura-density-preference';

interface DensityProviderProps {
  children: ReactNode;
}

/**
 * Density Provider Component
 *
 * Provides density management throughout the application via React Context.
 * Automatically detects optimal density based on screen size and allows
 * user override with persistence.
 *
 * @example
 * ```tsx
 * <DensityProvider>
 *   <App />
 * </DensityProvider>
 * ```
 */
export function DensityProvider({ children }: DensityProviderProps) {
  const display = useDisplayEnvironment();

  // Initialize from localStorage or default to 'auto'
  const [manualDensity, setManualDensity] = useState<Density | 'auto'>(() => {
    try {
      const saved = localStorage.getItem(DENSITY_STORAGE_KEY);
      if (saved && ['auto', 'compact', 'comfortable', 'spacious'].includes(saved)) {
        return saved as Density | 'auto';
      }
    } catch {
      // localStorage not available
    }
    return 'auto';
  });

  // Calculate auto density based on effective screen real estate
  const autoDensity = useMemo((): Density => {
    const { viewportWidth, viewportHeight, devicePixelRatio } = display;
    const effectiveArea = (viewportWidth * viewportHeight) / devicePixelRatio ** 2;

    // Calculate based on effective screen real estate
    if (effectiveArea < 800 * 600) {
      return 'compact';
    } else if (effectiveArea >= 1920 * 1080) {
      return 'spacious';
    }
    return 'comfortable';
  }, [display]);

  const density = manualDensity === 'auto' ? autoDensity : manualDensity;

  const spacingScale = useMemo(() => {
    switch (density) {
      case 'compact':
        return 0.75;
      case 'spacious':
        return 1.25;
      default:
        return 1;
    }
  }, [density]);

  // Persist preference to localStorage
  useEffect(() => {
    try {
      localStorage.setItem(DENSITY_STORAGE_KEY, manualDensity);
    } catch {
      // localStorage not available
    }
  }, [manualDensity]);

  // Apply density to document for CSS custom properties
  useEffect(() => {
    document.documentElement.dataset.density = density;
  }, [density]);

  const value: DensityContextValue = useMemo(
    () => ({
      density,
      setDensity: setManualDensity,
      isAuto: manualDensity === 'auto',
      autoDensity,
      spacingScale,
    }),
    [density, manualDensity, autoDensity, spacingScale]
  );

  return <DensityContext.Provider value={value}>{children}</DensityContext.Provider>;
}

/**
 * Hook to access density context
 *
 * @returns DensityContextValue with density state and controls
 * @throws Error if used outside DensityProvider
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { density, setDensity, isAuto } = useDensity();
 *
 *   return (
 *     <select value={isAuto ? 'auto' : density} onChange={(e) => setDensity(e.target.value as Density | 'auto')}>
 *       <option value="auto">Auto</option>
 *       <option value="compact">Compact</option>
 *       <option value="comfortable">Comfortable</option>
 *       <option value="spacious">Spacious</option>
 *     </select>
 *   );
 * }
 * ```
 */
export function useDensity(): DensityContextValue {
  const context = useContext(DensityContext);
  if (!context) {
    throw new Error('useDensity must be used within DensityProvider');
  }
  return context;
}
