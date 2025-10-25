/**
 * Video Effects System Type Definitions
 */

// Effect categories
export enum EffectCategory {
  ColorCorrection = 'Color Correction',
  BlurSharpen = 'Blur & Sharpen',
  Transform = 'Transform',
  Transitions = 'Transitions',
  Stylize = 'Stylize',
}

// Effect parameter types
export interface EffectParameter {
  name: string;
  label: string;
  type: 'number' | 'boolean' | 'color' | 'select';
  min?: number;
  max?: number;
  step?: number;
  defaultValue: number | boolean | string;
  options?: Array<{ label: string; value: string | number }>;
}

// Keyframe for parameter animation
export interface Keyframe {
  time: number; // Time in seconds
  value: number | boolean | string;
  easing?: 'linear' | 'ease-in' | 'ease-out' | 'ease-in-out' | 'bezier';
  // Bezier curve control points (for custom easing)
  bezier?: [number, number, number, number];
}

// Effect instance applied to a clip
export interface AppliedEffect {
  id: string;
  effectType: string;
  enabled: boolean;
  parameters: Record<string, number | boolean | string>;
  keyframes?: Record<string, Keyframe[]>; // Parameter name -> keyframes
}

// Effect definition
export interface EffectDefinition {
  type: string;
  name: string;
  category: EffectCategory;
  description: string;
  icon?: string;
  parameters: EffectParameter[];
}

// Effect preset
export interface EffectPreset {
  id: string;
  name: string;
  description: string;
  category?: string;
  effects: Array<{
    effectType: string;
    parameters: Record<string, number | boolean | string>;
  }>;
  thumbnail?: string;
}

