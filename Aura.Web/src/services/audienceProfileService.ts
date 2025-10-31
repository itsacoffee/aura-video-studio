import type {
  AudienceProfileDto,
  AudienceProfileResponse,
  AudienceProfileListResponse,
  CreateAudienceProfileRequest,
  UpdateAudienceProfileRequest,
  AnalyzeAudienceRequest,
  AnalyzeAudienceResponse,
} from '../types/api-v1';
import apiClient from './api/apiClient';

/**
 * Service for managing audience profiles
 */
export class AudienceProfileService {
  /**
   * Get all audience profiles
   */
  static async getProfiles(
    templatesOnly?: boolean,
    page: number = 1,
    pageSize: number = 50
  ): Promise<AudienceProfileListResponse> {
    const params = new URLSearchParams();
    if (templatesOnly !== undefined) {
      params.append('templatesOnly', String(templatesOnly));
    }
    params.append('page', String(page));
    params.append('pageSize', String(pageSize));

    const response = await apiClient.get<AudienceProfileListResponse>(
      `/api/audience/profiles?${params.toString()}`
    );
    return response.data;
  }

  /**
   * Get a specific audience profile by ID
   */
  static async getProfile(id: string): Promise<AudienceProfileResponse> {
    const response = await apiClient.get<AudienceProfileResponse>(`/api/audience/profiles/${id}`);
    return response.data;
  }

  /**
   * Create a new audience profile
   */
  static async createProfile(profile: AudienceProfileDto): Promise<AudienceProfileResponse> {
    const request: CreateAudienceProfileRequest = { profile };
    const response = await apiClient.post<AudienceProfileResponse>(
      '/api/audience/profiles',
      request
    );
    return response.data;
  }

  /**
   * Update an existing audience profile
   */
  static async updateProfile(
    id: string,
    profile: AudienceProfileDto
  ): Promise<AudienceProfileResponse> {
    const request: UpdateAudienceProfileRequest = { profile };
    const response = await apiClient.put<AudienceProfileResponse>(
      `/api/audience/profiles/${id}`,
      request
    );
    return response.data;
  }

  /**
   * Delete an audience profile
   */
  static async deleteProfile(id: string): Promise<void> {
    await apiClient.delete(`/api/audience/profiles/${id}`);
  }

  /**
   * Get all template profiles
   */
  static async getTemplates(): Promise<AudienceProfileListResponse> {
    const response = await apiClient.get<AudienceProfileListResponse>('/api/audience/templates');
    return response.data;
  }

  /**
   * Analyze script text and infer audience profile
   */
  static async analyzeAudience(scriptText: string): Promise<AnalyzeAudienceResponse> {
    const request: AnalyzeAudienceRequest = { scriptText };
    const response = await apiClient.post<AnalyzeAudienceResponse>(
      '/api/audience/analyze',
      request
    );
    return response.data;
  }

  /**
   * Toggle favorite status for a profile
   */
  static async toggleFavorite(id: string): Promise<AudienceProfileResponse> {
    const response = await apiClient.post<AudienceProfileResponse>(
      `/api/audience/profiles/${id}/favorite`
    );
    return response.data;
  }

  /**
   * Get all favorite profiles
   */
  static async getFavorites(): Promise<AudienceProfileListResponse> {
    const response = await apiClient.get<AudienceProfileListResponse>('/api/audience/favorites');
    return response.data;
  }

  /**
   * Move profile to a folder
   */
  static async moveToFolder(
    id: string,
    folderPath: string | null
  ): Promise<AudienceProfileResponse> {
    const request: { folderPath: string | null } = { folderPath };
    const response = await apiClient.post<AudienceProfileResponse>(
      `/api/audience/profiles/${id}/move`,
      request
    );
    return response.data;
  }

  /**
   * Get profiles in a specific folder
   */
  static async getProfilesByFolder(
    folderPath: string | null
  ): Promise<AudienceProfileListResponse> {
    const encodedPath = folderPath ? encodeURIComponent(folderPath) : '';
    const response = await apiClient.get<AudienceProfileListResponse>(
      `/api/audience/folders/${encodedPath}`
    );
    return response.data;
  }

  /**
   * Get all folder paths
   */
  static async getFolders(): Promise<string[]> {
    const response = await apiClient.get<{ folders: string[] }>('/api/audience/folders');
    return response.data.folders;
  }

  /**
   * Search profiles with full-text search
   */
  static async searchProfiles(query: string): Promise<AudienceProfileListResponse> {
    const response = await apiClient.get<AudienceProfileListResponse>(
      `/api/audience/search?query=${encodeURIComponent(query)}`
    );
    return response.data;
  }

  /**
   * Record profile usage for analytics
   */
  static async recordUsage(id: string): Promise<void> {
    await apiClient.post(`/api/audience/profiles/${id}/usage`);
  }

  /**
   * Export profile to JSON
   */
  static async exportProfile(id: string): Promise<string> {
    const response = await apiClient.get<{ json: string }>(`/api/audience/profiles/${id}/export`);
    return response.data.json;
  }

  /**
   * Import profile from JSON
   */
  static async importProfile(json: string): Promise<AudienceProfileResponse> {
    const request: { json: string } = { json };
    const response = await apiClient.post<AudienceProfileResponse>(
      '/api/audience/profiles/import',
      request
    );
    return response.data;
  }

  /**
   * Get recommended profiles based on topic and goal
   */
  static async recommendProfiles(
    topic: string,
    goal?: string | null,
    maxResults: number = 5
  ): Promise<AudienceProfileListResponse> {
    const request: { topic: string; goal?: string | null; maxResults?: number } = {
      topic,
      goal,
      maxResults,
    };
    const response = await apiClient.post<AudienceProfileListResponse>(
      '/api/audience/recommend',
      request
    );
    return response.data;
  }
}
