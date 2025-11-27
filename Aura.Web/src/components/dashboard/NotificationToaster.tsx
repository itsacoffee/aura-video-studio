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
  CheckmarkCircle20Filled,
  Warning20Filled,
  ErrorCircle20Filled,
  Info20Filled,
} from '@fluentui/react-icons';
import { useEffect } from 'react';
import { useNotificationStore, type NotificationType } from '../../state/notifications';

const useStyles = makeStyles({
  // Modern toast styling inspired by macOS/Windows 11 notifications
  toast: {
    // Override FluentUI default toast styles for a cleaner look
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

  // Toaster container overrides to remove extra wrapper styling
  toaster: {
    // Remove any default borders or backgrounds from the toaster container
    '& > div': {
      ...shorthands.border('none'),
      background: 'transparent',
    },
  },

  // Content layout - horizontal alignment
  content: {
    display: 'flex',
    alignItems: 'flex-start',
    ...shorthands.gap('10px'),
  },

  // Icon styling
  icon: {
    flexShrink: 0,
    marginTop: '2px',
  },

  // Text container
  textContainer: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap('2px'),
    minWidth: 0,
    ...shorthands.flex(1),
  },

  // Title styling - compact and readable
  title: {
    fontSize: '13px',
    fontWeight: '600',
    lineHeight: '1.4',
    color: tokens.colorNeutralForeground1,
    ...shorthands.margin(0),
    ...shorthands.padding(0),
    '@media (prefers-color-scheme: dark)': {
      color: 'rgba(255, 255, 255, 0.95)',
    },
  },

  // Body styling - secondary text
  body: {
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

  // Icon colors - Apple/SF Symbols inspired
  iconSuccess: {
    color: '#34C759',
  },
  iconWarning: {
    color: '#FF9500',
  },
  iconError: {
    color: '#FF3B30',
  },
  iconInfo: {
    color: '#007AFF',
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
        <Toast className={styles.toast}>
          <div className={styles.content}>
            <span className={styles.icon}>{iconElement}</span>
            <div className={styles.textContainer}>
              <ToastTitle className={styles.title}>{latestNotification.title}</ToastTitle>
              <ToastBody className={styles.body}>{latestNotification.message}</ToastBody>
            </div>
          </div>
        </Toast>,
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
        return <CheckmarkCircle20Filled className={iconStyles.iconSuccess} />;
      case 'warning':
        return <Warning20Filled className={iconStyles.iconWarning} />;
      case 'error':
        return <ErrorCircle20Filled className={iconStyles.iconError} />;
      case 'info':
        return <Info20Filled className={iconStyles.iconInfo} />;
    }
  };

  return (
    <Toaster
      toasterId={toasterId}
      position="top-end"
      offset={{ horizontal: 20, vertical: 20 }}
      pauseOnHover
      pauseOnWindowBlur
      limit={3}
      className={styles.toaster}
    />
  );
}
