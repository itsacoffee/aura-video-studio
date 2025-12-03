/**
 * OpenCut Types
 *
 * Type definitions for OpenCut editor including markers, tracks, and clips.
 */

/** Marker color options for visual identification */
export type MarkerColor =
  | 'blue'
  | 'green'
  | 'orange'
  | 'purple'
  | 'red'
  | 'yellow'
  | 'pink'
  | 'cyan';

/** Marker type classifications */
export type MarkerType = 'standard' | 'chapter' | 'todo' | 'beat';

/** Timeline marker for annotating specific points in the video */
export interface Marker {
  /** Unique identifier for the marker */
  id: string;
  /** Time position in seconds on the timeline */
  time: number;
  /** Type of marker (determines default behavior and appearance) */
  type: MarkerType;
  /** Color for visual identification */
  color: MarkerColor;
  /** Display name for the marker */
  name: string;
  /** Optional notes or description */
  notes?: string;
  /** Optional duration for range markers (in seconds) */
  duration?: number;
  /** Completion state for todo markers */
  completed?: boolean;
}
