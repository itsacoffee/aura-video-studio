/**
 * ShimmerText Component
 * Text with animated shimmer/shine effect
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { ReactNode } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  wrapper: {
    display: 'inline-block',
    position: 'relative',
  },
  shimmer: {
    background: 'linear-gradient(90deg, currentColor 0%, #fff 50%, currentColor 100%)',
    backgroundSize: '200% 100%',
    WebkitBackgroundClip: 'text',
    backgroundClip: 'text',
    WebkitTextFillColor: 'transparent',
    animationName: {
      '0%': { backgroundPosition: '200% 0' },
      '100%': { backgroundPosition: '-200% 0' },
    },
    animationDuration: '3s',
    animationTimingFunction: 'linear',
    animationIterationCount: 'infinite',
  },
  static: {
    color: 'inherit',
  },
});

interface ShimmerTextProps {
  children: ReactNode;
  className?: string;
  active?: boolean;
}

export function ShimmerText({ children, className, active = true }: ShimmerTextProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();

  const showShimmer = active && animationsEnabled && settings.effects.glowEffects;

  return (
    <span
      className={mergeClasses(
        styles.wrapper,
        showShimmer ? styles.shimmer : styles.static,
        className
      )}
    >
      {children}
    </span>
  );
}
