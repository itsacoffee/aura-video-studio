/**
 * AuraButton - Canonical button component for Aura Video Studio
 *
 * A premium, accessible button component with consistent styling across the app.
 * Built on top of Fluent UI with custom Aura branding.
 *
 * @example
 * ```tsx
 * // Primary action button
 * <AuraButton variant="primary" onClick={handleGenerate}>
 *   Generate Video
 * </AuraButton>
 *
 * // Secondary action with loading state
 * <AuraButton variant="secondary" loading={isLoading} onClick={handleSave}>
 *   Save
 * </AuraButton>
 *
 * // Destructive action
 * <AuraButton variant="destructive" onClick={handleDelete}>
 *   Delete Project
 * </AuraButton>
 * ```
 */

import {
  Button,
  Spinner,
  makeStyles,
  mergeClasses,
  tokens,
  type ButtonProps,
} from '@fluentui/react-components';
import type { ReactNode, ForwardedRef } from 'react';
import { forwardRef } from 'react';

export type AuraButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'destructive';
export type AuraButtonSize = 'small' | 'medium' | 'large';

export interface AuraButtonProps extends Omit<ButtonProps, 'appearance' | 'size' | 'ref'> {
  /** Button variant - determines visual styling */
  variant?: AuraButtonVariant;
  /** Button size */
  size?: AuraButtonSize;
  /** Show loading spinner and disable interaction */
  loading?: boolean;
  /** Text to show while loading (optional, defaults to children) */
  loadingText?: string;
  /** Icon to display before the button text */
  iconStart?: ReactNode;
  /** Icon to display after the button text */
  iconEnd?: ReactNode;
  /** Make button full width */
  fullWidth?: boolean;
}

const useStyles = makeStyles({
  // Base button styles
  button: {
    fontWeight: tokens.fontWeightSemibold,
    borderRadius: '6px',
    transitionProperty: 'background-color, border-color, box-shadow, transform',
    transitionDuration: tokens.durationNormal,
    transitionTimingFunction: tokens.curveEasyEase,
    cursor: 'pointer',
    position: 'relative',
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalS,

    // Focus state with visible ring
    ':focus-visible': {
      outlineWidth: '2px',
      outlineStyle: 'solid',
      outlineOffset: '2px',
    },

    // Pressed state
    ':active:not(:disabled)': {
      transform: 'scale(0.98)',
    },

    // Disabled state
    ':disabled': {
      cursor: 'not-allowed',
      opacity: '0.6',
    },
  },

  // Size variants
  small: {
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    paddingTop: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
    minHeight: '28px',
  },
  medium: {
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    paddingTop: tokens.spacingVerticalS,
    paddingBottom: tokens.spacingVerticalS,
    fontSize: tokens.fontSizeBase300,
    minHeight: '36px',
  },
  large: {
    paddingLeft: tokens.spacingHorizontalXL,
    paddingRight: tokens.spacingHorizontalXL,
    paddingTop: tokens.spacingVerticalM,
    paddingBottom: tokens.spacingVerticalM,
    fontSize: tokens.fontSizeBase400,
    minHeight: '44px',
  },

  // Primary variant - high-contrast, filled, strong label
  primary: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    border: 'none',
    boxShadow: tokens.shadow4,

    ':hover:not(:disabled)': {
      backgroundColor: tokens.colorBrandBackgroundHover,
      boxShadow: tokens.shadow8,
    },

    ':active:not(:disabled)': {
      backgroundColor: tokens.colorBrandBackgroundPressed,
      boxShadow: tokens.shadow2,
    },

    ':focus-visible': {
      outlineColor: tokens.colorBrandStroke1,
    },
  },

  // Secondary variant - outlined or subdued-filled
  secondary: {
    backgroundColor: tokens.colorNeutralBackground1,
    color: tokens.colorNeutralForeground1,
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

    ':hover:not(:disabled)': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      borderTopColor: tokens.colorNeutralStroke1Hover,
      borderRightColor: tokens.colorNeutralStroke1Hover,
      borderBottomColor: tokens.colorNeutralStroke1Hover,
      borderLeftColor: tokens.colorNeutralStroke1Hover,
    },

    ':active:not(:disabled)': {
      backgroundColor: tokens.colorNeutralBackground1Pressed,
      borderTopColor: tokens.colorNeutralStroke1Pressed,
      borderRightColor: tokens.colorNeutralStroke1Pressed,
      borderBottomColor: tokens.colorNeutralStroke1Pressed,
      borderLeftColor: tokens.colorNeutralStroke1Pressed,
    },

    ':focus-visible': {
      outlineColor: tokens.colorBrandStroke1,
    },
  },

  // Tertiary/Ghost variant - text-like button with minimal chrome
  tertiary: {
    backgroundColor: 'transparent',
    color: tokens.colorNeutralForeground2,
    border: 'none',

    ':hover:not(:disabled)': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      color: tokens.colorNeutralForeground1,
    },

    ':active:not(:disabled)': {
      backgroundColor: tokens.colorNeutralBackground1Pressed,
    },

    ':focus-visible': {
      outlineColor: tokens.colorBrandStroke1,
    },
  },

  // Destructive variant - red-accented for dangerous actions
  destructive: {
    backgroundColor: tokens.colorPaletteRedBackground3,
    color: tokens.colorPaletteRedForeground1,
    border: 'none',

    ':hover:not(:disabled)': {
      backgroundColor: tokens.colorPaletteRedBackground2,
    },

    ':active:not(:disabled)': {
      backgroundColor: tokens.colorPaletteRedBackground1,
    },

    ':focus-visible': {
      outlineColor: tokens.colorPaletteRedBorder2,
    },
  },

  // Full width modifier
  fullWidth: {
    width: '100%',
  },

  // Loading state
  loading: {
    cursor: 'wait',
    // Keep same dimensions to prevent layout shift
  },

  // Content wrapper for proper spacing
  content: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalS,
  },

  // Hide content when loading (but keep in DOM for width)
  contentHidden: {
    visibility: 'hidden',
  },

  // Spinner overlay
  spinnerOverlay: {
    position: 'absolute',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalS,
  },

  // Icon styling
  icon: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
  },
});

