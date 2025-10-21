/**
 * Pacing Panel Component
 * Displays pacing analysis and recommendations
 */

import React from 'react';
import {
  Card,
  makeStyles,
  tokens,
  ProgressBar,
  Body1,
  Body1Strong,
  Caption1,
  Title3,
} from '@fluentui/react-components';
import { PacingAnalysis } from '../../../services/editingIntelligenceService';

interface PacingPanelProps {
  analysis?: PacingAnalysis;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalM,
  },
  sceneCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  issueWarning: {
    color: tokens.colorPaletteRedForeground1,
  },
  metric: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalS,
  },
});

const getIssueLabel = (issueType?: string): string => {
  if (!issueType) return '';
  return issueType.replace(/([A-Z])/g, ' $1').trim();
};

export const PacingPanel: React.FC<PacingPanelProps> = ({
  analysis,
}) => {
  const styles = useStyles();

  if (!analysis) {
    return <Body1>No pacing analysis available.</Body1>;
  }

  return (
    <div className={styles.container}>
      <Card className={styles.summaryCard}>
        <Title3>Overall Pacing</Title3>
        <div className={styles.metric}>
          <Body1>Engagement Score:</Body1>
          <Body1Strong>{Math.round(analysis.overallEngagementScore * 100)}%</Body1Strong>
        </div>
        <ProgressBar
          value={analysis.overallEngagementScore}
          max={1}
          thickness="large"
        />
        <div className={styles.metric}>
          <Body1>Content Density:</Body1>
          <Body1Strong>{analysis.contentDensity.toFixed(1)} words/sec</Body1Strong>
        </div>
        <Body1 style={{ marginTop: tokens.spacingVerticalM }}>{analysis.summary}</Body1>
      </Card>

      {analysis.sceneRecommendations.map((rec, index) => (
        <Card key={index} className={styles.sceneCard}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Body1Strong>Scene {rec.sceneIndex + 1}</Body1Strong>
            {rec.issueType && (
              <Caption1 className={styles.issueWarning}>
                {getIssueLabel(rec.issueType)}
              </Caption1>
            )}
          </div>
          
          <div className={styles.metric}>
            <Body1>Engagement:</Body1>
            <Body1Strong>{Math.round(rec.engagementScore * 100)}%</Body1Strong>
          </div>
          <ProgressBar
            value={rec.engagementScore}
            max={1}
            thickness="medium"
          />

          <div className={styles.metric}>
            <Caption1>Current Duration:</Caption1>
            <Caption1>{rec.currentDuration}</Caption1>
          </div>
          <div className={styles.metric}>
            <Caption1>Recommended:</Caption1>
            <Caption1>{rec.recommendedDuration}</Caption1>
          </div>

          <Body1 style={{ marginTop: tokens.spacingVerticalS }}>
            {rec.reasoning}
          </Body1>
        </Card>
      ))}
    </div>
  );
};
