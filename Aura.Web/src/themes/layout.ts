/**
 * Centralized Layout Tokens
 *
 * This module provides standardized spacing tokens for consistent layout
 * across all screens in Aura Video Studio. Optimized for 1080p displays
 * with Apple/Adobe-inspired density.
 *
 * Spacing Scale (compact for better 1080p density):
 * - XS: 4px  - Tight spacing (icon-label gaps, inline elements)
 * - SM: 8px  - Small spacing (form hints, compact gaps)
 * - MD: 12px - Medium spacing (related elements)
 * - LG: 16px - Standard spacing (section padding)
 * - XL: 24px - Large spacing (page padding on mobile)
 * - XXL: 32px - Extra large (reserved for major sections)
 *
 * Note: These map to Fluent UI tokens which provide cross-platform consistency.
 *
 * @see https://react.fluentui.dev/?path=/docs/concepts-developer-design-tokens--page
 */

import { tokens } from '@fluentui/react-components';

/**
 * Spacing tokens for consistent vertical and horizontal rhythm.
 * Values map to Fluent UI spacing tokens for theme compatibility.
 */
export const spacing = {
  /** 4px - Tight spacing for icon-label gaps, inline elements */
  xs: tokens.spacingVerticalXS,
  /** 8px - Small spacing for form hints, compact item gaps */
  sm: tokens.spacingVerticalS,
  /** 12px - Medium spacing for related elements */
  md: tokens.spacingVerticalM,
  /** 16px - Standard spacing between form fields */
  lg: tokens.spacingVerticalL,
  /** 24px - Large spacing between sections */
  xl: tokens.spacingVerticalXL,
  /** 32px - Extra large for page padding and major sections */
  xxl: tokens.spacingVerticalXXL,
} as const;

/**
 * Horizontal spacing tokens (equivalent values, named for clarity).
 */
export const spacingHorizontal = {
  xs: tokens.spacingHorizontalXS,
  sm: tokens.spacingHorizontalS,
  md: tokens.spacingHorizontalM,
  lg: tokens.spacingHorizontalL,
  xl: tokens.spacingHorizontalXL,
  xxl: tokens.spacingHorizontalXXL,
} as const;

/**
 * Page layout constants for consistent content areas.
 * Optimized for 1080p displays with Apple/Adobe-like density.
 *
 * These fixed values are retained for backwards compatibility.
 * For adaptive layouts, use the CSS custom properties from adaptive-properties.css
 * or the useAdaptiveLayout hook which provides viewport-relative values.
 */
export const pageLayout = {
  /** Maximum content width for central flows (prevents overly wide reading columns) */
  maxContentWidth: '1400px',
  /** Standard page padding (desktop) - reduced for more content space */
  pagePadding: tokens.spacingVerticalL,
  /** Page padding for smaller screens */
  pagePaddingMobile: tokens.spacingVerticalM,
  /** Minimum page padding to ensure content is not flush with edges */
  pagePaddingMin: tokens.spacingVerticalS,
} as const;

/**
 * Panel layout ratios for multi-panel views.
 * Optimized for 1080p displays.
 *
 * These fixed values are retained for backwards compatibility.
 * For adaptive layouts, use CSS custom properties:
 * - var(--layout-sidebar-width)
 * - var(--layout-inspector-width)
 * Or the useAdaptiveLayout hook for JavaScript access.
 */
export const panelLayout = {
  /** Standard sidebar width - reduced for more content area */
  sidebarWidth: '200px',
  /** Collapsed sidebar width */
  sidebarWidthCollapsed: '48px',
  /** Inspector/detail panel width */
  inspectorWidth: '280px',
  /** Minimum panel width for resizable panels */
  panelMinWidth: '160px',
  /** Maximum panel width for resizable panels */
  panelMaxWidth: '400px',
} as const;

/**
 * Content container constants.
 */
export const container = {
  /** Standard max-width for forms and content flows */
  formMaxWidth: '800px',
  /** Wide content (dashboards, grids) */
  wideMaxWidth: '1400px',
  /** Full-width content with padding */
  fullWidth: '100%',
} as const;

/**
 * Standard gap values for flex and grid layouts.
 */
export const gaps = {
  /** Tight gap for icon groups */
  tight: tokens.spacingHorizontalXS,
  /** Standard gap for button groups */
  standard: tokens.spacingHorizontalM,
  /** Wide gap for card grids */
  wide: tokens.spacingHorizontalL,
  /** Extra wide for section separation */
  extraWide: tokens.spacingHorizontalXXL,
} as const;

/**
 * Form layout constants for consistent field spacing.
 */
export const formLayout = {
  /** Gap between label and control */
  labelControlGap: tokens.spacingVerticalXS,
  /** Gap between control and helper/error text */
  controlHelperGap: tokens.spacingVerticalXS,
  /** Gap between form fields */
  fieldGap: tokens.spacingVerticalL,
  /** Gap between form sections */
  sectionGap: tokens.spacingVerticalXL,
} as const;

/**
 * Modal/dialog layout constants.
 */
export const dialogLayout = {
  /** Padding around modal content */
  contentPadding: tokens.spacingHorizontalXL,
  /** Gap between modal title, body, and actions */
  sectionGap: tokens.spacingVerticalL,
  /** Gap between action buttons */
  actionGap: tokens.spacingHorizontalM,
} as const;

/**
 * Adaptive layout CSS custom property references.
 * These values are set by the AdaptiveLayoutContext and can be used
 * in CSS-in-JS or inline styles for viewport-responsive layouts.
 */
export const adaptiveLayoutVars = {
  /** Sidebar width (scales with viewport) */
  sidebarWidth: 'var(--layout-sidebar-width, 200px)',
  /** Sidebar collapsed width */
  sidebarCollapsedWidth: 'var(--layout-sidebar-collapsed-width, 48px)',
  /** Content max width (scales with viewport) */
  contentMaxWidth: 'var(--layout-content-max-width, 1400px)',
  /** Content padding (scales with density) */
  contentPadding: 'var(--layout-content-padding, 16px)',
  /** Grid gap (scales with viewport) */
  gridGap: 'var(--layout-grid-gap, 16px)',
  /** Grid columns (calculated based on viewport) */
  gridColumns: 'var(--layout-grid-columns, 2)',
  /** Inspector width */
  inspectorWidth: 'var(--layout-inspector-width, 280px)',
  /** Base font size (fluid) */
  fontBase: 'var(--layout-font-base, 14px)',
  /** Large font size (fluid) */
  fontLg: 'var(--layout-font-lg, 17px)',
  /** Extra large font size (fluid) */
  fontXl: 'var(--layout-font-xl, 20px)',
} as const;

/**
 * Export all layout tokens as a single object for convenience.
 */
export const layoutTokens = {
  spacing,
  spacingHorizontal,
  pageLayout,
  panelLayout,
  container,
  gaps,
  formLayout,
  dialogLayout,
  adaptiveLayoutVars,
} as const;

export default layoutTokens;
