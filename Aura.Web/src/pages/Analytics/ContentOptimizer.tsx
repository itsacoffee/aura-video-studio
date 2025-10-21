import { useState } from 'react';
import {
  Button,
  Card,
  Text,
  Title,
  Input,
  Textarea,
  Select,
  Spinner,
  Badge,
  Checkbox,
  makeStyles,
  tokens,
  Tab,
  TabList,
} from '@fluentui/react-components';
import { analyticsService, type ImprovementRoadmap, type PlatformOptimization } from '../../services/analytics/PlatformService';

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
  platformCheckboxes: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  results: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  scoreComparison: {
    display: 'flex',
    justifyContent: 'space-around',
    padding: tokens.spacingVerticalL,
  },
  scoreItem: {
    textAlign: 'center',
  },
  actionItems: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  actionItem: {
    padding: tokens.spacingVerticalM,
    borderLeft: `4px solid ${tokens.colorBrandBackground}`,
    paddingLeft: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  actionHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  platformOptimization: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
});

export function ContentOptimizer() {
  const styles = useStyles();
  const [content, setContent] = useState('');
  const [contentType, setContentType] = useState('tutorial');
  const [videoDuration, setVideoDuration] = useState('00:10:00');
  const [selectedPlatforms, setSelectedPlatforms] = useState<string[]>(['youtube']);
  const [loading, setLoading] = useState(false);
  const [roadmap, setRoadmap] = useState<ImprovementRoadmap | null>(null);
  const [platformOptimizations, setPlatformOptimizations] = useState<Record<string, PlatformOptimization>>({});
  const [error, setError] = useState<string | null>(null);
  const [selectedTab, setSelectedTab] = useState<string>('roadmap');

  const platforms = [
    { value: 'youtube', label: 'YouTube' },
    { value: 'tiktok', label: 'TikTok' },
    { value: 'instagram', label: 'Instagram' },
    { value: 'youtube shorts', label: 'YouTube Shorts' },
  ];

  const handlePlatformToggle = (platform: string, checked: boolean) => {
    if (checked) {
      setSelectedPlatforms([...selectedPlatforms, platform]);
    } else {
      setSelectedPlatforms(selectedPlatforms.filter(p => p !== platform));
    }
  };

  const handleOptimize = async () => {
    if (!content.trim()) {
      setError('Please enter content to optimize');
      return;
    }

    if (selectedPlatforms.length === 0) {
      setError('Please select at least one platform');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Get improvement roadmap
      const roadmapResult = await analyticsService.getImprovementRoadmap(
        content,
        contentType,
        videoDuration,
        selectedPlatforms
      );
      setRoadmap(roadmapResult);

      // Get platform-specific optimizations
      const platformResults: Record<string, PlatformOptimization> = {};
      for (const platform of selectedPlatforms) {
        const optimization = await analyticsService.optimizePlatform(
          platform,
          content,
          videoDuration
        );
        platformResults[platform] = optimization;
      }
      setPlatformOptimizations(platformResults);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to optimize content');
    } finally {
      setLoading(false);
    }
  };

  const getImpactColor = (impact: string) => {
    switch (impact) {
      case 'High':
        return 'success';
      case 'Medium':
        return 'warning';
      default:
        return 'informative';
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'Easy':
        return 'success';
      case 'Medium':
        return 'warning';
      default:
        return 'danger';
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title>Content Optimization Engine</Title>
        <Text>Get actionable recommendations to improve your content for maximum engagement</Text>
      </div>

      <Card className={styles.form}>
        <Textarea
          label="Content Script"
          placeholder="Enter your video script or content..."
          value={content}
          onChange={(_, data) => setContent(data.value)}
          rows={8}
          resize="vertical"
        />

        <div className={styles.formRow}>
          <Select
            label="Content Type"
            value={contentType}
            onChange={(_, data) => setContentType(data.value)}
          >
            <option value="tutorial">Tutorial</option>
            <option value="entertainment">Entertainment</option>
            <option value="educational">Educational</option>
            <option value="short">Short Form</option>
          </Select>

          <Input
            label="Video Duration"
            type="text"
            value={videoDuration}
            onChange={(_, data) => setVideoDuration(data.value)}
            placeholder="HH:MM:SS"
          />
        </div>

        <div>
          <Text weight="semibold">Target Platforms:</Text>
          <div className={styles.platformCheckboxes}>
            {platforms.map((platform) => (
              <Checkbox
                key={platform.value}
                label={platform.label}
                checked={selectedPlatforms.includes(platform.value)}
                onChange={(_, data) => handlePlatformToggle(platform.value, data.checked === true)}
              />
            ))}
          </div>
        </div>

        <Button
          appearance="primary"
          onClick={handleOptimize}
          disabled={loading || !content.trim() || selectedPlatforms.length === 0}
        >
          {loading ? <Spinner size="tiny" /> : 'Optimize Content'}
        </Button>

        {error && (
          <Text style={{ color: tokens.colorPaletteRedForeground1 }}>{error}</Text>
        )}
      </Card>

      {roadmap && (
        <div className={styles.results}>
          <Card className={styles.scoreComparison}>
            <div className={styles.scoreItem}>
              <Text size={200}>Current Score</Text>
              <Title size={900}>{(roadmap.currentScore * 100).toFixed(0)}%</Title>
            </div>
            <div className={styles.scoreItem}>
              <Text size={200}>Potential Score</Text>
              <Title size={900} style={{ color: tokens.colorBrandForeground1 }}>
                {(roadmap.potentialScore * 100).toFixed(0)}%
              </Title>
            </div>
            <div className={styles.scoreItem}>
              <Text size={200}>Estimated Time</Text>
              <Title size={600}>{roadmap.estimatedTimeToImprove}</Title>
            </div>
          </Card>

          <TabList selectedValue={selectedTab} onTabSelect={(_, data) => setSelectedTab(data.value as string)}>
            <Tab value="roadmap">Improvement Roadmap</Tab>
            <Tab value="quickwins">Quick Wins</Tab>
            <Tab value="platforms">Platform Optimizations</Tab>
          </TabList>

          {selectedTab === 'roadmap' && (
            <Card>
              <Title size={400}>Prioritized Action Items</Title>
              <div className={styles.actionItems}>
                {roadmap.prioritizedActions.map((action, index) => (
                  <div key={index} className={styles.actionItem}>
                    <div className={styles.actionHeader}>
                      <Text weight="semibold">{action.title}</Text>
                      <div className={styles.badges}>
                        <Badge appearance="filled" color={getImpactColor(action.impact)}>
                          {action.impact} Impact
                        </Badge>
                        <Badge appearance="outline" color={getDifficultyColor(action.difficulty)}>
                          {action.difficulty}
                        </Badge>
                        <Badge appearance="outline">{action.category}</Badge>
                      </div>
                    </div>
                    <Text>{action.description}</Text>
                    <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
                      Estimated time: {action.estimatedTime}
                    </Text>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {selectedTab === 'quickwins' && (
            <Card>
              <Title size={400}>Quick Wins</Title>
              <Text>Easy improvements with high impact:</Text>
              <div className={styles.actionItems}>
                {roadmap.quickWins.map((win, index) => (
                  <div key={index} className={styles.actionItem}>
                    <Text weight="semibold">{win.title}</Text>
                    <Text>{win.description}</Text>
                  </div>
                ))}
              </div>
            </Card>
          )}

          {selectedTab === 'platforms' && (
            <Card>
              <Title size={400}>Platform-Specific Optimizations</Title>
              {Object.entries(platformOptimizations).map(([platform, optimization]) => (
                <div key={platform} className={styles.platformOptimization}>
                  <Title size={300}>{optimization.platform}</Title>
                  <Text>Optimal Duration: {optimization.optimalDuration}</Text>
                  <Text>Aspect Ratio: {optimization.recommendedAspectRatio}</Text>
                  <Text>Thumbnail Size: {optimization.optimalThumbnailSize}</Text>
                  <div>
                    <Text weight="semibold">Recommendations:</Text>
                    {optimization.recommendations.map((rec, index) => (
                      <Text key={index} size={200}>• {rec}</Text>
                    ))}
                  </div>
                  <div>
                    <Text weight="semibold">Suggested Hashtags:</Text>
                    <Text size={200}>{optimization.hashtagSuggestions.join(', ')}</Text>
                  </div>
                </div>
              ))}
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
