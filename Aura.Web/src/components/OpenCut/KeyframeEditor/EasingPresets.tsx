/**
 * EasingPresets Component
 *
 * Dropdown selector for easing curve presets with visual previews.
 * Groups easing types into categories and shows a small curve visualization.
 */

import {
  makeStyles,
  tokens,
  Dropdown,
  Option,
  Text,
  OptionGroup,
} from '@fluentui/react-components';
import { useCallback, useMemo } from 'react';
import type { FC } from 'react';
import type { EasingType } from '../../../stores/opencutKeyframes';

export interface EasingPresetsProps {
  /** Currently selected easing type */
  value: EasingType;
  /** Called when easing is changed */
  onChange: (easing: EasingType) => void;
  /** Whether the selector is disabled */
  disabled?: boolean;
  /** Size variant */
  size?: 'small' | 'medium' | 'large';
  /** Additional class name */
  className?: string;
}

interface EasingOption {
  value: EasingType;
  label: string;
  category: string;
}

const EASING_OPTIONS: EasingOption[] = [
  { value: 'linear', label: 'Linear', category: 'Linear' },
  { value: 'hold', label: 'Hold', category: 'Linear' },
  { value: 'ease-in', label: 'Ease In', category: 'Ease' },
  { value: 'ease-out', label: 'Ease Out', category: 'Ease' },
  { value: 'ease-in-out', label: 'Ease In Out', category: 'Ease' },
  { value: 'ease-in-back', label: 'Ease In Back', category: 'Back' },
  { value: 'ease-out-back', label: 'Ease Out Back', category: 'Back' },
  { value: 'ease-in-elastic', label: 'Ease In Elastic', category: 'Elastic' },
  { value: 'ease-out-elastic', label: 'Ease Out Elastic', category: 'Elastic' },
  { value: 'ease-in-bounce', label: 'Ease In Bounce', category: 'Bounce' },
  { value: 'ease-out-bounce', label: 'Ease Out Bounce', category: 'Bounce' },
  { value: 'bezier', label: 'Custom Bezier', category: 'Custom' },
];

const useStyles = makeStyles({
  dropdown: {
    minWidth: '140px',
  },
  option: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  curvePreview: {
    width: '24px',
    height: '16px',
    flexShrink: 0,
  },
  curvePath: {
    fill: 'none',
    stroke: tokens.colorNeutralForeground2,
    strokeWidth: 1.5,
  },
});

/**
 * Generate SVG path data for an easing curve preview
 */
function getEasingPath(easing: EasingType): string {
  const points: { x: number; y: number }[] = [];
  const steps = 20;

  for (let i = 0; i <= steps; i++) {
    const t = i / steps;
    let y: number;

    switch (easing) {
      case 'linear':
        y = t;
        break;
      case 'hold':
        y = t < 1 ? 0 : 1;
        break;
      case 'ease-in':
        y = t * t;
        break;
      case 'ease-out':
        y = t * (2 - t);
        break;
      case 'ease-in-out':
        y = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        break;
      case 'ease-in-back':
        y = t * t * (2.70158 * t - 1.70158);
        break;
      case 'ease-out-back': {
        const t1 = t - 1;
        y = 1 + t1 * t1 * (2.70158 * t1 + 1.70158);
        break;
      }
      case 'ease-in-elastic':
        y = t === 0 || t === 1 ? t : -Math.pow(2, 10 * (t - 1)) * Math.sin((t - 1.1) * 5 * Math.PI);
        break;
      case 'ease-out-elastic':
        y = t === 0 || t === 1 ? t : Math.pow(2, -10 * t) * Math.sin((t - 0.1) * 5 * Math.PI) + 1;
        break;
      case 'ease-in-bounce':
        y = 1 - getEaseOutBounce(1 - t);
        break;
      case 'ease-out-bounce':
        y = getEaseOutBounce(t);
        break;
      case 'bezier':
        // Show a generic S-curve for bezier
        y = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        break;
      default:
        y = t;
    }

    // Clamp y to valid range for display
    y = Math.max(0, Math.min(1, y));
    points.push({ x: t * 24, y: (1 - y) * 16 });
  }

  return points
    .map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)} ${p.y.toFixed(1)}`)
    .join(' ');
}

function getEaseOutBounce(t: number): number {
  if (t < 1 / 2.75) return 7.5625 * t * t;
  if (t < 2 / 2.75) {
    const t1 = t - 1.5 / 2.75;
    return 7.5625 * t1 * t1 + 0.75;
  }
  if (t < 2.5 / 2.75) {
    const t1 = t - 2.25 / 2.75;
    return 7.5625 * t1 * t1 + 0.9375;
  }
  const t1 = t - 2.625 / 2.75;
  return 7.5625 * t1 * t1 + 0.984375;
}

const EasingCurvePreview: FC<{ easing: EasingType; className?: string }> = ({
  easing,
  className,
}) => {
  const styles = useStyles();
  const path = useMemo(() => getEasingPath(easing), [easing]);

  return (
    <svg className={`${styles.curvePreview} ${className || ''}`} viewBox="0 0 24 16">
      <path className={styles.curvePath} d={path} />
    </svg>
  );
};

export const EasingPresets: FC<EasingPresetsProps> = ({
  value,
  onChange,
  disabled = false,
  size = 'small',
  className,
}) => {
  const styles = useStyles();

  const handleChange = useCallback(
    (_: unknown, data: { optionValue?: string }) => {
      if (data.optionValue) {
        onChange(data.optionValue as EasingType);
      }
    },
    [onChange]
  );

  const selectedLabel = EASING_OPTIONS.find((opt) => opt.value === value)?.label || 'Select Easing';

  // Group options by category
  const groupedOptions = useMemo(() => {
    const groups: Record<string, EasingOption[]> = {};
    EASING_OPTIONS.forEach((opt) => {
      if (!groups[opt.category]) {
        groups[opt.category] = [];
      }
      groups[opt.category].push(opt);
    });
    return groups;
  }, []);

  return (
    <Dropdown
      className={`${styles.dropdown} ${className || ''}`}
      value={selectedLabel}
      onOptionSelect={handleChange}
      disabled={disabled}
      size={size}
    >
      {Object.entries(groupedOptions).map(([category, options]) => (
        <OptionGroup key={category} label={category}>
          {options.map((opt) => (
            <Option key={opt.value} value={opt.value} text={opt.label}>
              <div className={styles.option}>
                <EasingCurvePreview easing={opt.value} />
                <Text size={200}>{opt.label}</Text>
              </div>
            </Option>
          ))}
        </OptionGroup>
      ))}
    </Dropdown>
  );
};

export default EasingPresets;
