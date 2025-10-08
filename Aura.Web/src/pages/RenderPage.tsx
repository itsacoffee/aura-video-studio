import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  ProgressBar,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import type { RenderJob } from '../types';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
});

export function RenderPage() {
  const styles = useStyles();
  const [jobs, setJobs] = useState<RenderJob[]>([]);
  const [_loading, setLoading] = useState(true);

  useEffect(() => {
    fetchQueue();
  }, []);

  const fetchQueue = async () => {
    try {
      const response = await fetch('/api/queue');
      if (response.ok) {
        const data = await response.json();
        setJobs(data.jobs || []);
      }
    } catch (error) {
      console.error('Error fetching queue:', error);
    } finally {
      setLoading(false);
    }
  };

  const cancelJob = async (jobId: string) => {
    try {
      await fetch(`/api/render/${jobId}/cancel`, { method: 'POST' });
      fetchQueue();
    } catch (error) {
      console.error('Error cancelling job:', error);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Render Queue</Title1>
      </div>

      {jobs.length === 0 ? (
        <Card className={styles.emptyState}>
          <Title2>No render jobs</Title2>
          <Text>Create a video to start rendering</Text>
        </Card>
      ) : (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Job ID</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Progress</TableHeaderCell>
                <TableHeaderCell>Created</TableHeaderCell>
                <TableHeaderCell>Actions</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {jobs.map((job) => (
                <TableRow key={job.id}>
                  <TableCell>{job.id.substring(0, 8)}</TableCell>
                  <TableCell>{job.status}</TableCell>
                  <TableCell>
                    <ProgressBar value={job.progress / 100} />
                  </TableCell>
                  <TableCell>
                    {new Date(job.createdAt).toLocaleString()}
                  </TableCell>
                  <TableCell>
                    {job.status !== 'completed' && job.status !== 'cancelled' && (
                      <Button
                        size="small"
                        icon={<Dismiss24Regular />}
                        onClick={() => cancelJob(job.id)}
                      >
                        Cancel
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
