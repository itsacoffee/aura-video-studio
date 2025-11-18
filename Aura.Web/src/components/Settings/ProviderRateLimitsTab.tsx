import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Input,
  Switch,
  Field,
  Card,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { Save24Regular, Add24Regular, Shield24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type {
  ProviderRateLimits,
  ProviderRateLimit,
  RateLimitBehavior,
  LoadBalancingStrategy,
} from '../../types/settings';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  row: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  row3: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr 1fr',
    gap: tokens.spacingHorizontalL,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  providerCard: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
});

interface ProviderRateLimitsTabProps {
  settings: ProviderRateLimits;
  onChange: (settings: ProviderRateLimits) => void;
  onSave: () => void;
  hasChanges: boolean;
}

const DEFAULT_PROVIDERS = [
  'OpenAI',
  'Anthropic',
  'AzureOpenAI',
  'Gemini',
  'ElevenLabs',
  'StableDiffusion',
  'Ollama',
];

export function ProviderRateLimitsTab({
  settings,
  onChange,
  onSave,
  hasChanges,
}: ProviderRateLimitsTabProps) {
  const styles = useStyles();
  const [selectedProvider, setSelectedProvider] = useState<string>('');

  const updateGlobal = (updates: Partial<typeof settings.global>) => {
    onChange({
      ...settings,
      global: { ...settings.global, ...updates },
    });
  };

  const addProviderLimit = (providerName: string) => {
    const newLimit: ProviderRateLimit = {
      providerName,
      enabled: true,
      maxRequestsPerMinute: 60,
      maxRequestsPerHour: 1000,
      maxRequestsPerDay: 10000,
      maxConcurrentRequests: 5,
      maxTokensPerRequest: 4096,
      maxTokensPerMinute: 90000,
      dailyCostLimit: 10,
      monthlyCostLimit: 100,
      exceededBehavior: 'Queue' as RateLimitBehavior,
      priority: 50,
      retryDelayMs: 1000,
      maxRetries: 3,
      useExponentialBackoff: true,
      costWarningThreshold: 80,
      notifyOnLimitReached: true,
    };

    onChange({
      ...settings,
      limits: {
        ...settings.limits,
        [providerName]: newLimit,
      },
    });
    setSelectedProvider('');
  };

  const updateProviderLimit = (providerName: string, updates: Partial<ProviderRateLimit>) => {
    onChange({
      ...settings,
      limits: {
        ...settings.limits,
        [providerName]: {
          ...settings.limits[providerName],
          ...updates,
        },
      },
    });
  };

  const removeProviderLimit = (providerName: string) => {
    const { [providerName]: removed, ...remaining } = settings.limits;
    onChange({
      ...settings,
      limits: remaining,
    });
  };

  const availableProviders = DEFAULT_PROVIDERS.filter(
    (p) => !Object.keys(settings.limits).includes(p)
  );

  return (
    <>
      {/* Info Box */}
      <Card className={styles.infoBox}>
        <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, alignItems: 'center' }}>
          <Shield24Regular style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }} />
          <div>
            <Title3>Rate Limiting & Cost Management</Title3>
            <Text size={200}>
              Configure rate limits and cost controls to manage API usage, prevent overages, and
              ensure reliable service operation with automatic fallback and circuit breaker
              protection.
            </Text>
          </div>
        </div>
      </Card>

      {/* Global Settings */}
      <Card className={styles.section}>
        <Title2>Global Rate Limit Settings</Title2>
        <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
          Settings that apply across all providers
        </Text>

        <div className={styles.form}>
          <Field label="Enable Global Rate Limiting">
            <Switch
              checked={settings.global.enabled}
              onChange={(_, data) => updateGlobal({ enabled: data.checked })}
            />
            <Text size={200}>Master switch for all rate limiting functionality</Text>
          </Field>

          <Field label="Maximum Total Requests Per Minute">
            <Input
              type="number"
              value={settings.global.maxTotalRequestsPerMinute.toString()}
              onChange={(e) =>
                updateGlobal({ maxTotalRequestsPerMinute: parseInt(e.target.value) || 0 })
              }
            />
            <Text size={200}>Total API calls across all providers per minute (0 = unlimited)</Text>
          </Field>

          <div className={styles.row}>
            <Field label="Maximum Daily Cost (USD)">
              <Input
                type="number"
                step="0.01"
                value={settings.global.maxTotalDailyCost.toString()}
                onChange={(e) =>
                  updateGlobal({ maxTotalDailyCost: parseFloat(e.target.value) || 0 })
                }
              />
              <Text size={200}>Daily spending limit across all providers</Text>
            </Field>

            <Field label="Maximum Monthly Cost (USD)">
              <Input
                type="number"
                step="0.01"
                value={settings.global.maxTotalMonthlyCost.toString()}
                onChange={(e) =>
                  updateGlobal({ maxTotalMonthlyCost: parseFloat(e.target.value) || 0 })
                }
              />
              <Text size={200}>Monthly spending limit across all providers</Text>
            </Field>
          </div>

          <Field label="Global Exceeded Behavior">
            <Dropdown
              value={settings.global.globalExceededBehavior}
              onOptionSelect={(_, data) =>
                updateGlobal({ globalExceededBehavior: data.optionValue as RateLimitBehavior })
              }
            >
              <Option value="Block">Block - Reject requests immediately</Option>
              <Option value="Queue">Queue - Retry requests later</Option>
              <Option value="Fallback">Fallback - Use alternative provider</Option>
              <Option value="Warn">Warn - Allow but notify user</Option>
            </Dropdown>
          </Field>

          <Field label="Enable Circuit Breaker">
            <Switch
              checked={settings.global.enableCircuitBreaker}
              onChange={(_, data) => updateGlobal({ enableCircuitBreaker: data.checked })}
            />
            <Text size={200}>
              Automatically disable failing providers to prevent cascading failures
            </Text>
          </Field>

          {settings.global.enableCircuitBreaker && (
            <div className={styles.row}>
              <Field label="Circuit Breaker Threshold">
                <Input
                  type="number"
                  value={settings.global.circuitBreakerThreshold.toString()}
                  onChange={(e) =>
                    updateGlobal({ circuitBreakerThreshold: parseInt(e.target.value) || 5 })
                  }
                />
                <Text size={200}>Consecutive failures before opening circuit</Text>
              </Field>

              <Field label="Circuit Timeout (seconds)">
                <Input
                  type="number"
                  value={settings.global.circuitBreakerTimeoutSeconds.toString()}
                  onChange={(e) =>
                    updateGlobal({ circuitBreakerTimeoutSeconds: parseInt(e.target.value) || 60 })
                  }
                />
                <Text size={200}>Time before retrying a failed provider</Text>
              </Field>
            </div>
          )}

          <Field label="Enable Load Balancing">
            <Switch
              checked={settings.global.enableLoadBalancing}
              onChange={(_, data) => updateGlobal({ enableLoadBalancing: data.checked })}
            />
            <Text size={200}>Distribute requests across providers intelligently</Text>
          </Field>

          {settings.global.enableLoadBalancing && (
            <Field label="Load Balancing Strategy">
              <Dropdown
                value={settings.global.loadBalancingStrategy}
                onOptionSelect={(_, data) =>
                  updateGlobal({ loadBalancingStrategy: data.optionValue as LoadBalancingStrategy })
                }
              >
                <Option value="RoundRobin">Round Robin - Distribute evenly</Option>
                <Option value="LeastLoaded">Least Loaded - Use least busy provider</Option>
                <Option value="LeastCost">Least Cost - Minimize costs</Option>
                <Option value="LowestLatency">Lowest Latency - Fastest response</Option>
                <Option value="Priority">Priority - Based on provider priority</Option>
                <Option value="Random">Random - Random selection</Option>
              </Dropdown>
            </Field>
          )}
        </div>
      </Card>

      {/* Provider-Specific Limits */}
      <Card className={styles.section}>
        <div className={styles.cardHeader}>
          <div>
            <Title2>Provider-Specific Rate Limits</Title2>
            <Text size={200}>Configure individual limits for each provider</Text>
          </div>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS, alignItems: 'center' }}>
            {availableProviders.length > 0 && (
              <>
                <Dropdown
                  placeholder="Select provider..."
                  value={selectedProvider}
                  onOptionSelect={(_, data) => setSelectedProvider(data.optionValue as string)}
                  style={{ minWidth: '200px' }}
                >
                  {availableProviders.map((provider) => (
                    <Option key={provider} value={provider}>
                      {provider}
                    </Option>
                  ))}
                </Dropdown>
                <Button
                  appearance="primary"
                  icon={<Add24Regular />}
                  onClick={() => selectedProvider && addProviderLimit(selectedProvider)}
                  disabled={!selectedProvider}
                >
                  Add
                </Button>
              </>
            )}
          </div>
        </div>

        {Object.keys(settings.limits).length === 0 ? (
          <Text>No provider-specific limits configured. Add one to get started.</Text>
        ) : (
          Object.entries(settings.limits).map(([providerName, limit]) => (
            <Card key={providerName} className={styles.providerCard}>
              <div className={styles.cardHeader}>
                <Title3>{providerName}</Title3>
                <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
                  <Switch
                    checked={limit.enabled}
                    onChange={(_, data) =>
                      updateProviderLimit(providerName, { enabled: data.checked })
                    }
                  />
                  <Button appearance="subtle" onClick={() => removeProviderLimit(providerName)}>
                    Remove
                  </Button>
                </div>
              </div>

              <div className={styles.form}>
                <div className={styles.row3}>
                  <Field label="Max Requests/Minute">
                    <Input
                      type="number"
                      value={limit.maxRequestsPerMinute.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          maxRequestsPerMinute: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                  <Field label="Max Requests/Hour">
                    <Input
                      type="number"
                      value={limit.maxRequestsPerHour.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          maxRequestsPerHour: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                  <Field label="Max Requests/Day">
                    <Input
                      type="number"
                      value={limit.maxRequestsPerDay.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          maxRequestsPerDay: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                </div>

                <div className={styles.row}>
                  <Field label="Max Concurrent Requests">
                    <Input
                      type="number"
                      value={limit.maxConcurrentRequests.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          maxConcurrentRequests: parseInt(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                  <Field label="Priority (0-100)">
                    <Input
                      type="number"
                      value={limit.priority.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          priority: parseInt(e.target.value) || 50,
                        })
                      }
                    />
                  </Field>
                </div>

                <div className={styles.row}>
                  <Field label="Daily Cost Limit (USD)">
                    <Input
                      type="number"
                      step="0.01"
                      value={limit.dailyCostLimit.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          dailyCostLimit: parseFloat(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                  <Field label="Monthly Cost Limit (USD)">
                    <Input
                      type="number"
                      step="0.01"
                      value={limit.monthlyCostLimit.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          monthlyCostLimit: parseFloat(e.target.value) || 0,
                        })
                      }
                    />
                  </Field>
                </div>

                <Field label="Exceeded Behavior">
                  <Dropdown
                    value={limit.exceededBehavior}
                    onOptionSelect={(_, data) =>
                      updateProviderLimit(providerName, {
                        exceededBehavior: data.optionValue as RateLimitBehavior,
                      })
                    }
                  >
                    <Option value="Block">Block</Option>
                    <Option value="Queue">Queue</Option>
                    <Option value="Fallback">Fallback</Option>
                    <Option value="Warn">Warn</Option>
                  </Dropdown>
                </Field>

                {limit.exceededBehavior === 'Fallback' && (
                  <Field label="Fallback Provider">
                    <Input
                      value={limit.fallbackProvider || ''}
                      onChange={(e) =>
                        updateProviderLimit(providerName, { fallbackProvider: e.target.value })
                      }
                      placeholder="Enter fallback provider name"
                    />
                  </Field>
                )}

                <div className={styles.row}>
                  <Field label="Retry Delay (ms)">
                    <Input
                      type="number"
                      value={limit.retryDelayMs.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          retryDelayMs: parseInt(e.target.value) || 1000,
                        })
                      }
                    />
                  </Field>
                  <Field label="Max Retries">
                    <Input
                      type="number"
                      value={limit.maxRetries.toString()}
                      onChange={(e) =>
                        updateProviderLimit(providerName, {
                          maxRetries: parseInt(e.target.value) || 3,
                        })
                      }
                    />
                  </Field>
                </div>

                <Field label="Use Exponential Backoff">
                  <Switch
                    checked={limit.useExponentialBackoff}
                    onChange={(_, data) =>
                      updateProviderLimit(providerName, { useExponentialBackoff: data.checked })
                    }
                  />
                  <Text size={200}>Increase delay exponentially with each retry</Text>
                </Field>

                <Field label="Cost Warning Threshold (%)">
                  <Input
                    type="number"
                    min="0"
                    max="100"
                    value={limit.costWarningThreshold.toString()}
                    onChange={(e) =>
                      updateProviderLimit(providerName, {
                        costWarningThreshold: parseInt(e.target.value) || 80,
                      })
                    }
                  />
                  <Text size={200}>Warn when cost reaches this percentage of limit</Text>
                </Field>

                <Field label="Notify on Limit Reached">
                  <Switch
                    checked={limit.notifyOnLimitReached}
                    onChange={(_, data) =>
                      updateProviderLimit(providerName, { notifyOnLimitReached: data.checked })
                    }
                  />
                </Field>
              </div>
            </Card>
          ))
        )}
      </Card>

      <div className={styles.actions}>
        <Button
          appearance="primary"
          icon={<Save24Regular />}
          onClick={onSave}
          disabled={!hasChanges}
        >
          Save Rate Limits
        </Button>
      </div>
    </>
  );
}
