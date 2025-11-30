/**
 * Adaptive Layout Context
 *
 * Provides adaptive layout configuration throughout the application
 * via React Context. This enables consistent responsive behavior
 * across all components without prop drilling.
 */

import React, { createContext, useContext, useMemo, useEffect, type ReactNode } from 'react';
import {
  useDisplayEnvironment,
  applyDisplayEnvironmentProperties,
  type DisplayEnvironment,
} from '../hooks/useDisplayEnvironment';
import {
  useContentDensity,
  applyContentDensityProperties,
  type ContentDensity,
} from '../hooks/useContentDensity';
import {
  calculateLayoutConfig,
  type AdaptiveLayoutConfig,
  type SidebarConfig,
  type ContentConfig,
  type InspectorConfig,
  type GridConfig,
  type TimelineConfig,
  type TypographyConfig,
} from '../hooks/useAdaptiveLayout';

/**
 * Extended context value including both config and display state
 */
export interface AdaptiveLayoutContextValue extends AdaptiveLayoutConfig {
  /** Current content density setting */
  contentDensity: ContentDensity;
  /** Whether using automatic density detection */
  isAutoDensity: boolean;
  /** Set content density preference */
  setContentDensity: (density: ContentDensity | 'auto') => void;
  /** Reset to automatic density */
  resetDensity: () => void;
}

/**
 * Default context value for SSR or when provider is not mounted
 */
const defaultDisplayEnvironment: DisplayEnvironment = {
  screenWidth: 1920,
  screenHeight: 1080,
  viewportWidth: 1920,
  viewportHeight: 1080,
  devicePixelRatio: 1,
  sizeClass: 'regular',
  densityClass: 'standard',
  aspectRatio: 'landscape',
  effectiveWidth: 1920,
  contentColumns: 3,
  panelLayout: 'side-by-side',
  baseSpacing: 5,
  baseFontSize: 14,
  canShowSecondaryPanels: true,
  canShowDetailInspector: true,
  preferCompactControls: false,
  enableTouchOptimizations: false,
};

const defaultConfig = calculateLayoutConfig(defaultDisplayEnvironment);

const defaultContextValue: AdaptiveLayoutContextValue = {
  ...defaultConfig,
  contentDensity: 'comfortable',
  isAutoDensity: true,
  setContentDensity: () => {},
  resetDensity: () => {},
};

/**
 * React Context for adaptive layout
 */
const AdaptiveLayoutContext = createContext<AdaptiveLayoutContextValue>(defaultContextValue);

/**
 * Props for AdaptiveLayoutProvider
 */
export interface AdaptiveLayoutProviderProps {
  /** Child components that will have access to adaptive layout context */
  children: ReactNode;
  /** Override automatic density detection */
  forceDensity?: ContentDensity;
  /** Disable CSS custom property updates (for testing) */
  disableCssUpdates?: boolean;
}

/**
 * Adaptive Layout Provider
 *
 * Wraps the application to provide adaptive layout configuration
 * to all child components via React Context.
 *
 * @example
 * ```tsx
 * // In App.tsx
 * <AdaptiveLayoutProvider>
 *   <App />
 * </AdaptiveLayoutProvider>
 *
 * // In any child component
 * const { sidebar, content, grid } = useAdaptiveLayoutContext();
 * ```
 */
