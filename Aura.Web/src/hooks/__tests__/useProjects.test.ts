import { renderHook, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as projectService from '../../services/projectService';
import { ProjectListItem } from '../../types/project';
import { useProjects } from '../useProjects';

vi.mock('../../services/projectService');
vi.mock('../../services/loggingService');

describe('useProjects', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should load projects successfully', async () => {
    const mockProjects: ProjectListItem[] = [
      {
        id: '1',
        name: 'Test Project',
        description: 'Test Description',
        lastModifiedAt: '2024-01-01T00:00:00Z',
        duration: 120,
        clipCount: 5,
      },
    ];

    vi.spyOn(projectService, 'getProjects').mockResolvedValueOnce(mockProjects);

    const { result } = renderHook(() => useProjects());

    expect(result.current.loading).toBe(true);
    expect(result.current.projects).toEqual([]);
    expect(result.current.error).toBeNull();

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.projects).toEqual(mockProjects);
    expect(result.current.error).toBeNull();
  });

  it('should handle empty array response', async () => {
    vi.spyOn(projectService, 'getProjects').mockResolvedValueOnce([]);

    const { result } = renderHook(() => useProjects());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.projects).toEqual([]);
    expect(result.current.error).toBeNull();
  });

  it('should handle error and set empty array', async () => {
    const mockError = new Error('Failed to load projects');
    vi.spyOn(projectService, 'getProjects').mockRejectedValueOnce(mockError);

    const { result } = renderHook(() => useProjects());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.projects).toEqual([]);
    expect(result.current.error).toEqual(mockError);
  });

  it('should retry loading projects when retry is called', async () => {
    const mockProjects: ProjectListItem[] = [
      {
        id: '1',
        name: 'Test Project',
        description: 'Test Description',
        lastModifiedAt: '2024-01-01T00:00:00Z',
        duration: 120,
        clipCount: 5,
      },
    ];

    const getProjectsSpy = vi
      .spyOn(projectService, 'getProjects')
      .mockRejectedValueOnce(new Error('First call fails'))
      .mockResolvedValueOnce(mockProjects);

    const { result } = renderHook(() => useProjects());

    // Wait for initial load to fail
    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.error).toBeTruthy();
    expect(result.current.projects).toEqual([]);
    expect(getProjectsSpy).toHaveBeenCalledTimes(1);

    // Call retry
    result.current.retry();

    // Wait for retry to succeed
    await waitFor(() => {
      expect(result.current.projects).toEqual(mockProjects);
    });

    expect(result.current.error).toBeNull();
    expect(getProjectsSpy).toHaveBeenCalledTimes(2);
  });

  it('should handle non-Error objects in catch block', async () => {
    vi.spyOn(projectService, 'getProjects').mockRejectedValueOnce('String error');

    const { result } = renderHook(() => useProjects());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.projects).toEqual([]);
    expect(result.current.error).toBeInstanceOf(Error);
    expect(result.current.error?.message).toBe('String error');
  });
});
