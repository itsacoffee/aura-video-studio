/**
 * Design Tokens - Premium UI/UX Design System
 * Aura Video Studio Brand Identity
 */

export const designTokens = {
  /**
   * Brand Colors - Orange to Blue gradient from app icon
   * Warm (orange) = creativity and energy
   * Cool (blue) = precision and technology
   * Purple = AI/magic blend
   */
  colors: {
    brand: {
      orange: '#FF6B35',
      orangeLight: '#FF8960',
      orangeDark: '#E85D2F',
      purple: '#6366F1',
      purpleLight: '#818CF8',
      purpleDark: '#4F46E5',
      blue: '#3B82F6',
      blueLight: '#60A5FA',
      blueDark: '#2563EB',
      gradient: 'linear-gradient(135deg, #FF6B35 0%, #6366F1 50%, #3B82F6 100%)',
      gradientHover: 'linear-gradient(135deg, #FF8960 0%, #818CF8 50%, #60A5FA 100%)',
      gradientPressed: 'linear-gradient(135deg, #E85D2F 0%, #4F46E5 50%, #2563EB 100%)',
    },
    dark: {
      bg1: '#0F0F0F',
      bg2: '#1A1A1A',
      bg3: '#252525',
      surface: '#2A2A2A',
      surfaceHover: '#323232',
      surfacePressed: '#3A3A3A',
    },
    light: {
      bg1: '#FFFFFF',
      bg2: '#F8F9FA',
      bg3: '#E9ECEF',
      surface: '#FFFFFF',
      surfaceHover: '#F8F9FA',
      surfacePressed: '#E9ECEF',
    },
  },

  /**
   * Elevation System - Shadow depths for layering
   */
  elevation: {
    level1: '0 1px 3px rgba(0, 0, 0, 0.2)',
    level2: '0 4px 12px rgba(0, 0, 0, 0.15)',
    level3: '0 8px 24px rgba(0, 0, 0, 0.2)',
    level4: '0 16px 48px rgba(0, 0, 0, 0.25)',
    glow: {
      orange: '0 0 20px rgba(255, 107, 53, 0.3)',
      purple: '0 0 20px rgba(99, 102, 241, 0.3)',
      blue: '0 0 20px rgba(59, 130, 246, 0.3)',
      brand: '0 0 30px rgba(255, 107, 53, 0.2), 0 0 60px rgba(59, 130, 246, 0.2)',
    },
  },

  /**
   * Animation Curves - Material Design inspired
   */
  animation: {
    curves: {
      standard: 'cubic-bezier(0.4, 0.0, 0.2, 1)',
      decelerate: 'cubic-bezier(0.0, 0.0, 0.2, 1)',
      accelerate: 'cubic-bezier(0.4, 0.0, 1, 1)',
      sharp: 'cubic-bezier(0.4, 0.0, 0.6, 1)',
    },
    duration: {
      fast: '200ms',
      normal: '300ms',
      slow: '500ms',
      verySlow: '800ms',
    },
  },

  /**
   * Spacing Scale - Consistent rhythm
   */
  spacing: {
    xs: '4px',
    sm: '8px',
    md: '16px',
    lg: '24px',
    xl: '32px',
    xxl: '48px',
    xxxl: '64px',
    xxxxl: '96px',
  },

  /**
   * Border Radius Scale
   */
  radius: {
    sm: '4px',
    md: '8px',
    lg: '12px',
    xl: '16px',
    xxl: '24px',
    full: '9999px',
  },

  /**
   * Typography Scale
   */
  typography: {
    hero: {
      size: '48px',
      weight: '700',
      lineHeight: '1.2',
    },
    h1: {
      size: '36px',
      weight: '600',
      lineHeight: '1.3',
    },
    h2: {
      size: '32px',
      weight: '600',
      lineHeight: '1.3',
    },
    h3: {
      size: '24px',
      weight: '600',
      lineHeight: '1.4',
    },
    body: {
      size: '16px',
      weight: '400',
      lineHeight: '1.6',
    },
    small: {
      size: '14px',
      weight: '400',
      lineHeight: '1.5',
    },
    tiny: {
      size: '12px',
      weight: '400',
      lineHeight: '1.4',
    },
  },
} as const;

/**
 * Fluid Typography Scale Tokens
 *
 * CSS custom property references for the fluid type system.
 * These tokens use clamp() for viewport-responsive sizing.
 */
