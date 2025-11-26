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
  Switch,
  Caption1,
  Body1,
  Divider,
  Tooltip,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
  ErrorCircle24Regular,
  Settings24Regular,
  Clock24Regular,
  Key24Regular,
  Info24Regular,
  Gauge24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiUrl } from '../config/api';
import type { ProviderHealthDashboardResponse, ProviderDashboardStatus } from '../types/api-v1';

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
  summarySection: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  summaryCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
  },
  summaryNumber: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightBold,
    lineHeight: 1,
    marginBottom: tokens.spacingVerticalXS,
  },
  summaryLabel: {
    color: tokens.colorNeutralForeground3,
  },
  filterButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalL,
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  categoryHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
    gap: tokens.spacingVerticalL,
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
  providerInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  providerName: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  providerMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  tierBadge: {
    fontSize: tokens.fontSizeBase100,
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
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  quotaSection: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  quotaHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  quotaText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  cardActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: 'auto',
    paddingTop: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '400px',
    gap: tokens.spacingHorizontalM,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
  notConfiguredBanner: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  legend: {
    marginTop: tokens.spacingVerticalXXL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  legendTitle: {
    marginBottom: tokens.spacingVerticalM,
    fontWeight: tokens.fontWeightSemibold,
  },
  legendItems: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

type CategoryFilter = 'all' | 'LLM' | 'TTS' | 'Image';

export function ProvidersPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [dashboardData, setDashboardData] = useState<ProviderHealthDashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(true); // Default to on for 30-second refresh
  const [filter, setFilter] = useState<CategoryFilter>('all');

  const loadDashboard = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const response = await fetch(`${apiUrl}/health-dashboard`);
      if (response.ok) {
        const data: ProviderHealthDashboardResponse = await response.json();
        setDashboardData(data);
      }
    } catch (error: unknown) {
      console.error('Failed to load provider health dashboard:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  // Auto-refresh every 30 seconds as per requirement
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      loadDashboard(true);
    }, 30000); // 30 seconds

    return () => clearInterval(interval);
  }, [autoRefresh, loadDashboard]);

  const handleRefresh = async () => {
    setRefreshing(true);
    await loadDashboard(true);
    setRefreshing(false);
  };

  const handleConfigure = (providerName: string) => {
    navigate('/settings', { state: { focusProvider: providerName } });
  };

  const getStatusBadge = (status: ProviderDashboardStatus['healthStatus']) => {
    switch (status) {
      case 'healthy':
        return (
          <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
            Healthy
          </Badge>
        );
      case 'degraded':
        return (
          <Badge appearance="filled" color="warning" icon={<Warning24Regular />}>
            Degraded
          </Badge>
        );
      case 'offline':
        return (
          <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
            Offline
          </Badge>
        );
      case 'not_configured':
        return (
          <Badge appearance="tint" color="warning" icon={<Key24Regular />}>
            Not Configured
          </Badge>
        );
      case 'unknown':
      default:
        return (
          <Badge appearance="tint" color="subtle" icon={<Info24Regular />}>
            Unknown
          </Badge>
        );
    }
  };

  const getTierBadge = (tier: string) => {
    if (tier === 'Premium') {
      return (
        <Badge appearance="outline" color="brand" className={styles.tierBadge}>
          Premium
        </Badge>
      );
    }
    if (tier.includes('Free')) {
      return (
        <Badge appearance="outline" color="success" className={styles.tierBadge}>
          {tier}
        </Badge>
      );
    }
    return (
      <Badge appearance="outline" className={styles.tierBadge}>
        {tier}
      </Badge>
    );
  };

  const getSummaryColor = (type: 'healthy' | 'degraded' | 'offline' | 'notConfigured') => {
    switch (type) {
      case 'healthy':
        return tokens.colorPaletteGreenForeground1;
      case 'degraded':
        return tokens.colorPaletteYellowForeground1;
      case 'offline':
        return tokens.colorPaletteRedForeground1;
      case 'notConfigured':
        return tokens.colorNeutralForeground3;
    }
  };

  const filteredProviders =
    dashboardData?.providers.filter((p) => {
      if (filter === 'all') return true;
      return p.category === filter;
    }) ?? [];

  const notConfiguredCount = dashboardData?.summary.notConfiguredProviders ?? 0;

  if (loading && !dashboardData) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner size="large" />
        <Text>Loading provider health...</Text>
      </div>
    );
  }

  const summary = dashboardData?.summary;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>Provider Health Dashboard</Title1>
          <Text>Monitor provider availability, health status, and quota information</Text>
        </div>
        <div className={styles.headerActions}>
          <Switch
            label="Auto-refresh (30s)"
            checked={autoRefresh}
            onChange={(_, data) => setAutoRefresh(data.checked)}
          />
          <Button
            appearance="primary"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRefresh}
            disabled={refreshing}
          >
            {refreshing ? 'Refreshing...' : 'Refresh'}
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className={styles.summarySection}>
          <Title3>Summary</Title3>
          <div className={styles.summaryCards}>
            <Card className={styles.summaryCard}>
              <div className={styles.summaryNumber}>{summary.totalProviders}</div>
              <Caption1 className={styles.summaryLabel}>Total Providers</Caption1>
            </Card>
            <Card className={styles.summaryCard}>
              <div className={styles.summaryNumber} style={{ color: getSummaryColor('healthy') }}>
                {summary.healthyProviders}
              </div>
              <Caption1 className={styles.summaryLabel}>Healthy</Caption1>
            </Card>
            <Card className={styles.summaryCard}>
              <div className={styles.summaryNumber} style={{ color: getSummaryColor('degraded') }}>
                {summary.degradedProviders}
              </div>
              <Caption1 className={styles.summaryLabel}>Degraded</Caption1>
            </Card>
            <Card className={styles.summaryCard}>
              <div className={styles.summaryNumber} style={{ color: getSummaryColor('offline') }}>
                {summary.offlineProviders}
              </div>
              <Caption1 className={styles.summaryLabel}>Offline</Caption1>
            </Card>
            <Card className={styles.summaryCard}>
              <div
                className={styles.summaryNumber}
                style={{ color: getSummaryColor('notConfigured') }}
              >
                {summary.notConfiguredProviders}
              </div>
              <Caption1 className={styles.summaryLabel}>Not Configured</Caption1>
            </Card>
          </div>
        </div>
      )}

      <Divider />

      {/* Warning banner for unconfigured providers */}
      {notConfiguredCount > 0 && (
        <div className={styles.notConfiguredBanner}>
          <Key24Regular />
          <div>
            <Text weight="semibold">
              {notConfiguredCount} provider{notConfiguredCount > 1 ? 's' : ''} need API keys
            </Text>
            <br />
            <Caption1>Configure API keys in Settings to enable these providers</Caption1>
          </div>
          <Button
            appearance="primary"
            size="small"
            icon={<Settings24Regular />}
            onClick={() => navigate('/settings')}
            style={{ marginLeft: 'auto' }}
          >
            Open Settings
          </Button>
        </div>
      )}

      {/* Filter buttons */}
      <div className={styles.filterButtons}>
        <Button
          appearance={filter === 'all' ? 'primary' : 'subtle'}
          onClick={() => setFilter('all')}
        >
          All ({dashboardData?.providers.length ?? 0})
        </Button>
        <Button
          appearance={filter === 'LLM' ? 'primary' : 'subtle'}
          onClick={() => setFilter('LLM')}
        >
          LLM ({dashboardData?.providers.filter((p) => p.category === 'LLM').length ?? 0})
        </Button>
        <Button
          appearance={filter === 'TTS' ? 'primary' : 'subtle'}
          onClick={() => setFilter('TTS')}
        >
          TTS ({dashboardData?.providers.filter((p) => p.category === 'TTS').length ?? 0})
        </Button>
        <Button
          appearance={filter === 'Image' ? 'primary' : 'subtle'}
          onClick={() => setFilter('Image')}
        >
          Image ({dashboardData?.providers.filter((p) => p.category === 'Image').length ?? 0})
        </Button>
      </div>

      {/* Provider Cards */}
      {filteredProviders.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={500}>No providers found</Text>
          <br />
          <Caption1>Providers will appear here once configured</Caption1>
        </div>
      ) : (
        <div className={styles.grid}>
          {filteredProviders.map((provider) => (
            <Card key={provider.name} className={styles.providerCard}>
              <div className={styles.cardHeader}>
                <div className={styles.providerInfo}>
                  <Text className={styles.providerName}>{provider.name}</Text>
                  <div className={styles.providerMeta}>
                    <Caption1>{provider.category}</Caption1>
                    {getTierBadge(provider.tier)}
                  </div>
                </div>
                {getStatusBadge(provider.healthStatus)}
              </div>

              {provider.healthStatus !== 'not_configured' && (
                <div className={styles.metrics}>
                  <div className={styles.metricRow}>
                    <Caption1 className={styles.metricLabel}>Success Rate</Caption1>
                    <Body1
                      style={{
                        color:
                          provider.successRate >= 95
                            ? tokens.colorPaletteGreenForeground1
                            : provider.successRate >= 80
                              ? tokens.colorPaletteYellowForeground1
                              : tokens.colorPaletteRedForeground1,
                      }}
                    >
                      {provider.successRate.toFixed(0)}%
                    </Body1>
                  </div>
                  <div className={styles.metricRow}>
                    <Caption1 className={styles.metricLabel}>
                      <Clock24Regular style={{ fontSize: '14px' }} /> Avg Latency
                    </Caption1>
                    <Body1>{provider.averageLatencyMs.toFixed(0)}ms</Body1>
                  </div>
                  {provider.consecutiveFailures > 0 && (
                    <div className={styles.metricRow}>
                      <Caption1 className={styles.metricLabel}>Consecutive Failures</Caption1>
                      <Body1 style={{ color: tokens.colorPaletteRedForeground1 }}>
                        {provider.consecutiveFailures}
                      </Body1>
                    </div>
                  )}
                  <div className={styles.metricRow}>
                    <Caption1 className={styles.metricLabel}>Circuit State</Caption1>
                    <Body1>{provider.circuitState}</Body1>
                  </div>
                </div>
              )}

              {/* Quota Information */}
              {provider.quotaInfo && (
                <div className={styles.quotaSection}>
                  <div className={styles.quotaHeader}>
                    <Gauge24Regular style={{ fontSize: '14px' }} />
                    Rate Limits
                  </div>
                  <Text className={styles.quotaText}>{provider.quotaInfo.description}</Text>
                  {provider.quotaInfo.remainingValue != null && (
                    <div
                      className={styles.metricRow}
                      style={{ marginTop: tokens.spacingVerticalS }}
                    >
                      <Caption1>Remaining</Caption1>
                      <Body1>
                        {provider.quotaInfo.remainingValue} / {provider.quotaInfo.limitValue}
                      </Body1>
                    </div>
                  )}
                </div>
              )}

              {/* Action Buttons */}
              <div className={styles.cardActions}>
                {provider.healthStatus === 'not_configured' && provider.requiresApiKey ? (
                  <Button
                    appearance="primary"
                    size="small"
                    icon={<Settings24Regular />}
                    onClick={() => handleConfigure(provider.name)}
                  >
                    Configure
                  </Button>
                ) : (
                  <>
                    <Tooltip content="Refresh this provider's status" relationship="label">
                      <Button appearance="subtle" size="small" icon={<ArrowClockwise24Regular />}>
                        Test
                      </Button>
                    </Tooltip>
                    {provider.healthStatus !== 'healthy' && provider.configureUrl && (
                      <Button
                        appearance="secondary"
                        size="small"
                        icon={<Settings24Regular />}
                        onClick={() => handleConfigure(provider.name)}
                      >
                        Configure
                      </Button>
                    )}
                  </>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Legend */}
      <div className={styles.legend}>
        <Text className={styles.legendTitle}>Status Legend</Text>
        <div className={styles.legendItems}>
          <div className={styles.legendItem}>
            <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
              Healthy
            </Badge>
            <Caption1>Provider is working normally</Caption1>
          </div>
          <div className={styles.legendItem}>
            <Badge appearance="filled" color="warning" icon={<Warning24Regular />}>
              Degraded
            </Badge>
            <Caption1>Some failures, still operational</Caption1>
          </div>
          <div className={styles.legendItem}>
            <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
              Offline
            </Badge>
            <Caption1>Provider is unavailable</Caption1>
          </div>
          <div className={styles.legendItem}>
            <Badge appearance="tint" color="warning" icon={<Key24Regular />}>
              Not Configured
            </Badge>
            <Caption1>API key required</Caption1>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProvidersPage;
