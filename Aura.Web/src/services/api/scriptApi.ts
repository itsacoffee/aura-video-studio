/**
 * Script Generation API Service
 * Provides typed methods for script generation, editing, and provider selection
 */

import { get, post, put } from './apiClient';
import type { ExtendedAxiosRequestConfig } from './apiClient';

/**
 * Script generation request
 */
export interface GenerateScriptRequest {
  topic: string;
  audience?: string;
  goal?: string;
  tone?: string;
  language?: string;
  aspect?: string;
  targetDurationSeconds?: number;
  pacing?: string;
  density?: string;
  style?: string;
  preferredProvider?: string;
  modelOverride?: string;
}

/**
 * Script scene DTO
 */
export interface ScriptSceneDto {
  number: number;
  narration: string;
  visualPrompt: string;
  durationSeconds: number;
  transition: string;
}

/**
 * Script metadata DTO
 */
export interface ScriptMetadataDto {
  generatedAt: string;
  providerName: string;
  modelUsed: string;
  tokensUsed: number;
  estimatedCost: number;
  tier: string;
  generationTimeSeconds: number;
}

/**
 * Script generation response
 */
export interface GenerateScriptResponse {
  scriptId: string;
  title: string;
  scenes: ScriptSceneDto[];
  totalDurationSeconds: number;
  metadata: ScriptMetadataDto;
  correlationId: string;
}

/**
 * Update scene request
 */
export interface UpdateSceneRequest {
  narration?: string;
  visualPrompt?: string;
  durationSeconds?: number;
}

/**
 * Regenerate script request
 */
export interface RegenerateScriptRequest {
  preferredProvider?: string;
  modelOverride?: string;
}

/**
 * Provider info DTO
 */
export interface ProviderInfoDto {
  name: string;
  tier: string;
  isAvailable: boolean;
  requiresInternet: boolean;
  requiresApiKey: boolean;
  capabilities: string[];
  defaultModel: string;
  estimatedCostPer1KTokens: number;
  availableModels: string[];
}

/**
 * Providers list response
 */
export interface ProvidersListResponse {
  providers: ProviderInfoDto[];
  correlationId: string;
}

/**
 * Generate a new script
 */
export async function generateScript(
  request: GenerateScriptRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<GenerateScriptResponse> {
  return post<GenerateScriptResponse>('/api/scripts/generate', request, config);
}

/**
 * Get a previously generated script by ID
 */
export async function getScript(
  scriptId: string,
  config?: ExtendedAxiosRequestConfig
): Promise<GenerateScriptResponse> {
  return get<GenerateScriptResponse>(`/api/scripts/${scriptId}`, config);
}

/**
 * Update a specific scene in a script
 */
export async function updateScene(
  scriptId: string,
  sceneNumber: number,
  request: UpdateSceneRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<GenerateScriptResponse> {
  return put<GenerateScriptResponse>(
    `/api/scripts/${scriptId}/scenes/${sceneNumber}`,
    request,
    config
  );
}

/**
 * Regenerate a script with same or different provider
 */
export async function regenerateScript(
  scriptId: string,
  request: RegenerateScriptRequest,
  config?: ExtendedAxiosRequestConfig
): Promise<GenerateScriptResponse> {
  return post<GenerateScriptResponse>(`/api/scripts/${scriptId}/regenerate`, request, config);
}

/**
 * Regenerate a specific scene in a script
 */
export async function regenerateScene(
  scriptId: string,
  sceneNumber: number,
  config?: ExtendedAxiosRequestConfig
): Promise<GenerateScriptResponse> {
  return post<GenerateScriptResponse>(
    `/api/scripts/${scriptId}/scenes/${sceneNumber}/regenerate`,
    {},
    config
  );
}

/**
 * List available LLM providers and their status
 */
export async function listProviders(
  config?: ExtendedAxiosRequestConfig
): Promise<ProvidersListResponse> {
  return get<ProvidersListResponse>('/api/scripts/providers', config);
}

/**
 * Export script as text or markdown
 */
export async function exportScript(
  scriptId: string,
  format: 'text' | 'markdown' = 'text',
  config?: ExtendedAxiosRequestConfig
): Promise<Blob> {
  return await get<Blob>(`/api/scripts/${scriptId}/export?format=${format}`, {
    ...config,
    responseType: 'blob',
  });
}
