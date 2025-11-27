/**
 * useJobQueueContextMenu - Custom hook for job queue context menu interactions
 *
 * Provides a React-friendly interface for triggering context menus on job queue items
 * and listening for context menu action callbacks.
 */

import { useCallback } from 'react';
import type { JobQueueMenuData } from '../types/electron-context-menu';
import { useContextMenu, useContextMenuAction } from './useContextMenu';

export interface JobQueueContextMenuCallbacks {
  onPause: (jobId: string) => void;
  onResume: (jobId: string) => void;
  onCancel: (jobId: string) => void;
  onViewLogs: (jobId: string) => void;
  onRetry: (jobId: string) => void;
}

export interface JobForContextMenu {
  id: string;
  status: 'queued' | 'running' | 'paused' | 'completed' | 'failed' | 'canceled';
  outputPath?: string;
}

/**
 * Hook to integrate context menu functionality for job queue items
 *
 * @param callbacks - Object containing callback functions for each context menu action
 * @returns A function to call with the mouse event and job data to show the context menu
 */
export function useJobQueueContextMenu(callbacks: JobQueueContextMenuCallbacks) {
  const { onPause, onResume, onCancel, onViewLogs, onRetry } = callbacks;

  const showContextMenu = useContextMenu<JobQueueMenuData>('job-queue');

  const handleContextMenu = useCallback(
    (e: React.MouseEvent, job: JobForContextMenu) => {
      const menuData: JobQueueMenuData = {
        jobId: job.id,
        status: job.status,
        outputPath: job.outputPath,
      };
      showContextMenu(e, menuData);
    },
    [showContextMenu]
  );

  // Register action handlers for context menu callbacks
  useContextMenuAction<JobQueueMenuData>(
    'job-queue',
    'onPause',
    useCallback(
      (data: JobQueueMenuData) => {
        console.info('Pausing job:', data.jobId);
        onPause(data.jobId);
      },
      [onPause]
    )
  );

  useContextMenuAction<JobQueueMenuData>(
    'job-queue',
    'onResume',
    useCallback(
      (data: JobQueueMenuData) => {
        console.info('Resuming job:', data.jobId);
        onResume(data.jobId);
      },
      [onResume]
    )
  );

  useContextMenuAction<JobQueueMenuData>(
    'job-queue',
    'onCancel',
    useCallback(
      (data: JobQueueMenuData) => {
        // Using confirm as a fallback; in a real implementation this would use a dialog component
        const shouldCancel = window.confirm('Cancel this job?');
        if (shouldCancel) {
          console.info('Cancelling job:', data.jobId);
          onCancel(data.jobId);
        }
      },
      [onCancel]
    )
  );

  useContextMenuAction<JobQueueMenuData>(
    'job-queue',
    'onViewLogs',
    useCallback(
      (data: JobQueueMenuData) => {
        console.info('Viewing logs for job:', data.jobId);
        onViewLogs(data.jobId);
      },
      [onViewLogs]
    )
  );

  useContextMenuAction<JobQueueMenuData>(
    'job-queue',
    'onRetry',
    useCallback(
      (data: JobQueueMenuData) => {
        console.info('Retrying job:', data.jobId);
        onRetry(data.jobId);
      },
      [onRetry]
    )
  );

  return handleContextMenu;
}
