/**
 * Timeline editing service managing timeline state and operations
 */

import type { TimelineScene } from '../../types/timeline';

export interface TimelineOperation {
  type: 'splice' | 'delete' | 'ripple-delete' | 'update' | 'insert';
  sceneIndex?: number;
  scenes?: TimelineScene[];
  previousScenes?: TimelineScene[];
  timestamp: number;
}

export class TimelineEditor {
  private undoStack: TimelineOperation[] = [];
  private redoStack: TimelineOperation[] = [];
  private readonly maxUndoSteps = 50;

  /**
   * Splice (cut) a scene at the playhead position
   */
  public spliceAtPlayhead(
    scenes: TimelineScene[],
    sceneIndex: number,
    playheadTime: number
  ): TimelineScene[] {
    const scene = scenes[sceneIndex];
    if (!scene) return scenes;

    // Check if playhead is within scene bounds
    const sceneEnd = scene.start + scene.duration;
    if (playheadTime <= scene.start || playheadTime >= sceneEnd) {
      return scenes;
    }

    // Calculate split point relative to scene start
    const splitOffset = playheadTime - scene.start;

    // Create first scene (before split)
    const firstScene: TimelineScene = {
      ...scene,
      duration: splitOffset,
      script: scene.script.substring(0, Math.floor(scene.script.length * (splitOffset / scene.duration))),
    };

    // Create second scene (after split)
    const secondScene: TimelineScene = {
      ...scene,
      index: scene.index + 1,
      start: playheadTime,
      duration: scene.duration - splitOffset,
      script: scene.script.substring(Math.floor(scene.script.length * (splitOffset / scene.duration))),
      // Adjust visual assets timing
      visualAssets: scene.visualAssets.map(asset => ({
        ...asset,
        start: Math.max(0, asset.start - splitOffset),
      })).filter(asset => asset.start < (scene.duration - splitOffset)),
    };

    // Create new scene array
    const newScenes = [
      ...scenes.slice(0, sceneIndex),
      firstScene,
      secondScene,
      ...scenes.slice(sceneIndex + 1).map(s => ({ ...s, index: s.index + 1 })),
    ];

    // Record operation for undo
    this.recordOperation({
      type: 'splice',
      sceneIndex,
      scenes: newScenes,
      previousScenes: scenes,
      timestamp: Date.now(),
    });

    return newScenes;
  }

  /**
   * Ripple delete - removes scene and shifts all following scenes left
   */
  public rippleDelete(scenes: TimelineScene[], sceneIndex: number): TimelineScene[] {
    const scene = scenes[sceneIndex];
    if (!scene) return scenes;

    const deletedDuration = scene.duration;

    // Remove scene and shift following scenes
    const newScenes = [
      ...scenes.slice(0, sceneIndex),
      ...scenes.slice(sceneIndex + 1).map((s, idx) => ({
        ...s,
        index: sceneIndex + idx,
        start: s.start - deletedDuration,
      })),
    ];

    // Record operation for undo
    this.recordOperation({
      type: 'ripple-delete',
      sceneIndex,
      scenes: newScenes,
      previousScenes: scenes,
      timestamp: Date.now(),
    });

    return newScenes;
  }

  /**
   * Delete scene without ripple (leaves gap)
   */
  public deleteScene(scenes: TimelineScene[], sceneIndex: number): TimelineScene[] {
    if (sceneIndex < 0 || sceneIndex >= scenes.length) return scenes;

    const newScenes = [
      ...scenes.slice(0, sceneIndex),
      ...scenes.slice(sceneIndex + 1).map((s, idx) => ({
        ...s,
        index: sceneIndex + idx,
      })),
    ];

    // Record operation for undo
    this.recordOperation({
      type: 'delete',
      sceneIndex,
      scenes: newScenes,
      previousScenes: scenes,
      timestamp: Date.now(),
    });

    return newScenes;
  }

  /**
   * Close gaps in timeline by shifting scenes left
   */
  public closeGaps(scenes: TimelineScene[]): TimelineScene[] {
    const newScenes: TimelineScene[] = [];
    let currentTime = 0;

    for (const scene of scenes) {
      newScenes.push({
        ...scene,
        start: currentTime,
      });
      currentTime += scene.duration;
    }

    return newScenes;
  }

  /**
   * Undo last operation
   */
  public undo(): TimelineScene[] | null {
    const operation = this.undoStack.pop();
    if (!operation || !operation.previousScenes) return null;

    // Move to redo stack
    this.redoStack.push(operation);

    return operation.previousScenes;
  }

  /**
   * Redo last undone operation
   */
  public redo(): TimelineScene[] | null {
    const operation = this.redoStack.pop();
    if (!operation || !operation.scenes) return null;

    // Move back to undo stack
    this.undoStack.push(operation);

    return operation.scenes;
  }

  /**
   * Check if undo is available
   */
  public canUndo(): boolean {
    return this.undoStack.length > 0;
  }

  /**
   * Check if redo is available
   */
  public canRedo(): boolean {
    return this.redoStack.length > 0;
  }

  /**
   * Clear undo/redo stacks
   */
  public clearHistory(): void {
    this.undoStack = [];
    this.redoStack = [];
  }

  /**
   * Record an operation for undo/redo
   */
  private recordOperation(operation: TimelineOperation): void {
    this.undoStack.push(operation);

    // Limit stack size
    if (this.undoStack.length > this.maxUndoSteps) {
      this.undoStack.shift();
    }

    // Clear redo stack when new operation is recorded
    this.redoStack = [];
  }
}

// Singleton instance
export const timelineEditor = new TimelineEditor();
