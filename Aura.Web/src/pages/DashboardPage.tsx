import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Card,
  Spinner,
} from '@fluentui/react-components';
import { Add24Regular, PlayCircle24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiUrl } from '../config/api';
import { useJobState } from '../state/jobState';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  demoInfo: {
    maxWidth: '600px',
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalL,
  },
});

export function DashboardPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [isStartingDemo, setIsStartingDemo] = useState(false);
  const setJob = useJobState((state: { setJob: (jobId: string) => void }) => state.setJob);
  const updateProgress = useJobState(
    (state: { updateProgress: (progress: number, message: string) => void }) => state.updateProgress
  );

  const handleTryDemo = async () => {
    setIsStartingDemo(true);

    try {
      const demoUrl = apiUrl('/api/quick/demo');
      const requestData = { topic: null };

      const response = await fetch(demoUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestData),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Quick Demo failed:', errorText);
        alert('Failed to start Quick Demo. Please check the console for details.');
        return;
      }

      const data = (await response.json()) as { jobId: string };

      setJob(data.jobId);
      updateProgress(0, 'Starting quick demo...');

      navigate('/jobs');
    } catch (error: unknown) {
      console.error('Error starting Quick Demo:', error);
      const errorMessage = error instanceof Error ? error.message : String(error);
      alert(`Error starting Quick Demo: ${errorMessage}`);
    } finally {
      setIsStartingDemo(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>Project Dashboard</Title1>
          <Text className={styles.subtitle}>Manage all your video projects in one place</Text>
        </div>
        <Button
          appearance="primary"
          icon={<Add24Regular />}
          onClick={() => navigate('/create')}
          size="large"
        >
          New Project
        </Button>
      </div>

      <Card className={styles.emptyState}>
        <Title2>Welcome to Aura Video Studio</Title2>
        <Text>Create professional videos with AI-powered automation</Text>

        <div className={styles.buttonGroup}>
          <Button
            appearance="primary"
            icon={isStartingDemo ? <Spinner size="tiny" /> : <PlayCircle24Regular />}
            onClick={handleTryDemo}
            disabled={isStartingDemo}
            size="large"
          >
            {isStartingDemo ? 'Starting Demo...' : 'Try Demo (No Setup Required)'}
          </Button>
          <Button
            appearance="secondary"
            icon={<Add24Regular />}
            onClick={() => navigate('/create')}
            size="large"
          >
            Create Custom Project
          </Button>
        </div>

        <Card className={styles.demoInfo}>
          <Text>
            <strong>Quick Demo:</strong> Experience Aura Video Studio instantly with no API keys or
            configuration needed. The demo uses offline providers (RuleBased script generation +
            Windows TTS) to generate a complete video in seconds. Perfect for trying out the system
            before diving into advanced features.
          </Text>
        </Card>
      </Card>
    </div>
  );
}
