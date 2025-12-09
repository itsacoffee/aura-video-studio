/**
 * Store exports
 * Central export point for all Zustand stores
 */

export { useAppStore } from './appStore';
export type { AppState, AppSettings, Notification } from './appStore';

export { useAuthStore } from './authStore';
export type { AuthState } from './authStore';

export { useProjectsStore } from './projectsStore';
export type { ProjectsState } from './projectsStore';

export { useVideoGenerationStore } from './videoGenerationStore';
export type { VideoGenerationState, VideoGenerationJob } from './videoGenerationStore';

export { useJobQueueStore } from './jobQueueStore';
export type { JobQueueState } from './jobQueueStore';

export {
  useSettingsStore,
  loadOperatingModeFromBackend,
  isProviderAllowed,
  filterProvidersByMode,
} from './settingsStore';
export type { SettingsStoreState, OperatingMode, OperatingModeInfo } from './settingsStore';

export { useOpenCutTimelineStore } from './opencutTimeline';
export type {
  OpenCutTimelineState,
  OpenCutTimelineStore,
  TimelineClip,
  TimelineTrack,
  ClipType,
  ClipTransform,
  BlendMode,
  ClipAudio,
  ClipText,
} from './opencutTimeline';

export { useOpenCutMediaStore } from './opencutMedia';
export type { OpenCutMediaStore, OpenCutMediaFile, MediaType } from './opencutMedia';

export { useOpenCutPlaybackStore } from './opencutPlayback';
export type { OpenCutPlaybackStore } from './opencutPlayback';

export { useOpenCutProjectStore } from './opencutProject';
export type { OpenCutProjectState, OpenCutProject } from './opencutProject';

export { useOpenCutKeyframesStore } from './opencutKeyframes';
export type {
  OpenCutKeyframesStore,
  Keyframe,
  KeyframeTrack,
  EasingType,
  BezierHandles,
} from './opencutKeyframes';

export { useTextAnimationsStore, BUILTIN_TEXT_ANIMATION_PRESETS } from './opencutTextAnimations';
export type {
  TextAnimationsStore,
  TextAnimationType,
  TextAnimationCategory,
  TextAnimationPosition,
  TextAnimationPreset,
  AppliedTextAnimation,
} from './opencutTextAnimations';

export { useExportStore, BUILTIN_PRESETS } from './opencutExport';
export type {
  OpenCutExportStore,
  ExportState,
  ExportActions,
  ExportSettings,
  ExportPreset,
} from './opencutExport';

export { useMotionGraphicsStore, BUILTIN_GRAPHICS } from './opencutMotionGraphics';
export type { MotionGraphicsStore } from './opencutMotionGraphics';
