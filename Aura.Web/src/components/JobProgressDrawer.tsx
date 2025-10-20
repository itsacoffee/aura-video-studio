import { useState, useEffect } from 'react';
import {
  Drawer,
  DrawerHeader,
  DrawerBody,
  Button,
  Text,
  ProgressBar,
  makeStyles,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  drawer: {
    width: '400px',
  },
  progressSection: {
    marginBottom: '20px',
  },
  logEntry: {
    padding: '8px',
    borderBottom: '1px solid #e0e0e0',
    fontSize: '12px',
    fontFamily: 'monospace',
  },
  logContainer: {
    maxHeight: '400px',
    overflowY: 'auto',
  },
});

export interface JobProgressDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  jobId: string;
}

export function JobProgressDrawer({ isOpen, onClose, jobId }: JobProgressDrawerProps) {
  const styles = useStyles();
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('Running');
  const [logs, setLogs] = useState<string[]>([]);

  useEffect(() => {
    if (!isOpen || !jobId) {
      return;
    }

    const pollProgress = async () => {
      try {
        // Fetch job progress
        const progressResponse = await fetch(`/api/render/${jobId}/progress`);
        if (progressResponse.ok) {
          const progressData = await progressResponse.json();
          setProgress(progressData.progress);
          setStatus(progressData.status);
        }

        // Fetch logs filtered by job ID
        const logsResponse = await fetch(`/api/logs?limit=100&search=${jobId}`);
        if (logsResponse.ok) {
          const logsData = await logsResponse.json();
          if (logsData.logs && Array.isArray(logsData.logs)) {
            const messages = logsData.logs.map((log: any) => log.message || String(log));
            setLogs(messages);
          }
        }
      } catch (error) {
        console.error('Error polling job progress:', error);
      }
    };

    // Poll immediately
    pollProgress();

    // Set up polling interval
    const interval = setInterval(() => {
      pollProgress();
      
      // Stop polling if completed or failed
      if (status === 'completed' || status === 'failed') {
        clearInterval(interval);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [isOpen, jobId, status]);

  return (
    <Drawer
      open={isOpen}
      position="end"
      className={styles.drawer}
    >
      <DrawerHeader>
        <Text weight="semibold">Job Progress</Text>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={onClose}
        />
      </DrawerHeader>
      <DrawerBody>
        <div className={styles.progressSection}>
          <Text>Status: {status}</Text>
          <ProgressBar value={progress / 100} />
          <Text>{progress}%</Text>
        </div>
        
        <Text weight="semibold">Logs:</Text>
        <div className={styles.logContainer}>
          {logs.map((log, index) => (
            <div key={index} className={styles.logEntry}>
              {log}
            </div>
          ))}
        </div>
      </DrawerBody>
    </Drawer>
  );
}
