import { useState, useEffect, useCallback } from 'react';
import { loggingService } from '../services/loggingService';
import { getProjects } from '../services/projectService';
import { ProjectListItem } from '../types/project';

interface UseProjectsResult {
  projects: ProjectListItem[];
  loading: boolean;
  error: Error | null;
  retry: () => void;
}

/**
 * Hook for loading editor projects with error handling and retry capability
 */
export function useProjects(): UseProjectsResult {
  const [projects, setProjects] = useState<ProjectListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  const loadProjects = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await getProjects();

      // Handle empty response gracefully
      setProjects(Array.isArray(result) ? result : []);

      loggingService.info(
        `Loaded ${Array.isArray(result) ? result.length : 0} projects`,
        'useProjects',
        'loadProjects'
      );
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error(String(err));
      setError(error);
      setProjects([]); // Set empty array on error for graceful degradation

      loggingService.error('Failed to load projects', error, 'useProjects', 'loadProjects');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProjects();
  }, [loadProjects, retryCount]);

  const retry = useCallback(() => {
    loggingService.info('Retrying projects load', 'useProjects', 'retry');
    setRetryCount((prev) => prev + 1);
  }, []);

  return {
    projects,
    loading,
    error,
    retry,
  };
}
