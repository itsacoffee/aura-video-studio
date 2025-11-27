import {
  makeStyles,
  tokens,
  shorthands,
  Button,
  Toast,
  ToastBody,
  ToastFooter,
  Toaster,
  useToastController,
} from '@fluentui/react-components';
import {
  CheckmarkCircle20Regular,
  ErrorCircle20Regular,
  Folder20Regular,
  Open20Regular,
  Dismiss20Regular,
  DocumentBulletList20Regular,
} from '@fluentui/react-icons';
import React, { useState, useEffect, useRef, useCallback } from 'react';

const useStyles = makeStyles({
  // Modern toast styling - compact and clean like macOS/Windows 11 notifications
  toast: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    backdropFilter: 'blur(20px) saturate(180%)',
    WebkitBackdropFilter: 'blur(20px) saturate(180%)',
    ...shorthands.borderRadius('10px'),
    ...shorthands.border('1px', 'solid', 'rgba(0, 0, 0, 0.06)'),
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.08), 0 1px 3px rgba(0, 0, 0, 0.04)',
    maxWidth: '360px',
    minWidth: '280px',
    ...shorthands.padding('12px', '14px'),
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(40, 40, 40, 0.95)',
      ...shorthands.border('1px', 'solid', 'rgba(255, 255, 255, 0.08)'),
      boxShadow: '0 4px 12px rgba(0, 0, 0, 0.24), 0 1px 3px rgba(0, 0, 0, 0.12)',
    },
  },

  // Toast layout
  toastLayout: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap('6px'),
  },

  // Header row with title and close button
  toastHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    ...shorthands.gap('10px'),
  },

  // Title wrapper with icon
  toastTitleWrapper: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap('8px'),
    ...shorthands.flex(1),
    minWidth: 0,
  },

  // Icon container
  toastIcon: {
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },

  // Title typography - compact
  toastTitle: {
    fontSize: '13px',
    fontWeight: '600',
    lineHeight: '1.4',
    marginBottom: 0,
    color: tokens.colorNeutralForeground1,
    ...shorthands.flex(1),
    minWidth: 0,
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.95)',
    },
  },

  // For backwards compatibility
  toastTitleContent: {
    ...shorthands.flex(1),
    minWidth: 0,
  },

  // Body text wrapper
  toastBodyWrapper: {
    ...shorthands.flex(1),
    minWidth: 0,
  },

  // Body typography - compact
  toastBody: {
    fontSize: '12px',
    fontWeight: '400',
    lineHeight: '1.4',
    color: tokens.colorNeutralForeground2,
    ...shorthands.margin(0),
    ...shorthands.padding(0),
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.7)',
    },
  },

  // File path styling - compact
  pathText: {
    marginTop: '6px',
    fontSize: '11px',
    fontFamily:
      'ui-monospace, "SF Mono", "Cascadia Code", "Segoe UI Mono", Menlo, Monaco, Consolas, monospace',
    fontWeight: '400',
    lineHeight: '1.4',
    opacity: 0.7,
    color: tokens.colorNeutralForeground3,
    wordBreak: 'break-all',
    overflowWrap: 'anywhere',
    whiteSpace: 'pre-wrap',
    backgroundColor: 'rgba(0, 0, 0, 0.03)',
    ...shorthands.padding('4px', '6px'),
    ...shorthands.borderRadius('4px'),
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    ...shorthands.overflow('hidden'),
    textOverflow: 'ellipsis',
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(255, 255, 255, 0.05)',
      color: 'rgba(255, 255, 255, 0.6)',
    },
  },

  // Duration/metadata text - compact
  metadataText: {
    marginTop: '4px',
    fontSize: '11px',
    fontWeight: '500',
    lineHeight: '1.4',
    opacity: 0.6,
    color: tokens.colorNeutralForeground3,
    whiteSpace: 'nowrap',
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.6)',
    },
  },

  // Footer with action buttons - compact
  toastFooter: {
    display: 'flex',
    ...shorthands.gap('8px'),
    marginTop: '8px',
    alignItems: 'center',
    flexWrap: 'wrap',
  },

  // Progress bar container
  progressBar: {
    height: '2px',
    backgroundColor: 'rgba(0, 0, 0, 0.06)',
    ...shorthands.borderRadius('1px'),
    ...shorthands.overflow('hidden'),
    marginTop: '10px',
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(255, 255, 255, 0.1)',
    },
  },

  // Progress bar fill
  progressFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transitionProperty: 'width',
    transitionDuration: '100ms',
    transitionTimingFunction: 'linear',
  },

  // Success progress fill
  progressFillSuccess: {
    backgroundColor: '#34C759',
  },

  // Error progress fill
  progressFillError: {
    backgroundColor: '#FF3B30',
  },

  // Close button - compact
  closeButton: {
    minWidth: '24px',
    height: '24px',
    ...shorthands.padding('0'),
    ...shorthands.borderRadius('4px'),
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    opacity: 0.6,
    ':hover': {
      opacity: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.05)',
    },
    '@media (prefers-color-scheme: dark)': {
      ':hover': {
        backgroundColor: 'rgba(255, 255, 255, 0.1)',
      },
    },
  },

  // Action buttons - compact
  actionButton: {
    ...shorthands.borderRadius('6px'),
    ...shorthands.padding('4px', '10px'),
    fontSize: '12px',
    fontWeight: '500',
    lineHeight: '1.3',
    whiteSpace: 'nowrap',
  },

  // Success toast variant
  toastSuccess: {
    backgroundColor: 'rgba(52, 199, 89, 0.08)',
    ...shorthands.border('1px', 'solid', 'rgba(52, 199, 89, 0.15)'),
  },

  // Error toast variant
  toastError: {
    backgroundColor: 'rgba(255, 59, 48, 0.08)',
    ...shorthands.border('1px', 'solid', 'rgba(255, 59, 48, 0.15)'),
  },

  // Info toast variant
  toastInfo: {
    backgroundColor: 'rgba(0, 122, 255, 0.08)',
    ...shorthands.border('1px', 'solid', 'rgba(0, 122, 255, 0.15)'),
  },

  // Warning toast variant
  toastWarning: {
    backgroundColor: 'rgba(255, 149, 0, 0.08)',
    ...shorthands.border('1px', 'solid', 'rgba(255, 149, 0, 0.15)'),
  },
});

