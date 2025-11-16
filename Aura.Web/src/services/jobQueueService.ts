import * as signalR from '@microsoft/signalr';
import { env } from '@/config/env';
import type { JobStatus } from '@/state/activityContext';

export interface JobQueueItem {
  jobId: string;
  status: string;
  priority: number;
  progress: number;
  currentStage: string | null;
  enqueuedAt: string;
  startedAt: string | null;
  completedAt: string | null;
  outputPath: string | null;
  errorMessage: string | null;
  retryCount: number;
  maxRetries: number;
  nextRetryAt: string | null;
  workerId: string | null;
  correlationId: string | null;
  isQuickDemo: boolean;
}

export interface QueueStatistics {
  totalJobs: number;
  pendingJobs: number;
  processingJobs: number;
  completedJobs: number;
  failedJobs: number;
  cancelledJobs: number;
  activeWorkers: number;
}

export interface QueueConfiguration {
  id: number;
  maxConcurrentJobs: number;
  pauseOnBattery: boolean;
  cpuThrottleThreshold: number;
  memoryThrottleThreshold: number;
  isEnabled: boolean;
  pollingIntervalSeconds: number;
  jobHistoryRetentionDays: number;
  failedJobRetentionDays: number;
  retryBaseDelaySeconds: number;
  retryMaxDelaySeconds: number;
  enableNotifications: boolean;
  updatedAt: string;
}

export interface EnqueueJobRequest {
  brief: {
    topic: string;
    audience?: string;
    goal?: string;
    tone?: string;
    language?: string;
    aspect?: string;
  };
  planSpec: {
    targetDuration: string;
    pacing?: string;
    density?: string;
    style?: string;
  };
  voiceSpec: {
    voiceName: string;
    rate?: number;
    pitch?: number;
    pause?: number;
  };
  renderSpec: {
    res: string;
    container?: string;
    videoBitrateK?: number;
    audioBitrateK?: number;
    fps?: number;
    codec?: string;
    qualityLevel?: string;
    enableSceneCut?: boolean;
  };
  priority?: number;
  isQuickDemo?: boolean;
}

type JobStatusChangedCallback = (data: {
  jobId: string;
  status: string;
  correlationId?: string;
  outputPath?: string;
  errorMessage?: string;
  timestamp: string;
}) => void;

type JobProgressCallback = (data: {
  jobId: string;
  stage: string;
  progress: number;
  status: string;
  message: string;
  correlationId: string;
  timestamp: string;
}) => void;

type JobCompletedCallback = (data: {
  jobId: string;
  outputPath: string;
  correlationId?: string;
  timestamp: string;
}) => void;

type JobFailedCallback = (data: {
  jobId: string;
  errorMessage: string;
  correlationId?: string;
  timestamp: string;
}) => void;

