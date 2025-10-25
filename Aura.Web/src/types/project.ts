/**
 * Project file format types for saving and loading video editor projects
 */

import { TimelineClip } from '../pages/VideoEditorPage';
import { AppliedEffect } from './effects';

/**
 * Project file format version for backward compatibility
 */
export const PROJECT_FILE_VERSION = '1.0.0';

/**
 * Media item in the project media library
 */
export interface ProjectMediaItem {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'image';
  path?: string;
  dataUrl?: string;
  duration?: number;
  thumbnails?: Array<{ dataUrl: string; timestamp: number }>;
  waveform?: { peaks: number[]; duration: number };
}

/**
 * Timeline track definition
 */
export interface ProjectTrack {
  id: string;
  label: string;
  type: 'video' | 'audio';
  visible: boolean;
  locked: boolean;
}

/**
 * Clip on the timeline
 */
export interface ProjectClip {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
  prompt?: string;
  effects?: AppliedEffect[];
  transform?: {
    x?: number;
    y?: number;
    scale?: number;
    rotation?: number;
  };
  mediaId?: string;
  preview?: string;
}

/**
 * Project settings
 */
export interface ProjectSettings {
  resolution: {
    width: number;
    height: number;
  };
  frameRate: number;
  sampleRate: number;
}

/**
 * Project metadata
 */
export interface ProjectMetadata {
  name: string;
  description?: string;
  thumbnail?: string;
  createdAt: string;
  lastModifiedAt: string;
  duration: number;
  author?: string;
  tags?: string[];
}

/**
 * Complete project file format
 */
export interface ProjectFile {
  version: string;
  metadata: ProjectMetadata;
  settings: ProjectSettings;
  tracks: ProjectTrack[];
  clips: ProjectClip[];
  mediaLibrary: ProjectMediaItem[];
  playerPosition?: number;
}

/**
 * Project list item (for project library)
 */
export interface ProjectListItem {
  id: string;
  name: string;
  description?: string;
  thumbnail?: string;
  lastModifiedAt: string;
  duration: number;
  clipCount: number;
}

/**
 * Save project request
 */
export interface SaveProjectRequest {
  name: string;
  description?: string;
  project: ProjectFile;
}

/**
 * Load project response
 */
export interface LoadProjectResponse {
  id: string;
  project: ProjectFile;
}

/**
 * Autosave status
 */
export type AutosaveStatus = 'idle' | 'saving' | 'saved' | 'error';

/**
 * Default project settings
 */
export const DEFAULT_PROJECT_SETTINGS: ProjectSettings = {
  resolution: {
    width: 1920,
    height: 1080,
  },
  frameRate: 30,
  sampleRate: 48000,
};

/**
 * Create a new empty project
 */
export function createEmptyProject(name: string): ProjectFile {
  const now = new Date().toISOString();
  return {
    version: PROJECT_FILE_VERSION,
    metadata: {
      name,
      createdAt: now,
      lastModifiedAt: now,
      duration: 0,
    },
    settings: DEFAULT_PROJECT_SETTINGS,
    tracks: [
      { id: 'video1', label: 'Video 1', type: 'video', visible: true, locked: false },
      { id: 'video2', label: 'Video 2', type: 'video', visible: true, locked: false },
      { id: 'audio1', label: 'Audio 1', type: 'audio', visible: true, locked: false },
      { id: 'audio2', label: 'Audio 2', type: 'audio', visible: true, locked: false },
    ],
    clips: [],
    mediaLibrary: [],
    playerPosition: 0,
  };
}

/**
 * Convert TimelineClip to ProjectClip (removes File objects for serialization)
 */
export function timelineClipToProjectClip(clip: TimelineClip): ProjectClip {
  return {
    id: clip.id,
    trackId: clip.trackId,
    startTime: clip.startTime,
    duration: clip.duration,
    label: clip.label,
    type: clip.type,
    prompt: clip.prompt,
    effects: clip.effects,
    transform: clip.transform,
    mediaId: clip.mediaId,
    preview: clip.preview,
  };
}
