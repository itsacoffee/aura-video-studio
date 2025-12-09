/**
 * AdaptiveCard - Viewport-aware card component
 *
 * A card component that intelligently adapts padding, border-radius, and other
 * visual properties based on the current density context and display environment.
 *
 * @example
 * ```tsx
 * // Elevated card that responds to viewport
 * <AdaptiveCard variant="elevated">
 *   <CardContent>Content here</CardContent>
 * </AdaptiveCard>
 *
 * // Interactive full-height card
 * <AdaptiveCard variant="outlined" interactive fullHeight>
 *   <CardContent>Clickable content</CardContent>
 * </AdaptiveCard>
 * ```
 */

import { Card, makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import type { CardProps } from '@fluentui/react-components';
import { forwardRef } from 'react';
import { useDensity } from '../../contexts/DensityContext';

export interface AdaptiveCardProps extends Omit<CardProps, 'size'> {
  /** Card appearance variant */
  variant?: 'elevated' | 'outlined' | 'filled';
  /** Enable hover and click interactions */
  interactive?: boolean;
  /** Make card fill container height */
  fullHeight?: boolean;
}

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalL,
    borderRadius: tokens.borderRadiusLarge,
    transitionProperty: 'all',
    transitionDuration: tokens.durationNormal,
    transitionTimingFunction: tokens.curveEasyEase,
  },
  compact: {
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
  },
  spacious: {
    padding: tokens.spacingVerticalXL,
    borderRadius: tokens.borderRadiusXLarge,
  },
  elevated: {
    boxShadow: tokens.shadow8,
  },
  elevatedHover: {
    ':hover': {
      boxShadow: tokens.shadow16,
      transform: 'translateY(-2px)',
    },
  },
  outlined: {
    borderTopWidth: '1px',
    borderRightWidth: '1px',
    borderBottomWidth: '1px',
    borderLeftWidth: '1px',
    borderTopStyle: 'solid',
    borderRightStyle: 'solid',
    borderBottomStyle: 'solid',
    borderLeftStyle: 'solid',
    borderTopColor: tokens.colorNeutralStroke1,
    borderRightColor: tokens.colorNeutralStroke1,
    borderBottomColor: tokens.colorNeutralStroke1,
    borderLeftColor: tokens.colorNeutralStroke1,
    boxShadow: 'none',
  },
  filled: {
    backgroundColor: tokens.colorNeutralBackground2,
    boxShadow: 'none',
  },
  interactive: {
    cursor: 'pointer',
    ':active': {
      transform: 'scale(0.98)',
    },
  },
  fullHeight: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
  },
});

/**
 * AdaptiveCard component - viewport-aware card for Aura Video Studio
 *
 * Features:
 * - Three variants: elevated, outlined, filled
 * - Density-aware padding and border-radius
 * - Interactive mode with hover/press states
 * - Full-height option for grid layouts
 * - Smooth transitions between states
 */
export const AdaptiveCard = forwardRef<HTMLDivElement, AdaptiveCardProps>(
  (
    {
      variant = 'elevated',
      interactive = false,
      fullHeight = false,
      className,
      children,
      ...props
    },
    ref
  ) => {
    const styles = useStyles();
    const { density } = useDensity();

    return (
      <Card
        ref={ref}
        className={mergeClasses(
          styles.card,
          density === 'compact' && styles.compact,
          density === 'spacious' && styles.spacious,
          variant === 'elevated' && styles.elevated,
          variant === 'elevated' && interactive && styles.elevatedHover,
          variant === 'outlined' && styles.outlined,
          variant === 'filled' && styles.filled,
          interactive && styles.interactive,
          fullHeight && styles.fullHeight,
          className
        )}
        {...props}
      >
        {children}
      </Card>
    );
  }
);

AdaptiveCard.displayName = 'AdaptiveCard';

export default AdaptiveCard;
