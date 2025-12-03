/**
 * OpenCut Design Token System
 *
 * A comprehensive design token system for the OpenCut video editor following
 * Apple Human Interface Guidelines for a premium, professional video editing
 * experience with generous spacing, refined typography, and elegant animations.
 */

/**
 * Core design tokens for OpenCut video editor.
 * These tokens provide a consistent design language across all components.
 */
export const openCutTokens = {
  /**
   * Spacing scale with generous whitespace for a premium feel.
   * Based on 4px base unit with logical progression.
   */
  spacing: {
    /** 4px - Extra extra small */
    xxs: '4px',
    /** 8px - Extra small */
    xs: '8px',
    /** 12px - Small */
    sm: '12px',
    /** 16px - Medium (base) */
    md: '16px',
    /** 24px - Large */
    lg: '24px',
    /** 32px - Extra large */
    xl: '32px',
    /** 48px - Extra extra large */
    xxl: '48px',
    /** 20px - Standard panel padding */
    panelPadding: '20px',
    /** 24px - Gap between sections */
    sectionGap: '24px',
  },

  /**
   * Typography system for professional video editing interface.
   * Uses system fonts for optimal rendering and familiarity.
   */
  typography: {
    fontFamily: {
      /** Display font stack for headers and titles */
      display: '-apple-system, BlinkMacSystemFont, "SF Pro Display", system-ui, sans-serif',
      /** Body font stack for general text */
      body: '-apple-system, BlinkMacSystemFont, "SF Pro Text", system-ui, sans-serif',
      /** Monospace font stack for timecodes and technical values */
      mono: '"SF Mono", ui-monospace, "Cascadia Code", monospace',
    },
    fontSize: {
      /** 10px - Extra small for labels and badges */
      xs: '10px',
      /** 11px - Small for secondary info */
      sm: '11px',
      /** 13px - Base size for body text */
      base: '13px',
      /** 15px - Medium for emphasis */
      md: '15px',
      /** 17px - Large for section headers */
      lg: '17px',
      /** 20px - Extra large for panel headers */
      xl: '20px',
      /** 24px - Extra extra large for titles */
      xxl: '24px',
    },
    fontWeight: {
      /** 400 - Regular weight */
      regular: 400,
      /** 500 - Medium weight for subtle emphasis */
      medium: 500,
      /** 600 - Semibold for headers */
      semibold: 600,
      /** 700 - Bold for strong emphasis */
      bold: 700,
    },
    lineHeight: {
      /** 1.2 - Tight line height for compact UI */
      tight: 1.2,
      /** 1.4 - Normal line height for readability */
      normal: 1.4,
      /** 1.6 - Relaxed line height for body text */
      relaxed: 1.6,
    },
  },

  /**
   * Semantic color tokens for the OpenCut interface.
   * Uses CSS custom properties from Fluent UI for theme compatibility.
   */
  colors: {
    // Surface colors
    /** Primary surface - main content area */
    surfacePrimary: 'var(--colorNeutralBackground1)',
    /** Secondary surface - panels and sidebars */
    surfaceSecondary: 'var(--colorNeutralBackground2)',
    /** Tertiary surface - nested containers */
    surfaceTertiary: 'var(--colorNeutralBackground3)',
    /** Elevated surface - cards and dialogs */
    surfaceElevated: 'var(--colorNeutralBackground1)',

    // Track type colors for timeline
    /** Blue for video tracks */
    trackVideo: '#3B82F6',
    /** Green for audio tracks */
    trackAudio: '#22C55E',
    /** Purple for image tracks */
    trackImage: '#A855F7',
    /** Amber for text tracks */
    trackText: '#F59E0B',
    /** Pink for effect tracks */
    trackEffect: '#EC4899',

    // Timeline-specific colors
    /** Red for playhead */
    playhead: '#EF4444',
    /** Blue for selection highlight */
    selection: '#3B82F6',
    /** Violet for snap indicators */
    snap: '#8B5CF6',
    /** Orange for markers */
    marker: '#F97316',

    // Interactive state colors
    /** Subtle hover state */
    hover: 'rgba(255, 255, 255, 0.05)',
    /** Active/pressed state */
    active: 'rgba(255, 255, 255, 0.1)',
    /** Disabled state */
    disabled: 'rgba(255, 255, 255, 0.3)',
  },

  /**
   * Shadow system for depth and elevation.
   * Progressive depth levels from subtle to dramatic.
   */
  shadows: {
    /** Subtle shadow for inline elements */
    subtle: '0 1px 2px rgba(0, 0, 0, 0.1)',
    /** Small shadow for cards */
    sm: '0 2px 4px rgba(0, 0, 0, 0.15)',
    /** Medium shadow for panels */
    md: '0 4px 12px rgba(0, 0, 0, 0.2)',
    /** Large shadow for dialogs */
    lg: '0 8px 24px rgba(0, 0, 0, 0.25)',
    /** Extra large shadow for modals */
    xl: '0 16px 48px rgba(0, 0, 0, 0.3)',
    /** Floating shadow for tooltips and popovers */
    floating: '0 10px 40px rgba(0, 0, 0, 0.35)',
  },

  /**
   * Border radius scale for consistent rounding.
   * From minimal to full circular.
   */
  radius: {
    /** 4px - Extra small for inputs */
    xs: '4px',
    /** 6px - Small for buttons */
    sm: '6px',
    /** 8px - Medium for cards */
    md: '8px',
    /** 12px - Large for panels */
    lg: '12px',
    /** 16px - Extra large for dialogs */
    xl: '16px',
    /** Full circular radius */
    full: '9999px',
  },

  /**
   * Animation system for smooth, professional transitions.
   * Timing and easing curves for consistent motion.
   */
  animation: {
    duration: {
      /** 50ms - Instant response for micro-interactions */
      instant: '50ms',
      /** 150ms - Fast transitions for small elements */
      fast: '150ms',
      /** 250ms - Normal transitions for most animations */
      normal: '250ms',
      /** 400ms - Slow transitions for large elements */
      slow: '400ms',
      /** 600ms - Slower transitions for dramatic effect */
      slower: '600ms',
    },
    easing: {
      /** Ease out - starts fast, ends slow (natural deceleration) */
      easeOut: 'cubic-bezier(0.25, 1, 0.5, 1)',
      /** Ease in - starts slow, ends fast (acceleration) */
      easeIn: 'cubic-bezier(0.5, 0, 0.75, 0)',
      /** Ease in-out - smooth start and end */
      easeInOut: 'cubic-bezier(0.45, 0, 0.55, 1)',
      /** Spring - slight overshoot for playful feel */
      spring: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
      /** Bounce - elastic overshoot for emphasis */
      bounce: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
    },
  },

  /**
   * Z-index layering system for consistent stacking.
   * Logical layers from base content to toast notifications.
   */
  zIndex: {
    /** 0 - Base content layer */
    base: 0,
    /** 100 - Dropdowns and select menus */
    dropdown: 100,
    /** 200 - Sticky headers and footers */
    sticky: 200,
    /** 300 - Overlay backgrounds */
    overlay: 300,
    /** 400 - Modal dialogs */
    modal: 400,
    /** 500 - Popovers and popups */
    popover: 500,
    /** 600 - Tooltips */
    tooltip: 600,
    /** 700 - Toast notifications */
    toast: 700,
  },
} as const;

