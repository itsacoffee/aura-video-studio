/**
 * Animation utilities and presets for consistent animations across the application
 * Uses framer-motion and supports reduced motion preferences
 */

import { Transition, Variants } from 'framer-motion';

/**
 * Standard easing functions for professional animations
 */
export const easings = {
  // Smooth and natural for most UI elements
  default: [0.4, 0, 0.2, 1],
  // Snappier for buttons and small elements
  snappy: [0.6, 0.05, 0.01, 0.9],
  // Bouncy for success states
  bounce: [0.68, -0.55, 0.265, 1.55],
  // Smooth for page transitions
  smooth: [0.65, 0, 0.35, 1],
  // Elastic for attention-grabbing elements
  elastic: [0.68, -0.6, 0.32, 1.6],
} as const;

/**
 * Standard durations (in seconds)
 */
export const durations = {
  fast: 0.15,
  base: 0.25,
  slow: 0.35,
  slower: 0.5,
} as const;

/**
 * Standard spring configurations
 */
export const springs = {
  gentle: { type: 'spring', stiffness: 100, damping: 15 },
  snappy: { type: 'spring', stiffness: 400, damping: 30 },
  bouncy: { type: 'spring', stiffness: 300, damping: 10 },
  wobbly: { type: 'spring', stiffness: 180, damping: 12 },
} as const;

/**
 * Fade animations
 */
