import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
  Tooltip,
  Caption1,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Warning24Regular,
  Clock24Regular,
  ArrowSync24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { apiClient } from '@/services/api/apiClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  keyCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  keyCard: {
    padding: tokens.spacingVerticalM,
  },
  keyCardHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  keyCardTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  keyCardBody: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  statusRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButton: {
    marginTop: tokens.spacingVerticalS,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface KeyStatus {
  success: boolean;
  provider: string;
  status: string;
  message: string;
  lastValidated?: string;
  validationStarted?: string;
  elapsedMs: number;
  remainingTimeoutMs: number;
  details: Record<string, string>;
  canRetry: boolean;
  canManuallyRevalidate: boolean;
}

interface AllKeysStatusResponse {
  success: boolean;
  statuses: Record<string, KeyStatus>;
  totalKeys: number;
  validKeys: number;
  invalidKeys: number;
  pendingValidation: number;
}

const getStatusIcon = (status: string) => {
  switch (status) {
    case 'Valid':
      return <CheckmarkCircle24Regular />;
    case 'Invalid':
    case 'TimedOut':
      return <ErrorCircle24Regular />;
    case 'Validating':
    case 'ValidatingExtended':
    case 'ValidatingMaxWait':
      return <Clock24Regular />;
    case 'SlowButWorking':
      return <Warning24Regular />;
    default:
      return <Clock24Regular />;
  }
};

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'Valid':
      return <Badge appearance="filled" color="success">Valid</Badge>;
    case 'Invalid':
      return <Badge appearance="filled" color="danger">Invalid</Badge>;
    case 'TimedOut':
      return <Badge appearance="filled" color="danger">Timed Out</Badge>;
    case 'Validating':
      return <Badge appearance="outline" color="informative">Validating</Badge>;
    case 'ValidatingExtended':
      return <Badge appearance="outline" color="warning">Extended Wait</Badge>;
    case 'ValidatingMaxWait':
      return <Badge appearance="outline" color="severe">Max Wait</Badge>;
    case 'SlowButWorking':
      return <Badge appearance="filled" color="warning">Slow</Badge>;
    case 'NotValidated':
    default:
      return <Badge appearance="ghost">Not Validated</Badge>;
  }
};

const getStatusTooltip = (status: string, elapsedMs: number, remainingTimeoutMs: number): string => {
  switch (status) {
    case 'Valid':
      return 'API key is valid and working correctly.';
    case 'Invalid':
      return 'API key validation failed. Please check the key and try again.';
    case 'TimedOut':
      return 'Validation timed out. The provider may be down or unreachable.';
    case 'Validating':
      return `Validation in progress (${Math.round(elapsedMs / 1000)}s elapsed). Please wait...`;
    case 'ValidatingExtended':
      return `Validation taking longer than usual (${Math.round(elapsedMs / 1000)}s elapsed). This is normal for some providers. Please be patient.`;
    case 'ValidatingMaxWait':
      return `Validation taking very long (${Math.round(elapsedMs / 1000)}s elapsed, ${Math.round(remainingTimeoutMs / 1000)}s remaining). You can manually retry or wait for completion.`;
    case 'SlowButWorking':
      return 'Provider is slow but responding. Consider checking network connectivity.';
    case 'NotValidated':
    default:
      return 'API key has not been validated yet. Click "Revalidate" to test the connection.';
  }
};

export const KeyStatusPanel = () => {
  const styles = useStyles();
  const [allStatuses, setAllStatuses] = useState<AllKeysStatusResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [revalidating, setRevalidating] = useState<Set<string>>(new Set());

  const fetchKeyStatuses = useCallback(async () => {
    try {
      setLoading(true);
      const response = await apiClient.get<AllKeysStatusResponse>('/api/keys/status');
      setAllStatuses(response.data);
    } catch (error) {
      console.error('Failed to fetch key statuses:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchKeyStatuses();
    
    // Poll for status updates every 5 seconds when there are active validations
    const interval = setInterval(() => {
      if (allStatuses && Object.values(allStatuses.statuses).some(s => 
        ['Validating', 'ValidatingExtended', 'ValidatingMaxWait'].includes(s.status)
      )) {
        fetchKeyStatuses();
      }
    }, 5000);

    return () => clearInterval(interval);
  }, [fetchKeyStatuses, allStatuses]);

  const handleRevalidate = async (provider: string) => {
    try {
      setRevalidating(prev => new Set(prev).add(provider));
      
      await apiClient.post('/api/keys/revalidate', {
        provider,
      });

      // Refresh statuses after a short delay to get updated status
      setTimeout(() => {
        fetchKeyStatuses();
        setRevalidating(prev => {
          const next = new Set(prev);
          next.delete(provider);
          return next;
        });
      }, 2000);
    } catch (error) {
      console.error(`Failed to revalidate key for ${provider}:`, error);
      setRevalidating(prev => {
        const next = new Set(prev);
        next.delete(provider);
        return next;
      });
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading key status..." />
      </div>
    );
  }

  if (!allStatuses || allStatuses.totalKeys === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.emptyState}>
          <Text>No API keys configured yet.</Text>
          <Caption1>Configure API keys in the Providers tab to see their status here.</Caption1>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>API Key Status</Title3>
        <Button
          appearance="subtle"
          icon={<ArrowSync24Regular />}
          onClick={fetchKeyStatuses}
        >
          Refresh All
        </Button>
      </div>

      <div className={styles.keyCards}>
        {Object.entries(allStatuses.statuses).map(([provider, status]) => (
          <Card key={provider} className={styles.keyCard}>
            <div className={styles.keyCardHeader}>
              <div className={styles.keyCardTitle}>
                {getStatusIcon(status.status)}
                <Text weight="semibold">{provider}</Text>
              </div>
              {getStatusBadge(status.status)}
            </div>

            <div className={styles.keyCardBody}>
              <div className={styles.statusRow}>
                <Tooltip
                  content={getStatusTooltip(
                    status.status,
                    status.elapsedMs,
                    status.remainingTimeoutMs
                  )}
                  relationship="description"
                >
                  <Text size={300}>{status.message}</Text>
                </Tooltip>
              </div>

              {status.lastValidated && (
                <Caption1>
                  Last validated: {new Date(status.lastValidated).toLocaleString()}
                </Caption1>
              )}

              {status.elapsedMs > 0 && (
                <Caption1>
                  Elapsed: {Math.round(status.elapsedMs / 1000)}s
                  {status.remainingTimeoutMs > 0 && 
                    ` | Remaining: ${Math.round(status.remainingTimeoutMs / 1000)}s`}
                </Caption1>
              )}

              {status.canManuallyRevalidate && (
                <Button
                  className={styles.actionButton}
                  appearance="primary"
                  size="small"
                  icon={<ArrowSync24Regular />}
                  disabled={revalidating.has(provider)}
                  onClick={() => handleRevalidate(provider)}
                >
                  {revalidating.has(provider) ? 'Revalidating...' : 'Revalidate'}
                </Button>
              )}
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};
