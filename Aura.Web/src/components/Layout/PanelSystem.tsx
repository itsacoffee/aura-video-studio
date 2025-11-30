/**
 * Panel System - Priority-Based Panel Visibility Management
 *
 * Implements an Apple-inspired panel layout system where panels are shown/hidden/collapsed
 * based on their priority and available screen space. Higher priority panels (lower number)
 * are always shown first, while lower priority panels collapse or hide as space decreases.
 */

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useMemo,
  useEffect,
  type ReactNode,
} from 'react';
import { useDisplayEnvironment } from '../../hooks/useDisplayEnvironment';

/**
 * Unique identifiers for panels in the system
 */
export type PanelId = 'navigation' | 'content' | 'sidebar' | 'inspector' | 'timeline' | 'preview';

/**
 * Configuration for a panel in the system
 */
export interface PanelConfig {
  /** Unique identifier for the panel */
  id: PanelId;
  /** Priority number - lower numbers have higher priority and are shown first */
  priority: number;
  /** Minimum width before the panel must collapse or hide */
  minWidth: number;
  /** Preferred width, or 'flex' to fill available space */
  preferredWidth: number | 'flex';
  /** Maximum width, or 'none' for unlimited */
  maxWidth: number | 'none';
  /** Whether the panel can collapse to a smaller width */
  canCollapse: boolean;
  /** Width when collapsed (if canCollapse is true) */
  collapsedWidth: number;
  /** Position of the panel in the layout */
  position: 'left' | 'center' | 'right' | 'bottom';
}

/**
 * Runtime state of a panel
 */
export interface PanelState {
  /** Whether the panel is currently visible */
  isVisible: boolean;
  /** Whether the panel is in collapsed state */
  isCollapsed: boolean;
  /** Current actual width of the panel */
  actualWidth: number;
}

/**
 * Context value for the panel system
 */
export interface PanelSystemContextValue {
  /** Map of panel states by ID */
  panels: Map<PanelId, PanelState>;
  /** Register a panel with its configuration */
  registerPanel: (config: PanelConfig) => void;
  /** Unregister a panel from the system */
  unregisterPanel: (id: PanelId) => void;
  /** Toggle panel visibility */
  togglePanel: (id: PanelId) => void;
  /** Collapse a panel to its minimum width */
  collapsePanel: (id: PanelId) => void;
  /** Expand a panel from collapsed state */
  expandPanel: (id: PanelId) => void;
  /** Set a specific width for a panel */
  setPanelWidth: (id: PanelId, width: number) => void;
  /** Remaining available width after all visible panels */
  availableWidth: number;
}

const PanelSystemContext = createContext<PanelSystemContextValue | null>(null);

export interface PanelSystemProviderProps {
  children: ReactNode;
}

/**
 * Panel System Provider
 *
 * Wraps an application section to provide priority-based panel management.
 * Panels register themselves and are automatically shown/collapsed/hidden
 * based on their priority and available viewport space.
 *
 * @example
 * ```tsx
 * <PanelSystemProvider>
 *   <MyLayoutWithPanels />
 * </PanelSystemProvider>
 * ```
 */
