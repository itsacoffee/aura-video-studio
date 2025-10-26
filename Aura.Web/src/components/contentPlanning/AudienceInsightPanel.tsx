import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  tokens,
  Button,
  Dropdown,
  Option,
  Spinner,
} from '@fluentui/react-components';
import { PeopleRegular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import { contentPlanningService, AudienceInsight } from '../../services/contentPlanningService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    alignItems: 'flex-end',
  },
  input: {
    flex: '1',
    minWidth: '200px',
  },
  insightsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  sectionCard: {
    height: '100%',
  },
  chartContainer: {
    padding: tokens.spacingVerticalM,
  },
  barChart: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  barRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  barLabel: {
    width: '80px',
    fontSize: tokens.fontSizeBase200,
  },
  barTrack: {
    flex: 1,
    height: '24px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  barFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transition: 'width 0.3s ease',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-end',
    paddingRight: tokens.spacingHorizontalXS,
  },
  barValue: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralBackground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  listContainer: {
    padding: tokens.spacingVerticalM,
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  listItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: tokens.spacingVerticalXS,
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground1Hover,
  },
  recommendations: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  recommendation: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorBrandStroke1}`,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground2,
  },
});

export const AudienceInsightPanel: React.FC = () => {
  const styles = useStyles();
  const [insights, setInsights] = useState<AudienceInsight | null>(null);
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [platform, setPlatform] = useState<string>('YouTube');
  const [category, setCategory] = useState<string>('Technology');

  const platforms = ['YouTube', 'TikTok', 'Instagram'];
  const categories = ['Technology', 'Gaming', 'Fitness', 'Education', 'Entertainment', 'Lifestyle'];

  const handleAnalyze = async () => {
    setLoading(true);
    try {
      const response = await contentPlanningService.analyzeAudience({
        platform,
        category,
        contentTags: [],
      });
      setInsights(response.insights);
      setRecommendations(response.recommendations);
    } catch (error) {
      console.error('Failed to analyze audience:', error);
    } finally {
      setLoading(false);
    }
  };

  const renderDistributionChart = (title: string, data: Record<string, number>) => {
    const sortedData = Object.entries(data).sort((a, b) => b[1] - a[1]);
    return (
      <Card className={styles.sectionCard}>
        <CardHeader header={<Text weight="semibold">{title}</Text>} />
        <div className={styles.chartContainer}>
          <div className={styles.barChart}>
            {sortedData.map(([label, value]) => (
              <div key={label} className={styles.barRow}>
                <div className={styles.barLabel}>{label}</div>
                <div className={styles.barTrack}>
                  <div className={styles.barFill} style={{ width: `${value * 100}%` }}>
                    <span className={styles.barValue}>{(value * 100).toFixed(0)}%</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </Card>
    );
  };

  return (
    <div className={styles.container}>
      <Card>
        <CardHeader
          header={<Text weight="semibold">Analyze Your Audience</Text>}
          description="Understand your target audience demographics and preferences"
        />
        <div style={{ padding: tokens.spacingVerticalM }}>
          <div className={styles.controls}>
            <Dropdown
              placeholder="Select platform"
              value={platform}
              onOptionSelect={(_e, data) => setPlatform(data.optionValue as string)}
              className={styles.input}
            >
              {platforms.map((p) => (
                <Option key={p} value={p}>
                  {p}
                </Option>
              ))}
            </Dropdown>
            <Dropdown
              placeholder="Select category"
              value={category}
              onOptionSelect={(_e, data) => setCategory(data.optionValue as string)}
              className={styles.input}
            >
              {categories.map((c) => (
                <Option key={c} value={c}>
                  {c}
                </Option>
              ))}
            </Dropdown>
            <Button appearance="primary" onClick={handleAnalyze} disabled={loading}>
              Analyze
            </Button>
          </div>
        </div>
      </Card>

      {loading && (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Spinner label="Analyzing audience..." />
        </div>
      )}

      {!loading && !insights && (
        <div className={styles.emptyState}>
          <PeopleRegular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
          <Text size={400}>No insights available. Select a platform and category to analyze.</Text>
        </div>
      )}

      {!loading && insights && (
        <>
          <div className={styles.insightsGrid}>
            {renderDistributionChart('Age Distribution', insights.demographics.ageDistribution)}
            {renderDistributionChart(
              'Gender Distribution',
              insights.demographics.genderDistribution
            )}
            {renderDistributionChart(
              'Location Distribution',
              insights.demographics.locationDistribution
            )}
          </div>

          <div className={styles.insightsGrid}>
            <Card className={styles.sectionCard}>
              <CardHeader header={<Text weight="semibold">Top Interests</Text>} />
              <div className={styles.listContainer}>
                <div className={styles.list}>
                  {insights.topInterests.map((interest, idx) => (
                    <div key={idx} className={styles.listItem}>
                      <Text>{interest}</Text>
                    </div>
                  ))}
                </div>
              </div>
            </Card>

            <Card className={styles.sectionCard}>
              <CardHeader header={<Text weight="semibold">Preferred Content Types</Text>} />
              <div className={styles.listContainer}>
                <div className={styles.list}>
                  {insights.preferredContentTypes.map((type, idx) => (
                    <div key={idx} className={styles.listItem}>
                      <Text>{type}</Text>
                    </div>
                  ))}
                </div>
              </div>
            </Card>

            <Card className={styles.sectionCard}>
              <CardHeader
                header={<Text weight="semibold">Engagement Rate</Text>}
                description={`Average: ${insights.engagementRate.toFixed(2)}%`}
              />
              <div className={styles.chartContainer}>
                <div className={styles.barTrack} style={{ height: '40px' }}>
                  <div
                    className={styles.barFill}
                    style={{ width: `${Math.min(insights.engagementRate * 10, 100)}%` }}
                  >
                    <span className={styles.barValue}>{insights.engagementRate.toFixed(2)}%</span>
                  </div>
                </div>
              </div>
            </Card>
          </div>

          {recommendations.length > 0 && (
            <Card>
              <CardHeader header={<Text weight="semibold">Recommendations</Text>} />
              <div style={{ padding: tokens.spacingVerticalM }}>
                <div className={styles.recommendations}>
                  {recommendations.map((rec, idx) => (
                    <div key={idx} className={styles.recommendation}>
                      <Text>{rec}</Text>
                    </div>
                  ))}
                </div>
              </div>
            </Card>
          )}
        </>
      )}
    </div>
  );
};
