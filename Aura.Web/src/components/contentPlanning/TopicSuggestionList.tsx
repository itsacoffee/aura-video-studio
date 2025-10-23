import React, { useState } from 'react';
import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  tokens,
  Button,
  Input,
  Dropdown,
  Option,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import { LightbulbRegular, SparkleRegular } from '@fluentui/react-icons';
import { contentPlanningService, TopicSuggestion } from '../../services/contentPlanningService';

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
  topicsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  topicCard: {
    transition: 'all 0.2s ease',
    cursor: 'pointer',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  topicHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: tokens.spacingVerticalS,
  },
  topicTitle: {
    flex: 1,
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  scores: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  description: {
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalM,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  metadataItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  keywords: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground2,
  },
});

export const TopicSuggestionList: React.FC = () => {
  const styles = useStyles();
  const [topics, setTopics] = useState<TopicSuggestion[]>([]);
  const [loading, setLoading] = useState(false);
  const [category, setCategory] = useState<string>('');
  const [audience, setAudience] = useState<string>('');
  const [platform, setPlatform] = useState<string>('YouTube');
  const [interests, setInterests] = useState<string>('');
  const [count, setCount] = useState<number>(10);

  const platforms = ['YouTube', 'TikTok', 'Instagram', 'Facebook', 'Twitter'];

  const handleGenerate = async () => {
    setLoading(true);
    try {
      const interestList = interests
        .split(',')
        .map((i) => i.trim())
        .filter((i) => i.length > 0);

      const response = await contentPlanningService.generateTopics({
        category: category || undefined,
        targetAudience: audience || undefined,
        interests: interestList,
        preferredPlatforms: platform ? [platform] : [],
        count,
      });
      setTopics(response.suggestions);
    } catch (error) {
      console.error('Failed to generate topics:', error);
    } finally {
      setLoading(false);
    }
  };

  const getScoreColor = (score: number) => {
    if (score >= 80) return 'success';
    if (score >= 60) return 'warning';
    return 'informative';
  };

  return (
    <div className={styles.container}>
      <Card>
        <CardHeader
          header={<Text weight="semibold">Generate Topic Ideas</Text>}
          description="Get AI-powered topic suggestions for your content"
        />
        <div style={{ padding: tokens.spacingVerticalM }}>
          <div className={styles.controls}>
            <Input
              placeholder="Category (e.g., Technology, Gaming)"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              className={styles.input}
            />
            <Input
              placeholder="Target Audience (optional)"
              value={audience}
              onChange={(e) => setAudience(e.target.value)}
              className={styles.input}
            />
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
            <Input
              placeholder="Interests (comma-separated)"
              value={interests}
              onChange={(e) => setInterests(e.target.value)}
              className={styles.input}
            />
            <Button
              appearance="primary"
              icon={<SparkleRegular />}
              onClick={handleGenerate}
              disabled={loading}
            >
              Generate
            </Button>
          </div>
        </div>
      </Card>

      {loading && (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Spinner label="Generating topic ideas..." />
        </div>
      )}

      {!loading && topics.length === 0 && (
        <div className={styles.emptyState}>
          <LightbulbRegular style={{ fontSize: '48px', marginBottom: tokens.spacingVerticalM }} />
          <Text size={400}>No topics generated yet. Fill in the form and click Generate.</Text>
        </div>
      )}

      {!loading && topics.length > 0 && (
        <div className={styles.topicsList}>
          {topics.map((topic) => (
            <Card key={topic.id} className={styles.topicCard}>
              <div style={{ padding: tokens.spacingVerticalM }}>
                <div className={styles.topicHeader}>
                  <div className={styles.topicTitle}>{topic.topic}</div>
                  <div className={styles.scores}>
                    <Badge appearance="tinted" color={getScoreColor(topic.relevanceScore)}>
                      Relevance: {topic.relevanceScore.toFixed(0)}
                    </Badge>
                    <Badge appearance="tinted" color={getScoreColor(topic.trendScore)}>
                      Trend: {topic.trendScore.toFixed(0)}
                    </Badge>
                  </div>
                </div>

                <Text className={styles.description}>{topic.description}</Text>

                <div className={styles.metadata}>
                  <div className={styles.metadataItem}>
                    <Text weight="semibold">Category:</Text>
                    <Text>{topic.category}</Text>
                  </div>
                  <div className={styles.metadataItem}>
                    <Text weight="semibold">Predicted Engagement:</Text>
                    <Text>{topic.predictedEngagement.toFixed(1)}%</Text>
                  </div>
                </div>

                {topic.keywords.length > 0 && (
                  <div className={styles.keywords}>
                    <Text size={200} style={{ marginRight: tokens.spacingHorizontalXS }}>
                      Keywords:
                    </Text>
                    {topic.keywords.map((keyword, idx) => (
                      <Badge key={idx} size="small" appearance="outline">
                        {keyword}
                      </Badge>
                    ))}
                  </div>
                )}

                {topic.recommendedPlatforms.length > 0 && (
                  <div className={styles.keywords}>
                    <Text size={200} style={{ marginRight: tokens.spacingHorizontalXS }}>
                      Best for:
                    </Text>
                    {topic.recommendedPlatforms.map((plat, idx) => (
                      <Badge key={idx} size="small" appearance="filled" color="brand">
                        {plat}
                      </Badge>
                    ))}
                  </div>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};
