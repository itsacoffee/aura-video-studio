/**
 * useProjects Hook Tests
 */

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as projectsApi from '../../services/api/projectsApi';
import { useProjects } from '../useProjects';

vi.mock('../../services/api/projectsApi');

// Create a wrapper with QueryClient
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  Wrapper.displayName = 'QueryClientProviderWrapper';
  return Wrapper;
};

describe('useProjects Hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should fetch projects on mount', async () => {
    const mockProjects = {
      projects: [
        { id: '1', name: 'Project 1', status: 'draft' as const },
        { id: '2', name: 'Project 2', status: 'completed' as const },
      ],
      total: 2,
      page: 1,
      pageSize: 10,
    };

    vi.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

    const { result } = renderHook(() => useProjects(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.projects.length).toBe(2);
    });

    expect(result.current.total).toBe(2);
  });

  it('should create a project', async () => {
    const mockProjects = {
      projects: [],
      total: 0,
      page: 1,
      pageSize: 10,
    };

    const newProject = {
      id: '1',
      name: 'New Project',
      status: 'draft' as const,
      brief: {
        topic: 'Test',
        audience: 'General',
        goal: 'Inform',
        tone: 'Informative',
        language: 'en-US',
        aspect: 'Widescreen16x9',
      },
      planSpec: {
        targetDuration: '00:03:00',
        pacing: 'Conversational',
        density: 'Balanced',
        style: 'Standard',
      },
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
    };

    vi.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);
    vi.mocked(projectsApi.createProject).mockResolvedValue(newProject);

    const { result } = renderHook(() => useProjects(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await result.current.createProject({
      name: 'New Project',
      brief: newProject.brief,
      planSpec: newProject.planSpec,
    });

    await waitFor(() => {
      expect(projectsApi.createProject).toHaveBeenCalled();
    });
  });

  it('should handle errors', async () => {
    const error = new Error('Failed to fetch projects');
    vi.mocked(projectsApi.listProjects).mockRejectedValue(error);

    const { result } = renderHook(() => useProjects(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.error).toBeTruthy();
    });
  });

  it('should update filters', async () => {
    const mockProjects = {
      projects: [],
      total: 0,
      page: 1,
      pageSize: 10,
    };

    vi.mocked(projectsApi.listProjects).mockResolvedValue(mockProjects);

    const { result } = renderHook(() => useProjects(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    result.current.setFilters({ status: 'completed' });

    await waitFor(() => {
      expect(projectsApi.listProjects).toHaveBeenCalledWith(
        expect.objectContaining({ status: 'completed' })
      );
    });
  });
});
