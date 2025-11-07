import {
  makeStyles,
  tokens,
  Button,
  Toast,
  ToastTitle,
  ToastBody,
  ToastFooter,
  Toaster,
  useToastController,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Folder24Regular,
  Open24Regular,
  Dismiss24Regular,
  DocumentBulletList24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useRef } from 'react';

const useStyles = makeStyles({
  toastFooter: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
    alignItems: 'center',
  },
  progressBar: {
    height: '3px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '1px',
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalXS,
  },
  progressFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transition: 'width 100ms linear',
  },
  closeButton: {
    marginLeft: 'auto',
  },
});

export interface SuccessToastOptions {
  title: string;
  message: string;
  duration?: string;
  jobId?: string;
  artifactPath?: string;
  onViewResults?: () => void;
  onOpenFolder?: () => void;
  timeout?: number; // Auto-dismiss timeout in ms (default 5000)
}

export interface FailureToastOptions {
  title: string;
  message: string;
  errorDetails?: string;
  correlationId?: string;
  errorCode?: string;
  onRetry?: () => void;
  onOpenLogs?: () => void;
  timeout?: number; // Auto-dismiss timeout in ms (default 5000 for errors too)
}

/**
 * Constant toaster ID used across the app
 * Must match the toasterId in NotificationsToaster component
 */
const TOASTER_ID = 'notifications-toaster';

/**
 * Toast component with auto-dismiss progress bar and close button
 * Supports ESC key to dismiss and mouse hover to pause auto-dismiss
 */
function ToastWithProgress({
  children,
  timeout = 5000,
  onDismiss,
}: {
  children: React.ReactNode;
  timeout?: number;
  onDismiss?: () => void;
}) {
  const styles = useStyles();
  const [progress, setProgress] = useState(100);
  const [isPaused, setIsPaused] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const startTimeRef = useRef<number>(0);
  const remainingTimeRef = useRef<number>(timeout);

  useEffect(() => {
    if (timeout <= 0) {
      return;
    }

    const startTimer = () => {
      startTimeRef.current = Date.now();
      const interval = 100;
      const step = (interval / remainingTimeRef.current) * 100;
      let currentProgress = progress;

      timerRef.current = setInterval(() => {
        if (!isPaused) {
          currentProgress -= step;
          if (currentProgress <= 0) {
            if (timerRef.current) {
              clearInterval(timerRef.current);
            }
            onDismiss?.();
          } else {
            setProgress(currentProgress);
          }
        }
      }, interval);
    };

    startTimer();

    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        const elapsed = Date.now() - startTimeRef.current;
        remainingTimeRef.current = Math.max(0, remainingTimeRef.current - elapsed);
      }
    };
  }, [timeout, onDismiss, isPaused, progress]);

  // ESC key handler
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onDismiss?.();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onDismiss]);

  const handleMouseEnter = () => {
    setIsPaused(true);
  };

  const handleMouseLeave = () => {
    setIsPaused(false);
  };

  return (
    <div onMouseEnter={handleMouseEnter} onMouseLeave={handleMouseLeave}>
      {children}
      {timeout > 0 && (
        <div className={styles.progressBar}>
          <div className={styles.progressFill} style={{ width: `${progress}%` }} />
        </div>
      )}
    </div>
  );
}

/**
 * Hook to display success and failure toasts with action buttons
 */
// eslint-disable-next-line react-refresh/only-export-components
export function useNotifications() {
  const { dispatchToast, dismissToast } = useToastController(TOASTER_ID);
  const styles = useStyles();

  const showSuccessToast = (options: SuccessToastOptions) => {
    const { title, message, duration, onViewResults, onOpenFolder, timeout = 5000 } = options;

    const toastId = `toast-success-${Date.now()}`;

    const handleDismiss = () => {
      dismissToast(toastId);
    };

    dispatchToast(
      <ToastWithProgress timeout={timeout} onDismiss={handleDismiss}>
        <Toast>
          <ToastTitle
            action={
              <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
            }
          >
            {title}
          </ToastTitle>
          <ToastBody>
            <div>
              <div>{message}</div>
              {duration && (
                <div
                  style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}
                >
                  Duration: {duration}
                </div>
              )}
            </div>
          </ToastBody>
          <ToastFooter className={styles.toastFooter}>
            {onViewResults && (
              <Button
                size="small"
                appearance="primary"
                icon={<Open24Regular />}
                onClick={onViewResults}
              >
                View results
              </Button>
            )}
            {onOpenFolder && (
              <Button
                size="small"
                appearance="subtle"
                icon={<Folder24Regular />}
                onClick={onOpenFolder}
              >
                Open folder
              </Button>
            )}
            <Button
              size="small"
              appearance="transparent"
              icon={<Dismiss24Regular />}
              onClick={handleDismiss}
              className={styles.closeButton}
              aria-label="Dismiss notification"
            />
          </ToastFooter>
        </Toast>
      </ToastWithProgress>,
      { intent: 'success', toastId }
    );

    return toastId;
  };

  const showFailureToast = (options: FailureToastOptions) => {
    const {
      title,
      message,
      errorDetails,
      correlationId,
      errorCode,
      onRetry,
      onOpenLogs,
      timeout = 5000,
    } = options;

    const toastId = `toast-error-${Date.now()}`;

    const handleDismiss = () => {
      dismissToast(toastId);
    };

    dispatchToast(
      <ToastWithProgress timeout={timeout} onDismiss={handleDismiss}>
        <Toast>
          <ToastTitle
            action={<ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />}
          >
            {title}
          </ToastTitle>
          <ToastBody>
            <div>
              <div>{message}</div>
              {errorDetails && (
                <div
                  style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}
                >
                  {errorDetails}
                </div>
              )}
              {correlationId && (
                <div
                  style={{
                    marginTop: tokens.spacingVerticalXS,
                    fontSize: '11px',
                    opacity: 0.7,
                    fontFamily: 'monospace',
                  }}
                >
                  Correlation ID: {correlationId}
                </div>
              )}
              {errorCode && (
                <div
                  style={{ marginTop: tokens.spacingVerticalXXS, fontSize: '11px', opacity: 0.7 }}
                >
                  Error Code: {errorCode}
                </div>
              )}
            </div>
          </ToastBody>
          <ToastFooter className={styles.toastFooter}>
            {onRetry && (
              <Button size="small" appearance="primary" onClick={onRetry}>
                Retry
              </Button>
            )}
            {onOpenLogs && (
              <Button
                size="small"
                appearance="subtle"
                icon={<DocumentBulletList24Regular />}
                onClick={onOpenLogs}
              >
                View Logs
              </Button>
            )}
            <Button
              size="small"
              appearance="transparent"
              icon={<Dismiss24Regular />}
              onClick={handleDismiss}
              className={styles.closeButton}
              aria-label="Dismiss notification"
            />
          </ToastFooter>
        </Toast>
      </ToastWithProgress>,
      { intent: 'error', toastId }
    );

    return toastId;
  };

  return { showSuccessToast, showFailureToast };
}

/**
 * Notifications Toaster component that should be placed at the app root
 */
export function NotificationsToaster({ toasterId }: { toasterId: string }) {
  return <Toaster toasterId={toasterId} position="top-end" />;
}
