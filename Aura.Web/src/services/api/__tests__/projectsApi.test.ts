/**
 * Projects API Tests
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as apiClient from '../apiClient';
import * as projectsApi from '../projectsApi';

vi.mock('../apiClient');

describe('Projects API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should list projects with filters', async () => {
    const mockResponse = {
      projects: [
        { id: '1', name: 'Project 1', status: 'draft' },
        { id: '2', name: 'Project 2', status: 'completed' },
      ],
      total: 2,
      page: 1,
      pageSize: 10,
    };

    vi.mocked(apiClient.get).mockResolvedValue(mockResponse);

    const filters = {
      status: 'draft' as const,
      page: 1,
      pageSize: 10,
    };

    const result = await projectsApi.listProjects(filters);

    expect(result).toEqual(mockResponse);
    expect(apiClient.get).toHaveBeenCalledWith(
      expect.stringContaining('/api/projects?'),
      undefined
    );
  });

  it('should get a single project', async () => {
    const mockProject = {
      id: '1',
      name: 'Test Project',
      status: 'draft',
      createdAt: '2024-01-01',
    };

    vi.mocked(apiClient.get).mockResolvedValue(mockProject);

    const result = await projectsApi.getProject('1');

    expect(result).toEqual(mockProject);
    expect(apiClient.get).toHaveBeenCalledWith('/api/projects/1', undefined);
  });

  it('should create a project', async () => {
    const newProject = {
      name: 'New Project',
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
    };

    const mockResponse = {
      ...newProject,
      id: '123',
      status: 'draft',
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
    };

    vi.mocked(apiClient.post).mockResolvedValue(mockResponse);

    const result = await projectsApi.createProject(newProject as any);

    expect(result).toEqual(mockResponse);
    expect(apiClient.post).toHaveBeenCalledWith('/api/projects', newProject, undefined);
  });

  it('should update a project', async () => {
    const updates = { name: 'Updated Name', status: 'in-progress' as const };
    const mockResponse = {
      id: '1',
      ...updates,
      updatedAt: '2024-01-02',
    };

    vi.mocked(apiClient.put).mockResolvedValue(mockResponse);

    const result = await projectsApi.updateProject('1', updates);

    expect(result).toEqual(mockResponse);
    expect(apiClient.put).toHaveBeenCalledWith('/api/projects/1', updates, undefined);
  });

  it('should delete a project', async () => {
    vi.mocked(apiClient.del).mockResolvedValue(undefined);

    await projectsApi.deleteProject('1');

    expect(apiClient.del).toHaveBeenCalledWith('/api/projects/1', undefined);
  });

  it('should duplicate a project', async () => {
    const mockResponse = {
      id: '2',
      name: 'Copy of Project',
      status: 'draft',
    };

    vi.mocked(apiClient.post).mockResolvedValue(mockResponse);

    const result = await projectsApi.duplicateProject('1');

    expect(result).toEqual(mockResponse);
    expect(apiClient.post).toHaveBeenCalledWith('/api/projects/1/duplicate', undefined, undefined);
  });

  it('should get project statistics', async () => {
    const mockStats = {
      totalProjects: 10,
      byStatus: {
        draft: 3,
        'in-progress': 2,
        completed: 4,
        failed: 1,
      },
      recentProjects: [],
      totalVideosGenerated: 4,
    };

    vi.mocked(apiClient.get).mockResolvedValue(mockStats);

    const result = await projectsApi.getProjectStatistics();

    expect(result).toEqual(mockStats);
    expect(apiClient.get).toHaveBeenCalledWith('/api/projects/statistics', undefined);
  });
});
