/**
 * Example: Real-Time Video Generation Progress Monitoring
 *
 * This example demonstrates how to use SSE (Server-Sent Events) to monitor
 * video generation progress in real-time and cancel jobs if needed.
 */

import { useState, useCallback } from 'react';
import { subscribeToJobEvents, cancelJob, type JobEvent } from '@/features/render/api/jobs';
import { useJobProgress } from '@/hooks/useJobProgress';

// Example 1: Basic Progress Monitoring with React Hook
export function BasicProgressMonitor({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const [stage, setStage] = useState('Starting...');
  const [status, setStatus] = useState<'idle' | 'running' | 'completed' | 'failed'>('idle');

  useJobProgress(jobId, (event: JobEvent) => {
    switch (event.type) {
      case 'step-progress': {
        const data = event.data as {
          progressPct: number;
          step: string;
          message: string;
        };
        setProgress(data.progressPct);
        setStage(data.step);
        break;
      }

      case 'job-completed': {
        setStatus('completed');
        setProgress(100);
        break;
      }

      case 'job-failed': {
        setStatus('failed');
        break;
      }

      case 'job-status': {
        const statusData = event.data as { status: string };
        if (statusData.status === 'Running') {
          setStatus('running');
        }
        break;
      }

      default:
        break;
    }
  });

  return (
    <div>
      <h3>Video Generation Progress</h3>
      <div>Status: {status}</div>
      <div>Stage: {stage}</div>
      <progress value={progress} max={100} />
      <div>{progress}% complete</div>
    </div>
  );
}

// Example 2: Progress Monitoring with Cancellation
export function ProgressWithCancel({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('Running');
  const [isCancelling, setIsCancelling] = useState(false);

  useJobProgress(jobId, (event: JobEvent) => {
    if (event.type === 'step-progress') {
      const progressData = event.data as { progressPct: number };
      setProgress(progressData.progressPct);
    } else if (event.type === 'job-status') {
      const statusData = event.data as { status: string };
      setStatus(statusData.status);
    }
  });

  const handleCancel = useCallback(async () => {
    if (window.confirm('Are you sure you want to cancel this video generation?')) {
      setIsCancelling(true);
      try {
        await cancelJob(jobId);
        setStatus('Cancelled');
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        alert(`Failed to cancel job: ${errorMessage}`);
      } finally {
        setIsCancelling(false);
      }
    }
  }, [jobId]);

  return (
    <div>
      <h3>Video Generation</h3>
      <progress value={progress} max={100} />
      <p>
        {progress}% - Status: {status}
      </p>
      {status === 'Running' && (
        <button onClick={handleCancel} disabled={isCancelling}>
          {isCancelling ? 'Cancelling...' : 'Cancel'}
        </button>
      )}
    </div>
  );
}

// Example 3: Detailed Progress with All Event Types
export function DetailedProgressMonitor({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const [currentStep, setCurrentStep] = useState('');
  const [message, setMessage] = useState('');
  const [warnings, setWarnings] = useState<string[]>([]);
  const [eta, setEta] = useState<string | null>(null);
  const [artifacts, setArtifacts] = useState<
    Array<{ name: string; sizeBytes: number; type: string; path: string }>
  >([]);

  useJobProgress(jobId, (event: JobEvent) => {
    switch (event.type) {
      case 'step-progress': {
        const progressData = event.data as {
          progressPct: number;
          step: string;
          message: string;
          estimatedTimeRemaining?: string;
        };
        setProgress(progressData.progressPct);
        setCurrentStep(progressData.step);
        setMessage(progressData.message);
        if (progressData.estimatedTimeRemaining) {
          setEta(progressData.estimatedTimeRemaining);
        }
        break;
      }

      case 'warning': {
        const warningData = event.data as { message: string };
        setWarnings((prev) => [...prev, warningData.message]);
        break;
      }

      case 'job-completed': {
        const completionData = event.data as {
          artifacts: Array<{ name: string; sizeBytes: number; type: string; path: string }>;
        };
        setArtifacts(completionData.artifacts);
        setProgress(100);
        setMessage('Completed successfully!');
        break;
      }

      case 'job-failed': {
        const errorData = event.data as { errorMessage: string };
        setMessage(`Failed: ${errorData.errorMessage}`);
        break;
      }

      default:
        break;
    }
  });

  return (
    <div>
      <h3>Detailed Progress Monitor</h3>

      {/* Progress Bar */}
      <div>
        <progress value={progress} max={100} style={{ width: '100%' }} />
        <div>{progress}%</div>
      </div>

      {/* Current Step */}
      <div>
        <strong>Current Step:</strong> {currentStep}
      </div>

      {/* Status Message */}
      <div>
        <strong>Status:</strong> {message}
      </div>

      {/* ETA */}
      {eta && (
        <div>
          <strong>Estimated Time Remaining:</strong> {eta}
        </div>
      )}

      {/* Warnings */}
      {warnings.length > 0 && (
        <div style={{ color: 'orange' }}>
          <strong>Warnings:</strong>
          <ul>
            {warnings.map((warning, i) => (
              <li key={`warning-${warning}-${i}`}>{warning}</li>
            ))}
          </ul>
        </div>
      )}

      {/* Artifacts */}
      {artifacts.length > 0 && (
        <div>
          <strong>Generated Files:</strong>
          <ul>
            {artifacts.map((artifact, i) => (
              <li key={`artifact-${artifact.name}-${i}`}>
                {artifact.name} ({(artifact.sizeBytes / 1024 / 1024).toFixed(2)} MB)
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

// Example 4: Low-Level Direct SSE Usage (without React hook)
export function DirectSseUsage({ jobId }: { jobId: string }) {
  const [events, setEvents] = useState<Array<{ type: string; data: unknown }>>([]);
  const [unsubscribe, setUnsubscribe] = useState<(() => void) | null>(null);

  const startMonitoring = useCallback(() => {
    // Subscribe to job events
    const unsub = subscribeToJobEvents(
      jobId,
      (event) => {
        // Add event to list
        setEvents((prev) => [...prev, { type: event.type, data: event.data }]);

        // Auto-unsubscribe on terminal events
        if (event.type === 'job-completed' || event.type === 'job-failed') {
          setTimeout(() => unsub(), 1000);
        }
      },
      (error) => {
        setEvents((prev) => [...prev, { type: 'error', data: { message: error.message } }]);
      }
    );

    setUnsubscribe(() => unsub);
  }, [jobId]);

  const stopMonitoring = useCallback(() => {
    if (unsubscribe) {
      unsubscribe();
      setUnsubscribe(null);
    }
  }, [unsubscribe]);

  return (
    <div>
      <h3>Direct SSE Event Log</h3>
      <button onClick={startMonitoring} disabled={!!unsubscribe}>
        Start Monitoring
      </button>
      <button onClick={stopMonitoring} disabled={!unsubscribe}>
        Stop Monitoring
      </button>

      <div style={{ maxHeight: '400px', overflow: 'auto', marginTop: '1rem' }}>
        {events.map((event, i) => (
          <div
            key={`event-${event.type}-${i}`}
            style={{
              padding: '0.5rem',
              borderBottom: '1px solid #eee',
              fontFamily: 'monospace',
              fontSize: '0.9em',
            }}
          >
            <strong>{event.type}</strong>
            <pre>{JSON.stringify(event.data, null, 2)}</pre>
          </div>
        ))}
      </div>
    </div>
  );
}

// Example 5: Error Handling and Recovery
export function RobustProgressMonitor({ jobId }: { jobId: string }) {
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  useJobProgress(jobId, (event: JobEvent) => {
    setError(null);

    if (event.type === 'step-progress') {
      const progressData = event.data as { progressPct: number };
      setProgress(progressData.progressPct);
    } else if (event.type === 'error') {
      const errorData = event.data as { message: string };
      setError(errorData.message);
    }
  });

  return (
    <div>
      <h3>Robust Progress Monitor</h3>

      {/* Error Display */}
      {error && <div style={{ color: 'red', marginBottom: '1rem' }}>Error: {error}</div>}

      {/* Progress */}
      <progress value={progress} max={100} style={{ width: '100%' }} />
      <div>{progress}%</div>
    </div>
  );
}
