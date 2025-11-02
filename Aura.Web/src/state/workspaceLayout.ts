import { create } from 'zustand';
import {
  applyWorkspaceLayout,
  getCurrentLayoutId,
  getWorkspaceLayout,
  WorkspaceLayout,
  saveWorkspaceLayout,
  PanelSizes,
} from '../services/workspaceLayoutService';

interface WorkspaceLayoutState {
  currentLayoutId: string;
  isFullscreen: boolean;
  collapsedPanels: {
    properties: boolean;
    mediaLibrary: boolean;
    effects: boolean;
    history: boolean;
  };

  setCurrentLayout: (layoutId: string) => void;
  toggleFullscreen: () => void;
  exitFullscreen: () => void;
  togglePanelCollapsed: (panel: keyof WorkspaceLayoutState['collapsedPanels']) => void;
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
  // Default to all panels collapsed (Adobe Premiere Pro style)
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
  isFullscreen: false,
  collapsedPanels: loadCollapsedPanels(),

  setCurrentLayout: (layoutId: string) => {
    applyWorkspaceLayout(layoutId);
    set({ currentLayoutId: layoutId });
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

    // Reset collapsed panels to default (all collapsed - Adobe Premiere Pro style)
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
    set({ currentLayoutId: newLayout.id });
    return newLayout;
  },
}));
