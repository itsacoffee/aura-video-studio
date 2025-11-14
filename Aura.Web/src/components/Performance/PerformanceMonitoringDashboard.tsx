import {
  Card,
  Title3,
  Body1,
  Caption1,
  Button,
  makeStyles,
  tokens,
  Badge,
} from '@fluentui/react-components';
import { ChartMultiple24Regular, Warning24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect, useCallback } from 'react';
import { performanceMonitor } from '@/utils/performanceMonitor';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
    padding: '16px',
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '12px',
  },
  metricCard: {
    padding: '16px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
    marginBottom: '4px',
  },
  metricValue: {
    fontSize: '24px',
    fontWeight: 600,
    color: tokens.colorNeutralForeground1,
  },
  metricUnit: {
    fontSize: '14px',
    color: tokens.colorNeutralForeground3,
    marginLeft: '4px',
  },
  warningCard: {
    padding: '12px',
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  actions: {
    display: 'flex',
    gap: '8px',
    marginTop: '12px',
  },
});

interface PerformanceMetrics {
  memoryUsed: number;
  memoryTotal: number;
  memoryLimit: number;
  renderCount: number;
  lastRenderTime: number;
  loadTime: number;
}

export const PerformanceMonitoringDashboard: React.FC = React.memo(() => {
  const styles = useStyles();
  const [metrics, setMetrics] = useState<PerformanceMetrics>({
    memoryUsed: 0,
    memoryTotal: 0,
    memoryLimit: 0,
    renderCount: 0,
    lastRenderTime: 0,
    loadTime: 0,
  });
  const [showWarning, setShowWarning] = useState(false);

  const updateMetrics = useCallback(() => {
    const perfMetrics = performanceMonitor.getMetrics();

    const memoryUsage = perfMetrics.memoryUsage || {
      usedJSHeapSize: 0,
      totalJSHeapSize: 0,
      jsHeapSizeLimit: 0,
    };

    const loadTime = performance.timing
      ? performance.timing.loadEventEnd - performance.timing.navigationStart
      : 0;

    const newMetrics: PerformanceMetrics = {
      memoryUsed: memoryUsage.usedJSHeapSize / 1024 / 1024,
      memoryTotal: memoryUsage.totalJSHeapSize / 1024 / 1024,
      memoryLimit: memoryUsage.jsHeapSizeLimit / 1024 / 1024,
      renderCount: perfMetrics.renderCount,
      lastRenderTime: perfMetrics.lastRenderTime,
      loadTime: loadTime / 1000,
    };

    setMetrics(newMetrics);

    const memoryUsagePercent = (newMetrics.memoryUsed / newMetrics.memoryLimit) * 100;
    setShowWarning(memoryUsagePercent > 80);
  }, []);

  useEffect(() => {
    updateMetrics();

    const interval = setInterval(updateMetrics, 2000);

    return () => {
      clearInterval(interval);
    };
  }, [updateMetrics]);

  const handleClearMetrics = useCallback(() => {
    performanceMonitor.reset();
    updateMetrics();
  }, [updateMetrics]);

  const handleForceGC = useCallback(() => {
    if ('gc' in window && typeof (window as Window & { gc?: () => void }).gc === 'function') {
      (window as Window & { gc: () => void }).gc();
      setTimeout(updateMetrics, 500);
    } else {
      alert('Garbage collection is not available. Run Chrome with --js-flags="--expose-gc"');
    }
  }, [updateMetrics]);

  const memoryUsagePercent =
    metrics.memoryLimit > 0 ? (metrics.memoryUsed / metrics.memoryLimit) * 100 : 0;

  const getMemoryBadgeColor = (): 'danger' | 'warning' | 'success' => {
    if (memoryUsagePercent > 80) return 'danger';
    if (memoryUsagePercent > 60) return 'warning';
    return 'success';
  };

  return (
    <div className={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title3>Performance Monitoring</Title3>
        <Badge appearance="filled" color={getMemoryBadgeColor()}>
          <ChartMultiple24Regular />
          {memoryUsagePercent > 0 ? `${memoryUsagePercent.toFixed(1)}%` : 'N/A'}
        </Badge>
      </div>

      {showWarning && (
        <div className={styles.warningCard}>
          <Warning24Regular />
          <Body1>
            High memory usage detected. Consider closing unused tabs or refreshing the page.
          </Body1>
        </div>
      )}

      <div className={styles.metricsGrid}>
        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Memory Used</Caption1>
          <div>
            <span className={styles.metricValue}>{metrics.memoryUsed.toFixed(1)}</span>
            <span className={styles.metricUnit}>MB</span>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Memory Total</Caption1>
          <div>
            <span className={styles.metricValue}>{metrics.memoryTotal.toFixed(1)}</span>
            <span className={styles.metricUnit}>MB</span>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Memory Limit</Caption1>
          <div>
            <span className={styles.metricValue}>
              {metrics.memoryLimit > 0 ? metrics.memoryLimit.toFixed(1) : 'N/A'}
            </span>
            <span className={styles.metricUnit}>MB</span>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Render Count</Caption1>
          <div>
            <span className={styles.metricValue}>{metrics.renderCount}</span>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Last Render</Caption1>
          <div>
            <span className={styles.metricValue}>{metrics.lastRenderTime.toFixed(2)}</span>
            <span className={styles.metricUnit}>ms</span>
          </div>
        </Card>

        <Card className={styles.metricCard}>
          <Caption1 className={styles.metricLabel}>Load Time</Caption1>
          <div>
            <span className={styles.metricValue}>
              {metrics.loadTime > 0 ? metrics.loadTime.toFixed(2) : 'N/A'}
            </span>
            <span className={styles.metricUnit}>s</span>
          </div>
        </Card>
      </div>

      <div className={styles.actions}>
        <Button appearance="secondary" onClick={handleClearMetrics}>
          Reset Metrics
        </Button>
        <Button appearance="secondary" onClick={handleForceGC}>
          Force GC (Dev Only)
        </Button>
        <Button appearance="primary" onClick={() => performanceMonitor.logMetrics('Manual Check')}>
          Log to Console
        </Button>
      </div>
    </div>
  );
});

PerformanceMonitoringDashboard.displayName = 'PerformanceMonitoringDashboard';
