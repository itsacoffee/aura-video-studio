/**
 * Motion Graphics Type System
 *
 * Comprehensive TypeScript types for the motion graphics library featuring
 * professionally designed lower thirds, callouts, social media elements,
 * animated titles, kinetic typography, and shape animations.
 */

/** Category of motion graphic asset */
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

/** Animation style for graphics */
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

/** Easing preset options including spring physics */
export type EasingPreset =
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
  | 'springGentle'
  | 'springBouncy'
  | 'springStiff';

/** Aspect ratio presets */
export type AspectRatioPreset = '16:9' | '9:16' | '1:1' | '4:5' | '21:9' | 'custom';

/** Direction for animations */
export type AnimationDirection = 'left' | 'right' | 'up' | 'down' | 'center';

/** Color scheme options */
export type ColorScheme = 'dark' | 'light' | 'auto';

/** Animation keyframe with time and value */
export interface AnimationKeyframe {
  /** Time in seconds relative to animation start */
  time: number;
  /** Value at this keyframe (0-1 for normalized, or absolute value) */
  value: number;
  /** Optional easing for transition to next keyframe */
  easing?: EasingPreset;
}

/** Animation property definition */
export interface AnimationProperty {
  /** Property name (e.g., 'opacity', 'x', 'scale') */
  property: string;
  /** Keyframes for this property */
  keyframes: AnimationKeyframe[];
}

/** Animation sequence with multiple properties */
export interface AnimationSequence {
  /** Unique identifier */
  id: string;
  /** Display name */
  name: string;
  /** Total duration in seconds */
  duration: number;
  /** All animated properties */
  properties: AnimationProperty[];
  /** Loop the animation */
  loop?: boolean;
}

/** Entry/exit animation configuration */
export interface EntryExitAnimation {
  /** Animation style */
  style: AnimationStyle;
  /** Duration in seconds */
  duration: number;
  /** Easing preset */
  easing: EasingPreset;
  /** Direction of animation */
  direction?: AnimationDirection;
  /** Stagger delay for multi-element animations (seconds) */
  stagger?: number;
  /** Blur amount during animation (pixels) */
  blur?: number;
  /** Scale during animation (0-1) */
  scale?: number;
  /** Rotation during animation (degrees) */
  rotation?: number;
}

/** Blend mode options */
export type GraphicBlendMode =
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

/** Transform properties for layers */
export interface LayerTransform {
  /** X position (percentage of canvas) */
  x: number;
  /** Y position (percentage of canvas) */
  y: number;
  /** Scale X (percentage, 100 = normal) */
  scaleX: number;
  /** Scale Y (percentage, 100 = normal) */
  scaleY: number;
  /** Rotation in degrees */
  rotation: number;
  /** Opacity (0-100) */
  opacity: number;
  /** Anchor point X (percentage, 50 = center) */
  anchorX: number;
  /** Anchor point Y (percentage, 50 = center) */
  anchorY: number;
}

/** Graphic layer with animations and children */
export interface GraphicLayer {
  /** Unique layer identifier */
  id: string;
  /** Layer name */
  name: string;
  /** Layer type */
  type: 'text' | 'shape' | 'image' | 'group';
  /** Transform properties */
  transform: LayerTransform;
  /** Entry animation */
  entryAnimation?: EntryExitAnimation;
  /** Exit animation */
  exitAnimation?: EntryExitAnimation;
  /** Continuous animation during hold phase */
  holdAnimation?: AnimationSequence;
  /** Blend mode */
  blendMode: GraphicBlendMode;
  /** Whether layer is visible */
  visible: boolean;
  /** Z-index for stacking order */
  zIndex: number;
  /** Child layers (for groups) */
  children?: GraphicLayer[];
  /** Text-specific properties */
  textProperties?: TextLayerProperties;
  /** Shape-specific properties */
  shapeProperties?: ShapeLayerProperties;
}

/** Gradient stop */
export interface GradientStop {
  /** Position (0-1) */
  offset: number;
  /** Color value */
  color: string;
}

