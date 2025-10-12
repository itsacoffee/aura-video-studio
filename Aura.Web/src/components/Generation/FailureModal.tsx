import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  Title3,
} from '@fluentui/react-components';
import {
  Dismiss24Regular,
  Copy24Regular,
  Folder24Regular,
  ArrowClockwise24Regular,
  Wrench24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import { JobFailure } from '../../state/jobs';
import { openLogsFolder } from '../../utils/apiErrorHandler';

const useStyles = makeStyles({
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  code: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '200px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  correlationId: {
    fontFamily: 'monospace',
    fontSize: '13px',
    padding: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    display: 'inline-block',
  },
  actionsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalL,
  },
  actionItem: {
    listStyleType: 'disc',
  },
});

interface FailureModalProps {
  open: boolean;
  onClose: () => void;
  failure: JobFailure;
  jobId: string;
}

export function FailureModal({ open, onClose, failure, jobId: _jobId }: FailureModalProps) {
  const styles = useStyles();
  const [copied, setCopied] = useState(false);
  const [repairing, setRepairing] = useState(false);

  const handleCopyCorrelationId = () => {
    navigator.clipboard.writeText(failure.correlationId);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleViewFullLog = () => {
    if (failure.logPath) {
      // Try to open the log file directly
      window.open(`file:///${failure.logPath.replace(/\\/g, '/')}`, '_blank');
    } else {
      // Fallback to opening logs folder
      openLogsFolder();
    }
  };

  const handleRetry = () => {
    onClose();
    // User will need to start a new generation from the main UI
  };

  const handleRepairFFmpeg = async () => {
    setRepairing(true);
    try {
      const response = await fetch('/api/downloads/ffmpeg/repair', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      if (response.ok) {
        alert('FFmpeg repair initiated. Please wait for it to complete.');
      } else {
        alert('Failed to start FFmpeg repair. Please try manually from the Dependencies page.');
      }
    } catch (error) {
      console.error('Error repairing FFmpeg:', error);
      alert('Error initiating FFmpeg repair.');
    } finally {
      setRepairing(false);
    }
  };

  const handleAttachFFmpeg = async () => {
    // Open dependencies page where user can attach FFmpeg
    window.location.href = '/dependencies';
  };

  const isFFmpegError = failure.errorCode?.includes('FFMPEG') || 
                        failure.message.toLowerCase().includes('ffmpeg');

  return (
    <Dialog open={open} onOpenChange={(_, data) => data.open || onClose()}>
      <DialogSurface style={{ maxWidth: '600px' }}>
        <DialogTitle
          action={
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={onClose}
            />
          }
        >
          Generation Failed
        </DialogTitle>
        <DialogBody>
          <DialogContent className={styles.dialogContent}>
            {/* Error Message */}
            <div className={styles.section}>
              <Title3>Error</Title3>
              <Text>{failure.message}</Text>
              {failure.errorCode && (
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Error Code: {failure.errorCode}
                </Text>
              )}
            </div>

            {/* Correlation ID */}
            <div className={styles.section}>
              <Title3>Correlation ID</Title3>
              <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                <span className={styles.correlationId}>{failure.correlationId}</span>
                <Button
                  size="small"
                  appearance="subtle"
                  icon={<Copy24Regular />}
                  onClick={handleCopyCorrelationId}
                >
                  {copied ? 'Copied!' : 'Copy'}
                </Button>
              </div>
            </div>

            {/* Stderr Snippet */}
            {failure.stderrSnippet && (
              <div className={styles.section}>
                <Title3>Error Output (last 16KB)</Title3>
                <div className={styles.code}>{failure.stderrSnippet}</div>
              </div>
            )}

            {/* Install Log Snippet */}
            {failure.installLogSnippet && (
              <div className={styles.section}>
                <Title3>Install Log</Title3>
                <div className={styles.code}>{failure.installLogSnippet}</div>
              </div>
            )}

            {/* Suggested Actions */}
            {failure.suggestedActions && failure.suggestedActions.length > 0 && (
              <div className={styles.section}>
                <Title3>Suggested Actions</Title3>
                <ul className={styles.actionsList}>
                  {failure.suggestedActions.map((action, index) => (
                    <li key={index} className={styles.actionItem}>
                      <Text>{action}</Text>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </DialogContent>
        </DialogBody>
        <DialogActions>
          <Button
            appearance="secondary"
            icon={<Folder24Regular />}
            onClick={handleViewFullLog}
          >
            View Full Log
          </Button>
          {isFFmpegError && (
            <>
              <Button
                appearance="secondary"
                icon={<Wrench24Regular />}
                onClick={handleRepairFFmpeg}
                disabled={repairing}
              >
                {repairing ? 'Repairing...' : 'Repair FFmpeg'}
              </Button>
              <Button
                appearance="secondary"
                icon={<Settings24Regular />}
                onClick={handleAttachFFmpeg}
              >
                Attach FFmpeg
              </Button>
            </>
          )}
          <Button
            appearance="primary"
            icon={<ArrowClockwise24Regular />}
            onClick={handleRetry}
          >
            Close & Retry
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
}
