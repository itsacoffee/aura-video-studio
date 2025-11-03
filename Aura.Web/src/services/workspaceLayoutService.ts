/**
 * Workspace Layout Service
 * Manages panel arrangements and workspace presets for professional NLE workflow
 */

export interface PanelSizes {
  propertiesWidth: number;
  mediaLibraryWidth: number;
  effectsLibraryWidth: number;
  historyWidth: number;
  previewHeight: number; // Percentage
}

export interface WorkspaceLayout {
  id: string;
  name: string;
  description: string;
  panelSizes: PanelSizes;
  visiblePanels: {
    properties: boolean;
    mediaLibrary: boolean;
    effects: boolean;
    history: boolean;
  };
}

// Preset workspace layouts matching professional NLE standards
export const PRESET_LAYOUTS: Record<string, WorkspaceLayout> = {
  editing: {
    id: 'editing',
    name: 'Editing',
    description: 'Focus on timeline with large preview (Adobe Premiere Pro inspired)',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 280,
      effectsLibraryWidth: 280,
      historyWidth: 320,
      previewHeight: 70,
    },
    visiblePanels: {
      properties: false,
      mediaLibrary: false,
      effects: false,
      history: false,
    },
  },
  color: {
    id: 'color',
    name: 'Color',
    description: 'Color grading with scopes visible',
    panelSizes: {
      propertiesWidth: 350,
      mediaLibraryWidth: 0,
      effectsLibraryWidth: 0,
      historyWidth: 0,
      previewHeight: 70,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: false,
      effects: false,
      history: false,
    },
  },
  audio: {
    id: 'audio',
    name: 'Audio',
    description: 'Audio mixing with mixer visible',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 280,
      effectsLibraryWidth: 0,
      historyWidth: 0,
      previewHeight: 50,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: true,
      effects: false,
      history: false,
    },
  },
  effects: {
    id: 'effects',
    name: 'Effects',
    description: 'Effects library expanded',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 0,
      effectsLibraryWidth: 300,
      historyWidth: 0,
      previewHeight: 60,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: false,
      effects: true,
      history: false,
    },
  },
  assembly: {
    id: 'assembly',
    name: 'Assembly',
    description: 'Quick assembly with media library and timeline',
    panelSizes: {
      propertiesWidth: 0,
      mediaLibraryWidth: 300,
      effectsLibraryWidth: 0,
      historyWidth: 0,
      previewHeight: 55,
    },
    visiblePanels: {
      properties: false,
      mediaLibrary: true,
      effects: false,
      history: false,
    },
  },
};

const STORAGE_KEY = 'aura-workspace-layouts';
const CURRENT_LAYOUT_KEY = 'aura-current-workspace';

/**
 * Get all workspace layouts (presets + custom)
 */
export function getWorkspaceLayouts(): WorkspaceLayout[] {
  const customLayouts = getCustomLayouts();
  return [...Object.values(PRESET_LAYOUTS), ...customLayouts];
}

/**
 * Get custom user-created layouts
 */
export function getCustomLayouts(): WorkspaceLayout[] {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (error) {
    console.error('Error loading custom layouts:', error);
  }
  return [];
}

/**
 * Save a custom workspace layout
 */
export function saveWorkspaceLayout(layout: Omit<WorkspaceLayout, 'id'>): WorkspaceLayout {
  const id = `custom-${Date.now()}`;
  const newLayout: WorkspaceLayout = { ...layout, id };

  const customLayouts = getCustomLayouts();
  customLayouts.push(newLayout);

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(customLayouts));
  } catch (error) {
    console.error('Error saving layout:', error);
  }

  return newLayout;
}

/**
 * Delete a custom workspace layout
 */
export function deleteWorkspaceLayout(layoutId: string): void {
  // Can't delete preset layouts
  if (PRESET_LAYOUTS[layoutId]) {
    return;
  }

  const customLayouts = getCustomLayouts();
  const filtered = customLayouts.filter((l) => l.id !== layoutId);

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filtered));
  } catch (error) {
    console.error('Error deleting layout:', error);
  }
}

