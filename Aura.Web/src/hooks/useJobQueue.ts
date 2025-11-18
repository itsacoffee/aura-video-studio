/**
 * useJobQueue Hook
 * Custom hook for managing job queue operations and SignalR connection
 */

import { useEffect, useCallback, useRef } from 'react';
import { jobQueueService } from '../services/jobQueueService';
import type { EnqueueJobRequest } from '../services/jobQueueService';
import { useJobQueueStore } from '../stores/jobQueueStore';

export interface UseJobQueueOptions {
  autoConnect?: boolean;
  autoRefresh?: boolean;
  refreshInterval?: number;
}

export function useJobQueue(options: UseJobQueueOptions = {}) {
  const { autoConnect = true, autoRefresh = true, refreshInterval = 5000 } = options;

  const store = useJobQueueStore();
  const refreshIntervalRef = useRef<NodeJS.Timeout | null>(null);

  // Load jobs from API
  const loadJobs = useCallback(
    async (status?: string) => {
      try {
        store.setLoadingJobs(true);
        store.setError(null);
        const result = await jobQueueService.listJobs(status);
        store.setJobs(result.jobs);
      } catch (error) {
        console.error('Failed to load jobs:', error);
        store.setError(error instanceof Error ? error.message : 'Failed to load jobs');
      } finally {
        store.setLoadingJobs(false);
      }
    },
    [store]
  );

  // Load statistics
  const loadStatistics = useCallback(async () => {
    try {
      const stats = await jobQueueService.getStatistics();
      store.setStatistics(stats);
    } catch (error) {
      console.error('Failed to load statistics:', error);
    }
  }, [store]);

  // Load configuration
  const loadConfiguration = useCallback(async () => {
    try {
      const config = await jobQueueService.getConfiguration();
      store.setConfiguration(config);
    } catch (error) {
      console.error('Failed to load configuration:', error);
    }
  }, [store]);

  // Enqueue a new job
  const enqueueJob = useCallback(
    async (request: EnqueueJobRequest) => {
      try {
        store.setError(null);
        const result = await jobQueueService.enqueueJob(request);

        // Refresh jobs list after a short delay to ensure the job is in the queue
        setTimeout(() => loadJobs(), 500);

        return result;
      } catch (error) {
        console.error('Failed to enqueue job:', error);
        store.setError(error instanceof Error ? error.message : 'Failed to enqueue job');
        throw error;
      }
    },
    [store, loadJobs]
  );

  // Cancel a job
  const cancelJob = useCallback(
    async (jobId: string) => {
      try {
        store.setError(null);
        await jobQueueService.cancelJob(jobId);

        // Refresh jobs list
        await loadJobs();
      } catch (error) {
        console.error('Failed to cancel job:', error);
        store.setError(error instanceof Error ? error.message : 'Failed to cancel job');
        throw error;
      }
    },
    [store, loadJobs]
  );

  // Update configuration
  const updateConfiguration = useCallback(
    async (maxConcurrentJobs?: number, isEnabled?: boolean) => {
      try {
        store.setError(null);
        const config = await jobQueueService.updateConfiguration(maxConcurrentJobs, isEnabled);
        store.setConfiguration(config);
        return config;
      } catch (error) {
        console.error('Failed to update configuration:', error);
        store.setError(error instanceof Error ? error.message : 'Failed to update configuration');
        throw error;
      }
    },
    [store]
  );

  // Setup SignalR event handlers
  useEffect(() => {
    // Job status changed
    const unsubscribeStatus = jobQueueService.onJobStatusChanged((data) => {
      store.updateJob(data.jobId, {
        status: data.status,
        outputPath: data.outputPath || undefined,
        errorMessage: data.errorMessage || undefined,
      });

      // Refresh statistics when job status changes
      loadStatistics();
    });

    // Job progress
    const unsubscribeProgress = jobQueueService.onJobProgress((data) => {
      store.updateJob(data.jobId, {
        progress: data.progress,
        currentStage: data.stage,
        status: data.status,
      });
    });

    // Job completed
    const unsubscribeCompleted = jobQueueService.onJobCompleted((data) => {
      store.updateJob(data.jobId, {
        status: 'Completed',
        progress: 100,
        outputPath: data.outputPath,
        completedAt: data.timestamp,
      });

      // Show notification if browser supports it
      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('Video Ready!', {
          body: 'Your video has been generated successfully.',
          icon: '/favicon.ico',
        });
      }

      loadStatistics();
    });

    // Job failed
    const unsubscribeFailed = jobQueueService.onJobFailed((data) => {
      store.updateJob(data.jobId, {
        status: 'Failed',
        errorMessage: data.errorMessage,
      });

      // Show notification if browser supports it
      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('Video Generation Failed', {
          body: data.errorMessage || 'An error occurred during video generation.',
          icon: '/favicon.ico',
        });
      }

      loadStatistics();
    });

    return () => {
      unsubscribeStatus();
      unsubscribeProgress();
      unsubscribeCompleted();
      unsubscribeFailed();
    };
  }, [store, loadStatistics]);

  // Connect to SignalR and load initial data
  useEffect(() => {
    if (!autoConnect) return;

    const connect = async () => {
      try {
        await jobQueueService.start();
        store.setConnected(true);

        // Load initial data
        await Promise.all([loadJobs(), loadStatistics(), loadConfiguration()]);
      } catch (error) {
        console.error('Failed to connect to job queue:', error);
        store.setConnected(false);
        store.setError('Failed to connect to job queue service');
      }
    };

    connect();

    return () => {
      jobQueueService.stop();
      store.setConnected(false);
    };
  }, [autoConnect, store, loadJobs, loadStatistics, loadConfiguration]);

  // Auto-refresh jobs
  useEffect(() => {
    if (!autoRefresh) return;

    refreshIntervalRef.current = setInterval(() => {
      loadJobs(store.statusFilter || undefined);
      loadStatistics();
    }, refreshInterval);

    return () => {
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current);
      }
    };
  }, [autoRefresh, refreshInterval, store.statusFilter, loadJobs, loadStatistics]);

  // Request notification permission
  useEffect(() => {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  }, []);

  return {
    // State
    jobs: store.jobs,
    statistics: store.statistics,
    configuration: store.configuration,
    isConnected: store.isConnected,
    isLoadingJobs: store.isLoadingJobs,
    error: store.error,
    statusFilter: store.statusFilter,

    // Computed
    pendingJobs: store.getPendingJobs(),
    processingJobs: store.getProcessingJobs(),
    completedJobs: store.getCompletedJobs(),
    failedJobs: store.getFailedJobs(),

    // Actions
    enqueueJob,
    cancelJob,
    loadJobs,
    loadStatistics,
    loadConfiguration,
    updateConfiguration,
    clearCompletedJobs: store.clearCompletedJobs,
    setStatusFilter: store.setStatusFilter,
    getJobById: store.getJobById,
  };
}
