/**
 * useProjects Hook
 * Manages project CRUD operations with optimistic updates and caching
 */

import { useQueryClient, useQuery, useMutation } from '@tanstack/react-query';
import { useState, useCallback, useEffect } from 'react';
import {
  listProjects,
  getProject,
  createProject,
  updateProject,
  deleteProject,
  duplicateProject,
  type Project,
  type CreateProjectRequest,
  type UpdateProjectRequest,
  type ProjectListFilters,
  type ProjectListResponse,
} from '../services/api/projectsApi';
import { useApiError } from './useApiError';

export interface UseProjectsOptions {
  filters?: ProjectListFilters;
  autoRefetch?: boolean;
  refetchInterval?: number;
}

export interface UseProjectsResult {
  // Data
  projects: Project[];
  total: number;
  page: number;
  pageSize: number;

  // Loading states
  isLoading: boolean;
  isRefetching: boolean;

  // Error handling
  error: Error | null;
  clearError: () => void;

  // CRUD operations
  createProject: (project: CreateProjectRequest) => Promise<Project>;
  updateProject: (id: string, updates: UpdateProjectRequest) => Promise<Project>;
  deleteProject: (id: string) => Promise<void>;
  duplicateProject: (id: string) => Promise<Project>;

  // Data management
  refetch: () => Promise<void>;
  setFilters: (filters: ProjectListFilters) => void;
  getProjectById: (id: string) => Project | undefined;
}

/**
 * Hook for managing projects with CRUD operations
 */
