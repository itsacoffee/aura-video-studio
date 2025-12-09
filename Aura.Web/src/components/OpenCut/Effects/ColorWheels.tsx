/**
 * ColorWheels Component
 *
 * Professional color grading wheels for Lift/Gamma/Gain adjustment.
 * Provides visual color wheel interface for color correction.
 */

import { makeStyles, tokens, Text, Slider, Button, Tooltip } from '@fluentui/react-components';
import { ArrowReset24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useRef, useEffect } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export interface ColorWheelValue {
  x: number; // -1 to 1 (horizontal offset)
  y: number; // -1 to 1 (vertical offset)
  intensity: number; // 0 to 2 (multiplier)
}

export interface ColorWheelsProps {
  lift: ColorWheelValue;
  gamma: ColorWheelValue;
  gain: ColorWheelValue;
  onLiftChange: (value: ColorWheelValue) => void;
  onGammaChange: (value: ColorWheelValue) => void;
  onGainChange: (value: ColorWheelValue) => void;
  onReset?: () => void;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingHorizontalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  wheelsContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'space-between',
  },
  wheelSection: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
    flex: 1,
  },
  wheelLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontWeight: 500,
  },
  wheelWrapper: {
    position: 'relative',
    width: '100px',
    height: '100px',
  },
  wheel: {
    width: '100%',
    height: '100%',
    borderRadius: '50%',
    background: 'conic-gradient(from 180deg, red, yellow, lime, aqua, blue, magenta, red)',
    cursor: 'pointer',
    position: 'relative',
    overflow: 'hidden',
    boxShadow: 'inset 0 0 10px rgba(0, 0, 0, 0.3)',
  },
  wheelOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    borderRadius: '50%',
    background: 'radial-gradient(circle, white 0%, transparent 70%)',
    pointerEvents: 'none',
  },
  wheelIndicator: {
    position: 'absolute',
    width: '12px',
    height: '12px',
    borderRadius: '50%',
    backgroundColor: 'white',
    border: '2px solid rgba(0, 0, 0, 0.5)',
    transform: 'translate(-50%, -50%)',
    boxShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
    pointerEvents: 'none',
  },
  intensitySlider: {
    width: '100%',
    marginTop: tokens.spacingVerticalXS,
  },
  intensityValue: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  resetButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  offsetValues: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
});

interface SingleWheelProps {
  label: string;
  value: ColorWheelValue;
  onChange: (value: ColorWheelValue) => void;
}

const SingleWheel: FC<SingleWheelProps> = ({ label, value, onChange }) => {
  const styles = useStyles();
  const wheelRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const handleMouseDown = useCallback((e: ReactMouseEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const updatePosition = useCallback(
    (clientX: number, clientY: number) => {
      if (!wheelRef.current) return;

      const rect = wheelRef.current.getBoundingClientRect();
      const centerX = rect.left + rect.width / 2;
      const centerY = rect.top + rect.height / 2;
      const radius = rect.width / 2;

      let x = (clientX - centerX) / radius;
      let y = (clientY - centerY) / radius;

      // Clamp to circle
      const distance = Math.sqrt(x * x + y * y);
      if (distance > 1) {
        x /= distance;
        y /= distance;
      }

      onChange({ ...value, x, y });
    },
    [value, onChange]
  );

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      updatePosition(e.clientX, e.clientY);
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, updatePosition]);

  const handleClick = useCallback(
    (e: ReactMouseEvent<HTMLDivElement>) => {
      updatePosition(e.clientX, e.clientY);
    },
    [updatePosition]
  );

  const handleIntensityChange = useCallback(
    (_: unknown, data: { value: number }) => {
      onChange({ ...value, intensity: data.value });
    },
    [value, onChange]
  );

  // Calculate indicator position
  const indicatorX = 50 + value.x * 42; // 42 = 50 - indicator radius
  const indicatorY = 50 + value.y * 42;

  return (
    <div className={styles.wheelSection}>
      <Text className={styles.wheelLabel}>{label}</Text>
      <div className={styles.wheelWrapper}>
        <div
          ref={wheelRef}
          className={styles.wheel}
          onMouseDown={handleMouseDown}
          onClick={handleClick}
          role="slider"
          aria-label={`${label} color wheel`}
          aria-valuemin={-1}
          aria-valuemax={1}
          aria-valuenow={value.x}
          tabIndex={0}
        >
          <div className={styles.wheelOverlay} />
          <div
            className={styles.wheelIndicator}
            style={{
              left: `${indicatorX}%`,
              top: `${indicatorY}%`,
            }}
          />
        </div>
      </div>
      <div className={styles.offsetValues}>
        <span>X: {value.x.toFixed(2)}</span>
        <span>Y: {value.y.toFixed(2)}</span>
      </div>
      <Slider
        className={styles.intensitySlider}
        min={0}
        max={2}
        step={0.01}
        value={value.intensity}
        onChange={handleIntensityChange}
        size="small"
      />
      <Text className={styles.intensityValue}>{value.intensity.toFixed(2)}</Text>
    </div>
  );
};

export const ColorWheels: FC<ColorWheelsProps> = ({
  lift,
  gamma,
  gain,
  onLiftChange,
  onGammaChange,
  onGainChange,
  onReset,
  className,
}) => {
  const styles = useStyles();

  return (
    <div className={`${styles.container} ${className || ''}`}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Text weight="semibold" size={200}>
            Color Wheels
          </Text>
        </div>
        {onReset && (
          <Tooltip content="Reset color wheels" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ArrowReset24Regular />}
              className={styles.resetButton}
              onClick={onReset}
              aria-label="Reset color wheels"
            />
          </Tooltip>
        )}
      </div>
      <div className={styles.wheelsContainer}>
        <SingleWheel label="Lift" value={lift} onChange={onLiftChange} />
        <SingleWheel label="Gamma" value={gamma} onChange={onGammaChange} />
        <SingleWheel label="Gain" value={gain} onChange={onGainChange} />
      </div>
    </div>
  );
};

export default ColorWheels;
