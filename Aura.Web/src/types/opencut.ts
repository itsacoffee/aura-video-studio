/**
 * OpenCut Shared Types
 *
 * Comprehensive TypeScript types for the OpenCut video editor.
 * Defines types for transitions, effects, keyframes, markers, captions,
 * templates, export settings, and aspect ratio presets.
 */

// ============================================================================
// Transition Types
// ============================================================================

/**
 * Categories for transitions in the editor.
 */
export type TransitionCategory = 'basic' | 'dissolve' | 'wipe' | 'slide' | 'zoom' | '3d' | 'custom';

/**
 * Parameter type for transition/effect parameters.
 */
export type ParameterType = 'number' | 'string' | 'boolean' | 'color' | 'select' | 'range';

/**
 * Parameter definition for transitions and effects.
 */
export interface TransitionParameter {
  /** Unique parameter identifier */
  id: string;
  /** Display name */
  name: string;
  /** Parameter type */
  type: ParameterType;
  /** Default value */
  defaultValue: string | number | boolean;
  /** Minimum value (for number/range) */
  min?: number;
  /** Maximum value (for number/range) */
  max?: number;
  /** Step increment (for number/range) */
  step?: number;
  /** Options (for select type) */
  options?: string[];
  /** Unit display (e.g., 'px', '%', 's') */
  unit?: string;
}

/**
 * Definition of a transition type available in the editor.
 */
export interface TransitionDefinition {
  /** Unique transition identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Category for grouping */
  category: TransitionCategory;
  /** Thumbnail preview URL */
  thumbnailUrl?: string;
  /** Default duration in seconds */
  defaultDuration: number;
  /** Configurable parameters */
  parameters: TransitionParameter[];
  /** Whether this is a premium transition */
  isPremium?: boolean;
}

/**
 * Applied transition between two clips.
 */
export interface AppliedTransition {
  /** Unique instance identifier */
  id: string;
  /** Reference to transition definition */
  transitionId: string;
  /** Duration in seconds */
  duration: number;
  /** Parameter values */
  params: Record<string, string | number | boolean>;
  /** Clip ID this transition leads into */
  toClipId: string;
  /** Clip ID this transition comes from */
  fromClipId: string;
}

// ============================================================================
// Effect Types
// ============================================================================

/**
 * Categories for effects in the editor.
 */
export type EffectCategory =
  | 'color'
  | 'blur'
  | 'distort'
  | 'stylize'
  | 'keying'
  | 'motion'
  | 'audio'
  | 'custom';

/**
 * Parameter definition for effects.
 */
export interface EffectParameter {
  /** Unique parameter identifier */
  id: string;
  /** Display name */
  name: string;
  /** Parameter type */
  type: ParameterType;
  /** Default value */
  defaultValue: string | number | boolean;
  /** Minimum value (for number/range) */
  min?: number;
  /** Maximum value (for number/range) */
  max?: number;
  /** Step increment (for number/range) */
  step?: number;
  /** Options (for select type) */
  options?: string[];
  /** Unit display */
  unit?: string;
  /** Whether parameter is keyframeable */
  keyframeable?: boolean;
}

/**
 * Definition of an effect type available in the editor.
 */
export interface EffectDefinition {
  /** Unique effect identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Category for grouping */
  category: EffectCategory;
  /** Thumbnail preview URL */
  thumbnailUrl?: string;
  /** Configurable parameters */
  parameters: EffectParameter[];
  /** Whether this is a premium effect */
  isPremium?: boolean;
  /** GPU acceleration required */
  requiresGpu?: boolean;
}

/**
 * Applied effect on a clip.
 */
export interface AppliedEffect {
  /** Unique instance identifier */
  id: string;
  /** Reference to effect definition */
  effectId: string;
  /** Whether effect is enabled */
  enabled: boolean;
  /** Parameter values */
  params: Record<string, string | number | boolean>;
  /** Keyframe tracks for animated parameters */
  keyframeTracks?: KeyframeTrack[];
}

// ============================================================================
// Keyframe Types
// ============================================================================

/**
 * Easing types for keyframe interpolation.
 */
