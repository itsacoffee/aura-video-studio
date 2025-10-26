/**
 * Composition Panel Component
 * UI for composition analysis and reframing suggestions
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
import { Grid24Regular, CheckmarkCircle24Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import {
  visualAnalysisService,
  type CompositionAnalysisResult,
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
  recommendationItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
  },
});

interface CompositionPanelProps {
  sceneIndex?: number;
  onApplyEnhancement?: (enhancement: unknown) => void;
}

export const CompositionPanel: React.FC<CompositionPanelProps> = ({
  sceneIndex,
  onApplyEnhancement,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [analysis, setAnalysis] = useState<CompositionAnalysisResult | null>(null);

  // Sample dimensions (in real app, these would come from the actual image/video)
  const imageWidth = 1920;
  const imageHeight = 1080;

  const handleAnalyze = async () => {
    setLoading(true);
    try {
      const result = await visualAnalysisService.analyzeComposition(imageWidth, imageHeight);
      setAnalysis(result);
    } catch (error) {
      console.error('Failed to analyze composition:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleApply = () => {
    if (analysis?.suggestedCrop) {
      onApplyEnhancement?.({
        type: 'composition',
        crop: analysis.suggestedCrop,
        rule: analysis.suggestedRule,
        sceneIndex,
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
            <Grid24Regular />
            <Body1Strong>Composition Analysis</Body1Strong>
          </div>

          <Body1>
            Analyze and improve image composition using golden ratio and rule of thirds.
          </Body1>

          <Button
            appearance="primary"
            onClick={handleAnalyze}
            disabled={loading}
            icon={loading ? <Spinner size="tiny" /> : <Grid24Regular />}
            style={{ marginTop: tokens.spacingVerticalL }}
          >
            {loading ? 'Analyzing...' : 'Analyze Composition'}
          </Button>
        </div>
      </Card>

      {analysis && (
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
              <Body1Strong>Analysis Results</Body1Strong>
              {analysis.suggestedCrop && (
                <Button
                  appearance="primary"
                  onClick={handleApply}
                  icon={<CheckmarkCircle24Regular />}
                >
                  Apply Reframing
                </Button>
              )}
            </div>

            <div style={{ marginBottom: tokens.spacingVerticalM }}>
              <Caption1 block>Suggested Rule</Caption1>
              <Badge
                appearance="tint"
                color="brand"
                style={{ marginTop: tokens.spacingVerticalXS }}
              >
                {analysis.suggestedRule}
              </Badge>
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Composition Score</Caption1>
              <ProgressBar
                value={analysis.compositionScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(analysis.compositionScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(analysis.compositionScore)}>
                {(analysis.compositionScore * 100).toFixed(0)}%
              </Badge>
            </div>

            <div className={styles.scoreRow}>
              <Caption1>Balance Score</Caption1>
              <ProgressBar
                value={analysis.balanceScore}
                thickness="large"
                style={{ width: 120 }}
                color={getProgressColor(analysis.balanceScore)}
              />
              <Badge appearance="tint" color={getBadgeColor(analysis.balanceScore)}>
                {(analysis.balanceScore * 100).toFixed(0)}%
              </Badge>
            </div>

            {analysis.focalPoint && (
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Caption1 block>Detected Focal Point</Caption1>
                <Caption1>
                  X: {analysis.focalPoint.x.toFixed(0)}, Y: {analysis.focalPoint.y.toFixed(0)}
                </Caption1>
              </div>
            )}

            {analysis.recommendations.length > 0 && (
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Body1Strong>Recommendations</Body1Strong>
                {analysis.recommendations.map((rec, index) => (
                  <div key={index} className={styles.recommendationItem}>
                    <Caption1>{rec}</Caption1>
                  </div>
                ))}
              </div>
            )}

            {analysis.suggestedCrop && (
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Body1Strong>Suggested Crop</Body1Strong>
                <Caption1 block style={{ marginTop: tokens.spacingVerticalXS }}>
                  Position: ({analysis.suggestedCrop.x.toFixed(0)},{' '}
                  {analysis.suggestedCrop.y.toFixed(0)})
                </Caption1>
                <Caption1 block>
                  Size: {analysis.suggestedCrop.width.toFixed(0)} ×{' '}
                  {analysis.suggestedCrop.height.toFixed(0)}
                </Caption1>
              </div>
            )}
          </div>
        </Card>
      )}
    </div>
  );
};

export default CompositionPanel;
