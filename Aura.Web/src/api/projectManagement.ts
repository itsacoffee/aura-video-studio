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
  async getProjects(params: {
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
  } = {}): Promise<ProjectsResponse> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });

    const response = await typedClient.get<ProjectsResponse>(
      `/api/project-management/projects?${queryParams.toString()}`
    );
    return response;
  },

  async getProject(projectId: string): Promise<ProjectDetails> {
    const response = await typedClient.get<ProjectDetails>(
      `/api/project-management/projects/${projectId}`
    );
    return response;
  },

  async createProject(data: {
    title: string;
    description?: string;
    category?: string;
    tags?: string[];
    templateId?: string;
  }): Promise<{ id: string; title: string; status: string; createdAt: string }> {
    const response = await typedClient.post(
      '/api/project-management/projects',
      data
    );
    return response;
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
    const response = await typedClient.put(
      `/api/project-management/projects/${projectId}`,
      data
    );
    return response;
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
    const response = await typedClient.post(
      `/api/project-management/projects/${projectId}/auto-save`,
      data
    );
    return response;
  },

  async duplicateProject(projectId: string): Promise<{
    id: string;
    title: string;
    status: string;
    createdAt: string;
  }> {
    const response = await typedClient.post(
      `/api/project-management/projects/${projectId}/duplicate`,
      {}
    );
    return response;
  },

  async deleteProject(projectId: string): Promise<void> {
    await typedClient.delete(`/api/project-management/projects/${projectId}`);
  },

  async bulkDeleteProjects(projectIds: string[]): Promise<{
    deletedCount: number;
    requestedCount: number;
  }> {
    const response = await typedClient.post(
      '/api/project-management/projects/bulk-delete',
      { projectIds }
    );
    return response;
  },

  // Project versions
  async getProjectVersions(projectId: string): Promise<{ versions: ProjectVersion[] }> {
    const response = await typedClient.get(
      `/api/project-management/projects/${projectId}/versions`
    );
    return response;
  },

  // Metadata
  async getCategories(): Promise<{ categories: string[] }> {
    const response = await typedClient.get('/api/project-management/categories');
    return response;
  },

  async getTags(): Promise<{ tags: string[] }> {
    const response = await typedClient.get('/api/project-management/tags');
    return response;
  },

  async getStatistics(): Promise<ProjectStatistics> {
    const response = await typedClient.get<ProjectStatistics>(
      '/api/project-management/statistics'
    );
    return response;
  },

  // Template operations
  async getTemplates(params: {
    category?: string;
    subCategory?: string;
    isSystemTemplate?: boolean;
  } = {}): Promise<{ templates: Template[] }> {
    const queryParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        queryParams.append(key, String(value));
      }
    });

    const response = await typedClient.get(
      `/api/template-management/templates?${queryParams.toString()}`
    );
    return response;
  },

  async getTemplate(templateId: string): Promise<TemplateDetails> {
    const response = await typedClient.get<TemplateDetails>(
      `/api/template-management/templates/${templateId}`
    );
    return response;
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
    const response = await typedClient.post(
      '/api/template-management/templates',
      data
    );
    return response;
  },

  async createProjectFromTemplate(
    templateId: string,
    projectName?: string
  ): Promise<{ id: string; title: string; templateId: string; createdAt: string }> {
    const response = await typedClient.post(
      `/api/template-management/templates/${templateId}/create-project`,
      { projectName }
    );
    return response;
  },

  async deleteTemplate(templateId: string): Promise<void> {
    await typedClient.delete(`/api/template-management/templates/${templateId}`);
  },

  async seedSystemTemplates(): Promise<{ message: string }> {
    const response = await typedClient.post(
      '/api/template-management/templates/seed',
      {}
    );
    return response;
  },
};
