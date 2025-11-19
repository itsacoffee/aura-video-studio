import {
  makeStyles,
  tokens,
  Text,
  Button,
  MessageBar,
  MessageBarBody,
  MessageBarActions,
  Link,
} from '@fluentui/react-components';
import { ArrowClockwise24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { resetCircuitBreaker } from '../../services/api/apiClient';
import { setupApi } from '../../services/api/setupApi';

const useStyles = makeStyles({
  banner: {
    marginBottom: tokens.spacingVerticalM,
  },
  helpText: {
    display: 'block',
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
  },
});

export interface BackendStatusBannerProps {
  onDismiss?: () => void;
  showRetry?: boolean;
}

export function BackendStatusBanner({ onDismiss, showRetry = true }: BackendStatusBannerProps) {
  const styles = useStyles();
  const [isChecking, setIsChecking] = useState(false);
  const [backendReachable, setBackendReachable] = useState(true);
  const [dismissed, setDismissed] = useState(false);
  const [initialCheckComplete, setInitialCheckComplete] = useState(false);

  const checkBackend = useCallback(async () => {
    setIsChecking(true);
    try {
      resetCircuitBreaker();
      await setupApi.getSystemStatus();
      setBackendReachable(true);
    } catch {
      setBackendReachable(false);
    } finally {
      setIsChecking(false);
      setInitialCheckComplete(true);
    }
  }, []);

  useEffect(() => {
    void checkBackend();
  }, [checkBackend]);

  const handleRetry = useCallback(() => {
    void checkBackend();
  }, [checkBackend]);

  const handleDismiss = useCallback(() => {
    setDismissed(true);
    onDismiss?.();
  }, [onDismiss]);

  if (backendReachable || dismissed || !initialCheckComplete) {
    return null;
  }

  return (
    <MessageBar intent="error" className={styles.banner}>
      <MessageBarBody>
        <Text weight="semibold" block>
          Backend Server Not Running
        </Text>
        <Text className={styles.helpText}>
          The Aura backend server is not reachable. Please start the backend server before
          continuing with setup.
        </Text>
        <Text className={styles.helpText}>
          <strong>To start the backend:</strong>
        </Text>
        <Text className={styles.helpText}>
          • Navigate to the project root directory in your terminal
        </Text>
        <Text className={styles.helpText}>
          • Run: <code>dotnet run --project Aura.Api</code>
        </Text>
        <Text className={styles.helpText}>
          • Wait for the message &ldquo;Application started. Press Ctrl+C to shut down.&rdquo;
        </Text>
        <Text className={styles.helpText}>• Then refresh this page or click Retry below</Text>
        <Text className={styles.helpText}>
          For more help, see the{' '}
          <Link
            href="https://github.com/Coffee285/aura-video-studio/blob/main/INSTALLATION.md"
            target="_blank"
            rel="noopener noreferrer"
          >
            Installation Guide
          </Link>
        </Text>
      </MessageBarBody>
      <MessageBarActions>
        {showRetry && (
          <Button
            appearance="transparent"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRetry}
            disabled={isChecking}
          >
            {isChecking ? 'Checking...' : 'Retry'}
          </Button>
        )}
        <Button appearance="transparent" icon={<Dismiss24Regular />} onClick={handleDismiss}>
          Dismiss
        </Button>
      </MessageBarActions>
    </MessageBar>
  );
}
