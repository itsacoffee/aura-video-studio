/**
 * Example: Video Generation Progress with SSE
 *
 * This example demonstrates the VideoGenerationProgress component
 * with real-time updates via Server-Sent Events (SSE).
 */

import {
  Button,
  Text,
  Card,
  makeStyles,
  tokens,
  MessageBar,
  MessageBarBody,
  Input,
  Label,
} from '@fluentui/react-components';
import { Play24Regular, ArrowReset24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import { ComponentErrorBoundary } from '../components/ErrorBoundary/ComponentErrorBoundary';
import { VideoGenerationProgress } from '../components/VideoGenerationProgress';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '900px',
    margin: '0 auto',
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  formField: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  infoCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  codeBlock: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    backgroundColor: tokens.colorNeutralBackground1,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusSmall,
    overflowX: 'auto',
  },
});

export const VideoGenerationProgressExample: FC = () => {
  const styles = useStyles();
  const [jobId, setJobId] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [currentJobId, setCurrentJobId] = useState('');
  const [completedVideo, setCompletedVideo] = useState<{
    videoUrl: string;
    videoPath: string;
  } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleStartGeneration = async () => {
    if (!jobId.trim()) {
      setError('Please enter a job ID');
      return;
    }

    setError(null);
    setCompletedVideo(null);
    setCurrentJobId(jobId);
    setIsGenerating(true);
  };

  const handleComplete = (result: { videoUrl: string; videoPath: string }) => {
    setCompletedVideo(result);
    setIsGenerating(false);
  };

  const handleError = (err: Error) => {
    setError(err.message);
    setIsGenerating(false);
  };

  const handleCancel = () => {
    setIsGenerating(false);
    setCurrentJobId('');
  };

  const handleReset = () => {
    setJobId('');
    setCurrentJobId('');
    setIsGenerating(false);
    setCompletedVideo(null);
    setError(null);
  };

  return (
    <ComponentErrorBoundary componentName="VideoGenerationProgressExample">
      <div className={styles.container}>
        <div className={styles.section}>
          <Text size={800} weight="bold">
            Video Generation Progress Example
          </Text>
          <Text>
            This example demonstrates real-time video generation progress tracking using Server-Sent
            Events (SSE). The component shows live updates for each stage of the video generation
            process.
          </Text>
        </div>

        <Card className={styles.infoCard}>
          <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalM }}>
            How to Use:
          </Text>
          <ol style={{ margin: 0, paddingLeft: tokens.spacingHorizontalL }}>
            <li>Start a video generation job using the API or wizard</li>
            <li>Copy the job ID from the response</li>
            <li>Paste the job ID below and click &ldquo;Start Monitoring&rdquo;</li>
            <li>Watch real-time progress updates via SSE</li>
          </ol>
        </Card>

        {!isGenerating && (
          <div className={styles.section}>
            <Card>
              <div style={{ padding: tokens.spacingVerticalL }}>
                <div className={styles.formField}>
                  <Label htmlFor="jobId" weight="semibold">
                    Job ID
                  </Label>
                  <Input
                    id="jobId"
                    value={jobId}
                    onChange={(_, data) => setJobId(data.value)}
                    placeholder="e.g., abc123-def456-ghi789"
                    disabled={isGenerating}
                  />
                  <Text size={200}>
                    Enter the job ID returned from the video generation API endpoint
                  </Text>
                </div>

                <div className={styles.controls} style={{ marginTop: tokens.spacingVerticalL }}>
                  <Button
                    appearance="primary"
                    icon={<Play24Regular />}
                    onClick={handleStartGeneration}
                    disabled={isGenerating}
                  >
                    Start Monitoring
                  </Button>
                  <Button
                    appearance="secondary"
                    icon={<ArrowReset24Regular />}
                    onClick={handleReset}
                  >
                    Reset
                  </Button>
                </div>
              </div>
            </Card>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>
                  <Text weight="semibold">Error</Text>
                  <Text>{error}</Text>
                </MessageBarBody>
              </MessageBar>
            )}

            {completedVideo && (
              <MessageBar intent="success">
                <MessageBarBody>
                  <Text weight="semibold">Generation Complete!</Text>
                  <Text>Video URL: {completedVideo.videoUrl}</Text>
                  <Text>Video Path: {completedVideo.videoPath}</Text>
                </MessageBarBody>
              </MessageBar>
            )}
          </div>
        )}

        {isGenerating && currentJobId && (
          <VideoGenerationProgress
            jobId={currentJobId}
            onComplete={handleComplete}
            onError={handleError}
            onCancel={handleCancel}
          />
        )}

        <Card className={styles.infoCard}>
          <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalM }}>
            SSE Events:
          </Text>
          <Text block style={{ marginBottom: tokens.spacingVerticalS }}>
            The component subscribes to the following SSE events from{' '}
            <code>/api/jobs/&#123;jobId&#125;/events</code>:
          </Text>
          <ul style={{ margin: 0, paddingLeft: tokens.spacingHorizontalL }}>
            <li>
              <strong>job-status</strong> - Overall job status and progress percentage
            </li>
            <li>
              <strong>step-progress</strong> - Detailed progress within current step
            </li>
            <li>
              <strong>step-status</strong> - Step start/completion notifications
            </li>
            <li>
              <strong>job-completed</strong> - Success with video download information
            </li>
            <li>
              <strong>job-failed</strong> - Error details and failure information
            </li>
            <li>
              <strong>job-cancelled</strong> - Cancellation confirmation
            </li>
          </ul>
        </Card>

        <Card className={styles.infoCard}>
          <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalM }}>
            Example API Call:
          </Text>
          <div className={styles.codeBlock}>
            <pre style={{ margin: 0 }}>
              {`// Start video generation
const response = await fetch('/api/jobs', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    brief: {
      topic: 'Introduction to AI',
      audience: 'Students',
      goal: 'Educate',
      tone: 'Friendly',
      language: 'en',
      aspect: 'Widescreen16x9'
    },
    planSpec: {
      targetDuration: '00:01:00',
      pacing: 'Conversational',
      density: 'Balanced',
      style: 'Modern'
    },
    voiceSpec: {
      voiceName: 'David',
      rate: 1.0,
      pitch: 1.0,
      pause: 'Natural'
    },
    renderSpec: {
      res: '1080p',
      container: 'mp4',
      videoBitrateK: 5000,
      audioBitrateK: 192,
      fps: 30,
      codec: 'h264',
      qualityLevel: 'High'
    }
  })
});

const data = await response.json();
console.info('Job ID:', data.jobId);

// Use the job ID with VideoGenerationProgress component
<VideoGenerationProgress jobId={data.jobId} />`}
            </pre>
          </div>
        </Card>

        <Card className={styles.infoCard}>
          <Text weight="semibold" block style={{ marginBottom: tokens.spacingVerticalM }}>
            Features:
          </Text>
          <ul style={{ margin: 0, paddingLeft: tokens.spacingHorizontalL }}>
            <li>Real-time progress updates via SSE (no polling)</li>
            <li>Stage-based progress indicators (5 stages)</li>
            <li>Elapsed time and estimated time remaining</li>
            <li>Cancel generation with confirmation dialog</li>
            <li>Automatic reconnection on network interruption</li>
            <li>Success state with download button</li>
            <li>Error state with detailed error messages</li>
            <li>Live connection indicator</li>
          </ul>
        </Card>
      </div>
    </ComponentErrorBoundary>
  );
};

export default VideoGenerationProgressExample;
