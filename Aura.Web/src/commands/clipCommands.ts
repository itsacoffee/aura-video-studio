/**
 * Command implementations for editor operations
 */

import { Command } from '../services/commandHistory';
import { TimelineClip } from '../pages/VideoEditorPage';
import { AppliedEffect } from '../types/effects';

/**
 * Command to add a clip to the timeline
 */
export class AddClipCommand implements Command {
  private timestamp: Date;

  constructor(
    private clip: TimelineClip,
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.setClips((prevClips) => [...prevClips, this.clip]);
  }

  undo(): void {
    this.setClips((prevClips) => prevClips.filter((c) => c.id !== this.clip.id));
  }

  getDescription(): string {
    return `Add ${this.clip.label || this.clip.type}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to delete a clip from the timeline
 */
export class DeleteClipCommand implements Command {
  private timestamp: Date;
  private clipIndex: number;

  constructor(
    private clipId: string,
    private clips: TimelineClip[],
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
    this.clipIndex = clips.findIndex((c) => c.id === clipId);
  }

  execute(): void {
    this.setClips((prevClips) => prevClips.filter((c) => c.id !== this.clipId));
  }

  undo(): void {
    const deletedClip = this.clips[this.clipIndex];
    if (deletedClip) {
      this.setClips((prevClips) => {
        const newClips = [...prevClips];
        newClips.splice(this.clipIndex, 0, deletedClip);
        return newClips;
      });
    }
  }

  getDescription(): string {
    const clip = this.clips.find((c) => c.id === this.clipId);
    return `Delete ${clip?.label || 'clip'}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to move a clip in the timeline
 */
export class MoveClipCommand implements Command {
  private timestamp: Date;
  private previousStartTime: number;
  private previousTrackId: string;

  constructor(
    private clipId: string,
    private newStartTime: number,
    private newTrackId: string,
    clips: TimelineClip[],
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
    const clip = clips.find((c) => c.id === clipId);
    this.previousStartTime = clip?.startTime || 0;
    this.previousTrackId = clip?.trackId || '';
  }

  execute(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, startTime: this.newStartTime, trackId: this.newTrackId }
          : clip
      )
    );
  }

  undo(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, startTime: this.previousStartTime, trackId: this.previousTrackId }
          : clip
      )
    );
  }

  getDescription(): string {
    return 'Move clip';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to trim a clip
 */
export class TrimClipCommand implements Command {
  private timestamp: Date;
  private previousStartTime: number;
  private previousDuration: number;

  constructor(
    private clipId: string,
    private newStartTime: number,
    private newDuration: number,
    clips: TimelineClip[],
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
    const clip = clips.find((c) => c.id === clipId);
    this.previousStartTime = clip?.startTime || 0;
    this.previousDuration = clip?.duration || 0;
  }

  execute(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, startTime: this.newStartTime, duration: this.newDuration }
          : clip
      )
    );
  }

  undo(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, startTime: this.previousStartTime, duration: this.previousDuration }
          : clip
      )
    );
  }

  getDescription(): string {
    return 'Trim clip';
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to add an effect to a clip
 */
export class AddEffectCommand implements Command {
  private timestamp: Date;

  constructor(
    private clipId: string,
    private effect: AppliedEffect,
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
  }

  execute(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, effects: [...(clip.effects || []), this.effect] }
          : clip
      )
    );
  }

  undo(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, effects: (clip.effects || []).filter((e) => e.id !== this.effect.id) }
          : clip
      )
    );
  }

  getDescription(): string {
    return `Add ${this.effect.effectType} effect`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to remove an effect from a clip
 */
export class RemoveEffectCommand implements Command {
  private timestamp: Date;
  private effectIndex: number;
  private clips: TimelineClip[];

  constructor(
    private clipId: string,
    private effectId: string,
    clips: TimelineClip[],
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
    this.clips = clips;
    const clip = clips.find((c) => c.id === clipId);
    this.effectIndex = (clip?.effects || []).findIndex((e) => e.id === effectId);
  }

  execute(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId
          ? { ...clip, effects: (clip.effects || []).filter((e) => e.id !== this.effectId) }
          : clip
      )
    );
  }

  undo(): void {
    const clip = this.clips.find((c) => c.id === this.clipId);
    const removedEffect = clip?.effects?.[this.effectIndex];

    if (removedEffect) {
      this.setClips((prevClips) =>
        prevClips.map((c) => {
          if (c.id === this.clipId) {
            const newEffects = [...(c.effects || [])];
            newEffects.splice(this.effectIndex, 0, removedEffect);
            return { ...c, effects: newEffects };
          }
          return c;
        })
      );
    }
  }

  getDescription(): string {
    const clip = this.clips.find((c) => c.id === this.clipId);
    const effect = clip?.effects?.find((e) => e.id === this.effectId);
    return `Remove ${effect?.effectType || 'effect'}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}

/**
 * Command to update clip properties
 */
export class UpdatePropertyCommand implements Command {
  private timestamp: Date;
  private previousValue: Partial<TimelineClip>;

  constructor(
    private clipId: string,
    private updates: Partial<TimelineClip>,
    clips: TimelineClip[],
    private setClips: (clips: TimelineClip[] | ((prev: TimelineClip[]) => TimelineClip[])) => void
  ) {
    this.timestamp = new Date();
    const clip = clips.find((c) => c.id === clipId);
    
    // Store only the properties being updated
    this.previousValue = {};
    if (clip) {
      Object.keys(updates).forEach((key) => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (this.previousValue as any)[key] = (clip as any)[key];
      });
    }
  }

  execute(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId ? { ...clip, ...this.updates } : clip
      )
    );
  }

  undo(): void {
    this.setClips((prevClips) =>
      prevClips.map((clip) =>
        clip.id === this.clipId ? { ...clip, ...this.previousValue } : clip
      )
    );
  }

  getDescription(): string {
    const propertyNames = Object.keys(this.updates).join(', ');
    return `Update ${propertyNames}`;
  }

  getTimestamp(): Date {
    return this.timestamp;
  }
}
