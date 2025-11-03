import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createDefaultSettings } from '../types/settings';

describe('Advanced Mode Settings', () => {
  beforeEach(() => {
    // Clear any mocks before each test
    vi.clearAllMocks();
  });

  describe('Default Settings', () => {
    it('should have advancedModeEnabled set to false by default', () => {
      const settings = createDefaultSettings();
      expect(settings.general.advancedModeEnabled).toBe(false);
    });

    it('should include advancedModeEnabled property in general settings', () => {
      const settings = createDefaultSettings();
      expect(settings.general).toHaveProperty('advancedModeEnabled');
    });
  });

  describe('Settings Structure', () => {
    it('should have correct type for advancedModeEnabled', () => {
      const settings = createDefaultSettings();
      expect(typeof settings.general.advancedModeEnabled).toBe('boolean');
    });

    it('should allow setting advancedModeEnabled to true', () => {
      const settings = createDefaultSettings();
      settings.general.advancedModeEnabled = true;
      expect(settings.general.advancedModeEnabled).toBe(true);
    });

    it('should allow setting advancedModeEnabled to false', () => {
      const settings = createDefaultSettings();
      settings.general.advancedModeEnabled = true;
      settings.general.advancedModeEnabled = false;
      expect(settings.general.advancedModeEnabled).toBe(false);
    });
  });

  describe('Settings Persistence Format', () => {
    it('should serialize advancedModeEnabled correctly', () => {
      const settings = createDefaultSettings();
      settings.general.advancedModeEnabled = true;

      const serialized = JSON.stringify(settings);
      const parsed = JSON.parse(serialized);

      expect(parsed.general.advancedModeEnabled).toBe(true);
    });

    it('should maintain advancedModeEnabled value through serialization cycle', () => {
      const originalSettings = createDefaultSettings();
      originalSettings.general.advancedModeEnabled = true;

      const json = JSON.stringify(originalSettings);
      const restored = JSON.parse(json);

      expect(restored.general.advancedModeEnabled).toBe(
        originalSettings.general.advancedModeEnabled
      );
    });
  });

  describe('Settings Validation', () => {
    it('should not affect other general settings when changing advancedModeEnabled', () => {
      const settings = createDefaultSettings();
      const originalLanguage = settings.general.language;
      const originalTheme = settings.general.theme;

      settings.general.advancedModeEnabled = true;

      expect(settings.general.language).toBe(originalLanguage);
      expect(settings.general.theme).toBe(originalTheme);
    });

    it('should not affect other settings sections when changing advancedModeEnabled', () => {
      const settings = createDefaultSettings();
      const originalApiKeys = { ...settings.apiKeys };
      const originalVideoDefaults = { ...settings.videoDefaults };

      settings.general.advancedModeEnabled = true;

      expect(settings.apiKeys).toEqual(originalApiKeys);
      expect(settings.videoDefaults).toEqual(originalVideoDefaults);
    });
  });
});
