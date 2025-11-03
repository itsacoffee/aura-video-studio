/**
 * Workspace Import/Export Utilities
 * Handles validation, serialization, and file operations for workspace configurations
 */

import type { WorkspaceLayout } from '../services/workspaceLayoutService';
import type {
  WorkspaceExportFormat,
  WorkspaceBundle,
  ImportValidationResult,
  WorkspaceLayoutConfig,
} from '../types/workspace.types';
import { WORKSPACE_FILE_VERSION } from '../types/workspace.types';

/**
 * Convert internal WorkspaceLayout to exportable format
 */
export function workspaceToExportFormat(
  workspace: WorkspaceLayout,
  author?: string
): WorkspaceExportFormat {
  const now = new Date().toISOString();

  return {
    version: WORKSPACE_FILE_VERSION,
    name: workspace.name,
    description: workspace.description,
    author,
    created: now,
    modified: now,
    layout: {
      mediaLibrary: {
        visible: workspace.visiblePanels.mediaLibrary,
        width: `${workspace.panelSizes.mediaLibraryWidth}px`,
        collapsed: !workspace.visiblePanels.mediaLibrary,
      },
      effectsLibrary: {
        visible: workspace.visiblePanels.effects,
        width: `${workspace.panelSizes.effectsLibraryWidth}px`,
        collapsed: !workspace.visiblePanels.effects,
      },
      preview: {
        visible: true,
        width: '60%',
        collapsed: false,
      },
      properties: {
        visible: workspace.visiblePanels.properties,
        width: `${workspace.panelSizes.propertiesWidth}px`,
        collapsed: !workspace.visiblePanels.properties,
      },
      timeline: {
        visible: true,
        height: `${workspace.panelSizes.previewHeight}%`,
        collapsed: false,
      },
      history: {
        visible: workspace.visiblePanels.history,
        collapsed: !workspace.visiblePanels.history,
      },
    },
    shortcuts: {},
  };
}

/**
 * Convert exported format to internal WorkspaceLayout
 */
export function exportFormatToWorkspace(
  exported: WorkspaceExportFormat,
  customId?: string
): WorkspaceLayout {
  const id = customId || `custom-${Date.now()}`;

  const parseSize = (size?: string, defaultValue: number = 0): number => {
    if (!size) return defaultValue;
    const parsed = parseInt(size, 10);
    return isNaN(parsed) ? defaultValue : parsed;
  };

  return {
    id,
    name: exported.name,
    description: exported.description,
    panelSizes: {
      propertiesWidth: parseSize(exported.layout.properties.width, 320),
      mediaLibraryWidth: parseSize(exported.layout.mediaLibrary.width, 280),
      effectsLibraryWidth: parseSize(exported.layout.effectsLibrary.width, 280),
      historyWidth: parseSize(exported.layout.history.width, 320),
      previewHeight: parseSize(exported.layout.timeline.height, 70),
    },
    visiblePanels: {
      properties: exported.layout.properties.visible,
      mediaLibrary: exported.layout.mediaLibrary.visible,
      effects: exported.layout.effectsLibrary.visible,
      history: exported.layout.history.visible,
    },
  };
}

/**
 * Validate workspace export format
 */
export function validateWorkspaceFormat(data: unknown): ImportValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!data || typeof data !== 'object') {
    errors.push('Invalid workspace file: not a valid JSON object');
    return { valid: false, errors, warnings };
  }

  const workspace = data as Partial<WorkspaceExportFormat>;

  if (!workspace.version) {
    errors.push('Missing required field: version');
  } else if (workspace.version !== WORKSPACE_FILE_VERSION) {
    warnings.push(
      `Workspace version ${workspace.version} may not be fully compatible with current version ${WORKSPACE_FILE_VERSION}`
    );
  }

  if (!workspace.name || typeof workspace.name !== 'string') {
    errors.push('Missing or invalid required field: name');
  }

  if (!workspace.layout || typeof workspace.layout !== 'object') {
    errors.push('Missing or invalid required field: layout');
  } else {
    const requiredPanels = [
      'mediaLibrary',
      'effectsLibrary',
      'preview',
      'properties',
      'timeline',
      'history',
    ];
    for (const panel of requiredPanels) {
      if (!workspace.layout[panel as keyof WorkspaceLayoutConfig]) {
        errors.push(`Missing required panel configuration: ${panel}`);
      }
    }
  }

  if (!workspace.created) {
    warnings.push('Missing creation date, will use current date');
  }

  if (!workspace.modified) {
    warnings.push('Missing modified date, will use current date');
  }

  return {
    valid: errors.length === 0,
    errors,
    warnings,
  };
}

