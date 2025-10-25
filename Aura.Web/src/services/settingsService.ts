import { apiUrl } from '../config/api';
import type { UserSettings } from '../types/settings';
import { createDefaultSettings } from '../types/settings';
import { get, post } from './api/apiClient';

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
      console.error('Error loading settings from backend:', error);
      
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
      console.error('Error saving settings:', error);
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
      console.error('Error importing settings:', error);
      return null;
    }
  }

  /**
   * Test API key connection for a specific provider
   */
  async testApiKey(provider: string, apiKey: string): Promise<{ success: boolean; message: string }> {
    try {
      const response = await fetch(apiUrl(`/api/settings/test-api-key/${provider}`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey }),
      });

      if (response.ok) {
        const result = await response.json();
        return result;
      }

      return {
        success: false,
        message: 'Failed to test API key',
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
        const result = await response.json();
        return result;
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
      console.error('Error reading from localStorage:', error);
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
      console.error('Error saving to localStorage:', error);
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
