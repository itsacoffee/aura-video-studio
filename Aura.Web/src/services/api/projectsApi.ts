/**
 * Projects API Service
 * Provides typed methods for project management operations (CRUD)
 */

import { get, post, put, del, createAbortController } from './apiClient';
import type { ExtendedAxiosRequestConfig } from './apiClient';

/**
 * Project interface
 */
export interface Project {
  id: string;
  name: string;
  description?: string;
  brief: {
    topic: string;
    audience: string;
    goal: string;
    tone: string;
    language: string;
    aspect: string;
  };
  planSpec: {
    targetDuration: string;
    pacing: string;
    density: string;
    style: string;
  };
  voiceSpec?: {
    voiceName: string;
    rate: number;
    pitch: number;
    pause: string;
  };
  renderSpec?: {
    res: { width: number; height: number };
    container: string;
    videoBitrateK: number;
    audioBitrateK: number;
    fps: number;
    codec: string;
    qualityLevel: number;
    enableSceneCut: boolean;
  };
  status: 'draft' | 'in-progress' | 'completed' | 'failed';
  jobId?: string;
  outputPath?: string;
  createdAt: string;
  updatedAt: string;
  tags?: string[];
  thumbnail?: string;
}

/**
 * Project creation request
 */
export interface CreateProjectRequest {
  name: string;
  description?: string;
  brief: Project['brief'];
  planSpec: Project['planSpec'];
  voiceSpec?: Project['voiceSpec'];
  renderSpec?: Project['renderSpec'];
  tags?: string[];
}

/**
 * Project update request
 */
export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  brief?: Partial<Project['brief']>;
  planSpec?: Partial<Project['planSpec']>;
  voiceSpec?: Partial<Project['voiceSpec']>;
  renderSpec?: Partial<Project['renderSpec']>;
  status?: Project['status'];
  jobId?: string;
  outputPath?: string;
  tags?: string[];
  thumbnail?: string;
}

/**
 * Project list response
 */
export interface ProjectListResponse {
  projects: Project[];
  total: number;
  page: number;
  pageSize: number;
}

/**
 * Project list filters
 */
export interface ProjectListFilters {
  page?: number;
  pageSize?: number;
  status?: Project['status'];
  search?: string;
  tags?: string[];
  sortBy?: 'createdAt' | 'updatedAt' | 'name';
  sortOrder?: 'asc' | 'desc';
}

/**
 * Get all projects with optional filters
 */
export async function listProjects(
  filters?: ProjectListFilters,
  config?: ExtendedAxiosRequestConfig
): Promise<ProjectListResponse> {
  const params = new URLSearchParams();

  if (filters) {
    if (filters.page !== undefined) params.append('page', filters.page.toString());
    if (filters.pageSize !== undefined) params.append('pageSize', filters.pageSize.toString());
    if (filters.status) params.append('status', filters.status);
    if (filters.search) params.append('search', filters.search);
    if (filters.tags) filters.tags.forEach((tag) => params.append('tags', tag));
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);
  }

  const queryString = params.toString();
  const url = queryString ? `/api/projects?${queryString}` : '/api/projects';

  return get<ProjectListResponse>(url, config);
}

/**
 * Get a single project by ID
 */
export async function getProject(
  id: string,
  config?: ExtendedAxiosRequestConfig
): Promise<Project> {
  return get<Project>(`/api/projects/${id}`, config);
}

/**
 * Create a new project
 */
export async function createProject(
  project: CreateProjectRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<Project> {
  return post<Project>('/api/projects', project, config);
}

/**
 * Update an existing project
 */
export async function updateProject(
  id: string,
  updates: UpdateProjectRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<Project> {
  return put<Project>(`/api/projects/${id}`, updates, config);
}

/**
 * Delete a project
 */
export async function deleteProject(
  id: string,
  config?: ExtendedAxiosRequestConfig
): Promise<void> {
  return del<void>(`/api/projects/${id}`, config);
}

/**
 * Duplicate a project
 */
export async function duplicateProject(
  id: string,
  config?: ExtendedAxiosRequestConfig
): Promise<Project> {
  return post<Project>(`/api/projects/${id}/duplicate`, undefined, config);
}

/**
 * Get project statistics
 */
export interface ProjectStatistics {
  totalProjects: number;
  byStatus: Record<Project['status'], number>;
  recentProjects: Project[];
  totalVideosGenerated: number;
}

export async function getProjectStatistics(
  config?: ExtendedAxiosRequestConfig
): Promise<ProjectStatistics> {
  return get<ProjectStatistics>('/api/projects/statistics', config);
}

/**
 * Export project configuration
 */
export async function exportProject(
  id: string,
  config?: ExtendedAxiosRequestConfig
): Promise<Blob> {
  return get<Blob>(`/api/projects/${id}/export`, {
    ...config,
    responseType: 'blob' as any,
  });
}

/**
 * Import project from configuration
 */
export async function importProject(
  file: File,
  config?: ExtendedAxiosRequestConfig
): Promise<Project> {
  const formData = new FormData();
  formData.append('file', file);

  return post<Project>('/api/projects/import', formData, {
    ...config,
    headers: {
      ...config?.headers,
      'Content-Type': 'multipart/form-data',
    },
  });
}

/**
 * Create abort controller for request cancellation
 */
export { createAbortController };
