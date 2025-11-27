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
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Folder24Regular,
  Open24Regular,
  Dismiss24Regular,
  DocumentBulletList24Regular,
} from '@fluentui/react-icons';
import React, { useState, useEffect, useRef, useCallback } from 'react';

const useStyles = makeStyles({
  // Main toast container with Apple-inspired glass morphism
  toastContainer: {
    backdropFilter: 'blur(24px) saturate(180%)',
    WebkitBackdropFilter: 'blur(24px) saturate(180%)',
    backgroundColor: 'rgba(255, 255, 255, 0.88)',
    ...shorthands.border('0.5px', 'solid', 'rgba(0, 0, 0, 0.08)'),
    ...shorthands.borderRadius('12px'),
    ...shorthands.padding('18px', '22px', '16px', '22px'),
    minWidth: '320px',
    maxWidth: '480px',
    width: 'fit-content',
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.10), 0 12px 48px rgba(0, 0, 0, 0.06)',
    transitionProperty: 'transform, box-shadow',
    transitionDuration: '200ms',
    transitionTimingFunction: 'cubic-bezier(0.4, 0, 0.2, 1)',
    position: 'relative',
    contain: 'layout style paint',
    willChange: 'transform, opacity',
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(30, 30, 30, 0.92)',
      ...shorthands.border('0.5px', 'solid', 'rgba(255, 255, 255, 0.1)'),
    },
    '@media (max-width: 768px)': {
      minWidth: '280px',
      maxWidth: '360px',
      ...shorthands.padding('16px', '18px', '14px', '18px'),
    },
    '@media (min-width: 1920px)': {
      maxWidth: '520px',
    },
    '@media (prefers-reduced-motion: reduce)': {
      transitionDuration: '0.01ms',
    },
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: '0 6px 16px rgba(0, 0, 0, 0.12), 0 16px 56px rgba(0, 0, 0, 0.08)',
    },
  },

  // Toast layout wrapper
  toastLayout: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap('8px'),
  },

  // Header row with title and close button
  toastHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    ...shorthands.gap('14px'),
    marginBottom: '2px',
  },

  // Title wrapper with icon
  toastTitleWrapper: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap('14px'),
    ...shorthands.flex(1),
    minWidth: 0,
  },

  // Icon container
  toastIcon: {
    width: '24px',
    height: '24px',
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },

  // Title typography with balanced text wrapping
  toastTitle: {
    fontSize: '15px',
    fontWeight: '600',
    lineHeight: '1.35',
    letterSpacing: '-0.01em',
    marginBottom: 0,
    color: tokens.colorNeutralForeground1,
    wordBreak: 'break-word',
    overflowWrap: 'break-word',
    hyphens: 'auto',
    WebkitHyphens: 'auto',
    ...shorthands.flex(1),
    minWidth: 0,
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.92)',
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

  // Body typography for readability
  toastBody: {
    fontSize: '14px',
    fontWeight: '400',
    lineHeight: '1.5',
    letterSpacing: '0',
    opacity: 0.9,
    color: tokens.colorNeutralForeground2,
    wordBreak: 'break-word',
    overflowWrap: 'break-word',
    hyphens: 'auto',
    maxWidth: '100%',
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.80)',
    },
  },

  // File path styling with monospace font
  pathText: {
    marginTop: '10px',
    fontSize: '12px',
    fontFamily:
      'ui-monospace, "SF Mono", "Cascadia Code", "Segoe UI Mono", Menlo, Monaco, Consolas, monospace',
    fontWeight: '400',
    lineHeight: '1.45',
    opacity: 0.65,
    color: tokens.colorNeutralForeground3,
    wordBreak: 'break-all',
    overflowWrap: 'anywhere',
    whiteSpace: 'pre-wrap',
    backgroundColor: 'rgba(0, 0, 0, 0.04)',
    ...shorthands.padding('6px', '8px'),
    ...shorthands.borderRadius('6px'),
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    ...shorthands.overflow('hidden'),
    textOverflow: 'ellipsis',
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(255, 255, 255, 0.06)',
      color: 'rgba(255, 255, 255, 0.65)',
    },
  },

  // Duration/metadata text
  metadataText: {
    marginTop: '8px',
    fontSize: '12px',
    fontWeight: '500',
    lineHeight: '1.4',
    opacity: 0.65,
    letterSpacing: '0.01em',
    color: tokens.colorNeutralForeground3,
    whiteSpace: 'nowrap',
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.65)',
    },
  },

  // Footer with action buttons
  toastFooter: {
    display: 'flex',
    ...shorthands.gap('10px'),
    marginTop: '14px',
    alignItems: 'center',
    flexWrap: 'wrap',
  },

  // Progress bar container
  progressBar: {
    height: '3px',
    backgroundColor: 'rgba(0, 0, 0, 0.08)',
    ...shorthands.borderRadius('1.5px'),
    ...shorthands.overflow('hidden'),
    marginTop: '14px',
    marginLeft: '-22px',
    marginRight: '-22px',
    marginBottom: '-16px',
    willChange: 'width',
    '@media (prefers-color-scheme: dark)': {
      backgroundColor: 'rgba(255, 255, 255, 0.12)',
    },
    '@media (max-width: 768px)': {
      marginLeft: '-18px',
      marginRight: '-18px',
      marginBottom: '-14px',
    },
  },

  // Progress bar fill
  progressFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transitionProperty: 'width',
    transitionDuration: '100ms',
    transitionTimingFunction: 'linear',
    willChange: 'width',
  },

  // Success progress fill
  progressFillSuccess: {
    backgroundColor: '#34C759',
  },

  // Error progress fill
  progressFillError: {
    backgroundColor: '#FF453A',
  },

  // Close button with hover state
  closeButton: {
    minWidth: '28px',
    height: '28px',
    ...shorthands.padding('0'),
    ...shorthands.borderRadius('6px'),
    transitionProperty: 'transform, background-color',
    transitionDuration: '150ms',
    transitionTimingFunction: 'cubic-bezier(0.4, 0, 0.2, 1)',
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ':hover': {
      transform: 'scale(1.08)',
      backgroundColor: 'rgba(0, 0, 0, 0.06)',
    },
    ':active': {
      transform: 'scale(0.96)',
    },
    '@media (prefers-color-scheme: dark)': {
      ':hover': {
        backgroundColor: 'rgba(255, 255, 255, 0.1)',
      },
    },
  },

  // Action buttons
  actionButton: {
    ...shorthands.borderRadius('8px'),
    ...shorthands.padding('7px', '16px'),
    fontSize: '13px',
    fontWeight: '500',
    lineHeight: '1.3',
    transitionProperty: 'transform',
    transitionDuration: '150ms',
    transitionTimingFunction: 'cubic-bezier(0.4, 0, 0.2, 1)',
    whiteSpace: 'nowrap',
    ':hover': {
      transform: 'translateY(-1px)',
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },

  // Success toast variant
  toastSuccess: {
    backgroundColor: 'rgba(52, 199, 89, 0.10)',
    ...shorthands.border('0.5px', 'solid', 'rgba(52, 199, 89, 0.25)'),
  },

  // Error toast variant
  toastError: {
    backgroundColor: 'rgba(255, 69, 58, 0.10)',
    ...shorthands.border('0.5px', 'solid', 'rgba(255, 69, 58, 0.25)'),
  },

  // Info toast variant
  toastInfo: {
    backgroundColor: 'rgba(10, 132, 255, 0.10)',
    ...shorthands.border('0.5px', 'solid', 'rgba(10, 132, 255, 0.25)'),
  },

  // Warning toast variant
  toastWarning: {
    backgroundColor: 'rgba(255, 159, 10, 0.10)',
    ...shorthands.border('0.5px', 'solid', 'rgba(255, 159, 10, 0.25)'),
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
    <div
      className={`${styles.toastContainer} toast-slide-in`}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
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
                    <CheckmarkCircle24Regular style={{ color: '#34C759' }} />
                  </div>
                  <div className={styles.toastTitle}>{title}</div>
                </div>
                <Button
                  size="small"
                  appearance="transparent"
                  icon={<Dismiss24Regular />}
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
                      icon={<Open24Regular />}
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
                      icon={<Folder24Regular />}
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
                      icon={<Open24Regular />}
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
                    <ErrorCircle24Regular style={{ color: '#FF453A' }} />
                  </div>
                  <div className={styles.toastTitle}>{title}</div>
                </div>
                <Button
                  size="small"
                  appearance="transparent"
                  icon={<Dismiss24Regular />}
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
                      icon={<DocumentBulletList24Regular />}
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
