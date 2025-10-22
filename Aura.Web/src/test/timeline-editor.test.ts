/**
 * Tests for TimelineEditor service
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { TimelineEditor } from '../services/timeline/TimelineEditor';
import type { TimelineScene } from '../types/timeline';

describe('TimelineEditor', () => {
  let editor: TimelineEditor;
  let mockScenes: TimelineScene[];

  beforeEach(() => {
    editor = new TimelineEditor();
    mockScenes = [
      {
        index: 0,
        heading: 'Scene 1',
        script: 'This is scene 1 script',
        start: 0,
        duration: 10,
        visualAssets: [],
        transitionType: 'fade',
      },
      {
        index: 1,
        heading: 'Scene 2',
        script: 'This is scene 2 script',
        start: 10,
        duration: 15,
        visualAssets: [],
        transitionType: 'fade',
      },
      {
        index: 2,
        heading: 'Scene 3',
        script: 'This is scene 3 script',
        start: 25,
        duration: 8,
        visualAssets: [],
        transitionType: 'fade',
      },
    ];
  });

  describe('spliceAtPlayhead', () => {
    it('should split scene at playhead position', () => {
      const result = editor.spliceAtPlayhead(mockScenes, 1, 15);

      expect(result).toHaveLength(4);
      expect(result[1].duration).toBe(5); // First half
      expect(result[2].duration).toBe(10); // Second half
      expect(result[2].start).toBe(15);
    });

    it('should not split if playhead is outside scene bounds', () => {
      const result = editor.spliceAtPlayhead(mockScenes, 1, 5);
      expect(result).toEqual(mockScenes);
    });

    it('should update indices of following scenes', () => {
      const result = editor.spliceAtPlayhead(mockScenes, 1, 15);
      expect(result[3].index).toBe(3);
    });
  });

  describe('rippleDelete', () => {
    it('should delete scene and shift following scenes', () => {
      const result = editor.rippleDelete(mockScenes, 1);

      expect(result).toHaveLength(2);
      expect(result[1].start).toBe(10); // Scene 3 shifted to where Scene 2 was
      expect(result[1].heading).toBe('Scene 3');
    });

    it('should update indices correctly', () => {
      const result = editor.rippleDelete(mockScenes, 0);
      expect(result[0].index).toBe(0);
      expect(result[1].index).toBe(1);
    });
  });

  describe('deleteScene', () => {
    it('should delete scene without shifting others', () => {
      const result = editor.deleteScene(mockScenes, 1);

      expect(result).toHaveLength(2);
      expect(result[1].start).toBe(25); // Scene 3 not shifted
    });
  });

  describe('closeGaps', () => {
    it('should close gaps between scenes', () => {
      // Create scenes with gaps
      const scenesWithGaps: TimelineScene[] = [
        { ...mockScenes[0], start: 0, duration: 10 },
        { ...mockScenes[1], start: 20, duration: 10 }, // Gap of 10 seconds
        { ...mockScenes[2], start: 40, duration: 10 }, // Gap of 10 seconds
      ];

      const result = editor.closeGaps(scenesWithGaps);

      expect(result[0].start).toBe(0);
      expect(result[1].start).toBe(10); // No gap
      expect(result[2].start).toBe(20); // No gap
    });
  });

  describe('undo/redo', () => {
    it('should undo last operation', () => {
      const modified = editor.rippleDelete(mockScenes, 1);
      const undone = editor.undo();

      expect(undone).toEqual(mockScenes);
    });

    it('should redo undone operation', () => {
      const modified = editor.rippleDelete(mockScenes, 1);
      editor.undo();
      const redone = editor.redo();

      expect(redone).toEqual(modified);
    });

    it('should return null if no operations to undo', () => {
      expect(editor.undo()).toBeNull();
    });

    it('should return null if no operations to redo', () => {
      expect(editor.redo()).toBeNull();
    });

    it('should clear redo stack on new operation', () => {
      editor.rippleDelete(mockScenes, 1);
      editor.undo();
      expect(editor.canRedo()).toBe(true);

      editor.spliceAtPlayhead(mockScenes, 0, 5);
      expect(editor.canRedo()).toBe(false);
    });
  });

  describe('canUndo/canRedo', () => {
    it('should report undo availability correctly', () => {
      expect(editor.canUndo()).toBe(false);
      editor.rippleDelete(mockScenes, 1);
      expect(editor.canUndo()).toBe(true);
    });

    it('should report redo availability correctly', () => {
      expect(editor.canRedo()).toBe(false);
      editor.rippleDelete(mockScenes, 1);
      editor.undo();
      expect(editor.canRedo()).toBe(true);
    });
  });
});
