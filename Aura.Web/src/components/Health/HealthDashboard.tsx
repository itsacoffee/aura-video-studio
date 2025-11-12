/**
 * Comprehensive Health Dashboard
 * Displays system health status with real-time monitoring
 */

import {
  makeStyles,
  tokens,
  Title1,
  Title3,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
  Caption1,
  Body1,
  Divider,
  Switch,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  CheckmarkCircle24Filled,
  Warning24Filled,
  ErrorCircle24Filled,
  Server24Regular,
  Database24Regular,
  // CloudDatabase24Regular,  // Not available in this version
  HardDrive24Regular,
  DeveloperBoard24Regular,
} from '@fluentui/react-icons';
import React from 'react';
import { useHealthMonitoring } from '../../hooks/useHealthMonitoring';
import type { HealthCheckEntry } from '../../services/api/healthApi';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  overallStatus: {
    marginBottom: tokens.spacingVerticalXL,
  },
  statusCard: {
    padding: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXL,
  },
  statusIcon: {
    fontSize: '48px',
  },
  statusContent: {
    flex: 1,
  },
  statusMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalXL,
    marginTop: tokens.spacingVerticalM,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(400px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalL,
  },
  checkCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  checkHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  checkTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  metrics: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingVerticalS,
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
  },
  errorBanner: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  warningBanner: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '400px',
    gap: tokens.spacingHorizontalM,
  },
  tags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalS,
  },
});

const getStatusIcon = (status: string) => {
  switch (status) {
    case 'healthy':
      return <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />;
    case 'degraded':
      return <Warning24Filled style={{ color: tokens.colorPaletteYellowForeground1 }} />;
    case 'unhealthy':
      return <ErrorCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />;
    default:
      return <ErrorCircle24Filled />;
  }
};

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'healthy':
      return (
        <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Filled />}>
          Healthy
        </Badge>
      );
    case 'degraded':
      return (
        <Badge appearance="filled" color="warning" icon={<Warning24Filled />}>
          Degraded
        </Badge>
      );
    case 'unhealthy':
      return (
        <Badge appearance="filled" color="danger" icon={<ErrorCircle24Filled />}>
          Unhealthy
        </Badge>
      );
    default:
      return <Badge appearance="outline">Unknown</Badge>;
  }
};

const getCheckIcon = (name: string): React.JSX.Element => {
  const iconMap: Record<string, React.JSX.Element> = {
    Database: <Database24Regular />,
    Dependencies: <DeveloperBoard24Regular />,
    DiskSpace: <HardDrive24Regular />,
    Memory: <Database24Regular />,
    Providers: <Server24Regular />,
    Startup: <Server24Regular />,
  };
  return iconMap[name] || <Server24Regular />;
};

