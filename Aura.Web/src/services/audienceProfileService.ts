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
}
