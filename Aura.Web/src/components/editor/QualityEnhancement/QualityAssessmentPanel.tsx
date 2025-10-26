/**
 * Quality Assessment Panel Component
 * UI for technical quality assessment and enhancement suggestions
 */

import {
  Card,
  makeStyles,
  tokens,
  Button,
  Body1,
  Body1Strong,
  Caption1,
  Badge,
  Spinner,
  ProgressBar,
} from '@fluentui/react-components';
import { Eye24Regular, CheckmarkCircle24Regular, Warning24Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import {
  visualAnalysisService,
  type QualityMetrics,
  QualityLevel,
} from '../../../services/analysis/VisualAnalysisService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  resultCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  metricRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
  },
  issueItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  enhancementItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
  },
});

interface QualityAssessmentPanelProps {
  sceneIndex?: number;
  onApplyEnhancement?: (enhancement: unknown) => void;
}

export const QualityAssessmentPanel: React.FC<QualityAssessmentPanelProps> = ({
  sceneIndex,
  onApplyEnhancement,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<QualityMetrics | null>(null);
  const [enhancements, setEnhancements] = useState<Record<string, number> | null>(null);

  const handleAssess = async () => {
    setLoading(true);
    try {
      // Sample video dimensions (in real app, would come from actual video)
      const resolutionWidth = 1920;
      const resolutionHeight = 1080;

      const qualityMetrics = await visualAnalysisService.assessQuality(
        resolutionWidth,
        resolutionHeight,
        0.75, // sharpness
        0.15, // noise level
        0.8 // compression quality
      );

      setMetrics(qualityMetrics);

      // Get enhancement suggestions
      const suggestions = await visualAnalysisService.suggestEnhancements(qualityMetrics);
      setEnhancements(suggestions);
    } catch (error) {
      console.error('Failed to assess quality:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApplyEnhancements = () => {
    if (enhancements) {
      onApplyEnhancement?.({
        type: 'quality',
        enhancements,
        sceneIndex,
      });
    }
  };

  const getQualityColor = (level: QualityLevel): 'success' | 'warning' | 'danger' | 'subtle' => {
    switch (level) {
      case QualityLevel.Excellent:
      case QualityLevel.Good:
        return 'success';
      case QualityLevel.Acceptable:
        return 'warning';
      case QualityLevel.Poor:
      case QualityLevel.Unacceptable:
        return 'danger';
      default:
        return 'subtle';
    }
  };

  const getProgressColor = (value: number, inverted = false) => {
    const score = inverted ? 1 - value : value;
    if (score >= 0.7) return 'success';
    if (score >= 0.5) return 'warning';
    return 'error';
  };

  const getBadgeColor = (value: number, inverted = false): 'success' | 'warning' | 'danger' => {
    const score = inverted ? 1 - value : value;
    if (score >= 0.7) return 'success';
    if (score >= 0.5) return 'warning';
    return 'danger';
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.section}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: tokens.spacingHorizontalS,
              marginBottom: tokens.spacingVerticalM,
            }}
          >
            <Eye24Regular />
            <Body1Strong>Quality Assessment</Body1Strong>
          </div>

          <Body1>Assess technical quality and get AI-powered enhancement suggestions.</Body1>

          <Button
            appearance="primary"
            onClick={handleAssess}
            disabled={loading}
            icon={loading ? <Spinner size="tiny" /> : <Eye24Regular />}
            style={{ marginTop: tokens.spacingVerticalL }}
          >
            {loading ? 'Assessing...' : 'Assess Quality'}
          </Button>
        </div>
      </Card>

      {metrics && (
        <Card>
          <div className={styles.resultCard}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: tokens.spacingVerticalM,
              }}
            >
              <div>
                <Body1Strong>Quality Metrics</Body1Strong>
                <div style={{ marginTop: tokens.spacingVerticalXS }}>
                  <Badge appearance="tint" color={getQualityColor(metrics.overallQuality)}>
                    {metrics.overallQuality}
                  </Badge>
                </div>
              </div>
              {enhancements && Object.keys(enhancements).length > 0 && (
                <Button
                  appearance="primary"
                  onClick={handleApplyEnhancements}
                  icon={<CheckmarkCircle24Regular />}
                >
                  Apply Enhancements
                </Button>
              )}
            </div>

            <div className={styles.metricRow}>
              <Caption1>Resolution</Caption1>
              <ProgressBar
                value={metrics.resolution}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(metrics.resolution)}
              />
              <Badge appearance="tint" color={getBadgeColor(metrics.resolution)}>
                {(metrics.resolution * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.metricRow}>
              <Caption1>Sharpness</Caption1>
              <ProgressBar
                value={metrics.sharpness}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(metrics.sharpness)}
              />
              <Badge appearance="tint" color={getBadgeColor(metrics.sharpness)}>
                {(metrics.sharpness * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.metricRow}>
              <Caption1>Noise Level</Caption1>
              <ProgressBar
                value={metrics.noiseLevel}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(metrics.noiseLevel, true)}
              />
              <Badge appearance="tint" color={getBadgeColor(metrics.noiseLevel, true)}>
                {(metrics.noiseLevel * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.metricRow}>
              <Caption1>Compression Quality</Caption1>
              <ProgressBar
                value={metrics.compressionQuality}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(metrics.compressionQuality)}
              />
              <Badge appearance="tint" color={getBadgeColor(metrics.compressionQuality)}>
                {(metrics.compressionQuality * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.metricRow}>
              <Caption1>Color Accuracy</Caption1>
              <ProgressBar
                value={metrics.colorAccuracy}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(metrics.colorAccuracy)}
              />
              <Badge appearance="tint" color={getBadgeColor(metrics.colorAccuracy)}>
                {(metrics.colorAccuracy * 100).toFixed(0)}%
              </Badge>
            </div>

            {metrics.issues.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalL }}>
                <Body1Strong>Quality Issues</Body1Strong>
                {metrics.issues.map((issue, index) => (
                  <div key={index} className={styles.issueItem}>
                    <Warning24Regular />
                    <Caption1>{issue}</Caption1>
                  </div>
                ))}
              </div>
            )}

            {enhancements && Object.keys(enhancements).length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalL }}>
                <Body1Strong>Suggested Enhancements</Body1Strong>
                {Object.entries(enhancements).map(([key, value]) => (
                  <div key={key} className={styles.enhancementItem}>
                    <div
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                      }}
                    >
                      <Caption1>{key.charAt(0).toUpperCase() + key.slice(1)}</Caption1>
                      <Badge appearance="tint" color="brand">
                        {value.toFixed(2)}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Card>
      )}
    </div>
  );
};

export default QualityAssessmentPanel;
