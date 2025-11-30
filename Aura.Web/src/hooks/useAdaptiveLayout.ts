/**
 * Adaptive Layout Hook
 *
 * Provides calculated layout dimensions and configurations based on
 * display environment for Apple-level adaptive layouts.
 */

import { useMemo } from 'react';
import { useDisplayEnvironment, type DisplayEnvironment } from './useDisplayEnvironment';
import { useContentDensity } from './useContentDensity';

/**
 * Sidebar configuration
 */
export interface SidebarConfig {
  /** Current width in pixels (or 'collapsed' when hidden) */
  width: number | 'collapsed';
  /** Whether sidebar can be collapsed */
  canCollapse: boolean;
  /** Width when collapsed */
  collapsedWidth: number;
  /** Whether to show as overlay on mobile */
  isOverlay: boolean;
}

/**
 * Content area configuration
 */
export interface ContentConfig {
  /** Maximum content width (number for px, 'full' for 100%) */
  maxWidth: number | 'full';
  /** Content padding in pixels */
  padding: number;
  /** Gutter width between columns */
  gutterWidth: number;
}

/**
 * Inspector panel configuration
 */
export interface InspectorConfig {
  /** Width in pixels */
  width: number;
  /** Whether inspector is visible */
  visible: boolean;
  /** Position relative to content */
  position: 'right' | 'bottom' | 'drawer';
}

/**
 * Grid configuration
 */
export interface GridConfig {
  /** Number of columns */
  columns: number;
  /** Gap between items in pixels */
  gap: number;
  /** Minimum item width in pixels */
  itemMinWidth: number;
}

/**
 * Timeline configuration
 */
export interface TimelineConfig {
  /** Height in pixels or 'auto' */
  height: number | 'auto';
  /** Individual track height */
  trackHeight: number;
  /** Whether timeline is collapsible */
  canCollapse: boolean;
}

/**
 * Typography configuration
 */
export interface TypographyConfig {
  /** Base font size in pixels */
  baseSize: number;
  /** Type scale multiplier (e.g., 1.125, 1.2, 1.25, 1.333) */
  scale: number;
  /** Line height multiplier */
  lineHeight: number;
}

/**
 * Complete adaptive layout configuration
 */
export interface AdaptiveLayoutConfig {
  sidebar: SidebarConfig;
  content: ContentConfig;
  inspector: InspectorConfig;
  grid: GridConfig;
  timeline: TimelineConfig;
  typography: TypographyConfig;
  /** Display environment that drove these calculations */
  display: DisplayEnvironment;
}

/**
 * Calculate sidebar configuration based on display environment
 */
function calculateSidebarConfig(env: DisplayEnvironment): SidebarConfig {
  const { sizeClass, viewportWidth, panelLayout } = env;

  if (sizeClass === 'compact') {
    return {
      width: 'collapsed',
      canCollapse: true,
      collapsedWidth: 48,
      isOverlay: true,
    };
  }

  if (sizeClass === 'regular') {
    // Scale sidebar with viewport (15% of width, clamped)
    const scaledWidth = Math.round(viewportWidth * 0.15);
    return {
      width: Math.max(180, Math.min(scaledWidth, 260)),
      canCollapse: true,
      collapsedWidth: 48,
      isOverlay: false,
    };
  }

  // Expanded
  if (panelLayout === 'three-panel') {
    const scaledWidth = Math.round(viewportWidth * 0.12);
    return {
      width: Math.max(200, Math.min(scaledWidth, 300)),
      canCollapse: true,
      collapsedWidth: 56,
      isOverlay: false,
    };
  }

  const scaledWidth = Math.round(viewportWidth * 0.14);
  return {
    width: Math.max(200, Math.min(scaledWidth, 280)),
    canCollapse: true,
    collapsedWidth: 56,
    isOverlay: false,
  };
}

/**
 * Calculate content configuration based on display environment
 */
function calculateContentConfig(env: DisplayEnvironment, densityMultiplier: number): ContentConfig {
  const { sizeClass, viewportWidth, aspectRatio } = env;
  const basePadding = 16;

  if (sizeClass === 'compact') {
    return {
      maxWidth: 'full',
      padding: Math.round(basePadding * 0.75 * densityMultiplier),
      gutterWidth: Math.round(12 * densityMultiplier),
    };
  }

  if (sizeClass === 'regular') {
    return {
      maxWidth: Math.min(1400, viewportWidth - 240),
      padding: Math.round(basePadding * densityMultiplier),
      gutterWidth: Math.round(16 * densityMultiplier),
    };
  }

  // Expanded mode
  if (aspectRatio === 'ultrawide') {
    // Limit content width on ultrawide to prevent overly long lines
    return {
      maxWidth: Math.min(1800, Math.round(viewportWidth * 0.7)),
      padding: Math.round(basePadding * 1.25 * densityMultiplier),
      gutterWidth: Math.round(24 * densityMultiplier),
    };
  }

  return {
    maxWidth: Math.min(1600, viewportWidth - 320),
    padding: Math.round(basePadding * 1.25 * densityMultiplier),
    gutterWidth: Math.round(20 * densityMultiplier),
  };
}

