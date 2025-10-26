/**
 * Timeline Engine - Frame-accurate timing and editing operations
 * Provides utilities for broadcast-quality video editing
 */

export const FRAME_RATE = 30; // Standard frame rate (can be configurable)
export const FRAME_DURATION = 1 / FRAME_RATE; // Duration of one frame in seconds

/**
 * Convert seconds to frame number
 */
export function secondsToFrames(seconds: number, frameRate: number = FRAME_RATE): number {
  return Math.round(seconds * frameRate);
}

/**
 * Convert frame number to seconds
 */
export function framesToSeconds(frames: number, frameRate: number = FRAME_RATE): number {
  return frames / frameRate;
}

/**
 * Snap time to nearest frame boundary
 */
export function snapToFrame(seconds: number, frameRate: number = FRAME_RATE): number {
  const frames = Math.round(seconds * frameRate);
  return frames / frameRate;
}

/**
 * Format time as timecode (HH:MM:SS:FF)
 */
export function formatTimecode(seconds: number, frameRate: number = FRAME_RATE): string {
  const totalFrames = Math.floor(seconds * frameRate);
  const frames = totalFrames % frameRate;
  const totalSeconds = Math.floor(totalFrames / frameRate);
  const secs = totalSeconds % 60;
  const totalMinutes = Math.floor(totalSeconds / 60);
  const mins = totalMinutes % 60;
  const hours = Math.floor(totalMinutes / 60);

  return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

/**
 * Format time as frame number
 */
export function formatFrameNumber(seconds: number, frameRate: number = FRAME_RATE): string {
  const frames = Math.floor(seconds * frameRate);
  return frames.toString();
}

/**
 * Format time as seconds
 */
export function formatSeconds(seconds: number): string {
  return seconds.toFixed(2) + 's';
}

/**
 * Snap point calculation for magnetic timeline
 */
export interface SnapPoint {
  time: number;
  type: 'clip-start' | 'clip-end' | 'playhead' | 'marker' | 'in-point' | 'out-point';
  clipId?: string;
  label?: string;
}

/**
 * Find nearest snap point within threshold
 */
export function findNearestSnapPoint(
  time: number,
  snapPoints: SnapPoint[],
  threshold: number = 0.1 // threshold in seconds
): SnapPoint | null {
  let nearest: SnapPoint | null = null;
  let minDistance = threshold;

  for (const point of snapPoints) {
    const distance = Math.abs(point.time - time);
    if (distance < minDistance) {
      minDistance = distance;
      nearest = point;
    }
  }

  return nearest;
}

/**
 * Calculate snap points from clips
 */
export function calculateSnapPoints(
  clips: Array<{ id: string; startTime: number; duration: number }>,
  playheadTime: number,
  markers: Array<{ time: number; label?: string }> = [],
  inPoint?: number,
  outPoint?: number
): SnapPoint[] {
  const points: SnapPoint[] = [];

  // Add clip boundaries
  clips.forEach((clip) => {
    points.push({
      time: clip.startTime,
      type: 'clip-start',
      clipId: clip.id,
    });
    points.push({
      time: clip.startTime + clip.duration,
      type: 'clip-end',
      clipId: clip.id,
    });
  });

  // Add playhead
  points.push({
    time: playheadTime,
    type: 'playhead',
  });

  // Add markers
  markers.forEach((marker) => {
    points.push({
      time: marker.time,
      type: 'marker',
      label: marker.label,
    });
  });

  // Add in/out points
  if (inPoint !== undefined) {
    points.push({
      time: inPoint,
      type: 'in-point',
    });
  }
  if (outPoint !== undefined) {
    points.push({
      time: outPoint,
      type: 'out-point',
    });
  }

  return points;
}

/**
 * Trim mode types for professional editing
 */
export enum TrimMode {
  RIPPLE = 'ripple', // Adjusts following clips
  ROLL = 'roll', // Adjusts adjacent clip boundaries
  SLIP = 'slip', // Changes in/out keeping duration
  SLIDE = 'slide', // Moves clip without changing in/out
}

/**
 * Selection mode types
 */
export enum SelectionMode {
  STANDARD = 'standard',
  RANGE = 'range',
  RIPPLE = 'ripple',
}

/**
 * Tool types
 */
export enum TimelineTool {
  SELECT = 'select',
  RAZOR = 'razor',
  HAND = 'hand',
}

/**
 * Display mode for timeline ruler
 */
export enum TimelineDisplayMode {
  TIMECODE = 'timecode',
  FRAMES = 'frames',
  SECONDS = 'seconds',
}

/**
 * Calculate zoom level for ruler tick marks
 */
export function calculateRulerInterval(
  zoom: number,
  displayMode: TimelineDisplayMode,
  frameRate: number = FRAME_RATE
): { majorInterval: number; minorInterval: number } {
  // zoom is pixels per second
  if (displayMode === TimelineDisplayMode.FRAMES) {
    const frameDuration = 1 / frameRate;
    if (zoom > 100) {
      return { majorInterval: frameDuration * 10, minorInterval: frameDuration };
    } else if (zoom > 50) {
      return { majorInterval: frameDuration * 30, minorInterval: frameDuration * 5 };
    } else {
      return { majorInterval: 1, minorInterval: frameDuration * 15 };
    }
  } else if (displayMode === TimelineDisplayMode.TIMECODE) {
    if (zoom > 100) {
      return { majorInterval: 1, minorInterval: 0.5 };
    } else if (zoom > 50) {
      return { majorInterval: 5, minorInterval: 1 };
    } else if (zoom > 20) {
      return { majorInterval: 10, minorInterval: 5 };
    } else {
      return { majorInterval: 60, minorInterval: 10 };
    }
  } else {
    // SECONDS mode
    if (zoom > 100) {
      return { majorInterval: 1, minorInterval: 0.1 };
    } else if (zoom > 50) {
      return { majorInterval: 5, minorInterval: 1 };
    } else {
      return { majorInterval: 10, minorInterval: 5 };
    }
  }
}

/**
 * Apply ripple edit - move all clips after the edit point
 */
export function applyRippleEdit<T extends { startTime: number }>(
  clips: T[],
  editTime: number,
  delta: number
): T[] {
  return clips.map((clip) => {
    if (clip.startTime > editTime) {
      return { ...clip, startTime: clip.startTime + delta };
    }
    return clip;
  });
}

/**
 * Check for gaps in timeline (for magnetic timeline)
 */
export function findGaps(
  clips: Array<{ startTime: number; duration: number }>,
  trackId?: string
): Array<{ start: number; end: number }> {
  if (clips.length === 0) return [];

  // Filter by track if specified
  const trackClips = trackId
    ? clips.filter((c) => 'trackId' in c && (c as { trackId: string }).trackId === trackId)
    : clips;

  // Sort by start time
  const sorted = [...trackClips].sort((a, b) => a.startTime - b.startTime);

  const gaps: Array<{ start: number; end: number }> = [];

  for (let i = 0; i < sorted.length - 1; i++) {
    const currentEnd = sorted[i].startTime + sorted[i].duration;
    const nextStart = sorted[i + 1].startTime;

    if (nextStart > currentEnd) {
      gaps.push({ start: currentEnd, end: nextStart });
    }
  }

  return gaps;
}

/**
 * Close gaps in timeline (magnetic timeline)
 */
export function closeGaps<T extends { startTime: number; duration: number }>(clips: T[]): T[] {
  if (clips.length === 0) return clips;

  const sorted = [...clips].sort((a, b) => a.startTime - b.startTime);
  const result: T[] = [];

  let currentTime = sorted[0].startTime;

  for (const clip of sorted) {
    if (clip.startTime > currentTime) {
      // Move clip to close gap
      result.push({ ...clip, startTime: currentTime });
      currentTime = currentTime + clip.duration;
    } else {
      result.push(clip);
      currentTime = Math.max(currentTime, clip.startTime + clip.duration);
    }
  }

  return result;
}
