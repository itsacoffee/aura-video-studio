/**
 * IconButton Component
 *
 * Standardized icon button with tooltip and multiple variants.
 * Provides consistent styling and accessibility across the editor.
 */

import { makeStyles, tokens, Button, Tooltip, mergeClasses } from '@fluentui/react-components';
import type { FC, ReactNode, MouseEventHandler } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export type IconButtonSize = 'small' | 'medium' | 'large';
export type IconButtonAppearance = 'subtle' | 'outline' | 'primary';

export interface IconButtonProps {
  /** Icon to display */
  icon: ReactNode;
  /** Tooltip label */
  label: string;
  /** Keyboard shortcut to display in tooltip */
  shortcut?: string;
  /** Button size variant */
  size?: IconButtonSize;
  /** Button appearance variant */
  appearance?: IconButtonAppearance;
  /** Whether the button is in active/pressed state */
  active?: boolean;
  /** Whether to show the tooltip */
  showTooltip?: boolean;
  /** Additional class name */
  className?: string;
  /** Whether the button is disabled */
  disabled?: boolean;
  /** Click handler */
  onClick?: MouseEventHandler<HTMLButtonElement>;
}

const useStyles = makeStyles({
  small: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  medium: {
    minWidth: '32px',
    minHeight: '32px',
    padding: '4px',
  },
  large: {
    minWidth: '40px',
    minHeight: '40px',
    padding: '6px',
  },
  active: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    color: tokens.colorBrandForeground1,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  tooltipContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  shortcut: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground3,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
});

/**
 * IconButton provides a consistent icon button with tooltip support.
 * Includes keyboard shortcut display and active state styling.
 */
export const IconButton: FC<IconButtonProps> = ({
  icon,
  label,
  shortcut,
  size = 'medium',
  appearance = 'subtle',
  active = false,
  showTooltip = true,
  className,
  disabled,
  onClick,
}) => {
  const styles = useStyles();

  const sizeClass = {
    small: styles.small,
    medium: styles.medium,
    large: styles.large,
  }[size];

  const fluentSize = size === 'large' ? 'medium' : 'small';

  const button = (
    <Button
      appearance={appearance === 'primary' ? 'primary' : appearance}
      size={fluentSize}
      icon={<span>{icon}</span>}
      disabled={disabled}
      onClick={onClick}
      className={mergeClasses(sizeClass, active && styles.active, className)}
      aria-label={label}
      aria-pressed={active}
    />
  );

  if (!showTooltip) {
    return button;
  }

  const tooltipContent = shortcut ? (
    <div className={styles.tooltipContent}>
      <span>{label}</span>
      <span className={styles.shortcut}>{shortcut}</span>
    </div>
  ) : (
    label
  );

  return (
    <Tooltip content={tooltipContent} relationship="label">
      {button}
    </Tooltip>
  );
};

export default IconButton;
