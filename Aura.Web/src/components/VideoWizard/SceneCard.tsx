/**
 * SceneCard - A card component for displaying individual AI-generated script scenes
 *
 * Provides a context-menu-enabled card for viewing and interacting with script scenes.
 * Supports regeneration loading states and visual feedback.
 */

import { Card, CardHeader, makeStyles, Spinner, Text, tokens } from '@fluentui/react-components';
import type { FC } from 'react';
import React from 'react';

const useStyles = makeStyles({
  sceneCard: {
    marginBottom: tokens.spacingVerticalL,
    cursor: 'context-menu',
    transition: 'all 0.2s ease',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      border: `1px solid ${tokens.colorNeutralStroke1}`,
      boxShadow: `0 2px 8px ${tokens.colorNeutralShadowAmbient}`,
    },
  },
  sceneHeader: {
    fontWeight: 600,
    marginBottom: tokens.spacingVerticalXS,
  },
  sceneText: {
    lineHeight: '1.6',
    whiteSpace: 'pre-wrap',
    padding: tokens.spacingVerticalM,
    fontSize: tokens.fontSizeBase300,
  },
  regenerating: {
    opacity: 0.6,
    pointerEvents: 'none',
  },
  spinnerContainer: {
    display: 'flex',
    alignItems: 'center',
  },
});

export interface SceneForCard {
  index: number;
  heading: string;
  script: string;
}

interface SceneCardProps {
  scene: SceneForCard;
  jobId: string;
  isRegenerating: boolean;
  onContextMenu: (
    e: React.MouseEvent,
    sceneIndex: number,
    sceneText: string,
    jobId: string
  ) => void;
}

export const SceneCard: FC<SceneCardProps> = ({ scene, jobId, isRegenerating, onContextMenu }) => {
  const styles = useStyles();

  return (
    <Card
      className={`${styles.sceneCard} ${isRegenerating ? styles.regenerating : ''}`}
      onContextMenu={(e) => onContextMenu(e, scene.index, scene.script, jobId)}
    >
      <CardHeader
        header={<Text className={styles.sceneHeader}>{scene.heading}</Text>}
        action={
          isRegenerating ? (
            <div className={styles.spinnerContainer}>
              <Spinner size="tiny" />
            </div>
          ) : undefined
        }
      />
      <div className={styles.sceneText}>{scene.script}</div>
    </Card>
  );
};