export function useProjects(options: UseProjectsOptions = {}): UseProjectsResult {
  const { filters: initialFilters, autoRefetch = false, refetchInterval } = options;

  const [filters, setFilters] = useState<ProjectListFilters>(initialFilters || {});
  const { error, setError, clearError } = useApiError();
  const queryClient = useQueryClient();

  // Query key for cache management
  const queryKey = ['projects', filters];

  // Fetch projects list with React Query
  const {
    data: projectsData,
    isLoading,
    isRefetching,
    refetch: refetchQuery,
    error: queryError,
  } = useQuery<ProjectListResponse>({
    queryKey,
    queryFn: () => listProjects(filters),
    staleTime: 30000, // 30 seconds
    refetchOnWindowFocus: autoRefetch,
    refetchInterval: refetchInterval,
  });

  // Handle query errors using useEffect
  useEffect(() => {
    if (queryError) {
      setError(queryError instanceof Error ? queryError : new Error('Failed to fetch projects'));
    }
  }, [queryError, setError]);

  // Create project mutation with optimistic update
  const createMutation = useMutation({
    mutationFn: (project: CreateProjectRequest) => createProject(project),
    onMutate: async (newProject) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey });

      // Snapshot previous value
      const previousProjects = queryClient.getQueryData<ProjectListResponse>(queryKey);

      // Optimistically update to the new value
      if (previousProjects) {
        const optimisticProject: Project = {
          id: `temp-${Date.now()}`,
          ...newProject,
          status: 'draft',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        };

        queryClient.setQueryData<ProjectListResponse>(queryKey, {
          ...previousProjects,
          projects: [optimisticProject, ...previousProjects.projects],
          total: previousProjects.total + 1,
        });
      }

      return { previousProjects };
    },
    onError: (err, _, context) => {
      // Rollback on error
      if (context?.previousProjects) {
        queryClient.setQueryData(queryKey, context.previousProjects);
      }
      setError(err instanceof Error ? err : new Error('Failed to create project'));
    },
    onSuccess: () => {
      // Invalidate and refetch
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });

  // Update project mutation with optimistic update
  const updateMutation = useMutation({
    mutationFn: ({ id, updates }: { id: string; updates: UpdateProjectRequest }) =>
      updateProject(id, updates),
    onMutate: async ({ id, updates }) => {
      await queryClient.cancelQueries({ queryKey });

      const previousProjects = queryClient.getQueryData<ProjectListResponse>(queryKey);

      if (previousProjects) {
        queryClient.setQueryData<ProjectListResponse>(queryKey, {
          ...previousProjects,
          projects: previousProjects.projects.map((project) =>
            project.id === id
              ? ({
                  ...project,
                  ...updates,
                  // Properly merge nested partial objects
                  brief: updates.brief ? { ...project.brief, ...updates.brief } : project.brief,
                  planSpec: updates.planSpec
                    ? { ...project.planSpec, ...updates.planSpec }
                    : project.planSpec,
                  voiceSpec: updates.voiceSpec
                    ? { ...project.voiceSpec, ...updates.voiceSpec }
                    : project.voiceSpec,
                  renderSpec: updates.renderSpec
                    ? { ...project.renderSpec, ...updates.renderSpec }
                    : project.renderSpec,
                  updatedAt: new Date().toISOString(),
                } as Project)
              : project
          ),
        });
      }

      return { previousProjects };
    },
    onError: (err, _, context) => {
      if (context?.previousProjects) {
        queryClient.setQueryData(queryKey, context.previousProjects);
      }
      setError(err instanceof Error ? err : new Error('Failed to update project'));
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });

  // Delete project mutation with optimistic update
  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProject(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey });

      const previousProjects = queryClient.getQueryData<ProjectListResponse>(queryKey);

      if (previousProjects) {
        queryClient.setQueryData<ProjectListResponse>(queryKey, {
          ...previousProjects,
          projects: previousProjects.projects.filter((project) => project.id !== id),
          total: previousProjects.total - 1,
        });
      }

      return { previousProjects };
    },
    onError: (err, _, context) => {
      if (context?.previousProjects) {
        queryClient.setQueryData(queryKey, context.previousProjects);
      }
      setError(err instanceof Error ? err : new Error('Failed to delete project'));
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });

  // Duplicate project mutation
  const duplicateMutation = useMutation({
    mutationFn: (id: string) => duplicateProject(id),
    onError: (err) => {
      setError(err instanceof Error ? err : new Error('Failed to duplicate project'));
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });

  // Refetch projects
  const refetch = useCallback(async () => {
    clearError();
    await refetchQuery();
  }, [refetchQuery, clearError]);

  // Get project by ID from cache
  const getProjectById = useCallback(
    (id: string): Project | undefined => {
      return projectsData?.projects.find((p) => p.id === id);
    },
    [projectsData]
  );

  // Prefetch individual project details
  useEffect(() => {
    if (projectsData?.projects) {
      projectsData.projects.forEach((project) => {
        queryClient.prefetchQuery({
          queryKey: ['project', project.id],
          queryFn: () => getProject(project.id),
          staleTime: 60000, // 1 minute
        });
      });
    }
  }, [projectsData, queryClient]);

  return {
    // Data
    projects: projectsData?.projects || [],
    total: projectsData?.total || 0,
    page: projectsData?.page || 1,
    pageSize: projectsData?.pageSize || 10,

    // Loading states
    isLoading,
    isRefetching,

    // Error handling
    error,
    clearError,

    // CRUD operations
    createProject: async (project: CreateProjectRequest) => {
      clearError();
      return createMutation.mutateAsync(project);
    },
    updateProject: async (id: string, updates: UpdateProjectRequest) => {
      clearError();
      return updateMutation.mutateAsync({ id, updates });
    },
    deleteProject: async (id: string) => {
      clearError();
      return deleteMutation.mutateAsync(id);
    },
    duplicateProject: async (id: string) => {
      clearError();
      return duplicateMutation.mutateAsync(id);
    },

    // Data management
    refetch,
    setFilters,
    getProjectById,
  };
}

/**
 * Hook for managing a single project
 */
export function useProject(id: string) {
  const { error, setError, clearError } = useApiError();
  const queryClient = useQueryClient();

  const {
    data: project,
    isLoading,
    refetch,
    error: queryError,
  } = useQuery<Project>({
    queryKey: ['project', id],
    queryFn: () => getProject(id),
    enabled: !!id,
    staleTime: 60000, // 1 minute
  });

  // Handle query errors using useEffect
  useEffect(() => {
    if (queryError) {
      setError(queryError instanceof Error ? queryError : new Error('Failed to fetch project'));
    }
  }, [queryError, setError]);

  const updateMutation = useMutation({
    mutationFn: (updates: UpdateProjectRequest) => updateProject(id, updates),
    onSuccess: (updatedProject) => {
      queryClient.setQueryData(['project', id], updatedProject);
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
    onError: (err) => {
      setError(err instanceof Error ? err : new Error('Failed to update project'));
    },
  });

  return {
    project,
    isLoading,
    error,
    clearError,
    updateProject: async (updates: UpdateProjectRequest) => {
      clearError();
      return updateMutation.mutateAsync(updates);
    },
    refetch,
  };
}
