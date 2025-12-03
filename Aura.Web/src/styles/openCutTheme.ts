/**
 * OpenCut Theme Configuration
 *
 * Theme configuration using design tokens for the OpenCut video editor.
 * Provides CSS-in-JS compatible styles using Fluent UI makeStyles patterns.
 */

import { tokens } from '@fluentui/react-components';
import { openCutTokens } from './designTokens';

/**
 * Common panel styles for consistent panel appearance.
 */
export const panelStyles = {
  container: {
    display: 'flex',
    flexDirection: 'column' as const,
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.panelPadding}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
    gap: openCutTokens.spacing.sm,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: openCutTokens.spacing.panelPadding,
    display: 'flex',
    flexDirection: 'column' as const,
    gap: openCutTokens.spacing.sectionGap,
  },
} as const;

/**
 * Common section styles for collapsible sections within panels.
 */
export const sectionStyles = {
  container: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: openCutTokens.spacing.sm,
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.md,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    cursor: 'pointer',
    userSelect: 'none' as const,
    marginBottom: openCutTokens.spacing.xs,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    color: tokens.colorNeutralForeground2,
  },
  titleIcon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
  },
  chevron: {
    color: tokens.colorNeutralForeground3,
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
  },
  chevronExpanded: {
    transform: 'rotate(180deg)',
  },
  content: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: openCutTokens.spacing.sm,
  },
} as const;

/**
 * Common property row styles for form controls in panels.
 */
export const propertyRowStyles = {
  row: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    minHeight: '32px',
  },
  label: {
    color: tokens.colorNeutralForeground3,
    minWidth: '64px',
    fontSize: openCutTokens.typography.fontSize.sm,
  },
  input: {
    flex: 1,
    minWidth: '60px',
  },
  inputSmall: {
    width: '72px',
    minWidth: '72px',
  },
  dualInputRow: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xxs,
    flex: 1,
  },
  inputLabel: {
    color: tokens.colorNeutralForeground4,
    fontSize: openCutTokens.typography.fontSize.xs,
    width: '12px',
    textAlign: 'center' as const,
  },
} as const;

/**
 * Common slider row styles for sliders with value display.
 */
export const sliderRowStyles = {
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    width: '100%',
  },
  slider: {
    flex: 1,
    minWidth: '80px',
  },
  value: {
    minWidth: '40px',
    textAlign: 'right' as const,
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground2,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
} as const;

/**
 * Common button styles for icon buttons.
 */
export const iconButtonStyles = {
  small: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  medium: {
    minWidth: '32px',
    minHeight: '32px',
    padding: '4px',
  },
  large: {
    minWidth: '40px',
    minHeight: '40px',
    padding: '6px',
  },
  active: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    color: tokens.colorBrandForeground1,
  },
} as const;

/**
 * Common empty state styles.
 */
export const emptyStateStyles = {
  container: {
    display: 'flex',
    flexDirection: 'column' as const,
    alignItems: 'center',
    justifyContent: 'center',
    padding: openCutTokens.spacing.xl,
    gap: openCutTokens.spacing.md,
    textAlign: 'center' as const,
  },
  icon: {
    fontSize: '48px',
    color: tokens.colorNeutralForeground4,
    marginBottom: openCutTokens.spacing.sm,
  },
  title: {
    color: tokens.colorNeutralForeground2,
    fontWeight: openCutTokens.typography.fontWeight.medium,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    maxWidth: '280px',
  },
} as const;

/**
 * Common loading state styles for skeleton loading.
 */
export const loadingStateStyles = {
  container: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: openCutTokens.spacing.md,
    padding: openCutTokens.spacing.md,
  },
  skeleton: {
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: openCutTokens.radius.sm,
    overflow: 'hidden',
    position: 'relative' as const,
  },
  shimmer: {
    position: 'absolute' as const,
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    background: `linear-gradient(
      90deg,
      transparent 0%,
      rgba(255, 255, 255, 0.05) 50%,
      transparent 100%
    )`,
    animation: 'shimmer 1.5s infinite',
  },
} as const;

/**
 * Timeline-specific styles.
 */
export const timelineStyles = {
  clip: {
    base: {
      position: 'absolute' as const,
      top: '4px',
      bottom: '4px',
      borderRadius: openCutTokens.radius.md,
      overflow: 'hidden',
      cursor: 'pointer',
      display: 'flex',
      alignItems: 'center',
      transition: `box-shadow ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}, transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    },
    hover: {
      boxShadow: openCutTokens.shadows.md,
      transform: 'translateY(-1px)',
    },
    selected: {
      boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
      transform: 'translateY(-1px)',
    },
  },
  playhead: {
    line: {
      position: 'absolute' as const,
      top: 0,
      bottom: 0,
      width: '2px',
      backgroundColor: openCutTokens.colors.playhead,
      zIndex: 15,
      pointerEvents: 'none' as const,
    },
    handle: {
      position: 'absolute' as const,
      top: '-2px',
      left: '-7px',
      width: '16px',
      height: '16px',
      backgroundColor: openCutTokens.colors.playhead,
      borderRadius: '4px 4px 50% 50%',
      boxShadow: openCutTokens.shadows.sm,
      pointerEvents: 'auto' as const,
      cursor: 'ew-resize',
      transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    },
  },
  track: {
    base: {
      display: 'flex',
      minHeight: '56px',
      borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
      transition: `background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    },
    hover: {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    selected: {
      backgroundColor: tokens.colorNeutralBackground1Selected,
    },
    locked: {
      opacity: 0.6,
    },
  },
} as const;

/**
 * Color picker preset colors for common video editing needs.
 */
export const colorPickerPresets = {
  basic: [
    '#000000',
    '#FFFFFF',
    '#EF4444',
    '#F97316',
    '#F59E0B',
    '#84CC16',
    '#22C55E',
    '#14B8A6',
    '#06B6D4',
    '#3B82F6',
    '#6366F1',
    '#8B5CF6',
    '#A855F7',
    '#D946EF',
    '#EC4899',
    '#F43F5E',
  ],
  transparent: 'transparent',
} as const;

/**
 * CSS keyframes for animations (to be added to global styles).
 */
export const keyframes = {
  shimmer: `
    @keyframes shimmer {
      0% {
        transform: translateX(-100%);
      }
      100% {
        transform: translateX(100%);
      }
    }
  `,
  pulse: `
    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }
  `,
  spin: `
    @keyframes spin {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }
  `,
} as const;

export {
  openCutTokens,
  motionVariants,
  defaultTransition,
  reducedMotionTransition,
} from './designTokens';