export type EasingType =
  | 'linear'
  | 'ease-in'
  | 'ease-out'
  | 'ease-in-out'
  | 'ease-in-quad'
  | 'ease-out-quad'
  | 'ease-in-out-quad'
  | 'ease-in-cubic'
  | 'ease-out-cubic'
  | 'ease-in-out-cubic'
  | 'ease-in-expo'
  | 'ease-out-expo'
  | 'ease-in-out-expo'
  | 'ease-in-back'
  | 'ease-out-back'
  | 'ease-in-out-back'
  | 'bezier';

/**
 * Bezier curve handles for custom easing.
 */
export interface BezierHandles {
  /** Control point 1 x (0-1) */
  x1: number;
  /** Control point 1 y (can be outside 0-1 for overshoot) */
  y1: number;
  /** Control point 2 x (0-1) */
  x2: number;
  /** Control point 2 y (can be outside 0-1 for overshoot) */
  y2: number;
}

/**
 * Single keyframe in a keyframe track.
 */
export interface Keyframe {
  /** Unique keyframe identifier */
  id: string;
  /** Time in seconds relative to clip start */
  time: number;
  /** Value at this keyframe */
  value: number | string | boolean;
  /** Easing type for interpolation to next keyframe */
  easing: EasingType;
  /** Custom bezier handles (when easing is 'bezier') */
  bezierHandles?: BezierHandles;
  /** Whether this keyframe is selected */
  selected?: boolean;
}

/**
 * Track of keyframes for a single property.
 */
export interface KeyframeTrack {
  /** Unique track identifier */
  id: string;
  /** Parameter ID this track animates */
  parameterId: string;
  /** Display name */
  name: string;
  /** Keyframes in this track */
  keyframes: Keyframe[];
  /** Whether track is muted */
  muted?: boolean;
  /** Whether track is locked */
  locked?: boolean;
  /** Whether track is expanded in UI */
  expanded?: boolean;
}

// ============================================================================
// Marker Types
// ============================================================================

/**
 * Types of markers in the timeline.
 */
export type MarkerType = 'standard' | 'chapter' | 'todo' | 'beat';

/**
 * Marker color presets.
 */
export type MarkerColor = 'red' | 'orange' | 'yellow' | 'green' | 'blue' | 'purple' | 'pink';

/**
 * Marker in the timeline.
 */
export interface Marker {
  /** Unique marker identifier */
  id: string;
  /** Time position in seconds */
  time: number;
  /** Marker type */
  type: MarkerType;
  /** Display name/label */
  name: string;
  /** Optional notes/description */
  notes?: string;
  /** Marker color */
  color: MarkerColor;
  /** Duration in seconds (for range markers) */
  duration?: number;
  /** Completion state (for todo markers) */
  completed?: boolean;
}

// ============================================================================
// Caption Types
// ============================================================================

/**
 * Text alignment options.
 */
export type TextAlign = 'left' | 'center' | 'right';

/**
 * Vertical position options.
 */
export type VerticalPosition = 'top' | 'middle' | 'bottom';

/**
 * Caption animation types.
 */
export type CaptionAnimationType =
  | 'none'
  | 'fade'
  | 'slide-up'
  | 'slide-down'
  | 'pop'
  | 'typewriter'
  | 'word-by-word'
  | 'highlight';

/**
 * Style definition for captions.
 */
export interface CaptionStyle {
  /** Font family */
  fontFamily: string;
  /** Font size in pixels */
  fontSize: number;
  /** Font weight */
  fontWeight: number;
  /** Text color (hex) */
  color: string;
  /** Text alignment */
  textAlign: TextAlign;
  /** Background color (hex, with alpha) */
  backgroundColor?: string;
  /** Background padding in pixels */
  backgroundPadding?: number;
  /** Stroke/outline color */
  strokeColor?: string;
  /** Stroke width in pixels */
  strokeWidth?: number;
  /** Drop shadow */
  shadow?: {
    color: string;
    blur: number;
    offsetX: number;
    offsetY: number;
  };
  /** Letter spacing */
  letterSpacing?: number;
  /** Line height */
  lineHeight?: number;
}

/**
 * Caption animation configuration.
 */
export interface CaptionAnimation {
  /** Animation type */
  type: CaptionAnimationType;
  /** Animation duration in seconds */
  duration: number;
  /** Animation easing */
  easing: EasingType;
}

/**
 * Single caption entry.
 */
