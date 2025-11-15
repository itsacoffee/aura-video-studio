import { makeStyles, tokens, Text, Spinner } from '@fluentui/react-components';
import { Checkmark20Regular, CloudSync20Regular } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
    fontSize: tokens.fontSizeBase200,
  },
  saving: {
    color: tokens.colorBrandForeground1,
  },
  saved: {
    color: tokens.colorPaletteGreenForeground1,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
  },
  icon: {
    display: 'flex',
    alignItems: 'center',
  },
});

export type AutoSaveStatus = 'idle' | 'saving' | 'saved' | 'error';

export interface AutoSaveIndicatorProps {
  status: AutoSaveStatus;
  lastSaved?: Date | null;
  error?: string | null;
}

export function AutoSaveIndicator({ status, lastSaved, error }: AutoSaveIndicatorProps) {
  const styles = useStyles();
  const [showStatus, setShowStatus] = useState(false);

  useEffect(() => {
    if (status === 'saving' || status === 'saved' || status === 'error') {
      setShowStatus(true);

      if (status === 'saved') {
        const timer = setTimeout(() => {
          setShowStatus(false);
        }, 3000);
        return () => clearTimeout(timer);
      }
    }
  }, [status]);

  if (!showStatus || status === 'idle') {
    return null;
  }

  const getStatusContent = () => {
    switch (status) {
      case 'saving':
        return {
          icon: <Spinner size="tiny" />,
          text: 'Saving progress...',
          className: styles.saving,
        };
      case 'saved':
        return {
          icon: <Checkmark20Regular />,
          text: 'Progress saved',
          className: styles.saved,
        };
      case 'error':
        return {
          icon: <CloudSync20Regular />,
          text: error || 'Save failed',
          className: styles.error,
        };
      default:
        return null;
    }
  };

  const content = getStatusContent();
  if (!content) {
    return null;
  }

  return (
    <div className={`${styles.container} ${content.className}`}>
      <div className={styles.icon}>{content.icon}</div>
      <Text size={200}>{content.text}</Text>
      {status === 'saved' && lastSaved && (
        <Text size={100} style={{ opacity: 0.7 }}>
          {lastSaved.toLocaleTimeString()}
        </Text>
      )}
    </div>
  );
}
