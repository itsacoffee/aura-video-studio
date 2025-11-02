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
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
  ErrorCircle24Regular,
  Server24Regular,
  Pulse24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import apiClient from '../../services/api/apiClient';
import type {
  ProviderTypeHealthDto,
  SystemHealthDto,
  ProviderHealthCheckDto,
} from '../../types/api-v1';

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
  section: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  sectionHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  systemCard: {
    padding: tokens.spacingVerticalL,
  },
  providerCard: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  providerName: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  metrics: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  metricRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  metricLabel: {
    color: tokens.colorNeutralForeground3,
  },
  errorDetails: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    wordBreak: 'break-word',
  },
  circuitState: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  warningBanner: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXL,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '400px',
    gap: tokens.spacingHorizontalM,
  },
});

const SystemHealthDashboard = () => {
  const styles = useStyles();
  const [llmHealth, setLlmHealth] = useState<ProviderTypeHealthDto | null>(null);
  const [ttsHealth, setTtsHealth] = useState<ProviderTypeHealthDto | null>(null);
  const [imagesHealth, setImagesHealth] = useState<ProviderTypeHealthDto | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealthDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchHealthData = useCallback(async () => {
    try {
      const [llm, tts, images, system] = await Promise.all([
        apiClient
          .get<ProviderTypeHealthDto>('/api/health/llm')
          .then((r) => r.data)
          .catch(() => null),
        apiClient
          .get<ProviderTypeHealthDto>('/api/health/tts')
          .then((r) => r.data)
          .catch(() => null),
        apiClient
          .get<ProviderTypeHealthDto>('/api/health/images')
          .then((r) => r.data)
          .catch(() => null),
        apiClient
          .get<SystemHealthDto>('/api/health/system')
          .then((r) => r.data)
          .catch(() => null),
      ]);

      setLlmHealth(llm);
      setTtsHealth(tts);
      setImagesHealth(images);
      setSystemHealth(system);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch health data');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchHealthData();
  }, [fetchHealthData]);

  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      fetchHealthData();
    }, 30000); // Poll every 30 seconds

    return () => clearInterval(interval);
  }, [autoRefresh, fetchHealthData]);

  const handleRefresh = () => {
    setLoading(true);
    fetchHealthData();
  };

  const handleTestConnection = async (providerName: string) => {
    try {
      await apiClient.post(`/api/health/providers/${providerName}/check`);
      await fetchHealthData();
    } catch (err) {
      console.error('Test connection failed:', err);
    }
  };

  const handleResetCircuitBreaker = async (providerName: string) => {
    try {
      await apiClient.post(`/api/health/providers/${providerName}/reset`);
      await fetchHealthData();
    } catch (err) {
      console.error('Reset circuit breaker failed:', err);
    }
  };

  const getStatusBadge = (isHealthy: boolean, circuitState?: string) => {
    if (circuitState === 'Open') {
      return (
        <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
          Circuit Open
        </Badge>
      );
    }
    if (circuitState === 'HalfOpen') {
      return (
        <Badge appearance="filled" color="warning" icon={<Warning24Regular />}>
          Testing
        </Badge>
      );
    }
    if (isHealthy) {
      return (
        <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
          Healthy
        </Badge>
      );
    }
    return (
      <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
        Unhealthy
      </Badge>
    );
  };

  const renderProviderCard = (provider: ProviderHealthCheckDto) => (
    <Card key={provider.providerName} className={styles.providerCard}>
      <div className={styles.cardHeader}>
        <div>
          <Text className={styles.providerName}>{provider.providerName}</Text>
        </div>
        {getStatusBadge(provider.isHealthy, provider.circuitState)}
      </div>

      <div className={styles.metrics}>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Success Rate</Caption1>
          <Body1>{(provider.successRate * 100).toFixed(1)}%</Body1>
        </div>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Failure Rate</Caption1>
          <Body1>{(provider.failureRate * 100).toFixed(1)}%</Body1>
        </div>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Avg Response Time</Caption1>
          <Body1>{provider.averageResponseTimeMs.toFixed(0)}ms</Body1>
        </div>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Consecutive Failures</Caption1>
          <Body1>{provider.consecutiveFailures}</Body1>
        </div>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Circuit State</Caption1>
          <Body1>{provider.circuitState}</Body1>
        </div>
        <div className={styles.metricRow}>
          <Caption1 className={styles.metricLabel}>Last Check</Caption1>
          <Body1>{new Date(provider.lastCheckTime).toLocaleTimeString()}</Body1>
        </div>
      </div>

      {provider.lastError && (
        <div className={styles.errorDetails}>
          <strong>Error: </strong> {provider.lastError}
        </div>
      )}

      <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, marginTop: 'auto' }}>
        <Button
          size="small"
          appearance="secondary"
          onClick={() => handleTestConnection(provider.providerName)}
        >
          Test Connection
        </Button>
        {provider.circuitState === 'Open' && (
          <Button
            size="small"
            appearance="primary"
            onClick={() => handleResetCircuitBreaker(provider.providerName)}
          >
            Reset Circuit
          </Button>
        )}
      </div>
    </Card>
  );

  const renderProviderSection = (
    title: string,
    health: ProviderTypeHealthDto | null,
    icon: React.ReactElement
  ) => (
    <div className={styles.section}>
      <div className={styles.sectionHeader}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          {icon}
          <Title3>{title}</Title3>
          {health && (
            <Badge appearance="tint">
              {health.healthyCount} / {health.totalCount} healthy
            </Badge>
          )}
        </div>
      </div>
      {health && health.providers.length > 0 ? (
        <div className={styles.grid}>{health.providers.map(renderProviderCard)}</div>
      ) : (
        <Text>No {title.toLowerCase()} configured</Text>
      )}
    </div>
  );

  const hasAnyProviderDown = () => {
    return (
      (llmHealth && !llmHealth.isHealthy) ||
      (ttsHealth && !ttsHealth.isHealthy) ||
      (imagesHealth && !imagesHealth.isHealthy)
    );
  };

  if (loading && !llmHealth && !ttsHealth && !imagesHealth) {
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
          <Text>Monitor provider and system health with circuit breaker status</Text>
        </div>
        <div className={styles.headerActions}>
          <label style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Caption1>Auto-refresh (30s)</Caption1>
            <input
              type="checkbox"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
            />
          </label>
          <Button icon={<ArrowClockwise24Regular />} onClick={handleRefresh} disabled={loading}>
            Refresh
          </Button>
        </div>
      </div>

      {error && (
        <Card className={styles.warningBanner}>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
            <ErrorCircle24Regular />
            <div>
              <Text weight="semibold">Failed to load health data</Text>
              <br />
              <Text>{error}</Text>
            </div>
          </div>
        </Card>
      )}

      {hasAnyProviderDown() && (
        <Card className={styles.warningBanner}>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
            <Warning24Regular />
            <div>
              <Text weight="semibold">Critical Provider Warning</Text>
              <br />
              <Text>
                One or more critical providers are unavailable. Video generation may fail.
              </Text>
            </div>
          </div>
        </Card>
      )}

      {systemHealth && (
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              <Server24Regular />
              <Title3>System Status</Title3>
              {getStatusBadge(systemHealth.isHealthy)}
            </div>
          </div>
          <Card className={styles.systemCard}>
            <div className={styles.metrics}>
              <div className={styles.metricRow}>
                <Caption1 className={styles.metricLabel}>FFmpeg Available</Caption1>
                <Body1>{systemHealth.ffmpegAvailable ? 'Yes' : 'No'}</Body1>
              </div>
              {systemHealth.ffmpegVersion && (
                <div className={styles.metricRow}>
                  <Caption1 className={styles.metricLabel}>FFmpeg Version</Caption1>
                  <Body1>{systemHealth.ffmpegVersion}</Body1>
                </div>
              )}
              <div className={styles.metricRow}>
                <Caption1 className={styles.metricLabel}>Disk Space</Caption1>
                <Body1>{systemHealth.diskSpaceGB.toFixed(2)} GB</Body1>
              </div>
              <div className={styles.metricRow}>
                <Caption1 className={styles.metricLabel}>Memory Usage</Caption1>
                <Body1>{systemHealth.memoryUsagePercent.toFixed(1)}%</Body1>
              </div>
            </div>
            {systemHealth.issues.length > 0 && (
              <div className={styles.errorDetails}>
                <strong>Issues:</strong>
                <ul style={{ marginTop: tokens.spacingVerticalS, paddingLeft: '20px' }}>
                  {systemHealth.issues.map((issue, idx) => (
                    <li key={idx}>{issue}</li>
                  ))}
                </ul>
              </div>
            )}
          </Card>
        </div>
      )}

      <Divider />

      {renderProviderSection('LLM Providers', llmHealth, <Pulse24Regular />)}
      {renderProviderSection('TTS Providers', ttsHealth, <Pulse24Regular />)}
      {renderProviderSection('Image Providers', imagesHealth, <Pulse24Regular />)}
    </div>
  );
};

export default SystemHealthDashboard;