export function AdaptiveLayoutProvider({
  children,
  forceDensity,
  disableCssUpdates = false,
}: AdaptiveLayoutProviderProps): React.ReactElement {
  const display = useDisplayEnvironment();
  const { density, setDensity, isAuto, resetToAuto, spacingMultiplier, fontSizeAdjustment } =
    useContentDensity();

  // Use forced density if provided, otherwise use hook value
  const effectiveDensity = forceDensity ?? density;

  // Calculate layout config based on display and density
  const config = useMemo(
    () => calculateLayoutConfig(display, spacingMultiplier, fontSizeAdjustment),
    [display, spacingMultiplier, fontSizeAdjustment]
  );

  // Apply CSS custom properties to document root
  useEffect(() => {
    if (disableCssUpdates) return;

    applyDisplayEnvironmentProperties(display);
    applyContentDensityProperties(effectiveDensity);

    // Apply layout-specific CSS custom properties
    const root = document.documentElement;

    // Sidebar properties
    const sidebar = config.sidebar;
    root.style.setProperty(
      '--layout-sidebar-width',
      sidebar.width === 'collapsed' ? `${sidebar.collapsedWidth}px` : `${sidebar.width}px`
    );
    root.style.setProperty('--layout-sidebar-collapsed-width', `${sidebar.collapsedWidth}px`);

    // Content properties
    const content = config.content;
    root.style.setProperty(
      '--layout-content-max-width',
      content.maxWidth === 'full' ? '100%' : `${content.maxWidth}px`
    );
    root.style.setProperty('--layout-content-padding', `${content.padding}px`);
    root.style.setProperty('--layout-content-gutter', `${content.gutterWidth}px`);

    // Inspector properties
    const inspector = config.inspector;
    root.style.setProperty('--layout-inspector-width', `${inspector.width}px`);
    root.style.setProperty('--layout-inspector-visible', inspector.visible ? '1' : '0');

    // Grid properties
    const grid = config.grid;
    root.style.setProperty('--layout-grid-columns', String(grid.columns));
    root.style.setProperty('--layout-grid-gap', `${grid.gap}px`);
    root.style.setProperty('--layout-grid-item-min-width', `${grid.itemMinWidth}px`);

    // Typography properties
    const typography = config.typography;
    root.style.setProperty('--layout-font-base', `${typography.baseSize}px`);
    root.style.setProperty('--layout-font-scale', String(typography.scale));
    root.style.setProperty('--layout-line-height', String(typography.lineHeight));

    // Computed font sizes based on scale
    root.style.setProperty('--layout-font-sm', `${typography.baseSize / typography.scale}px`);
    root.style.setProperty('--layout-font-lg', `${typography.baseSize * typography.scale}px`);
    root.style.setProperty(
      '--layout-font-xl',
      `${typography.baseSize * typography.scale * typography.scale}px`
    );
    root.style.setProperty(
      '--layout-font-2xl',
      `${typography.baseSize * Math.pow(typography.scale, 3)}px`
    );

    // Panel layout class
    root.setAttribute('data-panel-layout', display.panelLayout);
  }, [config, display, effectiveDensity, disableCssUpdates]);

  // Memoize context value
  const contextValue = useMemo<AdaptiveLayoutContextValue>(
    () => ({
      ...config,
      contentDensity: effectiveDensity,
      isAutoDensity: isAuto && !forceDensity,
      setContentDensity: setDensity,
      resetDensity: resetToAuto,
    }),
    [config, effectiveDensity, isAuto, forceDensity, setDensity, resetToAuto]
  );

  return (
    <AdaptiveLayoutContext.Provider value={contextValue}>{children}</AdaptiveLayoutContext.Provider>
  );
}

/**
 * Hook to access adaptive layout context
 *
 * @returns AdaptiveLayoutContextValue with all layout configuration
 * @throws Error if used outside AdaptiveLayoutProvider
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { sidebar, grid, contentDensity } = useAdaptiveLayoutContext();
 *
 *   return (
 *     <div style={{ gridTemplateColumns: `repeat(${grid.columns}, 1fr)` }}>
 *       ...
 *     </div>
 *   );
 * }
 * ```
 */
export function useAdaptiveLayoutContext(): AdaptiveLayoutContextValue {
  const context = useContext(AdaptiveLayoutContext);
  return context;
}

// Re-export types for convenience
export type {
  DisplayEnvironment,
  ContentDensity,
  AdaptiveLayoutConfig,
  SidebarConfig,
  ContentConfig,
  InspectorConfig,
  GridConfig,
  TimelineConfig,
  TypographyConfig,
};
