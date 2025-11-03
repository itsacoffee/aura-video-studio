import {
  Button,
  Card,
  Text,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Field,
  Input,
  Badge,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  ErrorCircle24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '600px',
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
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
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  metricCard: {
    padding: tokens.spacingVerticalM,
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
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  scoreBar: {
    height: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalXS,
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
    gap: tokens.spacingVerticalM,
  },
  issueItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  qualityBadge: {
    marginTop: tokens.spacingVerticalS,
  },
});

interface QualityMetrics {
  resolution: number;
  sharpness: number;
  noiseLevel: number;
  compressionQuality: number;
  colorAccuracy: number;
  overallQuality: string;
  issues: string[];
}

export const QualityAssessment: FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<QualityMetrics | null>(null);
  const [resolutionWidth, setResolutionWidth] = useState('1920');
  const [resolutionHeight, setResolutionHeight] = useState('1080');
  const [sharpness, setSharpness] = useState('0.75');
  const [noiseLevel, setNoiseLevel] = useState('0.15');
  const [compressionQuality, setCompressionQuality] = useState('0.8');

  const handleAssess = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/aesthetics/quality/assess', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          resolutionWidth: parseInt(resolutionWidth, 10),
          resolutionHeight: parseInt(resolutionHeight, 10),
          sharpness: parseFloat(sharpness),
          noiseLevel: parseFloat(noiseLevel),
          compressionQuality: parseFloat(compressionQuality),
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to assess quality');
      }

      const data = await response.json();
      setResult(data);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj.message);
    } finally {
      setLoading(false);
    }
  }, [resolutionWidth, resolutionHeight, sharpness, noiseLevel, compressionQuality]);

  const getScoreColor = (score: number): string => {
    if (score >= 0.8) return tokens.colorPaletteGreenForeground1;
    if (score >= 0.6) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const getQualityBadgeColor = (quality: string) => {
    switch (quality) {
      case 'Excellent':
      case 'Good':
        return 'success' as const;
      case 'Acceptable':
        return 'warning' as const;
      case 'Poor':
      case 'Unacceptable':
        return 'danger' as const;
      default:
        return 'informative' as const;
    }
  };

  const getIssueIcon = (issue: string) => {
    if (issue.includes('Low resolution') || issue.includes('Unacceptable')) {
      return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
    }
    return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
  };

  const calculateOverallScore = (metrics: QualityMetrics): number => {
    const scores = [
      metrics.resolution,
      metrics.sharpness,
      1.0 - metrics.noiseLevel,
      metrics.compressionQuality,
      metrics.colorAccuracy,
    ];
    return scores.reduce((a, b) => a + b, 0) / scores.length;
  };

  return (
    <Card className={styles.toolCard}>
      <Title2>Quality Assessment</Title2>
      <Text>Assess technical quality metrics and get enhancement recommendations</Text>

      <div className={styles.form}>
        <div className={styles.formRow}>
          <Field label="Resolution Width">
            <Input
              type="number"
              value={resolutionWidth}
              onChange={(_, data) => setResolutionWidth(data.value)}
            />
          </Field>

          <Field label="Resolution Height">
            <Input
              type="number"
              value={resolutionHeight}
              onChange={(_, data) => setResolutionHeight(data.value)}
            />
          </Field>
        </div>

        <div className={styles.formRow}>
          <Field label="Sharpness (0-1)">
            <Input
              type="number"
              value={sharpness}
              step="0.01"
              min="0"
              max="1"
              onChange={(_, data) => setSharpness(data.value)}
            />
          </Field>

          <Field label="Noise Level (0-1)">
            <Input
              type="number"
              value={noiseLevel}
              step="0.01"
              min="0"
              max="1"
              onChange={(_, data) => setNoiseLevel(data.value)}
            />
          </Field>
        </div>

        <Field label="Compression Quality (0-1)">
          <Input
            type="number"
            value={compressionQuality}
            step="0.01"
            min="0"
            max="1"
            onChange={(_, data) => setCompressionQuality(data.value)}
          />
        </Field>

        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<CheckmarkCircle24Regular />}
            onClick={handleAssess}
            disabled={loading}
          >
            {loading ? <Spinner size="tiny" /> : 'Assess Quality'}
          </Button>
        </div>
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
              {(calculateOverallScore(result) * 100).toFixed(0)}
            </div>
            <Text className={styles.overallScoreLabel}>Overall Quality Score</Text>
            <Badge
              className={styles.qualityBadge}
              color={getQualityBadgeColor(result.overallQuality)}
              size="large"
            >
              {result.overallQuality}
            </Badge>
          </div>

          <Title3>Quality Metrics</Title3>
          <div className={styles.metricsGrid}>
            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Resolution</Text>
              <Text className={styles.metricValue}>{(result.resolution * 100).toFixed(0)}%</Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.resolution * 100}%`,
                    backgroundColor: getScoreColor(result.resolution),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Sharpness</Text>
              <Text className={styles.metricValue}>{(result.sharpness * 100).toFixed(0)}%</Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.sharpness * 100}%`,
                    backgroundColor: getScoreColor(result.sharpness),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Noise Level</Text>
              <Text className={styles.metricValue}>{(result.noiseLevel * 100).toFixed(0)}%</Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.noiseLevel * 100}%`,
                    backgroundColor: getScoreColor(1.0 - result.noiseLevel),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Compression Quality</Text>
              <Text className={styles.metricValue}>
                {(result.compressionQuality * 100).toFixed(0)}%
              </Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.compressionQuality * 100}%`,
                    backgroundColor: getScoreColor(result.compressionQuality),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Color Accuracy</Text>
              <Text className={styles.metricValue}>{(result.colorAccuracy * 100).toFixed(0)}%</Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.colorAccuracy * 100}%`,
                    backgroundColor: getScoreColor(result.colorAccuracy),
                  }}
                />
              </div>
            </div>
          </div>

          {result.issues.length > 0 && (
            <div className={styles.issuesSection}>
              <Title3>Issues & Recommendations</Title3>
              <ul className={styles.issuesList}>
                {result.issues.map((issue, index) => (
                  <li key={index} className={styles.issueItem}>
                    {getIssueIcon(issue)}
                    <Text>{issue}</Text>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {result.issues.length === 0 && (
            <div className={styles.issuesSection}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <CheckmarkCircle24Regular
                  style={{ color: tokens.colorPaletteGreenForeground1, fontSize: '24px' }}
                />
                <Text>No quality issues detected. Your content meets all quality standards.</Text>
              </div>
            </div>
          )}
        </div>
      )}
    </Card>
  );
};

export default QualityAssessment;