export function HealthDashboard() {
  const styles = useStyles();
  const {
    health,
    loading,
    error,
    retryCount,
    isMonitoring,
    lastUpdate,
    startMonitoring,
    stopMonitoring,
    refresh,
  } = useHealthMonitoring({
    pollingInterval: 30000,
    enableAutoRetry: true,
    autoStart: true,
  });

  const handleToggleMonitoring = () => {
    if (isMonitoring) {
      stopMonitoring();
    } else {
      startMonitoring();
    }
  };

  const renderMetrics = (check: HealthCheckEntry) => {
    if (!check.data || Object.keys(check.data).length === 0) {
      return null;
    }

    const importantMetrics = Object.entries(check.data)
      .filter(([key]) => !key.includes('_count') && !Array.isArray(check.data![key]))
      .slice(0, 6);

    if (importantMetrics.length === 0) {
      return null;
    }

    return (
      <div className={styles.metrics}>
        {importantMetrics.map(([key, value]) => (
          <div key={key} className={styles.metricItem}>
            <Caption1 className={styles.metricLabel}>
              {key.replace(/_/g, ' ').replace(/\b\w/g, (l) => l.toUpperCase())}
            </Caption1>
            <Body1>{typeof value === 'number' ? value.toLocaleString() : String(value)}</Body1>
          </div>
        ))}
      </div>
    );
  };

  const renderCheck = (check: HealthCheckEntry) => (
    <Card key={check.name} className={styles.checkCard}>
      <div className={styles.checkHeader}>
        <div className={styles.checkTitle}>
          {getCheckIcon(check.name)}
          <Text weight="semibold" size={400}>
            {check.name}
          </Text>
        </div>
        {getStatusBadge(check.status)}
      </div>

      {check.description && <Caption1>{check.description}</Caption1>}

      {renderMetrics(check)}

      {check.exception && (
        <div
          style={{
            padding: tokens.spacingVerticalS,
            backgroundColor: tokens.colorNeutralBackground3,
            borderRadius: tokens.borderRadiusSmall,
          }}
        >
          <Caption1 style={{ color: tokens.colorPaletteRedForeground1 }}>
            Error: {check.exception}
          </Caption1>
        </div>
      )}

      {check.tags && check.tags.length > 0 && (
        <div className={styles.tags}>
          {check.tags.map((tag) => (
            <Badge key={tag} appearance="tint" size="small">
              {tag}
            </Badge>
          ))}
        </div>
      )}

      <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
        Check duration: {check.duration.toFixed(0)}ms
      </Caption1>
    </Card>
  );

  if (loading && !health) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" />
        <Text>Loading health status...</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>System Health Dashboard</Title1>
          <Text>Real-time monitoring of all system dependencies and services</Text>
        </div>
        <div className={styles.headerActions}>
          <Switch
            label="Auto-refresh"
            checked={isMonitoring}
            onChange={handleToggleMonitoring}
          />
          <Button
            icon={<ArrowClockwise24Regular />}
            onClick={refresh}
            disabled={loading}
            appearance="primary"
          >
            Refresh
          </Button>
        </div>
      </div>

      {error && (
        <Card className={styles.errorBanner}>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
            <ErrorCircle24Filled />
            <div>
              <Text weight="semibold">Failed to load health data</Text>
              <br />
              <Text>{error.message}</Text>
              {retryCount > 0 && (
                <>
                  <br />
                  <Caption1>Retry attempt: {retryCount}/3</Caption1>
                </>
              )}
            </div>
          </div>
        </Card>
      )}

      {health && (
        <>
          {health.status === 'unhealthy' && (
            <Card className={styles.errorBanner}>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
                <ErrorCircle24Filled />
                <div>
                  <Text weight="semibold">System Unhealthy</Text>
                  <br />
                  <Text>One or more critical health checks are failing. Please review below.</Text>
                </div>
              </div>
            </Card>
          )}

          {health.status === 'degraded' && (
            <Card className={styles.warningBanner}>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
                <Warning24Filled />
                <div>
                  <Text weight="semibold">System Degraded</Text>
                  <br />
                  <Text>Some health checks are reporting degraded status. System may operate with reduced functionality.</Text>
                </div>
              </div>
            </Card>
          )}

          <div className={styles.overallStatus}>
            <Card className={styles.statusCard}>
              <div className={styles.statusIcon}>{getStatusIcon(health.status)}</div>
              <div className={styles.statusContent}>
                <Title3>Overall Status: {health.status.toUpperCase()}</Title3>
                <div className={styles.statusMeta}>
                  <div>
                    <Caption1>Last Updated</Caption1>
                    <Body1>{lastUpdate?.toLocaleTimeString() || 'Never'}</Body1>
                  </div>
                  <div>
                    <Caption1>Total Duration</Caption1>
                    <Body1>{health.duration?.toFixed(0) || 0}ms</Body1>
                  </div>
                  {health.environment && (
                    <div>
                      <Caption1>Environment</Caption1>
                      <Body1>{health.environment}</Body1>
                    </div>
                  )}
                  {health.version && (
                    <div>
                      <Caption1>Version</Caption1>
                      <Body1>{health.version}</Body1>
                    </div>
                  )}
                  <div>
                    <Caption1>Total Checks</Caption1>
                    <Body1>{health.checks.length}</Body1>
                  </div>
                  <div>
                    <Caption1>Healthy</Caption1>
                    <Body1 style={{ color: tokens.colorPaletteGreenForeground1 }}>
                      {health.checks.filter((c) => c.status === 'healthy').length}
                    </Body1>
                  </div>
                  <div>
                    <Caption1>Degraded</Caption1>
                    <Body1 style={{ color: tokens.colorPaletteYellowForeground1 }}>
                      {health.checks.filter((c) => c.status === 'degraded').length}
                    </Body1>
                  </div>
                  <div>
                    <Caption1>Unhealthy</Caption1>
                    <Body1 style={{ color: tokens.colorPaletteRedForeground1 }}>
                      {health.checks.filter((c) => c.status === 'unhealthy').length}
                    </Body1>
                  </div>
                </div>
              </div>
            </Card>
          </div>

          <Divider />

          <div className={styles.grid}>{health.checks.map(renderCheck)}</div>
        </>
      )}
    </div>
  );
}

export default HealthDashboard;
