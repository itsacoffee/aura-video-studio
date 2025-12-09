/**
 * TransitionEditor Component
 *
 * Properties editor for transitions showing duration slider,
 * parameter controls, and remove button.
 */

import {
  makeStyles,
  tokens,
  Text,
  Slider,
  Input,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import { Delete24Regular, Timer24Regular, Settings24Regular } from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import {
  useOpenCutTransitionsStore,
  type ClipTransition,
} from '../../../stores/opencutTransitions';

export interface TransitionEditorProps {
  /** The applied transition to edit */
  transition: ClipTransition;
  /** Callback when transition is removed */
  onRemove?: (transitionId: string) => void;
}

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
  colorInput: {
    width: '32px',
    height: '32px',
    padding: 0,
    border: 'none',
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    overflow: 'hidden',
  },
  removeButton: {
    marginTop: tokens.spacingVerticalS,
  },
});

export const TransitionEditor: FC<TransitionEditorProps> = ({ transition, onRemove }) => {
  const styles = useStyles();
  const transitionsStore = useOpenCutTransitionsStore();
  const definition = transitionsStore.getTransitionDefinition(transition.transitionId);

  const handleDurationChange = useCallback(
    (newDuration: number) => {
      transitionsStore.updateTransition(transition.id, { duration: newDuration });
    },
    [transitionsStore, transition.id]
  );

  const handleParameterChange = useCallback(
    (parameterId: string, value: string | number | boolean) => {
      transitionsStore.updateTransition(transition.id, {
        parameters: {
          ...transition.parameters,
          [parameterId]: value,
        },
      });
    },
    [transitionsStore, transition.id, transition.parameters]
  );

  const handleRemove = useCallback(() => {
    transitionsStore.removeTransition(transition.id);
    onRemove?.(transition.id);
  }, [transitionsStore, transition.id, onRemove]);

  if (!definition) {
    return null;
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Settings24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={200}>
            {definition.name}
          </Text>
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
            max={5}
            step={0.1}
            value={transition.duration}
            onChange={(_, data) => handleDurationChange(data.value)}
            size="small"
          />
          <Text className={styles.sliderValue}>{transition.duration.toFixed(1)}s</Text>
        </div>
      </div>

      {/* Dynamic parameters */}
      {definition.parameters.map((param) => {
        const value = transition.parameters[param.id] ?? param.defaultValue;

        return (
          <div key={param.id} className={styles.propertyRow}>
            <Text size={200} className={styles.propertyLabel}>
              {param.name}
            </Text>
            {param.type === 'color' ? (
              <input
                type="color"
                className={styles.colorInput}
                value={String(value)}
                onChange={(e) => handleParameterChange(param.id, e.target.value)}
                title={param.name}
              />
            ) : param.type === 'number' || param.type === 'range' ? (
              <div className={styles.sliderRow}>
                <Slider
                  className={styles.slider}
                  min={param.min ?? 0}
                  max={param.max ?? 100}
                  step={param.step ?? 1}
                  value={Number(value)}
                  onChange={(_, data) => handleParameterChange(param.id, data.value)}
                  size="small"
                />
                <Text className={styles.sliderValue}>
                  {Number(value)}
                  {param.unit || ''}
                </Text>
              </div>
            ) : (
              <Input
                className={styles.propertyInputSmall}
                value={String(value)}
                onChange={(_, data) => handleParameterChange(param.id, data.value)}
                size="small"
              />
            )}
          </div>
        );
      })}

      <Tooltip content="Remove this transition" relationship="label">
        <Button
          appearance="subtle"
          icon={<Delete24Regular />}
          size="small"
          className={styles.removeButton}
          onClick={handleRemove}
        >
          Remove Transition
        </Button>
      </Tooltip>
    </div>
  );
};

export default TransitionEditor;
