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
} from '@fluentui/react-components';
import { ImageEdit24Regular, CheckmarkCircle24Regular } from '@fluentui/react-icons';
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
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
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
  recommendations: {
    padding: tokens.spacingVerticalM,
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
  visualOverlay: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  focalPointInfo: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface CompositionResult {
  suggestedRule: string;
  compositionScore: number;
  balanceScore: number;
  recommendations: string[];
  focalPoint: { x: number; y: number } | null;
  suggestedCrop: { x: number; y: number; width: number; height: number } | null;
}

export const CompositionAnalysis: FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CompositionResult | null>(null);
  const [imageWidth, setImageWidth] = useState('1920');
  const [imageHeight, setImageHeight] = useState('1080');

  const handleAnalyze = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/aesthetics/composition/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          imageWidth: parseInt(imageWidth, 10),
          imageHeight: parseInt(imageHeight, 10),
          subjectPosition: null,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze composition');
      }

      const data = await response.json();
      setResult(data);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj.message);
    } finally {
      setLoading(false);
    }
  }, [imageWidth, imageHeight]);

  const getScoreColor = (score: number): string => {
    if (score >= 0.8) return tokens.colorPaletteGreenForeground1;
    if (score >= 0.6) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  return (
    <Card className={styles.toolCard}>
      <Title2>Composition Analysis</Title2>
      <Text>
        Analyze image composition using rule of thirds, golden ratio, and balance assessment
      </Text>

      <div className={styles.form}>
        <Field label="Image Width (pixels)">
          <Input
            type="number"
            value={imageWidth}
            onChange={(_, data) => setImageWidth(data.value)}
          />
        </Field>

        <Field label="Image Height (pixels)">
          <Input
            type="number"
            value={imageHeight}
            onChange={(_, data) => setImageHeight(data.value)}
          />
        </Field>

        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<ImageEdit24Regular />}
            onClick={handleAnalyze}
            disabled={loading}
          >
            {loading ? <Spinner size="tiny" /> : 'Analyze Composition'}
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
          <Title3>Analysis Results</Title3>

          <div className={styles.metricsGrid}>
            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Composition Score</Text>
              <Text className={styles.metricValue}>
                {(result.compositionScore * 100).toFixed(0)}%
              </Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.compositionScore * 100}%`,
                    backgroundColor: getScoreColor(result.compositionScore),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Balance Score</Text>
              <Text className={styles.metricValue}>{(result.balanceScore * 100).toFixed(0)}%</Text>
              <div className={styles.scoreBar}>
                <div
                  className={styles.scoreBarFill}
                  style={{
                    width: `${result.balanceScore * 100}%`,
                    backgroundColor: getScoreColor(result.balanceScore),
                  }}
                />
              </div>
            </div>

            <div className={styles.metricCard}>
              <Text className={styles.metricLabel}>Suggested Rule</Text>
              <Text className={styles.metricValue}>
                {result.suggestedRule.replace(/([A-Z])/g, ' $1').trim()}
              </Text>
            </div>
          </div>

          {result.focalPoint && (
            <div className={styles.focalPointInfo}>
              <Title3>Focal Point</Title3>
              <Text>
                Detected at position: X = {result.focalPoint.x.toFixed(0)}, Y ={' '}
                {result.focalPoint.y.toFixed(0)}
              </Text>
            </div>
          )}

          {result.recommendations.length > 0 && (
            <div className={styles.recommendations}>
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

          {result.suggestedCrop && (
            <div className={styles.visualOverlay}>
              <Title3>Suggested Crop</Title3>
              <Text>
                Reframe to improve composition: X={result.suggestedCrop.x.toFixed(0)}, Y=
                {result.suggestedCrop.y.toFixed(0)}, Width=
                {result.suggestedCrop.width.toFixed(0)}, Height=
                {result.suggestedCrop.height.toFixed(0)}
              </Text>
            </div>
          )}
        </div>
      )}
    </Card>
  );
};

export default CompositionAnalysis;
