import type {
  Brief,
  PlanSpec,
  VoiceSpec,
  BrandKitConfig,
  CaptionsConfig,
  StockSourcesConfig,
} from '../../types';

/**
 * Test data factory for creating Brief objects
 */
export function createMockBrief(overrides?: Partial<Brief>): Brief {
  return {
    topic: 'Introduction to AI',
    audience: 'Tech enthusiasts',
    goal: 'Educate viewers about artificial intelligence',
    tone: 'Informative and engaging',
    language: 'en-US',
    aspect: 'Widescreen16x9',
    ...overrides,
  };
}

/**
 * Test data factory for creating PlanSpec objects
 */
export function createMockPlanSpec(overrides?: Partial<PlanSpec>): PlanSpec {
  return {
    targetDurationMinutes: 5,
    pacing: 'Conversational',
    density: 'Balanced',
    style: 'Educational',
    ...overrides,
  };
}

/**
 * Test data factory for creating VoiceSpec objects
 */
export function createMockVoiceSpec(overrides?: Partial<VoiceSpec>): VoiceSpec {
  return {
    voiceName: 'en-US-AriaNeural',
    rate: 1.0,
    pitch: 1.0,
    pauseStyle: 'Auto',
    ...overrides,
  };
}

/**
 * Test data factory for creating BrandKitConfig objects
 */
export function createMockBrandKitConfig(overrides?: Partial<BrandKitConfig>): BrandKitConfig {
  return {
    watermarkPath: undefined,
    watermarkPosition: 'bottomRight',
    watermarkOpacity: 0.7,
    brandColor: '#0078D4',
    accentColor: '#50E6FF',
    ...overrides,
  };
}

/**
 * Test data factory for creating CaptionsConfig objects
 */
export function createMockCaptionsConfig(overrides?: Partial<CaptionsConfig>): CaptionsConfig {
  return {
    enabled: true,
    format: 'srt',
    burnIn: false,
    fontName: 'Arial',
    fontSize: 24,
    primaryColor: '#FFFFFF',
    outlineColor: '#000000',
    outlineWidth: 2,
    position: 'bottom',
    ...overrides,
  };
}

/**
 * Test data factory for creating StockSourcesConfig objects
 */
export function createMockStockSourcesConfig(
  overrides?: Partial<StockSourcesConfig>
): StockSourcesConfig {
  return {
    enablePexels: true,
    enablePixabay: false,
    enableUnsplash: true,
    enableLocalAssets: false,
    enableStableDiffusion: false,
    localAssetsDirectory: undefined,
    stableDiffusionUrl: undefined,
    ...overrides,
  };
}