/**
 * Validate workspace bundle format
 */
export function validateWorkspaceBundle(data: unknown): ImportValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!data || typeof data !== 'object') {
    errors.push('Invalid bundle file: not a valid JSON object');
    return { valid: false, errors, warnings };
  }

  const bundle = data as Partial<WorkspaceBundle>;

  if (!bundle.version) {
    errors.push('Missing required field: version');
  }

  if (!bundle.workspaces || !Array.isArray(bundle.workspaces)) {
    errors.push('Missing or invalid required field: workspaces');
  } else if (bundle.workspaces.length === 0) {
    errors.push('Bundle contains no workspaces');
  } else {
    bundle.workspaces.forEach((workspace, index) => {
      const validation = validateWorkspaceFormat(workspace);
      if (!validation.valid) {
        errors.push(`Workspace ${index + 1}: ${validation.errors.join(', ')}`);
      }
      if (validation.warnings.length > 0) {
        warnings.push(`Workspace ${index + 1}: ${validation.warnings.join(', ')}`);
      }
    });
  }

  return {
    valid: errors.length === 0,
    errors,
    warnings,
  };
}

/**
 * Export workspace to JSON string
 */
export function exportWorkspaceToJSON(workspace: WorkspaceLayout, author?: string): string {
  const exportData = workspaceToExportFormat(workspace, author);
  return JSON.stringify(exportData, null, 2);
}

/**
 * Export multiple workspaces as bundle
 */
export function exportWorkspacesAsBundle(workspaces: WorkspaceLayout[], author?: string): string {
  const bundle: WorkspaceBundle = {
    version: WORKSPACE_FILE_VERSION,
    created: new Date().toISOString(),
    workspaces: workspaces.map((ws) => workspaceToExportFormat(ws, author)),
  };
  return JSON.stringify(bundle, null, 2);
}

/**
 * Download workspace as file
 */
export function downloadWorkspaceFile(content: string, filename: string): void {
  const blob = new Blob([content], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Read file as text
 */
export function readFileAsText(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      if (e.target?.result) {
        resolve(e.target.result as string);
      } else {
        reject(new Error('Failed to read file'));
      }
    };
    reader.onerror = () => reject(new Error('Failed to read file'));
    reader.readAsText(file);
  });
}

/**
 * Parse workspace from JSON string
 */
export function parseWorkspaceJSON(jsonString: string): WorkspaceExportFormat | null {
  try {
    const data = JSON.parse(jsonString);
    const validation = validateWorkspaceFormat(data);
    if (!validation.valid) {
      throw new Error(validation.errors.join(', '));
    }
    return data as WorkspaceExportFormat;
  } catch (error) {
    console.error('Error parsing workspace JSON:', error);
    return null;
  }
}

/**
 * Parse workspace bundle from JSON string
 */
export function parseWorkspaceBundleJSON(jsonString: string): WorkspaceBundle | null {
  try {
    const data = JSON.parse(jsonString);
    const validation = validateWorkspaceBundle(data);
    if (!validation.valid) {
      throw new Error(validation.errors.join(', '));
    }
    return data as WorkspaceBundle;
  } catch (error) {
    console.error('Error parsing workspace bundle JSON:', error);
    return null;
  }
}

/**
 * Generate unique workspace name to avoid conflicts
 */
export function generateUniqueWorkspaceName(baseName: string, existingNames: string[]): string {
  let name = baseName;
  let counter = 1;

  while (existingNames.includes(name)) {
    name = `${baseName} (${counter})`;
    counter++;
  }

  return name;
}

/**
 * Sanitize filename for export
 */
export function sanitizeFilename(filename: string): string {
  return filename
    .replace(/[^a-z0-9_\-.]/gi, '_')
    .replace(/_{2,}/g, '_')
    .toLowerCase();
}
