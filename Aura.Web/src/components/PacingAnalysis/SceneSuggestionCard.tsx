/**
 * Scene Suggestion Card Component
 * Displays individual scene analysis with accept/reject actions
 */

import { useState } from 'react';
import {
  Card,
  Button,
  Badge,
  makeStyles,
  tokens,
  Body1,
  Caption1,
  Title3,
  Subtitle2,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Dismiss24Regular,
  ChevronDown24Regular,
  ChevronUp24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { SceneTimingSuggestion, TransitionType } from '../../types/pacing';
import { durationToSeconds, formatDuration, calculatePercentageChange } from '../../services/pacingService';

const useStyles = makeStyles({
  card: {
    marginBottom: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  sceneInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  durationComparison: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  durationItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    flex: 1,
  },
  arrow: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  reasoning: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandBackground}`,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalM,
  },
  expandButton: {
    marginTop: tokens.spacingVerticalM,
  },
  detailedMetrics: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1Hover,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface SceneSuggestionCardProps {
  suggestion: SceneTimingSuggestion;
  onAccept: (sceneIndex: number) => void;
  onReject: (sceneIndex: number) => void;
  isApplied?: boolean;
}

export const SceneSuggestionCard: React.FC<SceneSuggestionCardProps> = ({
  suggestion,
  onAccept,
  onReject,
  isApplied = false,
}) => {
  const styles = useStyles();
  const [expanded, setExpanded] = useState(false);

  const currentSeconds = durationToSeconds(suggestion.currentDuration);
  const optimalSeconds = durationToSeconds(suggestion.optimalDuration);
  const percentageChange = calculatePercentageChange(currentSeconds, optimalSeconds);

  const getConfidenceBadge = (confidence: number) => {
    if (confidence >= 80) {
      return <Badge appearance="filled" color="success">High Confidence</Badge>;
    } else if (confidence >= 60) {
      return <Badge appearance="filled" color="warning">Medium Confidence</Badge>;
    } else {
      return <Badge appearance="filled" color="danger">Low Confidence</Badge>;
    }
  };

  const getTransitionIcon = (transition: TransitionType) => {
    switch (transition) {
      case TransitionType.Cut:
        return '✂️';
      case TransitionType.Fade:
        return '🌅';
      case TransitionType.Dissolve:
        return '💫';
      default:
        return '🔄';
    }
  };

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div className={styles.sceneInfo}>
          <Title3>Scene {suggestion.sceneIndex + 1}</Title3>
          {isApplied && <Badge appearance="tint" color="success">Applied</Badge>}
          {getConfidenceBadge(suggestion.confidence)}
        </div>
      </div>

      <div className={styles.content}>
        {/* Duration Comparison */}
        <div className={styles.durationComparison}>
          <div className={styles.durationItem}>
            <Caption1>Current</Caption1>
            <Subtitle2>{formatDuration(suggestion.currentDuration)}</Subtitle2>
          </div>
          <div className={styles.arrow}>→</div>
          <div className={styles.durationItem}>
            <Caption1>Suggested</Caption1>
            <Subtitle2 style={{ color: tokens.colorBrandForeground1 }}>
              {formatDuration(suggestion.optimalDuration)}
            </Subtitle2>
            <Caption1 style={{ color: percentageChange > 0 ? tokens.colorPaletteGreenForeground1 : tokens.colorPaletteRedForeground1 }}>
              {percentageChange > 0 ? '+' : ''}{percentageChange.toFixed(0)}%
            </Caption1>
          </div>
        </div>

        {/* Importance Visualization */}
        <div className={styles.metricItem}>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <Caption1>Importance Score</Caption1>
            <Caption1>{suggestion.importanceScore.toFixed(0)}%</Caption1>
          </div>
          <ProgressBar
            value={suggestion.importanceScore / 100}
            color={suggestion.importanceScore >= 70 ? 'success' : suggestion.importanceScore >= 40 ? 'warning' : 'error'}
          />
        </div>

        {/* Key Metrics */}
        <div className={styles.metricsGrid}>
          <div className={styles.metricItem}>
            <Caption1>Complexity</Caption1>
            <Body1>{suggestion.complexityScore.toFixed(0)}%</Body1>
          </div>
          <div className={styles.metricItem}>
            <Caption1>Emotional Intensity</Caption1>
            <Body1>{suggestion.emotionalIntensity.toFixed(0)}%</Body1>
          </div>
          <div className={styles.metricItem}>
            <Caption1>Transition</Caption1>
            <Body1>{getTransitionIcon(suggestion.transitionType)} {suggestion.transitionType}</Body1>
          </div>
          <div className={styles.metricItem}>
            <Caption1>Info Density</Caption1>
            <Body1>{suggestion.informationDensity}</Body1>
          </div>
        </div>

        {/* LLM Reasoning */}
        {suggestion.reasoning && (
          <div className={styles.reasoning}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS, marginBottom: tokens.spacingVerticalS }}>
              <Info24Regular style={{ fontSize: '16px' }} />
              <Caption1>AI Reasoning</Caption1>
            </div>
            <Body1>{suggestion.reasoning}</Body1>
          </div>
        )}

        {/* Expandable Detailed Metrics */}
        {expanded && (
          <div className={styles.detailedMetrics}>
            <Subtitle2 style={{ marginBottom: tokens.spacingVerticalM }}>Detailed Metrics</Subtitle2>
            <div className={styles.metricsGrid}>
              <div className={styles.metricItem}>
                <Caption1>Min Duration</Caption1>
                <Body1>{formatDuration(suggestion.minDuration)}</Body1>
              </div>
              <div className={styles.metricItem}>
                <Caption1>Max Duration</Caption1>
                <Body1>{formatDuration(suggestion.maxDuration)}</Body1>
              </div>
              <div className={styles.metricItem}>
                <Caption1>LLM Analysis</Caption1>
                <Body1>{suggestion.usedLlmAnalysis ? 'Yes' : 'No'}</Body1>
              </div>
              <div className={styles.metricItem}>
                <Caption1>Confidence</Caption1>
                <Body1>{suggestion.confidence.toFixed(1)}%</Body1>
              </div>
            </div>
          </div>
        )}

        {/* Expand/Collapse Button */}
        <Button
          appearance="subtle"
          icon={expanded ? <ChevronUp24Regular /> : <ChevronDown24Regular />}
          onClick={() => setExpanded(!expanded)}
          className={styles.expandButton}
        >
          {expanded ? 'Show Less' : 'Show More Details'}
        </Button>

        {/* Action Buttons */}
        {!isApplied && (
          <div className={styles.actions}>
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={() => onReject(suggestion.sceneIndex)}
            >
              Reject
            </Button>
            <Button
              appearance="primary"
              icon={<Checkmark24Regular />}
              onClick={() => onAccept(suggestion.sceneIndex)}
            >
              Accept
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
};
