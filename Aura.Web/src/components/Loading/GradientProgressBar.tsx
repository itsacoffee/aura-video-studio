import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { memo } from 'react';

const useStyles = makeStyles({
  container: {
    width: '100%',
    height: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusLarge,
    overflow: 'hidden',
    position: 'relative',
  },
  containerLarge: {
    height: '12px',
  },
  containerMedium: {
    height: '8px',
  },
  containerSmall: {
    height: '4px',
  },
  bar: {
    height: '100%',
    background: 'linear-gradient(90deg, #00D4FF 0%, #0EA5E9 50%, #FF6B35 100%)',
    borderRadius: tokens.borderRadiusLarge,
    transition: 'width 0.3s ease-in-out',
    position: 'relative',
    overflow: 'hidden',
    '::after': {
      content: '""',
      position: 'absolute',
      top: '0',
      left: '0',
      right: '0',
      bottom: '0',
      background: 'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent)',
      animation: 'shimmer 2s infinite',
    },
  },
  '@keyframes shimmer': {
    '0%': {
      transform: 'translateX(-100%)',
    },
    '100%': {
      transform: 'translateX(100%)',
    },
  },
  indeterminate: {
    width: '100%',
    '::before': {
      content: '""',
      position: 'absolute',
      top: '0',
      left: '0',
      bottom: '0',
      width: '40%',
      background: 'linear-gradient(90deg, #00D4FF 0%, #0EA5E9 50%, #FF6B35 100%)',
      animation: 'indeterminate 1.5s infinite ease-in-out',
    },
  },
  '@keyframes indeterminate': {
    '0%': {
      left: '-40%',
    },
    '100%': {
      left: '100%',
    },
  },
});

export interface GradientProgressBarProps {
  /**
   * Progress value between 0 and 100. If undefined, shows indeterminate progress.
   */
  value?: number;
  /**
   * Maximum value (default: 100)
   */
  max?: number;
  /**
   * Size variant
   */
  thickness?: 'small' | 'medium' | 'large';
  /**
   * ARIA label for accessibility
   */
  'aria-label'?: string;
  /**
   * Additional className
   */
  className?: string;
}

/**
 * Progress bar with blue-to-orange gradient inspired by the Aura icon.
 * Supports both determinate and indeterminate states.
 */
export const GradientProgressBar = memo<GradientProgressBarProps>(
  ({ value, max = 100, thickness = 'medium', 'aria-label': ariaLabel, className }) => {
    const styles = useStyles();

    const isIndeterminate = value === undefined;
    const percentage = isIndeterminate ? 0 : Math.min(100, Math.max(0, (value / max) * 100));

    const containerClass = mergeClasses(
      styles.container,
      thickness === 'large' && styles.containerLarge,
      thickness === 'medium' && styles.containerMedium,
      thickness === 'small' && styles.containerSmall,
      className
    );

    const barClass = mergeClasses(styles.bar, isIndeterminate && styles.indeterminate);

    return (
      <div
        className={containerClass}
        role="progressbar"
        aria-valuenow={isIndeterminate ? undefined : value}
        aria-valuemin={0}
        aria-valuemax={max}
        aria-label={ariaLabel || (isIndeterminate ? 'Loading' : `${percentage}% complete`)}
      >
        {!isIndeterminate && <div className={barClass} style={{ width: `${percentage}%` }} />}
        {isIndeterminate && <div className={barClass} />}
      </div>
    );
  }
);

GradientProgressBar.displayName = 'GradientProgressBar';
