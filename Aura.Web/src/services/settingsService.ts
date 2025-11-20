import { apiUrl } from '../config/api';
import type { UserSettings } from '../types/settings';
import { createDefaultSettings } from '../types/settings';
import { get, post } from './api/apiClient';
import { providerPingClient } from './api/providerPingClient';
import { loggingService as logger } from './loggingService';

const STORAGE_KEY = 'aura-user-settings';
const CACHE_EXPIRY_MS = 5 * 60 * 1000; // 5 minutes

interface CachedSettings {
  data: UserSettings;
  timestamp: number;
}

/**
 * Settings service for managing user settings with backend persistence
 * and localStorage caching for offline access
 */
export class SettingsService {
  private cache: CachedSettings | null = null;

  /**
   * Load settings from backend with localStorage fallback
   */
  async loadSettings(): Promise<UserSettings> {
    // Check cache first
    const cached = this.getFromCache();
    if (cached) {
      return cached;
    }

    try {
      const settings = await get<UserSettings>(apiUrl('/api/settings/user'));
      this.saveToCache(settings);
      this.saveToLocalStorage(settings);
      return settings;
    } catch (error) {
      logger.error(
        'Error loading settings from backend',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'loadSettings'
      );

      // Fallback to localStorage
      const localSettings = this.getFromLocalStorage();
      if (localSettings) {
        return localSettings;
      }

      // Last resort: return defaults
      return createDefaultSettings();
    }
  }

  /**
   * Save settings to backend and localStorage
   */
  async saveSettings(settings: UserSettings): Promise<boolean> {
    try {
      await post<void>(apiUrl('/api/settings/user'), settings);
      this.saveToCache(settings);
      this.saveToLocalStorage(settings);
      return true;
    } catch (error) {
      logger.error(
        'Error saving settings',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'saveSettings'
      );
      // Still save to localStorage even if backend fails
      this.saveToLocalStorage(settings);
      return false;
    }
  }

  /**
   * Reset settings to defaults
   */
  async resetToDefaults(): Promise<UserSettings> {
    const defaults = createDefaultSettings();
    await this.saveSettings(defaults);
    return defaults;
  }

