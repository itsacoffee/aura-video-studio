import { useState } from 'react';
import {
  Button,
  makeStyles,
} from '@fluentui/react-components';
import {
  Video24Regular,
} from '@fluentui/react-icons';
import { createJob } from './api/jobs';

const useStyles = makeStyles({
  button: {
    minWidth: '150px',
  },
});

interface CreateSampleProps {
  onJobCreated?: (jobId: string, correlationId: string) => void;
}

export function CreateSample({ onJobCreated }: CreateSampleProps) {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);

  const handleCreateSample = async () => {
    setLoading(true);
    try {
      const response = await createJob({
        preset: 'sample-hello-youtube',
        options: {
          allowSkipUnavailable: true,
          quality: 'balanced',
        },
      });

      if (onJobCreated) {
        onJobCreated(response.jobId, response.correlationId);
      }
    } catch (error) {
      console.error('Failed to create sample:', error);
      alert('Failed to create sample video. Please check the console for details.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button
      appearance="primary"
      icon={<Video24Regular />}
      onClick={handleCreateSample}
      disabled={loading}
      className={styles.button}
    >
      {loading ? 'Creating...' : 'Try Sample'}
    </Button>
  );
}
