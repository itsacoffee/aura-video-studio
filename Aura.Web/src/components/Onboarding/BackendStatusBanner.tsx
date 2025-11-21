import {
  makeStyles,
  tokens,
  Text,
  Button,
  MessageBar,
  MessageBarBody,
  MessageBarActions,
  Link,
  Spinner,
} from '@fluentui/react-components';
import { ArrowClockwise24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useCallback, useRef } from 'react';
import { resetCircuitBreaker } from '../../services/api/apiClient';
import { backendHealthService } from '../../services/backendHealthService';
import { loggingService } from '../../services/loggingService';

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
  error: 'unreachable' | 'http-error' | null;
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

  const retryCountRef = useRef(0);
  const maxAutoRetries = 15; // Auto-retry for up to 15 seconds (backend startup time)

  const checkBackend = useCallback(async (isAutoRetry = false) => {
    setIsChecking(true);
    try {
      resetCircuitBreaker();

      // Use backend health service with retry logic
      // For initial check during startup, use aggressive retries
      const healthStatus = await backendHealthService.checkHealth({
        timeout: 3000,
        maxRetries: isAutoRetry ? 1 : 3,
        retryDelay: 500,
        exponentialBackoff: false,
      });

      if (healthStatus.healthy) {
        setStatus({
          reachable: true,
          online: true,
          error: null,
        });
        retryCountRef.current = 0;
      } else {
        setStatus({
          reachable: healthStatus.reachable,
          online: false,
          error: healthStatus.reachable ? 'http-error' : 'unreachable',
          message: healthStatus.error || 'Backend not responding',
        });
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      setStatus({
        reachable: false,
        online: false,
        error: 'unreachable',
        message: errorObj.message,
      });
    } finally {
      setIsChecking(false);
      setInitialCheckComplete(true);
    }
  }, []);

  useEffect(() => {
    // Initial check
    void checkBackend(false);

    // Auto-retry mechanism for backend startup
    // Backend might take a few seconds to start, especially on first launch
    const retryInterval = setInterval(() => {
      if (status.error === 'unreachable' && retryCountRef.current < maxAutoRetries) {
        retryCountRef.current++;
        // Log retry attempt for debugging
        loggingService.info(
          'BackendStatusBanner',
          'Auto-retrying backend health check',
          undefined,
          {
            attempt: retryCountRef.current,
            maxAttempts: maxAutoRetries,
          }
        );
        void checkBackend(true);
      } else if (status.error === null || retryCountRef.current >= maxAutoRetries) {
        clearInterval(retryInterval);
      }
    }, 1000);

    return () => {
      clearInterval(retryInterval);
    };
  }, [checkBackend, status.error]);

  const handleRetry = useCallback(() => {
    retryCountRef.current = 0; // Reset retry count on manual retry
    void checkBackend(false);
  }, [checkBackend]);

  const handleDismiss = useCallback(() => {
    setDismissed(true);
    onDismiss?.();
  }, [onDismiss]);

  // Show loading state while backend is starting up
  if (isChecking && !initialCheckComplete) {
    return (
      <MessageBar intent="info" className={styles.banner}>
        <MessageBarBody>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Spinner size="tiny" />
            <div>
              <Text weight="semibold" block>
                Starting Backend Server...
              </Text>
              <Text className={styles.helpText}>
                The Aura backend server is starting. This may take a few seconds on first launch.
              </Text>
            </div>
          </div>
        </MessageBarBody>
      </MessageBar>
    );
  }

  // Show auto-retry message if backend is unreachable but we're still retrying
  if (
    status.error === 'unreachable' &&
    retryCountRef.current > 0 &&
    retryCountRef.current < maxAutoRetries
  ) {
    return (
      <MessageBar intent="warning" className={styles.banner}>
        <MessageBarBody>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
            <Spinner size="tiny" />
            <div>
              <Text weight="semibold" block>
                Waiting for Backend Server...
              </Text>
              <Text className={styles.helpText}>
                The backend server is starting up. Attempt {retryCountRef.current} of{' '}
                {maxAutoRetries}.
              </Text>
              <Text className={styles.helpText}>
                If you&apos;re running Aura in Electron, the backend should auto-start
                automatically.
              </Text>
            </div>
          </div>
        </MessageBarBody>
      </MessageBar>
    );
  }

  // Don't show banner if everything is OK, dismissed, or not yet checked
  if (status.error === null || dismissed || !initialCheckComplete) {
    return null;
  }

  // Show unreachable error (network-level) - only after retries exhausted
  if (status.error === 'unreachable') {
    return (
      <MessageBar intent="error" className={styles.banner}>
        <MessageBarBody>
          <Text weight="semibold" block>
            Backend Server Not Reachable
          </Text>
          <Text className={styles.helpText}>
            The Aura backend server could not be reached after multiple attempts.
          </Text>
          <Text className={styles.helpText}>
            <strong>If you&apos;re running via Electron (Desktop App):</strong>
          </Text>
          <Text className={styles.helpText}>• The backend should auto-start automatically</Text>
          <Text className={styles.helpText}>• Check the application logs for errors</Text>
          <Text className={styles.helpText}>• Try restarting the application</Text>
          <Text className={styles.helpText}>
            <strong>If you&apos;re running via browser (Development):</strong>
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
          The backend server is running but returned an error. Check the application logs for more
          details.
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
