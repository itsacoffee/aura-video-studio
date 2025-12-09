import {
  Button,
  makeStyles,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  ProgressBar,
  Spinner,
  Text,
  tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled, Rocket24Regular, Warning24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { InstallAllResponse } from '../../services/api/setupApi';
import { setupApi } from '../../services/api/setupApi';

const useStyles = makeStyles({
  container: {
    marginBottom: tokens.spacingVerticalL,
  },
  buttonWrapper: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  progressWrapper: {
    marginTop: tokens.spacingVerticalM,
  },
  statusText: {
    marginTop: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground3,
  },
  resultsList: {
    marginTop: tokens.spacingVerticalS,
    listStyleType: 'none',
    padding: 0,
    '& li': {
      display: 'flex',
      alignItems: 'center',
      gap: tokens.spacingHorizontalS,
      marginBottom: tokens.spacingVerticalXS,
    },
  },
  successIcon: {
    color: tokens.colorStatusSuccessForeground1,
  },
  failureIcon: {
    color: tokens.colorStatusDangerForeground1,
  },
});

interface QuickSetupButtonProps {
  /**
   * Called when installation completes (success or failure)
   */
  onComplete?: (result: InstallAllResponse) => void;
  /**
   * Called when installation starts
   */
  onStart?: () => void;
  /**
   * Disables the button
   */
  disabled?: boolean;
}

/**
 * Quick Setup button that installs all recommended dependencies.
 * Shows progress and results of installation.
 */
export function QuickSetupButton({ onComplete, onStart, disabled }: QuickSetupButtonProps) {
  const styles = useStyles();
  const [isInstalling, setIsInstalling] = useState(false);
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState<string>('');
  const [result, setResult] = useState<InstallAllResponse | null>(null);

  const handleQuickSetup = async () => {
    setIsInstalling(true);
    setProgress(0);
    setStatus('Initializing...');
    setResult(null);
    onStart?.();

    try {
      // Initialize portable directories first
      setStatus('Preparing directories...');
      setProgress(10);
      await setupApi.initializePortableDirectories();

      // Install all dependencies
      setStatus('Installing FFmpeg and Piper TTS...');
      setProgress(30);
      const response = await setupApi.installAllDependencies();

      setResult(response);
      setProgress(100);
      setStatus(response.success ? 'Installation complete!' : 'Some installations failed');
      onComplete?.(response);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      setStatus(`Installation failed: ${errorMessage}`);
      setResult({
        success: false,
        error: errorMessage,
        message: 'Installation failed',
        results: [],
      });
      onComplete?.({
        success: false,
        error: errorMessage,
      });
    } finally {
      setIsInstalling(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.buttonWrapper}>
        <Button
          appearance="primary"
          icon={isInstalling ? <Spinner size="tiny" /> : <Rocket24Regular />}
          onClick={handleQuickSetup}
          disabled={disabled || isInstalling}
          size="large"
        >
          {isInstalling ? 'Installing...' : 'Quick Setup - Install All'}
        </Button>
        {!result && !isInstalling && (
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            Installs FFmpeg + Piper TTS automatically
          </Text>
        )}
      </div>

      {isInstalling && (
        <div className={styles.progressWrapper}>
          <ProgressBar value={progress / 100} />
          <Text className={styles.statusText}>{status}</Text>
        </div>
      )}

      {result && (
        <MessageBar
          intent={result.success ? 'success' : 'warning'}
          style={{ marginTop: tokens.spacingVerticalM }}
        >
          <MessageBarBody>
            <MessageBarTitle>{result.success ? 'Setup Complete' : 'Partial Setup'}</MessageBarTitle>
            {result.message}

            {result.results && result.results.length > 0 && (
              <ul className={styles.resultsList}>
                {result.results.map((r, i) => (
                  <li key={i}>
                    {r.success ? (
                      <CheckmarkCircle24Filled className={styles.successIcon} />
                    ) : (
                      <Warning24Regular className={styles.failureIcon} />
                    )}
                    <Text>
                      {r.component}: {r.success ? 'Installed' : r.error || 'Failed'}
                    </Text>
                  </li>
                ))}
              </ul>
            )}
          </MessageBarBody>
        </MessageBar>
      )}
    </div>
  );
}
