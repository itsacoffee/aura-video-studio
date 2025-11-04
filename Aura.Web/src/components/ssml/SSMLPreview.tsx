/**
 * SSML Preview Component
 * Main component for SSML editing with waveform visualization, timing markers, and per-scene controls
 */

import { Button, Spinner, Card, CardHeader, Text, Badge } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import type { FC } from 'react';
import { SSMLProviderSelector } from './SSMLProviderSelector';
import { SSMLSceneEditor } from './SSMLSceneEditor';
import { SSMLTimingDisplay } from './SSMLTimingDisplay';
import { planSSML } from '@/services/ssmlService';
import { useSSMLEditorStore } from '@/state/ssmlEditor';
import type { LineDto } from '@/types/api-v1';

interface SSMLPreviewProps {
  scriptLines: LineDto[];
  voiceName: string;
  initialProvider?: string;
  onSSMLGenerated?: (ssmlMarkup: string[]) => void;
}

export const SSMLPreview: FC<SSMLPreviewProps> = ({
  scriptLines,
  voiceName,
  initialProvider = 'ElevenLabs',
  onSSMLGenerated,
}) => {
  const {
    scenes,
    selectedSceneIndex,
    selectedProvider,
    isPlanning,
    planningResult,
    setProvider,
    setIsPlanning,
    setPlanningResult,
    selectScene,
  } = useSSMLEditorStore();

  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (initialProvider && !selectedProvider) {
      setProvider(initialProvider);
    }
  }, [initialProvider, selectedProvider, setProvider]);

  const handlePlanSSML = async (): Promise<void> => {
    if (!selectedProvider) {
      setError('Please select a TTS provider');
      return;
    }

    setIsPlanning(true);
    setError(null);

    try {
      const targetDurations: Record<number, number> = {};
      scriptLines.forEach((line) => {
        targetDurations[line.sceneIndex] = line.durationSeconds;
      });

      const result = await planSSML({
        scriptLines,
        targetProvider: selectedProvider,
        voiceSpec: {
          voiceName,
          rate: 1.0,
          pitch: 0.0,
          volume: 1.0,
        },
        targetDurations,
        durationTolerance: 0.02,
        maxFittingIterations: 10,
      });

      setPlanningResult(result);

      if (onSSMLGenerated) {
        const ssmlMarkups = result.segments.map((s) => s.ssmlMarkup);
        onSSMLGenerated(ssmlMarkups);
      }

      if (result.warnings.length > 0) {
        console.warn('SSML Planning warnings:', result.warnings);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to plan SSML';
      setError(errorMessage);
      console.error('SSML planning error:', err);
    } finally {
      setIsPlanning(false);
    }
  };

  const selectedScene = selectedSceneIndex !== null ? scenes.get(selectedSceneIndex) : null;

  const withinTolerancePercent = planningResult?.stats.withinTolerancePercent ?? 0;
  const toleranceColor =
    withinTolerancePercent >= 95 ? 'success' : withinTolerancePercent >= 80 ? 'warning' : 'danger';

  return (
    <div className="ssml-preview">
      <Card>
        <CardHeader
          header={
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <Text weight="semibold" size={500}>
                SSML Preview & Timing Alignment
              </Text>
              {planningResult && (
                <Badge appearance="filled" color={toleranceColor}>
                  {withinTolerancePercent.toFixed(1)}% within tolerance
                </Badge>
              )}
            </div>
          }
          description="Configure SSML with per-scene controls and duration fitting"
        />

        <div style={{ padding: '16px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {/* Provider Selection */}
          <SSMLProviderSelector />

          {/* Actions */}
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <Button
              appearance="primary"
              onClick={handlePlanSSML}
              disabled={isPlanning || !selectedProvider}
            >
              {isPlanning ? (
                <>
                  <Spinner size="tiny" /> Planning SSML...
                </>
              ) : (
                'Generate SSML'
              )}
            </Button>

            {planningResult && (
              <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
                Planned in {planningResult.planningDurationMs}ms
              </Text>
            )}
          </div>

          {/* Error Display */}
          {error && (
            <div
              style={{
                padding: '12px',
                background: 'var(--colorPaletteRedBackground2)',
                borderRadius: '4px',
              }}
            >
              <Text style={{ color: 'var(--colorPaletteRedForeground1)' }}>{error}</Text>
            </div>
          )}

          {/* Timing Stats */}
          {planningResult && <SSMLTimingDisplay stats={planningResult.stats} />}

          {/* Scene List */}
          {scenes.size > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <Text weight="semibold">Scenes ({scenes.size})</Text>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {Array.from(scenes.values()).map((scene) => (
                  <Card
                    key={scene.sceneIndex}
                    onClick={() => selectScene(scene.sceneIndex)}
                    style={{
                      cursor: 'pointer',
                      border:
                        selectedSceneIndex === scene.sceneIndex
                          ? '2px solid var(--colorBrandBackground)'
                          : '1px solid var(--colorNeutralStroke1)',
                    }}
                  >
                    <div style={{ padding: '12px' }}>
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                        }}
                      >
                        <Text weight="semibold">Scene {scene.sceneIndex + 1}</Text>
                        <Badge
                          appearance="outline"
                          color={
                            Math.abs(scene.deviationPercent) <= 2
                              ? 'success'
                              : Math.abs(scene.deviationPercent) <= 5
                                ? 'warning'
                                : 'danger'
                          }
                        >
                          {scene.deviationPercent > 0 ? '+' : ''}
                          {scene.deviationPercent.toFixed(1)}%
                        </Badge>
                      </div>
                      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                        Target: {scene.targetDurationMs}ms | Estimated: {scene.estimatedDurationMs}
                        ms
                      </Text>
                    </div>
                  </Card>
                ))}
              </div>
            </div>
          )}

          {/* Scene Editor */}
          {selectedScene && <SSMLSceneEditor scene={selectedScene} />}
        </div>
      </Card>
    </div>
  );
};
