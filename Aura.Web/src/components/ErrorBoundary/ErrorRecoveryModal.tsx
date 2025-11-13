/**
 * Error Recovery Modal
 * Provides user-friendly error recovery options with retry functionality
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  MessageBar,
  MessageBarBody,
  Text,
  Spinner,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  Dismiss24Regular,
  Info24Regular,
  Warning24Regular,
  ErrorCircle24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { ErrorSeverity } from '../../services/errorReportingService';
import { loggingService } from '../../services/loggingService';

export interface ErrorRecoveryOptions {
  title: string;
  message: string;
  severity: ErrorSeverity;
  error?: Error;
  canRetry: boolean;
  retryAction?: () => Promise<void>;
  suggestedActions?: Array<{
    label: string;
    action: () => void;
    primary?: boolean;
  }>;
  technicalDetails?: string;
  onClose?: () => void;
}

interface ErrorRecoveryModalProps {
  isOpen: boolean;
  options: ErrorRecoveryOptions;
  onDismiss: () => void;
}

export function ErrorRecoveryModal({ isOpen, options, onDismiss }: ErrorRecoveryModalProps) {
  const [isRetrying, setIsRetrying] = useState(false);
  const [retryError, setRetryError] = useState<string | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const handleRetry = useCallback(async () => {
    if (!options.retryAction) return;

    setIsRetrying(true);
    setRetryError(null);

    try {
      loggingService.info(
        'User initiated error recovery retry',
        'ErrorRecoveryModal',
        'handleRetry',
        {
          errorTitle: options.title,
          severity: options.severity,
        }
      );

      await options.retryAction();

      loggingService.info('Error recovery retry successful', 'ErrorRecoveryModal', 'handleRetry');

      onDismiss();
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : 'Retry failed. Please try again.';
      setRetryError(errorMessage);

      loggingService.error(
        'Error recovery retry failed',
        error instanceof Error ? error : new Error(String(error)),
        'ErrorRecoveryModal',
        'handleRetry'
      );
    } finally {
      setIsRetrying(false);
    }
  }, [options, onDismiss]);

  const getSeverityIcon = () => {
    switch (options.severity) {
      case 'info':
        return <Info24Regular />;
      case 'warning':
        return <Warning24Regular />;
      case 'error':
      case 'critical':
        return <ErrorCircle24Regular />;
      default:
        return <ErrorCircle24Regular />;
    }
  };

  const getSeverityColor = () => {
    switch (options.severity) {
      case 'info':
        return 'var(--colorPaletteBlueForeground1)';
      case 'warning':
        return 'var(--colorPaletteYellowForeground1)';
      case 'error':
        return 'var(--colorPaletteRedForeground1)';
      case 'critical':
        return 'var(--colorPaletteDarkRedForeground1)';
      default:
        return 'var(--colorNeutralForeground1)';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onDismiss()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ color: getSeverityColor() }}>{getSeverityIcon()}</span>
              {options.title}
            </div>
          </DialogTitle>
          <DialogContent>
            <MessageBar
              intent={options.severity === 'critical' ? 'error' : options.severity}
              style={{ marginBottom: '16px' }}
            >
              <MessageBarBody>
                <Text>{options.message}</Text>
              </MessageBarBody>
            </MessageBar>

            {retryError && (
              <MessageBar intent="error" style={{ marginBottom: '16px' }}>
                <MessageBarBody>
                  <Text weight="semibold">Retry Failed</Text>
                  <Text>{retryError}</Text>
                </MessageBarBody>
              </MessageBar>
            )}

            {options.suggestedActions && options.suggestedActions.length > 0 && (
              <div style={{ marginBottom: '16px' }}>
                <Text weight="semibold" style={{ display: 'block', marginBottom: '8px' }}>
                  Suggested Actions:
                </Text>
                <ul style={{ margin: '0', paddingLeft: '20px' }}>
                  {options.suggestedActions.map((action, index) => (
                    <li key={index} style={{ marginBottom: '8px' }}>
                      <Button
                        appearance="subtle"
                        size="small"
                        onClick={action.action}
                        style={{ padding: '0', height: 'auto', minHeight: 'auto' }}
                      >
                        {action.label}
                      </Button>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {options.technicalDetails && (
              <details>
                <summary
                  style={{
                    cursor: 'pointer',
                    padding: '8px',
                    background: 'var(--colorNeutralBackground3)',
                    borderRadius: '4px',
                    marginBottom: '8px',
                  }}
                  onClick={() => setShowDetails(!showDetails)}
                >
                  <Text weight="semibold">Technical Details</Text>
                </summary>
                {showDetails && (
                  <pre
                    style={{
                      padding: '12px',
                      background: 'var(--colorNeutralBackground4)',
                      borderRadius: '4px',
                      overflow: 'auto',
                      fontSize: '12px',
                      fontFamily: 'monospace',
                      maxHeight: '200px',
                    }}
                  >
                    {options.technicalDetails}
                  </pre>
                )}
              </details>
            )}

            {isRetrying && (
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  padding: '12px',
                  background: 'var(--colorNeutralBackground3)',
                  borderRadius: '4px',
                  marginTop: '16px',
                }}
              >
                <Spinner size="tiny" />
                <Text>Retrying operation...</Text>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            {options.canRetry && options.retryAction && (
              <Button
                appearance="primary"
                icon={<ArrowClockwise24Regular />}
                onClick={handleRetry}
                disabled={isRetrying}
              >
                {isRetrying ? 'Retrying...' : 'Try Again'}
              </Button>
            )}
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={onDismiss}
              disabled={isRetrying}
            >
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
