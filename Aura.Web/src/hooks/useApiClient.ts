/**
 * API Client Hook
 * Provides type-safe API client access with React Query integration
 */

import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import { queryKeys } from '../api/queryClient';
import * as adminApi from '../services/api/adminApi';
import * as projectsApi from '../services/api/projectsApi';
import * as userApi from '../services/api/userApi';
import * as videoApi from '../services/api/videoGenerationApi';
import type { VideoGenerationRequest, VideoJob } from '../services/api/videoGenerationApi';

/**
 * Hook for video generation operations
 */
export function useVideoGeneration() {
  const queryClient = useQueryClient();

  const generateVideo = useMutation({
    mutationFn: (request: VideoGenerationRequest) => videoApi.generateVideo(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });

  const cancelJob = useMutation({
    mutationFn: (jobId: string) => videoApi.cancelJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });

  const deleteJob = useMutation({
    mutationFn: (jobId: string) => videoApi.deleteJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all });
    },
  });

  return {
    generateVideo,
    cancelJob,
    deleteJob,
  };
}

/**
 * Hook for fetching job status
 */
export function useJobStatus(
  jobId: string | undefined,
  options?: Omit<UseQueryOptions<VideoJob>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.jobs.status(jobId || ''),
    queryFn: () => videoApi.getJobStatus(jobId!),
    enabled: !!jobId,
    refetchInterval: (data) => {
      // Stop polling when job is completed, failed, or cancelled
      if (data?.status && ['completed', 'failed', 'cancelled'].includes(data.status)) {
        return false;
      }
      return 2000; // Poll every 2 seconds
    },
    ...options,
  });
}

/**
 * Hook for fetching all jobs
 */
export function useJobs(
  filters?: Parameters<typeof videoApi.getJobs>[0],
  options?: Omit<UseQueryOptions<{ jobs: VideoJob[]; total: number }>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: queryKeys.jobs.list(filters),
    queryFn: () => videoApi.getJobs(filters),
    ...options,
  });
}

/**
 * Hook for user preferences
 */
export function useUserPreferences(
  options?: Omit<UseQueryOptions<userApi.UserPreferences>, 'queryKey' | 'queryFn'>
) {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['user', 'preferences'],
    queryFn: () => userApi.getUserPreferences(),
    ...options,
  });

  const update = useMutation({
    mutationFn: (preferences: Partial<userApi.UserPreferences>) =>
      userApi.updateUserPreferences(preferences),
    onSuccess: (data) => {
      queryClient.setQueryData(['user', 'preferences'], data);
    },
  });

  return {
    ...query,
    update,
  };
}

/**
 * Hook for admin statistics
 */
export function useAdminStats(
  options?: Omit<UseQueryOptions<adminApi.SystemStats>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: ['admin', 'stats'],
    queryFn: () => adminApi.getSystemStats(),
    ...options,
  });
}

/**
 * Hook for admin user management
 */
export function useAdminUsers(
  page: number = 1,
  limit: number = 50,
  filters?: Parameters<typeof adminApi.getUsers>[2],
  options?: Omit<
    UseQueryOptions<{
      users: adminApi.User[];
      total: number;
      page: number;
      limit: number;
    }>,
    'queryKey' | 'queryFn'
  >
) {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['admin', 'users', page, limit, filters],
    queryFn: () => adminApi.getUsers(page, limit, filters),
    ...options,
  });

  const updateUser = useMutation({
    mutationFn: ({ userId, updates }: { userId: string; updates: Parameters<typeof adminApi.updateUser>[1] }) =>
      adminApi.updateUser(userId, updates),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });

  const deleteUser = useMutation({
    mutationFn: (userId: string) => adminApi.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });

  const suspendUser = useMutation({
    mutationFn: ({ userId, reason }: { userId: string; reason?: string }) =>
      adminApi.suspendUser(userId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });

  return {
    ...query,
    updateUser,
    deleteUser,
    suspendUser,
  };
}
