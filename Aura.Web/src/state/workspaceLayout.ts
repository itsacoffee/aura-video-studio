import { create } from 'zustand';
import {
  applyWorkspaceLayout,
  getCurrentLayoutId,
  getWorkspaceLayout,
  WorkspaceLayout,
  saveWorkspaceLayout,
  PanelSizes,
} from '../services/workspaceLayoutService';
import { saveWorkspaceThumbnail } from '../services/workspaceThumbnailService';
import { generateWorkspaceThumbnail } from '../utils/workspaceThumbnailGenerator';

interface WorkspaceLayoutState {
  currentLayoutId: string;
  activePresetId: string | null;
  isFullscreen: boolean;
  collapsedPanels: {
    properties: boolean;
    mediaLibrary: boolean;
    effects: boolean;
    history: boolean;
  };
  visiblePanels: {
    properties: boolean;
    mediaLibrary: boolean;
    effects: boolean;
    history: boolean;
  };

  setCurrentLayout: (layoutId: string) => void;
  setActivePreset: (id: string) => void;
  resetToPreset: (id: string) => void;
  toggleFullscreen: () => void;
  exitFullscreen: () => void;
  togglePanelCollapsed: (panel: keyof WorkspaceLayoutState['collapsedPanels']) => void;
  togglePanelVisibility: (panel: keyof WorkspaceLayoutState['visiblePanels']) => void;
  setPanelVisibility: (
    panel: keyof WorkspaceLayoutState['visiblePanels'],
    visible: boolean
  ) => void;
  toggleAllLeftPanels: () => void;
  resetLayout: () => void;
  getCurrentLayout: () => WorkspaceLayout | null;
  saveCurrentLayout: (name: string, description: string, panelSizes: PanelSizes) => WorkspaceLayout;
}

const loadCollapsedPanels = () => {
  try {
    const stored = localStorage.getItem('aura-collapsed-panels');
    if (stored) {
      return JSON.parse(stored);
    }
  } catch {
    // Ignore errors
  }
  // Default to all panels collapsed (professional video editor style)
  return {
    properties: true,
    mediaLibrary: true,
    effects: true,
    history: true,
  };
};

const saveCollapsedPanels = (collapsed: WorkspaceLayoutState['collapsedPanels']) => {
  try {
    localStorage.setItem('aura-collapsed-panels', JSON.stringify(collapsed));
  } catch {
    // Ignore errors
  }
};

