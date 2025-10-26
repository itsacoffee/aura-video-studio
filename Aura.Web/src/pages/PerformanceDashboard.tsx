/**
 * Performance Dashboard
 *
 * Developer tool for monitoring application performance.
 * Shows render times, bundle sizes, memory usage, and performance metrics.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Switch,
  Badge,
} from '@fluentui/react-components';
import { ArrowSync24Regular, Delete24Regular, ArrowDownload24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useMemo } from 'react';
import { performanceMonitor } from '../services/performanceMonitor';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightBold,
    marginBottom: tokens.spacingVerticalS,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorNeutralForeground3,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
    alignItems: 'center',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  cardTitle: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalM,
  },
  metric: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
  },
  metricValue: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
  },
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalM,
  },
  budgetWarning: {
    backgroundColor: tokens.colorPaletteYellowBackground2,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  table: {
    marginTop: tokens.spacingVerticalM,
  },
});

export function PerformanceDashboard() {
  const styles = useStyles();
  const [isEnabled, setIsEnabled] = useState(performanceMonitor.isMonitoringEnabled());
  const [, setUpdateCounter] = useState(0);

  // Force re-render every second to show live data
  useEffect(() => {
    const interval = setInterval(() => {
      setUpdateCounter((prev) => prev + 1);
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  const summary = useMemo(() => performanceMonitor.getSummary(), []);
  const renderMetrics = useMemo(() => performanceMonitor.getRenderMetrics(), []);
  const budgets = useMemo(() => performanceMonitor.getBudgets(), []);
  const bundleMetrics = useMemo(() => performanceMonitor.getBundleMetrics(), []);

  const handleToggleMonitoring = (checked: boolean) => {
    performanceMonitor.setEnabled(checked);
    setIsEnabled(checked);
    localStorage.setItem('enablePerformanceMonitoring', String(checked));
  };

  const handleClearMetrics = () => {
    performanceMonitor.clearMetrics();
    setUpdateCounter((prev) => prev + 1);
  };

  const handleExportMetrics = () => {
    const json = performanceMonitor.exportMetrics();
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `performance-metrics-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const formatBytes = (bytes?: number) => {
    if (!bytes) return 'N/A';
    const mb = bytes / (1024 * 1024);
    return `${mb.toFixed(2)} MB`;
  };

  const formatMs = (ms: number) => {
    return `${ms.toFixed(2)} ms`;
  };

  const getBudgetStatus = (componentName: string, actualTime: number) => {
    const budget = budgets.find((b) => b.name === componentName);
    if (!budget) return 'unknown';
    return actualTime > budget.maxRenderTime ? 'over' : 'under';
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Performance Dashboard</Text>
        <Text className={styles.subtitle}>
          Monitor application performance, render times, and bundle sizes
        </Text>
      </div>

      <div className={styles.controls}>
        <Switch
          checked={isEnabled}
          onChange={(_, data) => handleToggleMonitoring(data.checked)}
          label="Enable Performance Monitoring"
        />
        <Button
          appearance="subtle"
          icon={<ArrowSync24Regular />}
          onClick={() => setUpdateCounter((prev) => prev + 1)}
        >
          Refresh
        </Button>
        <Button appearance="subtle" icon={<Delete24Regular />} onClick={handleClearMetrics}>
          Clear Metrics
        </Button>
        <Button
          appearance="primary"
          icon={<ArrowDownload24Regular />}
          onClick={handleExportMetrics}
        >
          Export
        </Button>
      </div>

      {/* Summary Cards */}
      <div className={styles.grid}>
        <Card className={styles.card}>
          <Text className={styles.cardTitle}>Performance Summary</Text>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Total Components:</Text>
            <Text className={styles.metricValue}>{summary.totalComponents}</Text>
          </div>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Total Renders:</Text>
            <Text className={styles.metricValue}>{summary.totalRenders}</Text>
          </div>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Avg Render Time:</Text>
            <Text className={styles.metricValue}>{formatMs(summary.averageRenderTime)}</Text>
          </div>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Estimated FPS:</Text>
            <Text className={styles.metricValue}>{summary.fps}</Text>
          </div>
        </Card>

        <Card className={styles.card}>
          <Text className={styles.cardTitle}>Memory Usage</Text>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Used Heap:</Text>
            <Text className={styles.metricValue}>
              {formatBytes(summary.memoryUsage.usedJSHeapSize)}
            </Text>
          </div>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Total Heap:</Text>
            <Text className={styles.metricValue}>
              {formatBytes(summary.memoryUsage.totalJSHeapSize)}
            </Text>
          </div>
          <div className={styles.metric}>
            <Text className={styles.metricLabel}>Heap Limit:</Text>
            <Text className={styles.metricValue}>
              {formatBytes(summary.memoryUsage.jsHeapSizeLimit)}
            </Text>
          </div>
        </Card>

        <Card className={styles.card}>
          <Text className={styles.cardTitle}>Slowest Component</Text>
          {summary.slowestComponent ? (
            <>
              <div className={styles.metric}>
                <Text className={styles.metricLabel}>Name:</Text>
                <Text className={styles.metricValue}>{summary.slowestComponent.componentName}</Text>
              </div>
              <div className={styles.metric}>
                <Text className={styles.metricLabel}>Max Render:</Text>
                <Text className={styles.metricValue}>
                  {formatMs(summary.slowestComponent.maxDuration)}
                </Text>
              </div>
              <div className={styles.metric}>
                <Text className={styles.metricLabel}>Avg Render:</Text>
                <Text className={styles.metricValue}>
                  {formatMs(summary.slowestComponent.averageDuration)}
                </Text>
              </div>
            </>
          ) : (
            <Text>No data available</Text>
          )}
        </Card>
      </div>

      {/* Component Render Metrics */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Component Render Metrics</Text>
        {renderMetrics.length > 0 ? (
          <div className={styles.table}>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Component</TableHeaderCell>
                  <TableHeaderCell>Renders</TableHeaderCell>
                  <TableHeaderCell>Avg Time</TableHeaderCell>
                  <TableHeaderCell>Max Time</TableHeaderCell>
                  <TableHeaderCell>Min Time</TableHeaderCell>
                  <TableHeaderCell>Status</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {renderMetrics.map((metric) => (
                  <TableRow key={metric.componentName}>
                    <TableCell>{metric.componentName}</TableCell>
                    <TableCell>{metric.renderCount}</TableCell>
                    <TableCell>{formatMs(metric.averageDuration)}</TableCell>
                    <TableCell>{formatMs(metric.maxDuration)}</TableCell>
                    <TableCell>{formatMs(metric.minDuration)}</TableCell>
                    <TableCell>
                      {getBudgetStatus(metric.componentName, metric.maxDuration) === 'over' ? (
                        <Badge appearance="filled" color="danger">
                          Over Budget
                        </Badge>
                      ) : getBudgetStatus(metric.componentName, metric.maxDuration) === 'under' ? (
                        <Badge appearance="filled" color="success">
                          Under Budget
                        </Badge>
                      ) : (
                        <Badge appearance="outline">No Budget</Badge>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        ) : (
          <Text>No render metrics available. Enable monitoring and interact with the app.</Text>
        )}
      </div>

      {/* Performance Budgets */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Performance Budgets</Text>
        <div className={styles.table}>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Component</TableHeaderCell>
                <TableHeaderCell>Max Render Time</TableHeaderCell>
                <TableHeaderCell>Max Bundle Size</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {budgets.map((budget) => (
                <TableRow key={budget.name}>
                  <TableCell>{budget.name}</TableCell>
                  <TableCell>{formatMs(budget.maxRenderTime)}</TableCell>
                  <TableCell>{(budget.maxBundleSize / 1024).toFixed(2)} KB</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>

      {/* Bundle Metrics */}
      {bundleMetrics.length > 0 && (
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Bundle Metrics</Text>
          <div className={styles.table}>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Bundle</TableHeaderCell>
                  <TableHeaderCell>Size</TableHeaderCell>
                  <TableHeaderCell>Gzip Size</TableHeaderCell>
                  <TableHeaderCell>Timestamp</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {bundleMetrics.map((metric, index) => (
                  <TableRow key={index}>
                    <TableCell>{metric.name}</TableCell>
                    <TableCell>{(metric.size / 1024).toFixed(2)} KB</TableCell>
                    <TableCell>
                      {metric.gzipSize ? `${(metric.gzipSize / 1024).toFixed(2)} KB` : 'N/A'}
                    </TableCell>
                    <TableCell>{new Date(metric.timestamp).toLocaleString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      )}
    </div>
  );
}
