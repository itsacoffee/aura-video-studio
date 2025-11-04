/**
 * SSML Scene Editor Component
 * Per-scene controls for pace, emphasis, and pause editing
 */

import { Card, Text, Slider, Button, Textarea, Badge } from '@fluentui/react-components';
import { SaveRegular, DismissRegular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { validateSSML, repairSSML } from '@/services/ssmlService';
import { useSSMLEditorStore, type SceneSSMLState } from '@/state/ssmlEditor';

interface SSMLSceneEditorProps {
  scene: SceneSSMLState;
}

export const SSMLSceneEditor: FC<SSMLSceneEditorProps> = ({ scene }) => {
  const { updateScene, selectedProvider, clearValidation, validationErrors, validationWarnings } =
    useSSMLEditorStore();

  const [localSSML, setLocalSSML] = useState(scene.ssmlMarkup);
  const [localRate, setLocalRate] = useState(scene.adjustments.rate);
  const [isValidating, setIsValidating] = useState(false);
  const [isRepairing, setIsRepairing] = useState(false);

  const sceneErrors = validationErrors.get(scene.sceneIndex) || [];
  const sceneWarnings = validationWarnings.get(scene.sceneIndex) || [];

  const handleSave = (): void => {
    updateScene(scene.sceneIndex, {
      ssmlMarkup: localSSML,
      adjustments: {
        ...scene.adjustments,
        rate: localRate,
      },
    });
  };

  const handleReset = (): void => {
    setLocalSSML(scene.ssmlMarkup);
    setLocalRate(scene.adjustments.rate);
    clearValidation(scene.sceneIndex);
  };

  const handleValidate = async (): Promise<void> => {
    if (!selectedProvider) return;

    setIsValidating(true);
    try {
      const result = await validateSSML({
        ssml: localSSML,
        targetProvider: selectedProvider,
      });

      const { setValidationErrors, setValidationWarnings } = useSSMLEditorStore.getState();

      if (!result.isValid) {
        setValidationErrors(scene.sceneIndex, result.errors);
      } else {
        clearValidation(scene.sceneIndex);
      }

      if (result.warnings.length > 0) {
        setValidationWarnings(scene.sceneIndex, result.warnings);
      }
    } catch (error: unknown) {
      console.error('Validation error:', error);
    } finally {
      setIsValidating(false);
    }
  };

  const handleAutoRepair = async (): Promise<void> => {
    if (!selectedProvider) return;

    setIsRepairing(true);
    try {
      const result = await repairSSML({
        ssml: localSSML,
        targetProvider: selectedProvider,
      });

      if (result.wasRepaired) {
        setLocalSSML(result.repairedSsml);
        clearValidation(scene.sceneIndex);
      }
    } catch (error: unknown) {
      console.error('Repair error:', error);
    } finally {
      setIsRepairing(false);
    }
  };

  const hasChanges = localSSML !== scene.ssmlMarkup || localRate !== scene.adjustments.rate;

  return (
    <Card>
      <div style={{ padding: '16px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Text weight="semibold" size={400}>
            Edit Scene {scene.sceneIndex + 1}
          </Text>
          {scene.userModified && (
            <Badge appearance="filled" color="informative">
              Modified
            </Badge>
          )}
        </div>

        {/* Original Text */}
        <div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            Original Text
          </Text>
          <Text>{scene.originalText}</Text>
        </div>

        {/* Rate Slider */}
        <div>
          <Text size={300} weight="semibold">
            Speech Rate: {localRate.toFixed(2)}x
          </Text>
          <Slider
            min={0.5}
            max={2.0}
            step={0.1}
            value={localRate}
            onChange={(_, data) => setLocalRate(data.value)}
          />
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            Adjust speech rate (0.5x - 2.0x)
          </Text>
        </div>

        {/* SSML Markup Editor */}
        <div>
          <Text size={300} weight="semibold">
            SSML Markup
          </Text>
          <Textarea
            value={localSSML}
            onChange={(_, data) => setLocalSSML(data.value)}
            rows={8}
            style={{ fontFamily: 'monospace', fontSize: '12px' }}
            resize="vertical"
          />
        </div>

        {/* Validation Errors */}
        {sceneErrors.length > 0 && (
          <div
            style={{
              padding: '12px',
              background: 'var(--colorPaletteRedBackground2)',
              borderRadius: '4px',
            }}
          >
            <Text weight="semibold" style={{ color: 'var(--colorPaletteRedForeground1)' }}>
              Validation Errors:
            </Text>
            <ul style={{ margin: '8px 0 0 0', paddingLeft: '20px' }}>
              {sceneErrors.map((error, idx) => (
                <li key={idx}>
                  <Text size={200} style={{ color: 'var(--colorPaletteRedForeground1)' }}>
                    {error}
                  </Text>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Validation Warnings */}
        {sceneWarnings.length > 0 && (
          <div
            style={{
              padding: '12px',
              background: 'var(--colorPaletteYellowBackground2)',
              borderRadius: '4px',
            }}
          >
            <Text weight="semibold" style={{ color: 'var(--colorPaletteYellowForeground1)' }}>
              Warnings:
            </Text>
            <ul style={{ margin: '8px 0 0 0', paddingLeft: '20px' }}>
              {sceneWarnings.map((warning, idx) => (
                <li key={idx}>
                  <Text size={200} style={{ color: 'var(--colorPaletteYellowForeground1)' }}>
                    {warning}
                  </Text>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Actions */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
          <Button appearance="secondary" onClick={handleValidate} disabled={isValidating}>
            {isValidating ? 'Validating...' : 'Validate'}
          </Button>
          {sceneErrors.length > 0 && (
            <Button appearance="secondary" onClick={handleAutoRepair} disabled={isRepairing}>
              {isRepairing ? 'Repairing...' : 'Auto-Repair'}
            </Button>
          )}
          <Button
            appearance="secondary"
            icon={<DismissRegular />}
            onClick={handleReset}
            disabled={!hasChanges}
          >
            Reset
          </Button>
          <Button
            appearance="primary"
            icon={<SaveRegular />}
            onClick={handleSave}
            disabled={!hasChanges}
          >
            Save
          </Button>
        </div>

        {/* Timing Info */}
        <div
          style={{
            padding: '12px',
            background: 'var(--colorNeutralBackground2)',
            borderRadius: '4px',
          }}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
            <div>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                Target Duration
              </Text>
              <Text weight="semibold">{scene.targetDurationMs}ms</Text>
            </div>
            <div>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                Estimated Duration
              </Text>
              <Text weight="semibold">{scene.estimatedDurationMs}ms</Text>
            </div>
            <div>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                Deviation
              </Text>
              <Text
                weight="semibold"
                style={{
                  color:
                    Math.abs(scene.deviationPercent) <= 2
                      ? 'var(--colorPaletteGreenForeground1)'
                      : Math.abs(scene.deviationPercent) <= 5
                        ? 'var(--colorPaletteYellowForeground1)'
                        : 'var(--colorPaletteRedForeground1)',
                }}
              >
                {scene.deviationPercent > 0 ? '+' : ''}
                {scene.deviationPercent.toFixed(2)}%
              </Text>
            </div>
            <div>
              <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                Fit Iterations
              </Text>
              <Text weight="semibold">{scene.adjustments.iterations}</Text>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
};
