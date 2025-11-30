/**
 * Panel Preferences Hook
 *
 * Persists panel sizes and collapsed states to localStorage for session continuity.
 * Supports both individual panel preferences and full layout restoration.
 */

import { useState, useCallback, useEffect, useMemo } from 'react';

const STORAGE_KEY = 'aura-panel-preferences';

/**
 * Preferences for a single panel
 */
export interface PanelPreference {
  /** Width of the panel when not collapsed */
  width: number;
  /** Whether the panel is collapsed */
  collapsed: boolean;
  /** Whether the panel is visible (user toggled) */
  visible: boolean;
}

/**
 * All panel preferences stored together
 */
export interface PanelPreferences {
  [panelId: string]: PanelPreference;
}

/**
 * Return value from usePanelPreferences hook
 */
export interface UsePanelPreferencesReturn {
  /** Current panel preferences */
  preferences: PanelPreferences;
  /** Get preference for a single panel */
  getPreference: (panelId: string) => PanelPreference | undefined;
  /** Set preference for a single panel */
  setPreference: (panelId: string, preference: Partial<PanelPreference>) => void;
  /** Set width for a panel */
  setWidth: (panelId: string, width: number) => void;
  /** Set collapsed state for a panel */
  setCollapsed: (panelId: string, collapsed: boolean) => void;
  /** Set visible state for a panel */
  setVisible: (panelId: string, visible: boolean) => void;
  /** Reset preferences for a single panel */
  resetPreference: (panelId: string) => void;
  /** Reset all preferences */
  resetAll: () => void;
}

/**
 * Load preferences from localStorage
 */
function loadPreferences(): PanelPreferences {
  if (typeof window === 'undefined') {
    return {};
  }

  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      // Validate structure
      if (typeof parsed === 'object' && parsed !== null) {
        return parsed as PanelPreferences;
      }
    }
  } catch {
    // Invalid JSON or other error, return empty
  }
  return {};
}

/**
 * Save preferences to localStorage
 */
function savePreferences(preferences: PanelPreferences): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
  } catch {
    // Storage full or other error, silently fail
  }
}

/**
 * Hook for managing panel size and state preferences
 *
 * Automatically persists panel widths and collapsed states to localStorage
 * so they can be restored when the user returns.
 *
 * @example
 * ```tsx
 * function MyLayout() {
 *   const { getPreference, setWidth, setCollapsed } = usePanelPreferences();
 *
 *   const sidebarPref = getPreference('sidebar');
 *
 *   return (
 *     <ResizablePanel
 *       id="sidebar"
 *       defaultWidth={sidebarPref?.width ?? 280}
 *       onResize={(width) => setWidth('sidebar', width)}
 *     >
 *       <SidebarContent />
 *     </ResizablePanel>
 *   );
 * }
 * ```
 */
export function usePanelPreferences(): UsePanelPreferencesReturn {
  const [preferences, setPreferences] = useState<PanelPreferences>(() => loadPreferences());

  // Save to localStorage whenever preferences change
  useEffect(() => {
    savePreferences(preferences);
  }, [preferences]);

  const getPreference = useCallback(
    (panelId: string): PanelPreference | undefined => {
      return preferences[panelId];
    },
    [preferences]
  );

  const setPreference = useCallback((panelId: string, preference: Partial<PanelPreference>) => {
    setPreferences((prev) => ({
      ...prev,
      [panelId]: {
        // Spread existing preference if it exists, otherwise use defaults
        width: 0,
        collapsed: false,
        visible: true,
        ...prev[panelId],
        ...preference,
      },
    }));
  }, []);

  const setWidth = useCallback(
    (panelId: string, width: number) => {
      setPreference(panelId, { width });
    },
    [setPreference]
  );

  const setCollapsed = useCallback(
    (panelId: string, collapsed: boolean) => {
      setPreference(panelId, { collapsed });
    },
    [setPreference]
  );

  const setVisible = useCallback(
    (panelId: string, visible: boolean) => {
      setPreference(panelId, { visible });
    },
    [setPreference]
  );

  const resetPreference = useCallback((panelId: string) => {
    setPreferences((prev) => {
      const next = { ...prev };
      delete next[panelId];
      return next;
    });
  }, []);

  const resetAll = useCallback(() => {
    setPreferences({});
    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, []);

  return useMemo(
    () => ({
      preferences,
      getPreference,
      setPreference,
      setWidth,
      setCollapsed,
      setVisible,
      resetPreference,
      resetAll,
    }),
    [
      preferences,
      getPreference,
      setPreference,
      setWidth,
      setCollapsed,
      setVisible,
      resetPreference,
      resetAll,
    ]
  );
}

/**
 * Hook for a single panel's preferences
 *
 * Convenience wrapper that focuses on a single panel's preferences.
 *
 * @param panelId - The ID of the panel
 * @param defaults - Default values if no preference exists
 *
 * @example
 * ```tsx
 * function Sidebar() {
 *   const { width, collapsed, setWidth, setCollapsed } = useSinglePanelPreference('sidebar', {
 *     width: 280,
 *     collapsed: false,
 *     visible: true,
 *   });
 *
 *   return (
 *     <CollapsiblePanel
 *       expandedWidth={width}
 *       collapsed={collapsed}
 *       onCollapsedChange={setCollapsed}
 *     >
 *       ...
 *     </CollapsiblePanel>
 *   );
 * }
 * ```
 */
export function useSinglePanelPreference(
  panelId: string,
  defaults: PanelPreference = { width: 280, collapsed: false, visible: true }
) {
  const { getPreference, setWidth, setCollapsed, setVisible, resetPreference } =
    usePanelPreferences();

  const preference = getPreference(panelId);

  return useMemo(
    () => ({
      width: preference?.width ?? defaults.width,
      collapsed: preference?.collapsed ?? defaults.collapsed,
      visible: preference?.visible ?? defaults.visible,
      setWidth: (width: number) => setWidth(panelId, width),
      setCollapsed: (collapsed: boolean) => setCollapsed(panelId, collapsed),
      setVisible: (visible: boolean) => setVisible(panelId, visible),
      reset: () => resetPreference(panelId),
    }),
    [
      preference,
      defaults.width,
      defaults.collapsed,
      defaults.visible,
      panelId,
      setWidth,
      setCollapsed,
      setVisible,
      resetPreference,
    ]
  );
}
