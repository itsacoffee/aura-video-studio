import {
  Button,
  Card,
  Text,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Badge,
} from '@fluentui/react-components';
import { Eye24Regular, CheckmarkCircle24Regular, Warning24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  description: {
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  overallScore: {
    padding: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'center',
  },
  overallScoreValue: {
    fontSize: '48px',
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorNeutralForegroundInverted,
    marginBottom: tokens.spacingVerticalS,
  },
  overallScoreLabel: {
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorNeutralForegroundInverted,
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  metricCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    marginBottom: tokens.spacingVerticalXS,
  },
  metricValue: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  scoreBar: {
    height: '10px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalS,
    overflow: 'hidden',
  },
  scoreBarFill: {
    height: '100%',
    transition: 'width 0.3s ease',
  },
  issuesSection: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  issuesList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  issueItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
  recommendationsSection: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  recommendationsList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  recommendationItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
  },
  qualityBadge: {
    marginTop: tokens.spacingVerticalS,
  },
  demoNote: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
});

interface CoherenceReport {
  styleConsistencyScore: number;
  colorConsistencyScore: number;
  lightingConsistencyScore: number;
  overallCoherenceScore: number;
  inconsistencies: string[];
  recommendations: string[];
}

export const VisualCoherence: FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CoherenceReport | null>(null);

  const handleAnalyze = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const demoScenes = [
        {
          sceneIndex: 0,
          timeOfDay: 'Morning',
          dominantMood: 'Natural',
          tags: ['outdoor', 'bright', 'cheerful'],
          colorHistogram: {
            warm: 0.6,
            cool: 0.2,
            neutral: 0.2,
          },
        },
        {
          sceneIndex: 1,
          timeOfDay: 'Midday',
          dominantMood: 'Natural',
          tags: ['outdoor', 'bright', 'energetic'],
          colorHistogram: {
            warm: 0.5,
            cool: 0.3,
            neutral: 0.2,
          },
        },
        {
          sceneIndex: 2,
          timeOfDay: 'Afternoon',
          dominantMood: 'Warm',
          tags: ['outdoor', 'soft', 'calm'],
          colorHistogram: {
            warm: 0.7,
            cool: 0.1,
            neutral: 0.2,
          },
        },
      ];

      const response = await fetch('/api/aesthetics/coherence/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(demoScenes),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze visual coherence');
      }

      const data = await response.json();
      setResult(data);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj.message);
    } finally {
      setLoading(false);
    }
  }, []);

  const getScoreColor = (score: number): string => {
    if (score >= 0.8) return tokens.colorPaletteGreenForeground1;
    if (score >= 0.6) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getCoherenceBadge = (score: number) => {
    if (score >= 0.85) return { text: 'Excellent', color: 'success' as const };
    if (score >= 0.7) return { text: 'Good', color: 'success' as const };
    if (score >= 0.5) return { text: 'Acceptable', color: 'warning' as const };
    return { text: 'Needs Improvement', color: 'danger' as const };
  };

  return (
    <Card className={styles.toolCard}>
      <Title2>Visual Coherence Analysis</Title2>
      <Text className={styles.description}>
        Analyze visual consistency across scenes including color palette, lighting, and style
        coherence
      </Text>

      <div className={styles.demoNote}>
        <Text>
          This demo analyzes sample scenes. In production, you would upload a complete video project
          for comprehensive analysis.
        </Text>
      </div>

      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<Eye24Regular />}
          onClick={handleAnalyze}
          disabled={loading}
        >
          {loading ? <Spinner size="tiny" /> : 'Analyze Visual Coherence'}
        </Button>
      </div>

      {error && (
        <Text
          style={{ color: tokens.colorPaletteRedForeground1, marginTop: tokens.spacingVerticalM }}
        >
          Error: {error}
        </Text>
      )}

      {result && (
        <div className={styles.resultsSection}>
          <div className={styles.overallScore}>
            <div className={styles.overallScoreValue}>
              {(result.overallCoherenceScore * 100).toFixed(0)}%
            </div>
            <Text className={styles.overallScoreLabel}>Overall Coherence Score</Text>
            <Badge
              className={styles.qualityBadge}
              color={getCoherenceBadge(result.overallCoherenceScore).color}
              size="large"
            >
              {getCoherenceBadge(result.overallCoherenceScore).text}
            </Badge>
          </div>

          <Title3>Coherence Metrics</Title3>
          <div className={styles.metricsGrid}>
            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Style Consistency</Text>
              <Text className={styles.metricValue}>
                {(result.styleConsistencyScore * 100).toFixed(0)}%
              </Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.styleConsistencyScore * 100}%`,
                    backgroundColor: getScoreColor(result.styleConsistencyScore),
                  }}
                />
              </div>
              <Text
                style={{
                  fontSize: tokens.fontSizeBase200,
                  color: tokens.colorNeutralForeground3,
                  marginTop: tokens.spacingVerticalXS,
                }}
              >
                Visual style uniformity across scenes
              </Text>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Color Consistency</Text>
              <Text className={styles.metricValue}>
                {(result.colorConsistencyScore * 100).toFixed(0)}%
              </Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.colorConsistencyScore * 100}%`,
                    backgroundColor: getScoreColor(result.colorConsistencyScore),
                  }}
                />
              </div>
              <Text
                style={{
                  fontSize: tokens.fontSizeBase200,
                  color: tokens.colorNeutralForeground3,
                  marginTop: tokens.spacingVerticalXS,
                }}
              >
                Color palette harmony between scenes
              </Text>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Lighting Consistency</Text>
              <Text className={styles.metricValue}>
                {(result.lightingConsistencyScore * 100).toFixed(0)}%
              </Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.lightingConsistencyScore * 100}%`,
                    backgroundColor: getScoreColor(result.lightingConsistencyScore),
                  }}
                />
              </div>
              <Text
                style={{
                  fontSize: tokens.fontSizeBase200,
                  color: tokens.colorNeutralForeground3,
                  marginTop: tokens.spacingVerticalXS,
                }}
              >
                Lighting and time-of-day consistency
              </Text>
            </div>
          </div>

          {result.inconsistencies.length > 0 && (
            <div className={styles.issuesSection}>
              <Title3>Detected Inconsistencies</Title3>
              <ul className={styles.issuesList}>
                {result.inconsistencies.map((inconsistency, index) => (
                  <li key={index} className={styles.issueItem}>
                    <Warning24Regular
                      style={{ color: tokens.colorPaletteYellowForeground1, flexShrink: 0 }}
                    />
                    <Text>{inconsistency}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.recommendations.length > 0 && (
            <div className={styles.recommendationsSection}>
              <Title3>Recommendations</Title3>
              <ul className={styles.recommendationsList}>
                {result.recommendations.map((rec, index) => (
                  <li key={index} className={styles.recommendationItem}>
                    <CheckmarkCircle24Regular
                      style={{ color: tokens.colorBrandForeground1, flexShrink: 0 }}
                    />
                    <Text>{rec}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.inconsistencies.length === 0 && result.overallCoherenceScore >= 0.85 && (
            <div className={styles.issuesSection}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <CheckmarkCircle24Regular
                  style={{ color: tokens.colorPaletteGreenForeground1, fontSize: '24px' }}
                />
                <Text>
                  Excellent visual coherence! Your scenes maintain consistent style, color, and
                  lighting throughout.
                </Text>
              </div>
            </div>
          )}
        </div>
      )}
    </Card>
  );
};

export default VisualCoherence;