/**
 * Calculate inspector configuration based on display environment
 */
function calculateInspectorConfig(env: DisplayEnvironment): InspectorConfig {
  const { sizeClass, viewportWidth, canShowDetailInspector, panelLayout } = env;

  if (sizeClass === 'compact' || !canShowDetailInspector) {
    return {
      width: 0,
      visible: false,
      position: 'drawer',
    };
  }

  if (sizeClass === 'regular') {
    // On regular screens, inspector is a bottom drawer
    return {
      width: 0,
      visible: false,
      position: 'bottom',
    };
  }

  // Expanded mode with inspector
  if (panelLayout === 'three-panel') {
    const scaledWidth = Math.round(viewportWidth * 0.2);
    return {
      width: Math.max(280, Math.min(scaledWidth, 400)),
      visible: true,
      position: 'right',
    };
  }

  return {
    width: Math.round(viewportWidth * 0.22),
    visible: true,
    position: 'right',
  };
}

/**
 * Calculate grid configuration based on display environment
 */
function calculateGridConfig(env: DisplayEnvironment, densityMultiplier: number): GridConfig {
  const { contentColumns, viewportWidth, sizeClass } = env;
  const baseGap = 16;

  // Calculate minimum item width based on columns and viewport
  let itemMinWidth: number;
  if (sizeClass === 'compact') {
    itemMinWidth = Math.min(280, viewportWidth - 32);
  } else if (sizeClass === 'regular') {
    itemMinWidth = Math.max(280, Math.round((viewportWidth - 300) / contentColumns));
  } else {
    itemMinWidth = Math.max(320, Math.round((viewportWidth - 400) / contentColumns));
  }

  return {
    columns: contentColumns,
    gap: Math.round(baseGap * densityMultiplier),
    itemMinWidth: Math.min(400, itemMinWidth),
  };
}

/**
 * Calculate timeline configuration based on display environment
 */
function calculateTimelineConfig(
  env: DisplayEnvironment,
  densityMultiplier: number
): TimelineConfig {
  const { viewportHeight, sizeClass } = env;
  const baseTrackHeight = 48;

  if (sizeClass === 'compact') {
    return {
      height: 'auto',
      trackHeight: Math.round(baseTrackHeight * 0.8 * densityMultiplier),
      canCollapse: true,
    };
  }

  // Reserve ~25% of viewport height for timeline on regular/expanded
  const timelineHeight = Math.max(150, Math.min(Math.round(viewportHeight * 0.25), 400));

  return {
    height: timelineHeight,
    trackHeight: Math.round(baseTrackHeight * densityMultiplier),
    canCollapse: viewportHeight < 900,
  };
}

/**
 * Calculate typography configuration based on display environment
 */
function calculateTypographyConfig(
  env: DisplayEnvironment,
  fontSizeAdjustment: number
): TypographyConfig {
  const { baseFontSize, densityClass } = env;

  // Type scale based on density
  let scale: number;
  switch (densityClass) {
    case 'ultra':
      scale = 1.25; // Major third
      break;
    case 'high':
      scale = 1.2; // Minor third
      break;
    case 'standard':
      scale = 1.175; // Slightly smaller
      break;
    case 'low':
      scale = 1.125; // Major second
      break;
  }

  return {
    baseSize: baseFontSize + fontSizeAdjustment,
    scale,
    lineHeight: densityClass === 'low' ? 1.4 : 1.5,
  };
}

/**
 * Calculate complete layout configuration from display environment
 */
export function calculateLayoutConfig(
  env: DisplayEnvironment,
  densityMultiplier: number = 1,
  fontSizeAdjustment: number = 0
): AdaptiveLayoutConfig {
  return {
    sidebar: calculateSidebarConfig(env),
    content: calculateContentConfig(env, densityMultiplier),
    inspector: calculateInspectorConfig(env),
    grid: calculateGridConfig(env, densityMultiplier),
    timeline: calculateTimelineConfig(env, densityMultiplier),
    typography: calculateTypographyConfig(env, fontSizeAdjustment),
    display: env,
  };
}

/**
 * Custom hook for adaptive layout configuration
 *
 * Combines display environment detection with content density preferences
 * to provide calculated layout dimensions.
 *
 * @returns AdaptiveLayoutConfig object with all computed layout dimensions
 *
 * @example
 * ```tsx
 * const layout = useAdaptiveLayout();
 *
 * // Use sidebar configuration
 * <Sidebar width={layout.sidebar.width} collapsed={layout.sidebar.width === 'collapsed'} />
 *
 * // Use grid configuration
 * <Grid columns={layout.grid.columns} gap={layout.grid.gap}>...</Grid>
 * ```
 */
export function useAdaptiveLayout(): AdaptiveLayoutConfig {
  const display = useDisplayEnvironment();
  const { spacingMultiplier, fontSizeAdjustment } = useContentDensity();

  const config = useMemo(
    () => calculateLayoutConfig(display, spacingMultiplier, fontSizeAdjustment),
    [display, spacingMultiplier, fontSizeAdjustment]
  );

  return config;
}
