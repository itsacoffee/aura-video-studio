import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  ProgressBar,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import {
  Play24Regular,
  ArrowClockwise24Regular,
  Warning24Regular,
  Image24Regular,
  Speaker224Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import type { PreviewData, ScriptData, StyleData, StepValidation, ScriptScene } from '../types';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  generationCard: {
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  previewGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalL,
  },
  sceneCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  scenePreview: {
    width: '100%',
    height: '160px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalM,
    position: 'relative',
    overflow: 'hidden',
  },
  sceneImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  sceneDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sceneActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
    justifyContent: 'space-between',
  },
  progressSection: {
    width: '100%',
    maxWidth: '500px',
  },
  statsRow: {
    display: 'flex',
    justifyContent: 'space-around',
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingHorizontalL,
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
  },
  audioPreview: {
    width: '100%',
    marginTop: tokens.spacingVerticalM,
    height: '40px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
});

interface PreviewGenerationProps {
  data: PreviewData;
  scriptData: ScriptData;
  styleData: StyleData;
  advancedMode: boolean;
  onChange: (data: PreviewData) => void;
  onValidationChange: (validation: StepValidation) => void;
}

type GenerationStatus = 'idle' | 'generating' | 'completed' | 'error';

export const PreviewGeneration: FC<PreviewGenerationProps> = ({
  data,
  scriptData,
  styleData,
  advancedMode,
  onChange,
  onValidationChange,
}) => {
  const styles = useStyles();
  const [status, setStatus] = useState<GenerationStatus>('idle');
  const [progress, setProgress] = useState(0);
  const [currentStage, setCurrentStage] = useState('');
  const [regeneratingScene, setRegeneratingScene] = useState<string | null>(null);

  const hasPreviewData = useMemo(() => {
    return data.thumbnails.length > 0 && data.audioSamples.length > 0;
  }, [data]);

  useEffect(() => {
    if (hasPreviewData) {
      setStatus('completed');
      onValidationChange({ isValid: true, errors: [] });
    } else {
      onValidationChange({ isValid: false, errors: ['Preview generation required'] });
    }
  }, [hasPreviewData, onValidationChange]);

  const generatePreviews = useCallback(async () => {
    setStatus('generating');
    setProgress(0);
    setCurrentStage('Initializing preview generation...');

    try {
      const thumbnails = [];
      const audioSamples = [];

      for (let i = 0; i < scriptData.scenes.length; i++) {
        const scene = scriptData.scenes[i];

        setCurrentStage(`Generating thumbnail ${i + 1} of ${scriptData.scenes.length}...`);
        setProgress(((i + 0.5) / scriptData.scenes.length) * 50);

        await new Promise((resolve) => setTimeout(resolve, 500));

        thumbnails.push({
          sceneId: scene.id,
          imageUrl: `https://via.placeholder.com/400x300/6264a7/ffffff?text=Scene+${i + 1}`,
          caption: scene.visualDescription || scene.text.substring(0, 50),
        });

        setCurrentStage(`Generating audio preview ${i + 1} of ${scriptData.scenes.length}...`);
        setProgress(50 + ((i + 0.5) / scriptData.scenes.length) * 50);

        await new Promise((resolve) => setTimeout(resolve, 500));

        audioSamples.push({
          sceneId: scene.id,
          audioUrl: `https://example.com/audio/${scene.id}.mp3`,
          duration: scene.duration,
          waveformData: Array.from({ length: 50 }, () => Math.random() * 100),
        });
      }

      onChange({
        thumbnails,
        audioSamples,
      });

      setProgress(100);
      setCurrentStage('Preview generation completed!');
      setStatus('completed');
    } catch (error) {
      console.error('Preview generation failed:', error);
      setStatus('error');
      setCurrentStage('Preview generation failed');
      onValidationChange({ isValid: false, errors: ['Preview generation failed'] });
    }
  }, [scriptData.scenes, onChange, onValidationChange]);

  const regenerateScene = useCallback(
    async (sceneId: string) => {
      setRegeneratingScene(sceneId);

      try {
        await new Promise((resolve) => setTimeout(resolve, 1500));

        const updatedThumbnails = data.thumbnails.map((thumb) =>
          thumb.sceneId === sceneId
            ? {
                ...thumb,
                imageUrl: `https://via.placeholder.com/400x300/6264a7/ffffff?text=Regenerated+${sceneId}`,
              }
            : thumb
        );

        onChange({
          thumbnails: updatedThumbnails,
          audioSamples: data.audioSamples,
        });
      } catch (error) {
        console.error('Scene regeneration failed:', error);
      } finally {
        setRegeneratingScene(null);
      }
    },
    [data, onChange]
  );

  const playScenePreview = useCallback((sceneId: string) => {
    console.log('Playing preview for scene:', sceneId);
  }, []);

  const renderGenerationView = () => (
    <div className={styles.generationCard}>
      <Title3>Generate Scene Previews</Title3>
      <Text>
        Create preview thumbnails and audio samples for each scene to review before final
        generation.
      </Text>

      <div className={styles.statsRow}>
        <div className={styles.statItem}>
          <Image24Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
          <Text weight="semibold">{scriptData.scenes.length}</Text>
          <Text size={200}>Scenes</Text>
        </div>
        <div className={styles.statItem}>
          <Speaker224Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
          <Text weight="semibold">{styleData.voiceProvider}</Text>
          <Text size={200}>Voice</Text>
        </div>
      </div>

      <Button appearance="primary" size="large" onClick={generatePreviews}>
        Generate Previews
      </Button>
    </div>
  );

  const renderGeneratingView = () => (
    <div className={styles.generationCard}>
      <Spinner size="large" />
      <Title3>Generating Previews...</Title3>
      <Text>{currentStage}</Text>
      <div className={styles.progressSection}>
        <ProgressBar value={progress / 100} />
        <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
          {Math.round(progress)}% complete
        </Text>
      </div>
    </div>
  );

  const renderCompletedView = () => (
    <div className={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <Title3>Scene Previews</Title3>
          <Text>Review and regenerate individual scenes as needed</Text>
        </div>
        <Tooltip content="Regenerate all previews" relationship="label">
          <Button
            appearance="secondary"
            icon={<ArrowClockwise24Regular />}
            onClick={generatePreviews}
          >
            Regenerate All
          </Button>
        </Tooltip>
      </div>

      <div className={styles.previewGrid}>
        {scriptData.scenes.map((scene: ScriptScene, index: number) => {
          const thumbnail = data.thumbnails.find((t) => t.sceneId === scene.id);
          const audioSample = data.audioSamples.find((a) => a.sceneId === scene.id);
          const isRegenerating = regeneratingScene === scene.id;

          return (
            <Card key={scene.id} className={styles.sceneCard}>
              <div className={styles.scenePreview}>
                {isRegenerating ? (
                  <Spinner size="large" />
                ) : thumbnail ? (
                  <img
                    src={thumbnail.imageUrl}
                    alt={thumbnail.caption}
                    className={styles.sceneImage}
                  />
                ) : (
                  <Image24Regular style={{ fontSize: '48px' }} />
                )}
                <Badge
                  appearance="filled"
                  style={{
                    position: 'absolute',
                    top: tokens.spacingVerticalS,
                    left: tokens.spacingHorizontalS,
                  }}
                >
                  Scene {index + 1}
                </Badge>
              </div>

              <div className={styles.sceneDetails}>
                <Text weight="semibold" size={300}>
                  {scene.text.substring(0, 60)}
                  {scene.text.length > 60 ? '...' : ''}
                </Text>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Duration: {scene.duration}s
                </Text>

                {audioSample && (
                  <div className={styles.audioPreview}>
                    <Speaker224Regular style={{ marginRight: tokens.spacingHorizontalS }} />
                    <Text size={200}>Audio ready</Text>
                  </div>
                )}

                <div className={styles.sceneActions}>
                  <Button
                    appearance="secondary"
                    icon={<Play24Regular />}
                    onClick={() => playScenePreview(scene.id)}
                    disabled={!thumbnail || !audioSample}
                  >
                    Preview
                  </Button>
                  <Button
                    appearance="subtle"
                    icon={<ArrowClockwise24Regular />}
                    onClick={() => regenerateScene(scene.id)}
                    disabled={isRegenerating}
                  >
                    Regenerate
                  </Button>
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      {advancedMode && (
        <Card style={{ padding: tokens.spacingVerticalL }}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Advanced Options</Title3>
          <Text size={300}>
            Previews use lower quality settings for faster generation. Final video will use full
            quality settings.
          </Text>
        </Card>
      )}
    </div>
  );

  const renderErrorView = () => (
    <div className={styles.generationCard}>
      <Warning24Regular style={{ fontSize: '48px', color: tokens.colorPaletteRedForeground1 }} />
      <Title3>Preview Generation Failed</Title3>
      <Text>{currentStage}</Text>
      <Button appearance="primary" onClick={generatePreviews}>
        Try Again
      </Button>
    </div>
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Preview Generation</Title2>
        <Text>
          Generate preview thumbnails and audio samples to review your video before final rendering.
        </Text>
      </div>

      {status === 'idle' && renderGenerationView()}
      {status === 'generating' && renderGeneratingView()}
      {status === 'completed' && renderCompletedView()}
      {status === 'error' && renderErrorView()}
    </div>
  );
};
