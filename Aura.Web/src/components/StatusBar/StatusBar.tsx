import { makeStyles, tokens, Button, Badge, Text } from '@fluentui/react-components';
import {
  ErrorCircle24Filled,
  Warning24Filled,
  Info24Filled,
  Dismiss24Regular,
  Copy24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';

const useStyles = makeStyles({
  statusBar: {
    position: 'fixed',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    zIndex: 1000,
    display: 'flex',
    flexDirection: 'column',
  },
  statusBarHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  statusBarHeaderLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  statusBarBody: {
    maxHeight: '300px',
    overflowY: 'auto',
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  errorItem: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  errorHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
  errorIcon: {
    marginTop: '2px',
  },
  errorContent: {
    flex: 1,
  },
  errorMessage: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  errorDetails: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalS,
  },
  errorMetadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  errorActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
    flexWrap: 'wrap',
  },
  errorBg: {
    backgroundColor: tokens.colorPaletteRedBackground1,
  },
  warningBg: {
    backgroundColor: tokens.colorPaletteYellowBackground1,
  },
  infoBg: {
    backgroundColor: tokens.colorNeutralBackground2,
  },
});

export type StatusSeverity = 'error' | 'warning' | 'info';

export interface StatusMessage {
  id: string;
  severity: StatusSeverity;
  message: string;
  details?: string;
  correlationId?: string;
  errorCode?: string;
  timestamp: Date;
  actionLabel?: string;
  actionHandler?: () => void;
  remediation?: string;
}

interface StatusBarProps {
  messages?: StatusMessage[];
  onDismiss?: (messageId: string) => void;
  onDismissAll?: () => void;
}

export function StatusBar({ messages = [], onDismiss, onDismissAll }: StatusBarProps) {
  const styles = useStyles();
  const [isExpanded, setIsExpanded] = useState(false);

  // Auto-expand when new errors appear
  useEffect(() => {
    if (messages.some((m) => m.severity === 'error')) {
      setIsExpanded(true);
    }
  }, [messages]);

  if (messages.length === 0) {
    return null;
  }

  const errorCount = messages.filter((m) => m.severity === 'error').length;
  const warningCount = messages.filter((m) => m.severity === 'warning').length;
  const infoCount = messages.filter((m) => m.severity === 'info').length;

  const getIcon = (severity: StatusSeverity) => {
    switch (severity) {
      case 'error':
        return (
          <ErrorCircle24Filled
            className={styles.errorIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
      case 'warning':
        return (
          <Warning24Filled
            className={styles.errorIcon}
            style={{ color: tokens.colorPaletteYellowForeground1 }}
          />
        );
      case 'info':
        return (
          <Info24Filled
            className={styles.errorIcon}
            style={{ color: tokens.colorBrandForeground1 }}
          />
        );
    }
  };

  const getBgClass = (severity: StatusSeverity) => {
    switch (severity) {
      case 'error':
        return styles.errorBg;
      case 'warning':
        return styles.warningBg;
      case 'info':
        return styles.infoBg;
    }
  };

  const handleCopyDetails = (message: StatusMessage) => {
    const details = {
      message: message.message,
      details: message.details,
      correlationId: message.correlationId,
      errorCode: message.errorCode,
      timestamp: message.timestamp.toISOString(),
      severity: message.severity,
    };
    navigator.clipboard.writeText(JSON.stringify(details, null, 2));
  };

  return (
    <div className={styles.statusBar}>
      <div
        className={styles.statusBarHeader}
        onClick={() => setIsExpanded(!isExpanded)}
        role="button"
        tabIndex={0}
      >
        <div className={styles.statusBarHeaderLeft}>
          {errorCount > 0 && (
            <>
              <ErrorCircle24Filled style={{ color: tokens.colorPaletteRedForeground1 }} />
              <Badge appearance="filled" color="danger">
                {errorCount} {errorCount === 1 ? 'Error' : 'Errors'}
              </Badge>
            </>
          )}
          {warningCount > 0 && (
            <>
              <Warning24Filled style={{ color: tokens.colorPaletteYellowForeground1 }} />
              <Badge appearance="filled" color="warning">
                {warningCount} {warningCount === 1 ? 'Warning' : 'Warnings'}
              </Badge>
            </>
          )}
          {infoCount > 0 && errorCount === 0 && warningCount === 0 && (
            <>
              <Info24Filled style={{ color: tokens.colorBrandForeground1 }} />
              <Text>
                {infoCount} {infoCount === 1 ? 'Message' : 'Messages'}
              </Text>
            </>
          )}
        </div>
        <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
          {onDismissAll && (
            <Button
              size="small"
              appearance="subtle"
              onClick={(e) => {
                e.stopPropagation();
                onDismissAll();
              }}
            >
              Clear All
            </Button>
          )}
        </div>
      </div>

      {isExpanded && (
        <div className={styles.statusBarBody}>
          {messages.map((message) => (
            <div key={message.id} className={`${styles.errorItem} ${getBgClass(message.severity)}`}>
              <div className={styles.errorHeader}>
                {getIcon(message.severity)}
                <div className={styles.errorContent}>
                  <div className={styles.errorMessage}>{message.message}</div>
                  {message.details && <div className={styles.errorDetails}>{message.details}</div>}
                  {message.remediation && (
                    <div className={styles.errorDetails}>
                      <strong>Remediation:</strong> {message.remediation}
                    </div>
                  )}
                  <div className={styles.errorMetadata}>
                    {message.correlationId && <span>Correlation ID: {message.correlationId}</span>}
                    {message.errorCode && <span>Error Code: {message.errorCode}</span>}
                    <span>{message.timestamp.toLocaleTimeString()}</span>
                  </div>
                  <div className={styles.errorActions}>
                    {message.actionLabel && message.actionHandler && (
                      <Button
                        size="small"
                        appearance="primary"
                        icon={<ArrowClockwise24Regular />}
                        onClick={message.actionHandler}
                      >
                        {message.actionLabel}
                      </Button>
                    )}
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<Copy24Regular />}
                      onClick={() => handleCopyDetails(message)}
                    >
                      Copy Details
                    </Button>
                    {onDismiss && (
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<Dismiss24Regular />}
                        onClick={() => onDismiss(message.id)}
                      >
                        Dismiss
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
