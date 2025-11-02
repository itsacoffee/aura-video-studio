/**
 * Error Report Dialog Component
 * Displays detailed error information and allows users to report errors to support
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Textarea,
  Text,
  Caption1,
  makeStyles,
  tokens,
  Spinner,
} from '@fluentui/react-components';
import {
  ErrorCircle24Regular,
  Copy24Regular,
  Send24Regular,
  Dismiss24Regular,
  CheckmarkCircle24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { loggingService } from '../services/loggingService';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  errorSection: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
  },
  errorIcon: {
    color: tokens.colorPaletteRedForeground1,
    flexShrink: 0,
  },
  errorText: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  errorTitle: {
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorPaletteRedForeground1,
  },
  errorMessage: {
    wordBreak: 'break-word',
  },
  detailsSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  codeBlock: {
    fontFamily: 'monospace',
    fontSize: '12px',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  successMessage: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    color: tokens.colorPaletteGreenForeground1,
  },
});

export interface ErrorReportDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  error: Error;
  errorInfo?: {
    componentStack?: string;
  };
  context?: Record<string, unknown>;
}

export function ErrorReportDialog({
  open,
  onOpenChange,
  error,
  errorInfo,
  context,
}: ErrorReportDialogProps) {
  const styles = useStyles();
  const [userDescription, setUserDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitSuccess, setSubmitSuccess] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const errorDetails = {
    timestamp: new Date().toISOString(),
    error: {
      name: error.name,
      message: error.message,
      stack: error.stack,
    },
    componentStack: errorInfo?.componentStack,
    context,
    userAgent: navigator.userAgent,
    url: window.location.href,
  };

  const handleCopy = async () => {
    try {
      const errorText = JSON.stringify(errorDetails, null, 2);
      await navigator.clipboard.writeText(errorText);
      setCopied(true);
      loggingService.info('Error details copied to clipboard', 'ErrorReportDialog', 'copy');
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      loggingService.error(
        'Failed to copy error details',
        err as Error,
        'ErrorReportDialog',
        'copy'
      );
    }
  };

  const handleSubmit = async () => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const reportData = {
        ...errorDetails,
        userDescription,
        logs: loggingService.getLogs().slice(-50), // Include last 50 logs for context
      };

      const response = await fetch('/api/error-report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(reportData),
      });

      if (!response.ok) {
        throw new Error(`Failed to submit error report: ${response.statusText}`);
      }

      loggingService.info('Error report submitted successfully', 'ErrorReportDialog', 'submit');
      setSubmitSuccess(true);

      // Close dialog after a short delay
      setTimeout(() => {
        onOpenChange(false);
        setSubmitSuccess(false);
        setUserDescription('');
      }, 2000);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      setSubmitError(errorMessage);
      loggingService.error(
        'Failed to submit error report',
        err as Error,
        'ErrorReportDialog',
        'submit'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    onOpenChange(false);
    setUserDescription('');
    setSubmitSuccess(false);
    setSubmitError(null);
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Error Report</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.errorSection}>
              <ErrorCircle24Regular className={styles.errorIcon} />
              <div className={styles.errorText}>
                <Text className={styles.errorTitle}>An error occurred</Text>
                <Text className={styles.errorMessage}>{error.message}</Text>
              </div>
            </div>

            <div className={styles.detailsSection}>
              <Text weight="semibold">What were you doing when this happened?</Text>
              <Textarea
                placeholder="Please describe what you were trying to do when this error occurred. This information helps us fix the issue."
                value={userDescription}
                onChange={(_, data) => setUserDescription(data.value)}
                rows={4}
                disabled={isSubmitting || submitSuccess}
              />
            </div>

            <div className={styles.detailsSection}>
              <Text weight="semibold">Error Details</Text>
              <Caption1>This technical information will be included in your report</Caption1>
              <div className={styles.codeBlock}>
                <div>
                  <strong>Error: </strong> {error.name}
                </div>
                <div>
                  <strong>Message: </strong> {error.message}
                </div>
                {error.stack && (
                  <div>
                    <strong>Stack Trace:</strong>
                    <pre>{error.stack}</pre>
                  </div>
                )}
              </div>
            </div>

            {submitError && (
              <div className={styles.errorSection}>
                <ErrorCircle24Regular className={styles.errorIcon} />
                <div className={styles.errorText}>
                  <Text className={styles.errorTitle}>Failed to submit report</Text>
                  <Text className={styles.errorMessage}>{submitError}</Text>
                  <Caption1>You can still copy the error details and report it manually.</Caption1>
                </div>
              </div>
            )}

            {submitSuccess && (
              <div className={styles.successMessage}>
                <CheckmarkCircle24Regular />
                <Text>Error report submitted successfully. Thank you!</Text>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button
              appearance="secondary"
              icon={<Copy24Regular />}
              onClick={handleCopy}
              disabled={isSubmitting}
            >
              {copied ? 'Copied!' : 'Copy Details'}
            </Button>
            <Button
              appearance="primary"
              icon={isSubmitting ? <Spinner size="tiny" /> : <Send24Regular />}
              onClick={handleSubmit}
              disabled={isSubmitting || submitSuccess}
            >
              {isSubmitting ? 'Submitting...' : 'Submit Report'}
            </Button>
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={handleClose}
              disabled={isSubmitting}
            >
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
