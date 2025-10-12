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
  useId,
} from '@fluentui/react-components';
import { 
  CheckmarkCircle24Regular, 
  ErrorCircle24Regular,
  Folder24Regular,
  Open24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  toastFooter: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
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
}

export interface FailureToastOptions {
  title: string;
  message: string;
  errorDetails?: string;
  correlationId?: string;
  errorCode?: string;
  onRetry?: () => void;
  onOpenLogs?: () => void;
}

/**
 * Hook to display success and failure toasts with action buttons
 */
export function useNotifications() {
  const toasterId = useId('notifications-toaster');
  const { dispatchToast } = useToastController(toasterId);
  const styles = useStyles();

  const showSuccessToast = (options: SuccessToastOptions) => {
    const { title, message, duration, onViewResults, onOpenFolder } = options;

    dispatchToast(
      <Toast>
        <ToastTitle action={<CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />}>
          {title}
        </ToastTitle>
        <ToastBody>
          <div>
            <div>{message}</div>
            {duration && (
              <div style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}>
                Duration: {duration}
              </div>
            )}
          </div>
        </ToastBody>
        {(onViewResults || onOpenFolder) && (
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
          </ToastFooter>
        )}
      </Toast>,
      { intent: 'success', timeout: 10000 }
    );
  };

  const showFailureToast = (options: FailureToastOptions) => {
    const { title, message, errorDetails, correlationId, errorCode, onRetry, onOpenLogs } = options;

    dispatchToast(
      <Toast>
        <ToastTitle action={<ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />}>
          {title}
        </ToastTitle>
        <ToastBody>
          <div>
            <div>{message}</div>
            {errorDetails && (
              <div style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}>
                {errorDetails}
              </div>
            )}
            {correlationId && (
              <div style={{ marginTop: tokens.spacingVerticalXS, fontSize: '11px', opacity: 0.7, fontFamily: 'monospace' }}>
                Correlation ID: {correlationId}
              </div>
            )}
            {errorCode && (
              <div style={{ marginTop: tokens.spacingVerticalXXS, fontSize: '11px', opacity: 0.7 }}>
                Error Code: {errorCode}
              </div>
            )}
          </div>
        </ToastBody>
        {(onRetry || onOpenLogs) && (
          <ToastFooter className={styles.toastFooter}>
            {onRetry && (
              <Button
                size="small"
                appearance="primary"
                onClick={onRetry}
              >
                Retry
              </Button>
            )}
            {onOpenLogs && (
              <Button
                size="small"
                appearance="subtle"
                onClick={onOpenLogs}
              >
                Open Logs
              </Button>
            )}
          </ToastFooter>
        )}
      </Toast>,
      { intent: 'error', timeout: -1 } // Don't auto-dismiss error toasts
    );
  };

  return { showSuccessToast, showFailureToast, toasterId };
}

/**
 * Notifications Toaster component that should be placed at the app root
 */
export function NotificationsToaster({ toasterId }: { toasterId: string }) {
  return <Toaster toasterId={toasterId} position="top-end" />;
}
