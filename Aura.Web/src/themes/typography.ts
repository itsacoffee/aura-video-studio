/**
 * Centralized Typography Tokens
 *
 * This module provides standardized typography tokens for consistent
 * text hierarchy across all screens in Aura Video Studio.
 * These tokens integrate with Fluent UI typography tokens.
 *
 * Typography Scale:
 * - Display: 48px - Hero areas, landing pages (rare use)
 * - H1: 28-32px - Main page titles
 * - H2: 22-24px - Section headings
 * - H3: 18-20px - Subgroup headings
 * - Body: 14-16px - Primary copy
 * - Caption: 12-13px - Helper text, labels
 *
 * @see https://react.fluentui.dev/?path=/docs/concepts-developer-design-tokens--page
 */

import { tokens, makeStyles } from '@fluentui/react-components';

/**
 * Font size tokens mapped to Fluent UI base sizes.
 */
export const fontSizes = {
  /** 12px - Captions, helper text */
  caption: tokens.fontSizeBase200,
  /** 13px - Small body text */
  small: tokens.fontSizeBase300,
  /** 14px - Standard body text */
  body: tokens.fontSizeBase400,
  /** 16px - Emphasized body text */
  bodyLarge: tokens.fontSizeBase500,
  /** 18px - Subgroup headings (H3) */
  h3: tokens.fontSizeBase600,
  /** 20px - Larger H3 variant */
  h3Large: tokens.fontSizeHero700,
  /** 24px - Section headings (H2) */
  h2: tokens.fontSizeHero800,
  /** 28px - Main page titles (H1) */
  h1: tokens.fontSizeHero900,
  /** 32px - Large page titles */
  h1Large: tokens.fontSizeHero1000,
  /** 48px - Display/hero text */
  display: '48px',
} as const;

/**
 * Font weight tokens.
 */
export const fontWeights = {
  /** 400 - Normal body text */
  normal: tokens.fontWeightRegular,
  /** 500 - Medium emphasis */
  medium: tokens.fontWeightMedium,
  /** 600 - Semi-bold for headings */
  semibold: tokens.fontWeightSemibold,
  /** 700 - Bold for emphasis */
  bold: tokens.fontWeightBold,
} as const;

/**
 * Line height tokens for optimal readability.
 */
export const lineHeights = {
  /** 1.25 - Tight line height for headings */
  tight: tokens.lineHeightBase200,
  /** 1.4 - Standard line height */
  normal: tokens.lineHeightBase400,
  /** 1.5 - Comfortable reading */
  relaxed: tokens.lineHeightBase500,
  /** 1.6 - Extra relaxed for long-form content */
  loose: tokens.lineHeightBase600,
} as const;

/**
 * Semantic typography tokens for specific use cases.
 */
export const typography = {
  /** Display text - Hero areas only (48px, bold) */
  display: {
    fontSize: '48px',
    fontWeight: tokens.fontWeightBold,
    lineHeight: '1.2',
    letterSpacing: '-0.02em',
  },
  /** H1 - Main page titles (28-32px, semi-bold) */
  h1: {
    fontSize: tokens.fontSizeHero900,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.25',
    letterSpacing: '-0.01em',
  },
  /** H2 - Section headings (22-24px, semi-bold) */
  h2: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.3',
  },
  /** H3 - Subgroup headings (18-20px, semi-bold) */
  h3: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.4',
  },
  /** Body - Primary copy (14-16px, normal) */
  body: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.5',
  },
  /** Body large - Emphasized body (16px, normal) */
  bodyLarge: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.5',
  },
  /** Caption - Helper text, labels (12-13px, normal) */
  caption: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.4',
  },
  /** Label - Form labels (14px, medium) */
  label: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightMedium,
    lineHeight: '1.4',
  },
} as const;

/**
 * Text color tokens for consistent hierarchy.
 */
export const textColors = {
  /** Primary text color */
  primary: tokens.colorNeutralForeground1,
  /** Secondary text color (slightly muted) */
  secondary: tokens.colorNeutralForeground2,
  /** Tertiary text color (muted, for hints) */
  tertiary: tokens.colorNeutralForeground3,
  /** Disabled text color */
  disabled: tokens.colorNeutralForegroundDisabled,
  /** Brand/accent text color */
  brand: tokens.colorBrandForeground1,
  /** Error text color */
  error: tokens.colorPaletteRedForeground1,
  /** Success text color */
  success: tokens.colorPaletteGreenForeground1,
  /** Warning text color */
  warning: tokens.colorPaletteYellowForeground1,
} as const;

/**
 * Reusable typography style hooks.
 * Use these in components via the makeStyles pattern.
 */
export const useTypographyStyles = makeStyles({
  display: {
    fontSize: '48px',
    fontWeight: tokens.fontWeightBold,
    lineHeight: '1.2',
    letterSpacing: '-0.02em',
    color: tokens.colorNeutralForeground1,
  },
  h1: {
    fontSize: tokens.fontSizeHero900,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.25',
    letterSpacing: '-0.01em',
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalM,
  },
  h2: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.3',
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalS,
  },
  h3: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.4',
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalXS,
  },
  body: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.5',
    color: tokens.colorNeutralForeground1,
  },
  bodySecondary: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.5',
    color: tokens.colorNeutralForeground2,
  },
  caption: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.4',
    color: tokens.colorNeutralForeground3,
  },
  label: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightMedium,
    lineHeight: '1.4',
    color: tokens.colorNeutralForeground1,
  },
  /** Page title style with consistent margins */
  pageTitle: {
    fontSize: tokens.fontSizeHero900,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.25',
    letterSpacing: '-0.01em',
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalL,
  },
  /** Section title style */
  sectionTitle: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.3',
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalM,
  },
  /** Page subtitle/description */
  pageSubtitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightRegular,
    lineHeight: '1.5',
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalL,
  },
});

/**
 * Export all typography tokens as a single object.
 */
export const typographyTokens = {
  fontSizes,
  fontWeights,
  lineHeights,
  typography,
  textColors,
} as const;

export default typographyTokens;
