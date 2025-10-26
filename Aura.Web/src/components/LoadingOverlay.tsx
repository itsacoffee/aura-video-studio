import { makeStyles, tokens, Spinner, Text } from '@fluentui/react-components';
import { ProgressIndicator } from './Loading/ProgressIndicator';

const useStyles = makeStyles({
  overlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    backdropFilter: 'blur(4px)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 9999,
    animation: 'fadeIn 0.2s ease-out',
  },
  content: {
    backgroundColor: tokens.colorNeutralBackground1,
    padding: tokens.spacingVerticalXXL,
    borderRadius: tokens.borderRadiusLarge,
    boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
    minWidth: '300px',
    animation: 'slideInRight 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  message: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    textAlign: 'center',
  },
  progressContainer: {
    width: '100%',
    marginTop: tokens.spacingVerticalM,
  },
});

interface LoadingOverlayProps {
  isVisible: boolean;
  title?: string;
  message?: string;
  progress?: number;
  showProgress?: boolean;
  estimatedTimeRemaining?: number;
  status?: string;
}

export function LoadingOverlay({
  isVisible,
  title = 'Processing...',
  message,
  progress = 0,
  showProgress = false,
  estimatedTimeRemaining,
  status,
}: LoadingOverlayProps) {
  const styles = useStyles();

  if (!isVisible) {
    return null;
  }

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-label={title}>
      <div className={styles.content}>
        <Spinner size="extra-large" aria-label={title} />
        <Text className={styles.title}>{title}</Text>
        {message && <Text className={styles.message}>{message}</Text>}
        {showProgress && (
          <div className={styles.progressContainer}>
            <ProgressIndicator
              progress={progress}
              title=""
              status={status}
              estimatedTimeRemaining={estimatedTimeRemaining}
              showPercentage={true}
              showTimeRemaining={estimatedTimeRemaining !== undefined}
            />
          </div>
        )}
      </div>
    </div>
  );
}
