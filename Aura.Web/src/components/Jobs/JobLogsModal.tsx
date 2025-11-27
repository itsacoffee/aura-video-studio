/**
 * JobLogsModal Component
 * Modal dialog for displaying job logs
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  Button,
  makeStyles,
  tokens,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  logsContainer: {
    backgroundColor: tokens.colorNeutralBackground2,
    padding: '12px',
    borderRadius: '4px',
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '400px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
  },
  actionsContainer: {
    marginTop: '16px',
    display: 'flex',
    gap: '8px',
    justifyContent: 'flex-end',
  },
});

interface JobLogsModalProps {
  logs: string | null;
  onClose: () => void;
}

export function JobLogsModal({ logs, onClose }: JobLogsModalProps) {
  const styles = useStyles();

  const handleCopyLogs = () => {
    if (logs) {
      navigator.clipboard.writeText(logs).catch((error) => {
        console.error('Failed to copy logs:', error);
      });
    }
  };

  return (
    <Dialog open={!!logs} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface style={{ minWidth: '600px' }}>
        <DialogBody>
          <DialogTitle>Job Logs</DialogTitle>
          <DialogContent>
            <div className={styles.logsContainer}>{logs || 'No logs available'}</div>
            <div className={styles.actionsContainer}>
              <Button onClick={handleCopyLogs}>Copy Logs</Button>
              <Button appearance="primary" onClick={onClose}>
                Close
              </Button>
            </div>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