export const fadeVariants: Variants = {
  hidden: { opacity: 0 },
  visible: { 
    opacity: 1,
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
  exit: { 
    opacity: 0,
    transition: {
      duration: durations.fast,
      ease: easings.default,
    },
  },
};

/**
 * Slide animations (from different directions)
 */
export const slideVariants = {
  fromTop: {
    hidden: { y: -20, opacity: 0 },
    visible: { 
      y: 0, 
      opacity: 1,
      transition: {
        duration: durations.base,
        ease: easings.default,
      },
    },
    exit: { 
      y: -20, 
      opacity: 0,
      transition: {
        duration: durations.fast,
        ease: easings.default,
      },
    },
  },
  fromBottom: {
    hidden: { y: 20, opacity: 0 },
    visible: { 
      y: 0, 
      opacity: 1,
      transition: {
        duration: durations.base,
        ease: easings.default,
      },
    },
    exit: { 
      y: 20, 
      opacity: 0,
      transition: {
        duration: durations.fast,
        ease: easings.default,
      },
    },
  },
  fromLeft: {
    hidden: { x: -20, opacity: 0 },
    visible: { 
      x: 0, 
      opacity: 1,
      transition: {
        duration: durations.base,
        ease: easings.default,
      },
    },
    exit: { 
      x: -20, 
      opacity: 0,
      transition: {
        duration: durations.fast,
        ease: easings.default,
      },
    },
  },
  fromRight: {
    hidden: { x: 20, opacity: 0 },
    visible: { 
      x: 0, 
      opacity: 1,
      transition: {
        duration: durations.base,
        ease: easings.default,
      },
    },
    exit: { 
      x: 20, 
      opacity: 0,
      transition: {
        duration: durations.fast,
        ease: easings.default,
      },
    },
  },
};

/**
 * Scale animations (for modals, popovers, etc.)
 */
export const scaleVariants: Variants = {
  hidden: { scale: 0.95, opacity: 0 },
  visible: { 
    scale: 1, 
    opacity: 1,
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
  exit: { 
    scale: 0.95, 
    opacity: 0,
    transition: {
      duration: durations.fast,
      ease: easings.default,
    },
  },
};

/**
 * Expand/collapse variants for accordions and collapsible sections
 */
export const expandVariants: Variants = {
  collapsed: { 
    height: 0, 
    opacity: 0,
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
  expanded: { 
    height: 'auto', 
    opacity: 1,
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
};

/**
 * Stagger children animation
 */
export const staggerContainer: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.05,
      delayChildren: 0.1,
    },
  },
};

export const staggerItem: Variants = {
  hidden: { y: 10, opacity: 0 },
  visible: {
    y: 0,
    opacity: 1,
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
};

/**
 * Shake animation for errors
 */
export const shakeVariants: Variants = {
  shake: {
    x: [0, -10, 10, -10, 10, 0],
    transition: {
      duration: 0.4,
      ease: 'easeInOut',
    },
  },
};

/**
 * Success celebration animation
 */
export const celebrationVariants: Variants = {
  initial: { scale: 0, rotate: -180 },
  animate: {
    scale: [0, 1.2, 1],
    rotate: [0, 10, -10, 0],
    transition: {
      duration: 0.6,
      ease: easings.bounce,
    },
  },
};

/**
 * Pulse animation for loading states
 */
export const pulseVariants: Variants = {
  pulse: {
    scale: [1, 1.05, 1],
    opacity: [1, 0.7, 1],
    transition: {
      duration: 1.5,
      repeat: Infinity,
      ease: 'easeInOut',
    },
  },
};

/**
 * Bounce animation for attention-grabbing
 */
export const bounceVariants: Variants = {
  bounce: {
    y: [0, -10, 0],
    transition: {
      duration: 0.6,
      repeat: Infinity,
      ease: 'easeInOut',
    },
  },
};

/**
 * Rotate animation for loading spinners
 */
export const rotateVariants: Variants = {
  rotate: {
    rotate: 360,
    transition: {
      duration: 1,
      repeat: Infinity,
      ease: 'linear',
    },
  },
};

/**
 * Page transition variants
 */
export const pageTransitionVariants: Variants = {
  initial: { 
    opacity: 0, 
    y: 20,
  },
  animate: { 
    opacity: 1, 
    y: 0,
    transition: {
      duration: durations.slow,
      ease: easings.smooth,
    },
  },
  exit: { 
    opacity: 0, 
    y: -20,
    transition: {
      duration: durations.fast,
      ease: easings.smooth,
    },
  },
};

/**
 * Modal backdrop variants
 */
export const backdropVariants: Variants = {
  hidden: { opacity: 0 },
  visible: { 
    opacity: 1,
    transition: {
      duration: durations.base,
    },
  },
  exit: { 
    opacity: 0,
    transition: {
      duration: durations.fast,
    },
  },
};

/**
 * Drawer slide variants
 */
export const drawerVariants = {
  left: {
    hidden: { x: '-100%' },
    visible: { 
      x: 0,
      transition: {
        duration: durations.slow,
        ease: easings.smooth,
      },
    },
    exit: { 
      x: '-100%',
      transition: {
        duration: durations.base,
        ease: easings.smooth,
      },
    },
  },
  right: {
    hidden: { x: '100%' },
    visible: { 
      x: 0,
      transition: {
        duration: durations.slow,
        ease: easings.smooth,
      },
    },
    exit: { 
      x: '100%',
      transition: {
        duration: durations.base,
        ease: easings.smooth,
      },
    },
  },
  top: {
    hidden: { y: '-100%' },
    visible: { 
      y: 0,
      transition: {
        duration: durations.slow,
        ease: easings.smooth,
      },
    },
    exit: { 
      y: '-100%',
      transition: {
        duration: durations.base,
        ease: easings.smooth,
      },
    },
  },
  bottom: {
    hidden: { y: '100%' },
    visible: { 
      y: 0,
      transition: {
        duration: durations.slow,
        ease: easings.smooth,
      },
    },
    exit: { 
      y: '100%',
      transition: {
        duration: durations.base,
        ease: easings.smooth,
      },
    },
  },
};

/**
 * Tooltip variants
 */
export const tooltipVariants: Variants = {
  hidden: { 
    opacity: 0, 
    scale: 0.9,
    y: 5,
  },
  visible: { 
    opacity: 1, 
    scale: 1,
    y: 0,
    transition: {
      duration: durations.fast,
      ease: easings.snappy,
    },
  },
  exit: { 
    opacity: 0, 
    scale: 0.9,
    y: 5,
    transition: {
      duration: durations.fast,
      ease: easings.snappy,
    },
  },
};

/**
 * Button press animation
 */
export const buttonPressVariants: Variants = {
  rest: { scale: 1 },
  hover: { 
    scale: 1.02,
    transition: {
      duration: durations.fast,
      ease: easings.snappy,
    },
  },
  tap: { 
    scale: 0.98,
    transition: {
      duration: durations.fast,
      ease: easings.snappy,
    },
  },
};

/**
 * Card hover effect
 */
export const cardHoverVariants: Variants = {
  rest: { 
    y: 0,
    boxShadow: '0 1px 3px 0 rgb(0 0 0 / 0.1)',
  },
  hover: { 
    y: -4,
    boxShadow: '0 10px 15px -3px rgb(0 0 0 / 0.1)',
    transition: {
      duration: durations.base,
      ease: easings.default,
    },
  },
};

/**
 * List item variants for staggered animations
 */
export const listItemVariants: Variants = {
  hidden: { 
    opacity: 0, 
    x: -20,
  },
  visible: (index: number) => ({
    opacity: 1,
    x: 0,
    transition: {
      delay: index * 0.05,
      duration: durations.base,
      ease: easings.default,
    },
  }),
};

/**
 * Helper function to create a transition with reduced motion support
 */
export function createTransition(
  transition: Transition,
  prefersReducedMotion: boolean
): Transition {
  if (prefersReducedMotion) {
    return {
      duration: 0.01,
      ease: 'linear',
    };
  }
  return transition;
}

/**
 * Helper function to create variants with reduced motion support
 */
export function createVariants(
  variants: Variants,
  prefersReducedMotion: boolean
): Variants {
  if (prefersReducedMotion) {
    // For reduced motion, only animate opacity
    const reducedVariants: Variants = {};
    Object.keys(variants).forEach((key) => {
      const variant = variants[key];
      if (typeof variant === 'object') {
        reducedVariants[key] = {
          opacity: variant.opacity ?? 1,
          transition: { duration: 0.01 },
        };
      } else {
        reducedVariants[key] = variant;
      }
    });
    return reducedVariants;
  }
  return variants;
}
