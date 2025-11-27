/**
 * AIProviderSettings - Component for managing AI provider settings with context menu support
 *
 * Displays a list of AI providers with right-click context menu functionality
 * for testing connections, viewing stats, setting defaults, and configuring providers.
 */

import { makeStyles, tokens, Text, Title2, Card, Spinner } from '@fluentui/react-components';
import { useState, useCallback, useEffect, type FC } from 'react';
import { apiUrl } from '../../config/api';
import { useAIProviderContextMenu } from '../../hooks/useAIProviderContextMenu';
import { ProviderCard, type AIProvider, type TestResult } from './ProviderCard';
import { ProviderConfigModal } from './ProviderConfigModal';
import { ProviderStatsModal } from './ProviderStatsModal';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalM,
  },
  providersList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
  },
  errorContainer: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
    color: tokens.colorNeutralForeground3,
  },
  hint: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export const AIProviderSettings: FC = () => {
  const styles = useStyles();
  const [providers, setProviders] = useState<AIProvider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [testingProvider, setTestingProvider] = useState<string | null>(null);
  const [testResults, setTestResults] = useState<Map<string, TestResult>>(new Map());
  const [showStatsModal, setShowStatsModal] = useState(false);
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(null);
  const [showConfigModal, setShowConfigModal] = useState(false);

  const loadProviders = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(apiUrl('/api/providers'));
      if (!response.ok) {
        throw new Error(`Failed to load providers: ${response.statusText}`);
      }
      const data = await response.json();
      setProviders(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load providers';
      setError(errorMessage);
      console.error('Failed to load providers:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProviders();
  }, [loadProviders]);

  const handleTestConnection = useCallback(async (providerId: string) => {
    setTestingProvider(providerId);
    try {
      const response = await fetch(apiUrl(`/api/providers/${providerId}/test`), {
        method: 'POST',
      });
      const result = await response.json();

      setTestResults((prev) => {
        const newMap = new Map(prev);
        newMap.set(providerId, {
          success: result.success,
          message:
            result.message || (result.success ? 'Connection successful' : 'Connection failed'),
          latency: result.latency || result.responseTimeMs || 0,
          timestamp: Date.now(),
        });
        return newMap;
      });
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Connection test failed';
      setTestResults((prev) => {
        const newMap = new Map(prev);
        newMap.set(providerId, {
          success: false,
          message: errorMessage,
          latency: 0,
          timestamp: Date.now(),
        });
        return newMap;
      });
    } finally {
      setTestingProvider(null);
    }
  }, []);

  const handleViewStats = useCallback((providerId: string) => {
    setSelectedProviderId(providerId);
    setShowStatsModal(true);
  }, []);

  const handleSetDefault = useCallback(async (providerId: string) => {
    try {
      const response = await fetch(apiUrl(`/api/providers/${providerId}/set-default`), {
        method: 'POST',
      });
      if (response.ok) {
        setProviders((prev) =>
          prev.map((p) => ({
            ...p,
            isDefault: p.id === providerId,
          }))
        );
      } else {
        console.error('Failed to set default provider');
      }
    } catch (err: unknown) {
      console.error('Failed to set default provider:', err);
    }
  }, []);

  const handleConfigure = useCallback((providerId: string) => {
    setSelectedProviderId(providerId);
    setShowConfigModal(true);
  }, []);

  const handleProviderContextMenu = useAIProviderContextMenu(
    handleTestConnection,
    handleViewStats,
    handleSetDefault,
    handleConfigure
  );

  if (loading) {
    return (
      <Card className={styles.container}>
        <div className={styles.loadingContainer}>
          <Spinner label="Loading providers..." />
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={styles.container}>
        <div className={styles.errorContainer}>
          <Text className={styles.errorText}>{error}</Text>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Title2>AI Provider Settings</Title2>
        <Text className={styles.subtitle}>
          Manage AI providers for script generation, text-to-speech, and image creation. Right-click
          a provider for quick actions.
        </Text>
      </div>

      {providers.length === 0 ? (
        <div className={styles.emptyState}>
          <Text>
            No providers configured. Configure API keys in the API Keys tab to get started.
          </Text>
        </div>
      ) : (
        <div className={styles.providersList}>
          {providers.map((provider) => (
            <ProviderCard
              key={provider.id}
              provider={provider}
              isTesting={testingProvider === provider.id}
              testResult={testResults.get(provider.id)}
              onContextMenu={handleProviderContextMenu}
            />
          ))}
        </div>
      )}

      <div className={styles.hint}>
        <Text size={200}>
          💡 <strong>Tip:</strong> Right-click on any provider to test connections, view usage
          statistics, set as default, or configure settings.
        </Text>
      </div>

      {showStatsModal && selectedProviderId && (
        <ProviderStatsModal
          providerId={selectedProviderId}
          onClose={() => setShowStatsModal(false)}
        />
      )}

      {showConfigModal && selectedProviderId && (
        <ProviderConfigModal
          providerId={selectedProviderId}
          onClose={() => setShowConfigModal(false)}
          onSave={loadProviders}
        />
      )}
    </Card>
  );
};

export default AIProviderSettings;
