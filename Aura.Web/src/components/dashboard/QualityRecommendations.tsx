import React, { useEffect } from 'react';
import {
  Card,
  Title3,
  Body1,
  Body2,
  Badge,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  Warning24Regular,
  Info24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';
import { useQualityDashboardStore } from '../../state/qualityDashboard';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  recommendationCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },
  titleRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flex: 1,
  },
  icon: {
    fontSize: '24px',
  },
  highPriority: {
    color: tokens.colorPaletteRedForeground1,
  },
  mediumPriority: {
    color: tokens.colorPaletteYellowForeground1,
  },
  lowPriority: {
    color: tokens.colorPaletteGreenForeground1,
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  impact: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  impactScore: {
    fontSize: '20px',
    fontWeight: 'bold',
  },
  actionItems: {
    marginTop: tokens.spacingVerticalM,
  },
  actionList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actionItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  actionIcon: {
    marginTop: '2px',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground2,
  },
});

export const QualityRecommendations: React.FC = () => {
  const styles = useStyles();
  const { recommendations, fetchRecommendations } = useQualityDashboardStore();

  useEffect(() => {
    if (!recommendations.length) {
      fetchRecommendations();
    }
  }, [recommendations, fetchRecommendations]);

  if (!recommendations.length) {
    return (
      <div className={styles.emptyState}>
        <CheckmarkCircle24Regular />
        <Body1>No recommendations at this time. Your quality metrics look great!</Body1>
      </div>
    );
  }

  const getPriorityIcon = (priority: string) => {
    switch (priority) {
      case 'high':
        return <Warning24Regular className={`${styles.icon} ${styles.highPriority}`} />;
      case 'medium':
        return <Info24Regular className={`${styles.icon} ${styles.mediumPriority}`} />;
      default:
        return <CheckmarkCircle24Regular className={`${styles.icon} ${styles.lowPriority}`} />;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case 'high':
        return { appearance: 'filled' as const, text: 'High Priority', color: 'red' };
      case 'medium':
        return { appearance: 'tint' as const, text: 'Medium Priority', color: 'orange' };
      default:
        return { appearance: 'tint' as const, text: 'Low Priority', color: 'green' };
    }
  };

  return (
    <div className={styles.container}>
      <Title3>{recommendations.length} Quality Recommendations</Title3>
      {recommendations.map((rec) => {
        const priorityBadge = getPriorityBadge(rec.priority);
        return (
          <Card key={rec.id} className={styles.recommendationCard}>
            <div className={styles.header}>
              <div className={styles.titleRow}>
                {getPriorityIcon(rec.priority)}
                <Title3>{rec.title}</Title3>
              </div>
              <div className={styles.badges}>
                <Badge appearance={priorityBadge.appearance}>{priorityBadge.text}</Badge>
                <Badge appearance="outline">{rec.category}</Badge>
              </div>
            </div>

            <Body1>{rec.description}</Body1>

            <div className={styles.impact}>
              <Body2>Impact Score:</Body2>
              <span className={styles.impactScore}>{rec.impactScore.toFixed(1)}</span>
              <Body2>• Estimated Improvement: {rec.estimatedImprovement}</Body2>
            </div>

            {rec.actionItems.length > 0 && (
              <div className={styles.actionItems}>
                <Body2>Action Items:</Body2>
                <ul className={styles.actionList}>
                  {rec.actionItems.map((item, index) => (
                    <li key={index} className={styles.actionItem}>
                      <CheckmarkCircle24Regular className={styles.actionIcon} />
                      <Body2>{item}</Body2>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </Card>
        );
      })}
    </div>
  );
};
