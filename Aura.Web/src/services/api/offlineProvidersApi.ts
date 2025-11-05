import apiClient from './apiClient';
import type { OfflineProvidersStatus, OfflineProviderStatus } from '@/types/offlineProviders';

/**
 * API client for offline provider availability checks
 */
export const offlineProvidersApi = {
  /**
   * Check availability of all offline providers
   */
  async checkAll(): Promise<OfflineProvidersStatus> {
    const response = await apiClient.get<OfflineProvidersStatus>('/api/offline-providers/status');
    return response.data;
  },

  /**
   * Check if Piper TTS is available
   */
  async checkPiper(): Promise<OfflineProviderStatus> {
    const response = await apiClient.get<OfflineProviderStatus>('/api/offline-providers/piper');
    return response.data;
  },

  /**
   * Check if Mimic3 TTS is available
   */
  async checkMimic3(): Promise<OfflineProviderStatus> {
    const response = await apiClient.get<OfflineProviderStatus>('/api/offline-providers/mimic3');
    return response.data;
  },

  /**
   * Check if Ollama is available
   */
  async checkOllama(): Promise<OfflineProviderStatus> {
    const response = await apiClient.get<OfflineProviderStatus>('/api/offline-providers/ollama');
    return response.data;
  },

  /**
   * Check if Stable Diffusion WebUI is available
   */
  async checkStableDiffusion(): Promise<OfflineProviderStatus> {
    const response = await apiClient.get<OfflineProviderStatus>(
      '/api/offline-providers/stable-diffusion'
    );
    return response.data;
  },

  /**
   * Check if Windows TTS is available
   */
  async checkWindowsTts(): Promise<OfflineProviderStatus> {
    const response = await apiClient.get<OfflineProviderStatus>(
      '/api/offline-providers/windows-tts'
    );
    return response.data;
  },
};
