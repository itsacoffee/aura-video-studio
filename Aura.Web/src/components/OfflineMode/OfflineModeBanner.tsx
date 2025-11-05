import {
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Button,
  Spinner,
} from '@fluentui/react-components';
import {
  DismissRegular,
  InfoRegular,
  WarningRegular,
  CheckmarkCircleRegular,
} from '@fluentui/react-icons';
import React, { useEffect, useState } from 'react';
import { offlineProvidersApi } from '@/services/api/offlineProvidersApi';
import type { OfflineProvidersStatus } from '@/types/offlineProviders';

import './OfflineModeBanner.css';

interface OfflineModeBannerProps {
  /**
   * Whether to show the banner
   */
  show?: boolean;
  /**
   * Callback when banner is dismissed
   */
  onDismiss?: () => void;
}

/**
 * Banner component that displays offline provider status and availability
 */
export const OfflineModeBanner: React.FC<OfflineModeBannerProps> = ({ show = true, onDismiss }) => {
  const [status, setStatus] = useState<OfflineProvidersStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    if (!show) return;

    const checkProviders = async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await offlineProvidersApi.checkAll();
        setStatus(result);
      } catch (err: unknown) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        console.error('Failed to check offline providers:', errorObj);
        setError('Failed to check offline provider status');
      } finally {
        setLoading(false);
      }
    };

    checkProviders();
  }, [show]);

  const handleDismiss = () => {
    setDismissed(true);
    onDismiss?.();
  };

  if (!show || dismissed) {
    return null;
  }

  if (loading) {
    return (
      <MessageBar intent="info" className="offline-mode-banner">
        <MessageBarBody>
          <Spinner size="tiny" />
          <span style={{ marginLeft: '8px' }}>Checking offline providers...</span>
        </MessageBarBody>
      </MessageBar>
    );
  }

  if (error) {
    return (
      <MessageBar intent="error" className="offline-mode-banner">
        <MessageBarBody>
          <MessageBarTitle>Offline Provider Check Failed</MessageBarTitle>
          {error}
        </MessageBarBody>
        {onDismiss && (
          <Button
            appearance="transparent"
            icon={<DismissRegular />}
            onClick={handleDismiss}
            aria-label="Dismiss"
          />
        )}
      </MessageBar>
    );
  }

  if (!status) {
    return null;
  }

  const getMessageIntent = (): 'success' | 'warning' | 'info' => {
    if (status.isFullyOperational) {
      return 'success';
    }
    if (status.hasTtsProvider && status.hasLlmProvider) {
      return 'info';
    }
    return 'warning';
  };

  const getIcon = () => {
    const intent = getMessageIntent();
    if (intent === 'success') return <CheckmarkCircleRegular />;
    if (intent === 'warning') return <WarningRegular />;
    return <InfoRegular />;
  };

  const getMessage = (): { title: string; body: string } => {
    if (status.isFullyOperational) {
      return {
        title: 'Offline Mode Ready',
        body: 'All offline providers are available. You can generate videos without internet connection.',
      };
    }

    const missing: string[] = [];
    if (!status.hasLlmProvider) missing.push('LLM (Ollama)');
    if (!status.hasTtsProvider) missing.push('TTS');
    if (!status.hasImageProvider) missing.push('Image generation');

    if (missing.length === 0) {
      return {
        title: 'Offline Mode Partially Ready',
        body: 'Core offline providers available. Some advanced features may be limited.',
      };
    }

    return {
      title: 'Offline Providers Missing',
      body: `Missing: ${missing.join(', ')}. Install these providers for full offline capabilities.`,
    };
  };

  const message = getMessage();

  return (
    <MessageBar intent={getMessageIntent()} className="offline-mode-banner" icon={getIcon()}>
      <MessageBarBody>
        <MessageBarTitle>{message.title}</MessageBarTitle>
        <div>{message.body}</div>

        <div className="offline-provider-summary">
          <strong>Provider Status:</strong>
          <ul>
            {status.ollama.isAvailable && <li>✓ Ollama: {status.ollama.message}</li>}
            {!status.ollama.isAvailable && <li>✗ Ollama: Not available</li>}

            {(status.piper.isAvailable ||
              status.mimic3.isAvailable ||
              status.windowsTts.isAvailable) && (
              <li>
                ✓ TTS:{' '}
                {[
                  status.piper.isAvailable && 'Piper',
                  status.mimic3.isAvailable && 'Mimic3',
                  status.windowsTts.isAvailable && 'Windows TTS',
                ]
                  .filter(Boolean)
                  .join(', ')}
              </li>
            )}
            {!status.hasTtsProvider && <li>✗ TTS: No offline TTS available</li>}

            {status.stableDiffusion.isAvailable && <li>✓ Stable Diffusion: Available</li>}
            {!status.stableDiffusion.isAvailable && (
              <li>ℹ Stable Diffusion: Not available (optional)</li>
            )}
          </ul>
        </div>
      </MessageBarBody>

      {onDismiss && (
        <Button
          appearance="transparent"
          icon={<DismissRegular />}
          onClick={handleDismiss}
          aria-label="Dismiss"
        />
      )}
    </MessageBar>
  );
};

export default OfflineModeBanner;
