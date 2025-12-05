/**
 * EffectControls Component
 *
 * Dynamic parameter controls for an applied effect.
 * Renders sliders, inputs, and color pickers based on parameter types.
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
import { ArrowReset24Regular } from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import { useOpenCutEffectsStore, type AppliedEffect } from '../../../stores/opencutEffects';
import { useOpenCutKeyframesStore } from '../../../stores/opencutKeyframes';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { KeyframeDiamond } from '../KeyframeEditor';

export interface EffectControlsProps {
  effect: AppliedEffect;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingHorizontalM,
  },
  propertyRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    minHeight: '32px',
  },
  propertyLabel: {
    color: tokens.colorNeutralForeground3,
    minWidth: '80px',
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
    minWidth: '50px',
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
  keyframeButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  resetButton: {
    marginTop: tokens.spacingVerticalS,
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    paddingTop: tokens.spacingVerticalS,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
});

export const EffectControls: FC<EffectControlsProps> = ({ effect, className }) => {
  const styles = useStyles();
  const effectsStore = useOpenCutEffectsStore();
  const keyframesStore = useOpenCutKeyframesStore();
  const playbackStore = useOpenCutPlaybackStore();

  const definition = effectsStore.getEffectDefinition(effect.effectId);
  const currentTime = playbackStore.currentTime;

  const handleParameterChange = useCallback(
    (paramId: string, value: string | number | boolean) => {
      effectsStore.updateEffectParameter(effect.id, paramId, value);
    },
    [effectsStore, effect.id]
  );

  const handleReset = useCallback(() => {
    effectsStore.resetEffectParameters(effect.id);
  }, [effectsStore, effect.id]);

  const hasKeyframeAt = useCallback(
    (paramId: string): boolean => {
      const propertyKey = `effect-${effect.id}-${paramId}`;
      const kf = keyframesStore.getKeyframeAtTime(effect.clipId, propertyKey, currentTime);
      return !!kf;
    },
    [effect.id, effect.clipId, keyframesStore, currentTime]
  );

  const handleKeyframeToggle = useCallback(
    (paramId: string, value: number) => {
      const propertyKey = `effect-${effect.id}-${paramId}`;
      const existingKf = keyframesStore.getKeyframeAtTime(effect.clipId, propertyKey, currentTime);
      if (existingKf) {
        keyframesStore.removeKeyframe(existingKf.id);
      } else {
        keyframesStore.addKeyframe(effect.clipId, propertyKey, currentTime, value);
      }
    },
    [effect.id, effect.clipId, keyframesStore, currentTime]
  );

  if (!definition) {
    return null;
  }

  const formatValue = (value: number, unit?: string, step?: number): string => {
    const precision = step && step < 1 ? Math.ceil(-Math.log10(step)) : 0;
    return `${value.toFixed(precision)}${unit || ''}`;
  };

  return (
    <div className={`${styles.container} ${className || ''}`}>
      {definition.parameters.map((param) => {
        const value = effect.parameters[param.id] ?? param.defaultValue;

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
            ) : param.type === 'boolean' ? (
              <input
                type="checkbox"
                checked={Boolean(value)}
                onChange={(e) => handleParameterChange(param.id, e.target.checked)}
                aria-label={param.name}
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
                  {formatValue(Number(value), param.unit, param.step)}
                </Text>
                {param.keyframeable && (
                  <Tooltip
                    content={hasKeyframeAt(param.id) ? 'Remove keyframe' : 'Add keyframe'}
                    relationship="label"
                  >
                    <span className={styles.keyframeButton}>
                      <KeyframeDiamond
                        isActive={hasKeyframeAt(param.id)}
                        color="default"
                        size="small"
                        onClick={() => handleKeyframeToggle(param.id, Number(value))}
                        ariaLabel={
                          hasKeyframeAt(param.id)
                            ? `Remove ${param.name} keyframe`
                            : `Add ${param.name} keyframe`
                        }
                      />
                    </span>
                  </Tooltip>
                )}
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
      <div className={styles.footer}>
        <Tooltip content="Reset all parameters to default" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<ArrowReset24Regular />}
            onClick={handleReset}
          >
            Reset
          </Button>
        </Tooltip>
      </div>
    </div>
  );
};

export default EffectControls;
