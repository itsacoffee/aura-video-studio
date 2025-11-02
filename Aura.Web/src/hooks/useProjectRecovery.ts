import { useState, useEffect, useCallback } from 'react';
import apiClient from '../services/api/apiClient';
import { loggingService as logger } from '../services/loggingService';

export interface RecoverableProject {
  projectId: string;
  title: string;
  jobId?: string;
  currentStage?: string;
  progressPercent: number;
  createdAt: string;
  updatedAt: string;
  filesExist: boolean;
  missingFilesCount: number;
  canRecover: boolean;
}

export interface ProjectRecoveryState {
  projects: RecoverableProject[];
  loading: boolean;
  error: string | null;
}

export interface UseProjectRecoveryReturn {
  state: ProjectRecoveryState;
  checkForIncompleteProjects: () => Promise<void>;
  discardProject: (projectId: string) => Promise<void>;
  getProjectDetails: (projectId: string) => Promise<ProjectDetails | null>;
}

export interface ProjectDetails extends RecoverableProject {
  missingFiles: string[];
  latestCheckpoint?: {
    stageName: string;
    checkpointTime: string;
    completedScenes: number;
    totalScenes: number;
    outputFilePath?: string;
  };
  scenes: Array<{
    sceneIndex: number;
    scriptText: string;
    durationSeconds: number;
    isCompleted: boolean;
    audioFilePath?: string;
    imageFilePath?: string;
  }>;
}

/**
 * Hook for managing project recovery functionality
 */
export function useProjectRecovery(): UseProjectRecoveryReturn {
  const [state, setState] = useState<ProjectRecoveryState>({
    projects: [],
    loading: false,
    error: null,
  });

  /**
   * Check for incomplete projects that can be recovered
   */
  const checkForIncompleteProjects = useCallback(async () => {
    setState((prev) => ({ ...prev, loading: true, error: null }));

    try {
      logger.debug(
        'Checking for incomplete projects',
        'useProjectRecovery',
        'checkForIncompleteProjects'
      );

      const response = await apiClient.get<{
        projects: RecoverableProject[];
        count: number;
      }>('/api/projects/incomplete');

      setState({
        projects: response.data.projects,
        loading: false,
        error: null,
      });

      logger.debug(
        `Found ${response.data.count} incomplete projects`,
        'useProjectRecovery',
        'checkForIncompleteProjects',
        { count: response.data.count }
      );
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error(
        'Failed to check for incomplete projects',
        errorObj,
        'useProjectRecovery',
        'checkForIncompleteProjects'
      );

      setState((prev) => ({
        ...prev,
        loading: false,
        error: 'Failed to check for incomplete projects. Please try again.',
      }));
    }
  }, []);

  /**
   * Get detailed information about a specific project
   */
  const getProjectDetails = useCallback(
    async (projectId: string): Promise<ProjectDetails | null> => {
      try {
        logger.debug('Fetching project details', 'useProjectRecovery', 'getProjectDetails', {
          projectId,
        });

        const response = await apiClient.get<ProjectDetails>(`/api/projects/${projectId}`);
        return response.data;
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        logger.error(
          'Failed to fetch project details',
          errorObj,
          'useProjectRecovery',
          'getProjectDetails',
          { projectId }
        );
        return null;
      }
    },
    []
  );

  /**
   * Discard a project permanently
   */
  const discardProject = useCallback(async (projectId: string) => {
    try {
      logger.debug('Discarding project', 'useProjectRecovery', 'discardProject', { projectId });

      await apiClient.delete(`/api/projects/${projectId}`);

      // Remove from local state
      setState((prev) => ({
        ...prev,
        projects: prev.projects.filter((p) => p.projectId !== projectId),
      }));

      logger.debug('Project discarded successfully', 'useProjectRecovery', 'discardProject', {
        projectId,
      });
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error('Failed to discard project', errorObj, 'useProjectRecovery', 'discardProject', {
        projectId,
      });
      throw errorObj;
    }
  }, []);

  // Check for incomplete projects on mount
  useEffect(() => {
    checkForIncompleteProjects();
  }, [checkForIncompleteProjects]);

  return {
    state,
    checkForIncompleteProjects,
    discardProject,
    getProjectDetails,
  };
}