/**
 * Get the current active workspace layout ID
 */
export function getCurrentLayoutId(): string {
  try {
    return localStorage.getItem(CURRENT_LAYOUT_KEY) || 'editing';
  } catch {
    return 'editing';
  }
}

/**
 * Set the current active workspace layout
 */
export function setCurrentLayout(layoutId: string): void {
  try {
    localStorage.setItem(CURRENT_LAYOUT_KEY, layoutId);
  } catch (error) {
    console.error('Error setting current layout:', error);
  }
}

/**
 * Get a specific workspace layout by ID
 */
export function getWorkspaceLayout(layoutId: string): WorkspaceLayout | null {
  // Check presets first
  if (PRESET_LAYOUTS[layoutId]) {
    return PRESET_LAYOUTS[layoutId];
  }

  // Check custom layouts
  const customLayouts = getCustomLayouts();
  return customLayouts.find((l) => l.id === layoutId) || null;
}

/**
 * Apply a workspace layout (returns the panel sizes and visibility)
 */
export function applyWorkspaceLayout(layoutId: string): WorkspaceLayout | null {
  const layout = getWorkspaceLayout(layoutId);
  if (layout) {
    setCurrentLayout(layoutId);
  }
  return layout;
}

/**
 * Snap a size value to common breakpoints
 */
export function snapToBreakpoint(value: number, min: number, max: number): number {
  const breakpoints = [
    min,
    min + (max - min) * 0.25, // 25%
    min + (max - min) * 0.33, // 33%
    min + (max - min) * 0.5, // 50%
    min + (max - min) * 0.66, // 66%
    min + (max - min) * 0.75, // 75%
    max,
  ];

  // Find closest breakpoint within 20px threshold
  const threshold = 20;
  for (const breakpoint of breakpoints) {
    if (Math.abs(value - breakpoint) < threshold) {
      return breakpoint;
    }
  }

  return value;
}

/**
 * Import a workspace layout
 */
export function importWorkspaceLayout(layout: WorkspaceLayout): WorkspaceLayout {
  const customLayouts = getCustomLayouts();

  const existingNames = [...Object.values(PRESET_LAYOUTS), ...customLayouts].map((l) => l.name);

  let name = layout.name;
  let counter = 1;
  while (existingNames.includes(name)) {
    name = `${layout.name} (${counter})`;
    counter++;
  }

  const newLayout: WorkspaceLayout = {
    ...layout,
    id: `custom-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`,
    name,
  };

  customLayouts.push(newLayout);

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(customLayouts));
  } catch (error) {
    console.error('Error importing layout:', error);
    throw error;
  }

  return newLayout;
}

/**
 * Update an existing workspace layout
 */
export function updateWorkspaceLayout(
  layoutId: string,
  updates: Partial<Omit<WorkspaceLayout, 'id'>>
): WorkspaceLayout | null {
  if (PRESET_LAYOUTS[layoutId]) {
    return null;
  }

  const customLayouts = getCustomLayouts();
  const index = customLayouts.findIndex((l) => l.id === layoutId);

  if (index === -1) {
    return null;
  }

  customLayouts[index] = {
    ...customLayouts[index],
    ...updates,
  };

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(customLayouts));
  } catch (error) {
    console.error('Error updating layout:', error);
    return null;
  }

  return customLayouts[index];
}

/**
 * Duplicate a workspace layout
 */
export function duplicateWorkspaceLayout(layoutId: string): WorkspaceLayout | null {
  const layout = getWorkspaceLayout(layoutId);
  if (!layout) {
    return null;
  }

  return saveWorkspaceLayout({
    name: `${layout.name} (Copy)`,
    description: layout.description,
    panelSizes: { ...layout.panelSizes },
    visiblePanels: { ...layout.visiblePanels },
  });
}