export function PanelSystemProvider({ children }: PanelSystemProviderProps): React.ReactElement {
  const display = useDisplayEnvironment();
  const viewportWidth = display.viewportWidth;
  const [panelConfigs, setPanelConfigs] = useState<Map<PanelId, PanelConfig>>(new Map());
  const [panelStates, setPanelStates] = useState<Map<PanelId, PanelState>>(new Map());
  const [manualOverrides, setManualOverrides] = useState<Map<PanelId, Partial<PanelState>>>(
    new Map()
  );

  /**
   * Calculate which panels should be visible based on priority and available space.
   * Panels are sorted by priority and assigned space in order until viewport is filled.
   */
  const calculateVisibility = useCallback(() => {
    const sortedPanels = Array.from(panelConfigs.entries()).sort(
      ([, a], [, b]) => a.priority - b.priority
    );

    let usedWidth = 0;
    const newStates = new Map<PanelId, PanelState>();

    for (const [id, config] of sortedPanels) {
      const remainingWidth = viewportWidth - usedWidth;
      const manualOverride = manualOverrides.get(id);

      // Check if user manually toggled visibility
      if (manualOverride?.isVisible === false) {
        newStates.set(id, {
          isVisible: false,
          isCollapsed: false,
          actualWidth: 0,
        });
        continue;
      }

      // Check if user manually collapsed
      const isManuallyCollapsed = manualOverride?.isCollapsed === true;

      if (remainingWidth >= config.minWidth) {
        // Panel can be shown at full or preferred width
        const preferredWidth =
          config.preferredWidth === 'flex' ? remainingWidth : config.preferredWidth;

        const actualWidth = Math.min(
          preferredWidth,
          config.maxWidth === 'none' ? Infinity : config.maxWidth,
          remainingWidth
        );

        const shouldCollapse = isManuallyCollapsed || actualWidth < config.minWidth * 1.5;

        newStates.set(id, {
          isVisible: true,
          isCollapsed: shouldCollapse && config.canCollapse,
          actualWidth:
            shouldCollapse && config.canCollapse
              ? Math.max(config.collapsedWidth, manualOverride?.actualWidth || 0)
              : actualWidth,
        });

        usedWidth +=
          shouldCollapse && config.canCollapse
            ? Math.max(config.collapsedWidth, manualOverride?.actualWidth || 0)
            : actualWidth;
      } else if (config.canCollapse && remainingWidth >= config.collapsedWidth) {
        // Panel should be collapsed
        newStates.set(id, {
          isVisible: true,
          isCollapsed: true,
          actualWidth: config.collapsedWidth,
        });
        usedWidth += config.collapsedWidth;
      } else {
        // Panel must be hidden
        newStates.set(id, {
          isVisible: false,
          isCollapsed: false,
          actualWidth: 0,
        });
      }
    }

    setPanelStates(newStates);
  }, [viewportWidth, panelConfigs, manualOverrides]);

  // Recalculate visibility when viewport or configs change
  useEffect(() => {
    calculateVisibility();
  }, [calculateVisibility]);

  const registerPanel = useCallback((config: PanelConfig) => {
    setPanelConfigs((prev) => new Map(prev).set(config.id, config));
  }, []);

  const unregisterPanel = useCallback((id: PanelId) => {
    setPanelConfigs((prev) => {
      const next = new Map(prev);
      next.delete(id);
      return next;
    });
    setPanelStates((prev) => {
      const next = new Map(prev);
      next.delete(id);
      return next;
    });
    setManualOverrides((prev) => {
      const next = new Map(prev);
      next.delete(id);
      return next;
    });
  }, []);

  const togglePanel = useCallback((id: PanelId) => {
    setManualOverrides((prev) => {
      const current = prev.get(id);
      const next = new Map(prev);
      next.set(id, {
        ...current,
        isVisible: current?.isVisible === false ? true : false,
      });
      return next;
    });
  }, []);

  const collapsePanel = useCallback(
    (id: PanelId) => {
      const config = panelConfigs.get(id);
      if (!config?.canCollapse) return;

      setManualOverrides((prev) => {
        const current = prev.get(id);
        const next = new Map(prev);
        next.set(id, {
          ...current,
          isCollapsed: true,
        });
        return next;
      });
    },
    [panelConfigs]
  );

  const expandPanel = useCallback((id: PanelId) => {
    setManualOverrides((prev) => {
      const current = prev.get(id);
      const next = new Map(prev);
      next.set(id, {
        ...current,
        isCollapsed: false,
      });
      return next;
    });
  }, []);

  const setPanelWidth = useCallback(
    (id: PanelId, width: number) => {
      const config = panelConfigs.get(id);
      if (!config) return;

      const clampedWidth = Math.max(
        config.minWidth,
        Math.min(width, config.maxWidth === 'none' ? Infinity : config.maxWidth)
      );

      setPanelStates((prev) => {
        const state = prev.get(id);
        if (!state) return prev;

        const next = new Map(prev);
        next.set(id, { ...state, actualWidth: clampedWidth });
        return next;
      });

      // Also store in manual overrides to persist through recalculations
      setManualOverrides((prev) => {
        const current = prev.get(id);
        const next = new Map(prev);
        next.set(id, {
          ...current,
          actualWidth: clampedWidth,
        });
        return next;
      });
    },
    [panelConfigs]
  );

  const availableWidth = useMemo(() => {
    let used = 0;
    panelStates.forEach((state) => {
      if (state.isVisible) used += state.actualWidth;
    });
    return viewportWidth - used;
  }, [viewportWidth, panelStates]);

  const value: PanelSystemContextValue = useMemo(
    () => ({
      panels: panelStates,
      registerPanel,
      unregisterPanel,
      togglePanel,
      collapsePanel,
      expandPanel,
      setPanelWidth,
      availableWidth,
    }),
    [
      panelStates,
      registerPanel,
      unregisterPanel,
      togglePanel,
      collapsePanel,
      expandPanel,
      setPanelWidth,
      availableWidth,
    ]
  );

  return <PanelSystemContext.Provider value={value}>{children}</PanelSystemContext.Provider>;
}

/**
 * Hook to access the full panel system context
 *
 * @throws Error if used outside PanelSystemProvider
 *
 * @example
 * ```tsx
 * const { panels, togglePanel } = usePanelSystem();
 * ```
 */
export function usePanelSystem(): PanelSystemContextValue {
  const context = useContext(PanelSystemContext);
  if (!context) {
    throw new Error('usePanelSystem must be used within PanelSystemProvider');
  }
  return context;
}

/**
 * Hook to access a single panel's state and controls
 *
 * @param id - The panel ID to access
 * @returns Panel state and control functions
 *
 * @example
 * ```tsx
 * const { isVisible, isCollapsed, width, toggle, collapse, expand } = usePanel('sidebar');
 * ```
 */
export function usePanel(id: PanelId) {
  const { panels, togglePanel, collapsePanel, expandPanel, setPanelWidth } = usePanelSystem();
  const state = panels.get(id);

  return useMemo(
    () => ({
      isVisible: state?.isVisible ?? false,
      isCollapsed: state?.isCollapsed ?? false,
      width: state?.actualWidth ?? 0,
      toggle: () => togglePanel(id),
      collapse: () => collapsePanel(id),
      expand: () => expandPanel(id),
      setWidth: (width: number) => setPanelWidth(id, width),
    }),
    [state, id, togglePanel, collapsePanel, expandPanel, setPanelWidth]
  );
}
