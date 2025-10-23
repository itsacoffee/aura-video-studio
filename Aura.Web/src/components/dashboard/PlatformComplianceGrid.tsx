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
import { useQualityDashboardStore } from '../../state/qualityDashboard';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  platformCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  complianceRate: {
    fontSize: '36px',
    fontWeight: 'bold',
    color: tokens.colorBrandForeground1,
  },
  stats: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  statRow: {
    display: 'flex',
    justifyContent: 'space-between',
  },
  progressBar: {
    width: '100%',
    height: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '4px',
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalS,
  },
  progressFill: {
    height: '100%',
    transition: 'width 0.3s ease',
  },
  issues: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  issueTitle: {
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalXS,
  },
});

export const PlatformComplianceGrid: React.FC = () => {
  const styles = useStyles();
  const { platformCompliance, fetchPlatformCompliance } = useQualityDashboardStore();

  useEffect(() => {
    if (!platformCompliance.length) {
      fetchPlatformCompliance();
    }
  }, [platformCompliance, fetchPlatformCompliance]);

  if (!platformCompliance.length) {
    return <Body1>No platform compliance data available</Body1>;
  }

  const getComplianceColor = (rate: number) => {
    if (rate >= 98) return tokens.colorPaletteGreenBackground3;
    if (rate >= 95) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteRedBackground3;
  };

  const getComplianceBadge = (rate: number) => {
    if (rate >= 98) return { appearance: 'tint' as const, text: 'Excellent' };
    if (rate >= 95) return { appearance: 'filled' as const, text: 'Good' };
    return { appearance: 'outline' as const, text: 'Needs Improvement' };
  };

  return (
    <div className={styles.container}>
      <Title3>Platform Compliance Status</Title3>
      <div className={styles.grid}>
        {platformCompliance.map((platform) => {
          const badge = getComplianceBadge(platform.complianceRate);
          return (
            <Card key={platform.platform} className={styles.platformCard}>
              <div className={styles.header}>
                <Title3>{platform.platform}</Title3>
                <Badge appearance={badge.appearance}>{badge.text}</Badge>
              </div>

              <div className={styles.complianceRate}>
                {platform.complianceRate.toFixed(1)}%
              </div>

              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{
                    width: `${platform.complianceRate}%`,
                    backgroundColor: getComplianceColor(platform.complianceRate),
                  }}
                />
              </div>

              <div className={styles.stats}>
                <div className={styles.statRow}>
                  <Body2>Total Videos</Body2>
                  <Body2>{platform.totalVideos}</Body2>
                </div>
                <div className={styles.statRow}>
                  <Body2>Compliant</Body2>
                  <Body2>{platform.compliantVideos}</Body2>
                </div>
                <div className={styles.statRow}>
                  <Body2>Non-Compliant</Body2>
                  <Body2>{platform.totalVideos - platform.compliantVideos}</Body2>
                </div>
              </div>

              {platform.commonIssues.length > 0 && (
                <>
                  <Body2 className={styles.issueTitle}>Common Issues:</Body2>
                  <div className={styles.issues}>
                    {platform.commonIssues.map((issue, index) => (
                      <Badge key={index} appearance="outline">
                        {issue}
                      </Badge>
                    ))}
                  </div>
                </>
              )}
            </Card>
          );
        })}
      </div>
    </div>
  );
};
