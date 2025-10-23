/**
 * Engagement Panel Component
 * Displays engagement curve and viewer retention predictions
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
import { EngagementCurve } from '../../../services/editingIntelligenceService';

interface EngagementPanelProps {
  curve?: EngagementCurve;
  jobId: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  metricsCard: {
    padding: tokens.spacingVerticalM,
  },
  metricRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalM,
  },
  suggestionCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  engagementPoint: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
});

const getScoreColor = (score: number): string => {
  if (score >= 0.8) return tokens.colorPaletteGreenForeground1;
  if (score >= 0.6) return tokens.colorPaletteYellowForeground1;
  return tokens.colorPaletteRedForeground1;
};

export const EngagementPanel: React.FC<EngagementPanelProps> = ({ curve }) => {
  const styles = useStyles();

  if (!curve) {
    return <Body1>No engagement analysis available.</Body1>;
  }

  return (
    <div className={styles.container}>
      <Card className={styles.metricsCard}>
        <Title3>Engagement Metrics</Title3>

        <div className={styles.metricRow}>
          <Body1>Average Engagement:</Body1>
          <Body1Strong style={{ color: getScoreColor(curve.averageEngagement) }}>
            {Math.round(curve.averageEngagement * 100)}%
          </Body1Strong>
        </div>
        <ProgressBar value={curve.averageEngagement} max={1} thickness="large" />

        <div className={styles.metricRow}>
          <Body1>Hook Strength:</Body1>
          <Body1Strong style={{ color: getScoreColor(curve.hookStrength) }}>
            {Math.round(curve.hookStrength * 100)}%
          </Body1Strong>
        </div>
        <ProgressBar value={curve.hookStrength} max={1} thickness="medium" />

        <div className={styles.metricRow}>
          <Body1>Ending Impact:</Body1>
          <Body1Strong style={{ color: getScoreColor(curve.endingImpact) }}>
            {Math.round(curve.endingImpact * 100)}%
          </Body1Strong>
        </div>
        <ProgressBar value={curve.endingImpact} max={1} thickness="medium" />
      </Card>

      {curve.retentionRisks.length > 0 && (
        <Card className={styles.suggestionCard}>
          <Body1Strong>Retention Risk Points</Body1Strong>
          {curve.retentionRisks.map((risk, index) => (
            <Caption1 key={index} style={{ display: 'block', marginTop: tokens.spacingVerticalS }}>
              ⚠️ @ {risk}
            </Caption1>
          ))}
        </Card>
      )}

      <Card className={styles.suggestionCard}>
        <Body1Strong>Engagement Boosters</Body1Strong>
        {curve.boosterSuggestions.map((suggestion, index) => (
          <Body1 key={index} style={{ marginTop: tokens.spacingVerticalS }}>
            • {suggestion}
          </Body1>
        ))}
      </Card>

      {curve.points.length > 0 && (
        <Card className={styles.suggestionCard}>
          <Body1Strong>Engagement Timeline</Body1Strong>
          {curve.points.map((point, index) => (
            <div key={index} className={styles.engagementPoint}>
              <Caption1>{point.context}</Caption1>
              <Caption1 style={{ color: getScoreColor(point.predictedEngagement) }}>
                {Math.round(point.predictedEngagement * 100)}%
              </Caption1>
            </div>
          ))}
        </Card>
      )}
    </div>
  );
};
