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
import { getSystemHealth } from '../../services/api/healthApi';

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

interface BackendStatus {
  reachable: boolean;
  online: boolean;
  error: 'unreachable' | 'http-error' | 'degraded' | null;
  message?: string;
}

export function BackendStatusBanner({ onDismiss, showRetry = true }: BackendStatusBannerProps) {
  const styles = useStyles();
  const [isChecking, setIsChecking] = useState(false);
  const [status, setStatus] = useState<BackendStatus>({
    reachable: true,
    online: true,
    error: null,
  });
  const [dismissed, setDismissed] = useState(false);
  const [initialCheckComplete, setInitialCheckComplete] = useState(false);

  const checkBackend = useCallback(async () => {
    setIsChecking(true);
    try {
      resetCircuitBreaker();
      const healthResponse = await getSystemHealth();
      
      // Backend is reachable and responding
      if (healthResponse.backendOnline) {
        setStatus({
          reachable: true,
          online: true,
          error: healthResponse.overallStatus === 'degraded' ? 'degraded' : null,
          message: healthResponse.overallStatus === 'degraded' 
            ? 'Backend is running but some components are not fully operational'
            : undefined,
        });
      } else {
        setStatus({
          reachable: true,
          online: false,
          error: 'http-error',
          message: 'Backend reports it is not online',
        });
      }
    } catch (error: unknown) {
      // Determine if this is a network/connection error or HTTP error
      const errorObj = error instanceof Error ? error : new Error(String(error));
      const errorMessage = errorObj.message.toLowerCase();
      
      // Network-level errors (no HTTP response at all)
      if (
        errorMessage.includes('network') ||
        errorMessage.includes('econnrefused') ||
        errorMessage.includes('timeout') ||
        errorMessage.includes('failed to fetch')
      ) {
        setStatus({
          reachable: false,
          online: false,
          error: 'unreachable',
          message: 'Cannot connect to backend server',
        });
      } else {
        // HTTP-level errors (got a response, but not successful)
        setStatus({
          reachable: true,
          online: false,
          error: 'http-error',
          message: 'Backend error - see logs for details',
        });
      }
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

  // Don't show banner if everything is OK, dismissed, or not yet checked
  if (status.error === null || dismissed || !initialCheckComplete) {
    return null;
  }

  // Show degraded warning (less severe)
  if (status.error === 'degraded') {
    return (
      <MessageBar intent="warning" className={styles.banner}>
        <MessageBarBody>
          <Text weight="semibold" block>
            Backend Degraded
          </Text>
          <Text className={styles.helpText}>
            The backend server is running but some components are not fully operational. Some features may not work as expected.
          </Text>
        </MessageBarBody>
        <MessageBarActions>
          <Button appearance="transparent" icon={<Dismiss24Regular />} onClick={handleDismiss}>
            Dismiss
          </Button>
        </MessageBarActions>
      </MessageBar>
    );
  }

  // Show unreachable error (network-level)
  if (status.error === 'unreachable') {
    return (
      <MessageBar intent="error" className={styles.banner}>
        <MessageBarBody>
          <Text weight="semibold" block>
            Backend Server Not Reachable
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

  // Show HTTP error (backend responded but with error)
  return (
    <MessageBar intent="error" className={styles.banner}>
      <MessageBarBody>
        <Text weight="semibold" block>
          Backend Error
        </Text>
        <Text className={styles.helpText}>
          The backend server is running but returned an error. Check the application logs for more details.
        </Text>
        {status.message && (
          <Text className={styles.helpText}>
            <strong>Details:</strong> {status.message}
          </Text>
        )}
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
