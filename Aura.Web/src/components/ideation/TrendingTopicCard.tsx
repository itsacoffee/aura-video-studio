import React from 'react';
import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  tokens,
  Badge,
  ProgressBar,
} from '@fluentui/react-components';
import { DataTrendingRegular, CalendarRegular } from '@fluentui/react-icons';
import type { TrendingTopic } from '../../services/ideationService';

const useStyles = makeStyles({
  card: {
    width: '100%',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow16,
    },
  },
  content: {
    padding: tokens.spacingVerticalM,
  },
  trendScore: {
    marginBottom: tokens.spacingVerticalM,
  },
  scoreLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  metadata: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  metadataItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  metadataLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  relatedTopics: {
    marginTop: tokens.spacingVerticalM,
  },
  tags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  lifecycleBadge: {
    marginLeft: 'auto',
  },
  aiInsights: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1Hover,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `3px solid ${tokens.colorBrandForeground1}`,
  },
  insightSection: {
    marginBottom: tokens.spacingVerticalS,
  },
  insightLabel: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
    marginBottom: tokens.spacingVerticalXXS,
  },
  insightText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    lineHeight: '1.5',
  },
  contentAngles: {
    paddingLeft: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXXS,
  },
  angleItem: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalXXS,
  },
  viralityBadge: {
    marginTop: tokens.spacingVerticalS,
  },
});

interface TrendingTopicCardProps {
  topic: TrendingTopic;
  onSelect?: (topic: TrendingTopic) => void;
}

export const TrendingTopicCard: React.FC<TrendingTopicCardProps> = ({ topic, onSelect }) => {
  const styles = useStyles();

  const handleClick = () => {
    if (onSelect) {
      onSelect(topic);
    }
  };

  const getLifecycleColor = (lifecycle?: string) => {
    switch (lifecycle?.toLowerCase()) {
      case 'rising':
        return 'success';
      case 'peak':
        return 'important';
      case 'declining':
        return 'warning';
      default:
        return 'informative';
    }
  };

  const getCompetitionColor = (competition?: string) => {
    switch (competition?.toLowerCase()) {
      case 'low':
        return '#107c10';
      case 'medium':
        return '#faa700';
      case 'high':
        return '#d13438';
      default:
        return tokens.colorNeutralForeground3;
    }
  };

  return (
    <Card className={styles.card} onClick={handleClick}>
      <CardHeader
        header={
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <DataTrendingRegular />
            <Text weight="semibold" size={400}>
              {topic.topic}
            </Text>
          </div>
        }
        action={
          topic.lifecycle && (
            <Badge
              className={styles.lifecycleBadge}
              appearance="filled"
              color={getLifecycleColor(topic.lifecycle)}
            >
              {topic.lifecycle}
            </Badge>
          )
        }
      />

      <div className={styles.content}>
        <div className={styles.trendScore}>
          <div className={styles.scoreLabel}>
            <Text size={300}>Trend Score</Text>
            <Text weight="semibold">{topic.trendScore}/100</Text>
          </div>
          <ProgressBar value={topic.trendScore / 100} />
        </div>

        <div className={styles.metadata}>
          {topic.searchVolume && (
            <div className={styles.metadataItem}>
              <Text className={styles.metadataLabel}>Search Volume</Text>
              <Text weight="semibold" size={300}>
                {topic.searchVolume}
              </Text>
            </div>
          )}

          {topic.competition && (
            <div className={styles.metadataItem}>
              <Text className={styles.metadataLabel}>Competition</Text>
              <Text
                weight="semibold"
                size={300}
                style={{ color: getCompetitionColor(topic.competition) }}
              >
                {topic.competition}
              </Text>
            </div>
          )}

          {topic.seasonality && (
            <div className={styles.metadataItem}>
              <Text className={styles.metadataLabel}>
                <CalendarRegular style={{ marginRight: '4px' }} />
                Seasonality
              </Text>
              <Text weight="semibold" size={300}>
                {topic.seasonality}
              </Text>
            </div>
          )}
        </div>

        {topic.relatedTopics && topic.relatedTopics.length > 0 && (
          <div className={styles.relatedTopics}>
            <Text className={styles.metadataLabel}>Related Topics:</Text>
            <div className={styles.tags}>
              {topic.relatedTopics.map((related, index) => (
                <Badge key={index} appearance="outline" size="small">
                  {related}
                </Badge>
              ))}
            </div>
          </div>
        )}

        {topic.hashtags && topic.hashtags.length > 0 && (
          <div className={styles.relatedTopics}>
            <Text className={styles.metadataLabel}>Suggested Hashtags:</Text>
            <div className={styles.tags}>
              {topic.hashtags.map((tag, index) => (
                <Badge key={index} appearance="tint" size="small" color="brand">
                  {tag}
                </Badge>
              ))}
            </div>
          </div>
        )}

        {topic.aiInsights && (
          <div className={styles.aiInsights}>
            <div className={styles.insightSection}>
              <div className={styles.insightLabel}>🔥 Why It's Trending</div>
              <Text className={styles.insightText}>{topic.aiInsights.whyTrending}</Text>
            </div>

            <div className={styles.insightSection}>
              <div className={styles.insightLabel}>👥 Audience Engagement</div>
              <Text className={styles.insightText}>{topic.aiInsights.audienceEngagement}</Text>
            </div>

            {topic.aiInsights.contentAngles && topic.aiInsights.contentAngles.length > 0 && (
              <div className={styles.insightSection}>
                <div className={styles.insightLabel}>💡 Content Angle Ideas</div>
                <div className={styles.contentAngles}>
                  {topic.aiInsights.contentAngles.map((angle, index) => (
                    <div key={index} className={styles.angleItem}>
                      • {angle}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className={styles.insightSection}>
              <div className={styles.insightLabel}>🎯 Target Demographics</div>
              <Text className={styles.insightText}>{topic.aiInsights.demographicAppeal}</Text>
            </div>

            {topic.aiInsights.viralityScore > 0 && (
              <div className={styles.viralityBadge}>
                <Badge
                  appearance="filled"
                  color={topic.aiInsights.viralityScore >= 75 ? 'success' : topic.aiInsights.viralityScore >= 50 ? 'warning' : 'informative'}
                >
                  Virality Potential: {Math.round(topic.aiInsights.viralityScore)}/100
                </Badge>
              </div>
            )}
          </div>
        )}
      </div>
    </Card>
  );
};
