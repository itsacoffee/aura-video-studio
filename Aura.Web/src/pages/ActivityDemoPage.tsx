import { makeStyles, tokens, Title1, Title2, Text, Button, Card } from '@fluentui/react-components';
import { Play24Regular, ErrorCircle24Regular, Checkmark24Regular } from '@fluentui/react-icons';
import { useActivity } from '../state/activityContext';

const useStyles = makeStyles({
  container: {
    maxWidth: '800px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  section: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalL,
  },
  buttonGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalL,
  },
});

/**
 * Demo page to test the GlobalStatusFooter functionality
 * This page allows testing various activity scenarios without needing the backend API
 */
export function ActivityDemoPage() {
  const styles = useStyles();
  const { addActivity, updateActivity } = useActivity();

  const simulateVideoGeneration = () => {
    const id = addActivity({
      type: 'video-generation',
      title: 'Generating Video',
      message: 'Creating video about "AI in Healthcare"',
      canCancel: true,
      canRetry: true,
    });

    // Simulate progress updates
    let progress = 0;
    const interval = setInterval(() => {
      progress += 10;

      if (progress <= 30) {
        updateActivity(id, {
          status: 'running',
          progress,
          message: 'Generating script...',
        });
      } else if (progress <= 60) {
        updateActivity(id, {
          status: 'running',
          progress,
          message: 'Creating visuals...',
        });
      } else if (progress <= 90) {
        updateActivity(id, {
          status: 'running',
          progress,
          message: 'Rendering video...',
        });
      } else if (progress >= 100) {
        updateActivity(id, {
          status: 'completed',
          progress: 100,
          message: 'Video generated successfully!',
        });
        clearInterval(interval);
      }
    }, 500);
  };

  const simulateFailedActivity = () => {
    const id = addActivity({
      type: 'api-call',
      title: 'API Request',
      message: 'Fetching data from server...',
      canRetry: true,
    });

    // Simulate a failure after 2 seconds
    setTimeout(() => {
      updateActivity(id, {
        status: 'failed',
        message: 'Request failed',
        error: 'Network error: Unable to connect to server (ERR_CONNECTION_REFUSED)',
      });
    }, 2000);
  };

  const simulateQuickSuccess = () => {
    const id = addActivity({
      type: 'file-upload',
      title: 'File Upload',
      message: 'Uploading test.mp4...',
    });

    let progress = 0;
    const interval = setInterval(() => {
      progress += 20;

      if (progress >= 100) {
        updateActivity(id, {
          status: 'completed',
          progress: 100,
          message: 'File uploaded successfully!',
        });
        clearInterval(interval);
      } else {
        updateActivity(id, {
          status: 'running',
          progress,
          message: `Uploading... ${progress}%`,
        });
      }
    }, 300);
  };

  const simulateMultipleActivities = () => {
    // Start 3 different activities simultaneously
    simulateVideoGeneration();
    setTimeout(() => simulateQuickSuccess(), 500);
    setTimeout(() => simulateFailedActivity(), 1000);
  };

  const simulateAnalysis = () => {
    const id = addActivity({
      type: 'analysis',
      title: 'Video Analysis',
      message: 'Analyzing video content...',
      canCancel: true,
    });

    let progress = 0;
    const interval = setInterval(() => {
      progress += 5;

      if (progress >= 100) {
        updateActivity(id, {
          status: 'completed',
          progress: 100,
          message: 'Analysis complete!',
        });
        clearInterval(interval);
      } else {
        updateActivity(id, {
          status: 'running',
          progress,
          message: `Analyzing... ${progress}%`,
        });
      }
    }, 800);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Activity Status Footer Demo</Title1>
        <Text className={styles.description}>
          This page demonstrates the global activity status footer functionality. Click the buttons
          below to simulate various activities and see how they appear in the footer at the bottom
          of the page.
        </Text>
      </div>

      <Card className={styles.section}>
        <Title2>Single Activities</Title2>
        <Text className={styles.description}>
          Test individual activity types to see how they display in the footer.
        </Text>
        <div className={styles.buttonGrid}>
          <Button appearance="primary" icon={<Play24Regular />} onClick={simulateVideoGeneration}>
            Video Generation
          </Button>
          <Button appearance="primary" icon={<Checkmark24Regular />} onClick={simulateQuickSuccess}>
            Quick Success
          </Button>
          <Button
            appearance="primary"
            icon={<ErrorCircle24Regular />}
            onClick={simulateFailedActivity}
          >
            Failed Request
          </Button>
          <Button appearance="primary" onClick={simulateAnalysis}>
            Video Analysis
          </Button>
        </div>
      </Card>

      <Card className={styles.section}>
        <Title2>Multiple Concurrent Activities</Title2>
        <Text className={styles.description}>
          Test how the footer handles multiple activities running at the same time.
        </Text>
        <Button appearance="primary" size="large" onClick={simulateMultipleActivities}>
          Start Multiple Activities
        </Button>
      </Card>

      <Card className={styles.section}>
        <Title2>Features to Test</Title2>
        <ul>
          <li>Activity progress bars and percentage</li>
          <li>Real-time status updates</li>
          <li>Success notifications (green checkmark)</li>
          <li>Error indicators (red error icon with details)</li>
          <li>Collapsible footer (click header to expand/collapse)</li>
          <li>Activity history with timestamps</li>
          <li>Dismiss completed/failed activities</li>
          <li>Clear all completed button</li>
          <li>Multiple concurrent activities display</li>
        </ul>
      </Card>
    </div>
  );
}
