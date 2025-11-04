import apiClient from '../services/api/apiClient';
import type {
  ProviderProfileDto,
  ProfileValidationResultDto,
  ProviderTestResultDto,
  TestProviderRequest,
  SaveApiKeysRequest,
  SetActiveProfileRequest,
  ProfileRecommendationDto,
} from '../types/api-v1';

/**
 * Get all provider profiles
 */
export async function getProfiles(): Promise<ProviderProfileDto[]> {
  const response = await apiClient.get<{ profiles: ProviderProfileDto[] }>(
    '/api/provider-profiles'
  );
  return response.data.profiles;
}

/**
 * Get the currently active profile
 */
export async function getActiveProfile(): Promise<ProviderProfileDto> {
  const response = await apiClient.get<ProviderProfileDto>('/api/provider-profiles/active');
  return response.data;
}

/**
 * Set the active provider profile
 */
export async function setActiveProfile(profileId: string): Promise<{
  success: boolean;
  message: string;
  profile: ProviderProfileDto;
}> {
  const request: SetActiveProfileRequest = { profileId };
  const response = await apiClient.post<{
    success: boolean;
    message: string;
    profile: ProviderProfileDto;
  }>('/api/provider-profiles/active', request);
  return response.data;
}

/**
 * Validate a provider profile
 */
export async function validateProfile(
  profileId: string
): Promise<ProfileValidationResultDto> {
  const response = await apiClient.post<ProfileValidationResultDto>(
    `/api/provider-profiles/${profileId}/validate`
  );
  return response.data;
}

/**
 * Get recommended profile based on available API keys
 */
export async function getRecommendedProfile(): Promise<ProfileRecommendationDto> {
  const response = await apiClient.get<ProfileRecommendationDto>(
    '/api/provider-profiles/recommend'
  );
  return response.data;
}

/**
 * Test a provider API key
 */
export async function testProvider(
  provider: string,
  apiKey?: string | null
): Promise<ProviderTestResultDto> {
  const request: TestProviderRequest = { provider, apiKey };
  const response = await apiClient.post<ProviderTestResultDto>(
    '/api/provider-profiles/test',
    request
  );
  return response.data;
}

/**
 * Save API keys
 */
export async function saveApiKeys(keys: Record<string, string>): Promise<{
  success: boolean;
  message: string;
  savedKeys: string[];
}> {
  const request: SaveApiKeysRequest = { keys };
  const response = await apiClient.post<{
    success: boolean;
    message: string;
    savedKeys: string[];
  }>('/api/provider-profiles/keys', request);
  return response.data;
}

/**
 * Get stored API keys (masked for security)
 */
export async function getApiKeys(): Promise<Record<string, string>> {
  const response = await apiClient.get<{ keys: Record<string, string> }>(
    '/api/provider-profiles/keys'
  );
  return response.data.keys;
}
