import {
  makeStyles,
  shorthands,
  tokens,
  Card,
  CardHeader,
  Text,
  Title3,
  Button,
  ProgressBar,
  Badge,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Spinner,
} from '@fluentui/react-components';
import {
  Database24Regular,
  Delete24Regular,
  Archive24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import React, { useState } from 'react';
import { mediaLibraryApi } from '../../../api/mediaLibraryApi';
import type { StorageStats, MediaType } from '../../../types/mediaLibrary';
import { formatFileSize } from '../../../utils/format';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalL),
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    ...shorthands.gap(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  statCard: {
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  statValue: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
    marginTop: tokens.spacingVerticalS,
  },
  statLabel: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
  },
  progressSection: {
    marginTop: tokens.spacingVerticalL,
  },
  progressLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  typeBreakdown: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  typeRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
    backgroundColor: tokens.colorNeutralBackground2,
  },
  typeInfo: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalM),
  },
  actionButtons: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  suggestionCard: {
    ...shorthands.padding(tokens.spacingVerticalL),
    backgroundColor: tokens.colorNeutralBackground2,
  },
  suggestionList: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
    marginTop: tokens.spacingVerticalM,
  },
  suggestionItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalS),
    ...shorthands.borderRadius(tokens.borderRadiusSmall),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke1),
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
});

interface StorageManagementPanelProps {
  stats: StorageStats;
}

