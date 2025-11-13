/**
 * Degraded Mode Banner
 * Shows when providers are in degraded/offline mode with fallback active
 */

import {
  MessageBar,
  MessageBarBody,
  MessageBarActions,
  Button,
  Text,
} from '@fluentui/react-components';
import { Warning24Regular, Dismiss24Regular, Settings24Regular } from '@fluentui/react-icons';
import { useState } from 'react';

export interface DegradedModeBannerProps {
  providerName: string;
  providerType: 'llm' | 'tts' | 'image' | 'video';
  fallbackName?: string;
  reason?: string;
  onDismiss?: () => void;
  onOpenSettings?: () => void;
}

export function DegradedModeBanner({
  providerName,
  providerType,
  fallbackName,
  reason,
  onDismiss,
  onOpenSettings,
}: DegradedModeBannerProps) {
  const [isDismissed, setIsDismissed] = useState(false);

  const handleDismiss = () => {
    setIsDismissed(true);
    onDismiss?.();
  };

  if (isDismissed) {
    return null;
  }

  const getProviderTypeLabel = () => {
    switch (providerType) {
      case 'llm':
        return 'AI language model';
      case 'tts':
        return 'text-to-speech';
      case 'image':
        return 'image generation';
      case 'video':
        return 'video processing';
      default:
        return providerType;
    }
  };

  return (
    <MessageBar
      intent="warning"
      icon={<Warning24Regular />}
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 1000,
        borderRadius: 0,
      }}
    >
      <MessageBarBody>
        <Text weight="semibold">Degraded Mode: {providerName} Unavailable</Text>
        <Text>
          The {getProviderTypeLabel()} provider &quot;{providerName}&quot; is currently unavailable
          {reason ? ` (${reason})` : ''}.
          {fallbackName
            ? ` Using fallback provider "${fallbackName}" instead.`
            : ' Some features may be limited.'}
        </Text>
      </MessageBarBody>
      <MessageBarActions>
        {onOpenSettings && (
          <Button
            appearance="transparent"
            icon={<Settings24Regular />}
            onClick={onOpenSettings}
            size="small"
          >
            Settings
          </Button>
        )}
        <Button
          appearance="transparent"
          icon={<Dismiss24Regular />}
          onClick={handleDismiss}
          size="small"
          aria-label="Dismiss"
        />
      </MessageBarActions>
    </MessageBar>
  );
}

export interface OfflineModeBannerProps {
  onDismiss?: () => void;
  onRetry?: () => void;
}

export function OfflineModeBanner({ onDismiss, onRetry }: OfflineModeBannerProps) {
  const [isDismissed, setIsDismissed] = useState(false);

  const handleDismiss = () => {
    setIsDismissed(true);
    onDismiss?.();
  };

  if (isDismissed) {
    return null;
  }

  return (
    <MessageBar
      intent="error"
      icon={<Warning24Regular />}
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 1000,
        borderRadius: 0,
      }}
    >
      <MessageBarBody>
        <Text weight="semibold">Offline Mode</Text>
        <Text>
          You are currently offline. Only offline features and cached content are available.
        </Text>
      </MessageBarBody>
      <MessageBarActions>
        {onRetry && (
          <Button appearance="transparent" onClick={onRetry} size="small">
            Retry Connection
          </Button>
        )}
        <Button
          appearance="transparent"
          icon={<Dismiss24Regular />}
          onClick={handleDismiss}
          size="small"
          aria-label="Dismiss"
        />
      </MessageBarActions>
    </MessageBar>
  );
}
