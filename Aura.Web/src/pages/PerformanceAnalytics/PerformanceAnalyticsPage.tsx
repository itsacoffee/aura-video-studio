import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Tab,
  TabList,
  Field,
  Input,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  ChartMultiple24Regular,
  DataUsage24Regular,
  Flash24Regular,
  DocumentBulletList24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
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
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  toolHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  toolIcon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
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
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  statCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
});

type TabValue = 'import' | 'videos' | 'insights' | 'abtest';

interface VideoMetrics {
  videoId: string;
  videoTitle: string;
  views: number;
  watchTimeMinutes: number;
  engagementRate: number;
}

interface InsightData {
  totalVideos: number;
  averageViews: number;
  averageEngagementRate: number;
  overallTrend: string;
}

const PerformanceAnalyticsPage: React.FC = () => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('import');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [profileId, setProfileId] = useState('');
  const [platform, setPlatform] = useState('youtube');
  const [filePath, setFilePath] = useState('');
  const [fileType, setFileType] = useState('csv');
  const [importSuccess, setImportSuccess] = useState(false);

  const [videosProfileId, setVideosProfileId] = useState('');
  const [videos, setVideos] = useState<VideoMetrics[]>([]);

  const [insightsProfileId, setInsightsProfileId] = useState('');
  const [insights, setInsights] = useState<InsightData | null>(null);

  const [testName, setTestName] = useState('');
  const [testDescription, setTestDescription] = useState('');
  const [testProfileId, setTestProfileId] = useState('');
  const [variantCount, setVariantCount] = useState(2);
  const [testCreated, setTestCreated] = useState(false);

  const handleImport = useCallback(async () => {
    setLoading(true);
    setError(null);
    setImportSuccess(false);

    try {
      const response = await fetch('/api/performance-analytics/import', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          profileId,
          platform,
          filePath,
          fileType,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Import failed');
      }

      setImportSuccess(true);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [profileId, platform, filePath, fileType]);

  const handleFetchVideos = useCallback(async () => {
    setLoading(true);
    setError(null);
    setVideos([]);

    try {
      const response = await fetch(`/api/performance-analytics/videos/${videosProfileId}`);

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to fetch videos');
      }

      const data = await response.json();
      const videoData = data.videos.map(
        (v: {
          videoId: string;
          videoTitle: string;
          metrics: {
            Views: number;
            WatchTimeMinutes: number;
            Engagement: { EngagementRate: number };
          };
        }) => ({
          videoId: v.videoId,
          videoTitle: v.videoTitle,
          views: v.metrics.Views,
          watchTimeMinutes: v.metrics.WatchTimeMinutes,
          engagementRate: v.metrics.Engagement.EngagementRate,
        })
      );
      setVideos(videoData);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [videosProfileId]);

  const handleFetchInsights = useCallback(async () => {
    setLoading(true);
    setError(null);
    setInsights(null);

    try {
      const response = await fetch(`/api/performance-analytics/insights/${insightsProfileId}`);

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to fetch insights');
      }

      const data = await response.json();
      setInsights({
        totalVideos: data.insights.TotalVideos,
        averageViews: data.insights.AverageViews,
        averageEngagementRate: data.insights.AverageEngagementRate,
        overallTrend: data.insights.OverallTrend,
      });
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [insightsProfileId]);

  const handleCreateABTest = useCallback(async () => {
    setLoading(true);
    setError(null);
    setTestCreated(false);

    try {
      const variants = Array.from({ length: variantCount }, (_, i) => ({
        variantName: `Variant ${String.fromCharCode(65 + i)}`,
        description: `Test variant ${i + 1}`,
        projectId: null,
        configuration: {},
      }));

      const response = await fetch('/api/performance-analytics/ab-test', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          profileId: testProfileId,
          testName,
          description: testDescription,
          category: 'general',
          variants,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to create A/B test');
      }

      setTestCreated(true);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [testProfileId, testName, testDescription, variantCount]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <ChartMultiple24Regular className={styles.headerIcon} />
        <div>
          <Title1>Performance Analytics</Title1>
          <Text className={styles.subtitle}>
            Track video performance, analyze patterns, and optimize content based on real data
          </Text>
        </div>
      </div>

      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as TabValue)}
        className={styles.tabs}
      >
        <Tab value="import" icon={<DataUsage24Regular />}>
          Import Data
        </Tab>
        <Tab value="videos" icon={<DocumentBulletList24Regular />}>
          Video Performance
        </Tab>
        <Tab value="insights" icon={<Flash24Regular />}>
          Insights
        </Tab>
        <Tab value="abtest" icon={<ChartMultiple24Regular />}>
          A/B Testing
        </Tab>
      </TabList>

      <div className={styles.content}>
        {error && <ErrorState message={error} />}

        {selectedTab === 'import' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <DataUsage24Regular className={styles.toolIcon} />
              <Title2>Import Analytics Data</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Profile ID" required>
                <Input
                  value={profileId}
                  onChange={(_, data) => setProfileId(data.value)}
                  placeholder="profile-123"
                />
              </Field>
              <Field label="Platform">
                <Dropdown
                  value={platform}
                  onOptionSelect={(_, data) => setPlatform(data.optionText || 'youtube')}
                >
                  <Option>youtube</Option>
                  <Option>tiktok</Option>
                  <Option>instagram</Option>
                  <Option>twitter</Option>
                </Dropdown>
              </Field>
              <Field label="File Type">
                <Dropdown
                  value={fileType}
                  onOptionSelect={(_, data) => setFileType(data.optionText || 'csv')}
                >
                  <Option>csv</Option>
                  <Option>json</Option>
                </Dropdown>
              </Field>
              <Field label="File Path" required>
                <Input
                  value={filePath}
                  onChange={(_, data) => setFilePath(data.value)}
                  placeholder="/path/to/analytics.csv"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleImport}
                  disabled={loading || !profileId || !filePath}
                >
                  {loading ? <Spinner size="tiny" /> : 'Import Analytics'}
                </Button>
              </div>
            </div>
            {importSuccess && (
              <div className={styles.resultsSection}>
                <Title3>Import Successful</Title3>
                <Text>Analytics data has been imported successfully.</Text>
              </div>
            )}
          </Card>
        )}

        {selectedTab === 'videos' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <DocumentBulletList24Regular className={styles.toolIcon} />
              <Title2>Video Performance</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Profile ID" required>
                <Input
                  value={videosProfileId}
                  onChange={(_, data) => setVideosProfileId(data.value)}
                  placeholder="profile-123"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleFetchVideos}
                  disabled={loading || !videosProfileId}
                >
                  {loading ? <Spinner size="tiny" /> : 'Fetch Videos'}
                </Button>
              </div>
            </div>
            {videos.length > 0 && (
              <div className={styles.resultsSection}>
                <Title3>Videos ({videos.length})</Title3>
                <div className={styles.statsGrid}>
                  {videos.slice(0, 6).map((video) => (
                    <div key={video.videoId} className={styles.statCard}>
                      <Text weight="semibold">{video.videoTitle}</Text>
                      <Text>Views: {video.views.toLocaleString()}</Text>
                      <Text>Watch Time: {video.watchTimeMinutes.toFixed(1)} min</Text>
                      <Text>Engagement: {(video.engagementRate * 100).toFixed(2)}%</Text>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </Card>
        )}

        {selectedTab === 'insights' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <Flash24Regular className={styles.toolIcon} />
              <Title2>Performance Insights</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Profile ID" required>
                <Input
                  value={insightsProfileId}
                  onChange={(_, data) => setInsightsProfileId(data.value)}
                  placeholder="profile-123"
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleFetchInsights}
                  disabled={loading || !insightsProfileId}
                >
                  {loading ? <Spinner size="tiny" /> : 'Get Insights'}
                </Button>
              </div>
            </div>
            {insights && (
              <div className={styles.resultsSection}>
                <Title3>Insights</Title3>
                <div className={styles.statsGrid}>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Total Videos</Text>
                    <Text size={500}>{insights.totalVideos}</Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Avg Views</Text>
                    <Text size={500}>{insights.averageViews.toFixed(0)}</Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Avg Engagement</Text>
                    <Text size={500}>{(insights.averageEngagementRate * 100).toFixed(2)}%</Text>
                  </div>
                  <div className={styles.statCard}>
                    <Text weight="semibold">Overall Trend</Text>
                    <Text size={500}>{insights.overallTrend}</Text>
                  </div>
                </div>
              </div>
            )}
          </Card>
        )}

        {selectedTab === 'abtest' && (
          <Card className={styles.toolCard}>
            <div className={styles.toolHeader}>
              <ChartMultiple24Regular className={styles.toolIcon} />
              <Title2>A/B Testing</Title2>
            </div>
            <div className={styles.form}>
              <Field label="Profile ID" required>
                <Input
                  value={testProfileId}
                  onChange={(_, data) => setTestProfileId(data.value)}
                  placeholder="profile-123"
                />
              </Field>
              <Field label="Test Name" required>
                <Input
                  value={testName}
                  onChange={(_, data) => setTestName(data.value)}
                  placeholder="Thumbnail Comparison"
                />
              </Field>
              <Field label="Description">
                <Input
                  value={testDescription}
                  onChange={(_, data) => setTestDescription(data.value)}
                  placeholder="Testing different thumbnail styles"
                />
              </Field>
              <Field label="Number of Variants">
                <Input
                  type="number"
                  value={variantCount.toString()}
                  onChange={(_, data) => setVariantCount(parseInt(data.value) || 2)}
                  min={2}
                  max={5}
                />
              </Field>
              <div className={styles.actions}>
                <Button
                  appearance="primary"
                  onClick={handleCreateABTest}
                  disabled={loading || !testProfileId || !testName}
                >
                  {loading ? <Spinner size="tiny" /> : 'Create A/B Test'}
                </Button>
              </div>
            </div>
            {testCreated && (
              <div className={styles.resultsSection}>
                <Title3>A/B Test Created</Title3>
                <Text>
                  Your A/B test has been created successfully with {variantCount} variants.
                </Text>
              </div>
            )}
          </Card>
        )}
      </div>
    </div>
  );
};

export default PerformanceAnalyticsPage;