/**
 * Framer Motion animation variants for consistent, reusable animations.
 * Use these with AnimatePresence for enter/exit transitions.
 */
export const motionVariants = {
  /** Simple fade in/out */
  fadeIn: {
    initial: { opacity: 0 },
    animate: { opacity: 1 },
    exit: { opacity: 0 },
  },

  /** Slide up with fade */
  slideUp: {
    initial: { opacity: 0, y: 10 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: 10 },
  },

  /** Slide down with fade */
  slideDown: {
    initial: { opacity: 0, y: -10 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: -10 },
  },

  /** Scale in with fade */
  scaleIn: {
    initial: { opacity: 0, scale: 0.95 },
    animate: { opacity: 1, scale: 1 },
    exit: { opacity: 0, scale: 0.95 },
  },

  /** Spring scale in with slight overshoot */
  springIn: {
    initial: { opacity: 0, scale: 0.9 },
    animate: {
      opacity: 1,
      scale: 1,
      transition: { type: 'spring', stiffness: 300, damping: 20 },
    },
  },

  /** Slide in from left */
  slideFromLeft: {
    initial: { opacity: 0, x: -20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: -20 },
  },

  /** Slide in from right */
  slideFromRight: {
    initial: { opacity: 0, x: 20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: 20 },
  },

  /** Collapse/expand for accordion sections */
  collapse: {
    initial: { height: 0, opacity: 0 },
    animate: { height: 'auto', opacity: 1 },
    exit: { height: 0, opacity: 0 },
  },
} as const;

/**
 * Default transition configuration for framer-motion.
 */
export const defaultTransition = {
  duration: 0.25,
  ease: [0.25, 1, 0.5, 1], // easeOut
} as const;

/**
 * Reduced motion transition for accessibility.
 * Used when prefers-reduced-motion is enabled.
 */
export const reducedMotionTransition = {
  duration: 0,
  ease: 'linear',
} as const;

/**
 * Type for track type color keys.
 */
export type TrackType = 'video' | 'audio' | 'image' | 'text' | 'effect';

/**
 * Get the color for a specific track type.
 * @param type - The track type
 * @returns The hex color for the track type
 */
export function getTrackColor(type: TrackType): string {
  const colors: Record<TrackType, string> = {
    video: openCutTokens.colors.trackVideo,
    audio: openCutTokens.colors.trackAudio,
    image: openCutTokens.colors.trackImage,
    text: openCutTokens.colors.trackText,
    effect: openCutTokens.colors.trackEffect,
  };
  return colors[type];
}
