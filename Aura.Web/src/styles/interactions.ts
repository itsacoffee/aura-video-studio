/**
 * Interaction Styles for OpenCut Components
 *
 * Enhanced hover states, focus management, and micro-interactions
 * following Apple Human Interface Guidelines for premium feel.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { openCutTokens } from './designTokens';

/**
 * Reusable interaction styles for OpenCut components.
 * Apply these to interactive elements for consistent hover, focus, and active states.
 */
export const useInteractionStyles = makeStyles({
  /**
   * Subtle lift on hover - perfect for cards and panels
   * Provides visual feedback with elevation change
   */
  liftOnHover: {
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}, 
                 box-shadow ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: openCutTokens.shadows.md,
    },
    ':active': {
      transform: 'translateY(0)',
      boxShadow: openCutTokens.shadows.sm,
    },
  },

  /**
   * Scale on hover - ideal for thumbnails and icons
   * Creates subtle emphasis on interactive elements
   */
  scaleOnHover: {
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.spring}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
    ':hover': {
      transform: 'scale(1.05)',
    },
    ':active': {
      transform: 'scale(0.98)',
    },
  },

  /**
   * Glow on focus - for keyboard accessibility
   * Provides clear visual focus indicator
   */
  glowOnFocus: {
    transition: `box-shadow ${openCutTokens.animation.duration.fast}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
    ':focus-visible': {
      outline: 'none',
      boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}, 0 0 12px ${tokens.colorBrandStroke1}40`,
    },
  },

  /**
   * Press feedback - for buttons and clickable elements
   * Provides immediate tactile response
   */
  pressable: {
    cursor: 'pointer',
    userSelect: 'none',
    transition: `transform ${openCutTokens.animation.duration.instant}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
    ':active': {
      transform: 'scale(0.97)',
    },
  },

  /**
   * Smooth color transition - for elements with color changes
   * Applies to background, text, and border colors
   */
  colorTransition: {
    transition: `color ${openCutTokens.animation.duration.fast}, 
                 background-color ${openCutTokens.animation.duration.fast},
                 border-color ${openCutTokens.animation.duration.fast}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
  },

  /**
   * Combined interactive styles - for general interactive elements
   * Includes hover, focus, and active states
   */
  interactive: {
    cursor: 'pointer',
    userSelect: 'none',
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut},
                 background-color ${openCutTokens.animation.duration.fast},
                 border-color ${openCutTokens.animation.duration.fast},
                 box-shadow ${openCutTokens.animation.duration.fast}`,
    '@media (prefers-reduced-motion: reduce)': {
      transition: 'none',
    },
    ':hover': {
      backgroundColor: openCutTokens.colors.hover,
    },
    ':active': {
      backgroundColor: openCutTokens.colors.active,
      transform: 'scale(0.98)',
    },
    ':focus-visible': {
      outline: 'none',
      boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
    },
  },

  /**
   * Disabled state overlay - for non-interactive elements
   * Reduces opacity and removes pointer events
   */
  disabled: {
    opacity: 0.5,
    pointerEvents: 'none',
    cursor: 'not-allowed',
  },

  /**
   * Pulse animation - for loading or attention states
   * Subtle breathing animation
   */
  pulse: {
    '@keyframes pulse': {
      '0%': { opacity: 1 },
      '50%': { opacity: 0.6 },
      '100%': { opacity: 1 },
    },
    animationName: 'pulse',
    animationDuration: '2s',
    animationIterationCount: 'infinite',
    animationTimingFunction: 'ease-in-out',
    '@media (prefers-reduced-motion: reduce)': {
      animation: 'none',
    },
  },

  /**
   * Fade in animation - for elements appearing
   */
  fadeIn: {
    '@keyframes fadeIn': {
      '0%': { opacity: 0 },
      '100%': { opacity: 1 },
    },
    animationName: 'fadeIn',
    animationDuration: openCutTokens.animation.duration.normal,
    animationTimingFunction: openCutTokens.animation.easing.easeOut,
    animationFillMode: 'forwards',
    '@media (prefers-reduced-motion: reduce)': {
      animation: 'none',
      opacity: 1,
    },
  },

  /**
   * Slide up animation - for toasts and notifications
   */
  slideUp: {
    '@keyframes slideUp': {
      '0%': {
        opacity: 0,
        transform: 'translateY(10px)',
      },
      '100%': {
        opacity: 1,
        transform: 'translateY(0)',
      },
    },
    animationName: 'slideUp',
    animationDuration: openCutTokens.animation.duration.normal,
    animationTimingFunction: openCutTokens.animation.easing.easeOut,
    animationFillMode: 'forwards',
    '@media (prefers-reduced-motion: reduce)': {
      animation: 'none',
      opacity: 1,
      transform: 'translateY(0)',
    },
  },

  /**
   * Slide down animation - for dropdowns
   */
  slideDown: {
    '@keyframes slideDown': {
      '0%': {
        opacity: 0,
        transform: 'translateY(-10px)',
      },
      '100%': {
        opacity: 1,
        transform: 'translateY(0)',
      },
    },
    animationName: 'slideDown',
    animationDuration: openCutTokens.animation.duration.normal,
    animationTimingFunction: openCutTokens.animation.easing.easeOut,
    animationFillMode: 'forwards',
    '@media (prefers-reduced-motion: reduce)': {
      animation: 'none',
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
});
