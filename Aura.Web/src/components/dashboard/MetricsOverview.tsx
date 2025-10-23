import React from 'react';
import {
  Card,
  Title3,
  Body1,
  Body2,
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
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  metricCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  metricValue: {
    fontSize: '32px',
    fontWeight: 'bold',
    color: tokens.colorBrandForeground1,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground2,
  },
  breakdownSection: {
    marginTop: tokens.spacingVerticalXL,
  },
  breakdownGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  breakdownCard: {
    padding: tokens.spacingVerticalM,
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
    backgroundColor: tokens.colorBrandBackground,
    transition: 'width 0.3s ease',
  },
  statsRow: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalS,
  },
});

export const MetricsOverview: React.FC = () => {
  const styles = useStyles();
  const { metrics, breakdown } = useQualityDashboardStore();

  if (!metrics) {
    return <Body1>No metrics available</Body1>;
  }

  const formatProcessingTime = (time: string) => {
    try {
      const match = time.match(/(\d+):(\d+):(\d+)/);
      if (match) {
        const hours = parseInt(match[1]);
        const minutes = parseInt(match[2]);
        return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
      }
      return time;
    } catch {
      return time;
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.metricsGrid}>
        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Total Videos Processed</Body2>
          <div className={styles.metricValue}>{metrics.totalVideosProcessed.toLocaleString()}</div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Average Quality Score</Body2>
          <div className={styles.metricValue}>{metrics.averageQualityScore.toFixed(1)}%</div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Success Rate</Body2>
          <div className={styles.metricValue}>{metrics.successRate.toFixed(1)}%</div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Avg Processing Time</Body2>
          <div className={styles.metricValue}>
            {formatProcessingTime(metrics.averageProcessingTime)}
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Errors (24h)</Body2>
          <div className={styles.metricValue}>{metrics.totalErrorsLast24h}</div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Jobs in Progress</Body2>
          <div className={styles.metricValue}>
            {metrics.currentProcessingJobs} / {metrics.queuedJobs} queued
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Compliance Rate</Body2>
          <div className={styles.metricValue}>{metrics.complianceRate.toFixed(1)}%</div>
        </Card>

        <Card className={styles.metricCard}>
          <Body2 className={styles.metricLabel}>Quality Range</Body2>
          <div className={styles.metricValue}>
            {metrics.lowestQualityScore.toFixed(1)} - {metrics.peakQualityScore.toFixed(1)}
          </div>
        </Card>
      </div>

      {breakdown && (
        <div className={styles.breakdownSection}>
          <Title3>Quality Breakdown by Category</Title3>
          <div className={styles.breakdownGrid}>
            <Card className={styles.breakdownCard}>
              <Body1>Resolution</Body1>
              <Body2 className={styles.metricLabel}>
                {breakdown.resolution.averageScore.toFixed(1)}% average
              </Body2>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${breakdown.resolution.averageScore}%` }}
                />
              </div>
              <div className={styles.statsRow}>
                <Body2>Passed: {breakdown.resolution.passedChecks}</Body2>
                <Body2>Failed: {breakdown.resolution.failedChecks}</Body2>
              </div>
            </Card>

            <Card className={styles.breakdownCard}>
              <Body1>Audio Quality</Body1>
              <Body2 className={styles.metricLabel}>
                {breakdown.audio.averageScore.toFixed(1)}% average
              </Body2>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${breakdown.audio.averageScore}%` }}
                />
              </div>
              <div className={styles.statsRow}>
                <Body2>Passed: {breakdown.audio.passedChecks}</Body2>
                <Body2>Failed: {breakdown.audio.failedChecks}</Body2>
              </div>
            </Card>

            <Card className={styles.breakdownCard}>
              <Body1>Frame Rate</Body1>
              <Body2 className={styles.metricLabel}>
                {breakdown.frameRate.averageScore.toFixed(1)}% average
              </Body2>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${breakdown.frameRate.averageScore}%` }}
                />
              </div>
              <div className={styles.statsRow}>
                <Body2>Passed: {breakdown.frameRate.passedChecks}</Body2>
                <Body2>Failed: {breakdown.frameRate.failedChecks}</Body2>
              </div>
            </Card>

            <Card className={styles.breakdownCard}>
              <Body1>Consistency</Body1>
              <Body2 className={styles.metricLabel}>
                {breakdown.consistency.averageScore.toFixed(1)}% average
              </Body2>
              <div className={styles.progressBar}>
                <div
                  className={styles.progressFill}
                  style={{ width: `${breakdown.consistency.averageScore}%` }}
                />
              </div>
              <div className={styles.statsRow}>
                <Body2>Passed: {breakdown.consistency.passedChecks}</Body2>
                <Body2>Failed: {breakdown.consistency.failedChecks}</Body2>
              </div>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
};