class JobQueueService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private statusChangeCallbacks: JobStatusChangedCallback[] = [];
  private progressCallbacks: JobProgressCallback[] = [];
  private completedCallbacks: JobCompletedCallback[] = [];
  private failedCallbacks: JobFailedCallback[] = [];

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection() {
    const baseUrl = env.apiBaseUrl;
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/job-queue`, {
        skipNegotiation: false,
        transport:
          signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop retrying
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Register event handlers
    this.connection.on('JobStatusChanged', (data) => {
      console.info('Job status changed:', data);
      this.statusChangeCallbacks.forEach((cb) => cb(data));
    });

    this.connection.on('JobProgress', (data) => {
      console.info('Job progress:', data);
      this.progressCallbacks.forEach((cb) => cb(data));
    });

    this.connection.on('JobCompleted', (data) => {
      console.info('Job completed:', data);
      this.completedCallbacks.forEach((cb) => cb(data));
    });

    this.connection.on('JobFailed', (data) => {
      console.info('Job failed:', data);
      this.failedCallbacks.forEach((cb) => cb(data));
    });

    this.connection.onreconnecting(() => {
      console.info('JobQueueHub reconnecting...');
      this.reconnectAttempts++;
    });

    this.connection.onreconnected(() => {
      console.info('JobQueueHub reconnected');
      this.reconnectAttempts = 0;
    });

    this.connection.onclose((error) => {
      console.error('JobQueueHub connection closed:', error);
      // Attempt to reconnect after a delay
      setTimeout(() => this.start(), 5000);
    });
  }

  async start() {
    if (!this.connection) return;

    try {
      if (this.connection.state === signalR.HubConnectionState.Disconnected) {
        await this.connection.start();
        console.info('JobQueueHub connected');
        await this.subscribeToQueue();
      }
    } catch (error) {
      console.error('Error starting JobQueueHub connection:', error);
      // Retry after delay
      setTimeout(() => this.start(), 5000);
    }
  }

  async stop() {
    if (!this.connection) return;

    try {
      await this.connection.stop();
      console.info('JobQueueHub disconnected');
    } catch (error) {
      console.error('Error stopping JobQueueHub connection:', error);
    }
  }

  async subscribeToJob(jobId: string) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.start();
    }

    try {
      await this.connection!.invoke('SubscribeToJob', jobId);
      console.info(`Subscribed to job: ${jobId}`);
    } catch (error) {
      console.error(`Error subscribing to job ${jobId}:`, error);
    }
  }

  async unsubscribeFromJob(jobId: string) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromJob', jobId);
      console.info(`Unsubscribed from job: ${jobId}`);
    } catch (error) {
      console.error(`Error unsubscribing from job ${jobId}:`, error);
    }
  }

  async subscribeToQueue() {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.start();
    }

    try {
      await this.connection!.invoke('SubscribeToQueue');
      console.info('Subscribed to queue updates');
    } catch (error) {
      console.error('Error subscribing to queue:', error);
    }
  }

  async unsubscribeFromQueue() {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromQueue');
      console.info('Unsubscribed from queue updates');
    } catch (error) {
      console.error('Error unsubscribing from queue:', error);
    }
  }

  onJobStatusChanged(callback: JobStatusChangedCallback) {
    this.statusChangeCallbacks.push(callback);
    return () => {
      const index = this.statusChangeCallbacks.indexOf(callback);
      if (index > -1) {
        this.statusChangeCallbacks.splice(index, 1);
      }
    };
  }

  onJobProgress(callback: JobProgressCallback) {
    this.progressCallbacks.push(callback);
    return () => {
      const index = this.progressCallbacks.indexOf(callback);
      if (index > -1) {
        this.progressCallbacks.splice(index, 1);
      }
    };
  }

  onJobCompleted(callback: JobCompletedCallback) {
    this.completedCallbacks.push(callback);
    return () => {
      const index = this.completedCallbacks.indexOf(callback);
      if (index > -1) {
        this.completedCallbacks.splice(index, 1);
      }
    };
  }

  onJobFailed(callback: JobFailedCallback) {
    this.failedCallbacks.push(callback);
    return () => {
      const index = this.failedCallbacks.indexOf(callback);
      if (index > -1) {
        this.failedCallbacks.splice(index, 1);
      }
    };
  }

  // API methods
  async enqueueJob(request: EnqueueJobRequest): Promise<{ jobId: string; status: string }> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/enqueue`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to enqueue job');
    }

    return response.json();
  }

  async getJob(jobId: string): Promise<JobQueueItem> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/${jobId}`);

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to get job');
    }

    return response.json();
  }

  async listJobs(status?: string, limit = 100): Promise<{ jobs: JobQueueItem[]; count: number }> {
    const baseUrl = env.apiBaseUrl;
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    params.append('limit', limit.toString());

    const response = await fetch(`${baseUrl}/api/queue?${params}`);

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to list jobs');
    }

    return response.json();
  }

  async cancelJob(jobId: string): Promise<void> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/${jobId}/cancel`, {
      method: 'POST',
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to cancel job');
    }
  }

  async getStatistics(): Promise<QueueStatistics> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/statistics`);

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to get statistics');
    }

    const data = await response.json();
    return data.statistics;
  }

  async getConfiguration(): Promise<QueueConfiguration> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/configuration`);

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to get configuration');
    }

    const data = await response.json();
    return data.configuration;
  }

  async updateConfiguration(
    maxConcurrentJobs?: number,
    isEnabled?: boolean
  ): Promise<QueueConfiguration> {
    const baseUrl = env.apiBaseUrl;
    const response = await fetch(`${baseUrl}/api/queue/configuration`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ maxConcurrentJobs, isEnabled }),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.detail || 'Failed to update configuration');
    }

    const data = await response.json();
    return data.configuration;
  }
}

export const jobQueueService = new JobQueueService();
