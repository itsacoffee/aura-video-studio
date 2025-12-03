/**
 * ColorPicker Component
 *
 * Color selection with preset colors and hex input.
 * Uses a popover-based interface for color selection.
 */

import {
  makeStyles,
  tokens,
  Input,
  Popover,
  PopoverTrigger,
  PopoverSurface,
  Text,
  mergeClasses,
} from '@fluentui/react-components';
import { useState, useCallback, useRef, useEffect } from 'react';
import type { FC, ChangeEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import { colorPickerPresets } from '../../../styles/openCutTheme';

export interface ColorPickerProps {
  /** Current color value (hex format) */
  value: string;
  /** Callback when color changes */
  onChange: (color: string) => void;
  /** Callback when color selection is complete */
  onChangeComplete?: (color: string) => void;
  /** Custom preset colors */
  presets?: string[];
  /** Whether to show the hex input */
  showInput?: boolean;
  /** Whether to show the native color picker */
  showNativePicker?: boolean;
  /** Whether the picker is disabled */
  disabled?: boolean;
  /** Size of the color swatch trigger */
  size?: 'small' | 'medium' | 'large';
  /** Additional class name */
  className?: string;
  /** Label for accessibility */
  label?: string;
}

const useStyles = makeStyles({
  trigger: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: openCutTokens.radius.sm,
    cursor: 'pointer',
    overflow: 'hidden',
    transition: `box-shadow ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      boxShadow: openCutTokens.shadows.sm,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
  },
  triggerSmall: {
    width: '24px',
    height: '24px',
  },
  triggerMedium: {
    width: '32px',
    height: '32px',
  },
  triggerLarge: {
    width: '40px',
    height: '40px',
  },
  triggerDisabled: {
    opacity: 0.5,
    cursor: 'not-allowed',
  },
  swatch: {
    width: '100%',
    height: '100%',
    display: 'block',
  },
  surface: {
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.md,
    minWidth: '200px',
  },
  presetsLabel: {
    color: tokens.colorNeutralForeground3,
    marginBottom: openCutTokens.spacing.xxs,
  },
  presetsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(8, 1fr)',
    gap: '4px',
  },
  presetColor: {
    width: '24px',
    height: '24px',
    borderRadius: openCutTokens.radius.xs,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    cursor: 'pointer',
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}, box-shadow ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      transform: 'scale(1.1)',
      boxShadow: openCutTokens.shadows.sm,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '1px',
    },
  },
  presetColorSelected: {
    boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
  },
  inputRow: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  hexLabel: {
    color: tokens.colorNeutralForeground3,
    minWidth: '24px',
  },
  hexInput: {
    flex: 1,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  nativePickerContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  nativePicker: {
    width: '32px',
    height: '32px',
    padding: 0,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: openCutTokens.radius.sm,
    cursor: 'pointer',
    overflow: 'hidden',
  },
});

/**
 * Validates a hex color string.
 */
function isValidHexColor(color: string): boolean {
  return /^#[0-9A-Fa-f]{6}$/.test(color);
}

/**
 * Normalizes a hex color to 6-digit format with hash.
 */
function normalizeHexColor(color: string): string {
  let hex = color.replace('#', '').toUpperCase();

  // Handle 3-digit hex
  if (hex.length === 3) {
    hex = hex
      .split('')
      .map((c) => c + c)
      .join('');
  }

  return `#${hex}`;
}

/**
 * ColorPicker provides color selection with presets and hex input.
 * Uses a popover interface for a professional editing experience.
 */
export const ColorPicker: FC<ColorPickerProps> = ({
  value,
  onChange,
  onChangeComplete,
  presets = colorPickerPresets.basic,
  showInput = true,
  showNativePicker = true,
  disabled = false,
  size = 'medium',
  className,
  label = 'Color picker',
}) => {
  const styles = useStyles();
  const [isOpen, setIsOpen] = useState(false);
  const [hexInput, setHexInput] = useState(value);
  const initialValueRef = useRef(value);

  // Sync hex input with prop value when not editing
  useEffect(() => {
    setHexInput(value);
  }, [value]);

  const handleOpenChange = useCallback(
    (_event: unknown, data: { open: boolean }) => {
      setIsOpen(data.open);

      if (data.open) {
        initialValueRef.current = value;
      } else if (value !== initialValueRef.current) {
        onChangeComplete?.(value);
      }
    },
    [value, onChangeComplete]
  );

  const handlePresetClick = useCallback(
    (color: string) => {
      onChange(color);
    },
    [onChange]
  );

  const handleHexInputChange = useCallback(
    (_event: ChangeEvent<HTMLInputElement>, data: { value: string }) => {
      setHexInput(data.value);

      // Auto-add hash if missing
      let colorValue = data.value;

      if (!colorValue.startsWith('#') && colorValue.length > 0) {
        colorValue = `#${colorValue}`;
      }

      // Validate and apply
      if (isValidHexColor(colorValue)) {
        onChange(normalizeHexColor(colorValue));
      }
    },
    [onChange]
  );

  const handleHexInputBlur = useCallback(() => {
    // Reset to current value if invalid
    if (!isValidHexColor(hexInput)) {
      setHexInput(value);
    }
  }, [hexInput, value]);

  const handleNativePickerChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const color = event.target.value.toUpperCase();
      onChange(color);
      setHexInput(color);
    },
    [onChange]
  );

  const sizeClass = {
    small: styles.triggerSmall,
    medium: styles.triggerMedium,
    large: styles.triggerLarge,
  }[size];

  return (
    <Popover open={isOpen} onOpenChange={handleOpenChange} positioning="below-start">
      <PopoverTrigger disableButtonEnhancement>
        <button
          type="button"
          className={mergeClasses(
            styles.trigger,
            sizeClass,
            disabled && styles.triggerDisabled,
            className
          )}
          disabled={disabled}
          aria-label={label}
          title={value}
        >
          <span className={styles.swatch} style={{ backgroundColor: value }} />
        </button>
      </PopoverTrigger>
      <PopoverSurface className={styles.surface}>
        {/* Preset Colors */}
        <div>
          <Text size={100} className={styles.presetsLabel}>
            Presets
          </Text>
          <div className={styles.presetsGrid}>
            {presets.map((color) => (
              <button
                key={color}
                type="button"
                className={mergeClasses(
                  styles.presetColor,
                  value.toUpperCase() === color.toUpperCase() && styles.presetColorSelected
                )}
                style={{ backgroundColor: color }}
                onClick={() => handlePresetClick(color)}
                aria-label={`Select color ${color}`}
              />
            ))}
          </div>
        </div>

        {/* Hex Input */}
        {showInput && (
          <div className={styles.inputRow}>
            <Text size={100} className={styles.hexLabel}>
              Hex
            </Text>
            <Input
              className={styles.hexInput}
              size="small"
              value={hexInput}
              onChange={handleHexInputChange}
              onBlur={handleHexInputBlur}
              maxLength={7}
            />
          </div>
        )}

        {/* Native Color Picker */}
        {showNativePicker && (
          <div className={styles.nativePickerContainer}>
            <Text size={100} className={styles.hexLabel}>
              Custom
            </Text>
            <input
              type="color"
              className={styles.nativePicker}
              value={value}
              onChange={handleNativePickerChange}
              title="Open color picker"
            />
          </div>
        )}
      </PopoverSurface>
    </Popover>
  );
};

export default ColorPicker;
