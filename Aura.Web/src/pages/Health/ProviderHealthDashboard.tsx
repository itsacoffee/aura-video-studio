import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';
import {
  makeStyles,
  tokens,
  Title1,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
  Switch,
  Caption1,
  Body1,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  CheckmarkCircle24Regular,
  Warning24Regular,
  ErrorCircle24Regular,
  ChevronDown24Regular,
  ChevronUp24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
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
  filterButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
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
  providerName: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  providerType: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
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
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface ProviderHealth {
  providerName: string;
  isHealthy: boolean;
  lastCheckTime: string;
  responseTimeMs: number;
  consecutiveFailures: number;
  lastError?: string;
  successRate: number;
  averageResponseTimeMs: number;
}

type ProviderFilter = 'all' | 'llm' | 'tts' | 'image';

export function ProviderHealthDashboard() {
  const styles = useStyles();
  const [providers, setProviders] = useState<ProviderHealth[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [filter, setFilter] = useState<ProviderFilter>('all');
  const [expandedErrors, setExpandedErrors] = useState<Set<string>>(new Set());

  useEffect(() => {
    loadProviders();
  }, []);

  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      loadProviders(true);
    }, 10000); // 10 seconds

    return () => clearInterval(interval);
  }, [autoRefresh]);

  const loadProviders = async (silent = false) => {
    if (!silent) setLoading(true);
    try {
      const response = await fetch(`${apiUrl}/health/providers`);
      if (response.ok) {
        const data = await response.json();
        setProviders(data);
      }
    } catch (error) {
      console.error('Failed to load provider health:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRefreshAll = async () => {
    setRefreshing(true);
    try {
      const response = await fetch(`${apiUrl}/health/providers/check-all`, {
        method: 'POST',
      });
      if (response.ok) {
        const data = await response.json();
        setProviders(data);
      }
    } catch (error) {
      console.error('Failed to refresh providers:', error);
    } finally {
      setRefreshing(false);
    }
  };

  const handleTestConnection = async (providerName: string) => {
    try {
      const response = await fetch(`${apiUrl}/health/providers/${providerName}/check`, {
        method: 'POST',
      });
      if (response.ok) {
        await loadProviders(true);
      }
    } catch (error) {
      console.error('Failed to test connection:', error);
    }
  };

  const toggleErrorExpanded = (providerName: string) => {
    const newExpanded = new Set(expandedErrors);
    if (newExpanded.has(providerName)) {
      newExpanded.delete(providerName);
    } else {
      newExpanded.add(providerName);
    }
    setExpandedErrors(newExpanded);
  };

  const getProviderType = (name: string): string => {
    const nameLower = name.toLowerCase();
    if (
      nameLower.includes('llm') ||
      ['rulebased', 'ollama', 'openai', 'azure', 'gemini'].includes(nameLower)
    ) {
      return 'LLM';
    }
    if (
      nameLower.includes('tts') ||
      nameLower.includes('voice') ||
      ['windows', 'elevenlabs'].includes(nameLower)
    ) {
      return 'TTS';
    }
    if (nameLower.includes('image') || ['stablediffusion', 'stock'].includes(nameLower)) {
      return 'Image';
    }
    return 'Other';
  };

  const getStatusBadge = (provider: ProviderHealth) => {
    if (provider.consecutiveFailures >= 3) {
      return (
        <Badge appearance="filled" color="danger" icon={<ErrorCircle24Regular />}>
          Offline
        </Badge>
      );
    }
    if (!provider.isHealthy) {
      return (
        <Badge appearance="filled" color="warning" icon={<Warning24Regular />}>
          Degraded
        </Badge>
      );
    }
    return (
      <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
        Healthy
      </Badge>
    );
  };

  const getSuccessRateColor = (rate: number): string => {
    if (rate >= 0.95) return tokens.colorPaletteGreenForeground1;
    if (rate >= 0.8) return tokens.colorPaletteYellowForeground1;
    return tokens.colorPaletteRedForeground1;
  };

  const formatTimestamp = (timestamp: string): string => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return date.toLocaleDateString();
  };

  const filteredProviders = providers.filter((p) => {
    if (filter === 'all') return true;
    const type = getProviderType(p.providerName).toLowerCase();
    return type === filter;
  });

  if (loading && !providers.length) {
    return (
      <div className={styles.container}>
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Spinner label="Loading provider health..." />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>Provider Health Dashboard</Title1>
          <Text className={styles.metricLabel}>
            Real-time monitoring of provider availability and performance
          </Text>
        </div>
        <div className={styles.headerActions}>
          <Switch
            label="Auto-refresh"
            checked={autoRefresh}
            onChange={(_, data) => setAutoRefresh(data.checked)}
          />
          <Button
            appearance="primary"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRefreshAll}
            disabled={refreshing}
          >
            {refreshing ? 'Refreshing...' : 'Refresh All'}
          </Button>
        </div>
      </div>

      <div className={styles.filterButtons}>
        <Button
          appearance={filter === 'all' ? 'primary' : 'subtle'}
          onClick={() => setFilter('all')}
        >
          All ({providers.length})
        </Button>
        <Button
          appearance={filter === 'llm' ? 'primary' : 'subtle'}
          onClick={() => setFilter('llm')}
        >
          LLM ({providers.filter((p) => getProviderType(p.providerName) === 'LLM').length})
        </Button>
        <Button
          appearance={filter === 'tts' ? 'primary' : 'subtle'}
          onClick={() => setFilter('tts')}
        >
          TTS ({providers.filter((p) => getProviderType(p.providerName) === 'TTS').length})
        </Button>
        <Button
          appearance={filter === 'image' ? 'primary' : 'subtle'}
          onClick={() => setFilter('image')}
        >
          Image ({providers.filter((p) => getProviderType(p.providerName) === 'Image').length})
        </Button>
      </div>

      {filteredProviders.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={500}>No providers found</Text>
          <br />
          <Caption1>
            Providers will appear here once they are registered and health checks run
          </Caption1>
        </div>
      ) : (
        <div className={styles.grid}>
          {filteredProviders.map((provider) => (
            <Card key={provider.providerName} className={styles.providerCard}>
              <div className={styles.cardHeader}>
                <div>
                  <div className={styles.providerName}>{provider.providerName}</div>
                  <div className={styles.providerType}>
                    {getProviderType(provider.providerName)}
                  </div>
                </div>
                {getStatusBadge(provider)}
              </div>

              <div className={styles.metrics}>
                <div className={styles.metricRow}>
                  <Caption1 className={styles.metricLabel}>Last Check</Caption1>
                  <Body1>{formatTimestamp(provider.lastCheckTime)}</Body1>
                </div>

                <div className={styles.metricRow}>
                  <Caption1 className={styles.metricLabel}>Response Time</Caption1>
                  <Body1>{provider.responseTimeMs.toFixed(0)}ms</Body1>
                </div>

                <div className={styles.metricRow}>
                  <Caption1 className={styles.metricLabel}>Success Rate</Caption1>
                  <Body1 style={{ color: getSuccessRateColor(provider.successRate) }}>
                    {(provider.successRate * 100).toFixed(0)}%
                  </Body1>
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
                  <Caption1 className={styles.metricLabel}>Avg Response</Caption1>
                  <Body1>{provider.averageResponseTimeMs.toFixed(0)}ms</Body1>
                </div>
              </div>

              {provider.lastError && (
                <>
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={
                      expandedErrors.has(provider.providerName) ? (
                        <ChevronUp24Regular />
                      ) : (
                        <ChevronDown24Regular />
                      )
                    }
                    onClick={() => toggleErrorExpanded(provider.providerName)}
                  >
                    {expandedErrors.has(provider.providerName) ? 'Hide' : 'Show'} Error Details
                  </Button>
                  {expandedErrors.has(provider.providerName) && (
                    <div className={styles.errorDetails}>{provider.lastError}</div>
                  )}
                </>
              )}

              <Button
                appearance="secondary"
                onClick={() => handleTestConnection(provider.providerName)}
              >
                Test Connection
              </Button>
            </Card>
          ))}
        </div>
      )}

      <div className={styles.legend}>
        <Text className={styles.legendTitle}>Status Legend</Text>
        <div className={styles.legendItems}>
          <div className={styles.legendItem}>
            <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Regular />}>
              Healthy
            </Badge>
            <Caption1>All checks passing, &lt;3 failures</Caption1>
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
            <Caption1>3+ consecutive failures</Caption1>
          </div>
        </div>
      </div>
    </div>
  );
}