export interface SuccessToastOptions {
  title: string;
  message: string;
  duration?: string;
  jobId?: string;
  artifactPath?: string;
  outputPath?: string; // Primary output file path to display
  onViewResults?: () => void;
  onOpenFile?: () => void; // Open the output file
  onOpenFolder?: () => void; // Open the folder containing output
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
    // This div is for hover detection to pause toast auto-dismiss, not for interactive content
    // eslint-disable-next-line jsx-a11y/no-static-element-interactions
    <div className={styles.toast} onMouseEnter={handleMouseEnter} onMouseLeave={handleMouseLeave}>
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

  const showSuccessToast = useCallback(
    (options: SuccessToastOptions) => {
      const {
        title,
        message,
        duration,
        outputPath,
        onViewResults,
        onOpenFile,
        onOpenFolder,
        timeout = 5000,
      } = options;

      const toastId = `toast-success-${Date.now()}`;

      const handleDismiss = () => {
        dismissToast(toastId);
      };

      dispatchToast(
        <ToastWithProgress timeout={timeout} onDismiss={handleDismiss}>
          <Toast>
            <div className={styles.toastLayout}>
              <div className={styles.toastHeader}>
                <div className={styles.toastTitleWrapper}>
                  <div className={styles.toastIcon}>
                    <CheckmarkCircle20Regular style={{ color: '#34C759' }} />
                  </div>
                  <div className={styles.toastTitle}>{title}</div>
                </div>
                <Button
                  size="small"
                  appearance="transparent"
                  icon={<Dismiss20Regular />}
                  onClick={handleDismiss}
                  aria-label="Dismiss notification"
                  className={styles.closeButton}
                />
              </div>
              <div className={styles.toastBodyWrapper}>
                <ToastBody className={styles.toastBody}>
                  <div>{message}</div>
                  {outputPath && (
                    <div className={styles.pathText} title={outputPath}>
                      {outputPath}
                    </div>
                  )}
                  {duration && <div className={styles.metadataText}>Duration: {duration}</div>}
                </ToastBody>
              </div>
              {(onViewResults || onOpenFile || onOpenFolder) && (
                <ToastFooter className={styles.toastFooter}>
                  {onOpenFile && (
                    <Button
                      size="small"
                      appearance="primary"
                      icon={<Open20Regular />}
                      onClick={onOpenFile}
                      className={styles.actionButton}
                    >
                      Open File
                    </Button>
                  )}
                  {onOpenFolder && (
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<Folder20Regular />}
                      onClick={onOpenFolder}
                      className={styles.actionButton}
                    >
                      Open Folder
                    </Button>
                  )}
                  {onViewResults && (
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<Open20Regular />}
                      onClick={onViewResults}
                      className={styles.actionButton}
                    >
                      View results
                    </Button>
                  )}
                </ToastFooter>
              )}
            </div>
          </Toast>
        </ToastWithProgress>,
        { intent: 'success', toastId }
      );

