/**
 * OpenCut Transitions Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutTransitionsStore, BUILTIN_TRANSITIONS } from '../opencutTransitions';

describe('OpenCutTransitionsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutTransitionsStore.setState({
      definitions: BUILTIN_TRANSITIONS,
      applied: [],
      selectedTransitionId: null,
      defaultDuration: 0.5,
    });
  });

  describe('Transition Definitions', () => {
    it('should have built-in transitions', () => {
      const { definitions } = useOpenCutTransitionsStore.getState();
      expect(definitions.length).toBeGreaterThan(0);
      expect(definitions.length).toBe(BUILTIN_TRANSITIONS.length);
    });

    it('should have cross-dissolve transition', () => {
      const { getTransitionDefinition } = useOpenCutTransitionsStore.getState();
      const crossDissolve = getTransitionDefinition('cross-dissolve');
      expect(crossDissolve).toBeDefined();
      expect(crossDissolve?.name).toBe('Cross Dissolve');
      expect(crossDissolve?.category).toBe('dissolve');
    });

    it('should get transitions by category', () => {
      const { getDefinitionsByCategory } = useOpenCutTransitionsStore.getState();
      const wipeTransitions = getDefinitionsByCategory('wipe');
      expect(wipeTransitions.length).toBeGreaterThan(0);
      expect(wipeTransitions.every((t) => t.category === 'wipe')).toBe(true);
    });

    it('should have blur category transitions', () => {
      const { getDefinitionsByCategory } = useOpenCutTransitionsStore.getState();
      const blurTransitions = getDefinitionsByCategory('blur');
      expect(blurTransitions.length).toBeGreaterThan(0);
    });
  });

  describe('Apply Transitions', () => {
    it('should apply a transition to a clip', () => {
      const { applyTransition, applied } = useOpenCutTransitionsStore.getState();
      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].id).toBe(transitionId);
      expect(state.applied[0].clipId).toBe('clip-1');
      expect(state.applied[0].position).toBe('start');
      expect(state.applied[0].transitionId).toBe('cross-dissolve');
    });

    it('should use default duration when applying transition', () => {
      const { applyTransition, setDefaultDuration } = useOpenCutTransitionsStore.getState();

      useOpenCutTransitionsStore.getState().setDefaultDuration(1.0);
      useOpenCutTransitionsStore.getState().applyTransition('cross-dissolve', 'clip-1', 'end');

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied[0].duration).toBe(1.0);
    });

    it('should replace existing transition at same position', () => {
      const { applyTransition } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      expect(useOpenCutTransitionsStore.getState().applied.length).toBe(1);

      applyTransition('fade-to-black', 'clip-1', 'start');
      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].transitionId).toBe('fade-to-black');
    });

    it('should allow different transitions at start and end', () => {
      const { applyTransition } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      applyTransition('fade-to-black', 'clip-1', 'end');

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied.length).toBe(2);
    });

    it('should return empty string for non-existent transition', () => {
      const { applyTransition } = useOpenCutTransitionsStore.getState();
      const id = applyTransition('non-existent', 'clip-1', 'start');
      expect(id).toBe('');
      expect(useOpenCutTransitionsStore.getState().applied.length).toBe(0);
    });

    it('should initialize parameters with default values', () => {
      const { applyTransition, getTransitionDefinition } = useOpenCutTransitionsStore.getState();

      applyTransition('fade-to-black', 'clip-1', 'start');

      const state = useOpenCutTransitionsStore.getState();
      const definition = getTransitionDefinition('fade-to-black');
      const colorParam = definition?.parameters.find((p) => p.id === 'color');

      expect(state.applied[0].parameters.color).toBe(colorParam?.defaultValue);
    });
  });

  describe('Remove Transitions', () => {
    it('should remove a transition', () => {
      const { applyTransition, removeTransition } = useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');
      expect(useOpenCutTransitionsStore.getState().applied.length).toBe(1);

      useOpenCutTransitionsStore.getState().removeTransition(transitionId);
      expect(useOpenCutTransitionsStore.getState().applied.length).toBe(0);
    });

    it('should clear selection when removing selected transition', () => {
      const { applyTransition, selectTransition, removeTransition } =
        useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');
      useOpenCutTransitionsStore.getState().selectTransition(transitionId);
      expect(useOpenCutTransitionsStore.getState().selectedTransitionId).toBe(transitionId);

      useOpenCutTransitionsStore.getState().removeTransition(transitionId);
      expect(useOpenCutTransitionsStore.getState().selectedTransitionId).toBeNull();
    });

    it('should remove all transitions for a clip', () => {
      const { applyTransition, removeTransitionsForClip } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      applyTransition('fade-to-black', 'clip-1', 'end');
      applyTransition('cross-dissolve', 'clip-2', 'start');

      expect(useOpenCutTransitionsStore.getState().applied.length).toBe(3);

      useOpenCutTransitionsStore.getState().removeTransitionsForClip('clip-1');

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied.length).toBe(1);
      expect(state.applied[0].clipId).toBe('clip-2');
    });

    it('should clear all transitions', () => {
      const { applyTransition, clearAllTransitions } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      applyTransition('fade-to-black', 'clip-2', 'end');

      useOpenCutTransitionsStore.getState().clearAllTransitions();

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied.length).toBe(0);
      expect(state.selectedTransitionId).toBeNull();
    });
  });

  describe('Update Transitions', () => {
    it('should update transition duration', () => {
      const { applyTransition, updateTransition } = useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');

      useOpenCutTransitionsStore.getState().updateTransition(transitionId, { duration: 1.5 });

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied[0].duration).toBe(1.5);
    });

    it('should update transition parameters', () => {
      const { applyTransition, updateTransition } = useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('fade-to-black', 'clip-1', 'start');

      useOpenCutTransitionsStore.getState().updateTransition(transitionId, {
        parameters: { color: '#FF0000' },
      });

      const state = useOpenCutTransitionsStore.getState();
      expect(state.applied[0].parameters.color).toBe('#FF0000');
    });
  });

  describe('Selection', () => {
    it('should select a transition', () => {
      const { applyTransition, selectTransition } = useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');
      useOpenCutTransitionsStore.getState().selectTransition(transitionId);

      expect(useOpenCutTransitionsStore.getState().selectedTransitionId).toBe(transitionId);
    });

    it('should get selected transition', () => {
      const { applyTransition, selectTransition, getSelectedTransition } =
        useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');
      useOpenCutTransitionsStore.getState().selectTransition(transitionId);

      const selected = useOpenCutTransitionsStore.getState().getSelectedTransition();
      expect(selected).toBeDefined();
      expect(selected?.id).toBe(transitionId);
    });

    it('should clear selection', () => {
      const { applyTransition, selectTransition } = useOpenCutTransitionsStore.getState();

      const transitionId = applyTransition('cross-dissolve', 'clip-1', 'start');
      useOpenCutTransitionsStore.getState().selectTransition(transitionId);
      useOpenCutTransitionsStore.getState().selectTransition(null);

      expect(useOpenCutTransitionsStore.getState().selectedTransitionId).toBeNull();
    });
  });

  describe('Query Operations', () => {
    it('should get transitions for a clip', () => {
      const { applyTransition, getTransitionsForClip } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      applyTransition('fade-to-black', 'clip-1', 'end');
      applyTransition('cross-dissolve', 'clip-2', 'start');

      const clipTransitions = useOpenCutTransitionsStore.getState().getTransitionsForClip('clip-1');
      expect(clipTransitions.length).toBe(2);
      expect(clipTransitions.every((t) => t.clipId === 'clip-1')).toBe(true);
    });

    it('should get transition at specific position', () => {
      const { applyTransition, getTransitionAtPosition } = useOpenCutTransitionsStore.getState();

      applyTransition('cross-dissolve', 'clip-1', 'start');
      applyTransition('fade-to-black', 'clip-1', 'end');

      const startTransition = useOpenCutTransitionsStore
        .getState()
        .getTransitionAtPosition('clip-1', 'start');
      const endTransition = useOpenCutTransitionsStore
        .getState()
        .getTransitionAtPosition('clip-1', 'end');

      expect(startTransition?.transitionId).toBe('cross-dissolve');
      expect(endTransition?.transitionId).toBe('fade-to-black');
    });

    it('should return undefined for non-existent position', () => {
      const { getTransitionAtPosition } = useOpenCutTransitionsStore.getState();

      const transition = getTransitionAtPosition('clip-1', 'start');
      expect(transition).toBeUndefined();
    });
  });

  describe('Settings', () => {
    it('should set default duration', () => {
      const { setDefaultDuration } = useOpenCutTransitionsStore.getState();

      useOpenCutTransitionsStore.getState().setDefaultDuration(2.0);
      expect(useOpenCutTransitionsStore.getState().defaultDuration).toBe(2.0);
    });

    it('should clamp default duration to valid range', () => {
      const { setDefaultDuration } = useOpenCutTransitionsStore.getState();

      useOpenCutTransitionsStore.getState().setDefaultDuration(0.01);
      expect(useOpenCutTransitionsStore.getState().defaultDuration).toBe(0.1);

      useOpenCutTransitionsStore.getState().setDefaultDuration(10);
      expect(useOpenCutTransitionsStore.getState().defaultDuration).toBe(5);
    });
  });
});
