import {
  Card,
  Button,
  Spinner,
  Text,
  makeStyles,
  tokens,
  Caption1,
  Body1,
} from '@fluentui/react-components';
import {
  DatabaseRegular,
  DeleteRegular,
  ArrowClockwiseRegular,
  DismissCircleRegular,
} from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import {
  getCacheStatistics,
  clearCache,
  evictExpiredEntries,
  forceRefresh,
} from '../../services/api/cacheApi';
import type { CacheStatistics } from '../../types/cache';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
    gap: tokens.spacingHorizontalS,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalL,
  },
  statCard: {
    padding: tokens.spacingVerticalM,
  },
  statValue: {
    fontSize: '24px',
    fontWeight: 600,
    marginBottom: tokens.spacingVerticalXS,
  },
  statLabel: {
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
    marginTop: tokens.spacingVerticalS,
  },
  successMessage: {
    color: tokens.colorPaletteGreenForeground1,
    marginTop: tokens.spacingVerticalS,
  },
});

const CacheManagementPanel: React.FC = () => {
  const styles = useStyles();
  const [stats, setStats] = useState<CacheStatistics | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const loadStats = async () => {
    setLoading(true);
    setMessage(null);
    try {
      const data = await getCacheStatistics();
      setStats(data);
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setMessage({ type: 'error', text: `Failed to load statistics: ${errorObj.message}` });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadStats();
    const interval = setInterval(loadStats, 30000);
    return () => clearInterval(interval);
  }, []);

  const handleClearCache = async () => {
    setActionLoading(true);
    setMessage(null);
    try {
      const response = await clearCache();
      setMessage({ type: 'success', text: response.message });
      await loadStats();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setMessage({ type: 'error', text: `Failed to clear cache: ${errorObj.message}` });
    } finally {
      setActionLoading(false);
    }
  };

  const handleEvictExpired = async () => {
    setActionLoading(true);
    setMessage(null);
    try {
      const response = await evictExpiredEntries();
      setMessage({ type: 'success', text: response.message });
      await loadStats();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setMessage({ type: 'error', text: `Failed to evict entries: ${errorObj.message}` });
    } finally {
      setActionLoading(false);
    }
  };

  const handleForceRefresh = async () => {
    setActionLoading(true);
    setMessage(null);
    try {
      const response = await forceRefresh();
      setMessage({ type: 'success', text: response.message });
      await loadStats();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setMessage({ type: 'error', text: `Failed to refresh cache: ${errorObj.message}` });
    } finally {
      setActionLoading(false);
    }
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  };

  const formatPercentage = (value: number): string => {
    return `${(value * 100).toFixed(1)}%`;
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <DatabaseRegular style={{ fontSize: '24px' }} />
        <Text size={500} weight="semibold">
          LLM Cache Management
        </Text>
      </div>

      {loading && !stats ? (
        <Spinner label="Loading cache statistics..." />
      ) : stats ? (
        <>
          <div className={styles.statsGrid}>
            <Card className={styles.statCard}>
              <div className={styles.statValue}>{stats.totalEntries}</div>
              <Caption1 className={styles.statLabel}>Total Entries</Caption1>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statValue}>{formatPercentage(stats.hitRate)}</div>
              <Caption1 className={styles.statLabel}>Hit Rate</Caption1>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statValue}>{stats.totalHits}</div>
              <Caption1 className={styles.statLabel}>Cache Hits</Caption1>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statValue}>{stats.totalMisses}</div>
              <Caption1 className={styles.statLabel}>Cache Misses</Caption1>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statValue}>{formatBytes(stats.totalSizeBytes)}</div>
              <Caption1 className={styles.statLabel}>Cache Size</Caption1>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statValue}>{stats.totalEvictions}</div>
              <Caption1 className={styles.statLabel}>Evictions</Caption1>
            </Card>

            {stats.memoryUsageMB !== undefined && (
              <Card className={styles.statCard}>
                <div className={styles.statValue}>{stats.memoryUsageMB.toFixed(1)} MB</div>
                <Caption1 className={styles.statLabel}>Process Memory</Caption1>
              </Card>
            )}

            {stats.gcMemoryMB !== undefined && (
              <Card className={styles.statCard}>
                <div className={styles.statValue}>{stats.gcMemoryMB.toFixed(1)} MB</div>
                <Caption1 className={styles.statLabel}>GC Memory</Caption1>
              </Card>
            )}
          </div>

          <div className={styles.actions}>
            <Button
              appearance="primary"
              icon={<ArrowClockwiseRegular />}
              onClick={loadStats}
              disabled={actionLoading}
            >
              Refresh Stats
            </Button>

            <Button
              appearance="secondary"
              icon={<DismissCircleRegular />}
              onClick={handleEvictExpired}
              disabled={actionLoading}
            >
              Evict Expired
            </Button>

            <Button
              appearance="secondary"
              icon={<ArrowClockwiseRegular />}
              onClick={handleForceRefresh}
              disabled={actionLoading}
            >
              Force Refresh
            </Button>

            <Button
              appearance="secondary"
              icon={<DeleteRegular />}
              onClick={handleClearCache}
              disabled={actionLoading}
            >
              Clear Cache
            </Button>
          </div>

          {message && (
            <Body1
              className={message.type === 'error' ? styles.errorMessage : styles.successMessage}
            >
              {message.text}
            </Body1>
          )}
        </>
      ) : (
        <Text>No statistics available</Text>
      )}
    </div>
  );
};

export default CacheManagementPanel;
