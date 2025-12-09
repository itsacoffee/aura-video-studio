import {
  Badge,
  Body1,
  Button,
  Caption1,
  Card,
  Divider,
  makeStyles,
  Spinner,
  Text,
  Title1,
  Title3,
  tokens,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Pulse24Regular,
  Server24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import type { AxiosError } from 'axios';
import React, { useCallback, useEffect, useState } from 'react';
import apiClient from '../../services/api/apiClient';
import type {
  HealthDetailsResponse,
  ProviderDashboardStatus,
  ProviderHealthCheckDto,
  ProviderHealthDashboardResponse,
  ProviderTypeHealthDto,
  SystemHealthDto,
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

  const mapHealthDetailsToSystem = (details: HealthDetailsResponse): SystemHealthDto => {
    const diskCheck = details.checks.find(
      (c) => c.id === 'disk_space' || c.name.toLowerCase().includes('disk')
    );
    const ffmpegCheck = details.checks.find(
      (c) => c.id?.toLowerCase().includes('ffmpeg') || c.name.toLowerCase().includes('ffmpeg')
    );
    const issues = details.checks
      .filter((c) => c.status === 'fail')
      .map((c) => c.message ?? `${c.name} failed`);

    const diskSpaceGB =
      typeof diskCheck?.data?.freeSpaceGB === 'number'
        ? diskCheck.data.freeSpaceGB
        : typeof diskCheck?.data?.freeSpaceGb === 'number'
          ? // Some backends might expose different casing
            diskCheck.data.freeSpaceGb
          : 0;

    const memoryCheck = details.checks.find((c) => c.name.toLowerCase().includes('memory'));
    const memoryUsagePercent =
      memoryCheck && typeof memoryCheck.data?.usagePercent === 'number'
        ? (memoryCheck.data.usagePercent as number)
        : 0;

    return {
      ffmpegAvailable: ffmpegCheck?.status === 'pass',
      ffmpegVersion: (ffmpegCheck?.data?.version as string | undefined) ?? null,
      diskSpaceGB,
      memoryUsagePercent,
      isHealthy: details.overallStatus === 'healthy',
      issues,
    };
  };

  const mapDashboardProviders = (
    dashboard: ProviderHealthDashboardResponse | null,
    category: 'LLM' | 'TTS' | 'Image'
  ): ProviderTypeHealthDto | null => {
    if (!dashboard) return null;

    const providersByCategory = dashboard.providers.filter(
      (provider) => provider.category.toLowerCase() === category.toLowerCase()
    );

    if (providersByCategory.length === 0) {
      return null;
    }

    const toProviderHealth = (provider: ProviderDashboardStatus): ProviderHealthCheckDto => {
      const successRatePercent = provider.successRate ?? 0;
      const successRate = successRatePercent / 100;
      const averageLatencyMs = provider.averageLatencyMs ?? 0;

      return {
        providerName: provider.name,
        isHealthy: provider.healthStatus === 'healthy',
        lastCheckTime: provider.lastCheckTime ?? new Date().toISOString(),
        responseTimeMs: averageLatencyMs,
        consecutiveFailures: provider.consecutiveFailures ?? 0,
        lastError: provider.lastError ?? null,
        successRate,
        averageResponseTimeMs: averageLatencyMs,
        circuitState: provider.circuitState ?? 'Closed',
        failureRate: Math.max(0, 1 - successRate),
        circuitOpenedAt: null,
      };
    };

    const providers = providersByCategory.map(toProviderHealth);
    const healthyCount = providers.filter((p) => p.isHealthy).length;

    return {
      providerType: category.toLowerCase(),
      providers,
      isHealthy: healthyCount > 0,
      healthyCount,
      totalCount: providers.length,
    };
  };

  const shouldFallbackToDashboard = (health: ProviderTypeHealthDto | null) => {
    if (!health) return true;
    if (health.totalCount === 0) return true;
    if (!health.providers || health.providers.length === 0) return true;
    return false;
  };

  const fetchHealthData = useCallback(async () => {
    const fetchHealthEndpoint = async <T,>(url: string) => {
      try {
        const response = await apiClient.get<T>(url);
        return response.data;
      } catch (err) {
        const axiosError = err as AxiosError<T>;
        if (axiosError.response?.data) {
          return axiosError.response.data;
        }

        setError(err instanceof Error ? err.message : 'Failed to fetch health data');
        return null;
      }
    };

    try {
      const [llm, tts, images, system] = await Promise.all([
        fetchHealthEndpoint<ProviderTypeHealthDto>('/api/health/llm'),
        fetchHealthEndpoint<ProviderTypeHealthDto>('/api/health/tts'),
        fetchHealthEndpoint<ProviderTypeHealthDto>('/api/health/images'),
        fetchHealthEndpoint<SystemHealthDto>('/api/health/system'),
      ]);

      let resolvedSystem = system;
      let resolvedLlm = llm;
      let resolvedTts = tts;
      let resolvedImages = images;

      // Fallback: if the legacy endpoint fails or returns null, use canonical health details endpoint
      if (!resolvedSystem) {
        const details = await fetchHealthEndpoint<HealthDetailsResponse>('/health/details');
        if (details) {
          resolvedSystem = mapHealthDetailsToSystem(details);
        }
      }

      const needsDashboard =
        shouldFallbackToDashboard(resolvedLlm) ||
        shouldFallbackToDashboard(resolvedTts) ||
        shouldFallbackToDashboard(resolvedImages);

      if (needsDashboard) {
        const dashboard =
          await fetchHealthEndpoint<ProviderHealthDashboardResponse>('/api/health-dashboard');
        if (dashboard) {
          resolvedLlm = resolvedLlm ?? mapDashboardProviders(dashboard, 'LLM');
          resolvedTts = resolvedTts ?? mapDashboardProviders(dashboard, 'TTS');
          resolvedImages = resolvedImages ?? mapDashboardProviders(dashboard, 'Image');

          // If legacy endpoints returned empty provider lists, replace them with dashboard data
          if (shouldFallbackToDashboard(resolvedLlm)) {
            resolvedLlm = mapDashboardProviders(dashboard, 'LLM');
          }
          if (shouldFallbackToDashboard(resolvedTts)) {
            resolvedTts = mapDashboardProviders(dashboard, 'TTS');
          }
          if (shouldFallbackToDashboard(resolvedImages)) {
            resolvedImages = mapDashboardProviders(dashboard, 'Image');
          }
        }
      }

      setLlmHealth(resolvedLlm);
      setTtsHealth(resolvedTts);
      setImagesHealth(resolvedImages);
      setSystemHealth(resolvedSystem);
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
