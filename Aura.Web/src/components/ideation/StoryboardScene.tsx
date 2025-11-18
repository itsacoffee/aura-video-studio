import { Card, makeStyles, tokens, Text, Badge, Button } from '@fluentui/react-components';
import {
  VideoRegular,
  ClockRegular,
  ImageRegular,
  ChevronRightRegular,
  EditRegular,
  PlayRegular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import type { FC } from 'react';
import type { StoryboardScene as StoryboardSceneType } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    width: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  icon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  scenesTimeline: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    position: 'relative',
  },
  timelineConnector: {
    position: 'absolute',
    left: '16px',
    top: '40px',
    bottom: '40px',
    width: '2px',
    backgroundColor: tokens.colorNeutralStroke2,
    zIndex: 0,
  },
  sceneCard: {
    padding: tokens.spacingVerticalL,
    position: 'relative',
    zIndex: 1,
    transition: 'all 0.2s ease',
    ':hover': {
      boxShadow: tokens.shadow16,
    },
  },
  sceneHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  sceneNumber: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '32px',
    height: '32px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    flexShrink: 0,
  },
  sceneContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sceneTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  description: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    lineHeight: tokens.lineHeightBase400,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  metadataItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  visualStyle: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  shotList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  shot: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    lineHeight: tokens.lineHeightBase300,
  },
  shotBullet: {
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
  transition: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
  },
  purpose: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteBlueBorder2,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    marginTop: tokens.spacingVerticalXS,
  },
  purposeLabel: {
    fontWeight: tokens.fontWeightSemibold,
    marginRight: tokens.spacingHorizontalXS,
  },
  summary: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  summaryRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  totalDuration: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: '48px',
  },
});

export interface StoryboardSceneProps {
  scenes: StoryboardSceneType[];
  onEditScene?: (sceneNumber: number) => void;
  onPreviewScene?: (sceneNumber: number) => void;
  showActions?: boolean;
}

export const StoryboardScene: FC<StoryboardSceneProps> = ({
  scenes,
  onEditScene,
  onPreviewScene,
  showActions = true,
}) => {
  const styles = useStyles();
  const [expandedScenes, setExpandedScenes] = useState<Set<number>>(new Set());

  const toggleScene = (sceneNumber: number) => {
    setExpandedScenes((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(sceneNumber)) {
        newSet.delete(sceneNumber);
      } else {
        newSet.add(sceneNumber);
      }
      return newSet;
    });
  };

  const totalDuration = scenes.reduce((sum, scene) => sum + scene.durationSeconds, 0);

  const formatDuration = (seconds: number): string => {
    if (seconds < 60) return `${seconds}s`;
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}m ${secs}s`;
  };

  if (scenes.length === 0) {
    return (
      <div className={styles.emptyState}>
        <VideoRegular className={styles.emptyIcon} />
        <Text size={400}>No storyboard scenes available</Text>
        <Text size={300}>Generate a storyboard to visualize your video structure</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <VideoRegular className={styles.icon} />
        <div>
          <Text size={500} weight="semibold">
            Video Storyboard
          </Text>
          <Text size={200} as="div" style={{ color: tokens.colorNeutralForeground3 }}>
            {scenes.length} scene{scenes.length !== 1 ? 's' : ''} • Total duration:{' '}
            {formatDuration(totalDuration)}
          </Text>
        </div>
      </div>

      <div className={styles.summary}>
        <div className={styles.summaryRow}>
          <Text size={300}>Total Scenes</Text>
          <Text size={300} weight="semibold">
            {scenes.length}
          </Text>
        </div>
        <div className={styles.summaryRow}>
          <Text size={300}>Total Duration</Text>
          <Text className={styles.totalDuration}>{formatDuration(totalDuration)}</Text>
        </div>
        <div className={styles.summaryRow}>
          <Text size={300}>Avg Scene Length</Text>
          <Text size={300} weight="semibold">
            {formatDuration(Math.round(totalDuration / scenes.length))}
          </Text>
        </div>
      </div>

      <div className={styles.scenesTimeline}>
        {scenes.length > 1 && <div className={styles.timelineConnector} />}
        {scenes.map((scene, index) => (
          <React.Fragment key={scene.sceneNumber}>
            <Card className={styles.sceneCard}>
              <div className={styles.sceneHeader}>
                <div className={styles.sceneNumber}>{scene.sceneNumber}</div>
                <div className={styles.sceneContent}>
                  <div className={styles.sceneTitle}>
                    Scene {scene.sceneNumber}
                    <Badge appearance="tint" size="small">
                      {formatDuration(scene.durationSeconds)}
                    </Badge>
                  </div>

                  <Text className={styles.description}>{scene.description}</Text>

                  <div className={styles.purpose}>
                    <Text className={styles.purposeLabel}>Purpose:</Text>
                    {scene.purpose}
                  </div>

                  <div className={styles.metadata}>
                    <div className={styles.metadataItem}>
                      <ClockRegular />
                      <Text>{formatDuration(scene.durationSeconds)}</Text>
                    </div>
                    <div className={styles.visualStyle}>
                      <ImageRegular />
                      <Text>{scene.visualStyle}</Text>
                    </div>
                  </div>

                  {scene.shotList && scene.shotList.length > 0 && (
                    <>
                      <Button
                        appearance="subtle"
                        size="small"
                        onClick={() => toggleScene(scene.sceneNumber)}
                        style={{ alignSelf: 'flex-start', marginTop: tokens.spacingVerticalXS }}
                      >
                        {expandedScenes.has(scene.sceneNumber) ? 'Hide' : 'Show'} shots (
                        {scene.shotList.length})
                      </Button>

                      {expandedScenes.has(scene.sceneNumber) && (
                        <div className={styles.shotList}>
                          {scene.shotList.map((shot, idx) => (
                            <div key={idx} className={styles.shot}>
                              <Text className={styles.shotBullet}>•</Text>
                              <Text>{shot}</Text>
                            </div>
                          ))}
                        </div>
                      )}
                    </>
                  )}

                  {showActions && (onEditScene || onPreviewScene) && (
                    <div className={styles.actions}>
                      {onEditScene && (
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<EditRegular />}
                          onClick={() => onEditScene(scene.sceneNumber)}
                        >
                          Edit
                        </Button>
                      )}
                      {onPreviewScene && (
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<PlayRegular />}
                          onClick={() => onPreviewScene(scene.sceneNumber)}
                        >
                          Preview
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </Card>

            {index < scenes.length - 1 && scene.transitionType && (
              <div className={styles.transition}>
                <ChevronRightRegular />
                <Text>{scene.transitionType}</Text>
                <ChevronRightRegular />
              </div>
            )}
          </React.Fragment>
        ))}
      </div>
    </div>
  );
};