export const fluidTypeScale = {
  /** Extra small - ~11-15px fluid */
  xs: 'var(--type-xs)',
  /** Small - ~13-16px fluid */
  sm: 'var(--type-sm)',
  /** Medium (base) - ~14-18px fluid */
  md: 'var(--type-md)',
  /** Large - ~17-22px fluid */
  lg: 'var(--type-lg)',
  /** Extra large - ~20-26px fluid */
  xl: 'var(--type-xl)',
  /** 2XL - ~24-31px fluid */
  '2xl': 'var(--type-2xl)',
  /** 3XL - ~29-37px fluid */
  '3xl': 'var(--type-3xl)',
  /** 4XL (display) - ~35-45px fluid */
  '4xl': 'var(--type-4xl)',
} as const;

/**
 * Line Height Tokens
 */
export const lineHeights = {
  tight: 'var(--leading-tight)',
  snug: 'var(--leading-snug)',
  normal: 'var(--leading-normal)',
  relaxed: 'var(--leading-relaxed)',
  loose: 'var(--leading-loose)',
} as const;

/**
 * Letter Spacing Tokens
 */
export const letterSpacing = {
  tight: 'var(--tracking-tight)',
  normal: 'var(--tracking-normal)',
  wide: 'var(--tracking-wide)',
  wider: 'var(--tracking-wider)',
} as const;

/**
 * Font Weight Tokens
 */
export const fontWeights = {
  light: 'var(--weight-light)',
  regular: 'var(--weight-regular)',
  medium: 'var(--weight-medium)',
  semibold: 'var(--weight-semibold)',
  bold: 'var(--weight-bold)',
} as const;

/**
 * OpenCut Design Tokens
 *
 * Design tokens specific to the OpenCut video editor components.
 * These tokens provide consistent styling across all OpenCut components.
 */
export const openCutTokens = {
  /**
   * Radius tokens for OpenCut components
   */
  radius: {
    sm: '2px',
    md: '4px',
    lg: '6px',
    xl: '8px',
    full: '9999px',
  },

  /**
   * Spacing tokens
   */
  spacing: {
    xs: '2px',
    sm: '4px',
    md: '8px',
    lg: '12px',
    xl: '16px',
  },

  /**
   * Waveform-specific colors
   */
  waveform: {
    audio: '#22C55E',
    video: '#3B82F6',
    loading: 'rgba(255, 255, 255, 0.2)',
    loadingProgress: '#3B82F6',
  },

  /**
   * Timeline clip colors
   */
  clips: {
    video: '#4F46E5',
    audio: '#22C55E',
    image: '#8B5CF6',
    text: '#EAB308',
  },
} as const;

/**
 * CSS Custom Properties Generator
 * Converts design tokens to CSS variables
 */
export function generateCSSVariables(isDark: boolean): Record<string, string> {
  const theme = isDark ? designTokens.colors.dark : designTokens.colors.light;

  return {
    // Brand colors
    '--color-brand-orange': designTokens.colors.brand.orange,
    '--color-brand-purple': designTokens.colors.brand.purple,
    '--color-brand-blue': designTokens.colors.brand.blue,
    '--color-brand-gradient': designTokens.colors.brand.gradient,

    // Background colors
    '--bg-primary': theme.bg1,
    '--bg-secondary': theme.bg2,
    '--bg-tertiary': theme.bg3,
    '--bg-surface': theme.surface,
    '--bg-surface-hover': theme.surfaceHover,

    // Elevation
    '--shadow-1': designTokens.elevation.level1,
    '--shadow-2': designTokens.elevation.level2,
    '--shadow-3': designTokens.elevation.level3,
    '--shadow-4': designTokens.elevation.level4,

    // Spacing
    '--space-xs': designTokens.spacing.xs,
    '--space-sm': designTokens.spacing.sm,
    '--space-md': designTokens.spacing.md,
    '--space-lg': designTokens.spacing.lg,
    '--space-xl': designTokens.spacing.xl,
    '--space-xxl': designTokens.spacing.xxl,
    '--space-xxxl': designTokens.spacing.xxxl,

    // Border radius
    '--radius-sm': designTokens.radius.sm,
    '--radius-md': designTokens.radius.md,
    '--radius-lg': designTokens.radius.lg,
    '--radius-xl': designTokens.radius.xl,
    '--radius-xxl': designTokens.radius.xxl,
  };
}
