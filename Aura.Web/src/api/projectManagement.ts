import { typedClient } from './typedClient';

export interface Project {
  id: string;
  title: string;
  description?: string;
  status: 'Draft' | 'InProgress' | 'Completed' | 'Failed' | 'Cancelled';
  category?: string;
  tags: string[];
  thumbnailPath?: string;
  outputFilePath?: string;
  durationSeconds?: number;
  currentWizardStep: number;
  progressPercent: number;
  sceneCount: number;
  assetCount: number;
  templateId?: string;
  createdAt: string;
  updatedAt: string;
  lastAutoSaveAt?: string;
  createdBy?: string;
}

export interface ProjectDetails extends Project {
  jobId?: string;
  briefJson?: string;
  planSpecJson?: string;
  voiceSpecJson?: string;
  renderSpecJson?: string;
  errorMessage?: string;
  scenes: Scene[];
  assets: Asset[];
  checkpoints: Checkpoint[];
  modifiedBy?: string;
  completedAt?: string;
}

export interface Scene {
  id: string;
  sceneIndex: number;
  scriptText: string;
  audioFilePath?: string;
  imageFilePath?: string;
  durationSeconds: number;
  isCompleted: boolean;
}

export interface Asset {
  id: string;
  assetType: string;
  filePath: string;
  fileSizeBytes: number;
  mimeType?: string;
  isTemporary: boolean;
  createdAt: string;
}

export interface Checkpoint {
  id: string;
  stageName: string;
  checkpointTime: string;
  completedScenes: number;
  totalScenes: number;
  outputFilePath?: string;
  isValid: boolean;
}

export interface ProjectVersion {
  id: string;
  projectId: string;
  versionNumber: number;
  name?: string;
  description?: string;
  versionType: string;
  trigger?: string;
  storageSizeBytes: number;
  isMarkedImportant: boolean;
  createdAt: string;
  createdBy?: string;
}

export interface Template {
  id: string;
  name: string;
  description: string;
  category: string;
  subCategory: string;
  tags: string[];
  previewImage?: string;
  previewVideo?: string;
  isSystemTemplate: boolean;
  isCommunityTemplate: boolean;
  author: string;
  usageCount: number;
  rating: number;
  ratingCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TemplateDetails extends Template {
  templateData: string;
}

export interface ProjectsResponse {
  projects: Project[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

export interface ProjectStatistics {
  totalProjects: number;
  draftProjects: number;
  inProgressProjects: number;
  completedProjects: number;
  failedProjects: number;
}

export const projectManagementApi = {
  // Project CRUD operations
  async getProjects(
    params: {
      search?: string;
      status?: string;
      category?: string;
      tags?: string;
      fromDate?: string;
      toDate?: string;
      sortBy?: string;
      ascending?: boolean;
      page?: number;
      pageSize?: number;
    } = {}
  ): Promise<ProjectsResponse> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });

    return await typedClient.get<ProjectsResponse>(
      `/api/project-management/projects?${queryParams.toString()}`
    );
  },

  async getProject(projectId: string): Promise<ProjectDetails> {
    return await typedClient.get<ProjectDetails>(`/api/project-management/projects/${projectId}`);
  },

  async createProject(data: {
    title: string;
    description?: string;
    category?: string;
    tags?: string[];
    templateId?: string;
  }): Promise<{ id: string; title: string; status: string; createdAt: string }> {
    return await typedClient.post<{ id: string; title: string; status: string; createdAt: string }>(
      '/api/project-management/projects',
      data
    );
  },

  async updateProject(
    projectId: string,
    data: {
      title?: string;
      description?: string;
      category?: string;
      tags?: string[];
      status?: string;
      currentWizardStep?: number;
      thumbnailPath?: string;
      outputFilePath?: string;
      durationSeconds?: number;
    }
  ): Promise<{ id: string; title: string; status: string; updatedAt: string }> {
    return await typedClient.put<{ id: string; title: string; status: string; updatedAt: string }>(
      `/api/project-management/projects/${projectId}`,
      data
    );
  },

  async autoSaveProject(
    projectId: string,
    data: {
      briefJson?: string;
      planSpecJson?: string;
      voiceSpecJson?: string;
      renderSpecJson?: string;
    }
  ): Promise<{ success: boolean; autoSavedAt: string }> {
    return await typedClient.post<{ success: boolean; autoSavedAt: string }>(
      `/api/project-management/projects/${projectId}/auto-save`,
      data
    );
  },

  async duplicateProject(projectId: string): Promise<{
    id: string;
    title: string;
    status: string;
    createdAt: string;
  }> {
    return await typedClient.post<{
      id: string;
      title: string;
      status: string;
      createdAt: string;
    }>(`/api/project-management/projects/${projectId}/duplicate`, {});
  },

  async deleteProject(projectId: string): Promise<void> {
    await typedClient.delete(`/api/project-management/projects/${projectId}`);
  },

  async bulkDeleteProjects(projectIds: string[]): Promise<{
    deletedCount: number;
    requestedCount: number;
  }> {
    return await typedClient.post<{
      deletedCount: number;
      requestedCount: number;
    }>('/api/project-management/projects/bulk-delete', { projectIds });
  },

  // Project versions
  async getProjectVersions(projectId: string): Promise<{ versions: ProjectVersion[] }> {
    return await typedClient.get<{ versions: ProjectVersion[] }>(
      `/api/project-management/projects/${projectId}/versions`
    );
  },

  // Metadata
  async getCategories(): Promise<{ categories: string[] }> {
    return await typedClient.get<{ categories: string[] }>('/api/project-management/categories');
  },

  async getTags(): Promise<{ tags: string[] }> {
    return await typedClient.get('/api/project-management/tags');
  },

  async getStatistics(): Promise<ProjectStatistics> {
    return await typedClient.get<ProjectStatistics>('/api/project-management/statistics');
  },

  // Template operations
  async getTemplates(
    params: {
      category?: string;
      subCategory?: string;
      isSystemTemplate?: boolean;
    } = {}
  ): Promise<{ templates: Template[] }> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });

    return await typedClient.get(`/api/template-management/templates?${queryParams.toString()}`);
  },

  async getTemplate(templateId: string): Promise<TemplateDetails> {
    return await typedClient.get<TemplateDetails>(
      `/api/template-management/templates/${templateId}`
    );
  },

  async createTemplate(data: {
    name: string;
    description?: string;
    category?: string;
    subCategory?: string;
    templateData?: string;
    tags?: string[];
    previewImage?: string;
    previewVideo?: string;
  }): Promise<{ id: string; name: string; createdAt: string }> {
    return await typedClient.post('/api/template-management/templates', data);
  },

  async createProjectFromTemplate(
    templateId: string,
    projectName?: string
  ): Promise<{ id: string; title: string; templateId: string; createdAt: string }> {
    return await typedClient.post(
      `/api/template-management/templates/${templateId}/create-project`,
      { projectName }
    );
  },

  async deleteTemplate(templateId: string): Promise<void> {
    await typedClient.delete(`/api/template-management/templates/${templateId}`);
  },

  async seedSystemTemplates(): Promise<{ message: string }> {
    return await typedClient.post('/api/template-management/templates/seed', {});
  },
};
