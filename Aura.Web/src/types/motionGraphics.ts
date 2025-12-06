/**
 * Motion Graphics Type System
 *
 * Comprehensive TypeScript types for broadcast-quality motion graphics
 * including lower thirds, callouts, social media elements, animated titles,
 * kinetic typography, and shape animations.
 */

/** Categories of motion graphics */
export type GraphicCategory =
  | 'lower-thirds'
  | 'callouts'
  | 'social'
  | 'titles'
  | 'kinetic-text'
  | 'shapes'
  | 'overlays'
  | 'transitions'
  | 'badges'
  | 'counters';

/** Animation style types */
export type AnimationStyle =
  | 'slide'
  | 'fade'
  | 'scale'
  | 'reveal'
  | 'blur'
  | 'bounce'
  | 'elastic'
  | 'spring'
  | 'glitch'
  | 'typewriter'
  | 'wave'
  | 'split'
  | 'morph';

/** Easing presets including spring physics */
export type EasingPreset =
  // Standard easings
  | 'linear'
  | 'easeIn'
  | 'easeOut'
  | 'easeInOut'
  | 'easeInQuad'
  | 'easeOutQuad'
  | 'easeInOutQuad'
  | 'easeInCubic'
  | 'easeOutCubic'
  | 'easeInOutCubic'
  | 'easeInQuart'
  | 'easeOutQuart'
  | 'easeInOutQuart'
  | 'easeInQuint'
  | 'easeOutQuint'
  | 'easeInOutQuint'
  | 'easeInExpo'
  | 'easeOutExpo'
  | 'easeInOutExpo'
  | 'easeInCirc'
  | 'easeOutCirc'
  | 'easeInOutCirc'
  | 'easeInBack'
  | 'easeOutBack'
  | 'easeInOutBack'
  | 'easeInElastic'
  | 'easeOutElastic'
  | 'easeInOutElastic'
  | 'easeInBounce'
  | 'easeOutBounce'
  | 'easeInOutBounce'
  // Spring physics
  | 'springGentle'
  | 'springBouncy'
  | 'springStiff';

/** Aspect ratio presets for responsive graphics */
export type AspectRatioPreset = '16:9' | '9:16' | '1:1' | '4:5' | '21:9' | 'custom';

/** Animation direction options */
export type AnimationDirection = 'left' | 'right' | 'up' | 'down' | 'center';

/** Blend mode options for layers */
export type BlendMode =
  | 'normal'
  | 'multiply'
  | 'screen'
  | 'overlay'
  | 'darken'
  | 'lighten'
  | 'color-dodge'
  | 'color-burn'
  | 'hard-light'
  | 'soft-light'
  | 'difference'
  | 'exclusion';

/** Color scheme options */
export type ColorScheme = 'dark' | 'light' | 'auto';

/** Keyframe for animation property */
export interface AnimationKeyframe {
  /** Time in seconds */
  time: number;
  /** Value at this keyframe */
  value: number | string;
  /** Easing to next keyframe */
  easing?: EasingPreset;
}

/** Animatable property definition */
export interface AnimationProperty {
  /** Property name (e.g., 'opacity', 'x', 'scale') */
  name: string;
  /** Keyframes for this property */
  keyframes: AnimationKeyframe[];
}

/** Animation sequence containing multiple properties */
export interface AnimationSequence {
  /** Unique identifier */
  id: string;
  /** Display name */
  name: string;
  /** Animation properties */
  properties: AnimationProperty[];
  /** Total duration in seconds */
  duration: number;
  /** Loop count (0 = no loop, -1 = infinite) */
  loop?: number;
}

/** Entry/exit animation configuration */
export interface EntryExitAnimation {
  /** Animation style */
  style: AnimationStyle;
  /** Duration in seconds */
  duration: number;
  /** Easing preset */
  easing: EasingPreset;
  /** Animation direction */
  direction?: AnimationDirection;
  /** Stagger delay for multi-element animations */
  stagger?: number;
  /** Blur amount for blur animations */
  blur?: number;
  /** Scale factor for scale animations */
  scale?: number;
  /** Rotation angle in degrees */
  rotation?: number;
}

