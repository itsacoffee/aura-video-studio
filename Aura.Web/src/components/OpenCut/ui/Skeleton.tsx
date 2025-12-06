/**
 * OpenCut Skeleton Loading Component
 *
 * Skeleton loading states with wave/pulse animation for the OpenCut editor.
 * Provides visual feedback during content loading with accessibility support.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import type { FC } from 'react';
import { useReducedMotion } from '../../../hooks/useReducedMotion';
import { openCutTokens } from '../../../styles/designTokens';

export interface SkeletonProps {
  /** Width of the skeleton (CSS value or number for pixels) */
  width?: string | number;
  /** Height of the skeleton (CSS value or number for pixels) */
  height?: string | number;
  /** Shape variant of the skeleton */
  variant?: 'text' | 'rectangular' | 'circular';
  /** Animation type */
  animation?: 'pulse' | 'wave' | 'none';
  /** Additional CSS class */
  className?: string;
}

const useStyles = makeStyles({
  root: {
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
    display: 'block',
  },
  text: {
    height: '1em',
    borderRadius: openCutTokens.radius.xs,
  },
  rectangular: {
    borderRadius: openCutTokens.radius.md,
  },
  circular: {
    borderRadius: '50%',
  },
  pulse: {
    '@keyframes skeletonPulse': {
      '0%': { opacity: 1 },
      '50%': { opacity: 0.4 },
      '100%': { opacity: 1 },
    },
    animationName: 'skeletonPulse',
    animationDuration: '1.5s',
    animationTimingFunction: 'ease-in-out',
    animationIterationCount: 'infinite',
  },
  wave: {
    position: 'relative',
    overflow: 'hidden',
    '::after': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.1), transparent)',
      '@keyframes skeletonWave': {
        '0%': { transform: 'translateX(-100%)' },
        '100%': { transform: 'translateX(100%)' },
      },
      animationName: 'skeletonWave',
      animationDuration: '1.5s',
      animationTimingFunction: 'linear',
      animationIterationCount: 'infinite',
    },
  },
  noAnimation: {
    animation: 'none',
    '::after': {
      animation: 'none',
    },
  },
});

/**
 * Skeleton provides a placeholder while content is loading.
 * Supports multiple variants and animation types with accessibility.
 */
export const Skeleton: FC<SkeletonProps> = ({
  width = '100%',
  height = 20,
  variant = 'rectangular',
  animation = 'wave',
  className,
}) => {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();

  const effectiveAnimation = prefersReducedMotion ? 'none' : animation;

  const style = {
    width: typeof width === 'number' ? `${width}px` : width,
    height: typeof height === 'number' ? `${height}px` : height,
  };

  return (
    <div
      className={mergeClasses(
        styles.root,
        variant === 'text' && styles.text,
        variant === 'rectangular' && styles.rectangular,
        variant === 'circular' && styles.circular,
        effectiveAnimation === 'pulse' && styles.pulse,
        effectiveAnimation === 'wave' && styles.wave,
        effectiveAnimation === 'none' && styles.noAnimation,
        className
      )}
      style={style}
      role="progressbar"
      aria-busy="true"
      aria-label="Loading..."
    />
  );
};

export default Skeleton;
