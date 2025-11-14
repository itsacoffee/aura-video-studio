import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  Color24Regular,
  ImageEdit24Regular,
  CheckmarkCircle24Regular,
  Eye24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback } from 'react';
import CompositionAnalysis from '../../components/aesthetics/CompositionAnalysis';
import QualityAssessment from '../../components/aesthetics/QualityAssessment';
import VisualCoherence from '../../components/aesthetics/VisualCoherence';
import { ErrorState } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
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
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

type ToolTab = 'color-grading' | 'composition' | 'quality' | 'coherence';

export const AestheticsPage: React.FC = () => {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState<ToolTab>('color-grading');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [contentType, setContentType] = useState('educational');
  const [sentiment, setSentiment] = useState('neutral');
  const [timeOfDay, setTimeOfDay] = useState('day');
  const [result, setResult] = useState<unknown>(null);

  const handleColorGradingAnalysis = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/aesthetics/color-grading/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contentType,
          sentiment,
          timeOfDay,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to analyze color grading');
      }

      const data = await response.json();
      setResult(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [contentType, sentiment, timeOfDay]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Eye24Regular className={styles.headerIcon} />
        <div>
          <Title1>Visual Aesthetics Enhancement</Title1>
          <Text className={styles.subtitle}>
            AI-powered tools for color grading, composition analysis, quality assessment, and visual
            coherence
          </Text>
        </div>
      </div>

      <TabList
        className={styles.tabs}
        selectedValue={activeTab}
        onTabSelect={(_, data) => setActiveTab(data.value as ToolTab)}
      >
        <Tab value="color-grading" icon={<Color24Regular />}>
          Color Grading
        </Tab>
        <Tab value="composition" icon={<ImageEdit24Regular />}>
          Composition
        </Tab>
        <Tab value="quality" icon={<CheckmarkCircle24Regular />}>
          Quality Assessment
        </Tab>
        <Tab value="coherence" icon={<Eye24Regular />}>
          Visual Coherence
        </Tab>
      </TabList>

      {error && <ErrorState message={error} />}

      {activeTab === 'color-grading' && (
        <Card className={styles.toolCard}>
          <Title2>Mood-Based Color Grading</Title2>
          <Text>Analyze and suggest color grading based on content type and mood</Text>

          <div className={styles.form}>
            <Field label="Content Type">
              <Dropdown
                value={contentType}
                onOptionSelect={(_, data) => setContentType(data.optionValue as string)}
              >
                <Option value="educational">Educational</Option>
                <Option value="entertainment">Entertainment</Option>
                <Option value="documentary">Documentary</Option>
                <Option value="commercial">Commercial</Option>
                <Option value="cinematic">Cinematic</Option>
              </Dropdown>
            </Field>

            <Field label="Sentiment">
              <Dropdown
                value={sentiment}
                onOptionSelect={(_, data) => setSentiment(data.optionValue as string)}
              >
                <Option value="positive">Positive</Option>
                <Option value="neutral">Neutral</Option>
                <Option value="negative">Negative</Option>
                <Option value="dramatic">Dramatic</Option>
                <Option value="energetic">Energetic</Option>
              </Dropdown>
            </Field>

            <Field label="Time of Day">
              <Dropdown
                value={timeOfDay}
                onOptionSelect={(_, data) => setTimeOfDay(data.optionValue as string)}
              >
                <Option value="day">Day</Option>
                <Option value="night">Night</Option>
                <Option value="golden-hour">Golden Hour</Option>
                <Option value="blue-hour">Blue Hour</Option>
              </Dropdown>
            </Field>

            <div className={styles.actions}>
              <Button
                appearance="primary"
                icon={<Color24Regular />}
                onClick={handleColorGradingAnalysis}
                disabled={loading}
              >
                {loading ? <Spinner size="tiny" /> : 'Analyze Color Grading'}
              </Button>
            </div>
          </div>

          {result && (
            <div className={styles.resultsSection}>
              <Title2>Analysis Results</Title2>
              <pre>{JSON.stringify(result, null, 2)}</pre>
            </div>
          )}
        </Card>
      )}

      {activeTab === 'composition' && <CompositionAnalysis />}

      {activeTab === 'quality' && <QualityAssessment />}

      {activeTab === 'coherence' && <VisualCoherence />}
    </div>
  );
};

export default AestheticsPage;
