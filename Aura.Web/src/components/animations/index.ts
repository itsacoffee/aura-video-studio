/**
 * Animation components index
 * Exports all animation components for easy importing
 */

export { AnimatedDiv } from './AnimatedDiv';
export { AnimatedList, AnimatedListItem } from './AnimatedList';
export { PageTransition } from './PageTransition';
export { FadeIn } from './FadeIn';
export { SlideIn } from './SlideIn';
export { ScaleIn } from './ScaleIn';

// Re-export animation utilities for convenience
export * from '../../utils/animations';
export { useReducedMotion } from '../../hooks/useReducedMotion';
