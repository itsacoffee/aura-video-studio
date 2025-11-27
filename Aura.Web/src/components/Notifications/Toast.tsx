import {
  makeStyles,
  tokens,
  shorthands,
  Button,
  Toast,
  ToastBody,
  ToastFooter,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Info24Regular,
  Warning24Regular,
  Dismiss24Regular,
  Open24Regular,
} from '@fluentui/react-icons';

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
    animationName: {
      from: {
        transform: 'translateX(100%) translateY(-8px)',
        opacity: 0,
      },
      to: {
        transform: 'translateX(0) translateY(0)',
        opacity: 1,
      },
    },
    animationDuration: '350ms',
    animationTimingFunction: 'cubic-bezier(0.16, 1, 0.3, 1)',
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
    '@media (prefers-reduced-motion: reduce)': {
      animationDuration: '0.01ms',
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

  // Title typography
  toastTitle: {
    fontSize: '15px',
    fontWeight: '600',
    lineHeight: '1.35',
    letterSpacing: '-0.01em',
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

  // Body typography
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
    const iconColors = {
      success: '#34C759',
      error: '#FF453A',
      warning: '#FF9F0A',
      info: '#0A84FF',
    };

    const color = iconColors[type];

    switch (type) {
      case 'success':
        return <CheckmarkCircle24Regular style={{ color }} />;
      case 'error':
        return <ErrorCircle24Regular style={{ color }} />;
      case 'warning':
        return <Warning24Regular style={{ color }} />;
      case 'info':
        return <Info24Regular style={{ color }} />;
      default:
        return null;
    }
  };

  const handleDismiss = () => {
    onDismiss?.();
    onClose?.();
  };

  return (
    <div className={styles.toastContainer}>
      <Toast>
        <div className={styles.toastLayout}>
          <div className={styles.toastHeader}>
            <div className={styles.toastTitleWrapper}>
              <div className={styles.toastIcon}>{getIcon()}</div>
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
              {duration && <div className={styles.metadataText}>Duration: {duration}</div>}
            </ToastBody>
          </div>
          {(showOpenButton || onOpenFile) && (
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
            </ToastFooter>
          )}
        </div>
      </Toast>
    </div>
  );
}
