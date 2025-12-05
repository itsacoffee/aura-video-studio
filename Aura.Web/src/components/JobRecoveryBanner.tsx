/**
 * Job Recovery Banner Component
 * Shows a notification when there is a recent job that the user can recover
 */

import { useEffect, useState } from 'react';
import {
  MessageBar,
  MessageBarBody,
  MessageBarActions,
  Button,
  Link,
} from '@fluentui/react-components';
import { useNavigate } from 'react-router-dom';

interface LastJob {
  id: string;
  topic: string;
}

export function JobRecoveryBanner() {
  const [lastJob, setLastJob] = useState<LastJob | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const jobId = sessionStorage.getItem('lastJobId');
    const topic = sessionStorage.getItem('lastJobTopic');

    if (jobId) {
      setLastJob({ id: jobId, topic: topic || 'Unknown' });
    }
  }, []);

  const handleDismiss = () => {
    sessionStorage.removeItem('lastJobId');
    sessionStorage.removeItem('lastJobTopic');
    setLastJob(null);
  };

  const handleViewJob = () => {
    if (lastJob) {
      navigate(`/jobs/${lastJob.id}`);
      handleDismiss();
    }
  };

  if (!lastJob) return null;

  return (
    <MessageBar intent="info" style={{ marginBottom: '16px' }}>
      <MessageBarBody>
        You have a recent job: &quot;{lastJob.topic}&quot;
        <Link onClick={handleViewJob} style={{ marginLeft: '8px' }}>
          View Progress
        </Link>
      </MessageBarBody>
      <MessageBarActions>
        <Button size="small" onClick={handleDismiss}>
          Dismiss
        </Button>
      </MessageBarActions>
    </MessageBar>
  );
}

export default JobRecoveryBanner;