  /**
   * Export settings as JSON file
   */
  exportSettings(settings: UserSettings, filename?: string): void {
    const exportData = {
      ...settings,
      exportedAt: new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || `aura-settings-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  /**
   * Import settings from JSON file
   */
  async importSettings(file: File): Promise<UserSettings | null> {
    try {
      const text = await file.text();
      const data = JSON.parse(text);

      // Basic validation
      if (!data.version) {
        throw new Error('Invalid settings file: missing version');
      }

      // Remove export timestamp if present
      delete data.exportedAt;

      // Merge with defaults to ensure all fields exist
      const defaults = createDefaultSettings();
      const settings: UserSettings = {
        ...defaults,
        ...data,
        general: { ...defaults.general, ...data.general },
        apiKeys: { ...defaults.apiKeys, ...data.apiKeys },
        fileLocations: { ...defaults.fileLocations, ...data.fileLocations },
        videoDefaults: { ...defaults.videoDefaults, ...data.videoDefaults },
        editorPreferences: { ...defaults.editorPreferences, ...data.editorPreferences },
        ui: { ...defaults.ui, ...data.ui },
        advanced: { ...defaults.advanced, ...data.advanced },
        lastUpdated: new Date().toISOString(),
      };

      return settings;
    } catch (error) {
      logger.error(
        'Error importing settings',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'importSettings'
      );
      return null;
    }
  }

  /**
   * Test API key connection for a specific provider
   */
  async testApiKey(
    provider: string,
    apiKey: string
  ): Promise<{
    success: boolean;
    message: string;
    responseTimeMs?: number | null;
    statusCode?: number | null;
    errorCode?: string | null;
    endpoint?: string | null;
    correlationId?: string | null;
  }> {
    const trimmedKey = apiKey.trim();
    if (!trimmedKey) {
      return {
        success: false,
        message: 'API key is required',
      };
    }

    const mapping = mapProviderToIdentifiers(provider);

    try {
      const saveResponse = await fetch(apiUrl('/api/keys/set'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider: mapping.storageName,
          apiKey: trimmedKey,
        }),
      });

      if (!saveResponse.ok) {
        const errorText = await saveResponse.text();
        return {
          success: false,
          message: errorText || `Failed to persist API key for ${mapping.displayName}.`,
        };
      }

      const pingResult = await providerPingClient.pingProvider(mapping.pingName);

      return {
        success: pingResult.success,
        message:
          pingResult.message ||
          (pingResult.success
            ? `${mapping.displayName} is reachable.`
            : `${mapping.displayName} did not respond.`),
        responseTimeMs: pingResult.latencyMs,
        statusCode: pingResult.statusCode,
        errorCode: pingResult.errorCode,
        endpoint: pingResult.endpoint,
        correlationId: pingResult.correlationId,
      };
    } catch (error) {
      return {
        success: false,
        message: `Network error: ${error}`,
      };
    }
  }

  /**
   * Get available models for OpenAI API key
   */
  async getOpenAIModels(
    apiKey: string
  ): Promise<{ success: boolean; models?: string[]; message?: string }> {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 15000); // 15 second timeout

      try {
        const response = await fetch(apiUrl('/api/providers/openai/models'), {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ apiKey }),
          signal: controller.signal,
        });

        clearTimeout(timeoutId);

        const data = await response.json();

        if (response.ok && data.success) {
          return {
            success: true,
            models: data.models || [],
          };
        }

        return {
          success: false,
          message: data.message || 'Failed to fetch models',
        };
      } catch (error: unknown) {
        clearTimeout(timeoutId);

        if (error instanceof Error && error.name === 'AbortError') {
          return {
            success: false,
            message: 'Connection timeout - check network connectivity',
          };
        }
        throw error;
      }
    } catch (error) {
      return {
        success: false,
        message: `Network error: ${error}`,
      };
    }
  }

  /**
   * Test OpenAI script generation with a specific model
   */
  async testOpenAIGeneration(
    apiKey: string,
    model: string
  ): Promise<{
    success: boolean;
    generatedText?: string;
    responseTimeMs?: number;
    message?: string;
  }> {
    try {
      const response = await fetch(apiUrl('/api/providers/openai/test-generation'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey, model }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        return {
          success: true,
          generatedText: data.generatedText,
          responseTimeMs: data.responseTimeMs,
        };
      }

      return {
        success: false,
        message: data.message || 'Failed to test generation',
        responseTimeMs: data.responseTimeMs,
      };
    } catch (error) {
      return {
        success: false,
        message: `Network error: ${error}`,
      };
    }
  }

  /**
   * Validate file path exists
   */
  async validatePath(path: string): Promise<{ valid: boolean; message: string }> {
    try {
      const response = await fetch(apiUrl('/api/settings/validate-path'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path }),
      });

      if (response.ok) {
        return await response.json();
      }

      return {
        valid: false,
        message: 'Failed to validate path',
      };
    } catch (error) {
      return {
        valid: false,
        message: `Network error: ${error}`,
      };
    }
  }

  /**
   * Get from memory cache
   */
  private getFromCache(): UserSettings | null {
    if (!this.cache) return null;

    const age = Date.now() - this.cache.timestamp;
    if (age > CACHE_EXPIRY_MS) {
      this.cache = null;
      return null;
    }

    return this.cache.data;
  }

  /**
   * Save to memory cache
   */
  private saveToCache(settings: UserSettings): void {
    this.cache = {
      data: settings,
      timestamp: Date.now(),
    };
  }

  /**
   * Get from localStorage
   */
  private getFromLocalStorage(): UserSettings | null {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        return JSON.parse(stored);
      }
    } catch (error) {
      logger.error(
        'Error reading from localStorage',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'getFromLocalStorage'
      );
    }
    return null;
  }

  /**
   * Save to localStorage
   */
  private saveToLocalStorage(settings: UserSettings): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
    } catch (error) {
      logger.error(
        'Error saving to localStorage',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'saveToLocalStorage'
      );
    }
  }

  /**
   * Verify settings are synchronized with backend on app startup
   * Compares localStorage cache with backend and updates if needed
   */
  async verifySyncWithBackend(): Promise<boolean> {
    try {
      const backendSettings = await get<UserSettings>(apiUrl('/api/settings/user'));
      const localSettings = this.getFromLocalStorage();

      if (!localSettings || JSON.stringify(backendSettings) !== JSON.stringify(localSettings)) {
        this.saveToLocalStorage(backendSettings);
        this.saveToCache(backendSettings);
        logger.info(
          'Settings synchronized with backend',
          'settingsService',
          'verifySyncWithBackend'
        );
        return true;
      }
      logger.info(
        'Settings already in sync with backend',
        'settingsService',
        'verifySyncWithBackend'
      );
      return true;
    } catch (error) {
      logger.error(
        'Settings sync verification failed',
        error instanceof Error ? error : new Error(String(error)),
        'settingsService',
        'verifySyncWithBackend'
      );
      return false;
    }
  }

  /**
   * Clear cache (useful for testing)
   */
  clearCache(): void {
    this.cache = null;
  }
}

// Export singleton instance
export const settingsService = new SettingsService();

interface ProviderIdentifierMap {
  storageName: string;
  pingName: string;
  displayName: string;
}

const providerIdentifierMap: Record<string, ProviderIdentifierMap> = {
  openai: { storageName: 'OpenAI', pingName: 'openai', displayName: 'OpenAI' },
  anthropic: { storageName: 'Anthropic', pingName: 'anthropic', displayName: 'Anthropic' },
  google: { storageName: 'Gemini', pingName: 'gemini', displayName: 'Google Gemini' },
  gemini: { storageName: 'Gemini', pingName: 'gemini', displayName: 'Google Gemini' },
  elevenlabs: { storageName: 'ElevenLabs', pingName: 'elevenlabs', displayName: 'ElevenLabs' },
  stabilityai: { storageName: 'StabilityAI', pingName: 'stabilityai', displayName: 'Stability AI' },
  azure: { storageName: 'AzureOpenAI', pingName: 'azureopenai', displayName: 'Azure OpenAI' },
  azureopenai: { storageName: 'AzureOpenAI', pingName: 'azureopenai', displayName: 'Azure OpenAI' },
  pexels: { storageName: 'Pexels', pingName: 'pexels', displayName: 'Pexels' },
  pixabay: { storageName: 'Pixabay', pingName: 'pixabay', displayName: 'Pixabay' },
  unsplash: { storageName: 'Unsplash', pingName: 'unsplash', displayName: 'Unsplash' },
  playht: { storageName: 'PlayHT', pingName: 'playht', displayName: 'PlayHT' },
};

function mapProviderToIdentifiers(provider: string): ProviderIdentifierMap {
  const normalized = provider.trim().toLowerCase();
  return (
    providerIdentifierMap[normalized] ?? {
      storageName: provider,
      pingName: normalized,
      displayName: provider,
    }
  );
}
