import {
  makeStyles,
  tokens,
  Button,
  Toast,
  ToastTitle,
  ToastBody,
  ToastFooter,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Dismiss24Regular,
  Open24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  toastFooter: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
});

export interface ToastNotificationOptions {
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  duration?: string;
  onOpenFile?: () => void;
  onDismiss?: () => void;
  showOpenButton?: boolean;
}

interface ToastNotificationProps extends ToastNotificationOptions {
  onClose?: () => void;
}

export function ToastNotification({
  type,
  title,
  message,
  duration,
  onOpenFile,
  onDismiss,
  onClose,
  showOpenButton = false,
}: ToastNotificationProps) {
  const styles = useStyles();

  const getIcon = () => {
    switch (type) {
      case 'success':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'error':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      default:
        return null;
    }
  };

  const handleDismiss = () => {
    onDismiss?.();
    onClose?.();
  };

  return (
    <Toast>
      <ToastTitle action={getIcon()}>{title}</ToastTitle>
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
      {(showOpenButton || onOpenFile) && (
        <ToastFooter className={styles.toastFooter}>
          {onOpenFile && (
            <Button size="small" appearance="primary" icon={<Open24Regular />} onClick={onOpenFile}>
              Open File
            </Button>
          )}
          <Button
            size="small"
            appearance="subtle"
            icon={<Dismiss24Regular />}
            onClick={handleDismiss}
          >
            Dismiss
          </Button>
        </ToastFooter>
      )}
    </Toast>
  );
}
