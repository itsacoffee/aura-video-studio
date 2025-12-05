/**
 * AnimationEditor Component
 *
 * Animation parameter editor with duration slider, delay slider,
 * easing selector, type-specific parameters, and preview button.
 */

import {
  makeStyles,
  tokens,
  Text,
  Slider,
  Input,
  Button,
  Dropdown,
  Option,
  Tooltip,
} from '@fluentui/react-components';
import {
  Delete24Regular,
  Timer24Regular,
  Settings24Regular,
  Play24Regular,
} from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import {
  useTextAnimationsStore,
  type AppliedTextAnimation,
} from '../../../stores/opencutTextAnimations';

export interface AnimationEditorProps {
  /** The applied animation to edit */
  animation: AppliedTextAnimation;
  /** Callback when animation is removed */
  onRemove?: (animationId: string) => void;
  /** Callback when preview is requested */
  onPreview?: (animation: AppliedTextAnimation) => void;
  /** CSS class name */
  className?: string;
}

const EASING_OPTIONS = [
  { value: 'linear', label: 'Linear' },
  { value: 'ease-in', label: 'Ease In' },
  { value: 'ease-out', label: 'Ease Out' },
  { value: 'ease-in-out', label: 'Ease In-Out' },
  { value: 'ease-out-back', label: 'Ease Out Back' },
  { value: 'ease-out-bounce', label: 'Ease Out Bounce' },
];

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  headerIcon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  propertyRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    minHeight: '32px',
  },
  propertyLabel: {
    color: tokens.colorNeutralForeground3,
    minWidth: '64px',
    fontSize: tokens.fontSizeBase200,
  },
  sliderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  slider: {
    flex: 1,
    minWidth: '80px',
  },
  sliderValue: {
    minWidth: '40px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  propertyInputSmall: {
    width: '72px',
    minWidth: '72px',
  },
  dropdown: {
    minWidth: '120px',
  },
  colorInput: {
    width: '32px',
    height: '32px',
    padding: 0,
    border: 'none',
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    overflow: 'hidden',
  },
  buttonRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalS,
  },
  positionBadge: {
    fontSize: tokens.fontSizeBase100,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorNeutralForeground3,
  },
});

