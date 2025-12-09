/**
 * OpenCut Text Animations Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useTextAnimationsStore, BUILTIN_TEXT_ANIMATION_PRESETS } from '../opencutTextAnimations';

describe('TextAnimationsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useTextAnimationsStore.setState({
      presets: BUILTIN_TEXT_ANIMATION_PRESETS,
      applied: [],
      selectedAnimationId: null,
    });
  });

  describe('Animation Presets', () => {
    it('should have built-in presets', () => {
      const { presets } = useTextAnimationsStore.getState();
      expect(presets.length).toBeGreaterThan(0);
      expect(presets.length).toBe(BUILTIN_TEXT_ANIMATION_PRESETS.length);
    });

    it('should have fade-in preset', () => {
      const { getPreset } = useTextAnimationsStore.getState();
      const fadeIn = getPreset('fade-in');
      expect(fadeIn).toBeDefined();
      expect(fadeIn?.name).toBe('Fade In');
      expect(fadeIn?.category).toBe('entrance');
    });

    it('should have typewriter preset', () => {
      const { getPreset } = useTextAnimationsStore.getState();
      const typewriter = getPreset('typewriter');
      expect(typewriter).toBeDefined();
      expect(typewriter?.name).toBe('Typewriter');
      expect(typewriter?.type).toBe('typewriter');
    });

    it('should get presets by category', () => {
      const { getPresetsByCategory } = useTextAnimationsStore.getState();
      const entrancePresets = getPresetsByCategory('entrance');
      expect(entrancePresets.length).toBeGreaterThan(0);
      expect(entrancePresets.every((p) => p.category === 'entrance')).toBe(true);
    });

    it('should have entrance category presets', () => {
      const { getPresetsByCategory } = useTextAnimationsStore.getState();
      const entrancePresets = getPresetsByCategory('entrance');
      expect(entrancePresets.length).toBeGreaterThan(5);
    });

    it('should have exit category presets', () => {
      const { getPresetsByCategory } = useTextAnimationsStore.getState();
      const exitPresets = getPresetsByCategory('exit');
      expect(exitPresets.length).toBeGreaterThan(0);
    });

    it('should have continuous category presets', () => {
      const { getPresetsByCategory } = useTextAnimationsStore.getState();
      const continuousPresets = getPresetsByCategory('continuous');
      expect(continuousPresets.length).toBeGreaterThan(0);
    });

    it('should have emphasis category presets', () => {
      const { getPresetsByCategory } = useTextAnimationsStore.getState();
      const emphasisPresets = getPresetsByCategory('emphasis');
      expect(emphasisPresets.length).toBeGreaterThan(0);
    });
  });

  describe('Apply Animations', () => {
    it('should apply an animation to a clip', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();
      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');

      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].id).toBe(animationId);
      expect(state.applied[0].targetId).toBe('clip-1');
      expect(state.applied[0].targetType).toBe('clip');
      expect(state.applied[0].presetId).toBe('fade-in');
      expect(state.applied[0].position).toBe('in');
    });

    it('should apply an animation to a caption', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();
      const animationId = applyAnimation('caption-1', 'caption', 'typewriter', 'in');

      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].targetType).toBe('caption');
    });

    it('should use preset duration when applying animation', () => {
      const { applyAnimation, getPreset } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'typewriter', 'in');

      const state = useTextAnimationsStore.getState();
      const preset = getPreset('typewriter');
      expect(state.applied[0].duration).toBe(preset?.duration);
    });

    it('should initialize parameters with preset values', () => {
      const { applyAnimation, getPreset } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'typewriter-cursor', 'in');

      const state = useTextAnimationsStore.getState();
      const preset = getPreset('typewriter-cursor');

      expect(state.applied[0].parameters.cursor).toBe(preset?.parameters.cursor);
      expect(state.applied[0].parameters.cursorChar).toBe(preset?.parameters.cursorChar);
    });

    it('should replace existing animation at same position', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      expect(useTextAnimationsStore.getState().applied.length).toBe(1);

      applyAnimation('clip-1', 'clip', 'slide-up', 'in');
      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].presetId).toBe('slide-up');
    });

    it('should allow different animations at different positions', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      applyAnimation('clip-1', 'clip', 'fade-out', 'out');
      applyAnimation('clip-1', 'clip', 'wave', 'continuous');

      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(3);
    });

    it('should return empty string for non-existent preset', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();
      const id = applyAnimation('clip-1', 'clip', 'non-existent', 'in');
      expect(id).toBe('');
      expect(useTextAnimationsStore.getState().applied.length).toBe(0);
    });

    it('should set delay to 0 by default', () => {
      const { applyAnimation } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');

      const state = useTextAnimationsStore.getState();
      expect(state.applied[0].delay).toBe(0);
    });
  });

  describe('Remove Animations', () => {
    it('should remove an animation', () => {
      const { applyAnimation, removeAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      expect(useTextAnimationsStore.getState().applied.length).toBe(1);

      useTextAnimationsStore.getState().removeAnimation(animationId);
      expect(useTextAnimationsStore.getState().applied.length).toBe(0);
    });

    it('should clear selection when removing selected animation', () => {
      const { applyAnimation, selectAnimation, removeAnimation } =
        useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      useTextAnimationsStore.getState().selectAnimation(animationId);
      expect(useTextAnimationsStore.getState().selectedAnimationId).toBe(animationId);

      useTextAnimationsStore.getState().removeAnimation(animationId);
      expect(useTextAnimationsStore.getState().selectedAnimationId).toBeNull();
    });

    it('should remove all animations for a target', () => {
      const { applyAnimation, removeAnimationsForTarget } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      applyAnimation('clip-1', 'clip', 'fade-out', 'out');
      applyAnimation('clip-2', 'clip', 'fade-in', 'in');

      expect(useTextAnimationsStore.getState().applied.length).toBe(3);

      useTextAnimationsStore.getState().removeAnimationsForTarget('clip-1');

      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].targetId).toBe('clip-2');
    });

    it('should clear all animations', () => {
      const { applyAnimation, clearAllAnimations } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      applyAnimation('clip-2', 'clip', 'fade-out', 'out');

      useTextAnimationsStore.getState().clearAllAnimations();

      const state = useTextAnimationsStore.getState();
      expect(state.applied.length).toBe(0);
      expect(state.selectedAnimationId).toBeNull();
    });
  });

  describe('Update Animations', () => {
    it('should update animation duration', () => {
      const { applyAnimation, updateAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');

      useTextAnimationsStore.getState().updateAnimation(animationId, { duration: 1.5 });

      const state = useTextAnimationsStore.getState();
      expect(state.applied[0].duration).toBe(1.5);
    });

    it('should update animation delay', () => {
      const { applyAnimation, updateAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');

      useTextAnimationsStore.getState().updateAnimation(animationId, { delay: 0.5 });

      const state = useTextAnimationsStore.getState();
      expect(state.applied[0].delay).toBe(0.5);
    });

    it('should update animation parameters', () => {
      const { applyAnimation, updateAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'word-highlight', 'continuous');

      useTextAnimationsStore.getState().updateAnimation(animationId, {
        parameters: { highlightColor: '#FF0000' },
      });

      const state = useTextAnimationsStore.getState();
      expect(state.applied[0].parameters.highlightColor).toBe('#FF0000');
    });
  });

  describe('Selection', () => {
    it('should select an animation', () => {
      const { applyAnimation, selectAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      useTextAnimationsStore.getState().selectAnimation(animationId);

      expect(useTextAnimationsStore.getState().selectedAnimationId).toBe(animationId);
    });

    it('should get selected animation', () => {
      const { applyAnimation, selectAnimation, getSelectedAnimation } =
        useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      useTextAnimationsStore.getState().selectAnimation(animationId);

      const selected = useTextAnimationsStore.getState().getSelectedAnimation();
      expect(selected).toBeDefined();
      expect(selected?.id).toBe(animationId);
    });

    it('should clear selection', () => {
      const { applyAnimation, selectAnimation } = useTextAnimationsStore.getState();

      const animationId = applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      useTextAnimationsStore.getState().selectAnimation(animationId);
      useTextAnimationsStore.getState().selectAnimation(null);

      expect(useTextAnimationsStore.getState().selectedAnimationId).toBeNull();
    });
  });

  describe('Query Operations', () => {
    it('should get animations for a target', () => {
      const { applyAnimation, getAnimationsForTarget } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      applyAnimation('clip-1', 'clip', 'fade-out', 'out');
      applyAnimation('clip-2', 'clip', 'fade-in', 'in');

      const clipAnimations = useTextAnimationsStore.getState().getAnimationsForTarget('clip-1');
      expect(clipAnimations.length).toBe(2);
      expect(clipAnimations.every((a) => a.targetId === 'clip-1')).toBe(true);
    });

    it('should get animation at specific position', () => {
      const { applyAnimation, getAnimationAtPosition } = useTextAnimationsStore.getState();

      applyAnimation('clip-1', 'clip', 'fade-in', 'in');
      applyAnimation('clip-1', 'clip', 'fade-out', 'out');

      const inAnimation = useTextAnimationsStore.getState().getAnimationAtPosition('clip-1', 'in');
      const outAnimation = useTextAnimationsStore
        .getState()
        .getAnimationAtPosition('clip-1', 'out');

      expect(inAnimation?.presetId).toBe('fade-in');
      expect(outAnimation?.presetId).toBe('fade-out');
    });

    it('should return undefined for non-existent position', () => {
      const { getAnimationAtPosition } = useTextAnimationsStore.getState();

      const animation = getAnimationAtPosition('clip-1', 'in');
      expect(animation).toBeUndefined();
    });

    it('should return empty array for target with no animations', () => {
      const { getAnimationsForTarget } = useTextAnimationsStore.getState();
      const animations = getAnimationsForTarget('non-existent');
      expect(animations).toEqual([]);
    });
  });
});
