/**
 * Fallback Mode Notification Component
 * Displays a prominent but dismissible banner when ideation uses offline fallback
 */

import {
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  MessageBarActions,
  Button,
  Link,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { DismissRegular, SettingsRegular } from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    marginBottom: tokens.spacingVerticalL,
  },
  actionsContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
});

interface FallbackModeNotificationProps {
  /** Whether the notification should be shown */
  isVisible: boolean;
  /** Provider that was used (e.g., 'RuleBased') */
  providerUsed?: string;
  /** Reason for the fallback (e.g., 'Ollama not available') */
  fallbackReason?: string;
  /** Callback when notification is dismissed */
  onDismiss?: () => void;
  /** Whether to persist dismissal in session storage */
  persistDismissal?: boolean;
  /** Key for session storage persistence */
  storageKey?: string;
}

const DEFAULT_STORAGE_KEY = 'fallback-notification-dismissed';

export const FallbackModeNotification: React.FC<FallbackModeNotificationProps> = ({
  isVisible,
  providerUsed = 'RuleBased',
  fallbackReason = 'Ollama not available',
  onDismiss,
  persistDismissal = true,
  storageKey = DEFAULT_STORAGE_KEY,
}) => {
  const styles = useStyles();
  const navigate = useNavigate();
  const [isDismissed, setIsDismissed] = useState(false);

  // Check if notification was previously dismissed in this session
  useEffect(() => {
    if (persistDismissal) {
      const wasDismissed = sessionStorage.getItem(storageKey) === 'true';
      setIsDismissed(wasDismissed);
    }
  }, [persistDismissal, storageKey]);

  const handleDismiss = useCallback(() => {
    setIsDismissed(true);
    if (persistDismissal) {
      sessionStorage.setItem(storageKey, 'true');
    }
    onDismiss?.();
  }, [persistDismissal, storageKey, onDismiss]);

  const handleConfigureOllama = useCallback(() => {
    navigate('/settings?tab=ai-providers');
  }, [navigate]);

  // Don't render if not visible or already dismissed
  if (!isVisible || isDismissed) {
    return null;
  }

  return (
    <div className={styles.container} role="alert">
      <MessageBar intent="warning" layout="multiline">
        <MessageBarBody>
          <MessageBarTitle>⚠️ Running in Offline Mode</MessageBarTitle>
          AI ideation is using basic templates (provider: {providerUsed}).{' '}
          {fallbackReason && <>Reason: {fallbackReason}. </>}
          For better results, ensure Ollama is running.{' '}
          <Link href="https://ollama.com" target="_blank" rel="noopener noreferrer" inline>
            Download Ollama
          </Link>{' '}
          or run <code>ollama serve</code> if already installed.
        </MessageBarBody>
        <MessageBarActions
          containerAction={
            <Button
              appearance="transparent"
              icon={<DismissRegular />}
              onClick={handleDismiss}
              aria-label="Dismiss notification"
            />
          }
        >
          <div className={styles.actionsContainer}>
            <Button
              appearance="primary"
              size="small"
              icon={<SettingsRegular />}
              onClick={handleConfigureOllama}
            >
              Configure Ollama
            </Button>
          </div>
        </MessageBarActions>
      </MessageBar>
    </div>
  );
};

export default FallbackModeNotification;
