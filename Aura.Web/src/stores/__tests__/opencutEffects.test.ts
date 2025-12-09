/**
 * OpenCut Effects Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutEffectsStore, BUILTIN_EFFECTS } from '../opencutEffects';

describe('OpenCutEffectsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutEffectsStore.setState({
      definitions: BUILTIN_EFFECTS,
      applied: [],
      selectedEffectId: null,
    });
  });

  describe('Effect Definitions', () => {
    it('should have built-in effects', () => {
      const { definitions } = useOpenCutEffectsStore.getState();
      expect(definitions.length).toBeGreaterThan(0);
      expect(definitions.length).toBe(BUILTIN_EFFECTS.length);
    });

    it('should have exposure effect', () => {
      const { getEffectDefinition } = useOpenCutEffectsStore.getState();
      const exposure = getEffectDefinition('exposure');
      expect(exposure).toBeDefined();
      expect(exposure?.name).toBe('Exposure');
      expect(exposure?.category).toBe('color');
    });

    it('should get effects by category', () => {
      const { getDefinitionsByCategory } = useOpenCutEffectsStore.getState();
      const colorEffects = getDefinitionsByCategory('color');
      expect(colorEffects.length).toBeGreaterThan(0);
      expect(colorEffects.every((e) => e.category === 'color')).toBe(true);
    });

    it('should have blur category effects', () => {
      const { getDefinitionsByCategory } = useOpenCutEffectsStore.getState();
      const blurEffects = getDefinitionsByCategory('blur');
      expect(blurEffects.length).toBeGreaterThan(0);
    });

    it('should have stylize category effects', () => {
      const { getDefinitionsByCategory } = useOpenCutEffectsStore.getState();
      const stylizeEffects = getDefinitionsByCategory('stylize');
      expect(stylizeEffects.length).toBeGreaterThan(0);
    });
  });

  describe('Apply Effects', () => {
    it('should apply an effect to a clip', () => {
      const { applyEffect } = useOpenCutEffectsStore.getState();
      const effectId = applyEffect('exposure', 'clip-1');

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].id).toBe(effectId);
      expect(state.applied[0].clipId).toBe('clip-1');
      expect(state.applied[0].effectId).toBe('exposure');
      expect(state.applied[0].enabled).toBe(true);
    });

    it('should initialize parameters with default values', () => {
      const { applyEffect, getEffectDefinition } = useOpenCutEffectsStore.getState();
      applyEffect('exposure', 'clip-1');

      const state = useOpenCutEffectsStore.getState();
      const definition = getEffectDefinition('exposure');
      const exposureParam = definition?.parameters.find((p) => p.id === 'exposure');

      expect(state.applied[0].parameters.exposure).toBe(exposureParam?.defaultValue);
    });

    it('should assign correct order when applying multiple effects', () => {
      const { applyEffect } = useOpenCutEffectsStore.getState();

      applyEffect('exposure', 'clip-1');
      applyEffect('contrast', 'clip-1');
      applyEffect('vignette', 'clip-1');

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied[0].order).toBe(0);
      expect(state.applied[1].order).toBe(1);
      expect(state.applied[2].order).toBe(2);
    });

    it('should return empty string for non-existent effect', () => {
      const { applyEffect } = useOpenCutEffectsStore.getState();
      const id = applyEffect('non-existent', 'clip-1');
      expect(id).toBe('');
      expect(useOpenCutEffectsStore.getState().applied.length).toBe(0);
    });

    it('should allow same effect applied multiple times', () => {
      const { applyEffect } = useOpenCutEffectsStore.getState();

      applyEffect('exposure', 'clip-1');
      applyEffect('exposure', 'clip-1');

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied.length).toBe(2);
    });
  });

  describe('Remove Effects', () => {
    it('should remove an effect', () => {
      const { applyEffect, removeEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      expect(useOpenCutEffectsStore.getState().applied.length).toBe(1);

      useOpenCutEffectsStore.getState().removeEffect(effectId);
      expect(useOpenCutEffectsStore.getState().applied.length).toBe(0);
    });

    it('should clear selection when removing selected effect', () => {
      const { applyEffect, selectEffect, removeEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      useOpenCutEffectsStore.getState().selectEffect(effectId);
      expect(useOpenCutEffectsStore.getState().selectedEffectId).toBe(effectId);

      useOpenCutEffectsStore.getState().removeEffect(effectId);
      expect(useOpenCutEffectsStore.getState().selectedEffectId).toBeNull();
    });

    it('should remove all effects for a clip', () => {
      const { applyEffect, removeEffectsForClip } = useOpenCutEffectsStore.getState();

      applyEffect('exposure', 'clip-1');
      applyEffect('contrast', 'clip-1');
      applyEffect('exposure', 'clip-2');

      expect(useOpenCutEffectsStore.getState().applied.length).toBe(3);

      useOpenCutEffectsStore.getState().removeEffectsForClip('clip-1');

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].clipId).toBe('clip-2');
    });

    it('should clear all effects', () => {
      const { applyEffect, clearAllEffects } = useOpenCutEffectsStore.getState();

      applyEffect('exposure', 'clip-1');
      applyEffect('contrast', 'clip-2');

      useOpenCutEffectsStore.getState().clearAllEffects();

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied.length).toBe(0);
      expect(state.selectedEffectId).toBeNull();
    });
  });

  describe('Update Effects', () => {
    it('should update effect parameter', () => {
      const { applyEffect, updateEffectParameter } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');

      useOpenCutEffectsStore.getState().updateEffectParameter(effectId, 'exposure', 1.5);

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied[0].parameters.exposure).toBe(1.5);
    });

    it('should toggle effect enabled state', () => {
      const { applyEffect, toggleEffectEnabled } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      expect(useOpenCutEffectsStore.getState().applied[0].enabled).toBe(true);

      useOpenCutEffectsStore.getState().toggleEffectEnabled(effectId);
      expect(useOpenCutEffectsStore.getState().applied[0].enabled).toBe(false);

      useOpenCutEffectsStore.getState().toggleEffectEnabled(effectId);
      expect(useOpenCutEffectsStore.getState().applied[0].enabled).toBe(true);
    });

    it('should reset effect parameters to defaults', () => {
      const { applyEffect, updateEffectParameter, resetEffectParameters, getEffectDefinition } =
        useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');

      // Change some parameters
      useOpenCutEffectsStore.getState().updateEffectParameter(effectId, 'exposure', 2.5);
      useOpenCutEffectsStore.getState().updateEffectParameter(effectId, 'gamma', 2.0);

      // Reset
      useOpenCutEffectsStore.getState().resetEffectParameters(effectId);

      const state = useOpenCutEffectsStore.getState();
      const definition = getEffectDefinition('exposure');
      const exposureParam = definition?.parameters.find((p) => p.id === 'exposure');
      const gammaParam = definition?.parameters.find((p) => p.id === 'gamma');

      expect(state.applied[0].parameters.exposure).toBe(exposureParam?.defaultValue);
      expect(state.applied[0].parameters.gamma).toBe(gammaParam?.defaultValue);
    });
  });

  describe('Reorder Effects', () => {
    it('should reorder effects for a clip', () => {
      const { applyEffect, reorderEffects, getEffectsForClip } = useOpenCutEffectsStore.getState();

      const id1 = applyEffect('exposure', 'clip-1');
      const id2 = applyEffect('contrast', 'clip-1');
      const id3 = applyEffect('vignette', 'clip-1');

      // Reorder: vignette first, then exposure, then contrast
      useOpenCutEffectsStore.getState().reorderEffects('clip-1', [id3, id1, id2]);

      const effects = useOpenCutEffectsStore.getState().getEffectsForClip('clip-1');
      expect(effects[0].id).toBe(id3);
      expect(effects[1].id).toBe(id1);
      expect(effects[2].id).toBe(id2);
    });
  });

  describe('Duplicate Effects', () => {
    it('should duplicate an effect', () => {
      const { applyEffect, duplicateEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      useOpenCutEffectsStore.getState().updateEffectParameter(effectId, 'exposure', 1.5);

      const newId = useOpenCutEffectsStore.getState().duplicateEffect(effectId);

      const state = useOpenCutEffectsStore.getState();
      expect(state.applied.length).toBe(2);
      expect(newId).toBeDefined();
      expect(newId).not.toBe(effectId);

      const newEffect = state.applied.find((e) => e.id === newId);
      expect(newEffect?.effectId).toBe('exposure');
      expect(newEffect?.clipId).toBe('clip-1');
      expect(newEffect?.parameters.exposure).toBe(1.5);
    });

    it('should return null for non-existent effect', () => {
      const { duplicateEffect } = useOpenCutEffectsStore.getState();
      const result = duplicateEffect('non-existent');
      expect(result).toBeNull();
    });
  });

  describe('Selection', () => {
    it('should select an effect', () => {
      const { applyEffect, selectEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      useOpenCutEffectsStore.getState().selectEffect(effectId);

      expect(useOpenCutEffectsStore.getState().selectedEffectId).toBe(effectId);
    });

    it('should get selected effect', () => {
      const { applyEffect, selectEffect, getSelectedEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      useOpenCutEffectsStore.getState().selectEffect(effectId);

      const selected = useOpenCutEffectsStore.getState().getSelectedEffect();
      expect(selected).toBeDefined();
      expect(selected?.id).toBe(effectId);
    });

    it('should clear selection', () => {
      const { applyEffect, selectEffect } = useOpenCutEffectsStore.getState();

      const effectId = applyEffect('exposure', 'clip-1');
      useOpenCutEffectsStore.getState().selectEffect(effectId);
      useOpenCutEffectsStore.getState().selectEffect(null);

      expect(useOpenCutEffectsStore.getState().selectedEffectId).toBeNull();
    });
  });

  describe('Query Operations', () => {
    it('should get effects for a clip', () => {
      const { applyEffect, getEffectsForClip } = useOpenCutEffectsStore.getState();

      applyEffect('exposure', 'clip-1');
      applyEffect('contrast', 'clip-1');
      applyEffect('exposure', 'clip-2');

      const clipEffects = useOpenCutEffectsStore.getState().getEffectsForClip('clip-1');
      expect(clipEffects.length).toBe(2);
      expect(clipEffects.every((e) => e.clipId === 'clip-1')).toBe(true);
    });

    it('should return effects sorted by order', () => {
      const { applyEffect, reorderEffects, getEffectsForClip } = useOpenCutEffectsStore.getState();

      const id1 = applyEffect('exposure', 'clip-1');
      const id2 = applyEffect('contrast', 'clip-1');
      const id3 = applyEffect('vignette', 'clip-1');

      // Reorder
      useOpenCutEffectsStore.getState().reorderEffects('clip-1', [id3, id2, id1]);

      const effects = useOpenCutEffectsStore.getState().getEffectsForClip('clip-1');
      expect(effects[0].order).toBe(0);
      expect(effects[1].order).toBe(1);
      expect(effects[2].order).toBe(2);
    });

    it('should return empty array for clip with no effects', () => {
      const { getEffectsForClip } = useOpenCutEffectsStore.getState();
      const effects = getEffectsForClip('non-existent');
      expect(effects).toEqual([]);
    });
  });
});
