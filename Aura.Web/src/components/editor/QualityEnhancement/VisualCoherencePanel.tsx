/**
 * Visual Coherence Panel Component
 * UI for analyzing visual coherence across scenes
 */

import React, { useState } from 'react';
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
import { Video24Regular, CheckmarkCircle24Regular, Warning24Regular } from '@fluentui/react-icons';
import {
  visualAnalysisService,
  type VisualCoherenceReport,
  ColorMood,
  TimeOfDay,
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
  scoreRow: {
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
  recommendationItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
  },
});

interface VisualCoherencePanelProps {
  onApplyEnhancement?: (enhancement: any) => void;
}

export const VisualCoherencePanel: React.FC<VisualCoherencePanelProps> = ({
  onApplyEnhancement,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState<VisualCoherenceReport | null>(null);
  const [theme, setTheme] = useState<string | null>(null);

  const handleAnalyze = async () => {
    setLoading(true);
    try {
      // Sample scene data (in real app, would come from actual scenes)
      const scenes = [
        {
          sceneIndex: 0,
          timeOfDay: TimeOfDay.Morning,
          dominantMood: ColorMood.Natural,
          tags: ['outdoor', 'bright'],
          colorHistogram: { red: 0.3, blue: 0.3, green: 0.4 },
        },
        {
          sceneIndex: 1,
          timeOfDay: TimeOfDay.Midday,
          dominantMood: ColorMood.Vibrant,
          tags: ['outdoor', 'sunny'],
          colorHistogram: { red: 0.35, blue: 0.25, green: 0.4 },
        },
        {
          sceneIndex: 2,
          timeOfDay: TimeOfDay.Afternoon,
          dominantMood: ColorMood.Warm,
          tags: ['indoor', 'warm'],
          colorHistogram: { red: 0.4, blue: 0.2, green: 0.4 },
        },
      ];

      const [coherenceReport, visualTheme] = await Promise.all([
        visualAnalysisService.analyzeCoherence(scenes),
        visualAnalysisService.detectVisualTheme(scenes),
      ]);

      setReport(coherenceReport);
      setTheme(visualTheme);
    } catch (error) {
      console.error('Failed to analyze coherence:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApplyFixes = () => {
    if (report) {
      onApplyEnhancement?.({
        type: 'coherence',
        fixes: report.recommendations,
      });
    }
  };

  const getProgressColor = (score: number) => {
    if (score >= 0.7) return 'success';
    if (score >= 0.5) return 'warning';
    return 'error';
  };

  const getBadgeColor = (score: number): 'success' | 'warning' | 'danger' => {
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
            <Video24Regular />
            <Body1Strong>Visual Coherence Analysis</Body1Strong>
          </div>

          <Body1>Ensure visual consistency and coherence across all scenes in your video.</Body1>

          <Button
            appearance="primary"
            onClick={handleAnalyze}
            disabled={loading}
            icon={loading ? <Spinner size="tiny" /> : <Video24Regular />}
            style={{ marginTop: tokens.spacingVerticalL }}
          >
            {loading ? 'Analyzing...' : 'Analyze Coherence'}
          </Button>
        </div>
      </Card>

      {theme && (
        <Card>
          <div className={styles.section}>
            <Body1Strong>Detected Visual Theme</Body1Strong>
            <Caption1 block style={{ marginTop: tokens.spacingVerticalS }}>
              {theme}
            </Caption1>
          </div>
        </Card>
      )}

      {report && (
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
              <Body1Strong>Coherence Report</Body1Strong>
              {report.recommendations.length > 0 && (
                <Button
                  appearance="primary"
                  onClick={handleApplyFixes}
                  icon={<CheckmarkCircle24Regular />}
                >
                  Apply Fixes
                </Button>
              )}
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Overall Coherence</Caption1>
              <ProgressBar
                value={report.overallCoherenceScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(report.overallCoherenceScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(report.overallCoherenceScore)}>
                {(report.overallCoherenceScore * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Style Consistency</Caption1>
              <ProgressBar
                value={report.styleConsistencyScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(report.styleConsistencyScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(report.styleConsistencyScore)}>
                {(report.styleConsistencyScore * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Color Consistency</Caption1>
              <ProgressBar
                value={report.colorConsistencyScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(report.colorConsistencyScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(report.colorConsistencyScore)}>
                {(report.colorConsistencyScore * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Lighting Consistency</Caption1>
              <ProgressBar
                value={report.lightingConsistencyScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(report.lightingConsistencyScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(report.lightingConsistencyScore)}>
                {(report.lightingConsistencyScore * 100).toFixed(0)}%
              </Badge>
            </div>

            {report.inconsistencies.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalL }}>
                <Body1Strong>Inconsistencies Detected</Body1Strong>
                {report.inconsistencies.map((issue, index) => (
                  <div key={index} className={styles.issueItem}>
                    <Warning24Regular />
                    <Caption1>{issue}</Caption1>
                  </div>
                ))}
              </div>
            )}

            {report.recommendations.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalL }}>
                <Body1Strong>Recommendations</Body1Strong>
                {report.recommendations.map((rec, index) => (
                  <div key={index} className={styles.recommendationItem}>
                    <Caption1>{rec}</Caption1>
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

export default VisualCoherencePanel;
