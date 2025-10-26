import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { DataTrendingRegular, SearchRegular, ArrowClockwiseRegular } from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TrendingTopicCard } from '../../components/ideation/TrendingTopicCard';
import { ideationService, type TrendingTopic } from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  icon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    maxWidth: '800px',
  },
  filterSection: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalXL,
    alignItems: 'flex-end',
  },
  filterInput: {
    flex: 1,
    maxWidth: '400px',
  },
  topicsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
  },
  errorContainer: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorPaletteRedForeground1,
  },
  lastUpdated: {
    marginTop: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
    textAlign: 'center',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  nicheDropdown: {
    minWidth: '250px',
  },
});

export const TrendingTopicsExplorer: React.FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const [topics, setTopics] = useState<TrendingTopic[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [niche, setNiche] = useState('');
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const nicheCategories = [
    'General',
    'Gaming',
    'Technology',
    'Health',
    'Business',
    'Education',
    'Lifestyle',
    'Science',
    'News',
    'Sports',
    'Entertainment',
  ];

  useEffect(() => {
    loadTrendingTopics();
  }, []);

  const loadTrendingTopics = async (nicheFilter?: string) => {
    setLoading(true);
    setError(null);

    try {
      const response = await ideationService.getTrending(nicheFilter, 10);
      setTopics(response.topics);
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trending topics');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    loadTrendingTopics(niche || undefined);
  };

  const handleRefresh = () => {
    loadTrendingTopics(niche || undefined);
  };

  const handleSelectTopic = (topic: TrendingTopic) => {
    // Navigate to ideation dashboard with the topic pre-filled for brainstorming
    navigate('/ideation', { state: { initialTopic: topic.topic } });
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <DataTrendingRegular className={styles.icon} />
          <Text size={800} weight="bold">
            Trending Topics
          </Text>
        </div>
        <Text className={styles.subtitle} size={400}>
          Discover popular and rising topics in your niche. Find content opportunities with high
          search volume and manageable competition.
        </Text>
      </div>

      <div className={styles.filterSection}>
        <div className={styles.filterInput}>
          <Text size={300} style={{ marginBottom: '4px' }}>
            Select Niche Category
          </Text>
          <Dropdown
            className={styles.nicheDropdown}
            placeholder="Select a category"
            value={niche || 'General'}
            onOptionSelect={(_, data) => setNiche(data.optionValue || '')}
          >
            {nicheCategories.map((category) => (
              <Option key={category} value={category}>
                {category}
              </Option>
            ))}
          </Dropdown>
        </div>
        <Button appearance="primary" icon={<SearchRegular />} onClick={handleSearch}>
          Analyze Trends
        </Button>
        <Button icon={<ArrowClockwiseRegular />} onClick={handleRefresh} disabled={loading}>
          Refresh
        </Button>
      </div>

      {lastUpdated && (
        <div className={styles.lastUpdated}>
          <Text size={200}>
            Last updated: {lastUpdated.toLocaleTimeString()} • Data cached for 30 minutes
          </Text>
        </div>
      )}

      {error && (
        <div className={styles.errorContainer}>
          <Text weight="semibold">Error:</Text>
          <Text>{error}</Text>
        </div>
      )}

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="extra-large" />
          <Text size={500} weight="semibold">
            Analyzing trending topics...
          </Text>
        </div>
      ) : (
        <div className={styles.topicsGrid}>
          {topics.map((topic) => (
            <TrendingTopicCard key={topic.topicId} topic={topic} onSelect={handleSelectTopic} />
          ))}
        </div>
      )}
    </div>
  );
};
