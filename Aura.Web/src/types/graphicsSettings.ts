/**
 * Graphics Settings Type Definitions
 * Mirrors backend models for type safety
 */

/**
 * Performance profile presets
 */
export type PerformanceProfile = 'maximum' | 'balanced' | 'powerSaver' | 'custom';

/**
 * DPI scaling mode
 */
export type ScalingMode = 'system' | 'manual';

/**
 * Individual visual effect toggles for "beauty" features
 */
export interface VisualEffectsSettings {
  animations: boolean;
  blurEffects: boolean;
  shadows: boolean;
  transparency: boolean;
  smoothScrolling: boolean;
  springPhysics: boolean;
  parallaxEffects: boolean;
  glowEffects: boolean;
  microInteractions: boolean;
  staggeredAnimations: boolean;
}

/**
 * DPI and display scaling configuration
 */
export interface DisplayScalingSettings {
  mode: ScalingMode;
  manualScaleFactor: number; // 1.0 = 100%, 1.5 = 150%, etc.
  perMonitorDpiAware: boolean;
  subpixelRendering: boolean;
}

/**
 * Accessibility-related visual settings
 */
export interface AccessibilitySettings {
  reducedMotion: boolean;
  highContrast: boolean;
  largeText: boolean;
  focusIndicators: boolean;
}

/**
 * Comprehensive graphics and visual settings
 */
export interface GraphicsSettings {
  profile: PerformanceProfile;
  gpuAccelerationEnabled: boolean;
  detectedGpuName: string | null;
  detectedGpuVendor: string | null;
  detectedVramMB: number;
  effects: VisualEffectsSettings;
  scaling: DisplayScalingSettings;
  accessibility: AccessibilitySettings;
  lastModified: string;
  settingsVersion: number;
}

/**
 * Factory function for default settings
 */
export function createDefaultGraphicsSettings(): GraphicsSettings {
  return {
    profile: 'maximum',
    gpuAccelerationEnabled: true,
    detectedGpuName: null,
    detectedGpuVendor: null,
    detectedVramMB: 0,
    effects: {
      animations: true,
      blurEffects: true,
      shadows: true,
      transparency: true,
      smoothScrolling: true,
      springPhysics: true,
      parallaxEffects: true,
      glowEffects: true,
      microInteractions: true,
      staggeredAnimations: true,
    },
    scaling: {
      mode: 'system',
      manualScaleFactor: 1.0,
      perMonitorDpiAware: true,
      subpixelRendering: true,
    },
    accessibility: {
      reducedMotion: false,
      highContrast: false,
      largeText: false,
      focusIndicators: true,
    },
    lastModified: new Date().toISOString(),
    settingsVersion: 1,
  };
}

/**
 * Create default visual effects for a specific profile
 */
export function createProfileEffects(profile: PerformanceProfile): VisualEffectsSettings {
  switch (profile) {
    case 'maximum':
      return {
        animations: true,
        blurEffects: true,
        shadows: true,
        transparency: true,
        smoothScrolling: true,
        springPhysics: true,
        parallaxEffects: true,
        glowEffects: true,
        microInteractions: true,
        staggeredAnimations: true,
      };
    case 'balanced':
      return {
        animations: true,
        blurEffects: false,
        shadows: true,
        transparency: true,
        smoothScrolling: true,
        springPhysics: false,
        parallaxEffects: false,
        glowEffects: false,
        microInteractions: true,
        staggeredAnimations: false,
      };
    case 'powerSaver':
      return {
        animations: false,
        blurEffects: false,
        shadows: false,
        transparency: false,
        smoothScrolling: false,
        springPhysics: false,
        parallaxEffects: false,
        glowEffects: false,
        microInteractions: false,
        staggeredAnimations: false,
      };
    case 'custom':
    default:
      return createDefaultGraphicsSettings().effects;
  }
}
