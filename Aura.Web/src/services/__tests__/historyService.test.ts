/**
 * Tests for History Service
 */

import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { HistoryService } from '../historyService';
import type { HistoryActionType, HistoryScope } from '../../types/history';

describe('HistoryService', () => {
  let historyService: HistoryService;

  beforeEach(() => {
    vi.useFakeTimers();
    historyService = new HistoryService();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('Basic Operations', () => {
    it('should start with empty history', () => {
      expect(historyService.canUndo()).toBe(false);
      expect(historyService.canRedo()).toBe(false);
      expect(historyService.getHistorySize()).toBe(0);
    });

    it('should record an action', () => {
      historyService.record(
        'clip:add',
        'Add clip',
        { clipId: '1', previousState: null },
        { clipId: '1', newState: { startTime: 0 } },
        'timeline'
      );

      expect(historyService.canUndo()).toBe(true);
      expect(historyService.canRedo()).toBe(false);
      expect(historyService.getHistorySize()).toBe(1);
    });

    it('should undo an action', () => {
      historyService.record('clip:add', 'Add clip', { clipId: '1' }, { clipId: '1' }, 'timeline');

      const entry = historyService.undo();

      expect(entry).not.toBeNull();
      expect(entry?.actionType).toBe('clip:add');
      expect(historyService.canUndo()).toBe(false);
      expect(historyService.canRedo()).toBe(true);
    });

    it('should redo an action', () => {
      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');

      historyService.undo();
      expect(historyService.canRedo()).toBe(true);

      const entry = historyService.redo();

      expect(entry).not.toBeNull();
      expect(entry?.actionType).toBe('clip:add');
      expect(historyService.canUndo()).toBe(true);
      expect(historyService.canRedo()).toBe(false);
    });

    it('should return null when undo is not available', () => {
      const result = historyService.undo();
      expect(result).toBeNull();
    });

    it('should return null when redo is not available', () => {
      const result = historyService.redo();
      expect(result).toBeNull();
    });

    it('should clear redo stack when new action is recorded', () => {
      historyService.record('clip:add', 'Add clip 1', {}, {}, 'timeline');
      historyService.undo();

      expect(historyService.canRedo()).toBe(true);

      historyService.record('clip:add', 'Add clip 2', {}, {}, 'timeline');

      expect(historyService.canRedo()).toBe(false);
    });
  });

  describe('History Descriptions', () => {
    it('should return correct undo description', () => {
      historyService.record('clip:add', 'Add video clip', {}, {}, 'timeline');

      expect(historyService.getUndoDescription()).toBe('Add video clip');
    });

    it('should return correct redo description', () => {
      historyService.record('clip:delete', 'Delete clip', {}, {}, 'timeline');
      historyService.undo();

      expect(historyService.getRedoDescription()).toBe('Delete clip');
    });

    it('should return null for undo description when empty', () => {
      expect(historyService.getUndoDescription()).toBeNull();
    });

    it('should return null for redo description when empty', () => {
      expect(historyService.getRedoDescription()).toBeNull();
    });
  });

  describe('History Management', () => {
    it('should get history in reverse chronological order', () => {
      historyService.record('clip:add', 'Action 1', {}, {}, 'timeline');
      historyService.record('clip:delete', 'Action 2', {}, {}, 'timeline');
      historyService.record('track:add', 'Action 3', {}, {}, 'timeline');

      const history = historyService.getHistory();

      expect(history).toHaveLength(3);
      expect(history[0].description).toBe('Action 3');
      expect(history[1].description).toBe('Action 2');
      expect(history[2].description).toBe('Action 1');
    });

    it('should clear all history', () => {
      historyService.record('clip:add', 'Action 1', {}, {}, 'timeline');
      historyService.record('clip:add', 'Action 2', {}, {}, 'timeline');
      historyService.undo();

      historyService.clear();

      expect(historyService.canUndo()).toBe(false);
      expect(historyService.canRedo()).toBe(false);
      expect(historyService.getHistorySize()).toBe(0);
    });

    it('should enforce maximum history size', () => {
      const smallHistory = new HistoryService({ maxEntries: 3 });

      for (let i = 0; i < 5; i++) {
        smallHistory.record('clip:add', `Action ${i}`, {}, {}, 'timeline');
      }

      expect(smallHistory.getHistorySize()).toBe(3);
      expect(smallHistory.getUndoDescription()).toBe('Action 4');
    });

    it('should update configuration', () => {
      for (let i = 0; i < 10; i++) {
        historyService.record('clip:add', `Action ${i}`, {}, {}, 'timeline');
      }

      historyService.setConfig({ maxEntries: 5 });

      expect(historyService.getHistorySize()).toBe(5);
    });
  });

  describe('Excluded Actions', () => {
    it('should exclude selection:change by default', () => {
      historyService.record('selection:change', 'Selection changed', {}, {}, 'timeline');

      expect(historyService.getHistorySize()).toBe(0);
    });

    it('should allow configuring excluded actions', () => {
      const customHistory = new HistoryService({
        excludeActions: ['clip:move', 'clip:resize'],
      });

      customHistory.record('clip:add', 'Add clip', {}, {}, 'timeline');
      customHistory.record('clip:move', 'Move clip', {}, {}, 'timeline');
      customHistory.record('clip:resize', 'Resize clip', {}, {}, 'timeline');

      expect(customHistory.getHistorySize()).toBe(1);
      expect(customHistory.getUndoDescription()).toBe('Add clip');
    });
  });

  describe('Compound Actions', () => {
    it('should start and end compound actions', () => {
      const groupId = historyService.startCompound('Batch delete');

      expect(groupId).toBeDefined();
      expect(typeof groupId).toBe('string');

      historyService.record('clip:delete', 'Delete clip 1', {}, {}, 'timeline');
      historyService.record('clip:delete', 'Delete clip 2', {}, {}, 'timeline');

      historyService.endCompound();

      // Should have: compound:start + 2 deletions + compound:end
      expect(historyService.getHistorySize()).toBe(4);
    });

    it('should group compound entries with the same groupId', () => {
      historyService.startCompound('Multi-clip operation');
      historyService.record('clip:add', 'Add clip 1', {}, {}, 'timeline');
      historyService.record('clip:add', 'Add clip 2', {}, {}, 'timeline');
      historyService.endCompound();

      const history = historyService.getHistory();

      // All entries except compound:end should have the same groupId
      const groupIds = history.filter((e) => e.actionType !== 'compound:end').map((e) => e.groupId);
      const uniqueGroupIds = [...new Set(groupIds.filter((id) => id !== undefined))];

      expect(uniqueGroupIds.length).toBe(1);
    });
  });

  describe('Save Points', () => {
    it('should mark save points', () => {
      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');
      historyService.markSavePoint();

      const state = historyService.getState();

      expect(state.savePoints.length).toBe(1);
      expect(state.entries[0].isSavePoint).toBe(true);
    });

    it('should not mark save point when history is empty', () => {
      historyService.markSavePoint();

      const state = historyService.getState();
      expect(state.savePoints.length).toBe(0);
    });
  });

  describe('Debouncing', () => {
    it('should debounce clip:resize actions', () => {
      historyService.record('clip:resize', 'Resize 1', {}, {}, 'timeline');
      historyService.record('clip:resize', 'Resize 2', {}, {}, 'timeline');
      historyService.record('clip:resize', 'Resize 3', {}, {}, 'timeline');

      // Before debounce timer fires
      expect(historyService.getHistorySize()).toBe(0);

      // Fast-forward past debounce time
      vi.advanceTimersByTime(350);

      expect(historyService.getHistorySize()).toBe(1);
      expect(historyService.getUndoDescription()).toBe('Resize 3');
    });

    it('should debounce clip:move actions', () => {
      historyService.record('clip:move', 'Move 1', {}, {}, 'timeline');

      expect(historyService.getHistorySize()).toBe(0);

      vi.advanceTimersByTime(350);

      expect(historyService.getHistorySize()).toBe(1);
    });

    it('should not debounce non-debounceable actions', () => {
      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');

      // Should be added immediately
      expect(historyService.getHistorySize()).toBe(1);
    });

    it('should clear pending entries on clear()', () => {
      historyService.record('clip:resize', 'Resize', {}, {}, 'timeline');

      historyService.clear();

      vi.advanceTimersByTime(350);

      expect(historyService.getHistorySize()).toBe(0);
    });
  });

  describe('Listeners', () => {
    it('should notify listeners on record', () => {
      const listener = vi.fn();
      historyService.subscribe(listener);

      // Should be called immediately on subscribe
      expect(listener).toHaveBeenCalledTimes(1);

      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');

      expect(listener).toHaveBeenCalledTimes(2);
    });

    it('should notify listeners on undo', () => {
      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');

      const listener = vi.fn();
      historyService.subscribe(listener);
      listener.mockClear();

      historyService.undo();

      expect(listener).toHaveBeenCalled();
    });

    it('should notify listeners on redo', () => {
      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');
      historyService.undo();

      const listener = vi.fn();
      historyService.subscribe(listener);
      listener.mockClear();

      historyService.redo();

      expect(listener).toHaveBeenCalled();
    });

    it('should allow unsubscribing', () => {
      const listener = vi.fn();
      const unsubscribe = historyService.subscribe(listener);

      listener.mockClear();
      unsubscribe();

      historyService.record('clip:add', 'Add clip', {}, {}, 'timeline');

      expect(listener).not.toHaveBeenCalled();
    });

    it('should handle errors in listeners gracefully', () => {
      const faultyListener = () => {
        throw new Error('Listener error');
      };
      const goodListener = vi.fn();

      historyService.subscribe(faultyListener);
      historyService.subscribe(goodListener);

      // Should not throw
      expect(() => historyService.record('clip:add', 'Add clip', {}, {}, 'timeline')).not.toThrow();
      expect(goodListener).toHaveBeenCalled();
    });
  });

  describe('Initialization', () => {
    it('should initialize with existing state', () => {
      const existingState = {
        entries: [
          {
            id: '1',
            actionType: 'clip:add' as HistoryActionType,
            description: 'Existing action',
            timestamp: new Date().toISOString(),
            undoData: {},
            redoData: {},
            scope: 'timeline' as HistoryScope,
          },
        ],
        currentIndex: 0,
        maxEntries: 100,
        redoStack: [],
        savePoints: [],
      };

      historyService.initialize(existingState);

      expect(historyService.getHistorySize()).toBe(1);
      expect(historyService.getUndoDescription()).toBe('Existing action');
    });

    it('should clear when initialized without state', () => {
      historyService.record('clip:add', 'Action', {}, {}, 'timeline');
      historyService.initialize();

      expect(historyService.getHistorySize()).toBe(0);
    });
  });

  describe('Multiple Undo/Redo Operations', () => {
    it('should handle multiple undo operations', () => {
      historyService.record('clip:add', 'Action 1', {}, {}, 'timeline');
      historyService.record('clip:add', 'Action 2', {}, {}, 'timeline');
      historyService.record('clip:add', 'Action 3', {}, {}, 'timeline');

      historyService.undo();
      expect(historyService.getUndoDescription()).toBe('Action 2');

      historyService.undo();
      expect(historyService.getUndoDescription()).toBe('Action 1');

      historyService.undo();
      expect(historyService.canUndo()).toBe(false);
    });

    it('should handle multiple redo operations', () => {
      historyService.record('clip:add', 'Action 1', {}, {}, 'timeline');
      historyService.record('clip:add', 'Action 2', {}, {}, 'timeline');

      historyService.undo();
      historyService.undo();

      historyService.redo();
      expect(historyService.getUndoDescription()).toBe('Action 1');

      historyService.redo();
      expect(historyService.getUndoDescription()).toBe('Action 2');
      expect(historyService.canRedo()).toBe(false);
    });
  });

  describe('State Retrieval', () => {
    it('should return a copy of state', () => {
      historyService.record('clip:add', 'Action', {}, {}, 'timeline');

      const state1 = historyService.getState();
      const state2 = historyService.getState();

      expect(state1).not.toBe(state2);
      expect(state1).toEqual(state2);
    });

    it('should return a copy of config', () => {
      const config1 = historyService.getConfig();
      const config2 = historyService.getConfig();

      expect(config1).not.toBe(config2);
      expect(config1).toEqual(config2);
    });
  });
});
