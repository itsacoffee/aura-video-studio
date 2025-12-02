/**
 * Graphics Settings Service
 * Manages graphics and visual settings with backend persistence and local caching
 */

import { apiUrl } from '../config/api';
import type { GraphicsSettings, PerformanceProfile } from '../types/graphicsSettings';
import { createDefaultGraphicsSettings } from '../types/graphicsSettings';
import { get, post } from './api/apiClient';
import { loggingService as logger } from './loggingService';

const STORAGE_KEY = 'aura-graphics-settings';
const CACHE_EXPIRY_MS = 5 * 60 * 1000; // 5 minutes

interface CachedSettings {
  settings: GraphicsSettings;
  timestamp: number;
}

/**
 * Graphics Settings Service class for managing visual and performance settings
 */
class GraphicsSettingsService {
  private cache: CachedSettings | null = null;
  private listeners: Set<(settings: GraphicsSettings) => void> = new Set();

  /**
   * Load graphics settings from backend with localStorage fallback
   */
  async loadSettings(): Promise<GraphicsSettings> {
    // Check memory cache
    if (this.cache && Date.now() - this.cache.timestamp < CACHE_EXPIRY_MS) {
      return this.cache.settings;
    }

    try {
      const response = await get<GraphicsSettings>(apiUrl('/api/graphics'));
      this.updateCache(response);
      return response;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.warn(
        'Failed to load graphics settings from backend',
        'graphicsSettingsService',
        'loadSettings',
        { error: errorObj.message }
      );

      // Try localStorage
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        try {
          const settings = JSON.parse(stored) as GraphicsSettings;
          this.updateCache(settings);
          return settings;
        } catch {
          // Invalid stored data
        }
      }

      // Return defaults
      return createDefaultGraphicsSettings();
    }
  }

  /**
   * Save graphics settings
   */
  async saveSettings(settings: GraphicsSettings): Promise<boolean> {
    try {
      const response = await post<GraphicsSettings>(apiUrl('/api/graphics'), settings);
      this.updateCache(response);
      this.notifyListeners(response);

      // Also save to localStorage for offline access
      localStorage.setItem(STORAGE_KEY, JSON.stringify(response));

      logger.info('Graphics settings saved', 'graphicsSettingsService', 'saveSettings');
      return true;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error(
        'Failed to save graphics settings',
        errorObj,
        'graphicsSettingsService',
        'saveSettings'
      );

      // Still save to localStorage even if backend fails
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
      return false;
    }
  }

  /**
   * Apply a performance profile preset
   */
  async applyProfile(profile: PerformanceProfile): Promise<GraphicsSettings> {
    try {
      const response = await post<GraphicsSettings>(apiUrl(`/api/graphics/profile/${profile}`));
      this.updateCache(response);
      this.notifyListeners(response);

      // Also save to localStorage
      localStorage.setItem(STORAGE_KEY, JSON.stringify(response));

      logger.info(
        `Applied performance profile: ${profile}`,
        'graphicsSettingsService',
        'applyProfile'
      );
      return response;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error(
        'Failed to apply performance profile',
        errorObj,
        'graphicsSettingsService',
        'applyProfile'
      );
      throw error;
    }
  }

  /**
   * Detect optimal settings based on hardware
   */
  async detectOptimalSettings(): Promise<GraphicsSettings> {
    try {
      const response = await post<GraphicsSettings>(apiUrl('/api/graphics/detect'));
      this.updateCache(response);
      this.notifyListeners(response);

      // Also save to localStorage
      localStorage.setItem(STORAGE_KEY, JSON.stringify(response));

      logger.info(
        'Detected optimal graphics settings',
        'graphicsSettingsService',
        'detectOptimalSettings'
      );
      return response;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error(
        'Failed to detect optimal settings',
        errorObj,
        'graphicsSettingsService',
        'detectOptimalSettings'
      );
      throw error;
    }
  }

  /**
   * Reset to default settings
   */
  async resetToDefaults(): Promise<GraphicsSettings> {
    try {
      await post(apiUrl('/api/graphics/reset'));
      const settings = await this.loadSettings();

      // Clear localStorage
      localStorage.removeItem(STORAGE_KEY);

      logger.info(
        'Graphics settings reset to defaults',
        'graphicsSettingsService',
        'resetToDefaults'
      );
      return settings;
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      logger.error(
        'Failed to reset graphics settings',
        errorObj,
        'graphicsSettingsService',
        'resetToDefaults'
      );
      throw error;
    }
  }

  /**
   * Subscribe to settings changes
   */
  subscribe(callback: (settings: GraphicsSettings) => void): () => void {
    this.listeners.add(callback);
    return () => {
      this.listeners.delete(callback);
    };
  }

  /**
   * Update internal cache
   */
  private updateCache(settings: GraphicsSettings): void {
    this.cache = {
      settings,
      timestamp: Date.now(),
    };
  }

  /**
   * Notify all listeners of settings change
   */
  private notifyListeners(settings: GraphicsSettings): void {
    this.listeners.forEach((callback) => {
      try {
        callback(settings);
      } catch (error: unknown) {
        const errorObj = error instanceof Error ? error : new Error(String(error));
        logger.error(
          'Error in settings change listener',
          errorObj,
          'graphicsSettingsService',
          'notifyListeners'
        );
      }
    });
  }

  /**
   * Clear cache (useful for testing)
   */
  clearCache(): void {
    this.cache = null;
  }
}

// Export singleton instance
export const graphicsSettingsService = new GraphicsSettingsService();
