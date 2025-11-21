import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  ProgressBar,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import { ArrowClockwise24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import { healthCheckService } from '../services/HealthCheckService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    textAlign: 'center',
    minHeight: '400px',
  },
  spinner: {
    marginBottom: tokens.spacingVerticalL,
  },
  progressBar: {
    width: '100%',
    maxWidth: '400px',
    marginTop: tokens.spacingVerticalL,
  },
  progressText: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
  },
  errorContainer: {
    padding: tokens.spacingVerticalXL,
    maxWidth: '600px',
  },
  troubleshootingList: {
    textAlign: 'left',
    marginTop: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalL,
  },
  troubleshootingItem: {
    marginTop: tokens.spacingVerticalXS,
  },
  retryButton: {
    marginTop: tokens.spacingVerticalL,
  },
});

type BackendStatus = 'checking' | 'healthy' | 'unhealthy';

export interface SetupWizardProps {
  onBackendReady?: () => void;
  children?: React.ReactNode;
}

export const SetupWizard: React.FC<SetupWizardProps> = ({ onBackendReady, children }) => {
  const classes = useStyles();
  const [backendStatus, setBackendStatus] = useState<BackendStatus>('checking');
  const [healthCheckProgress, setHealthCheckProgress] = useState({ current: 0, max: 10 });
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    void checkBackendHealth();
    // Only run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const checkBackendHealth = async () => {
    setBackendStatus('checking');
    setErrorMessage('');

    const result = await healthCheckService.checkHealth((attempt, maxAttempts) => {
      setHealthCheckProgress({ current: attempt, max: maxAttempts });
    });

    if (result.isHealthy) {
      setBackendStatus('healthy');
      // eslint-disable-next-line no-console
      console.log(`[SetupWizard] Backend is healthy (latency: ${result.latencyMs}ms)`);

      if (onBackendReady) {
        onBackendReady();
      }
    } else {
      setBackendStatus('unhealthy');
      setErrorMessage(result.message);
      console.error('[SetupWizard] Backend health check failed:', result.message);
    }
  };

  const handleRetry = () => {
    setRetryCount((prev) => prev + 1);
    void checkBackendHealth();
  };

  if (backendStatus === 'checking') {
    return (
      <div className={classes.container}>
        <Spinner
          size="extra-large"
          className={classes.spinner}
          label="Connecting to Aura backend..."
        />
        <ProgressBar
          value={healthCheckProgress.current}
          max={healthCheckProgress.max}
          className={classes.progressBar}
        />
        <Text className={classes.progressText}>
          Attempt {healthCheckProgress.current} of {healthCheckProgress.max}
        </Text>
      </div>
    );
  }

  if (backendStatus === 'unhealthy') {
    return (
      <div className={classes.errorContainer}>
        <MessageBar intent="error">
          <MessageBarBody>
            <MessageBarTitle>Backend Server Not Reachable</MessageBarTitle>
            <Text block>{errorMessage}</Text>
          </MessageBarBody>
        </MessageBar>

        <div className={classes.troubleshootingList}>
          <Text weight="semibold" block>
            Troubleshooting Steps:
          </Text>
          <ol>
            <li className={classes.troubleshootingItem}>
              <Text>Ensure Windows Firewall allows Aura Video Studio</Text>
            </li>
            <li className={classes.troubleshootingItem}>
              <Text>Check if another application is using port 5000</Text>
            </li>
            <li className={classes.troubleshootingItem}>
              <Text>Try restarting the application</Text>
            </li>
            <li className={classes.troubleshootingItem}>
              <Text>Check the logs in %APPDATA%/AuraVideoStudio/logs</Text>
            </li>
          </ol>
        </div>

        <Button
          appearance="primary"
          icon={<ArrowClockwise24Regular />}
          onClick={handleRetry}
          className={classes.retryButton}
        >
          Retry Connection (Attempt {retryCount + 1})
        </Button>
      </div>
    );
  }

  return <>{children}</>;
};
