/**
 * Tests for clip commands
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  AddClipCommand,
  DeleteClipCommand,
  MoveClipCommand,
  TrimClipCommand,
  AddEffectCommand,
  RemoveEffectCommand,
  UpdatePropertyCommand,
} from '../clipCommands';
import { TimelineClip } from '../../pages/VideoEditorPage';
import { AppliedEffect } from '../../types/effects';

describe('Clip Commands', () => {
  let clips: TimelineClip[];
  let setClips: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    clips = [
      {
        id: 'clip-1',
        trackId: 'track-1',
        startTime: 0,
        duration: 5,
        label: 'Test Clip 1',
        type: 'video',
        effects: [],
      },
      {
        id: 'clip-2',
        trackId: 'track-1',
        startTime: 5,
        duration: 3,
        label: 'Test Clip 2',
        type: 'audio',
        effects: [],
      },
    ];
    setClips = vi.fn((updater) => {
      if (typeof updater === 'function') {
        clips = updater(clips);
      } else {
        clips = updater;
      }
    });
  });

  describe('AddClipCommand', () => {
    it('should add a clip on execute', () => {
      const newClip: TimelineClip = {
        id: 'clip-3',
        trackId: 'track-2',
        startTime: 10,
        duration: 4,
        label: 'New Clip',
        type: 'video',
      };

      const command = new AddClipCommand(newClip, setClips);
      command.execute();

      expect(setClips).toHaveBeenCalled();
      expect(clips).toHaveLength(3);
      expect(clips[2].id).toBe('clip-3');
    });

    it('should remove the clip on undo', () => {
      const newClip: TimelineClip = {
        id: 'clip-3',
        trackId: 'track-2',
        startTime: 10,
        duration: 4,
        label: 'New Clip',
        type: 'video',
      };

      const command = new AddClipCommand(newClip, setClips);
      command.execute();
      
      setClips.mockClear();
      command.undo();

      expect(setClips).toHaveBeenCalled();
      expect(clips).toHaveLength(2);
      expect(clips.find((c) => c.id === 'clip-3')).toBeUndefined();
    });

    it('should have correct description', () => {
      const newClip: TimelineClip = {
        id: 'clip-3',
        trackId: 'track-2',
        startTime: 10,
        duration: 4,
        label: 'My Video',
        type: 'video',
      };

      const command = new AddClipCommand(newClip, setClips);
      expect(command.getDescription()).toBe('Add My Video');
    });
  });

  describe('DeleteClipCommand', () => {
    it('should delete a clip on execute', () => {
      const command = new DeleteClipCommand('clip-1', clips, setClips);
      command.execute();

      expect(clips).toHaveLength(1);
      expect(clips.find((c) => c.id === 'clip-1')).toBeUndefined();
    });

    it('should restore the clip on undo', () => {
      const originalClips = [...clips];
      const command = new DeleteClipCommand('clip-1', clips, setClips);
      command.execute();
      command.undo();

      expect(clips).toHaveLength(2);
      expect(clips[0]).toEqual(originalClips[0]);
    });

    it('should restore clip at correct index', () => {
      const command = new DeleteClipCommand('clip-1', clips, setClips);
      command.execute();
      command.undo();

      expect(clips[0].id).toBe('clip-1');
      expect(clips[1].id).toBe('clip-2');
    });

    it('should have correct description', () => {
      const command = new DeleteClipCommand('clip-1', clips, setClips);
      expect(command.getDescription()).toBe('Delete Test Clip 1');
    });
  });

  describe('MoveClipCommand', () => {
    it('should move clip to new position on execute', () => {
      const command = new MoveClipCommand('clip-1', 10, 'track-2', clips, setClips);
      command.execute();

      const movedClip = clips.find((c) => c.id === 'clip-1');
      expect(movedClip?.startTime).toBe(10);
      expect(movedClip?.trackId).toBe('track-2');
    });

    it('should restore original position on undo', () => {
      const command = new MoveClipCommand('clip-1', 10, 'track-2', clips, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.startTime).toBe(0);
      expect(clip?.trackId).toBe('track-1');
    });

    it('should have correct description', () => {
      const command = new MoveClipCommand('clip-1', 10, 'track-2', clips, setClips);
      expect(command.getDescription()).toBe('Move clip');
    });
  });

  describe('TrimClipCommand', () => {
    it('should trim clip on execute', () => {
      const command = new TrimClipCommand('clip-1', 1, 3, clips, setClips);
      command.execute();

      const trimmedClip = clips.find((c) => c.id === 'clip-1');
      expect(trimmedClip?.startTime).toBe(1);
      expect(trimmedClip?.duration).toBe(3);
    });

    it('should restore original dimensions on undo', () => {
      const command = new TrimClipCommand('clip-1', 1, 3, clips, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.startTime).toBe(0);
      expect(clip?.duration).toBe(5);
    });

    it('should have correct description', () => {
      const command = new TrimClipCommand('clip-1', 1, 3, clips, setClips);
      expect(command.getDescription()).toBe('Trim clip');
    });
  });

  describe('AddEffectCommand', () => {
    it('should add effect to clip on execute', () => {
      const effect: AppliedEffect = {
        id: 'effect-1',
        effectType: 'brightness',
        enabled: true,
        parameters: { value: 50 },
      };

      const command = new AddEffectCommand('clip-1', effect, setClips);
      command.execute();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.effects).toHaveLength(1);
      expect(clip?.effects?.[0].id).toBe('effect-1');
    });

    it('should remove effect on undo', () => {
      const effect: AppliedEffect = {
        id: 'effect-1',
        effectType: 'brightness',
        enabled: true,
        parameters: { value: 50 },
      };

      const command = new AddEffectCommand('clip-1', effect, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.effects).toHaveLength(0);
    });

    it('should have correct description', () => {
      const effect: AppliedEffect = {
        id: 'effect-1',
        effectType: 'brightness',
        enabled: true,
        parameters: { value: 50 },
      };

      const command = new AddEffectCommand('clip-1', effect, setClips);
      expect(command.getDescription()).toBe('Add brightness effect');
    });
  });

  describe('RemoveEffectCommand', () => {
    beforeEach(() => {
      clips[0].effects = [
        {
          id: 'effect-1',
          effectType: 'brightness',
          enabled: true,
          parameters: { value: 50 },
        },
        {
          id: 'effect-2',
          effectType: 'contrast',
          enabled: true,
          parameters: { value: 30 },
        },
      ];
    });

    it('should remove effect on execute', () => {
      const command = new RemoveEffectCommand('clip-1', 'effect-1', clips, setClips);
      command.execute();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.effects).toHaveLength(1);
      expect(clip?.effects?.[0].id).toBe('effect-2');
    });

    it('should restore effect on undo', () => {
      const command = new RemoveEffectCommand('clip-1', 'effect-1', clips, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.effects).toHaveLength(2);
      expect(clip?.effects?.[0].id).toBe('effect-1');
    });

    it('should restore effect at correct index', () => {
      const command = new RemoveEffectCommand('clip-1', 'effect-1', clips, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.effects?.[0].id).toBe('effect-1');
      expect(clip?.effects?.[1].id).toBe('effect-2');
    });

    it('should have correct description', () => {
      const command = new RemoveEffectCommand('clip-1', 'effect-1', clips, setClips);
      expect(command.getDescription()).toBe('Remove brightness');
    });
  });

  describe('UpdatePropertyCommand', () => {
    it('should update properties on execute', () => {
      const updates = { label: 'Updated Label', duration: 10 };
      const command = new UpdatePropertyCommand('clip-1', updates, clips, setClips);
      command.execute();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.label).toBe('Updated Label');
      expect(clip?.duration).toBe(10);
    });

    it('should restore original values on undo', () => {
      const updates = { label: 'Updated Label', duration: 10 };
      const command = new UpdatePropertyCommand('clip-1', updates, clips, setClips);
      command.execute();
      command.undo();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.label).toBe('Test Clip 1');
      expect(clip?.duration).toBe(5);
    });

    it('should handle transform updates', () => {
      const updates = { transform: { x: 100, y: 50, scale: 1.5 } };
      const command = new UpdatePropertyCommand('clip-1', updates, clips, setClips);
      command.execute();

      const clip = clips.find((c) => c.id === 'clip-1');
      expect(clip?.transform?.x).toBe(100);
      expect(clip?.transform?.y).toBe(50);
      expect(clip?.transform?.scale).toBe(1.5);
    });

    it('should have correct description', () => {
      const updates = { label: 'Updated', duration: 10 };
      const command = new UpdatePropertyCommand('clip-1', updates, clips, setClips);
      expect(command.getDescription()).toBe('Update label, duration');
    });
  });

  describe('Command Timestamps', () => {
    it('should have timestamps', () => {
      const newClip: TimelineClip = {
        id: 'clip-3',
        trackId: 'track-2',
        startTime: 10,
        duration: 4,
        label: 'New Clip',
        type: 'video',
      };

      const command = new AddClipCommand(newClip, setClips);
      const timestamp = command.getTimestamp();
      
      expect(timestamp).toBeInstanceOf(Date);
      expect(timestamp.getTime()).toBeLessThanOrEqual(Date.now());
    });
  });
});
