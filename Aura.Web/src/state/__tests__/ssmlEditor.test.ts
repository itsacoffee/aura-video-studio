import { describe, it, expect, beforeEach } from 'vitest';
import { useSSMLEditorStore } from '../ssmlEditor';
import type { SSMLSegmentResult } from '@/services/ssmlService';

describe('SSML Editor Store', () => {
  beforeEach(() => {
    useSSMLEditorStore.getState().reset();
  });

  describe('Initial State', () => {
    it('should have empty scenes map', () => {
      const { scenes } = useSSMLEditorStore.getState();
      expect(scenes.size).toBe(0);
    });

    it('should have no selected scene', () => {
      const { selectedSceneIndex } = useSSMLEditorStore.getState();
      expect(selectedSceneIndex).toBeNull();
    });

    it('should have default UI state', () => {
      const { showWaveform, showTimingMarkers, autoFitEnabled } = useSSMLEditorStore.getState();
      expect(showWaveform).toBe(true);
      expect(showTimingMarkers).toBe(false);
      expect(autoFitEnabled).toBe(true);
    });
  });

  describe('setScenes', () => {
    it('should populate scenes from segments', () => {
      const segments: SSMLSegmentResult[] = [
        {
          sceneIndex: 0,
          originalText: 'Hello world',
          ssmlMarkup: '<speak>Hello world</speak>',
          estimatedDurationMs: 1000,
          targetDurationMs: 1000,
          deviationPercent: 0,
          adjustments: {
            rate: 1.0,
            pitch: 0.0,
            volume: 1.0,
            pauses: {},
            emphasis: [],
            iterations: 0,
          },
          timingMarkers: [],
        },
        {
          sceneIndex: 1,
          originalText: 'Second scene',
          ssmlMarkup: '<speak>Second scene</speak>',
          estimatedDurationMs: 1500,
          targetDurationMs: 1500,
          deviationPercent: 0,
          adjustments: {
            rate: 1.0,
            pitch: 0.0,
            volume: 1.0,
            pauses: {},
            emphasis: [],
            iterations: 0,
          },
          timingMarkers: [],
        },
      ];

      useSSMLEditorStore.getState().setScenes(segments);
      const { scenes } = useSSMLEditorStore.getState();

      expect(scenes.size).toBe(2);
      expect(scenes.get(0)?.originalText).toBe('Hello world');
      expect(scenes.get(1)?.originalText).toBe('Second scene');
    });

    it('should set userModified to false initially', () => {
      const segments: SSMLSegmentResult[] = [
        {
          sceneIndex: 0,
          originalText: 'Test',
          ssmlMarkup: '<speak>Test</speak>',
          estimatedDurationMs: 1000,
          targetDurationMs: 1000,
          deviationPercent: 0,
          adjustments: {
            rate: 1.0,
            pitch: 0.0,
            volume: 1.0,
            pauses: {},
            emphasis: [],
            iterations: 0,
          },
          timingMarkers: [],
        },
      ];

      useSSMLEditorStore.getState().setScenes(segments);
      const scene = useSSMLEditorStore.getState().scenes.get(0);

      expect(scene?.userModified).toBe(false);
    });
  });

  describe('updateScene', () => {
    beforeEach(() => {
      const segments: SSMLSegmentResult[] = [
        {
          sceneIndex: 0,
          originalText: 'Test',
          ssmlMarkup: '<speak>Test</speak>',
          estimatedDurationMs: 1000,
          targetDurationMs: 1000,
          deviationPercent: 0,
          adjustments: {
            rate: 1.0,
            pitch: 0.0,
            volume: 1.0,
            pauses: {},
            emphasis: [],
            iterations: 0,
          },
          timingMarkers: [],
        },
      ];
      useSSMLEditorStore.getState().setScenes(segments);
    });

    it('should update scene properties', () => {
      useSSMLEditorStore.getState().updateScene(0, {
        ssmlMarkup: '<speak>Updated</speak>',
      });

      const scene = useSSMLEditorStore.getState().scenes.get(0);
      expect(scene?.ssmlMarkup).toBe('<speak>Updated</speak>');
    });

    it('should mark scene as userModified', () => {
      useSSMLEditorStore.getState().updateScene(0, {
        ssmlMarkup: '<speak>Updated</speak>',
      });

      const scene = useSSMLEditorStore.getState().scenes.get(0);
      expect(scene?.userModified).toBe(true);
    });

    it('should preserve other properties', () => {
      useSSMLEditorStore.getState().updateScene(0, {
        ssmlMarkup: '<speak>Updated</speak>',
      });

      const scene = useSSMLEditorStore.getState().scenes.get(0);
      expect(scene?.originalText).toBe('Test');
      expect(scene?.estimatedDurationMs).toBe(1000);
    });
  });

  describe('selectScene', () => {
    it('should set selected scene index', () => {
      useSSMLEditorStore.getState().selectScene(5);
      expect(useSSMLEditorStore.getState().selectedSceneIndex).toBe(5);
    });

    it('should allow null selection', () => {
      useSSMLEditorStore.getState().selectScene(3);
      useSSMLEditorStore.getState().selectScene(null);
      expect(useSSMLEditorStore.getState().selectedSceneIndex).toBeNull();
    });
  });

  describe('Provider Management', () => {
    it('should set provider', () => {
      useSSMLEditorStore.getState().setProvider('ElevenLabs');
      expect(useSSMLEditorStore.getState().selectedProvider).toBe('ElevenLabs');
    });

    it('should set provider constraints', () => {
      const constraints = {
        supportedTags: ['speak', 'break'],
        supportedProsodyAttributes: ['rate', 'pitch'],
        minRate: 0.5,
        maxRate: 2.0,
        minPitch: -12,
        maxPitch: 12,
        minVolume: 0,
        maxVolume: 2,
        maxPauseDurationMs: 10000,
        supportsTimingMarkers: true,
      };

      useSSMLEditorStore.getState().setProviderConstraints(constraints);
      expect(useSSMLEditorStore.getState().providerConstraints).toEqual(constraints);
    });
  });

  describe('Validation Management', () => {
    it('should set validation errors', () => {
      useSSMLEditorStore.getState().setValidationErrors(0, ['Error 1', 'Error 2']);
      const errors = useSSMLEditorStore.getState().validationErrors.get(0);
      expect(errors).toEqual(['Error 1', 'Error 2']);
    });

    it('should set validation warnings', () => {
      useSSMLEditorStore.getState().setValidationWarnings(0, ['Warning 1', 'Warning 2']);
      const warnings = useSSMLEditorStore.getState().validationWarnings.get(0);
      expect(warnings).toEqual(['Warning 1', 'Warning 2']);
    });

    it('should clear validation for scene', () => {
      useSSMLEditorStore.getState().setValidationErrors(0, ['Error 1', 'Error 2']);
      useSSMLEditorStore.getState().setValidationWarnings(0, ['Warning 1', 'Warning 2']);

      useSSMLEditorStore.getState().clearValidation(0);

      expect(useSSMLEditorStore.getState().validationErrors.get(0)).toBeUndefined();
      expect(useSSMLEditorStore.getState().validationWarnings.get(0)).toBeUndefined();
    });
  });

  describe('UI State Management', () => {
    it('should toggle waveform visibility', () => {
      const initialState = useSSMLEditorStore.getState().showWaveform;
      useSSMLEditorStore.getState().toggleWaveform();
      expect(useSSMLEditorStore.getState().showWaveform).toBe(!initialState);
      useSSMLEditorStore.getState().toggleWaveform();
      expect(useSSMLEditorStore.getState().showWaveform).toBe(initialState);
    });

    it('should toggle timing markers visibility', () => {
      const initialState = useSSMLEditorStore.getState().showTimingMarkers;
      useSSMLEditorStore.getState().toggleTimingMarkers();
      expect(useSSMLEditorStore.getState().showTimingMarkers).toBe(!initialState);
      useSSMLEditorStore.getState().toggleTimingMarkers();
      expect(useSSMLEditorStore.getState().showTimingMarkers).toBe(initialState);
    });

    it('should set auto-fit enabled', () => {
      useSSMLEditorStore.getState().setAutoFitEnabled(false);
      expect(useSSMLEditorStore.getState().autoFitEnabled).toBe(false);
      useSSMLEditorStore.getState().setAutoFitEnabled(true);
      expect(useSSMLEditorStore.getState().autoFitEnabled).toBe(true);
    });
  });

  describe('reset', () => {
    it('should reset all state to defaults', () => {
      const segments: SSMLSegmentResult[] = [
        {
          sceneIndex: 0,
          originalText: 'Test',
          ssmlMarkup: '<speak>Test</speak>',
          estimatedDurationMs: 1000,
          targetDurationMs: 1000,
          deviationPercent: 0,
          adjustments: {
            rate: 1.0,
            pitch: 0.0,
            volume: 1.0,
            pauses: {},
            emphasis: [],
            iterations: 0,
          },
          timingMarkers: [],
        },
      ];

      useSSMLEditorStore.getState().setScenes(segments);
      useSSMLEditorStore.getState().selectScene(0);
      useSSMLEditorStore.getState().setProvider('ElevenLabs');
      useSSMLEditorStore.getState().setAutoFitEnabled(false);

      useSSMLEditorStore.getState().reset();

      const state = useSSMLEditorStore.getState();
      expect(state.scenes.size).toBe(0);
      expect(state.selectedSceneIndex).toBeNull();
      expect(state.selectedProvider).toBeNull();
      expect(state.autoFitEnabled).toBe(true);
    });
  });
});
