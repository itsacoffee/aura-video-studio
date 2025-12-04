/**
 * LoadingState Component
 *
 * Skeleton loading states with shimmer animation.
 * Provides visual feedback during content loading.
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import type { FC } from 'react';
import { useReducedMotion } from '../../../hooks/useReducedMotion';
import { openCutTokens } from '../../../styles/designTokens';

export interface LoadingStateProps {
  /** Number of skeleton rows to show */
  rows?: number;
  /** Heights of each row (can be array or single value) */
  heights?: number | number[];
  /** Gap between rows */
  gap?: string;
  /** Whether to show shimmer animation */
  animated?: boolean;
  /** Additional class name */
  className?: string;
  /** Variant layout */
  variant?: 'default' | 'card' | 'list' | 'grid';
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    width: '100%',
  },
  skeleton: {
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: openCutTokens.radius.sm,
    overflow: 'hidden',
    position: 'relative',
  },
  shimmer: {
    position: 'absolute',
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
    animationName: {
      '0%': { transform: 'translateX(-100%)' },
      '100%': { transform: 'translateX(100%)' },
    },
    animationDuration: '1.5s',
    animationIterationCount: 'infinite',
    animationTimingFunction: 'linear',
  },
  shimmerDisabled: {
    animation: 'none',
  },
  // Card variant
  cardContainer: {
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.md,
  },
  cardHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    marginBottom: openCutTokens.spacing.md,
  },
  cardAvatar: {
    width: '40px',
    height: '40px',
    borderRadius: openCutTokens.radius.full,
  },
  cardTitle: {
    height: '16px',
    flex: 1,
    maxWidth: '200px',
  },
  cardBody: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
  },
  // List variant
  listItem: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.md,
    padding: openCutTokens.spacing.sm,
  },
  listThumbnail: {
    width: '48px',
    height: '32px',
    borderRadius: openCutTokens.radius.xs,
    flexShrink: 0,
  },
  listContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  listTitle: {
    height: '14px',
    width: '80%',
  },
  listSubtitle: {
    height: '10px',
    width: '50%',
  },
  // Grid variant
  gridContainer: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))',
  },
  gridItem: {
    aspectRatio: '16 / 9',
    borderRadius: openCutTokens.radius.md,
  },
});

/**
 * Skeleton row component with shimmer animation.
 */
const SkeletonRow: FC<{
  height: number;
  animated: boolean;
  prefersReducedMotion: boolean;
}> = ({ height, animated, prefersReducedMotion }) => {
  const styles = useStyles();
  const shouldAnimate = animated && !prefersReducedMotion;

  return (
    <div className={styles.skeleton} style={{ height }} aria-hidden="true">
      {shouldAnimate && (
        <div className={mergeClasses(styles.shimmer, !shouldAnimate && styles.shimmerDisabled)} />
      )}
    </div>
  );
};

/**
 * Card loading skeleton variant.
 */
const CardSkeleton: FC<{ animated: boolean; prefersReducedMotion: boolean }> = ({
  animated,
  prefersReducedMotion,
}) => {
  const styles = useStyles();
  const shouldAnimate = animated && !prefersReducedMotion;

  return (
    <div className={styles.cardContainer}>
      <div className={styles.cardHeader}>
        <div className={mergeClasses(styles.skeleton, styles.cardAvatar)}>
          {shouldAnimate && <div className={styles.shimmer} />}
        </div>
        <div className={mergeClasses(styles.skeleton, styles.cardTitle)}>
          {shouldAnimate && <div className={styles.shimmer} />}
        </div>
      </div>
      <div className={styles.cardBody}>
        <SkeletonRow height={12} animated={animated} prefersReducedMotion={prefersReducedMotion} />
        <SkeletonRow height={12} animated={animated} prefersReducedMotion={prefersReducedMotion} />
        <SkeletonRow height={12} animated={animated} prefersReducedMotion={prefersReducedMotion} />
      </div>
    </div>
  );
};

/**
 * List loading skeleton variant.
 */
const ListSkeleton: FC<{ rows: number; animated: boolean; prefersReducedMotion: boolean }> = ({
  rows,
  animated,
  prefersReducedMotion,
}) => {
  const styles = useStyles();
  const shouldAnimate = animated && !prefersReducedMotion;

  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className={styles.listItem}>
          <div className={mergeClasses(styles.skeleton, styles.listThumbnail)}>
            {shouldAnimate && <div className={styles.shimmer} />}
          </div>
          <div className={styles.listContent}>
            <div className={mergeClasses(styles.skeleton, styles.listTitle)}>
              {shouldAnimate && <div className={styles.shimmer} />}
            </div>
            <div className={mergeClasses(styles.skeleton, styles.listSubtitle)}>
              {shouldAnimate && <div className={styles.shimmer} />}
            </div>
          </div>
        </div>
      ))}
    </>
  );
};

/**
 * Grid loading skeleton variant.
 */
const GridSkeleton: FC<{
  rows: number;
  gap: string;
  animated: boolean;
  prefersReducedMotion: boolean;
}> = ({ rows, gap, animated, prefersReducedMotion }) => {
  const styles = useStyles();
  const shouldAnimate = animated && !prefersReducedMotion;

  return (
    <div className={styles.gridContainer} style={{ gap }}>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className={mergeClasses(styles.skeleton, styles.gridItem)}>
          {shouldAnimate && <div className={styles.shimmer} />}
        </div>
      ))}
    </div>
  );
};

/**
 * LoadingState provides skeleton loading UI with shimmer animation.
 * Supports multiple variants for different content layouts.
 */
export const LoadingState: FC<LoadingStateProps> = ({
  rows = 3,
  heights = 16,
  gap = openCutTokens.spacing.md,
  animated = true,
  className,
  variant = 'default',
}) => {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();

  const getHeight = (index: number): number => {
    if (Array.isArray(heights)) {
      return heights[index % heights.length];
    }
    return heights;
  };

  // Render variant
  if (variant === 'card') {
    return (
      <div
        className={mergeClasses(styles.container, className)}
        style={{ gap }}
        role="progressbar"
        aria-busy="true"
        aria-label="Loading content"
      >
        <CardSkeleton animated={animated} prefersReducedMotion={prefersReducedMotion} />
      </div>
    );
  }

  if (variant === 'list') {
    return (
      <div
        className={mergeClasses(styles.container, className)}
        style={{ gap }}
        role="progressbar"
        aria-busy="true"
        aria-label="Loading content"
      >
        <ListSkeleton rows={rows} animated={animated} prefersReducedMotion={prefersReducedMotion} />
      </div>
    );
  }

  if (variant === 'grid') {
    return (
      <div className={className} role="progressbar" aria-busy="true" aria-label="Loading content">
        <GridSkeleton
          rows={rows}
          gap={gap}
          animated={animated}
          prefersReducedMotion={prefersReducedMotion}
        />
      </div>
    );
  }

  // Default variant - simple rows
  return (
    <div
      className={mergeClasses(styles.container, className)}
      style={{ gap }}
      role="progressbar"
      aria-busy="true"
      aria-label="Loading content"
    >
      {Array.from({ length: rows }).map((_, i) => (
        <SkeletonRow
          key={i}
          height={getHeight(i)}
          animated={animated}
          prefersReducedMotion={prefersReducedMotion}
        />
      ))}
    </div>
  );
};

export default LoadingState;