/**
 * Maps AuraButtonSize to Fluent UI spinner size
 */
function getSpinnerSize(size: AuraButtonSize): 'extra-tiny' | 'tiny' | 'extra-small' {
  switch (size) {
    case 'small':
      return 'extra-tiny';
    case 'medium':
      return 'tiny';
    case 'large':
      return 'extra-small';
  }
}

/**
 * AuraButton component - the canonical button for Aura Video Studio
 *
 * Features:
 * - Four variants: primary, secondary, tertiary, destructive
 * - Three sizes: small, medium, large
 * - Loading state with spinner (no layout shift)
 * - Icon support (start and end positions)
 * - Full width option
 * - Accessible focus states
 * - Reduced motion support
 */
export const AuraButton = forwardRef<HTMLButtonElement, AuraButtonProps>(
  (
    {
      variant = 'primary',
      size = 'medium',
      loading = false,
      loadingText,
      iconStart,
      iconEnd,
      fullWidth = false,
      disabled,
      className,
      children,
      ...props
    },
    ref
  ) => {
    const styles = useStyles();

    const isDisabled = disabled || loading;

    // Build class names
    const buttonClassName = mergeClasses(
      styles.button,
      styles[size],
      styles[variant],
      fullWidth && styles.fullWidth,
      loading && styles.loading,
      className
    );

    const contentClassName = mergeClasses(styles.content, loading && styles.contentHidden);

    return (
      <Button
        // Fluent UI Button supports both anchor and button elements.
        // AuraButton is always rendered as a button, so we assert the ref type.
        // Using HTMLButtonElement directly as that's what AuraButton is declared with.
        ref={ref as ForwardedRef<HTMLButtonElement>}
        className={buttonClassName}
        disabled={isDisabled}
        aria-busy={loading}
        aria-disabled={isDisabled}
        {...(props as ButtonProps)}
      >
        {/* Content wrapper - hidden when loading but keeps width */}
        <span className={contentClassName}>
          {iconStart && <span className={styles.icon}>{iconStart}</span>}
          {children}
          {iconEnd && <span className={styles.icon}>{iconEnd}</span>}
        </span>

        {/* Loading overlay - shown when loading */}
        {loading && (
          <span className={styles.spinnerOverlay} aria-hidden="true">
            <Spinner size={getSpinnerSize(size)} />
            {loadingText ?? children}
          </span>
        )}
      </Button>
    );
  }
);

AuraButton.displayName = 'AuraButton';

export default AuraButton;
