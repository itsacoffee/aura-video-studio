/**
 * OpenCut Keybindings Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useOpenCutKeybindingsStore, DEFAULT_KEYBINDINGS } from '../opencutKeybindings';

describe('OpenCutKeybindingsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useOpenCutKeybindingsStore.setState({
      keybindings: [...DEFAULT_KEYBINDINGS],
      jklSpeed: 1,
      jklDirection: 0,
    });
  });

  describe('Keybinding Definitions', () => {
    it('should have default keybindings', () => {
      const { keybindings } = useOpenCutKeybindingsStore.getState();
      expect(keybindings.length).toBeGreaterThan(0);
      expect(keybindings.length).toBe(DEFAULT_KEYBINDINGS.length);
    });

    it('should have JKL playback controls', () => {
      const { keybindings } = useOpenCutKeybindingsStore.getState();
      const jKey = keybindings.find((kb) => kb.id === 'play-reverse');
      const kKey = keybindings.find((kb) => kb.id === 'pause');
      const lKey = keybindings.find((kb) => kb.id === 'play-forward');

      expect(jKey).toBeDefined();
      expect(jKey?.key).toBe('j');
      expect(kKey).toBeDefined();
      expect(kKey?.key).toBe('k');
      expect(lKey).toBeDefined();
      expect(lKey?.key).toBe('l');
    });

    it('should have mark in/out keybindings', () => {
      const { keybindings } = useOpenCutKeybindingsStore.getState();
      const markIn = keybindings.find((kb) => kb.id === 'mark-in');
      const markOut = keybindings.find((kb) => kb.id === 'mark-out');

      expect(markIn).toBeDefined();
      expect(markIn?.key).toBe('i');
      expect(markOut).toBeDefined();
      expect(markOut?.key).toBe('o');
    });
  });

  describe('Get Keybindings', () => {
    it('should get keybinding for action', () => {
      const { getKeybindingForAction } = useOpenCutKeybindingsStore.getState();
      const togglePlay = getKeybindingForAction('togglePlayPause');

      expect(togglePlay).toBeDefined();
      expect(togglePlay?.key).toBe(' ');
    });

    it('should return undefined for non-existent action', () => {
      const { getKeybindingForAction } = useOpenCutKeybindingsStore.getState();
      const nonExistent = getKeybindingForAction('nonExistentAction');

      expect(nonExistent).toBeUndefined();
    });

    it('should get keybindings by category', () => {
      const { getKeybindingsForCategory } = useOpenCutKeybindingsStore.getState();
      const playbackBindings = getKeybindingsForCategory('playback');

      expect(playbackBindings.length).toBeGreaterThan(0);
      expect(playbackBindings.every((kb) => kb.category === 'playback')).toBe(true);
    });
  });

  describe('Find Keybinding By Key', () => {
    it('should find keybinding by key without modifiers', () => {
      const { findKeybindingByKey } = useOpenCutKeybindingsStore.getState();
      const kb = findKeybindingByKey('j', {});

      expect(kb).toBeDefined();
      expect(kb?.action).toBe('playReverse');
    });

    it('should find keybinding by key with modifiers', () => {
      const { findKeybindingByKey } = useOpenCutKeybindingsStore.getState();
      const kb = findKeybindingByKey('z', { ctrl: true });

      expect(kb).toBeDefined();
      expect(kb?.action).toBe('undo');
    });

    it('should not find keybinding with wrong modifiers', () => {
      const { findKeybindingByKey } = useOpenCutKeybindingsStore.getState();
      // 'j' with ctrl should not match plain 'j'
      const kb = findKeybindingByKey('j', { ctrl: true });

      expect(kb).toBeUndefined();
    });

    it('should be case insensitive', () => {
      const { findKeybindingByKey } = useOpenCutKeybindingsStore.getState();
      const kb = findKeybindingByKey('J', {});

      expect(kb).toBeDefined();
      expect(kb?.action).toBe('playReverse');
    });
  });

  describe('Update Keybinding', () => {
    it('should update keybinding key', () => {
      const { updateKeybinding, keybindings } = useOpenCutKeybindingsStore.getState();

      useOpenCutKeybindingsStore.getState().updateKeybinding('play-reverse', { key: 'h' });

      const state = useOpenCutKeybindingsStore.getState();
      const updated = state.keybindings.find((kb) => kb.id === 'play-reverse');
      expect(updated?.key).toBe('h');
    });

    it('should update keybinding modifiers', () => {
      useOpenCutKeybindingsStore.getState().updateKeybinding('play-reverse', {
        modifiers: { ctrl: true },
      });

      const state = useOpenCutKeybindingsStore.getState();
      const updated = state.keybindings.find((kb) => kb.id === 'play-reverse');
      expect(updated?.modifiers.ctrl).toBe(true);
    });

    it('should toggle keybinding enabled state', () => {
      useOpenCutKeybindingsStore.getState().updateKeybinding('play-reverse', {
        enabled: false,
      });

      const state = useOpenCutKeybindingsStore.getState();
      const updated = state.keybindings.find((kb) => kb.id === 'play-reverse');
      expect(updated?.enabled).toBe(false);
    });
  });

  describe('Reset Keybindings', () => {
    it('should reset single keybinding to default', () => {
      // First modify the keybinding
      useOpenCutKeybindingsStore.getState().updateKeybinding('play-reverse', { key: 'h' });

      // Then reset it
      useOpenCutKeybindingsStore.getState().resetKeybinding('play-reverse');

      const state = useOpenCutKeybindingsStore.getState();
      const reset = state.keybindings.find((kb) => kb.id === 'play-reverse');
      expect(reset?.key).toBe('j');
    });

    it('should reset all keybindings to defaults', () => {
      // Modify multiple keybindings
      useOpenCutKeybindingsStore.getState().updateKeybinding('play-reverse', { key: 'h' });
      useOpenCutKeybindingsStore.getState().updateKeybinding('pause', { key: 'x' });

      // Reset all
      useOpenCutKeybindingsStore.getState().resetAllKeybindings();

      const state = useOpenCutKeybindingsStore.getState();
      expect(state.keybindings.find((kb) => kb.id === 'play-reverse')?.key).toBe('j');
      expect(state.keybindings.find((kb) => kb.id === 'pause')?.key).toBe('k');
    });
  });

  describe('Conflict Detection', () => {
    it('should detect keybinding conflict', () => {
      const { isKeybindingConflict } = useOpenCutKeybindingsStore.getState();

      // 'j' is already bound to play-reverse
      const hasConflict = isKeybindingConflict('j', {});
      expect(hasConflict).toBe(true);
    });

    it('should not report conflict for unused key', () => {
      const { isKeybindingConflict } = useOpenCutKeybindingsStore.getState();

      const hasConflict = isKeybindingConflict('x', {});
      expect(hasConflict).toBe(false);
    });

    it('should exclude specific ID from conflict check', () => {
      const { isKeybindingConflict } = useOpenCutKeybindingsStore.getState();

      // 'j' conflicts, but not if we exclude play-reverse
      const hasConflict = isKeybindingConflict('j', {}, 'play-reverse');
      expect(hasConflict).toBe(false);
    });

    it('should consider modifiers in conflict detection', () => {
      const { isKeybindingConflict } = useOpenCutKeybindingsStore.getState();

      // 'z' without modifiers should not conflict with Ctrl+Z
      const noConflict = isKeybindingConflict('z', {});
      expect(noConflict).toBe(false);

      // Ctrl+Z is bound to undo
      const hasConflict = isKeybindingConflict('z', { ctrl: true });
      expect(hasConflict).toBe(true);
    });
  });

  describe('JKL Speed Control', () => {
    it('should set JKL speed', () => {
      useOpenCutKeybindingsStore.getState().setJKLSpeed(2);

      const state = useOpenCutKeybindingsStore.getState();
      expect(state.jklSpeed).toBe(2);
    });

    it('should clamp JKL speed to valid range', () => {
      useOpenCutKeybindingsStore.getState().setJKLSpeed(0.1);
      expect(useOpenCutKeybindingsStore.getState().jklSpeed).toBe(0.25);

      useOpenCutKeybindingsStore.getState().setJKLSpeed(16);
      expect(useOpenCutKeybindingsStore.getState().jklSpeed).toBe(8);
    });

    it('should increment JKL speed', () => {
      useOpenCutKeybindingsStore.getState().setJKLSpeed(1);
      useOpenCutKeybindingsStore.getState().incrementJKLSpeed();

      expect(useOpenCutKeybindingsStore.getState().jklSpeed).toBe(2);
    });

    it('should decrement JKL speed', () => {
      useOpenCutKeybindingsStore.getState().setJKLSpeed(2);
      useOpenCutKeybindingsStore.getState().decrementJKLSpeed();

      expect(useOpenCutKeybindingsStore.getState().jklSpeed).toBe(1);
    });

    it('should set JKL direction', () => {
      useOpenCutKeybindingsStore.getState().setJKLDirection(-1);
      expect(useOpenCutKeybindingsStore.getState().jklDirection).toBe(-1);

      useOpenCutKeybindingsStore.getState().setJKLDirection(1);
      expect(useOpenCutKeybindingsStore.getState().jklDirection).toBe(1);
    });

    it('should reset JKL state', () => {
      useOpenCutKeybindingsStore.getState().setJKLSpeed(4);
      useOpenCutKeybindingsStore.getState().setJKLDirection(-1);
      useOpenCutKeybindingsStore.getState().resetJKL();

      const state = useOpenCutKeybindingsStore.getState();
      expect(state.jklSpeed).toBe(1);
      expect(state.jklDirection).toBe(0);
    });
  });
});