export interface Caption {
  /** Unique caption identifier */
  id: string;
  /** Start time in seconds */
  startTime: number;
  /** End time in seconds */
  endTime: number;
  /** Caption text */
  text: string;
  /** Style overrides (inherits from track style) */
  style?: Partial<CaptionStyle>;
  /** Enter animation */
  enterAnimation?: CaptionAnimation;
  /** Exit animation */
  exitAnimation?: CaptionAnimation;
  /** Speaker/character name */
  speaker?: string;
}

/**
 * Caption track in the timeline.
 */
export interface CaptionTrack {
  /** Unique track identifier */
  id: string;
  /** Display name */
  name: string;
  /** Language code (e.g., 'en-US') */
  language: string;
  /** Default style for captions in this track */
  defaultStyle: CaptionStyle;
  /** Vertical position */
  position: VerticalPosition;
  /** Margin from edge in pixels */
  margin: number;
  /** Captions in this track */
  captions: Caption[];
  /** Whether track is visible */
  visible: boolean;
  /** Whether track is locked */
  locked: boolean;
}

// ============================================================================
// Template Types
// ============================================================================

/**
 * Template category.
 */
export type TemplateCategory =
  | 'social-media'
  | 'business'
  | 'education'
  | 'entertainment'
  | 'personal'
  | 'custom';

/**
 * Template data for preset video configurations.
 */
export interface TemplateData {
  /** Timeline tracks configuration */
  tracks: {
    type: 'video' | 'audio' | 'image' | 'text';
    name: string;
  }[];
  /** Placeholder clips */
  clips: {
    trackIndex: number;
    startTime: number;
    duration: number;
    name: string;
    type: 'video' | 'audio' | 'image' | 'text';
    isPlaceholder: boolean;
  }[];
  /** Default project settings */
  settings: {
    resolution: { width: number; height: number };
    frameRate: number;
    aspectRatio: string;
  };
}

/**
 * Template definition.
 */
export interface Template {
  /** Unique template identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Category */
  category: TemplateCategory;
  /** Thumbnail URL */
  thumbnailUrl: string;
  /** Preview video URL */
  previewUrl?: string;
  /** Template data */
  data: TemplateData;
  /** Whether this is a premium template */
  isPremium: boolean;
  /** Tags for searching */
  tags: string[];
  /** Estimated duration in seconds */
  estimatedDuration: number;
}

// ============================================================================
// Export Types
// ============================================================================

/**
 * Video codec options.
 */
export type VideoCodec = 'h264' | 'h265' | 'vp9' | 'av1' | 'prores';

/**
 * Audio codec options.
 */
export type AudioCodec = 'aac' | 'mp3' | 'opus' | 'flac' | 'pcm';

/**
 * Export quality preset.
 */
export type QualityPreset = 'draft' | 'low' | 'medium' | 'high' | 'ultra' | 'custom';

/**
 * Export preset definition.
 */
export interface ExportPreset {
  /** Unique preset identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Target platform/use case */
  platform?: string;
  /** Export settings */
  settings: ExportSettings;
}

/**
 * Detailed export settings.
 */
export interface ExportSettings {
  /** Output format/container */
  format: 'mp4' | 'webm' | 'mov' | 'mkv' | 'gif';
  /** Video codec */
  videoCodec: VideoCodec;
  /** Audio codec */
  audioCodec: AudioCodec;
  /** Output resolution */
  resolution: {
    width: number;
    height: number;
  };
  /** Frame rate */
  frameRate: number;
  /** Video bitrate in kbps */
  videoBitrate: number;
  /** Audio bitrate in kbps */
  audioBitrate: number;
  /** Quality preset */
  qualityPreset: QualityPreset;
  /** Use hardware acceleration */
  useHardwareAcceleration: boolean;
  /** Two-pass encoding */
  twoPass: boolean;
  /** Include audio */
  includeAudio: boolean;
  /** Burn in captions */
  burnCaptions: boolean;
  /** Caption track ID to burn (if burnCaptions is true) */
  captionTrackId?: string;
}

// ============================================================================
// Aspect Ratio Types
// ============================================================================

/**
 * Aspect ratio preset definition.
 */
export interface AspectRatioPreset {
  /** Unique preset identifier */
  id: string;
  /** Display name */
  name: string;
  /** Aspect ratio string (e.g., '16:9') */
  ratio: string;
  /** Width value for ratio calculation */
  width: number;
  /** Height value for ratio calculation */
  height: number;
  /** Platform/use case description */
  description: string;
  /** Common resolutions for this ratio */
  commonResolutions: { width: number; height: number; label: string }[];
}

