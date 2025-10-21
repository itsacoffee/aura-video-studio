/**
 * Timeline editor types matching backend models
 */

export enum AssetType {
  Image = 'Image',
  Video = 'Video',
  Audio = 'Audio',
}

export interface Position {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface EffectConfig {
  brightness: number;
  contrast: number;
  saturation: number;
  filter?: string;
}

export interface TimelineAsset {
  id: string;
  type: AssetType;
  filePath: string;
  start: number; // seconds
  duration: number; // seconds
  position: Position;
  zIndex: number;
  opacity: number;
  effects?: EffectConfig;
}

export interface TimelineScene {
  index: number;
  heading: string;
  script: string;
  start: number; // seconds
  duration: number; // seconds
  narrationAudioPath?: string;
  visualAssets: TimelineAsset[];
  transitionType: string;
  transitionDuration?: number; // seconds
}

export interface SubtitleTrack {
  enabled: boolean;
  filePath?: string;
  position: string;
  fontSize: number;
  fontColor: string;
  backgroundColor: string;
  backgroundOpacity: number;
}

export interface EditableTimeline {
  scenes: TimelineScene[];
  backgroundMusicPath?: string;
  subtitles: SubtitleTrack;
}

export interface TimelineState {
  timeline: EditableTimeline | null;
  selectedSceneIndex: number | null;
  selectedAssetId: string | null;
  isPlaying: boolean;
  currentTime: number;
  zoom: number;
  snapEnabled: boolean;
  isDirty: boolean;
  isSaving: boolean;
  lastSaved?: Date;
}
