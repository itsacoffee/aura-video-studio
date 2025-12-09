/**
 * OpenCut Export Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useExportStore, BUILTIN_PRESETS } from '../opencutExport';

describe('OpenCutExportStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useExportStore.setState({
      builtinPresets: BUILTIN_PRESETS,
      customPresets: [],
      selectedPresetId: 'youtube-1080p',
      currentSettings: BUILTIN_PRESETS.find((p) => p.id === 'youtube-1080p')?.settings ?? null,
      exportProgress: 0,
      isExporting: false,
      exportError: null,
    });
  });

  describe('Built-in Presets', () => {
    it('should have built-in presets', () => {
      const { builtinPresets } = useExportStore.getState();
      expect(builtinPresets.length).toBeGreaterThan(0);
      expect(builtinPresets.length).toBe(BUILTIN_PRESETS.length);
    });

    it('should have youtube-1080p preset', () => {
      const { getPreset } = useExportStore.getState();
      const preset = getPreset('youtube-1080p');
      expect(preset).toBeDefined();
      expect(preset?.name).toBe('1080p Full HD');
      expect(preset?.platform).toBe('Video Platform');
    });

    it('should have correct settings for youtube-4k preset', () => {
      const { getPreset } = useExportStore.getState();
      const preset = getPreset('youtube-4k');
      expect(preset).toBeDefined();
      expect(preset?.settings.resolution.width).toBe(3840);
      expect(preset?.settings.resolution.height).toBe(2160);
      expect(preset?.settings.frameRate).toBe(60);
    });

    it('should get presets by platform', () => {
      const { getPresetsByPlatform } = useExportStore.getState();
      const videoPresets = getPresetsByPlatform('Video Platform');
      expect(videoPresets.length).toBeGreaterThan(0);
      expect(videoPresets.every((p) => p.platform === 'Video Platform')).toBe(true);
    });

    it('should get all unique platforms', () => {
      const { getAllPlatforms } = useExportStore.getState();
      const platforms = getAllPlatforms();
      expect(platforms.length).toBeGreaterThan(0);
      expect(platforms).toContain('Video Platform');
      expect(platforms).toContain('Mobile Video');
      expect(platforms).toContain('Web');
    });
  });

  describe('Preset Selection', () => {
    it('should select a preset', () => {
      const { selectPreset } = useExportStore.getState();
      selectPreset('youtube-4k');

      const state = useExportStore.getState();
      expect(state.selectedPresetId).toBe('youtube-4k');
      expect(state.currentSettings?.resolution.width).toBe(3840);
    });

    it('should update current settings when selecting preset', () => {
      const { selectPreset } = useExportStore.getState();
      selectPreset('vertical-1080');

      const state = useExportStore.getState();
      expect(state.currentSettings?.resolution.width).toBe(1080);
      expect(state.currentSettings?.resolution.height).toBe(1920);
    });

    it('should not change state for non-existent preset', () => {
      const { selectPreset, selectedPresetId: initialId } = useExportStore.getState();
      selectPreset('non-existent');

      const state = useExportStore.getState();
      expect(state.selectedPresetId).toBe(initialId);
    });
  });

  describe('Custom Presets', () => {
    it('should create a custom preset', () => {
      const { createCustomPreset } = useExportStore.getState();
      const settings = BUILTIN_PRESETS[0].settings;

      const id = createCustomPreset('My Custom Preset', 'Custom', settings);

      expect(id).toContain('custom-');
      const preset = useExportStore.getState().getPreset(id);
      expect(preset).toBeDefined();
      expect(preset?.name).toBe('My Custom Preset');
      expect(preset?.platform).toBe('Custom');
    });

    it('should update a custom preset', () => {
      const { createCustomPreset } = useExportStore.getState();
      const settings = BUILTIN_PRESETS[0].settings;

      const id = createCustomPreset('Original Name', 'Custom', settings);
      useExportStore.getState().updateCustomPreset(id, { name: 'Updated Name' });

      const preset = useExportStore.getState().getPreset(id);
      expect(preset?.name).toBe('Updated Name');
    });

    it('should delete a custom preset', () => {
      const { createCustomPreset } = useExportStore.getState();
      const settings = BUILTIN_PRESETS[0].settings;

      const id = createCustomPreset('To Be Deleted', 'Custom', settings);
      expect(useExportStore.getState().getPreset(id)).toBeDefined();

      useExportStore.getState().deleteCustomPreset(id);
      expect(useExportStore.getState().getPreset(id)).toBeUndefined();
    });

    it('should reset selected preset when deleting selected custom preset', () => {
      const { createCustomPreset } = useExportStore.getState();
      const settings = BUILTIN_PRESETS[0].settings;

      const id = createCustomPreset('Selected Custom', 'Custom', settings);
      useExportStore.getState().selectPreset(id);
      expect(useExportStore.getState().selectedPresetId).toBe(id);

      useExportStore.getState().deleteCustomPreset(id);
      expect(useExportStore.getState().selectedPresetId).toBe('youtube-1080p');
    });
  });

  describe('Settings Updates', () => {
    it('should update current settings', () => {
      const { setCurrentSettings } = useExportStore.getState();
      const newSettings = {
        ...BUILTIN_PRESETS[0].settings,
        videoBitrate: 25000,
      };

      setCurrentSettings(newSettings);

      const state = useExportStore.getState();
      expect(state.currentSettings?.videoBitrate).toBe(25000);
    });

    it('should update a single setting', () => {
      const { updateCurrentSetting } = useExportStore.getState();

      updateCurrentSetting('frameRate', 60);

      const state = useExportStore.getState();
      expect(state.currentSettings?.frameRate).toBe(60);
    });

    it('should update resolution', () => {
      const { updateCurrentSetting } = useExportStore.getState();

      updateCurrentSetting('resolution', { width: 2560, height: 1440 });

      const state = useExportStore.getState();
      expect(state.currentSettings?.resolution.width).toBe(2560);
      expect(state.currentSettings?.resolution.height).toBe(1440);
    });
  });

  describe('File Size Estimation', () => {
    it('should estimate file size based on bitrate and duration', () => {
      const { selectPreset } = useExportStore.getState();
      selectPreset('youtube-1080p');

      // 12000 kbps video + 256 kbps audio = 12256 kbps
      // (12256 * 60) / 8 / 1024 = ~89.8 MB for 60 seconds
      const size = useExportStore.getState().estimateFileSize(60);
      expect(size).toBeGreaterThan(0);
      expect(size).toBeLessThan(150); // Reasonable range for 60 seconds
    });

    it('should return 0 when no settings', () => {
      useExportStore.setState({ currentSettings: null });
      const { estimateFileSize } = useExportStore.getState();

      const size = estimateFileSize(60);
      expect(size).toBe(0);
    });

    it('should scale with duration', () => {
      const { estimateFileSize } = useExportStore.getState();

      const size30s = estimateFileSize(30);
      const size60s = estimateFileSize(60);

      expect(size60s).toBeCloseTo(size30s * 2, 1);
    });
  });

  describe('Export Process', () => {
    it('should start export and update progress', async () => {
      const { startExport } = useExportStore.getState();

      const exportPromise = startExport();

      // Check initial state
      expect(useExportStore.getState().isExporting).toBe(true);

      await exportPromise;

      const state = useExportStore.getState();
      expect(state.isExporting).toBe(false);
      expect(state.exportProgress).toBe(100);
    });

    it('should cancel export', async () => {
      const { startExport } = useExportStore.getState();

      startExport();
      expect(useExportStore.getState().isExporting).toBe(true);

      useExportStore.getState().cancelExport();

      const state = useExportStore.getState();
      expect(state.isExporting).toBe(false);
      expect(state.exportProgress).toBe(0);
    });

    it('should clear export error', () => {
      useExportStore.setState({ exportError: 'Some error' });
      expect(useExportStore.getState().exportError).toBe('Some error');

      useExportStore.getState().clearExportError();

      expect(useExportStore.getState().exportError).toBeNull();
    });
  });

  describe('Preset Queries', () => {
    it('should return undefined for non-existent preset', () => {
      const { getPreset } = useExportStore.getState();
      const preset = getPreset('non-existent');
      expect(preset).toBeUndefined();
    });

    it('should return empty array for non-existent platform', () => {
      const { getPresetsByPlatform } = useExportStore.getState();
      const presets = getPresetsByPlatform('Non-Existent Platform');
      expect(presets).toEqual([]);
    });

    it('should include custom presets in platform query', () => {
      const { createCustomPreset } = useExportStore.getState();
      const settings = BUILTIN_PRESETS[0].settings;

      createCustomPreset('Custom Web Preset', 'Web', settings);

      const webPresets = useExportStore.getState().getPresetsByPlatform('Web');
      expect(webPresets.some((p) => p.name === 'Custom Web Preset')).toBe(true);
    });
  });
});
