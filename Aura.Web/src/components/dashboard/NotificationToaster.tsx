import {
  Toast,
  ToastTitle,
  ToastBody,
  Toaster,
  useToastController,
  useId,
  ToastIntent,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  Warning24Filled,
  ErrorCircle24Filled,
  Info24Filled,
} from '@fluentui/react-icons';
import { useEffect } from 'react';
import { useNotificationStore, type NotificationType } from '../../state/notifications';

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

  // Header row with icon and title
  toastHeader: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap('14px'),
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
    paddingLeft: '38px',
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.80)',
    },
  },

  // Success icon color
  iconSuccess: {
    color: '#34C759',
  },

  // Warning icon color
  iconWarning: {
    color: '#FF9F0A',
  },

  // Error icon color
  iconError: {
    color: '#FF453A',
  },

  // Info icon color
  iconInfo: {
    color: '#0A84FF',
  },
});

export function NotificationToaster() {
  const toasterId = useId('toaster');
  const { dispatchToast } = useToastController(toasterId);
  const { notifications } = useNotificationStore();
  const styles = useStyles();

  useEffect(() => {
    const latestNotification = notifications[0];
    if (latestNotification && !latestNotification.read) {
      const intent = getToastIntent(latestNotification.type);
      const iconElement = getIcon(latestNotification.type, styles);

      dispatchToast(
        <div className={styles.toastContainer}>
          <Toast>
            <div className={styles.toastLayout}>
              <div className={styles.toastHeader}>
                <div className={styles.toastIcon}>{iconElement}</div>
                <ToastTitle className={styles.toastTitle}>{latestNotification.title}</ToastTitle>
              </div>
              <ToastBody className={styles.toastBody}>{latestNotification.message}</ToastBody>
            </div>
          </Toast>
        </div>,
        { intent, timeout: 5000 }
      );
    }
  }, [notifications, dispatchToast, styles]);

  const getToastIntent = (type: NotificationType): ToastIntent => {
    switch (type) {
      case 'success':
        return 'success';
      case 'warning':
        return 'warning';
      case 'error':
        return 'error';
      case 'info':
        return 'info';
    }
  };

  const getIcon = (type: NotificationType, iconStyles: ReturnType<typeof useStyles>) => {
    switch (type) {
      case 'success':
        return <CheckmarkCircle24Filled className={iconStyles.iconSuccess} />;
      case 'warning':
        return <Warning24Filled className={iconStyles.iconWarning} />;
      case 'error':
        return <ErrorCircle24Filled className={iconStyles.iconError} />;
      case 'info':
        return <Info24Filled className={iconStyles.iconInfo} />;
    }
  };

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
