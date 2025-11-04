/**
 * Command implementations for timeline operations with Zustand store
 * These commands integrate with the global undo/redo system
 */

import { Command, BatchCommandImpl } from '../services/commandHistory';
import { TimelineClip, useTimelineStore } from '../state/timeline';

/**
 * Command to select multiple clips
 */
export class SelectClipsCommand implements Command {
  private timestamp: Date;
  private previousSelection: string[];

  constructor(
    private clipIds: string[],
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    this.previousSelection = store.getState().selectedClipIds;
  }

  execute(): void {
    this.store.getState().setSelectedClipIds(this.clipIds);
  }

  undo(): void {
    this.store.getState().setSelectedClipIds(this.previousSelection);
  }

  getDescription(): string {
    return `Select ${this.clipIds.length} clips`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to delete multiple clips
 */
export class DeleteClipsCommand implements Command {
  private timestamp: Date;
  private deletedClips: Array<{ clip: TimelineClip; trackId: string; index: number }>;

  constructor(
    private clipIds: string[],
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    this.deletedClips = [];

    const state = store.getState();
    state.tracks.forEach((track) => {
      track.clips.forEach((clip, index) => {
        if (clipIds.includes(clip.id)) {
          this.deletedClips.push({ clip, trackId: track.id, index });
        }
      });
    });
  }

  execute(): void {
    this.store.getState().removeClips(this.clipIds);
  }

  undo(): void {
    const state = this.store.getState();
    this.deletedClips.sort((a, b) => a.index - b.index);

    this.deletedClips.forEach(({ clip, trackId }) => {
      state.addClip(trackId, clip);
    });
  }

  getDescription(): string {
    return `Delete ${this.clipIds.length} clips`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to ripple delete clips
 */
export class RippleDeleteClipsCommand implements Command {
  private timestamp: Date;
  private previousState: {
    clips: Array<{ clip: TimelineClip; trackId: string }>;
  };

  constructor(
    private clipIds: string[],
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    this.previousState = { clips: [] };

    const state = store.getState();
    state.tracks.forEach((track) => {
      track.clips.forEach((clip) => {
        this.previousState.clips.push({ clip: { ...clip }, trackId: track.id });
      });
    });
  }

  execute(): void {
    this.store.getState().rippleDeleteClips(this.clipIds);
  }

  undo(): void {
    const state = this.store.getState();
    const trackMap = new Map<string, TimelineClip[]>();

    state.tracks.forEach((track) => {
      trackMap.set(track.id, []);
    });

    this.previousState.clips.forEach(({ clip, trackId }) => {
      const clips = trackMap.get(trackId);
      if (clips) {
        clips.push(clip);
      }
    });

    trackMap.forEach((clips, trackId) => {
      clips.sort((a, b) => a.timelineStart - b.timelineStart);
      state.updateTrack(trackId, { clips });
    });
  }

  getDescription(): string {
    return `Ripple delete ${this.clipIds.length} clips`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to move a clip
 */
export class MoveClipCommand implements Command {
  private timestamp: Date;
  private previousClip: TimelineClip;

  constructor(
    private clipId: string,
    private newTimelineStart: number,
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    const state = store.getState();
    let foundClip: TimelineClip | undefined;
    state.tracks.forEach((track) => {
      const clip = track.clips.find((c) => c.id === clipId);
      if (clip) {
        foundClip = { ...clip };
      }
    });
    this.previousClip = foundClip!;
  }

  execute(): void {
    const state = this.store.getState();
    let clip: TimelineClip | undefined;
    state.tracks.forEach((track) => {
      const foundClip = track.clips.find((c) => c.id === this.clipId);
      if (foundClip) {
        clip = foundClip;
      }
    });

    if (clip) {
      state.updateClip({ ...clip, timelineStart: this.newTimelineStart });
    }
  }

  undo(): void {
    this.store.getState().updateClip(this.previousClip);
  }

  getDescription(): string {
    return 'Move clip';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to move multiple clips
 */
export class MoveClipsCommand implements Command {
  private timestamp: Date;
  private previousClips: TimelineClip[];
  private delta: number;

  constructor(
    private clipIds: string[],
    delta: number,
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    this.delta = delta;
    this.previousClips = [];

    const state = store.getState();
    state.tracks.forEach((track) => {
      track.clips.forEach((clip) => {
        if (clipIds.includes(clip.id)) {
          this.previousClips.push({ ...clip });
        }
      });
    });
  }

  execute(): void {
    const state = this.store.getState();
    this.clipIds.forEach((clipId) => {
      let foundClip: TimelineClip | undefined;
      state.tracks.forEach((track) => {
        const clip = track.clips.find((c) => c.id === clipId);
        if (clip) {
          foundClip = clip;
        }
      });

      if (foundClip) {
        state.updateClip({ ...foundClip, timelineStart: foundClip.timelineStart + this.delta });
      }
    });
  }

  undo(): void {
    const state = this.store.getState();
    this.previousClips.forEach((prevClip) => {
      state.updateClip(prevClip);
    });
  }

  getDescription(): string {
    return `Move ${this.clipIds.length} clips`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to split a clip
 */
export class SplitClipCommand implements Command {
  private timestamp: Date;
  private originalClip: TimelineClip;
  private trackId: string;
  private newClipId?: string;

  constructor(
    private clipId: string,
    private splitTime: number,
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
    const state = store.getState();
    let foundClip: TimelineClip | undefined;
    let foundTrackId = '';

    state.tracks.forEach((track) => {
      const clip = track.clips.find((c) => c.id === clipId);
      if (clip) {
        foundClip = { ...clip };
        foundTrackId = track.id;
      }
    });

    this.originalClip = foundClip!;
    this.trackId = foundTrackId;
  }

  execute(): void {
    const state = this.store.getState();
    const trackBefore = state.tracks.find((t) => t.id === this.trackId);
    const clipCountBefore = trackBefore?.clips.length || 0;

    state.splitClip(this.clipId, this.splitTime);

    const trackAfter = state.tracks.find((t) => t.id === this.trackId);
    const clipCountAfter = trackAfter?.clips.length || 0;

    if (clipCountAfter > clipCountBefore) {
      const newClips = trackAfter?.clips.filter(
        (c) => c.id !== this.clipId && c.timelineStart >= this.splitTime
      );
      if (newClips && newClips.length > 0) {
        this.newClipId = newClips[0].id;
      }
    }
  }

  undo(): void {
    const state = this.store.getState();

    if (this.newClipId) {
      state.removeClip(this.newClipId);
    }

    state.updateClip(this.originalClip);
  }

  getDescription(): string {
    return 'Split clip';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to add a marker
 */
export class AddMarkerCommand implements Command {
  private timestamp: Date;

  constructor(
    private marker: { id: string; title: string; time: number },
    private store: typeof useTimelineStore
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.store.getState().addMarker(this.marker);
  }

  undo(): void {
    this.store.getState().removeMarker(this.marker.id);
  }

  getDescription(): string {
    return `Add marker "${this.marker.title}"`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to toggle ripple edit mode
 */
export class ToggleRippleEditCommand implements Command {
  private timestamp: Date;
  private previousState: boolean;

  constructor(private store: typeof useTimelineStore) {
    this.timestamp = new Date();
    this.previousState = store.getState().rippleEditMode;
  }

  execute(): void {
    this.store.getState().setRippleEditMode(!this.previousState);
  }

  undo(): void {
    this.store.getState().setRippleEditMode(this.previousState);
  }

  getDescription(): string {
    return this.previousState ? 'Disable ripple edit' : 'Enable ripple edit';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Helper to create a grouped command for drag operations
 */
export function createDragOperationGroup(description: string): BatchCommandImpl {
  return new BatchCommandImpl(description);
}
