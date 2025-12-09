/**
 * EnhancedSlider Component
 *
 * Professional slider with value input field and unit display.
 * Supports editable number input alongside the slider track.
 */

import { makeStyles, tokens, Slider, Input, Text, mergeClasses } from '@fluentui/react-components';
import { useState, useCallback, useEffect, useRef } from 'react';
import type { FC, ChangeEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export interface EnhancedSliderProps {
  /** Current value */
  value: number;
  /** Callback when value changes during dragging */
  onChange: (value: number) => void;
  /** Callback when value change is complete (for undo history) */
  onChangeComplete?: (value: number) => void;
  /** Minimum value */
  min: number;
  /** Maximum value */
  max: number;
  /** Step increment */
  step?: number;
  /** Unit display (e.g., '%', 'px', '°') */
  unit?: string;
  /** Custom value formatter for display */
  formatValue?: (value: number) => string;
  /** Whether to show the input field */
  showInput?: boolean;
  /** Width of the input field */
  inputWidth?: string;
  /** Whether the slider is disabled */
  disabled?: boolean;
  /** Additional class name */
  className?: string;
  /** Slider label for accessibility */
  label?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    width: '100%',
  },
  slider: {
    flex: 1,
    minWidth: '80px',
  },
  inputContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  input: {
    width: '56px',
    minWidth: '56px',
    textAlign: 'right',
  },
  valueDisplay: {
    minWidth: '40px',
    textAlign: 'right',
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground2,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  unit: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: tokens.colorNeutralForeground3,
    marginLeft: '2px',
  },
});

/**
 * EnhancedSlider combines a slider with an editable input field.
 * Supports units, custom formatting, and change complete callbacks.
 */
export const EnhancedSlider: FC<EnhancedSliderProps> = ({
  value,
  onChange,
  onChangeComplete,
  min,
  max,
  step = 1,
  unit,
  formatValue,
  showInput = true,
  inputWidth = '56px',
  disabled = false,
  className,
  label,
}) => {
  const styles = useStyles();
  const [inputValue, setInputValue] = useState(value.toString());
  const [isEditing, setIsEditing] = useState(false);
  const initialValueRef = useRef(value);

  // Sync input value with prop value when not editing
  useEffect(() => {
    if (!isEditing) {
      setInputValue(value.toString());
    }
  }, [value, isEditing]);

  const handleSliderChange = useCallback(
    (_event: ChangeEvent<HTMLInputElement>, data: { value: number }) => {
      onChange(data.value);
    },
    [onChange]
  );

  const handleSliderMouseDown = useCallback(() => {
    initialValueRef.current = value;
  }, [value]);

  const handleSliderMouseUp = useCallback(() => {
    if (value !== initialValueRef.current) {
      onChangeComplete?.(value);
    }
  }, [value, onChangeComplete]);

  const handleInputChange = useCallback(
    (_event: ChangeEvent<HTMLInputElement>, data: { value: string }) => {
      setInputValue(data.value);
    },
    []
  );

  const handleInputFocus = useCallback(() => {
    setIsEditing(true);
    initialValueRef.current = value;
  }, [value]);

  const handleInputBlur = useCallback(() => {
    setIsEditing(false);

    const numValue = parseFloat(inputValue);

    if (!isNaN(numValue)) {
      const clampedValue = Math.max(min, Math.min(max, numValue));
      onChange(clampedValue);

      if (clampedValue !== initialValueRef.current) {
        onChangeComplete?.(clampedValue);
      }

      setInputValue(clampedValue.toString());
    } else {
      setInputValue(value.toString());
    }
  }, [inputValue, min, max, value, onChange, onChangeComplete]);

  const handleInputKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLInputElement>) => {
      if (event.key === 'Enter') {
        (event.target as HTMLInputElement).blur();
      } else if (event.key === 'Escape') {
        setInputValue(value.toString());
        setIsEditing(false);
        (event.target as HTMLInputElement).blur();
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        const newValue = Math.min(max, value + step);
        onChange(newValue);
        setInputValue(newValue.toString());
      } else if (event.key === 'ArrowDown') {
        event.preventDefault();
        const newValue = Math.max(min, value - step);
        onChange(newValue);
        setInputValue(newValue.toString());
      }
    },
    [value, min, max, step, onChange]
  );

  const displayValue = formatValue ? formatValue(value) : value.toString();

  return (
    <div className={mergeClasses(styles.container, className)}>
      <Slider
        className={styles.slider}
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={handleSliderChange}
        onMouseDown={handleSliderMouseDown}
        onMouseUp={handleSliderMouseUp}
        disabled={disabled}
        size="small"
        aria-label={label}
      />

      {showInput ? (
        <div className={styles.inputContainer}>
          <Input
            className={styles.input}
            style={{ width: inputWidth }}
            type="number"
            size="small"
            value={inputValue}
            onChange={handleInputChange}
            onFocus={handleInputFocus}
            onBlur={handleInputBlur}
            onKeyDown={handleInputKeyDown}
            disabled={disabled}
            aria-label={label}
          />
          {unit && (
            <Text size={100} className={styles.unit}>
              {unit}
            </Text>
          )}
        </div>
      ) : (
        <Text className={styles.valueDisplay}>
          {displayValue}
          {unit && <span className={styles.unit}>{unit}</span>}
        </Text>
      )}
    </div>
  );
};

export default EnhancedSlider;
