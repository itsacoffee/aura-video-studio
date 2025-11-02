import apiClient from './apiClient';
import type {
  OllamaStatusResponse,
  OllamaStartResponse,
  OllamaStopResponse,
  OllamaLogsResponse,
} from '@/types/api-v1';

/**
 * API client for Ollama process control
 */
export const ollamaClient = {
  /**
   * Get Ollama service status
   */
  async getStatus(): Promise<OllamaStatusResponse> {
    const response = await apiClient.get<OllamaStatusResponse>('/api/ollama/status');
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
   * List available Ollama models (returns raw models array)
   */
  async getModels(): Promise<{
    models: Array<{ name: string; size?: string; modifiedAt?: string }>;
  }> {
    const response = await apiClient.get<{
      models: Array<{ name: string; size?: string; modifiedAt?: string }>;
    }>('/api/ollama/models');
    return response.data;
  },
};
