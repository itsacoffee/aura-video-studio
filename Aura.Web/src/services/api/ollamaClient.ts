import apiClient from './apiClient';
import type {
  OllamaStatusResponse,
  OllamaStartResponse,
  OllamaStopResponse,
  OllamaLogsResponse,
} from '@/types/api-v1';

/**
 * Recommended model information
 */
export interface RecommendedModel {
  name: string;
  displayName: string;
  description: string;
  size: string;
  sizeBytes: number;
  isRecommended: boolean;
}

/**
 * Model pull progress event
 */
export interface ModelPullProgress {
  status: string;
  completed: number;
  total: number;
  percentComplete: number;
}

/**
 * Extended status response with installation info
 */
export interface ExtendedOllamaStatus extends OllamaStatusResponse {
  installed?: boolean;
  installPath?: string;
  version?: string;
}

/**
 * API client for Ollama process control
 */
export const ollamaClient = {
  /**
   * Get Ollama service status
   */
  async getStatus(): Promise<ExtendedOllamaStatus> {
    const response = await apiClient.get<ExtendedOllamaStatus>('/api/ollama/status');
    return response.data;
  },

  /**
   * Start Ollama server process
   */
  async start(): Promise<OllamaStartResponse> {
    const response = await apiClient.post<OllamaStartResponse>('/api/ollama/start');
    return response.data;
  },

  /**
   * Stop Ollama server process
   */
  async stop(): Promise<OllamaStopResponse> {
    const response = await apiClient.post<OllamaStopResponse>('/api/ollama/stop');
    return response.data;
  },

  /**
   * Get recent Ollama log entries
   */
  async getLogs(maxLines = 200): Promise<OllamaLogsResponse> {
    const response = await apiClient.get<OllamaLogsResponse>('/api/ollama/logs', {
      params: { maxLines },
    });
    return response.data;
  },

  /**
   * List available Ollama models
   */
  async getModels(): Promise<{
    models: Array<{ name: string; size?: string; modifiedAt?: string }>;
  }> {
    const response = await apiClient.get<{
      models: Array<{ name: string; size?: string; modifiedAt?: string }>;
    }>('/api/ollama/models');
    return response.data;
  },

  /**
   * Get recommended models for script generation
   */
  async getRecommendedModels(): Promise<{ models: RecommendedModel[] }> {
    const response = await apiClient.get<{ models: RecommendedModel[] }>(
      '/api/ollama/models/recommended'
    );
    return response.data;
  },

  /**
   * Pull a model from Ollama registry
   */
  async pullModel(
    modelName: string
  ): Promise<{ success: boolean; message: string; modelName: string }> {
    const response = await apiClient.post<{ success: boolean; message: string; modelName: string }>(
      `/api/ollama/models/${encodeURIComponent(modelName)}/pull`
    );
    return response.data;
  },

  /**
   * Delete a model from local storage
   */
  async deleteModel(
    modelName: string
  ): Promise<{ success: boolean; message: string; modelName: string }> {
    const response = await apiClient.delete<{
      success: boolean;
      message: string;
      modelName: string;
    }>(`/api/ollama/models/${encodeURIComponent(modelName)}`);
    return response.data;
  },

  /**
   * Install Ollama via the engines API
   */
  async install(): Promise<{ success: boolean; installPath: string; message: string }> {
    const response = await apiClient.post<{
      success: boolean;
      installPath: string;
      message: string;
    }>('/api/engines/install', { engineId: 'ollama' });
    return response.data;
  },

  /**
   * Check if a model is available locally
   */
  async checkModelAvailable(
    modelName: string
  ): Promise<{ modelName: string; isAvailable: boolean }> {
    const response = await apiClient.get<{ modelName: string; isAvailable: boolean }>(
      `/api/ollama/models/${encodeURIComponent(modelName)}/available`
    );
    return response.data;
  },

  /**
   * Get the recommended default Ollama model based on available models
   * Returns the best model for script generation based on priority order
   */
  async getRecommendedModel(): Promise<{
    success: boolean;
    recommendedModel: string | null;
    message: string;
  }> {
    const response = await apiClient.get<{
      success: boolean;
      recommendedModel: string | null;
      message: string;
    }>('/api/providers/ollama/recommended-model');
    return response.data;
  },
};
