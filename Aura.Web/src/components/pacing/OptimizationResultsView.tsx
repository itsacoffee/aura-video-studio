/**
 * Optimization Results View Component
 * Visualization of optimization results and engagement predictions
 */

import {
  Card,
  makeStyles,
  tokens,
  Body1,
  Body1Strong,
  Caption1,
  Badge,
  Spinner,
  ProgressBar,
  Divider,
} from '@fluentui/react-components';
import {
  ChartMultiple24Regular,
  Eye24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { Scene } from '../../types';

interface OptimizationResultsViewProps {
  scenes: Scene[];
  optimizationActive: boolean;
}

interface OptimizationResults {
  overallEngagement: number;
  retentionRate: number;
  totalDuration: number;
  engagementDrops: EngagementDrop[];
  recommendations: string[];
}

interface EngagementDrop {
  sceneIndex: number;
  timestamp: number;
  severity: 'low' | 'medium' | 'high' | 'critical';
  recommendation: string;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  metricCard: {
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
  },
  metricValue: {
    fontSize: '32px',
    fontWeight: tokens.fontWeightSemibold,
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  section: {
    marginTop: tokens.spacingVerticalL,
  },
  dropsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  dropCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  recommendationsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  recommendationItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export const OptimizationResultsView = ({
  scenes,
  optimizationActive,
}: OptimizationResultsViewProps) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<OptimizationResults | null>(null);

  useEffect(() => {
    if (optimizationActive && scenes.length > 0) {
      generateResults();
    }
  }, [optimizationActive, scenes]);

  const generateResults = async () => {
    setLoading(true);

    try {
      // Simulate optimization results
      // In production, this would call the AttentionPredictionService and ABTestingService APIs
      await new Promise((resolve) => setTimeout(resolve, 1200));

      const totalDuration = scenes.reduce((sum, scene) => sum + scene.duration, 0);
      const avgEngagement = 0.7 + Math.random() * 0.2; // 70-90%
      const retentionRate = 0.65 + Math.random() * 0.25; // 65-90%

      const drops: EngagementDrop[] = scenes
        .filter(() => Math.random() > 0.6)
        .slice(0, 3)
        .map((scene) => ({
          sceneIndex: scenes.indexOf(scene),
          timestamp: scene.start,
          severity: (['low', 'medium', 'high', 'critical'] as const)[Math.floor(Math.random() * 4)],
          recommendation: generateDropRecommendation(),
        }));

      const recommendations = [
        'Opening scene has strong engagement - maintain this energy',
        'Consider shortening scenes 3-5 to improve pacing',
        'Add visual variety in middle section to prevent drop-off',
        'Closing section could benefit from faster pacing',
      ].slice(0, Math.floor(Math.random() * 2) + 2);

      setResults({
        overallEngagement: avgEngagement,
        retentionRate,
        totalDuration,
        engagementDrops: drops,
        recommendations,
      });
    } catch (error) {
      console.error('Results generation failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const generateDropRecommendation = (): string => {
    const recommendations = [
      'Increase pacing to maintain viewer attention',
      'Add more dynamic content or visual elements',
      'Consider splitting into shorter scenes',
      'Review script complexity - may be too dense',
    ];
    return recommendations[Math.floor(Math.random() * recommendations.length)];
  };

  const getSeverityBadge = (severity: string) => {
    const config: Record<string, { color: any; label: string }> = {
      low: { color: 'informative', label: 'Low Risk' },
      medium: { color: 'warning', label: 'Medium Risk' },
      high: { color: 'important', label: 'High Risk' },
      critical: { color: 'danger', label: 'Critical' },
    };

    const { color, label } = config[severity] || config.low;
    return (
      <Badge appearance="tint" color={color}>
        {label}
      </Badge>
    );
  };

  const getEngagementColor = (score: number): string => {
    if (score >= 0.8) return tokens.colorPaletteGreenForeground1;
    if (score >= 0.6) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Generating optimization results..." />
      </div>
    );
  }

  if (!optimizationActive) {
    return (
      <div className={styles.emptyState}>
        <Info24Regular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
        <Body1>Click &quot;Optimize Pacing&quot; to view results</Body1>
      </div>
    );
  }

  if (!results) {
    return (
      <div className={styles.emptyState}>
        <ChartMultiple24Regular
          style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }}
        />
        <Body1>No optimization results available</Body1>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {/* Key Metrics */}
      <div>
        <Body1Strong>Optimization Metrics</Body1Strong>
      </div>

      <div className={styles.metricsGrid}>
        <Card className={styles.metricCard}>
          <Eye24Regular style={{ fontSize: '24px' }} />
          <div
            className={styles.metricValue}
            style={{ color: getEngagementColor(results.overallEngagement) }}
          >
            {(results.overallEngagement * 100).toFixed(0)}%
          </div>
          <Caption1>Overall Engagement</Caption1>
          <ProgressBar
            value={results.overallEngagement}
            max={1}
            style={{ marginTop: tokens.spacingVerticalS }}
          />
        </Card>

        <Card className={styles.metricCard}>
          <CheckmarkCircle24Regular style={{ fontSize: '24px' }} />
          <div
            className={styles.metricValue}
            style={{ color: getEngagementColor(results.retentionRate) }}
          >
            {(results.retentionRate * 100).toFixed(0)}%
          </div>
          <Caption1>Predicted Retention</Caption1>
          <ProgressBar
            value={results.retentionRate}
            max={1}
            style={{ marginTop: tokens.spacingVerticalS }}
          />
        </Card>

        <Card className={styles.metricCard}>
          <ChartMultiple24Regular style={{ fontSize: '24px' }} />
          <div className={styles.metricValue}>{formatDuration(results.totalDuration)}</div>
          <Caption1>Total Duration</Caption1>
        </Card>
      </div>

      <Divider />

      {/* Engagement Drops */}
      {results.engagementDrops.length > 0 && (
        <div className={styles.section}>
          <Body1Strong>Potential Engagement Drops</Body1Strong>
          <div className={styles.dropsList}>
            {results.engagementDrops.map((drop, index) => (
              <Card key={index} className={styles.dropCard}>
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    marginBottom: tokens.spacingVerticalS,
                  }}
                >
                  <Body1Strong>Scene {drop.sceneIndex + 1}</Body1Strong>
                  {getSeverityBadge(drop.severity)}
                </div>
                <Caption1>At {formatDuration(drop.timestamp)}</Caption1>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'flex-start',
                    gap: tokens.spacingHorizontalXS,
                    marginTop: tokens.spacingVerticalS,
                  }}
                >
                  <Warning24Regular style={{ fontSize: '16px', marginTop: '2px' }} />
                  <Caption1>{drop.recommendation}</Caption1>
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Recommendations */}
      <div className={styles.section}>
        <Body1Strong>Optimization Recommendations</Body1Strong>
        <div className={styles.recommendationsList}>
          {results.recommendations.map((rec, index) => (
            <div key={index} className={styles.recommendationItem}>
              <CheckmarkCircle24Regular
                style={{ fontSize: '20px', color: tokens.colorPaletteBlueForeground2 }}
              />
              <Body1>{rec}</Body1>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
