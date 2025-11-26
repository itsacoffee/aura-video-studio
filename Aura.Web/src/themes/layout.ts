/**
 * Centralized Layout Tokens
 *
 * This module provides standardized spacing tokens for consistent layout
 * across all screens in Aura Video Studio. These tokens align with
 * Fluent UI spacing tokens where possible.
 *
 * Spacing Scale:
 * - XS: 4px  - Tight spacing (between icon and label)
 * - SM: 8px  - Small spacing (form hints, compact lists)
 * - MD: 12px - Medium spacing (between related elements)
 * - LG: 16px - Standard spacing (between form fields)
 * - XL: 24px - Large spacing (between sections)
 * - XXL: 32px - Extra large spacing (page padding, major sections)
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
} as const;

export default layoutTokens;
