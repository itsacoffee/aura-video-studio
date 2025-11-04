import { describe, it, expect, beforeEach } from 'vitest';
import { useTimelineStore } from '../../state/timeline';
import {
  SelectClipsCommand,
  DeleteClipsCommand,
  RippleDeleteClipsCommand,
  MoveClipsCommand,
  AddMarkerCommand,
  ToggleRippleEditCommand,
} from '../timelineCommands';

describe('Timeline Commands', () => {
  beforeEach(() => {
    useTimelineStore.setState({
      tracks: [
        { id: 'V1', name: 'Video 1', type: 'video', clips: [] },
        { id: 'A1', name: 'Audio 1', type: 'audio', clips: [] },
      ],
      markers: [],
      overlays: [],
      currentTime: 0,
      zoom: 50,
      snappingEnabled: true,
      selectedClipId: null,
      selectedClipIds: [],
      selectedOverlayId: null,
      rippleEditMode: false,
      magneticTimelineEnabled: false,
    });
  });

  describe('SelectClipsCommand', () => {
    it('should select clips and undo selection', () => {
      const store = useTimelineStore;
      const command = new SelectClipsCommand(['clip1', 'clip2'], store);

      command.execute();
      expect(store.getState().selectedClipIds).toEqual(['clip1', 'clip2']);

      command.undo();
      expect(store.getState().selectedClipIds).toEqual([]);
    });

    it('should have correct description', () => {
      const command = new SelectClipsCommand(['clip1', 'clip2'], useTimelineStore);
      expect(command.getDescription()).toBe('Select 2 clips');
    });
  });

  describe('DeleteClipsCommand', () => {
    it('should delete clips and restore them on undo', () => {
      const store = useTimelineStore.getState();

      const clip1 = {
        id: 'clip1',
        sourcePath: '/video1.mp4',
        sourceIn: 0,
        sourceOut: 10,
        timelineStart: 0,
        trackId: 'V1',
      };

      const clip2 = {
        id: 'clip2',
        sourcePath: '/video2.mp4',
        sourceIn: 0,
        sourceOut: 10,
        timelineStart: 10,
        trackId: 'V1',
      };

      store.addClip('V1', clip1);
      store.addClip('V1', clip2);

      const command = new DeleteClipsCommand(['clip1'], useTimelineStore);

      command.execute();
      const v1Track = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      expect(v1Track?.clips).toHaveLength(1);
      expect(v1Track?.clips[0].id).toBe('clip2');

      command.undo();
      const v1TrackAfterUndo = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      expect(v1TrackAfterUndo?.clips).toHaveLength(2);
    });

    it('should have correct description', () => {
      const command = new DeleteClipsCommand(['clip1', 'clip2'], useTimelineStore);
      expect(command.getDescription()).toBe('Delete 2 clips');
    });
  });

  describe('RippleDeleteClipsCommand', () => {
    it('should ripple delete clips and restore timeline on undo', () => {
      const store = useTimelineStore.getState();

      const clips = [
        {
          id: 'clip1',
          sourcePath: '/video1.mp4',
          sourceIn: 0,
          sourceOut: 5,
          timelineStart: 0,
          trackId: 'V1',
        },
        {
          id: 'clip2',
          sourcePath: '/video2.mp4',
          sourceIn: 0,
          sourceOut: 5,
          timelineStart: 5,
          trackId: 'V1',
        },
        {
          id: 'clip3',
          sourcePath: '/video3.mp4',
          sourceIn: 0,
          sourceOut: 5,
          timelineStart: 10,
          trackId: 'V1',
        },
      ];

      clips.forEach((clip) => store.addClip('V1', clip));

      const command = new RippleDeleteClipsCommand(['clip1'], useTimelineStore);

      command.execute();
      const v1Track = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      expect(v1Track?.clips).toHaveLength(2);
      expect(v1Track?.clips[0].timelineStart).toBe(0);

      command.undo();
      const v1TrackAfterUndo = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      expect(v1TrackAfterUndo?.clips).toHaveLength(3);
      expect(v1TrackAfterUndo?.clips[0].timelineStart).toBe(0);
      expect(v1TrackAfterUndo?.clips[1].timelineStart).toBe(5);
    });
  });

  describe('MoveClipsCommand', () => {
    it('should move clips and restore positions on undo', () => {
      const store = useTimelineStore.getState();

      const clips = [
        {
          id: 'clip1',
          sourcePath: '/video1.mp4',
          sourceIn: 0,
          sourceOut: 5,
          timelineStart: 0,
          trackId: 'V1',
        },
        {
          id: 'clip2',
          sourcePath: '/video2.mp4',
          sourceIn: 0,
          sourceOut: 5,
          timelineStart: 5,
          trackId: 'V1',
        },
      ];

      clips.forEach((clip) => store.addClip('V1', clip));

      const command = new MoveClipsCommand(['clip1', 'clip2'], 10, useTimelineStore);

      command.execute();
      const v1Track = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      const movedClip1 = v1Track?.clips.find((c) => c.id === 'clip1');
      expect(movedClip1?.timelineStart).toBe(10);

      command.undo();
      const v1TrackAfterUndo = useTimelineStore.getState().tracks.find((t) => t.id === 'V1');
      const restoredClip1 = v1TrackAfterUndo?.clips.find((c) => c.id === 'clip1');
      expect(restoredClip1?.timelineStart).toBe(0);
    });

    it('should have correct description', () => {
      const command = new MoveClipsCommand(['clip1', 'clip2'], 5, useTimelineStore);
      expect(command.getDescription()).toBe('Move 2 clips');
    });
  });

  describe('AddMarkerCommand', () => {
    it('should add marker and remove on undo', () => {
      const marker = { id: 'marker1', title: 'Chapter 1', time: 10 };
      const command = new AddMarkerCommand(marker, useTimelineStore);

      command.execute();
      expect(useTimelineStore.getState().markers).toHaveLength(1);
      expect(useTimelineStore.getState().markers[0]).toEqual(marker);

      command.undo();
      expect(useTimelineStore.getState().markers).toHaveLength(0);
    });

    it('should have correct description', () => {
      const marker = { id: 'marker1', title: 'Chapter 1', time: 10 };
      const command = new AddMarkerCommand(marker, useTimelineStore);
      expect(command.getDescription()).toBe('Add marker "Chapter 1"');
    });
  });

  describe('ToggleRippleEditCommand', () => {
    it('should toggle ripple edit mode and revert on undo', () => {
      const command = new ToggleRippleEditCommand(useTimelineStore);

      expect(useTimelineStore.getState().rippleEditMode).toBe(false);

      command.execute();
      expect(useTimelineStore.getState().rippleEditMode).toBe(true);

      command.undo();
      expect(useTimelineStore.getState().rippleEditMode).toBe(false);
    });

    it('should have correct description', () => {
      const command = new ToggleRippleEditCommand(useTimelineStore);
      expect(command.getDescription()).toBe('Enable ripple edit');
    });
  });

  describe('Command timestamp', () => {
    it('should have timestamp', () => {
      const command = new SelectClipsCommand(['clip1'], useTimelineStore);
      expect(command.getTimestamp()).toBeInstanceOf(Date);
    });
  });
});
