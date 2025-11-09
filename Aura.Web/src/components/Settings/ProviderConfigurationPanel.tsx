/**
 * Provider Configuration Panel
 * Comprehensive UI for configuring all providers with API keys, priorities, and cost limits
 */

import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Input,
  Switch,
  Card,
  Field,
  Spinner,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Tooltip,
} from '@fluentui/react-components';
import {
  Save24Regular,
  Checkmark24Filled,
  Dismiss24Regular,
  ArrowUp24Regular,
  ArrowDown24Regular,
  Eye24Regular,
  EyeOff24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import apiClient from '../../services/api/apiClient';
import { useProviderConfigStore } from '../../state/providerConfig';
import type { ProviderConfigDto, SaveProviderConfigRequest } from '../../types/api-v1';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  providerList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  providerCard: {
    padding: tokens.spacingVerticalM,
  },
  providerHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  providerInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  providerFields: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  fieldRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  priorityControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  testButton: {
    minWidth: '100px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  badge: {
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusMedium,
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  enabledBadge: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    color: tokens.colorPaletteGreenForeground2,
  },
  disabledBadge: {
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
  },
});

interface ProviderConfigurationPanelProps {
  onSave?: () => void;
}

export function ProviderConfigurationPanel({ onSave }: ProviderConfigurationPanelProps) {
  const styles = useStyles();
  const {
    providers,
    isLoading,
    isSaving,
    error,
    setProviders,
    setIsLoading,
    setIsSaving,
    setError,
    updateProvider,
    reorderProviders,
  } = useProviderConfigStore();

  const [testingProvider, setTestingProvider] = useState<string | null>(null);
  const [testResults, setTestResults] = useState<
    Record<string, { success: boolean; message: string; responseTimeMs?: number }>
  >({});
  const [showApiKeys, setShowApiKeys] = useState<Record<string, boolean>>({});

  useEffect(() => {
    loadProviders();
  }, []);

  const loadProviders = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await apiClient.get<SaveProviderConfigRequest>(
        '/api/providerconfiguration/providers'
      );
      setProviders(response.data.providers);
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError('Failed to load provider configuration: ' + error.message);
    } finally {
      setIsLoading(false);
    }
  };

  const saveProviders = async () => {
    setIsSaving(true);
    setError(null);
    try {
      const request: SaveProviderConfigRequest = { providers };
      await apiClient.post('/api/providerconfiguration/providers', request);
      setError(null);
      onSave?.();
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError('Failed to save provider configuration: ' + error.message);
    } finally {
      setIsSaving(false);
    }
  };

  const testConnection = async (provider: ProviderConfigDto) => {
    if (!provider.apiKey) {
      setTestResults({
        ...testResults,
        [provider.name]: { success: false, message: 'API key is required', responseTimeMs: 0 },
      });
      return;
    }

    setTestingProvider(provider.name);
    const startTime = Date.now();

    try {
      const response = await apiClient.post('/api/providers/test-connection', {
        providerName: provider.name,
        apiKey: provider.apiKey,
      });

      const result = response.data as {
        success: boolean;
        message?: string;
        responseTimeMs?: number;
        details?: Record<string, unknown>;
      };

      const responseTime = result.responseTimeMs || Date.now() - startTime;

      setTestResults({
        ...testResults,
        [provider.name]: {
          success: result.success,
          message:
            result.message ||
            (result.success ? `Connection successful (${responseTime}ms)` : 'Connection failed'),
          responseTimeMs: responseTime,
        },
      });
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error(String(err));
      const responseTime = Date.now() - startTime;

      setTestResults({
        ...testResults,
        [provider.name]: {
          success: false,
          message: error.message || 'Connection test failed',
          responseTimeMs: responseTime,
        },
      });
    } finally {
      setTestingProvider(null);
    }
  };

  const moveProvider = (index: number, direction: 'up' | 'down') => {
    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex >= 0 && newIndex < providers.length) {
      reorderProviders(index, newIndex);
    }
  };

  const toggleShowApiKey = (providerName: string) => {
    setShowApiKeys({
      ...showApiKeys,
      [providerName]: !showApiKeys[providerName],
    });
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading provider configuration..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title2>Provider Configuration</Title2>
          <Text>Configure API keys, priorities, and cost limits for all providers</Text>
        </div>
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={loadProviders} disabled={isLoading || isSaving}>
            Reload
          </Button>
          <Button
            appearance="primary"
            icon={<Save24Regular />}
            onClick={saveProviders}
            disabled={isSaving}
          >
            {isSaving ? 'Saving...' : 'Save Configuration'}
          </Button>
        </div>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Error</MessageBarTitle>
            {error}
          </MessageBarBody>
        </MessageBar>
      )}

      <div className={styles.providerList}>
        {providers
          .sort((a, b) => a.priority - b.priority)
          .map((provider, index) => (
            <Card key={provider.name} className={styles.providerCard}>
              <div className={styles.providerHeader}>
                <div className={styles.providerInfo}>
                  <Title3>{provider.name}</Title3>
                  <span
                    className={`${styles.badge} ${provider.enabled ? styles.enabledBadge : styles.disabledBadge}`}
                  >
                    {provider.enabled ? 'Enabled' : 'Disabled'}
                  </span>
                  <Text size={200}>Type: {provider.type}</Text>
                  <Text size={200}>Priority: {provider.priority}</Text>
                </div>
                <div className={styles.priorityControls}>
                  <Tooltip content="Move up" relationship="label">
                    <Button
                      appearance="subtle"
                      icon={<ArrowUp24Regular />}
                      disabled={index === 0}
                      onClick={() => moveProvider(index, 'up')}
                    />
                  </Tooltip>
                  <Tooltip content="Move down" relationship="label">
                    <Button
                      appearance="subtle"
                      icon={<ArrowDown24Regular />}
                      disabled={index === providers.length - 1}
                      onClick={() => moveProvider(index, 'down')}
                    />
                  </Tooltip>
                </div>
              </div>

              <div className={styles.providerFields}>
                <Field label="Enabled">
                  <Switch
                    checked={provider.enabled}
                    onChange={(_, data) => updateProvider(provider.name, { enabled: data.checked })}
                  />
                </Field>

                <div className={styles.fieldRow}>
                  <Field label="API Key" style={{ flex: 1 }}>
                    <Input
                      type={showApiKeys[provider.name] ? 'text' : 'password'}
                      value={provider.apiKey || ''}
                      onChange={(_, data) => updateProvider(provider.name, { apiKey: data.value })}
                      placeholder="Enter API key"
                      contentAfter={
                        <Button
                          appearance="transparent"
                          icon={showApiKeys[provider.name] ? <EyeOff24Regular /> : <Eye24Regular />}
                          onClick={() => toggleShowApiKey(provider.name)}
                        />
                      }
                    />
                  </Field>
                  <Button
                    appearance="secondary"
                    className={styles.testButton}
                    onClick={() => testConnection(provider)}
                    disabled={!provider.apiKey || testingProvider === provider.name}
                    icon={
                      testingProvider === provider.name ? (
                        <Spinner size="tiny" />
                      ) : testResults[provider.name]?.success ? (
                        <Checkmark24Filled />
                      ) : testResults[provider.name] ? (
                        <Dismiss24Regular />
                      ) : undefined
                    }
                  >
                    {testingProvider === provider.name ? 'Testing...' : 'Test Connection'}
                  </Button>
                </div>

                {testResults[provider.name] && (
                  <MessageBar intent={testResults[provider.name].success ? 'success' : 'error'}>
                    <MessageBarBody>{testResults[provider.name].message}</MessageBarBody>
                  </MessageBar>
                )}

                <Field label="Cost Limit (USD/month)" hint="Leave empty for no limit">
                  <Input
                    type="number"
                    value={provider.costLimit?.toString() || ''}
                    onChange={(_, data) =>
                      updateProvider(provider.name, {
                        costLimit: data.value ? parseFloat(data.value) : null,
                      })
                    }
                    placeholder="No limit"
                  />
                </Field>
              </div>
            </Card>
          ))}
      </div>
    </div>
  );
}

export default ProviderConfigurationPanel;
