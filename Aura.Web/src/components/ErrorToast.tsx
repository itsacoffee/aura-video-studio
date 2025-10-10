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
import { Copy24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  toastFooter: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
});

export interface ErrorDetails {
  message: string;
  correlationId?: string;
  errorCode?: string;
  timestamp?: string;
  [key: string]: any;
}

export interface ErrorToastOptions {
  title: string;
  details: ErrorDetails;
  intent?: 'error' | 'warning' | 'info';
}

/**
 * Hook to display error toasts with copy-to-clipboard functionality
 */
export function useErrorToast() {
  const toasterId = useId('error-toaster');
  const { dispatchToast } = useToastController(toasterId);
  const styles = useStyles();

  const showErrorToast = (options: ErrorToastOptions) => {
    const { title, details, intent = 'error' } = options;

    const handleCopyDetails = () => {
      const detailsJson = JSON.stringify(
        {
          ...details,
          timestamp: details.timestamp || new Date().toISOString(),
        },
        null,
        2
      );
      navigator.clipboard.writeText(detailsJson);
    };

    dispatchToast(
      <Toast>
        <ToastTitle>{title}</ToastTitle>
        <ToastBody>
          <div>
            <div>{details.message}</div>
            {details.correlationId && (
              <div style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}>
                Correlation ID: {details.correlationId}
              </div>
            )}
            {details.errorCode && (
              <div style={{ marginTop: tokens.spacingVerticalXS, fontSize: '12px', opacity: 0.8 }}>
                Error Code: {details.errorCode}
              </div>
            )}
          </div>
        </ToastBody>
        <ToastFooter className={styles.toastFooter}>
          <Button
            size="small"
            appearance="subtle"
            icon={<Copy24Regular />}
            onClick={handleCopyDetails}
          >
            Copy Details
          </Button>
        </ToastFooter>
      </Toast>,
      { intent, timeout: 10000 }
    );
  };

  return { showErrorToast, toasterId };
}

/**
 * Error Toaster component that should be placed at the app root
 */
export function ErrorToaster({ toasterId }: { toasterId: string }) {
  return <Toaster toasterId={toasterId} position="top-end" />;
}
