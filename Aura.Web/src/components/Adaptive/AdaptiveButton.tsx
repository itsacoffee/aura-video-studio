/**
 * AdaptiveButton - Viewport-aware button component
 *
 * A button component that intelligently adapts its size, padding, and other
 * visual properties based on the current density context.
 *
 * @example
 * ```tsx
 * // Standard adaptive button
 * <AdaptiveButton onClick={handleClick}>
 *   Click Me
 * </AdaptiveButton>
 *
 * // Full width adaptive button
 * <AdaptiveButton fullWidth appearance="primary">
 *   Submit
 * </AdaptiveButton>
 * ```
 */

import { forwardRef } from 'react';
import type { ReactNode, ForwardedRef } from 'react';
import { Button, makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import type { ButtonProps } from '@fluentui/react-components';
import { useDensity } from '../../contexts/DensityContext';

export interface AdaptiveButtonProps extends Omit<ButtonProps, 'size' | 'ref'> {
  /** Make button fill container width */
  fullWidth?: boolean;
  /** Button content */
  children?: ReactNode;
}

const useStyles = makeStyles({
  button: {
    minHeight: '36px',
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    paddingTop: tokens.spacingVerticalS,
    paddingBottom: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    fontWeight: tokens.fontWeightSemibold,
    transitionProperty: 'all',
    transitionDuration: tokens.durationFaster,
    transitionTimingFunction: tokens.curveEasyEase,
  },
  compact: {
    minHeight: '32px',
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
  },
  spacious: {
    minHeight: '44px',
    paddingLeft: tokens.spacingHorizontalXL,
    paddingRight: tokens.spacingHorizontalXL,
    paddingTop: tokens.spacingVerticalM,
    paddingBottom: tokens.spacingVerticalM,
    fontSize: tokens.fontSizeBase400,
  },
  fullWidth: {
    width: '100%',
  },
});

/**
 * AdaptiveButton component - viewport-aware button for Aura Video Studio
 *
 * Features:
 * - Density-aware sizing (compact, comfortable, spacious)
 * - Full width option
 * - Smooth transitions between states
 * - Inherits all Fluent UI Button props
 */
export const AdaptiveButton = forwardRef<HTMLButtonElement, AdaptiveButtonProps>(
  ({ fullWidth = false, className, children, ...props }, ref) => {
    const styles = useStyles();
    const { density } = useDensity();

    return (
      <Button
        // Fluent UI Button supports both anchor and button elements.
        // AdaptiveButton is always rendered as a button, so we assert the ref type.
        ref={ref as ForwardedRef<HTMLButtonElement>}
        className={mergeClasses(
          styles.button,
          density === 'compact' && styles.compact,
          density === 'spacious' && styles.spacious,
          fullWidth && styles.fullWidth,
          className
        )}
        {...(props as ButtonProps)}
      >
        {children}
      </Button>
    );
  }
);

AdaptiveButton.displayName = 'AdaptiveButton';

export default AdaptiveButton;
