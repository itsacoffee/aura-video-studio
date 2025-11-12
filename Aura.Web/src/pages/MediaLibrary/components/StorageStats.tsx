import {
  makeStyles,
  shorthands,
  tokens,
  Card,
  Text,
  ProgressBar,
  Badge,
} from '@fluentui/react-components';
import React from 'react';
import type { StorageStats as StorageStatsType } from '../../../types/mediaLibrary';
import { formatFileSize } from '../../../utils/format';

const useStyles = makeStyles({
  card: {
    marginBottom: tokens.spacingVerticalM,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  stats: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginTop: tokens.spacingVerticalS,
  },
  stat: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
  },
});

interface StorageStatsProps {
  stats: StorageStatsType;
}

export const StorageStats: React.FC<StorageStatsProps> = ({ stats }) => {
  const styles = useStyles();

  const getUsageColor = (percentage: number) => {
    if (percentage >= 90) return 'danger';
    if (percentage >= 75) return 'warning';
    return 'success';
  };

  return (
    <Card className={styles.card}>
      <div className={styles.content}>
        <div className={styles.row}>
          <Text weight="semibold">Storage Usage</Text>
          <Badge appearance="tint" color={getUsageColor(stats.usagePercentage)}>
            {stats.usagePercentage.toFixed(1)}% used
          </Badge>
        </div>

        <ProgressBar
          value={stats.usagePercentage / 100}
          color={getUsageColor(stats.usagePercentage)}
        />

        <div className={styles.row}>
          <Text size={300}>
            {formatFileSize(stats.totalSizeBytes)} of{' '}
            {formatFileSize(stats.quotaBytes)} used
          </Text>
          <Text size={300}>
            {formatFileSize(stats.availableBytes)} available
          </Text>
        </div>

        <div className={styles.stats}>
          <div className={styles.stat}>
            <Text size={200} weight="semibold">
              Total Files
            </Text>
            <Text size={400}>{stats.totalFiles}</Text>
          </div>

          {Object.entries(stats.filesByType).map(([type, count]) => (
            <div key={type} className={styles.stat}>
              <Text size={200} weight="semibold">
                {type}
              </Text>
              <Text size={400}>
                {count} ({formatFileSize(stats.sizeByType[type] || 0)})
              </Text>
            </div>
          ))}
        </div>
      </div>
    </Card>
  );
};