/**
 * Standard aspect ratio presets.
 */
export const ASPECT_RATIO_PRESETS: AspectRatioPreset[] = [
  {
    id: 'landscape-16-9',
    name: 'Landscape 16:9',
    ratio: '16:9',
    width: 16,
    height: 9,
    description: 'YouTube, TV, most video content',
    commonResolutions: [
      { width: 1920, height: 1080, label: '1080p HD' },
      { width: 2560, height: 1440, label: '1440p QHD' },
      { width: 3840, height: 2160, label: '4K UHD' },
      { width: 1280, height: 720, label: '720p HD' },
    ],
  },
  {
    id: 'portrait-9-16',
    name: 'Portrait 9:16',
    ratio: '9:16',
    width: 9,
    height: 16,
    description: 'TikTok, Instagram Reels, YouTube Shorts',
    commonResolutions: [
      { width: 1080, height: 1920, label: '1080x1920' },
      { width: 720, height: 1280, label: '720x1280' },
    ],
  },
  {
    id: 'square-1-1',
    name: 'Square 1:1',
    ratio: '1:1',
    width: 1,
    height: 1,
    description: 'Instagram posts, social media',
    commonResolutions: [
      { width: 1080, height: 1080, label: '1080x1080' },
      { width: 720, height: 720, label: '720x720' },
    ],
  },
  {
    id: 'cinema-21-9',
    name: 'Cinematic 21:9',
    ratio: '21:9',
    width: 21,
    height: 9,
    description: 'Ultrawide, cinematic letterbox',
    commonResolutions: [
      { width: 2560, height: 1080, label: '2560x1080' },
      { width: 3440, height: 1440, label: '3440x1440' },
    ],
  },
  {
    id: 'classic-4-3',
    name: 'Classic 4:3',
    ratio: '4:3',
    width: 4,
    height: 3,
    description: 'Classic TV, presentations',
    commonResolutions: [
      { width: 1440, height: 1080, label: '1440x1080' },
      { width: 1024, height: 768, label: '1024x768' },
    ],
  },
  {
    id: 'instagram-story-4-5',
    name: 'Instagram 4:5',
    ratio: '4:5',
    width: 4,
    height: 5,
    description: 'Instagram posts (portrait)',
    commonResolutions: [
      { width: 1080, height: 1350, label: '1080x1350' },
      { width: 864, height: 1080, label: '864x1080' },
    ],
  },
  {
    id: 'twitter-16-9',
    name: 'Twitter/X 16:9',
    ratio: '16:9',
    width: 16,
    height: 9,
    description: 'Twitter/X video posts',
    commonResolutions: [
      { width: 1280, height: 720, label: '1280x720' },
      { width: 1920, height: 1080, label: '1920x1080' },
    ],
  },
  {
    id: 'linkedin-1-1',
    name: 'LinkedIn 1:1',
    ratio: '1:1',
    width: 1,
    height: 1,
    description: 'LinkedIn video posts',
    commonResolutions: [{ width: 1080, height: 1080, label: '1080x1080' }],
  },
];

// ============================================================================
// Utility Types
// ============================================================================

/**
 * Generic ID type for entities.
 */
export type EntityId = string;

/**
 * Time range type.
 */
export interface TimeRange {
  /** Start time in seconds */
  start: number;
  /** End time in seconds */
  end: number;
}

/**
 * Point in 2D space.
 */
export interface Point2D {
  x: number;
  y: number;
}

/**
 * Size dimensions.
 */
export interface Size {
  width: number;
  height: number;
}

/**
 * Rectangle bounds.
 */
export interface Bounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

// ============================================================================
// Track Types
// ============================================================================

/**
 * Type for track type in the timeline editor.
 */
export type TrackType = 'video' | 'audio' | 'image' | 'text' | 'effect';

/**
 * Track type color mapping.
 * These colors are defined in designTokens.ts and mapped here for type safety.
 */
export const TRACK_TYPE_COLORS: Record<TrackType, string> = {
  video: '#3B82F6',
  audio: '#22C55E',
  image: '#A855F7',
  text: '#F59E0B',
  effect: '#EC4899',
};

/**
 * Get the color for a specific track type.
 * @param type - The track type
 * @returns The hex color for the track type
 */
export function getTrackColor(type: TrackType): string {
  return TRACK_TYPE_COLORS[type];
}
