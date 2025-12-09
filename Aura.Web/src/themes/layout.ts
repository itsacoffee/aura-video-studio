/**
 * Centralized Layout Tokens
 *
 * This module provides standardized spacing tokens for consistent layout
 * across all screens in Aura Video Studio. Updated to follow Apple Human
 * Interface Guidelines (HIG) standards for spacing, touch targets, and
 * responsive design.
 *
 * Apple HIG Spacing Scale:
 * - XS: 4px   - Minimal spacing (icon-label gaps)
 * - SM: 8px   - Small spacing (compact elements)
 * - MD: 12px  - Medium spacing (related elements)
 * - LG: 16px  - Standard spacing (form fields)
 * - XL: 20px  - Large spacing (section dividers)
 * - XXL: 24px - Extra large (section padding)
 * - 3XL: 32px - Major spacing (page sections)
 * - 4XL: 40px - Very large spacing
 * - 5XL: 48px - Extra large spacing
 * - 6XL: 64px - Maximum spacing
 *
 * Touch Targets: Minimum 44pt (44px) for all interactive elements (Apple HIG)
 * Responsive Breakpoints: 480px, 768px, 1024px, 1440px for smooth scaling
 *
 * @see https://developer.apple.com/design/human-interface-guidelines/layout
 * @see https://react.fluentui.dev/?path=/docs/concepts-developer-design-tokens--page
 */

import { tokens } from '@fluentui/react-components';

/**
 * Spacing tokens following Apple HIG spacing scale.
 * All values are optimized for clarity, visual hierarchy, and breathing room.
 * Values map to Fluent UI spacing tokens for theme compatibility.
 */
export const spacing = {
  /** 4px - Minimal spacing for icon-label gaps, inline elements */
  xs: tokens.spacingVerticalXS,
  /** 8px - Small spacing for compact elements */
  sm: tokens.spacingVerticalS,
  /** 12px - Medium spacing for related elements */
  md: tokens.spacingVerticalM,
  /** 16px - Standard spacing between form fields */
  lg: tokens.spacingVerticalL,
  /** 20px - Large spacing for section dividers */
  xl: '20px',
  /** 24px - Extra large for section padding */
  xxl: tokens.spacingVerticalXXL,
  /** 32px - Major spacing for page sections */
  xxxl: tokens.spacingVerticalXXXL,
  /** 40px - Very large spacing */
  xxxxl: '40px',
  /** 48px - Extra large spacing */
  xxxxxl: '48px',
  /** 64px - Maximum spacing for major separations */
  xxxxxxl: '64px',
} as const;

/**
 * Horizontal spacing tokens (equivalent values, named for clarity).
 */
export const spacingHorizontal = {
  xs: tokens.spacingHorizontalXS,
  sm: tokens.spacingHorizontalS,
  md: tokens.spacingHorizontalM,
  lg: tokens.spacingHorizontalL,
  xl: '20px',
  xxl: tokens.spacingHorizontalXXL,
  xxxl: tokens.spacingHorizontalXXXL,
  xxxxl: '40px',
  xxxxxl: '48px',
  xxxxxxl: '64px',
} as const;

/**
 * Responsive breakpoints for smooth scaling from mobile to 4K displays.
 * Follows Apple HIG recommendations for adaptive layouts.
 */
export const breakpoints = {
  /** Mobile portrait - 320px to 479px */
  mobile: '480px',
  /** Tablet portrait / large mobile - 480px to 767px */
  tablet: '768px',
  /** Tablet landscape / small desktop - 768px to 1023px */
  desktop: '1024px',
  /** Large desktop - 1024px to 1439px */
  wide: '1440px',
  /** Ultra-wide / 4K - 1440px and above */
  ultrawide: '1920px',
} as const;

/**
 * Touch target sizes following Apple HIG (minimum 44pt).
 * All interactive elements must meet these minimums for accessibility.
 */
export const touchTargets = {
  /** Minimum touch target size (Apple HIG standard) */
  minimum: '44px',
  /** Comfortable touch target size */
  comfortable: '48px',
  /** Large touch target for primary actions */
  large: '56px',
} as const;

/**
 * Page layout constants for consistent content areas.
 * Updated for better space utilization following Apple HIG principles.
 *
 * maxContentWidth significantly increased to utilize screen space more effectively.
 * For adaptive layouts, use the CSS custom properties from adaptive-properties.css
 * or the useAdaptiveLayout hook which provides viewport-relative values.
 */
export const pageLayout = {
  /** Maximum content width - tuned for 16:9 desktops for better space utilization */
  maxContentWidth: '1760px',
  /** Standard page padding (desktop) - tighter for better space utilization with responsive scaling */
  pagePadding: 'clamp(20px, 3.6vw, 36px)',
  /** Page padding for smaller screens */
  pagePaddingMobile: tokens.spacingVerticalM,
  /** Minimum page padding to ensure content is not flush with edges */
  pagePaddingMin: tokens.spacingVerticalS,
} as const;

/**
 * Panel layout ratios for multi-panel views.
 * Updated with Apple HIG touch targets and improved spacing.
 *
 * These fixed values are retained for backwards compatibility.
 * For adaptive layouts, use CSS custom properties:
 * - var(--layout-sidebar-width)
 * - var(--layout-inspector-width)
 * Or the useAdaptiveLayout hook for JavaScript access.
 */
export const panelLayout = {
  /** Standard sidebar width - tighter for better content space utilization */
  sidebarWidth: '232px',
  /** Collapsed sidebar width - comfortable for icon-only navigation */
  sidebarWidthCollapsed: '72px',
  /** Inspector/detail panel width */
  inspectorWidth: '320px',
  /** Minimum panel width for resizable panels */
  panelMinWidth: '200px',
  /** Maximum panel width for resizable panels */
  panelMaxWidth: '480px',
} as const;

/**
 * Content container constants.
 */
export const container = {
  /** Standard max-width for forms and content flows */
  formMaxWidth: '1040px',
  /** Wide content (dashboards, grids) - increased for better space utilization */
  wideMaxWidth: '1800px',
  /** Full-width content with padding */
  fullWidth: '100%',
} as const;

/**
 * Standard gap values for flex and grid layouts.
 * Updated for Apple HIG spacing and visual hierarchy.
 */
export const gaps = {
  /** Tight gap for icon groups */
  tight: tokens.spacingHorizontalXS,
  /** Standard gap for button groups and related elements */
  standard: tokens.spacingHorizontalM,
  /** Wide gap for card grids and section separation */
  wide: tokens.spacingHorizontalL,
  /** Extra wide for major section separation */
  extraWide: '24px',
  /** Maximum gap for distinct content areas */
  maximum: '32px',
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
  breakpoints,
  touchTargets,
  pageLayout,
  panelLayout,
  container,
  gaps,
  formLayout,
  dialogLayout,
  adaptiveLayoutVars,
} as const;

export default layoutTokens;
