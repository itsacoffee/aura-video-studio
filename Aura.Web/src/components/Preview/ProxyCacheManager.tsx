import { FC, useState, useEffect } from 'react';
import {
  Button,
  Card,
  CardHeader,
  Text,
  ProgressBar,
  makeStyles,
  tokens,
  Tooltip,
  Input,
  Label,
  Spinner,
} from '@fluentui/react-components';
import {
  Delete24Regular,
  Settings24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { proxyMediaService, type ProxyCacheStats } from '../../services/proxyMediaService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  statCard: {
    padding: tokens.spacingVerticalM,
  },
  statValue: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  statLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  progressContainer: {
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  settingsSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  inputGroup: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: tokens.spacingHorizontalS,
  },
  warningText: {
    color: tokens.colorPaletteRedForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
});

interface ProxyCacheManagerProps {
  onStatsChanged?: (stats: ProxyCacheStats) => void;
}

export const ProxyCacheManager: FC<ProxyCacheManagerProps> = ({ onStatsChanged }) => {
  const styles = useStyles();
  const [stats, setStats] = useState<ProxyCacheStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [maxSizeGB, setMaxSizeGB] = useState('10');

  useEffect(() => {
    loadStats();
    loadMaxSize();
  }, []);

  const loadStats = async () => {
    try {
      setLoading(true);
      const cacheStats = await proxyMediaService.getCacheStats();
      setStats(cacheStats);
      onStatsChanged?.(cacheStats);
    } catch (error) {
      console.error('Error loading cache stats:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadMaxSize = async () => {
    try {
      const maxSize = await proxyMediaService.getMaxCacheSize();
      setMaxSizeGB((maxSize / (1024 * 1024 * 1024)).toFixed(1));
    } catch (error) {
      console.error('Error loading max cache size:', error);
    }
  };

  const handleClearCache = async () => {
    if (!confirm('Are you sure you want to clear all proxy cache? This cannot be undone.')) {
      return;
    }

    try {
      setLoading(true);
      await proxyMediaService.clearAllProxies();
      await loadStats();
    } catch (error) {
      console.error('Error clearing cache:', error);
      alert('Failed to clear cache. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleTriggerEviction = async () => {
    try {
      setLoading(true);
      await proxyMediaService.triggerEviction();
      await loadStats();
    } catch (error) {
      console.error('Error triggering eviction:', error);
      alert('Failed to trigger eviction. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleSetMaxSize = async () => {
    const sizeGB = parseFloat(maxSizeGB);
    if (isNaN(sizeGB) || sizeGB <= 0) {
      alert('Please enter a valid size greater than 0');
      return;
    }

    try {
      setLoading(true);
      const sizeBytes = Math.floor(sizeGB * 1024 * 1024 * 1024);
      await proxyMediaService.setMaxCacheSize(sizeBytes);
      await loadStats();
    } catch (error) {
      console.error('Error setting max size:', error);
      alert('Failed to set max size. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  if (loading && !stats) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading cache statistics..." />
      </div>
    );
  }

  if (!stats) {
    return (
      <div className={styles.container}>
        <Text>No cache statistics available</Text>
      </div>
    );
  }

  return (
    <Card className={styles.container}>
      <CardHeader
        header={<Text weight="semibold">Proxy Cache Management</Text>}
        description="Manage proxy media cache size and performance"
      />

      <div className={styles.statsGrid}>
        <Card className={styles.statCard}>
          <Text className={styles.statLabel}>Total Proxies</Text>
          <Text className={styles.statValue}>{stats.totalProxies}</Text>
        </Card>

        <Card className={styles.statCard}>
          <Text className={styles.statLabel}>Cache Size</Text>
          <Text className={styles.statValue}>{formatBytes(stats.totalCacheSizeBytes)}</Text>
        </Card>

        <Card className={styles.statCard}>
          <Text className={styles.statLabel}>Source Size</Text>
          <Text className={styles.statValue}>{formatBytes(stats.totalSourceSizeBytes)}</Text>
        </Card>

        <Card className={styles.statCard}>
          <Text className={styles.statLabel}>Space Saved</Text>
          <Text className={styles.statValue}>{(stats.compressionRatio * 100).toFixed(1)}%</Text>
        </Card>
      </div>

      <div className={styles.progressContainer}>
        <div style={{ marginBottom: tokens.spacingVerticalXS }}>
          <Text>
            Cache Usage: {formatBytes(stats.totalCacheSizeBytes)} / {formatBytes(stats.maxCacheSizeBytes)}
          </Text>
          {stats.isOverLimit && (
            <Text className={styles.warningText}> (Over Limit - Eviction Recommended)</Text>
          )}
        </div>
        <ProgressBar
          value={Math.min(stats.cacheUsagePercent / 100, 1)}
          color={stats.isOverLimit ? 'error' : stats.cacheUsagePercent > 80 ? 'warning' : 'success'}
        />
        <Text size={200}>{stats.cacheUsagePercent.toFixed(1)}% used</Text>
      </div>

      <div className={styles.actions}>
        <Tooltip content="Refresh statistics" relationship="label">
          <Button
            icon={<ArrowClockwise24Regular />}
            onClick={loadStats}
            disabled={loading}
            appearance="secondary"
          >
            Refresh
          </Button>
        </Tooltip>

        <Tooltip
          content="Manually remove least recently used proxies to free space"
          relationship="label"
        >
          <Button
            icon={<Delete24Regular />}
            onClick={handleTriggerEviction}
            disabled={loading || !stats.isOverLimit}
            appearance="secondary"
          >
            Evict LRU
          </Button>
        </Tooltip>

        <Tooltip content="Clear all proxy cache (cannot be undone)" relationship="label">
          <Button
            icon={<Delete24Regular />}
            onClick={handleClearCache}
            disabled={loading}
            appearance="secondary"
          >
            Clear All
          </Button>
        </Tooltip>
      </div>

      <div className={styles.settingsSection}>
        <Label>
          <Settings24Regular style={{ marginRight: tokens.spacingHorizontalXS }} />
          Maximum Cache Size
        </Label>
        <div className={styles.inputGroup}>
          <Input
            type="number"
            value={maxSizeGB}
            onChange={(e) => setMaxSizeGB(e.target.value)}
            disabled={loading}
            contentAfter="GB"
          />
          <Button onClick={handleSetMaxSize} disabled={loading} appearance="primary">
            Set Limit
          </Button>
        </div>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          Proxies will be automatically evicted when cache exceeds this limit
        </Text>
      </div>
    </Card>
  );
};

export default ProxyCacheManager;