/** Gradient definition */
export interface GradientDefinition {
  /** Gradient type */
  type: 'linear' | 'radial' | 'conic';
  /** Gradient stops */
  stops: GradientStop[];
  /** Angle for linear gradient (degrees) */
  angle?: number;
  /** Center X for radial/conic (percentage) */
  centerX?: number;
  /** Center Y for radial/conic (percentage) */
  centerY?: number;
  /** Radius for radial gradient (percentage) */
  radius?: number;
}

/** Text shadow definition */
export interface TextShadow {
  /** Horizontal offset (pixels) */
  offsetX: number;
  /** Vertical offset (pixels) */
  offsetY: number;
  /** Blur radius (pixels) */
  blur: number;
  /** Shadow color */
  color: string;
}

/** Text glow effect */
export interface TextGlow {
  /** Blur radius (pixels) */
  blur: number;
  /** Glow color */
  color: string;
  /** Intensity (0-1) */
  intensity: number;
}

/** Text layer properties */
export interface TextLayerProperties {
  /** Text content */
  content: string;
  /** Font family */
  fontFamily: string;
  /** Font size (pixels) */
  fontSize: number;
  /** Font weight */
  fontWeight: number;
  /** Font style */
  fontStyle: 'normal' | 'italic';
  /** Text alignment */
  textAlign: 'left' | 'center' | 'right';
  /** Line height multiplier */
  lineHeight: number;
  /** Letter spacing (pixels) */
  letterSpacing: number;
  /** Text color (solid) */
  color: string;
  /** Text gradient (overrides color if set) */
  gradient?: GradientDefinition;
  /** Stroke/outline color */
  strokeColor?: string;
  /** Stroke width (pixels) */
  strokeWidth?: number;
  /** Text shadow */
  shadow?: TextShadow;
  /** Text glow effect */
  glow?: TextGlow;
  /** Text transform */
  textTransform?: 'none' | 'uppercase' | 'lowercase' | 'capitalize';
}

/** Shape types */
export type ShapeType = 'rectangle' | 'circle' | 'ellipse' | 'line' | 'polygon' | 'path';

/** Shape layer properties */
export interface ShapeLayerProperties {
  /** Shape type */
  shapeType: ShapeType;
  /** Width (for rectangles) */
  width?: number;
  /** Height (for rectangles) */
  height?: number;
  /** Radius (for circles) */
  radius?: number;
  /** Corner radius (for rounded rectangles) */
  cornerRadius?: number;
  /** Fill color */
  fillColor?: string;
  /** Fill gradient */
  fillGradient?: GradientDefinition;
  /** Stroke color */
  strokeColor?: string;
  /** Stroke width */
  strokeWidth?: number;
  /** Stroke dash array */
  strokeDashArray?: number[];
  /** SVG path data (for custom paths) */
  pathData?: string;
  /** Polygon points */
  points?: Array<{ x: number; y: number }>;
}

/** Customization field types */
export type CustomizationFieldType =
  | 'text'
  | 'color'
  | 'number'
  | 'select'
  | 'toggle'
  | 'slider'
  | 'gradient'
  | 'font';

/** Customization field definition */
export interface CustomizationField {
  /** Field identifier */
  id: string;
  /** Display label */
  label: string;
  /** Field type */
  type: CustomizationFieldType;
  /** Default value */
  defaultValue: string | number | boolean;
  /** Category for grouping */
  category: 'text' | 'colors' | 'layout' | 'animation' | 'advanced';
  /** Options for select fields */
  options?: Array<{ value: string; label: string }>;
  /** Min value for number/slider */
  min?: number;
  /** Max value for number/slider */
  max?: number;
  /** Step for number/slider */
  step?: number;
  /** Maximum character count for text fields */
  maxLength?: number;
  /** Placeholder text */
  placeholder?: string;
}

/** Customization schema for a graphic */
export interface CustomizationSchema {
  /** All customizable fields */
  fields: CustomizationField[];
}

