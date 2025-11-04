/**
 * React Query Client Configuration
 *
 * Provides centralized query/mutation configuration with:
 * - Retry logic with exponential backoff
 * - Stale-while-revalidate behavior
 * - Request deduplication
 * - Cache management
 */

import { QueryClient, DefaultOptions } from '@tanstack/react-query';
import { loggingService } from '@/services/loggingService';

/**
 * Default options for all queries and mutations
 */
const defaultOptions: DefaultOptions = {
  queries: {
    // Stale time: Data considered fresh for 30 seconds
    staleTime: 30 * 1000,

    // Cache time: Keep unused data in cache for 5 minutes
    gcTime: 5 * 60 * 1000,

    // Retry configuration for idempotent GET requests
    retry: (failureCount, error) => {
      // Don't retry on 4xx errors (client errors)
      if (error && typeof error === 'object' && 'status' in error) {
        const status = (error as { status?: number }).status;
        if (status && status >= 400 && status < 500) {
          return false;
        }
      }

      // Retry up to 3 times for transient errors
      return failureCount < 3;
    },

    // Exponential backoff: 1s, 2s, 4s
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),

    // Don't refetch on window focus by default (can be overridden per query)
    refetchOnWindowFocus: false,

    // Refetch on reconnect
    refetchOnReconnect: true,

    // Refetch on mount if data is stale
    refetchOnMount: true,
  },

  mutations: {
    // Mutations (POST/PUT/DELETE) don't retry by default
    // Individual mutations can opt-in to retries
    retry: false,

    // Error handling for mutations
    onError: (error) => {
      loggingService.error(
        'Mutation failed',
        error instanceof Error ? error : new Error(String(error)),
        'queryClient',
        'mutation'
      );
    },
  },
};

/**
 * Create and export the query client
 *
 * This is a singleton instance used throughout the app
 */
export const queryClient = new QueryClient({
  defaultOptions,

  // Query cache configuration
  // Request deduplication is handled automatically by React Query
  // Multiple components requesting the same query key will share the same request
});

/**
 * Query key factory for consistent key generation
 *
 * This ensures request deduplication works properly across the app
 */
export const queryKeys = {
  // Health checks
  health: {
    all: ['health'] as const,
    live: () => [...queryKeys.health.all, 'live'] as const,
    ready: () => [...queryKeys.health.all, 'ready'] as const,
    dependencies: () => [...queryKeys.health.all, 'dependencies'] as const,
  },

  // Jobs
  jobs: {
    all: ['jobs'] as const,
    list: (filters?: Record<string, unknown>) => [...queryKeys.jobs.all, 'list', filters] as const,
    detail: (id: string) => [...queryKeys.jobs.all, 'detail', id] as const,
    status: (id: string) => [...queryKeys.jobs.all, 'status', id] as const,
  },

  // Settings
  settings: {
    all: ['settings'] as const,
    hardware: () => [...queryKeys.settings.all, 'hardware'] as const,
    providers: () => [...queryKeys.settings.all, 'providers'] as const,
    apiKeys: () => [...queryKeys.settings.all, 'apiKeys'] as const,
  },

  // Engines
  engines: {
    all: ['engines'] as const,
    list: () => [...queryKeys.engines.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.engines.all, 'detail', id] as const,
  },

  // Prompts
  prompts: {
    all: ['prompts'] as const,
    list: () => [...queryKeys.prompts.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.prompts.all, 'detail', id] as const,
  },

  // Projects
  projects: {
    all: ['projects'] as const,
    list: () => [...queryKeys.projects.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.projects.all, 'detail', id] as const,
  },
} as const;

/**
 * Utility to invalidate related queries after mutations
 */
export const invalidateQueries = {
  jobs: () => queryClient.invalidateQueries({ queryKey: queryKeys.jobs.all }),
  settings: () => queryClient.invalidateQueries({ queryKey: queryKeys.settings.all }),
  engines: () => queryClient.invalidateQueries({ queryKey: queryKeys.engines.all }),
  prompts: () => queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all }),
  projects: () => queryClient.invalidateQueries({ queryKey: queryKeys.projects.all }),
};