/** Transform properties for a layer */
export interface LayerTransform {
  /** X position as percentage (0-100) or pixels */
  x: number;
  /** Y position as percentage (0-100) or pixels */
  y: number;
  /** Scale X (1 = 100%) */
  scaleX: number;
  /** Scale Y (1 = 100%) */
  scaleY: number;
  /** Rotation in degrees */
  rotation: number;
  /** Anchor X as percentage (0-100) */
  anchorX: number;
  /** Anchor Y as percentage (0-100) */
  anchorY: number;
  /** Opacity (0-1) */
  opacity: number;
}

/** Graphic layer definition */
export interface GraphicLayer {
  /** Unique layer identifier */
  id: string;
  /** Layer type */
  type: 'text' | 'shape' | 'image' | 'group';
  /** Layer name */
  name: string;
  /** Transform properties */
  transform: LayerTransform;
  /** Blend mode */
  blendMode: BlendMode;
  /** Animation sequences */
  animations: AnimationSequence[];
  /** Entry animation */
  entryAnimation?: EntryExitAnimation;
  /** Exit animation */
  exitAnimation?: EntryExitAnimation;
  /** Child layers for groups */
  children?: GraphicLayer[];
  /** Text layer properties (if type is 'text') */
  textProperties?: TextLayerProperties;
  /** Shape layer properties (if type is 'shape') */
  shapeProperties?: ShapeLayerProperties;
}

/** Text layer styling properties */
export interface TextLayerProperties {
  /** Text content */
  content: string;
  /** Font family */
  fontFamily: string;
  /** Font size in pixels */
  fontSize: number;
  /** Font weight */
  fontWeight: number;
  /** Font style */
  fontStyle: 'normal' | 'italic';
  /** Text color */
  color: string;
  /** Text alignment */
  textAlign: 'left' | 'center' | 'right';
  /** Line height */
  lineHeight: number;
  /** Letter spacing in pixels */
  letterSpacing: number;
  /** Text shadow */
  shadow?: {
    color: string;
    offsetX: number;
    offsetY: number;
    blur: number;
  };
  /** Text stroke */
  stroke?: {
    color: string;
    width: number;
  };
  /** Text glow effect */
  glow?: {
    color: string;
    blur: number;
    strength: number;
  };
  /** Gradient fill */
  gradient?: GradientDefinition;
}

/** Shape types for shape layers */
export type ShapeType = 'rectangle' | 'ellipse' | 'line' | 'polygon' | 'path';

/** Shape layer properties */
export interface ShapeLayerProperties {
  /** Shape type */
  shape: ShapeType;
  /** Width */
  width: number;
  /** Height */
  height: number;
  /** Fill color */
  fill?: string;
  /** Fill gradient */
  fillGradient?: GradientDefinition;
  /** Stroke color */
  strokeColor?: string;
  /** Stroke width */
  strokeWidth?: number;
  /** Stroke dash array */
  strokeDashArray?: number[];
  /** Corner radius for rectangles */
  cornerRadius?: number;
  /** SVG path data for custom paths */
  pathData?: string;
  /** Number of sides for polygons */
  sides?: number;
}

/** Gradient types */
export type GradientType = 'linear' | 'radial' | 'conic';

/** Gradient color stop */
export interface GradientStop {
  /** Color value */
  color: string;
  /** Position (0-1) */
  position: number;
}

/** Gradient definition */
export interface GradientDefinition {
  /** Gradient type */
  type: GradientType;
  /** Color stops */
  stops: GradientStop[];
  /** Angle in degrees (for linear gradients) */
  angle?: number;
  /** Center X (for radial/conic gradients) */
  centerX?: number;
  /** Center Y (for radial/conic gradients) */
  centerY?: number;
}

/** Text customization field definition */
export interface TextCustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Layer ID this field maps to */
  layerId: string;
  /** Default value */
  defaultValue: string;
  /** Maximum character count */
  maxLength?: number;
  /** Placeholder text */
  placeholder?: string;
}

/** Color customization field definition */
export interface ColorCustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Layer IDs this color applies to */
  layerIds: string[];
  /** Property name (e.g., 'color', 'fill', 'stroke') */
  property: string;
  /** Default color value */
  defaultValue: string;
  /** Preset color swatches */
  presets?: string[];
  /** Allow gradient */
  allowGradient?: boolean;
}

