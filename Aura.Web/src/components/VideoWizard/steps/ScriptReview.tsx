import {
  Title2,
  Text,
  Card,
  Textarea,
  Button,
  Field,
  Label,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowSyncRegular,
  PlayRegular,
  CheckmarkCircleRegular,
  DismissCircleRegular,
} from '@fluentui/react-icons';
import { useEffect, useState, useCallback } from 'react';
import type { FC } from 'react';
import { ttsService } from '../../../services/ttsService';
import type { RegenerateAudioRequest } from '../../../services/ttsService';
import type { ScriptData, BriefData, StyleData, StepValidation, ScriptScene } from '../types';

interface ScriptReviewProps {
  data: ScriptData;
  briefData: BriefData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: ScriptData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  scenesContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sceneCard: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sceneHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXS,
  },
  sceneTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  sceneTiming: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  sceneActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalXS,
  },
  statusMessage: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    padding: tokens.spacingVerticalXS,
  },
  successMessage: {
    color: tokens.colorPaletteGreenForeground1,
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
  },
  textarea: {
    minHeight: '80px',
  },
});

export const ScriptReview: FC<ScriptReviewProps> = ({
  data,
  styleData,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const [regeneratingScenes, setRegeneratingScenes] = useState<Set<string>>(new Set());
  const [audioResults, setAudioResults] = useState<
    Map<string, { success: boolean; error?: string }>
  >(new Map());
  const [audioElements, setAudioElements] = useState<Map<string, HTMLAudioElement>>(new Map());
  const [playingScenes, setPlayingScenes] = useState<Set<string>>(new Set());

  useEffect(() => {
    const hasScenes = data.scenes && data.scenes.length > 0;
    const allScenesHaveText = data.scenes?.every((scene) => scene.text.trim().length > 0) ?? false;

    onValidationChange({
      isValid: hasScenes && allScenesHaveText,
      errors: !hasScenes
        ? ['No script scenes available']
        : !allScenesHaveText
          ? ['All scenes must have text']
          : [],
    });
  }, [data.scenes, onValidationChange]);

  const handleSceneTextChange = useCallback(
    (sceneId: string, newText: string) => {
      const updatedScenes = data.scenes.map((scene) =>
        scene.id === sceneId ? { ...scene, text: newText } : scene
      );
      onChange({ ...data, scenes: updatedScenes });
    },
    [data, onChange]
  );

  const handleRegenerateAudio = useCallback(
    async (scene: ScriptScene) => {
      if (!styleData.voiceProvider || !styleData.voiceName) {
        console.error('Voice provider and voice name are required for audio generation');
        setAudioResults((prev) =>
          new Map(prev).set(scene.id, {
            success: false,
            error: 'Voice settings not configured',
          })
        );
        return;
      }

      setRegeneratingScenes((prev) => new Set(prev).add(scene.id));
      setAudioResults((prev) => {
        const newMap = new Map(prev);
        newMap.delete(scene.id);
        return newMap;
      });

      try {
        const request: RegenerateAudioRequest = {
          sceneIndex: data.scenes.findIndex((s) => s.id === scene.id),
          text: scene.text,
          startSeconds: scene.timestamp,
          durationSeconds: scene.duration,
          provider: styleData.voiceProvider,
          voiceName: styleData.voiceName,
        };

        const response = await ttsService.regenerateAudio(request);

        setAudioResults((prev) => new Map(prev).set(scene.id, { success: true }));

        const audioElement = new Audio(response.audioPath);
        setAudioElements((prev) => new Map(prev).set(scene.id, audioElement));
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        console.error(`Failed to regenerate audio for scene ${scene.id}:`, errorObj.message);
        setAudioResults((prev) =>
          new Map(prev).set(scene.id, {
            success: false,
            error: errorObj.message,
          })
        );
      } finally {
        setRegeneratingScenes((prev) => {
          const newSet = new Set(prev);
          newSet.delete(scene.id);
          return newSet;
        });
      }
    },
    [data.scenes, styleData]
  );

  const handlePlayAudio = useCallback(
    (sceneId: string) => {
      const audioElement = audioElements.get(sceneId);
      if (!audioElement) return;

      const isPlaying = playingScenes.has(sceneId);

      if (isPlaying) {
        audioElement.pause();
        audioElement.currentTime = 0;
        setPlayingScenes((prev) => {
          const newSet = new Set(prev);
          newSet.delete(sceneId);
          return newSet;
        });
      } else {
        audioElement.onended = () => {
          setPlayingScenes((prev) => {
            const newSet = new Set(prev);
            newSet.delete(sceneId);
            return newSet;
          });
        };
        audioElement.play().catch((error: unknown) => {
          const errorObj = error instanceof Error ? error : new Error(String(error));
          console.error('Failed to play audio:', errorObj.message);
          setPlayingScenes((prev) => {
            const newSet = new Set(prev);
            newSet.delete(sceneId);
            return newSet;
          });
        });
        setPlayingScenes((prev) => new Set(prev).add(sceneId));
      }
    },
    [audioElements, playingScenes]
  );

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Script Review</Title2>
        <Text>
          Review and edit the AI-generated script. You can modify the text for each scene and
          regenerate audio if needed.
        </Text>
      </div>

      {!data.scenes || data.scenes.length === 0 ? (
        <Card>
          <Text>No script scenes available. Please generate a script in the previous step.</Text>
        </Card>
      ) : (
        <div className={styles.scenesContainer}>
          {data.scenes.map((scene, index) => {
            const isRegenerating = regeneratingScenes.has(scene.id);
            const result = audioResults.get(scene.id);
            const hasAudio = audioElements.has(scene.id);
            const isPlaying = playingScenes.has(scene.id);

            return (
              <Card key={scene.id} className={styles.sceneCard}>
                <div className={styles.sceneHeader}>
                  <div>
                    <Label className={styles.sceneTitle}>Scene {index + 1}</Label>
                    <Text className={styles.sceneTiming}>
                      {formatTime(scene.timestamp)} - {formatTime(scene.timestamp + scene.duration)}
                      ({scene.duration}s)
                    </Text>
                  </div>
                </div>

                <Field label="Scene Text">
                  <Textarea
                    className={styles.textarea}
                    value={scene.text}
                    onChange={(_, data) => handleSceneTextChange(scene.id, data.value)}
                    resize="vertical"
                  />
                </Field>

                {scene.visualDescription && (
                  <Field label="Visual Description">
                    <Text>{scene.visualDescription}</Text>
                  </Field>
                )}

                <div className={styles.sceneActions}>
                  <Button
                    appearance="secondary"
                    icon={isRegenerating ? <Spinner size="tiny" /> : <ArrowSyncRegular />}
                    onClick={() => handleRegenerateAudio(scene)}
                    disabled={isRegenerating || !scene.text.trim()}
                  >
                    {isRegenerating ? 'Generating...' : 'Regenerate Audio'}
                  </Button>

                  {hasAudio && (
                    <Button
                      appearance="primary"
                      icon={<PlayRegular />}
                      onClick={() => handlePlayAudio(scene.id)}
                      disabled={isRegenerating}
                    >
                      {isPlaying ? 'Stop' : 'Play Audio'}
                    </Button>
                  )}
                </div>

                {result && (
                  <div
                    className={`${styles.statusMessage} ${result.success ? styles.successMessage : styles.errorMessage}`}
                  >
                    {result.success ? (
                      <>
                        <CheckmarkCircleRegular />
                        <Text>Audio generated successfully</Text>
                      </>
                    ) : (
                      <>
                        <DismissCircleRegular />
                        <Text>Failed: {result.error}</Text>
                      </>
                    )}
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
};