export const StorageManagementPanel: React.FC<StorageManagementPanelProps> = ({ stats }) => {
  const styles = useStyles();
  const queryClient = useQueryClient();
  const [showCleanupDialog, setShowCleanupDialog] = useState(false);
  const [cleanupInProgress, setCleanupInProgress] = useState(false);

  const usagePercentage = (stats.totalSizeBytes / stats.quotaBytes) * 100;
  const isNearCapacity = usagePercentage > 80;
  const isAtCapacity = usagePercentage > 95;

  const getStorageColor = () => {
    if (isAtCapacity) return 'danger';
    if (isNearCapacity) return 'warning';
    return 'success';
  };

  const getTypeIcon = (type: MediaType) => {
    switch (type) {
      case 'Video':
        return '🎥';
      case 'Audio':
        return '🎵';
      case 'Image':
        return '🖼️';
      case 'Document':
        return '📄';
      default:
        return '📁';
    }
  };

  const handleCleanup = async () => {
    setCleanupInProgress(true);
    try {
      // Implement cleanup logic
      // This could involve calling an API endpoint to clean up old/unused media
      await new Promise((resolve) => setTimeout(resolve, 2000)); // Simulate cleanup
      queryClient.invalidateQueries({ queryKey: ['media-stats'] });
      setShowCleanupDialog(false);
    } finally {
      setCleanupInProgress(false);
    }
  };

  const suggestions = [];
  if (isAtCapacity) {
    suggestions.push({
      id: 'capacity',
      message: 'Storage is at capacity. Delete unused media to free up space.',
      action: 'Review Files',
      severity: 'error' as const,
    });
  } else if (isNearCapacity) {
    suggestions.push({
      id: 'near-capacity',
      message: 'Storage is nearing capacity. Consider archiving old projects.',
      action: 'Archive',
      severity: 'warning' as const,
    });
  }

  if (stats.totalFiles > 1000) {
    suggestions.push({
      id: 'many-files',
      message: `You have ${stats.totalFiles} files. Organize them into collections.`,
      action: 'Organize',
      severity: 'info' as const,
    });
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div>
          <Title3>Storage Management</Title3>
          <Text className={styles.statLabel}>Monitor and optimize your storage usage</Text>
        </div>
        <div className={styles.actionButtons}>
          <Button
            appearance="secondary"
            icon={<Archive24Regular />}
            onClick={() => setShowCleanupDialog(true)}
          >
            Cleanup
          </Button>
        </div>
      </div>

      <div className={styles.statsGrid}>
        <Card className={styles.statCard}>
          <CardHeader
            image={<Database24Regular />}
            header={<Text className={styles.statLabel}>Total Used</Text>}
          />
          <Text className={styles.statValue}>{formatFileSize(stats.totalSizeBytes)}</Text>
          <div className={styles.progressSection}>
            <div className={styles.progressLabel}>
              <Text size={200}>
                {formatFileSize(stats.totalSizeBytes)} of {formatFileSize(stats.quotaBytes)}
              </Text>
              <Text size={200}>{usagePercentage.toFixed(1)}%</Text>
            </div>
            <ProgressBar value={usagePercentage} max={100} color={getStorageColor()} />
          </div>
        </Card>

        <Card className={styles.statCard}>
          <CardHeader
            image={
              isAtCapacity ? (
                <Warning24Regular className={styles.warningIcon} />
              ) : (
                <CheckmarkCircle24Regular className={styles.successIcon} />
              )
            }
            header={<Text className={styles.statLabel}>Status</Text>}
          />
          <Text className={styles.statValue}>
            {isAtCapacity ? 'At Capacity' : isNearCapacity ? 'Near Capacity' : 'Healthy'}
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
            {formatFileSize(stats.availableBytes)} available
          </Text>
        </Card>

        <Card className={styles.statCard}>
          <CardHeader header={<Text className={styles.statLabel}>Total Files</Text>} />
          <Text className={styles.statValue}>{stats.totalFiles.toLocaleString()}</Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalS }}>
            Across {Object.keys(stats.filesByType).length} media types
          </Text>
        </Card>
      </div>

      <Card className={styles.statCard}>
        <CardHeader header={<Title3>Storage by Type</Title3>} />
        <div className={styles.typeBreakdown}>
          {Object.entries(stats.sizeByType)
            .sort(([, sizeA], [, sizeB]) => sizeB - sizeA)
            .map(([type, size]) => {
              const count = stats.filesByType[type as MediaType] || 0;
              const percentage = (size / stats.totalSizeBytes) * 100;
              return (
                <div key={type} className={styles.typeRow}>
                  <div className={styles.typeInfo}>
                    <Text style={{ fontSize: '24px' }}>{getTypeIcon(type as MediaType)}</Text>
                    <div>
                      <Text weight="semibold">{type}</Text>
                      <Text size={200} style={{ display: 'block' }}>
                        {count} files • {percentage.toFixed(1)}%
                      </Text>
                    </div>
                  </div>
                  <Text weight="semibold">{formatFileSize(size)}</Text>
                </div>
              );
            })}
        </div>
      </Card>

      {suggestions.length > 0 && (
        <Card className={styles.suggestionCard}>
          <CardHeader header={<Title3>Recommendations</Title3>} />
          <div className={styles.suggestionList}>
            {suggestions.map((suggestion) => (
              <div key={suggestion.id} className={styles.suggestionItem}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}
                >
                  {suggestion.severity === 'error' && (
                    <Warning24Regular className={styles.warningIcon} />
                  )}
                  <Text>{suggestion.message}</Text>
                </div>
                <Button appearance="primary" size="small">
                  {suggestion.action}
                </Button>
              </div>
            ))}
          </div>
        </Card>
      )}

      {showCleanupDialog && (
        <Dialog open onOpenChange={() => setShowCleanupDialog(false)}>
          <DialogSurface>
            <DialogBody>
              <DialogTitle>Storage Cleanup</DialogTitle>
              <DialogContent>
                <Text>
                  This will remove temporary files, optimize storage, and clean up unused media.
                  This action cannot be undone.
                </Text>
                {cleanupInProgress && (
                  <div style={{ marginTop: tokens.spacingVerticalL, textAlign: 'center' }}>
                    <Spinner label="Cleaning up..." />
                  </div>
                )}
              </DialogContent>
              <DialogActions>
                <Button
                  appearance="secondary"
                  onClick={() => setShowCleanupDialog(false)}
                  disabled={cleanupInProgress}
                >
                  Cancel
                </Button>
                <Button
                  appearance="primary"
                  icon={<Delete24Regular />}
                  onClick={handleCleanup}
                  disabled={cleanupInProgress}
                >
                  {cleanupInProgress ? 'Cleaning...' : 'Start Cleanup'}
                </Button>
              </DialogActions>
            </DialogBody>
          </DialogSurface>
        </Dialog>
      )}
    </div>
  );
};