      return toastId;
    },
    [
      dispatchToast,
      dismissToast,
      styles.toastLayout,
      styles.toastHeader,
      styles.toastTitleWrapper,
      styles.toastIcon,
      styles.toastTitle,
      styles.closeButton,
      styles.toastBodyWrapper,
      styles.toastBody,
      styles.pathText,
      styles.metadataText,
      styles.toastFooter,
      styles.actionButton,
    ]
  );

  const showFailureToast = useCallback(
    (options: FailureToastOptions) => {
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
            <div className={styles.toastLayout}>
              <div className={styles.toastHeader}>
                <div className={styles.toastTitleWrapper}>
                  <div className={styles.toastIcon}>
                    <ErrorCircle20Regular style={{ color: '#FF3B30' }} />
                  </div>
                  <div className={styles.toastTitle}>{title}</div>
                </div>
                <Button
                  size="small"
                  appearance="transparent"
                  icon={<Dismiss20Regular />}
                  onClick={handleDismiss}
                  aria-label="Dismiss notification"
                  className={styles.closeButton}
                />
              </div>
              <div className={styles.toastBodyWrapper}>
                <ToastBody className={styles.toastBody}>
                  <div>{message}</div>
                  {errorDetails && <div className={styles.metadataText}>{errorDetails}</div>}
                  {correlationId && (
                    <div className={styles.pathText} title={correlationId}>
                      Correlation ID: {correlationId}
                    </div>
                  )}
                  {errorCode && <div className={styles.metadataText}>Error Code: {errorCode}</div>}
                </ToastBody>
              </div>
              {(onRetry || onOpenLogs) && (
                <ToastFooter className={styles.toastFooter}>
                  {onRetry && (
                    <Button
                      size="small"
                      appearance="primary"
                      onClick={onRetry}
                      className={styles.actionButton}
                    >
                      Retry
                    </Button>
                  )}
                  {onOpenLogs && (
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<DocumentBulletList20Regular />}
                      onClick={onOpenLogs}
                      className={styles.actionButton}
                    >
                      View Logs
                    </Button>
                  )}
                </ToastFooter>
              )}
            </div>
          </Toast>
        </ToastWithProgress>,
        { intent: 'error', toastId }
      );

      return toastId;
    },
    [
      dispatchToast,
      dismissToast,
      styles.toastLayout,
      styles.toastHeader,
      styles.toastTitleWrapper,
      styles.toastIcon,
      styles.toastTitle,
      styles.closeButton,
      styles.toastBodyWrapper,
      styles.toastBody,
      styles.pathText,
      styles.metadataText,
      styles.toastFooter,
      styles.actionButton,
    ]
  );

  return {
    showSuccessToast,
    showFailureToast,
    // Aliases for backwards compatibility
    showSuccess: showSuccessToast,
    showError: showFailureToast,
    showInfo: showSuccessToast,
  };
}

/**
 * Notifications Toaster component that should be placed at the app root
 * Enhanced with Apple-level positioning and behavior
 */
export function NotificationsToaster({ toasterId }: { toasterId: string }) {
  return (
    <Toaster
      toasterId={toasterId}
      position="top-end"
      offset={{ horizontal: 24, vertical: 24 }}
      pauseOnHover
      pauseOnWindowBlur
      limit={3}
    />
  );
}
