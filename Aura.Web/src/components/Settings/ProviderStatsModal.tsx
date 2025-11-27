/**
 * ProviderStatsModal - Modal component for displaying AI provider usage statistics
 *
 * Shows detailed statistics including request counts, success rates,
 * latency, token usage, and cost estimates for a specific provider.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Button,
  makeStyles,
  tokens,
  Spinner,
  Text,
} from '@fluentui/react-components';
import { useCallback, useEffect, useState, type FC } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  statsContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  statRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: `${tokens.spacingVerticalXS} 0`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  statLabel: {
    fontWeight: 600,
  },
  statValue: {
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    marginTop: tokens.spacingVerticalL,
    display: 'flex',
    justifyContent: 'flex-end',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    textAlign: 'center',
  },
});

export interface ProviderStats {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  averageLatency: number;
  totalTokensUsed: number;
  totalCost: number;
  lastUsed: string;
}

interface ProviderStatsModalProps {
  providerId: string;
  onClose: () => void;
}

export const ProviderStatsModal: FC<ProviderStatsModalProps> = ({ providerId, onClose }) => {
  const styles = useStyles();
  const [stats, setStats] = useState<ProviderStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadStats = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(apiUrl(`/api/providers/${providerId}/stats`));
      if (!response.ok) {
        throw new Error(`Failed to load stats: ${response.statusText}`);
      }
      const data = await response.json();
      setStats(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load provider stats';
      setError(errorMessage);
      console.error('Failed to load provider stats:', err);
    } finally {
      setLoading(false);
    }
  }, [providerId]);

  useEffect(() => {
    loadStats();
  }, [loadStats]);

  const getSuccessRate = (): string => {
    if (!stats || stats.totalRequests === 0) return '0%';
    return `${Math.round((stats.successfulRequests / stats.totalRequests) * 100)}%`;
  };

  const formatDate = (dateString: string): string => {
    try {
      return new Date(dateString).toLocaleString();
    } catch {
      return 'Unknown';
    }
  };

  return (
    <Dialog
      open={true}
      onOpenChange={(_, data) => {
        if (!data.open) onClose();
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Provider Usage Statistics</DialogTitle>
          <DialogContent>
            {loading ? (
              <div className={styles.loadingContainer}>
                <Spinner label="Loading statistics..." />
              </div>
            ) : error ? (
              <Text className={styles.errorText}>{error}</Text>
            ) : stats ? (
              <div className={styles.statsContainer}>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Total Requests:</Text>
                  <Text className={styles.statValue}>{stats.totalRequests.toLocaleString()}</Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Successful:</Text>
                  <Text className={styles.statValue}>
                    {stats.successfulRequests.toLocaleString()} ({getSuccessRate()})
                  </Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Failed:</Text>
                  <Text className={styles.statValue}>{stats.failedRequests.toLocaleString()}</Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Average Latency:</Text>
                  <Text className={styles.statValue}>{stats.averageLatency}ms</Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Total Tokens Used:</Text>
                  <Text className={styles.statValue}>{stats.totalTokensUsed.toLocaleString()}</Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Estimated Cost:</Text>
                  <Text className={styles.statValue}>${stats.totalCost.toFixed(2)}</Text>
                </div>
                <div className={styles.statRow}>
                  <Text className={styles.statLabel}>Last Used:</Text>
                  <Text className={styles.statValue}>{formatDate(stats.lastUsed)}</Text>
                </div>
              </div>
            ) : (
              <Text>No statistics available</Text>
            )}
            <div className={styles.actions}>
              <Button appearance="primary" onClick={onClose}>
                Close
              </Button>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default ProviderStatsModal;
