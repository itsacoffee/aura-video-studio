import { makeStyles, tokens, Text, Button, Spinner, Card } from '@fluentui/react-components';
import { ArrowLeftRegular, PlayRegular, SaveRegular } from '@fluentui/react-icons';
import type { FC } from 'react';
import React, { useState, useCallback, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { StoryboardScene } from '../../components/ideation/StoryboardScene';
import { ErrorState } from '../../components/Loading';
import {
  ideationService,
  type StoryboardScene as StoryboardSceneType,
} from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
    minHeight: '100vh',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXL,
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  backButton: {
    minWidth: 'auto',
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  visualization: {
    marginTop: tokens.spacingVerticalXL,
  },
  timelineOverview: {
    marginBottom: tokens.spacingVerticalXL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  timelineBar: {
    display: 'flex',
    height: '60px',
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    marginBottom: tokens.spacingVerticalM,
    boxShadow: tokens.shadow4,
  },
  timelineSegment: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      filter: 'brightness(1.1)',
      transform: 'scale(1.02)',
      zIndex: 1,
    },
  },
  legendRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
  },
  legendColor: {
    width: '16px',
    height: '16px',
    borderRadius: tokens.borderRadiusSmall,
  },
  loadingState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
  },
  infoCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalL,
    backgroundColor: tokens.colorBrandBackground2,
  },
  infoText: {
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase400,
  },
});

const SCENE_COLORS = [
  '#0078D4', // Blue
  '#107C10', // Green
  '#D83B01', // Orange
  '#881798', // Purple
  '#004E8C', // Dark Blue
  '#498205', // Dark Green
  '#8A1538', // Dark Red
  '#5C2E91', // Dark Purple
];

export const StoryboardVisualizer: FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const { conceptId } = useParams<{ conceptId: string }>();
  const location = useLocation();

  const [scenes, setScenes] = useState<StoryboardSceneType[]>(location.state?.scenes || []);
  const [isLoading, setIsLoading] = useState(!scenes || scenes.length === 0);
  const [error, setError] = useState<string | null>(null);

  const loadStoryboard = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await ideationService.generateStoryboard({
        concept: {
          conceptId: conceptId || 'default',
          title: location.state?.title || 'Untitled',
          description: location.state?.description || '',
          angle: '',
          targetAudience: '',
          pros: [],
          cons: [],
          appealScore: 0,
          hook: '',
        },
        targetDurationSeconds: location.state?.targetDuration || 60,
      });

      if (response.success && response.scenes) {
        setScenes(response.scenes);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load storyboard';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  }, [conceptId, location.state]);

  useEffect(() => {
    if (conceptId && (!scenes || scenes.length === 0)) {
      loadStoryboard();
    }
  }, [conceptId, scenes, loadStoryboard]);

  const handleEditScene = useCallback((sceneNumber: number) => {
    // Navigate to scene editor or show edit modal
    console.info('Edit scene:', sceneNumber);
  }, []);

  const handlePreviewScene = useCallback((sceneNumber: number) => {
    // Show preview of scene
    console.info('Preview scene:', sceneNumber);
  }, []);

  const handleSave = useCallback(() => {
    // Save storyboard changes
    console.info('Save storyboard');
  }, []);

  const handleUseForVideo = useCallback(() => {
    navigate('/create', {
      state: {
        storyboard: scenes,
      },
    });
  }, [navigate, scenes]);

  const totalDuration = scenes.reduce((sum, scene) => sum + scene.durationSeconds, 0);

  const getSceneColor = (index: number): string => {
    return SCENE_COLORS[index % SCENE_COLORS.length];
  };

  if (isLoading) {
    return (
      <div className={styles.loadingState}>
        <Spinner size="extra-large" />
        <Text size={400}>Loading storyboard...</Text>
      </div>
    );
  }

  if (error || !scenes || scenes.length === 0) {
    return (
      <div className={styles.container}>
        <ErrorState
          title="Error loading storyboard"
          message={error || 'No storyboard available'}
          onRetry={loadStoryboard}
        />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Button
            appearance="subtle"
            icon={<ArrowLeftRegular />}
            onClick={() => navigate(-1)}
            className={styles.backButton}
            aria-label="Go back"
          />
          <div className={styles.headerContent}>
            <Text className={styles.title}>Storyboard Visualizer</Text>
            <Text className={styles.subtitle}>Visual timeline of your video structure</Text>
          </div>
        </div>
        <div className={styles.headerActions}>
          <Button appearance="outline" icon={<SaveRegular />} onClick={handleSave}>
            Save
          </Button>
          <Button appearance="outline" icon={<PlayRegular />}>
            Preview All
          </Button>
          <Button appearance="primary" onClick={handleUseForVideo}>
            Use for Video
          </Button>
        </div>
      </div>

      <Card className={styles.infoCard}>
        <Text className={styles.infoText}>
          This visual timeline shows the structure of your video. Each colored segment represents a
          scene. Click on a scene to edit or preview it. The width of each segment represents its
          relative duration in the video.
        </Text>
      </Card>

      <div className={styles.visualization}>
        <div className={styles.timelineOverview}>
          <div style={{ marginBottom: tokens.spacingVerticalM }}>
            <Text size={400} weight="semibold">
              Timeline Overview
            </Text>
          </div>
          <div className={styles.timelineBar}>
            {scenes.map((scene, index) => {
              const widthPercentage = (scene.durationSeconds / totalDuration) * 100;
              return (
                <div
                  key={scene.sceneNumber}
                  className={styles.timelineSegment}
                  style={{
                    width: `${widthPercentage}%`,
                    backgroundColor: getSceneColor(index),
                  }}
                  onClick={() => handleEditScene(scene.sceneNumber)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      handleEditScene(scene.sceneNumber);
                    }
                  }}
                  role="button"
                  tabIndex={0}
                  title={`Scene ${scene.sceneNumber}: ${scene.description}`}
                  aria-label={`Scene ${scene.sceneNumber}: ${scene.description}`}
                >
                  {widthPercentage > 10 && `Scene ${scene.sceneNumber}`}
                </div>
              );
            })}
          </div>
          <div className={styles.legendRow}>
            {scenes.map((scene, index) => (
              <div key={scene.sceneNumber} className={styles.legendItem}>
                <div
                  className={styles.legendColor}
                  style={{ backgroundColor: getSceneColor(index) }}
                />
                <Text>
                  Scene {scene.sceneNumber} ({scene.durationSeconds}s)
                </Text>
              </div>
            ))}
          </div>
        </div>

        <StoryboardScene
          scenes={scenes}
          onEditScene={handleEditScene}
          onPreviewScene={handlePreviewScene}
          showActions={true}
        />
      </div>
    </div>
  );
};

export default StoryboardVisualizer;
