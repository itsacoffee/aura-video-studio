/**
 * Workspace Import/Export Types
 * Defines the schema for workspace configuration files
 */

export interface WorkspaceExportFormat {
  version: string;
  name: string;
  description: string;
  author?: string;
  created: string;
  modified: string;
  layout: WorkspaceLayoutConfig;
  shortcuts?: Record<string, string>;
}

export interface WorkspaceLayoutConfig {
  mediaLibrary: PanelConfig;
  effectsLibrary: PanelConfig;
  preview: PanelConfig;
  properties: PanelConfig;
  timeline: PanelConfig;
  history: PanelConfig;
}

export interface PanelConfig {
  visible: boolean;
  width?: string;
  height?: string;
  collapsed: boolean;
}

export interface WorkspaceBundle {
  version: string;
  created: string;
  workspaces: WorkspaceExportFormat[];
}

export interface ImportValidationResult {
  valid: boolean;
  errors: string[];
  warnings: string[];
}

export interface ImportOptions {
  overwriteExisting: boolean;
  renameOnConflict: boolean;
  importShortcuts: boolean;
}

export interface ExportOptions {
  includeMetadata: boolean;
  includeShortcuts: boolean;
  format: 'single' | 'bundle';
}

export interface BackupMetadata {
  timestamp: string;
  workspaceCount: number;
  size: number;
}

export const WORKSPACE_FILE_VERSION = '1.0';
export const WORKSPACE_FILE_EXTENSION = '.workspace';
export const WORKSPACE_BUNDLE_EXTENSION = '.workspace-bundle';
