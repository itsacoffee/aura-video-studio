/**
 * KeyframeDiamond Component
 *
 * Visual representation of a keyframe point. Shows as a diamond shape
 * that can be selected, dragged, and indicates whether the property
 * has an active keyframe at the current time.
 */

import { makeStyles, tokens, mergeClasses, Tooltip } from '@fluentui/react-components';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';

export interface KeyframeDiamondProps {
  /** Whether this keyframe is selected */
  isSelected?: boolean;
  /** Whether this represents an existing keyframe (filled) vs potential keyframe (outline) */
  isActive?: boolean;
  /** Size of the diamond in pixels */
  size?: 'small' | 'medium' | 'large';
  /** Color variant */
  color?: 'default' | 'position' | 'scale' | 'rotation' | 'opacity' | 'audio';
  /** Click handler */
  onClick?: (e: ReactMouseEvent) => void;
  /** Double-click handler */
  onDoubleClick?: (e: ReactMouseEvent) => void;
  /** Mouse down handler for dragging */
  onMouseDown?: (e: ReactMouseEvent) => void;
  /** Tooltip text */
  tooltip?: string;
  /** Additional class name */
  className?: string;
  /** Accessible label */
  ariaLabel?: string;
  /** Whether the diamond is disabled */
  disabled?: boolean;
}

const useStyles = makeStyles({
  diamond: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    transition: 'transform 100ms ease-out, background-color 100ms ease-out',
    userSelect: 'none',
    ':hover': {
      transform: 'scale(1.15)',
    },
    ':active': {
      transform: 'scale(0.95)',
    },
  },
  diamondDisabled: {
    cursor: 'default',
    opacity: 0.5,
    ':hover': {
      transform: 'none',
    },
  },
  svg: {
    display: 'block',
  },
  // Size variants
  small: {
    width: '12px',
    height: '12px',
  },
  medium: {
    width: '16px',
    height: '16px',
  },
  large: {
    width: '20px',
    height: '20px',
  },
  // Selected state
  selected: {
    filter: `drop-shadow(0 0 4px ${tokens.colorBrandBackground})`,
  },
});

const COLOR_MAP: Record<
  NonNullable<KeyframeDiamondProps['color']>,
  { fill: string; stroke: string }
> = {
  default: {
    fill: tokens.colorBrandBackground,
    stroke: tokens.colorBrandStroke1,
  },
  position: {
    fill: tokens.colorPaletteRedBackground3,
    stroke: tokens.colorPaletteRedBorder1,
  },
  scale: {
    fill: tokens.colorPaletteGreenBackground3,
    stroke: tokens.colorPaletteGreenBorder1,
  },
  rotation: {
    fill: tokens.colorPalettePurpleBackground2,
    stroke: tokens.colorPalettePurpleBorderActive,
  },
  opacity: {
    fill: tokens.colorPaletteYellowBackground3,
    stroke: tokens.colorPaletteYellowBorderActive,
  },
  audio: {
    fill: tokens.colorPaletteTealBackground2,
    stroke: tokens.colorPaletteTealBorderActive,
  },
};

export const KeyframeDiamond: FC<KeyframeDiamondProps> = ({
  isSelected = false,
  isActive = false,
  size = 'medium',
  color = 'default',
  onClick,
  onDoubleClick,
  onMouseDown,
  tooltip,
  className,
  ariaLabel = 'Keyframe',
  disabled = false,
}) => {
  const styles = useStyles();
  const colors = COLOR_MAP[color];

  const sizeClass = {
    small: styles.small,
    medium: styles.medium,
    large: styles.large,
  }[size];

  const handleClick = (e: ReactMouseEvent) => {
    if (disabled) return;
    onClick?.(e);
  };

  const handleDoubleClick = (e: ReactMouseEvent) => {
    if (disabled) return;
    onDoubleClick?.(e);
  };

  const handleMouseDown = (e: ReactMouseEvent) => {
    if (disabled) return;
    onMouseDown?.(e);
  };

  const diamond = (
    <span
      className={mergeClasses(
        styles.diamond,
        sizeClass,
        isSelected && styles.selected,
        disabled && styles.diamondDisabled,
        className
      )}
      onClick={handleClick}
      onDoubleClick={handleDoubleClick}
      onMouseDown={handleMouseDown}
      role="button"
      tabIndex={disabled ? -1 : 0}
      aria-label={ariaLabel}
      aria-pressed={isSelected}
      aria-disabled={disabled}
      onKeyDown={(e) => {
        if (disabled) return;
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.(e as unknown as ReactMouseEvent);
        }
      }}
    >
      <svg
        className={styles.svg}
        viewBox="0 0 16 16"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M8 1L15 8L8 15L1 8L8 1Z"
          fill={isActive ? colors.fill : 'transparent'}
          stroke={colors.stroke}
          strokeWidth="1.5"
        />
      </svg>
    </span>
  );

  if (tooltip) {
    return (
      <Tooltip content={tooltip} relationship="label">
        {diamond}
      </Tooltip>
    );
  }

  return diamond;
};

export default KeyframeDiamond;
