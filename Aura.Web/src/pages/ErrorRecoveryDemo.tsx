/**
 * Error Recovery Features Demo Page
 * Demonstrates error recovery capabilities
 */

import {
  Button,
  Card,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Text,
} from '@fluentui/react-components';
import {
  ErrorCircle24Regular,
  Warning24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { ErrorRecoveryModal } from '../components/ErrorBoundary/ErrorRecoveryModal';
import { useErrorRecovery } from '../hooks/useErrorRecovery';
import { useProviderFallback } from '../hooks/useProviderFallback';
import type { ErrorSeverity } from '../services/errorReportingService';
import { providerFallbackService, ProviderConfig } from '../services/providerFallbackService';

export function ErrorRecoveryDemo() {
  const [errorTitle, setErrorTitle] = useState('Test Error');
  const [errorMessage, setErrorMessage] = useState('This is a simulated error for testing');
  const [errorSeverity, setErrorSeverity] = useState<ErrorSeverity>('error');
  const [canRetry, setCanRetry] = useState(true);
  const [simulateRetrySuccess, setSimulateRetrySuccess] = useState(true);

  const { showErrorRecovery, hideErrorRecovery, isErrorRecoveryOpen, errorRecoveryOptions } =
    useErrorRecovery();

  const { currentProvider, availableProviders, isProviderHealthy, fallbackToNext, resetChain } =
    useProviderFallback('llm');

  useState(() => {
    const mockProviders: ProviderConfig[] = [
      {
        name: 'OpenAI GPT-4',
        type: 'llm',
        isAvailable: async () => Math.random() > 0.3,
        priority: 10,
        requiresApiKey: true,
      },
      {
        name: 'Anthropic Claude',
        type: 'llm',
        isAvailable: async () => Math.random() > 0.3,
        priority: 9,
        requiresApiKey: true,
      },
      {
        name: 'Google Gemini',
        type: 'llm',
        isAvailable: async () => Math.random() > 0.5,
        priority: 8,
        requiresApiKey: true,
      },
      {
        name: 'Ollama (Local)',
        type: 'llm',
        isAvailable: async () => true,
        priority: 5,
        isOffline: true,
      },
      {
        name: 'Rule-Based (Fallback)',
        type: 'llm',
        isAvailable: async () => true,
        priority: 1,
        isOffline: true,
      },
    ];

    providerFallbackService.registerFallbackChain('llm', mockProviders);
  });

  const handleShowErrorRecovery = () => {
    showErrorRecovery({
      title: errorTitle,
      message: errorMessage,
      severity: errorSeverity,
      canRetry,
      retryAction: simulateRetrySuccess
        ? async () => {
            await new Promise((resolve) => setTimeout(resolve, 1000));
          }
        : async () => {
            await new Promise((resolve) => setTimeout(resolve, 1000));
            throw new Error('Simulated retry failure');
          },
      suggestedActions: [
        {
          label: 'Check Settings',
          action: () => {
            alert('Opening settings...');
          },
        },
        {
          label: 'View Documentation',
          action: () => {
            window.open('https://github.com/Coffee285/aura-video-studio', '_blank');
          },
        },
      ],
      technicalDetails: `Error Details:\n${JSON.stringify(
        {
          timestamp: new Date().toISOString(),
          severity: errorSeverity,
          canRetry,
          stack: 'at ErrorRecoveryDemo (/src/pages/ErrorRecoveryDemo.tsx:42:15)',
        },
        null,
        2
      )}`,
    });
  };

  const handleFallbackToNext = async () => {
    const nextProvider = await fallbackToNext();
    if (nextProvider) {
      alert(`Switched to provider: ${nextProvider.name}`);
    } else {
      alert('No more providers available');
    }
  };

  const handleResetChain = () => {
    resetChain();
    alert('Provider chain reset to first provider');
  };

  return (
    <div style={{ padding: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ marginBottom: '24px' }}>
        <Text as="h1" size={900} weight="bold">
          Error Recovery Features Demo
        </Text>
        <Text as="p" size={400}>
          Test and demonstrate error recovery and provider fallback capabilities
        </Text>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '24px' }}>
        <Card>
          <div style={{ padding: '16px' }}>
            <Text as="h2" size={700} weight="semibold" style={{ marginBottom: '16px' }}>
              <ErrorCircle24Regular style={{ marginRight: '8px' }} />
              Error Recovery Modal
            </Text>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <Field label="Error Title">
                <Input
                  value={errorTitle}
                  onChange={(_, data) => setErrorTitle(data.value)}
                  placeholder="Enter error title"
                />
              </Field>

              <Field label="Error Message">
                <Input
                  value={errorMessage}
                  onChange={(_, data) => setErrorMessage(data.value)}
                  placeholder="Enter error message"
                />
              </Field>

              <Field label="Severity">
                <Select
                  value={errorSeverity}
                  onChange={(_, data) => setErrorSeverity(data.value as ErrorSeverity)}
                >
                  <option value="info">Info</option>
                  <option value="warning">Warning</option>
                  <option value="error">Error</option>
                  <option value="critical">Critical</option>
                </Select>
              </Field>

              <Field label="Retry Behavior">
                <Select
                  value={canRetry ? 'yes' : 'no'}
                  onChange={(_, data) => setCanRetry(data.value === 'yes')}
                >
                  <option value="yes">Can Retry</option>
                  <option value="no">Cannot Retry</option>
                </Select>
              </Field>

              {canRetry && (
                <Field label="Retry Result">
                  <Select
                    value={simulateRetrySuccess ? 'success' : 'failure'}
                    onChange={(_, data) => setSimulateRetrySuccess(data.value === 'success')}
                  >
                    <option value="success">Success</option>
                    <option value="failure">Failure</option>
                  </Select>
                </Field>
              )}

              <Button appearance="primary" onClick={handleShowErrorRecovery}>
                Show Error Recovery Modal
              </Button>
            </div>
          </div>
        </Card>

        <Card>
          <div style={{ padding: '16px' }}>
            <Text as="h2" size={700} weight="semibold" style={{ marginBottom: '16px' }}>
              <Warning24Regular style={{ marginRight: '8px' }} />
              Provider Fallback
            </Text>

            <MessageBar
              intent={isProviderHealthy ? 'success' : 'warning'}
              style={{ marginBottom: '16px' }}
            >
              <MessageBarBody>
                <Text weight="semibold">Current Provider: {currentProvider?.name || 'None'}</Text>
                <Text>Status: {isProviderHealthy ? 'Healthy' : 'Degraded'}</Text>
              </MessageBarBody>
            </MessageBar>

            <div style={{ marginBottom: '16px' }}>
              <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                Available Providers ({availableProviders.length}):
              </Text>
              <ul style={{ margin: '0', paddingLeft: '20px' }}>
                {availableProviders.map((provider) => (
                  <li key={provider.name}>
                    <Text>
                      {provider.name} (Priority: {provider.priority})
                      {provider.isOffline && ' - Offline Capable'}
                    </Text>
                  </li>
                ))}
              </ul>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <Button
                appearance="primary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleFallbackToNext}
              >
                Fallback to Next Provider
              </Button>
              <Button appearance="secondary" onClick={handleResetChain}>
                Reset Provider Chain
              </Button>
            </div>
          </div>
        </Card>
      </div>

      <Card style={{ marginTop: '24px' }}>
        <div style={{ padding: '16px' }}>
          <Text as="h2" size={700} weight="semibold" style={{ marginBottom: '16px' }}>
            Features Demonstrated
          </Text>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div>
              <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                Error Recovery Modal:
              </Text>
              <ul style={{ margin: '0', paddingLeft: '20px' }}>
                <li>User-friendly error messages</li>
                <li>Retry functionality with progress feedback</li>
                <li>Suggested recovery actions</li>
                <li>Technical details for developers</li>
                <li>Multiple severity levels</li>
              </ul>
            </div>

            <div>
              <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                Provider Fallback:
              </Text>
              <ul style={{ margin: '0', paddingLeft: '20px' }}>
                <li>Automatic provider switching</li>
                <li>Priority-based selection</li>
                <li>Health checking</li>
                <li>Offline fallback support</li>
                <li>Graceful degradation</li>
              </ul>
            </div>
          </div>
        </div>
      </Card>

      {errorRecoveryOptions && (
        <ErrorRecoveryModal
          isOpen={isErrorRecoveryOpen}
          options={errorRecoveryOptions}
          onDismiss={hideErrorRecovery}
        />
      )}
    </div>
  );
}
