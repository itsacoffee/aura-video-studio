import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  ProgressBar,
  Tooltip,
} from '@fluentui/react-components';
import {
  Window24Regular,
  DataArea24Regular,
  HardDrive24Regular,
  Warning24Filled,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    marginBottom: tokens.spacingVerticalS,
  },
  resourceItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  resourceHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
  },
  resourceLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
  },
  resourceValue: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  suggestion: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    paddingLeft: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXXS,
  },
  highUsage: {
    color: tokens.colorPaletteYellowForeground1,
  },
  criticalUsage: {
    color: tokens.colorPaletteRedForeground1,
  },
});

interface ResourceMetrics {
  cpu: number; // percentage
  memory: number; // percentage
  gpu: number; // percentage
  diskIO: number; // MB/s
}

interface ResourceMonitorProps {
  compact?: boolean;
}

export function ResourceMonitor({ compact = false }: ResourceMonitorProps) {
  const styles = useStyles();
  const [metrics, setMetrics] = useState<ResourceMetrics>({
    cpu: 0,
    memory: 0,
    gpu: 0,
    diskIO: 0,
  });

  // Simulate resource monitoring
  // In production, this would use performance.memory, navigator.deviceMemory, etc.
  useEffect(() => {
    const updateMetrics = () => {
      // Use performance.memory if available
      let memoryUsage = 0;
      if ('memory' in performance) {
        const mem = (performance as any).memory;
        memoryUsage = (mem.usedJSHeapSize / mem.jsHeapSizeLimit) * 100;
      }

      // Simulate CPU and GPU (would need native integration for real metrics)
      const cpuUsage = Math.random() * 30 + 20; // Simulated 20-50%
      const gpuUsage = Math.random() * 40 + 10; // Simulated 10-50%
      const diskIO = Math.random() * 50 + 10; // Simulated 10-60 MB/s

      setMetrics({
        cpu: cpuUsage,
        memory: memoryUsage || Math.random() * 40 + 20, // Fallback to simulated
        gpu: gpuUsage,
        diskIO: diskIO,
      });
    };

    updateMetrics();
    const interval = setInterval(updateMetrics, 2000);

    return () => clearInterval(interval);
  }, []);

  const getUsageColor = (value: number): 'success' | 'warning' | 'error' => {
    if (value >= 90) return 'error';
    if (value >= 75) return 'warning';
    return 'success';
  };

  const getUsageClass = (value: number): string => {
    if (value >= 90) return styles.criticalUsage;
    if (value >= 75) return styles.highUsage;
    return '';
  };

  const getSuggestion = (metric: string, value: number): string | null => {
    if (value < 75) return null;

    const suggestions: Record<string, string> = {
      cpu: value >= 90 
        ? 'Close other applications to improve performance'
        : 'Consider reducing video quality or effects complexity',
      memory: value >= 90
        ? 'Close unused browser tabs and applications'
        : 'Consider working with lower resolution proxies',
      gpu: value >= 90
        ? 'Disable GPU-intensive effects temporarily'
        : 'Reduce preview quality to free up GPU resources',
      diskIO: value >= 90
        ? 'Move working files to faster storage (SSD)'
        : 'Consider reducing simultaneous operations',
    };

    return suggestions[metric] || null;
  };

  if (compact) {
    return (
      <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
        <Tooltip content={`CPU: ${metrics.cpu.toFixed(0)}%`} relationship="label">
          <div className={`${styles.resourceLabel} ${getUsageClass(metrics.cpu)}`}>
            <Window24Regular />
            <Text size={200}>{metrics.cpu.toFixed(0)}%</Text>
          </div>
        </Tooltip>
        <Tooltip content={`Memory: ${metrics.memory.toFixed(0)}%`} relationship="label">
          <div className={`${styles.resourceLabel} ${getUsageClass(metrics.memory)}`}>
            <DataArea24Regular />
            <Text size={200}>{metrics.memory.toFixed(0)}%</Text>
          </div>
        </Tooltip>
        {metrics.gpu > 0 && (
          <Tooltip content={`GPU: ${metrics.gpu.toFixed(0)}%`} relationship="label">
            <div className={`${styles.resourceLabel} ${getUsageClass(metrics.gpu)}`}>
              <HardDrive24Regular />
              <Text size={200}>{metrics.gpu.toFixed(0)}%</Text>
            </div>
          </Tooltip>
        )}
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Text className={styles.title}>System Resources</Text>

      {/* CPU Usage */}
      <div className={styles.resourceItem}>
        <div className={styles.resourceHeader}>
          <div className={styles.resourceLabel}>
            <Window24Regular />
            <Text>CPU</Text>
          </div>
          <div className={styles.resourceValue}>
            {metrics.cpu >= 75 && <Warning24Filled className={styles.warningIcon} />}
            <Text className={getUsageClass(metrics.cpu)}>{metrics.cpu.toFixed(1)}%</Text>
          </div>
        </div>
        <ProgressBar 
          value={metrics.cpu / 100} 
          color={getUsageColor(metrics.cpu)}
        />
        {getSuggestion('cpu', metrics.cpu) && (
          <Text className={styles.suggestion}>{getSuggestion('cpu', metrics.cpu)}</Text>
        )}
      </div>

      {/* Memory Usage */}
      <div className={styles.resourceItem}>
        <div className={styles.resourceHeader}>
          <div className={styles.resourceLabel}>
            <DataArea24Regular />
            <Text>Memory</Text>
          </div>
          <div className={styles.resourceValue}>
            {metrics.memory >= 75 && <Warning24Filled className={styles.warningIcon} />}
            <Text className={getUsageClass(metrics.memory)}>{metrics.memory.toFixed(1)}%</Text>
          </div>
        </div>
        <ProgressBar 
          value={metrics.memory / 100} 
          color={getUsageColor(metrics.memory)}
        />
        {getSuggestion('memory', metrics.memory) && (
          <Text className={styles.suggestion}>{getSuggestion('memory', metrics.memory)}</Text>
        )}
      </div>

      {/* GPU Usage */}
      {metrics.gpu > 0 && (
        <div className={styles.resourceItem}>
          <div className={styles.resourceHeader}>
            <div className={styles.resourceLabel}>
              <HardDrive24Regular />
              <Text>GPU</Text>
            </div>
            <div className={styles.resourceValue}>
              {metrics.gpu >= 75 && <Warning24Filled className={styles.warningIcon} />}
              <Text className={getUsageClass(metrics.gpu)}>{metrics.gpu.toFixed(1)}%</Text>
            </div>
          </div>
          <ProgressBar 
            value={metrics.gpu / 100} 
            color={getUsageColor(metrics.gpu)}
          />
          {getSuggestion('gpu', metrics.gpu) && (
            <Text className={styles.suggestion}>{getSuggestion('gpu', metrics.gpu)}</Text>
          )}
        </div>
      )}

      {/* Disk I/O */}
      <div className={styles.resourceItem}>
        <div className={styles.resourceHeader}>
          <div className={styles.resourceLabel}>
            <HardDrive24Regular />
            <Text>Disk I/O</Text>
          </div>
          <div className={styles.resourceValue}>
            <Text>{metrics.diskIO.toFixed(1)} MB/s</Text>
          </div>
        </div>
      </div>
    </div>
  );
}
