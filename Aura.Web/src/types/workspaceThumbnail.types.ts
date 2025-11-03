/**
 * Workspace Thumbnail Types
 * Types for workspace thumbnail metadata and configuration
 */

export interface WorkspaceThumbnailMetadata {
  workspaceId: string;
  thumbnailDataUrl: string | null;
  isCustom: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface WorkspaceThumbnailConfig {
  width: number;
  height: number;
  backgroundColor: string;
  panelColors: {
    mediaLibrary: string;
    effects: string;
    preview: string;
    properties: string;
    timeline: string;
    history: string;
  };
  labelStyle: {
    fontSize: number;
    color: string;
  };
}

export const DEFAULT_THUMBNAIL_CONFIG: WorkspaceThumbnailConfig = {
  width: 320,
  height: 180,
  backgroundColor: '#1a1a1a',
  panelColors: {
    mediaLibrary: '#3b82f6', // blue
    effects: '#a855f7', // purple
    preview: '#6b7280', // gray
    properties: '#10b981', // green
    timeline: '#f97316', // orange
    history: '#14b8a6', // teal
  },
  labelStyle: {
    fontSize: 8,
    color: '#ffffff',
  },
};