// Built-in effect definitions
export const EFFECT_DEFINITIONS: EffectDefinition[] = [
  // Color Correction
  {
    type: 'brightness',
    name: 'Brightness',
    category: EffectCategory.ColorCorrection,
    description: 'Adjust image brightness',
    parameters: [
      {
        name: 'value',
        label: 'Brightness',
        type: 'number',
        min: -100,
        max: 100,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'contrast',
    name: 'Contrast',
    category: EffectCategory.ColorCorrection,
    description: 'Adjust image contrast',
    parameters: [
      {
        name: 'value',
        label: 'Contrast',
        type: 'number',
        min: -100,
        max: 100,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'saturation',
    name: 'Saturation',
    category: EffectCategory.ColorCorrection,
    description: 'Adjust color saturation',
    parameters: [
      {
        name: 'value',
        label: 'Saturation',
        type: 'number',
        min: -100,
        max: 100,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'hue',
    name: 'Hue Rotate',
    category: EffectCategory.ColorCorrection,
    description: 'Rotate color hue',
    parameters: [
      {
        name: 'value',
        label: 'Hue',
        type: 'number',
        min: 0,
        max: 360,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  // Blur & Sharpen
  {
    type: 'gaussian-blur',
    name: 'Gaussian Blur',
    category: EffectCategory.BlurSharpen,
    description: 'Apply gaussian blur',
    parameters: [
      {
        name: 'radius',
        label: 'Blur Radius',
        type: 'number',
        min: 0,
        max: 20,
        step: 0.1,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'motion-blur',
    name: 'Motion Blur',
    category: EffectCategory.BlurSharpen,
    description: 'Apply directional motion blur',
    parameters: [
      {
        name: 'angle',
        label: 'Angle',
        type: 'number',
        min: 0,
        max: 360,
        step: 1,
        defaultValue: 0,
      },
      {
        name: 'distance',
        label: 'Distance',
        type: 'number',
        min: 0,
        max: 50,
        step: 1,
        defaultValue: 5,
      },
    ],
  },
  // Transform
  {
    type: 'scale',
    name: 'Scale',
    category: EffectCategory.Transform,
    description: 'Scale video size',
    parameters: [
      {
        name: 'scaleX',
        label: 'Scale X',
        type: 'number',
        min: 0.1,
        max: 5,
        step: 0.01,
        defaultValue: 1,
      },
      {
        name: 'scaleY',
        label: 'Scale Y',
        type: 'number',
        min: 0.1,
        max: 5,
        step: 0.01,
        defaultValue: 1,
      },
    ],
  },
  {
    type: 'rotate',
    name: 'Rotate',
    category: EffectCategory.Transform,
    description: 'Rotate video',
    parameters: [
      {
        name: 'angle',
        label: 'Rotation',
        type: 'number',
        min: -360,
        max: 360,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'position',
    name: 'Position',
    category: EffectCategory.Transform,
    description: 'Adjust video position',
    parameters: [
      {
        name: 'x',
        label: 'X Position',
        type: 'number',
        min: -1000,
        max: 1000,
        step: 1,
        defaultValue: 0,
      },
      {
        name: 'y',
        label: 'Y Position',
        type: 'number',
        min: -1000,
        max: 1000,
        step: 1,
        defaultValue: 0,
      },
    ],
  },
  // Transitions
  {
    type: 'fade',
    name: 'Fade',
    category: EffectCategory.Transitions,
    description: 'Fade in/out transition',
    parameters: [
      {
        name: 'opacity',
        label: 'Opacity',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 1,
      },
    ],
  },
  {
    type: 'dissolve',
    name: 'Dissolve',
    category: EffectCategory.Transitions,
    description: 'Dissolve transition',
    parameters: [
      {
        name: 'amount',
        label: 'Amount',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 0,
      },
    ],
  },
  {
    type: 'wipe',
    name: 'Wipe',
    category: EffectCategory.Transitions,
    description: 'Wipe transition',
    parameters: [
      {
        name: 'progress',
        label: 'Progress',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 0,
      },
      {
        name: 'direction',
        label: 'Direction',
        type: 'select',
        defaultValue: 'left-to-right',
        options: [
          { label: 'Left to Right', value: 'left-to-right' },
          { label: 'Right to Left', value: 'right-to-left' },
          { label: 'Top to Bottom', value: 'top-to-bottom' },
          { label: 'Bottom to Top', value: 'bottom-to-top' },
        ],
      },
    ],
  },
  // Stylize
  {
    type: 'vignette',
    name: 'Vignette',
    category: EffectCategory.Stylize,
    description: 'Add vignette effect',
    parameters: [
      {
        name: 'intensity',
        label: 'Intensity',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 0.5,
      },
      {
        name: 'radius',
        label: 'Radius',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 0.7,
      },
    ],
  },
  {
    type: 'grain',
    name: 'Film Grain',
    category: EffectCategory.Stylize,
    description: 'Add film grain texture',
    parameters: [
      {
        name: 'intensity',
        label: 'Intensity',
        type: 'number',
        min: 0,
        max: 1,
        step: 0.01,
        defaultValue: 0.1,
      },
    ],
  },
];

// Built-in effect presets
export const EFFECT_PRESETS: EffectPreset[] = [
  {
    id: 'cinematic-look',
    name: 'Cinematic Look',
    description: 'Professional cinematic color grading',
    category: 'Professional',
    effects: [
      { effectType: 'contrast', parameters: { value: 15 } },
      { effectType: 'saturation', parameters: { value: -10 } },
      { effectType: 'vignette', parameters: { intensity: 0.3, radius: 0.8 } },
    ],
  },
  {
    id: 'vintage-film',
    name: 'Vintage Film',
    description: 'Old film look with grain',
    category: 'Retro',
    effects: [
      { effectType: 'saturation', parameters: { value: -30 } },
      { effectType: 'contrast', parameters: { value: 20 } },
      { effectType: 'grain', parameters: { intensity: 0.3 } },
      { effectType: 'vignette', parameters: { intensity: 0.5, radius: 0.6 } },
    ],
  },
  {
    id: 'black-and-white',
    name: 'Black & White',
    description: 'Classic monochrome',
    category: 'Classic',
    effects: [
      { effectType: 'saturation', parameters: { value: -100 } },
      { effectType: 'contrast', parameters: { value: 10 } },
    ],
  },
  {
    id: 'dreamy',
    name: 'Dreamy',
    description: 'Soft dreamy look',
    category: 'Artistic',
    effects: [
      { effectType: 'gaussian-blur', parameters: { radius: 1 } },
      { effectType: 'brightness', parameters: { value: 10 } },
      { effectType: 'saturation', parameters: { value: 20 } },
    ],
  },
  {
    id: 'high-contrast',
    name: 'High Contrast',
    description: 'Bold and vibrant',
    category: 'Bold',
    effects: [
      { effectType: 'contrast', parameters: { value: 40 } },
      { effectType: 'saturation', parameters: { value: 25 } },
    ],
  },
];