/** Responsive breakpoint configuration */
export interface ResponsiveBreakpoint {
  /** Aspect ratio this breakpoint applies to */
  aspectRatio: AspectRatioPreset;
  /** Layer transform overrides */
  transforms?: Record<string, Partial<LayerTransform>>;
  /** Font size adjustments (multiplier) */
  fontSizeMultiplier?: number;
  /** Whether to hide certain layers */
  hiddenLayers?: string[];
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
  /** Duration in seconds */
  duration: number;
  /** Thumbnail URL */
  thumbnailUrl?: string;
  /** Preview video URL */
  previewUrl?: string;
  /** Whether this is a premium asset */
  isPremium: boolean;
  /** Layers that make up the graphic */
  layers: GraphicLayer[];
  /** Customization options */
  customization: CustomizationSchema;
  /** Default customization values */
  defaultValues: Record<string, string | number | boolean>;
  /** Responsive breakpoints */
  responsiveBreakpoints?: ResponsiveBreakpoint[];
  /** Color scheme support */
  colorSchemes?: ColorScheme[];
  /** Version */
  version: string;
  /** Author */
  author?: string;
}

/** Applied graphic instance on timeline */
export interface AppliedGraphic {
  /** Unique instance identifier */
  id: string;
  /** Reference to asset ID */
  assetId: string;
  /** Track ID where graphic is placed */
  trackId: string;
  /** Start time on timeline (seconds) */
  startTime: number;
  /** Duration (can differ from asset default) */
  duration: number;
  /** Customization values applied to this instance */
  customValues: Record<string, string | number | boolean>;
  /** Position override (percentage) */
  positionX?: number;
  /** Position override (percentage) */
  positionY?: number;
  /** Scale override (percentage) */
  scale?: number;
  /** Opacity override (0-100) */
  opacity?: number;
  /** Whether graphic is locked */
  locked: boolean;
  /** Instance name (for display) */
  name?: string;
}

/** Motion graphics store state */
export interface MotionGraphicsState {
  /** All available graphic assets */
  assets: MotionGraphicAsset[];
  /** Applied graphics on timeline */
  applied: AppliedGraphic[];
  /** Currently selected graphic ID */
  selectedGraphicId: string | null;
  /** Search query for filtering */
  searchQuery: string;
  /** Filter by category */
  filterCategory: GraphicCategory | 'all';
  /** Asset being previewed */
  previewingAssetId: string | null;
}

/** Motion graphics store actions */
export interface MotionGraphicsActions {
  /** Add a graphic to the timeline */
  addGraphic: (assetId: string, trackId: string, startTime: number) => string;
  /** Remove a graphic from the timeline */
  removeGraphic: (graphicId: string) => void;
  /** Update an applied graphic */
  updateGraphic: (graphicId: string, updates: Partial<AppliedGraphic>) => void;
  /** Update a single customization value */
  updateGraphicValue: (graphicId: string, key: string, value: string | number | boolean) => void;
  /** Duplicate a graphic with offset timing */
  duplicateGraphic: (graphicId: string) => string | null;
  /** Select a graphic for editing */
  selectGraphic: (graphicId: string | null) => void;
  /** Set search query */
  setSearchQuery: (query: string) => void;
  /** Set filter category */
  setFilterCategory: (category: GraphicCategory | 'all') => void;
  /** Set previewing asset */
  setPreviewingAsset: (assetId: string | null) => void;
  /** Get asset by ID */
  getAsset: (assetId: string) => MotionGraphicAsset | undefined;
  /** Get assets by category */
  getAssetsByCategory: (category: GraphicCategory) => MotionGraphicAsset[];
  /** Get graphics on a specific track */
  getGraphicsForTrack: (trackId: string) => AppliedGraphic[];
  /** Get graphics in a time range */
  getGraphicsInRange: (startTime: number, endTime: number) => AppliedGraphic[];
  /** Search assets by query */
  searchAssets: (query: string) => MotionGraphicAsset[];
}

/** Complete motion graphics store type */
export type MotionGraphicsStore = MotionGraphicsState & MotionGraphicsActions;
