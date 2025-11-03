import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PRESET_LAYOUTS } from '../../services/workspaceLayoutService';
import { useWorkspaceLayoutStore } from '../workspaceLayout';

describe('WorkspaceLayout Store', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();

    // Reset store to initial state
    useWorkspaceLayoutStore.setState({
      currentLayoutId: 'editing',
      isFullscreen: false,
      collapsedPanels: {
        properties: true,
        mediaLibrary: true,
        effects: true,
        history: true,
      },
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('setCurrentLayout', () => {
    it('should update currentLayoutId when layout is changed', () => {
      const store = useWorkspaceLayoutStore.getState();

      store.setCurrentLayout('color');

      const updatedState = useWorkspaceLayoutStore.getState();
      expect(updatedState.currentLayoutId).toBe('color');
    });

    it('should apply panel visibility from layout when switching to color layout', () => {
      const store = useWorkspaceLayoutStore.getState();
      const colorLayout = PRESET_LAYOUTS['color'];

      store.setCurrentLayout('color');

      const updatedState = useWorkspaceLayoutStore.getState();

      // Color layout has properties visible, others hidden
      expect(updatedState.collapsedPanels.properties).toBe(!colorLayout.visiblePanels.properties);
      expect(updatedState.collapsedPanels.mediaLibrary).toBe(
        !colorLayout.visiblePanels.mediaLibrary
      );
      expect(updatedState.collapsedPanels.effects).toBe(!colorLayout.visiblePanels.effects);
      expect(updatedState.collapsedPanels.history).toBe(!colorLayout.visiblePanels.history);
    });

    it('should apply panel visibility from layout when switching to effects layout', () => {
      const store = useWorkspaceLayoutStore.getState();
      const effectsLayout = PRESET_LAYOUTS['effects'];

      store.setCurrentLayout('effects');

      const updatedState = useWorkspaceLayoutStore.getState();

      // Effects layout has properties and effects visible
      expect(updatedState.collapsedPanels.properties).toBe(!effectsLayout.visiblePanels.properties);
      expect(updatedState.collapsedPanels.mediaLibrary).toBe(
        !effectsLayout.visiblePanels.mediaLibrary
      );
      expect(updatedState.collapsedPanels.effects).toBe(!effectsLayout.visiblePanels.effects);
      expect(updatedState.collapsedPanels.history).toBe(!effectsLayout.visiblePanels.history);
    });

    it('should apply panel visibility from layout when switching to audio layout', () => {
      const store = useWorkspaceLayoutStore.getState();
      const audioLayout = PRESET_LAYOUTS['audio'];

      store.setCurrentLayout('audio');

      const updatedState = useWorkspaceLayoutStore.getState();

      // Audio layout has properties and mediaLibrary visible
      expect(updatedState.collapsedPanels.properties).toBe(!audioLayout.visiblePanels.properties);
      expect(updatedState.collapsedPanels.mediaLibrary).toBe(
        !audioLayout.visiblePanels.mediaLibrary
      );
      expect(updatedState.collapsedPanels.effects).toBe(!audioLayout.visiblePanels.effects);
      expect(updatedState.collapsedPanels.history).toBe(!audioLayout.visiblePanels.history);
    });

    it('should apply panel visibility from layout when switching to assembly layout', () => {
      const store = useWorkspaceLayoutStore.getState();
      const assemblyLayout = PRESET_LAYOUTS['assembly'];

      store.setCurrentLayout('assembly');

      const updatedState = useWorkspaceLayoutStore.getState();

      // Assembly layout has mediaLibrary visible
      expect(updatedState.collapsedPanels.properties).toBe(
        !assemblyLayout.visiblePanels.properties
      );
      expect(updatedState.collapsedPanels.mediaLibrary).toBe(
        !assemblyLayout.visiblePanels.mediaLibrary
      );
      expect(updatedState.collapsedPanels.effects).toBe(!assemblyLayout.visiblePanels.effects);
      expect(updatedState.collapsedPanels.history).toBe(!assemblyLayout.visiblePanels.history);
    });

    it('should persist collapsed panels to localStorage', () => {
      const store = useWorkspaceLayoutStore.getState();

      store.setCurrentLayout('color');

      const stored = localStorage.getItem('aura-collapsed-panels');
      expect(stored).toBeTruthy();

      const parsedCollapsed = JSON.parse(stored as string);
      const colorLayout = PRESET_LAYOUTS['color'];

      expect(parsedCollapsed.properties).toBe(!colorLayout.visiblePanels.properties);
      expect(parsedCollapsed.mediaLibrary).toBe(!colorLayout.visiblePanels.mediaLibrary);
      expect(parsedCollapsed.effects).toBe(!colorLayout.visiblePanels.effects);
      expect(parsedCollapsed.history).toBe(!colorLayout.visiblePanels.history);
    });
  });

  describe('togglePanelCollapsed', () => {
    it('should toggle a panel collapsed state', () => {
      const store = useWorkspaceLayoutStore.getState();

      const initialState = store.collapsedPanels.properties;
      store.togglePanelCollapsed('properties');

      const updatedState = useWorkspaceLayoutStore.getState();
      expect(updatedState.collapsedPanels.properties).toBe(!initialState);
    });

    it('should persist toggle to localStorage', () => {
      const store = useWorkspaceLayoutStore.getState();

      store.togglePanelCollapsed('properties');

      const stored = localStorage.getItem('aura-collapsed-panels');
      expect(stored).toBeTruthy();

      const parsedCollapsed = JSON.parse(stored as string);
      const updatedState = useWorkspaceLayoutStore.getState();

      expect(parsedCollapsed.properties).toBe(updatedState.collapsedPanels.properties);
    });
  });

  describe('resetLayout', () => {
    it('should reset to editing layout', () => {
      const store = useWorkspaceLayoutStore.getState();

      // Change to another layout first
      store.setCurrentLayout('color');

      // Reset
      store.resetLayout();

      const updatedState = useWorkspaceLayoutStore.getState();
      expect(updatedState.currentLayoutId).toBe('editing');
    });

    it('should collapse all panels when resetting', () => {
      const store = useWorkspaceLayoutStore.getState();

      // Expand some panels first
      store.togglePanelCollapsed('properties');
      store.togglePanelCollapsed('mediaLibrary');

      // Reset
      store.resetLayout();

      const updatedState = useWorkspaceLayoutStore.getState();
      expect(updatedState.collapsedPanels.properties).toBe(true);
      expect(updatedState.collapsedPanels.mediaLibrary).toBe(true);
      expect(updatedState.collapsedPanels.effects).toBe(true);
      expect(updatedState.collapsedPanels.history).toBe(true);
    });

    it('should clear panel size preferences from localStorage', () => {
      // Set some panel sizes in localStorage
      localStorage.setItem('editor-properties-width', '400');
      localStorage.setItem('editor-media-library-width', '300');

      const store = useWorkspaceLayoutStore.getState();
      store.resetLayout();

      expect(localStorage.getItem('editor-properties-width')).toBeNull();
      expect(localStorage.getItem('editor-media-library-width')).toBeNull();
    });
  });

  describe('getCurrentLayout', () => {
    it('should return the current layout', () => {
      const store = useWorkspaceLayoutStore.getState();

      store.setCurrentLayout('color');

      const layout = store.getCurrentLayout();
      expect(layout).toBeTruthy();
      expect(layout?.id).toBe('color');
      expect(layout?.name).toBe('Color');
    });
  });
});