export const AnimationEditor: FC<AnimationEditorProps> = ({
  animation,
  onRemove,
  onPreview,
  className,
}) => {
  const styles = useStyles();
  const textAnimationsStore = useTextAnimationsStore();
  const preset = textAnimationsStore.getPreset(animation.presetId);

  const handleDurationChange = useCallback(
    (newDuration: number) => {
      textAnimationsStore.updateAnimation(animation.id, { duration: newDuration });
    },
    [textAnimationsStore, animation.id]
  );

  const handleDelayChange = useCallback(
    (newDelay: number) => {
      textAnimationsStore.updateAnimation(animation.id, { delay: newDelay });
    },
    [textAnimationsStore, animation.id]
  );

  const handleParameterChange = useCallback(
    (parameterId: string, value: string | number | boolean) => {
      textAnimationsStore.updateAnimation(animation.id, {
        parameters: {
          ...animation.parameters,
          [parameterId]: value,
        },
      });
    },
    [textAnimationsStore, animation.id, animation.parameters]
  );

  const handleRemove = useCallback(() => {
    textAnimationsStore.removeAnimation(animation.id);
    onRemove?.(animation.id);
  }, [textAnimationsStore, animation.id, onRemove]);

  const handlePreview = useCallback(() => {
    onPreview?.(animation);
  }, [onPreview, animation]);

  if (!preset) {
    return null;
  }

  const renderParameterControl = (paramId: string, paramValue: string | number | boolean) => {
    // Determine control type based on parameter name and value type
    const isColor = paramId.toLowerCase().includes('color');
    const isBoolean = typeof paramValue === 'boolean';

    if (isColor && typeof paramValue === 'string') {
      return (
        <input
          type="color"
          className={styles.colorInput}
          value={paramValue}
          onChange={(e) => handleParameterChange(paramId, e.target.value)}
          title={paramId}
        />
      );
    }

    if (isBoolean) {
      return (
        <Dropdown
          className={styles.dropdown}
          value={paramValue ? 'Yes' : 'No'}
          onOptionSelect={(_, data) => handleParameterChange(paramId, data.optionValue === 'Yes')}
          size="small"
        >
          <Option value="Yes">Yes</Option>
          <Option value="No">No</Option>
        </Dropdown>
      );
    }

    if (typeof paramValue === 'number') {
      return (
        <div className={styles.sliderRow}>
          <Slider
            className={styles.slider}
            min={0}
            max={paramId === 'stagger' ? 0.5 : 100}
            step={paramId === 'stagger' ? 0.01 : 1}
            value={paramValue}
            onChange={(_, data) => handleParameterChange(paramId, data.value)}
            size="small"
          />
          <Text className={styles.sliderValue}>
            {paramId === 'stagger' ? paramValue.toFixed(2) : paramValue}
          </Text>
        </div>
      );
    }

    return (
      <Input
        className={styles.propertyInputSmall}
        value={String(paramValue)}
        onChange={(_, data) => handleParameterChange(paramId, data.value)}
        size="small"
      />
    );
  };

  const formatParamLabel = (paramId: string): string => {
    return paramId
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim();
  };

  const positionLabel =
    animation.position === 'in' ? 'Entry' : animation.position === 'out' ? 'Exit' : 'Continuous';

  return (
    <div className={className ? `${styles.container} ${className}` : styles.container}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Settings24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={200}>
            {preset.name}
          </Text>
          <span className={styles.positionBadge}>{positionLabel}</span>
        </div>
        <div className={styles.headerActions}>
          {onPreview && (
            <Tooltip content="Preview animation" relationship="label">
              <Button
                appearance="subtle"
                icon={<Play24Regular />}
                size="small"
                onClick={handlePreview}
                aria-label="Preview animation"
              />
            </Tooltip>
          )}
        </div>
      </div>

      {/* Duration */}
      <div className={styles.propertyRow}>
        <Text size={200} className={styles.propertyLabel}>
          <Timer24Regular style={{ fontSize: '14px', marginRight: '4px' }} />
          Duration
        </Text>
        <div className={styles.sliderRow}>
          <Slider
            className={styles.slider}
            min={0.1}
            max={10}
            step={0.1}
            value={animation.duration}
            onChange={(_, data) => handleDurationChange(data.value)}
            size="small"
          />
          <Text className={styles.sliderValue}>{animation.duration.toFixed(1)}s</Text>
        </div>
      </div>

      {/* Delay */}
      <div className={styles.propertyRow}>
        <Text size={200} className={styles.propertyLabel}>
          Delay
        </Text>
        <div className={styles.sliderRow}>
          <Slider
            className={styles.slider}
            min={0}
            max={5}
            step={0.1}
            value={animation.delay}
            onChange={(_, data) => handleDelayChange(data.value)}
            size="small"
          />
          <Text className={styles.sliderValue}>{animation.delay.toFixed(1)}s</Text>
        </div>
      </div>

      {/* Easing (from preset) */}
      <div className={styles.propertyRow}>
        <Text size={200} className={styles.propertyLabel}>
          Easing
        </Text>
        <Dropdown
          className={styles.dropdown}
          value={EASING_OPTIONS.find((e) => e.value === preset.easing)?.label || preset.easing}
          size="small"
          disabled
        >
          {EASING_OPTIONS.map((option) => (
            <Option key={option.value} value={option.value}>
              {option.label}
            </Option>
          ))}
        </Dropdown>
      </div>

      {/* Dynamic parameters */}
      {Object.entries(animation.parameters).map(([paramId, paramValue]) => (
        <div key={paramId} className={styles.propertyRow}>
          <Text size={200} className={styles.propertyLabel}>
            {formatParamLabel(paramId)}
          </Text>
          {renderParameterControl(paramId, paramValue)}
        </div>
      ))}

      <div className={styles.buttonRow}>
        <Tooltip content="Remove this animation" relationship="label">
          <Button
            appearance="subtle"
            icon={<Delete24Regular />}
            size="small"
            onClick={handleRemove}
          >
            Remove
          </Button>
        </Tooltip>
      </div>
    </div>
  );
};

export default AnimationEditor;