/** Layout customization field definition */
export interface LayoutCustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Property type */
  property: 'position' | 'size' | 'margin' | 'padding';
  /** Default value */
  defaultValue: number;
  /** Minimum value */
  min?: number;
  /** Maximum value */
  max?: number;
  /** Step value */
  step?: number;
  /** Unit (px, %, etc.) */
  unit?: string;
}

/** Animation customization field definition */
export interface AnimationCustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Property type */
  property: 'duration' | 'delay' | 'easing' | 'style';
  /** Default value */
  defaultValue: number | string;
  /** Minimum value (for numbers) */
  min?: number;
  /** Maximum value (for numbers) */
  max?: number;
  /** Options (for selects) */
  options?: Array<{ label: string; value: string }>;
}

/** Advanced customization field definition */
export interface AdvancedCustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Field type */
  type: 'slider' | 'toggle' | 'select' | 'number';
  /** Default value */
  defaultValue: number | boolean | string;
  /** Minimum value */
  min?: number;
  /** Maximum value */
  max?: number;
  /** Step value */
  step?: number;
  /** Options for select fields */
  options?: Array<{ label: string; value: string }>;
}

/** Complete customization schema for a graphic */
export interface CustomizationSchema {
  /** Text customization fields */
  text: TextCustomizationField[];
  /** Color customization fields */
  colors: ColorCustomizationField[];
  /** Layout customization fields */
  layout: LayoutCustomizationField[];
  /** Animation customization fields */
  animation: AnimationCustomizationField[];
  /** Advanced customization fields */
  advanced: AdvancedCustomizationField[];
}

/** Responsive breakpoint definition */
export interface ResponsiveBreakpoint {
  /** Aspect ratio this breakpoint applies to */
  aspectRatio: AspectRatioPreset;
  /** Layer transform overrides */
  transforms?: Record<string, Partial<LayerTransform>>;
  /** Visibility overrides for layers */
  visibility?: Record<string, boolean>;
  /** Font size scale factor */
  fontScale?: number;
}

/** Motion graphic asset definition */
export interface MotionGraphicAsset {
  /** Unique asset identifier */
  id: string;
  /** Display name */
  name: string;
  /** Description */
  description: string;
  /** Category */
  category: GraphicCategory;
  /** Tags for search */
  tags: string[];
  /** Thumbnail URL */
  thumbnail?: string;
  /** Preview video URL */
  previewVideo?: string;
  /** Default duration in seconds */
  duration: number;
  /** Minimum duration in seconds */
  minDuration: number;
  /** Maximum duration in seconds */
  maxDuration: number;
  /** Graphic layers */
  layers: GraphicLayer[];
  /** Customization schema */
  customization: CustomizationSchema;
  /** Responsive breakpoints */
  responsiveBreakpoints: ResponsiveBreakpoint[];
  /** Color scheme */
  colorScheme: ColorScheme;
  /** Is premium asset */
  isPremium: boolean;
  /** Author information */
  author?: string;
  /** Version */
  version: string;
  /** Created date */
  createdAt: string;
}

/** Applied graphic instance on the timeline */
export interface AppliedGraphic {
  /** Unique instance identifier */
  id: string;
  /** Reference to source asset ID */
  assetId: string;
  /** Track ID on timeline */
  trackId: string;
  /** Start time in seconds */
  startTime: number;
  /** Duration in seconds */
  duration: number;
  /** Customized values (key is field id from schema) */
  customValues: Record<string, string | number | boolean>;
  /** Position X override (0-100 percentage) */
  positionX: number;
  /** Position Y override (0-100 percentage) */
  positionY: number;
  /** Scale override (1 = 100%) */
  scale: number;
  /** Opacity override (0-1) */
  opacity: number;
  /** Lock aspect ratio */
  lockAspectRatio: boolean;
}

/** Animation state for rendering */
export interface AnimationState {
  /** Current animation phase */
  phase: 'entry' | 'hold' | 'exit';
  /** Progress within current phase (0-1) */
  progress: number;
  /** Elapsed time in seconds */
  elapsed: number;
  /** Is animation playing */
  isPlaying: boolean;
}
