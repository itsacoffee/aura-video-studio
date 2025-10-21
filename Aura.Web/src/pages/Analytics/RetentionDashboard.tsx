import { useState } from 'react';
import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Field,
  Textarea,
  Dropdown,
  Option,
  Spinner,
  Badge,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { analyticsService, type RetentionPrediction } from '../../services/analytics/PlatformService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXL,
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
  results: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  scoreCard: {
    padding: tokens.spacingVerticalL,
  },
  retentionCurve: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  dips: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  dipItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderLeft: `4px solid ${tokens.colorPaletteDarkOrangeBorder1}`,
    paddingLeft: tokens.spacingHorizontalM,
  },
  recommendations: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  recommendationItem: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export function RetentionDashboard() {
  const styles = useStyles();
  const [content, setContent] = useState('');
  const [contentType, setContentType] = useState('tutorial');
  const [videoDuration, setVideoDuration] = useState('00:10:00');
  const [targetDemographic, setTargetDemographic] = useState('');
  const [loading, setLoading] = useState(false);
  const [prediction, setPrediction] = useState<RetentionPrediction | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleAnalyze = async () => {
    if (!content.trim()) {
      setError('Please enter content to analyze');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await analyticsService.predictRetention(
        content,
        contentType,
        videoDuration,
        targetDemographic || undefined
      );
      setPrediction(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to predict retention');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Audience Retention Analytics</Title1>
        <Text>Analyze and predict how your audience will engage with your content</Text>
      </div>

      <Card className={styles.form}>
        <Field label="Content Script">
          <Textarea
            placeholder="Enter your video script or content..."
            value={content}
            onChange={(_, data) => setContent(data.value)}
            rows={8}
            resize="vertical"
          />
        </Field>

        <div className={styles.formRow}>
          <Field label="Content Type">
            <Dropdown
              value={contentType}
              selectedOptions={[contentType]}
              onOptionSelect={(_, data) => setContentType(data.optionValue || 'tutorial')}
            >
              <Option value="tutorial">Tutorial</Option>
              <Option value="entertainment">Entertainment</Option>
              <Option value="educational">Educational</Option>
              <Option value="short">Short Form</Option>
            </Dropdown>
          </Field>

          <Field label="Video Duration">
            <Textarea
              value={videoDuration}
              onChange={(_, data) => setVideoDuration(data.value)}
              placeholder="HH:MM:SS"
              rows={1}
            />
          </Field>
        </div>

        <Field label="Target Demographic (Optional)">
          <Textarea
            value={targetDemographic}
            onChange={(_, data) => setTargetDemographic(data.value)}
            placeholder="e.g., Young adults, Tech professionals"
            rows={1}
          />
        </Field>

        <Button
          appearance="primary"
          onClick={handleAnalyze}
          disabled={loading || !content.trim()}
        >
          {loading ? <Spinner size="tiny" /> : 'Analyze Retention'}
        </Button>

        {error && (
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>{error}</Text>
        )}
      </Card>

      {prediction && (
        <div className={styles.results}>
          <Card className={styles.scoreCard}>
            <Title2>Predicted Average Retention</Title2>
            <Title1 style={{ color: tokens.colorBrandForeground1 }}>
              {(prediction.predictedAverageRetention * 100).toFixed(1)}%
            </Title1>
            <Text>Optimal Length: {prediction.optimalLength}</Text>
          </Card>

          {prediction.engagementDips.length > 0 && (
            <Card>
              <Title2>Engagement Dip Points</Title2>
              <div className={styles.dips}>
                {prediction.engagementDips.map((dip, index) => (
                  <div key={index} className={styles.dipItem}>
                    <Badge
                      appearance="filled"
                      color={dip.severity === 'High' ? 'danger' : 'warning'}
                    >
                      {dip.severity}
                    </Badge>
                    <div>
                      <Text weight="semibold">At {dip.timePoint}</Text>
                      <Text> - {dip.reason}</Text>
                      <Text> (Drop: {(dip.retentionDrop * 100).toFixed(1)}%)</Text>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          <Card>
            <Title2>Recommendations</Title2>
            <div className={styles.recommendations}>
              {prediction.recommendations.map((rec, index) => (
                <div key={index} className={styles.recommendationItem}>
                  <Text>{rec}</Text>
                </div>
              ))}
            </div>
          </Card>

          <Card>
            <Title2>Retention Curve</Title2>
            <div className={styles.retentionCurve}>
              <Text size={200}>Predicted retention over time:</Text>
              {prediction.retentionCurve.map((point, index) => (
                <div key={index} style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <Text size={200} style={{ minWidth: '80px' }}>
                    {point.timePoint}
                  </Text>
                  <div
                    style={{
                      width: `${point.retention * 100}%`,
                      height: '20px',
                      backgroundColor: tokens.colorBrandBackground,
                      borderRadius: tokens.borderRadiusMedium,
                    }}
                  />
                  <Text size={200}>{(point.retention * 100).toFixed(1)}%</Text>
                </div>
              ))}
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