export const useWorkspaceLayoutStore = create<WorkspaceLayoutState>((set, get) => ({
  currentLayoutId: getCurrentLayoutId(),
  activePresetId: getCurrentLayoutId(),
  isFullscreen: false,
  collapsedPanels: loadCollapsedPanels(),
  visiblePanels: {
    properties: true,
    mediaLibrary: true,
    effects: true,
    history: true,
  },

  setCurrentLayout: (layoutId: string) => {
    const layout = applyWorkspaceLayout(layoutId);
    if (layout) {
      // Apply the layout's panel visibility to collapsed panels state
      // A panel is collapsed if it's NOT visible in the layout
      const newCollapsedPanels = {
        properties: !layout.visiblePanels.properties,
        mediaLibrary: !layout.visiblePanels.mediaLibrary,
        effects: !layout.visiblePanels.effects,
        history: !layout.visiblePanels.history,
      };
      const newVisiblePanels = { ...layout.visiblePanels };
      saveCollapsedPanels(newCollapsedPanels);
      set({
        currentLayoutId: layoutId,
        activePresetId: layoutId,
        collapsedPanels: newCollapsedPanels,
        visiblePanels: newVisiblePanels,
      });
    } else {
      set({ currentLayoutId: layoutId, activePresetId: layoutId });
    }
  },

  setActivePreset: (id: string) => {
    const layout = applyWorkspaceLayout(id);
    if (layout) {
      const newCollapsedPanels = {
        properties: !layout.visiblePanels.properties,
        mediaLibrary: !layout.visiblePanels.mediaLibrary,
        effects: !layout.visiblePanels.effects,
        history: !layout.visiblePanels.history,
      };
      const newVisiblePanels = { ...layout.visiblePanels };
      saveCollapsedPanels(newCollapsedPanels);
      set({
        currentLayoutId: id,
        activePresetId: id,
        collapsedPanels: newCollapsedPanels,
        visiblePanels: newVisiblePanels,
      });
    }
  },

  resetToPreset: (id: string) => {
    const layout = getWorkspaceLayout(id);
    if (layout) {
      // Clear localStorage for panel sizes to reset to defaults
      const panelKeys = [
        'aura-editor-panel-properties',
        'aura-editor-panel-mediaLibrary',
        'aura-editor-panel-effects',
        'aura-editor-panel-history',
        'aura-editor-panel-preview',
      ];

      panelKeys.forEach((key) => {
        try {
          localStorage.removeItem(key);
        } catch {
          // Ignore errors
        }
      });

      // Apply the preset layout
      applyWorkspaceLayout(id);

      const newCollapsedPanels = {
        properties: !layout.visiblePanels.properties,
        mediaLibrary: !layout.visiblePanels.mediaLibrary,
        effects: !layout.visiblePanels.effects,
        history: !layout.visiblePanels.history,
      };
      const newVisiblePanels = { ...layout.visiblePanels };
      saveCollapsedPanels(newCollapsedPanels);

      set({
        currentLayoutId: id,
        activePresetId: id,
        collapsedPanels: newCollapsedPanels,
        visiblePanels: newVisiblePanels,
      });
    }
  },

  toggleFullscreen: () => {
    const current = get().isFullscreen;
    const newState = !current;

    if (newState) {
      document.documentElement.requestFullscreen?.().catch(() => {
        // Fullscreen request failed, revert state
        set({ isFullscreen: false });
      });
      set({ isFullscreen: true });
    } else {
      document.exitFullscreen?.().catch(() => {
        // Exit fullscreen failed
      });
      set({ isFullscreen: false });
    }
  },

  exitFullscreen: () => {
    if (get().isFullscreen) {
      document.exitFullscreen?.();
      set({ isFullscreen: false });
    }
  },

  togglePanelCollapsed: (panel: keyof WorkspaceLayoutState['collapsedPanels']) => {
    const newCollapsed = {
      ...get().collapsedPanels,
      [panel]: !get().collapsedPanels[panel],
    };
    saveCollapsedPanels(newCollapsed);
    set({ collapsedPanels: newCollapsed });
  },

  togglePanelVisibility: (panel: keyof WorkspaceLayoutState['visiblePanels']) => {
    const newVisible = {
      ...get().visiblePanels,
      [panel]: !get().visiblePanels[panel],
    };
    // Also update collapsed state to match visibility
    const newCollapsed = {
      ...get().collapsedPanels,
      [panel]: !newVisible[panel],
    };
    saveCollapsedPanels(newCollapsed);
    set({ visiblePanels: newVisible, collapsedPanels: newCollapsed });
  },

  setPanelVisibility: (panel: keyof WorkspaceLayoutState['visiblePanels'], visible: boolean) => {
    const newVisible = {
      ...get().visiblePanels,
      [panel]: visible,
    };
    // Also update collapsed state to match visibility
    const newCollapsed = {
      ...get().collapsedPanels,
      [panel]: !visible,
    };
    saveCollapsedPanels(newCollapsed);
    set({ visiblePanels: newVisible, collapsedPanels: newCollapsed });
  },

  toggleAllLeftPanels: () => {
    const currentPanels = get().collapsedPanels;
    // If any left panel is visible (not collapsed), collapse all. Otherwise, expand all.
    const shouldCollapseAll = !currentPanels.mediaLibrary || !currentPanels.effects;
    const newCollapsed = {
      ...currentPanels,
      mediaLibrary: shouldCollapseAll,
      effects: shouldCollapseAll,
    };
    const newVisible = {
      ...get().visiblePanels,
      mediaLibrary: !shouldCollapseAll,
      effects: !shouldCollapseAll,
    };
    saveCollapsedPanels(newCollapsed);
    set({ collapsedPanels: newCollapsed, visiblePanels: newVisible });
  },

  resetLayout: () => {
    const defaultLayoutId = 'editing';
    applyWorkspaceLayout(defaultLayoutId);

    // Clear localStorage for panel sizes
    const panelKeys = [
      'editor-properties-width',
      'editor-media-library-width',
      'editor-effects-library-width',
      'editor-history-width',
      'editor-preview-height',
    ];

    panelKeys.forEach((key) => {
      try {
        localStorage.removeItem(key);
      } catch {
        // Ignore errors
      }
    });

    // Reset collapsed panels to default (all collapsed - professional video editor style)
    const defaultCollapsed = {
      properties: true,
      mediaLibrary: true,
      effects: true,
      history: true,
    };
    saveCollapsedPanels(defaultCollapsed);

    set({
      currentLayoutId: defaultLayoutId,
      collapsedPanels: defaultCollapsed,
    });
  },

  getCurrentLayout: () => {
    const layoutId = get().currentLayoutId;
    return getWorkspaceLayout(layoutId);
  },

  saveCurrentLayout: (name: string, description: string, panelSizes: PanelSizes) => {
    const currentCollapsed = get().collapsedPanels;
    const newLayout = saveWorkspaceLayout({
      name,
      description,
      panelSizes,
      visiblePanels: {
        properties: !currentCollapsed.properties,
        mediaLibrary: !currentCollapsed.mediaLibrary,
        effects: !currentCollapsed.effects,
        history: !currentCollapsed.history,
      },
    });

    // Generate and save thumbnail for the new layout
    try {
      const thumbnailDataUrl = generateWorkspaceThumbnail(newLayout);
      saveWorkspaceThumbnail(newLayout.id, thumbnailDataUrl, false);
    } catch (error) {
      console.error('Failed to generate thumbnail for workspace:', error);
      // Don't fail the save operation if thumbnail generation fails
    }

    set({ currentLayoutId: newLayout.id });
    return newLayout;
  },
}));
