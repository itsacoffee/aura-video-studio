import { makeStyles } from '@fluentui/react-components';
import React, { ReactNode, useState, useEffect, useCallback } from 'react';
import { snapToBreakpoint } from '../../services/workspaceLayoutService';
import { useWorkspaceLayoutStore } from '../../state/workspaceLayout';
import { TopMenuBar } from '../Layout/TopMenuBar';
import { MenuBar } from '../MenuBar/MenuBar';
import { PanelHeader } from './PanelHeader';
import '../../styles/video-editor-theme.css';

// Panel region types - Premiere-style layout structure
export type PanelRegion = 'top' | 'bottom' | 'right';

// Panel configuration interface
export interface EditorLayoutPanelConfig {
  id: string;
  title: string;
  icon?: ReactNode;
  defaultSize?: number; // width in pixels for right panels, percentage for top/bottom
  minSize?: number;
  maxSize?: number;
  region: PanelRegion;
  visibleByDefault?: boolean;
}

// Constants
const COLLAPSED_PANEL_WIDTH = 48;

const useStyles = makeStyles({
  // Shell container - top-level editor structure
  editorShell: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
    backgroundColor: 'var(--editor-bg-primary)',
  },
  fullscreenContainer: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 9999,
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
    backgroundColor: 'var(--editor-bg-primary)',
  },
  // Main content area below menu bar
  editorContent: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  // Left sidebar (media, effects)
  leftSidebar: {
    display: 'flex',
    overflow: 'hidden',
  },
  // Center region (preview + timeline stack)
  centerRegion: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflow: 'hidden',
  },
  // Right sidebar (properties, history stack)
  rightSidebar: {
    display: 'flex',
    overflow: 'hidden',
  },
  // Top region panel (preview)
  topRegionPanel: {
    minHeight: '300px',
    display: 'flex',
    flexDirection: 'column',
    borderBottom: `1px solid var(--editor-panel-border-subtle)`,
    backgroundColor: 'var(--editor-bg-secondary)',
    overflow: 'hidden',
    transition: 'flex var(--editor-transition-base)',
  },
  // Bottom region panel (timeline)
  bottomRegionPanel: {
    minHeight: '200px',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: 'var(--timeline-bg)',
    overflow: 'hidden',
    transition: 'flex var(--editor-transition-base)',
  },
  // Right sidebar panel - Increased min width to prevent text cutoff
  rightSidebarPanel: {
    minWidth: '320px',
    maxWidth: '450px',
    borderLeft: `1px solid var(--editor-panel-border-subtle)`,
    backgroundColor: 'var(--editor-panel-bg)',
    overflow: 'auto',
    display: 'flex',
    flexDirection: 'column',
    transition: 'width var(--editor-transition-base)',
  },
  // Left sidebar panel - Increased min width for better readability and prevent clipping
  leftSidebarPanel: {
    minWidth: '300px', // Increased from 280px to prevent text/file name clipping
    maxWidth: '450px', // Increased from 400px for better flexibility
    borderRight: `1px solid var(--editor-panel-border-subtle)`,
    backgroundColor: 'var(--editor-panel-bg)',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    transition: 'width var(--editor-transition-base)',
  },
  panelCollapsed: {
    width: `${COLLAPSED_PANEL_WIDTH}px !important`,
    minWidth: `${COLLAPSED_PANEL_WIDTH}px !important`,
  },
  // Vertical resizer (for horizontal panel separation)
  dividerVertical: {
    width: '4px',
    cursor: 'ew-resize',
    backgroundColor: 'transparent',
    position: 'relative',
    transition: 'background-color var(--editor-transition-fast)',
    '&::after': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: '50%',
      transform: 'translateX(-50%)',
      width: '2px',
      height: '100%',
      backgroundColor: 'var(--editor-panel-border)',
      transition: 'all var(--editor-transition-fast)',
    },
    '&:hover::after': {
      backgroundColor: 'var(--editor-accent)',
      boxShadow: '0 0 4px var(--editor-focus-ring)',
      width: '3px',
    },
    '&:active::after': {
      backgroundColor: 'var(--editor-accent)',
    },
    '&:focus': {
      outline: `2px solid var(--editor-accent)`,
      outlineOffset: '2px',
    },
  },
  dividerDragging: {
    '&::after': {
      backgroundColor: 'var(--editor-accent)',
      boxShadow: '0 0 6px var(--editor-focus-ring)',
      width: '3px',
    },
  },
  // Horizontal resizer (for vertical panel separation)
  dividerHorizontal: {
    height: '4px',
    cursor: 'ns-resize',
    backgroundColor: 'transparent',
    position: 'relative',
    transition: 'background-color var(--editor-transition-fast)',
    '&::after': {
      content: '""',
      position: 'absolute',
      left: 0,
      top: '50%',
      transform: 'translateY(-50%)',
      width: '100%',
      height: '2px',
      backgroundColor: 'var(--editor-panel-border)',
      transition: 'all var(--editor-transition-fast)',
    },
    '&:hover::after': {
      backgroundColor: 'var(--editor-accent)',
      boxShadow: '0 0 4px var(--editor-focus-ring)',
      height: '3px',
    },
    '&:active::after': {
      backgroundColor: 'var(--editor-accent)',
    },
    '&:focus': {
      outline: `2px solid var(--editor-accent)`,
      outlineOffset: '2px',
    },
  },
  dividerHorizontalDragging: {
    '&::after': {
      backgroundColor: 'var(--editor-accent)',
      boxShadow: '0 0 6px var(--editor-focus-ring)',
      height: '3px',
    },
  },
});

// New interface using panel configuration
export interface EditorLayoutProps {
  panels: EditorLayoutPanelConfig[];
  renderPanel: (id: string) => React.ReactNode;
  onImportMedia?: () => void;
  onExportVideo?: () => void;
  onShowKeyboardShortcuts?: () => void;
  onSaveProject?: () => void;
  projectName?: string | null;
  isDirty?: boolean;
  autosaveStatus?: 'idle' | 'saving' | 'saved' | 'error';
  lastSaved?: Date | null;
  useTopMenuBar?: boolean;
}

// LocalStorage key prefix for panel sizes
const STORAGE_KEY_PREFIX = 'aura-editor-panel-';

// Helper to load from localStorage with default fallback
const loadPanelSize = (panelId: string, defaultValue: number): number => {
  try {
    const stored = localStorage.getItem(`${STORAGE_KEY_PREFIX}${panelId}`);
    return stored ? parseFloat(stored) : defaultValue;
  } catch {
    return defaultValue;
  }
};

// Helper to save to localStorage
const savePanelSize = (panelId: string, value: number): void => {
  try {
    localStorage.setItem(`${STORAGE_KEY_PREFIX}${panelId}`, value.toString());
  } catch {
    // Ignore localStorage errors
  }
};

export function EditorLayout({
  panels,
  renderPanel,
  onImportMedia,
  onExportVideo,
  onShowKeyboardShortcuts,
  onSaveProject,
  projectName,
  isDirty,
  autosaveStatus = 'idle',
  lastSaved,
  useTopMenuBar = false,
}: EditorLayoutProps) {
  const styles = useStyles();
  const {
    isFullscreen,
    exitFullscreen,
    collapsedPanels,
    visiblePanels,
    togglePanelCollapsed,
    getCurrentLayout,
    currentLayoutId,
  } = useWorkspaceLayoutStore();

  // Group panels by region and filter by visibility
  // Preview and timeline are always visible (critical panels)
  const topPanels = panels.filter((p) => p.region === 'top');
  const bottomPanels = panels.filter((p) => p.region === 'bottom');
  const rightPanels = panels.filter((p) => {
    if (p.region !== 'right') return false;
    // Check if panel should be visible based on visiblePanels state
    const panelId = p.id as keyof typeof visiblePanels;
    if (panelId in visiblePanels) {
      return visiblePanels[panelId];
    }
    return true; // Show by default if not in visiblePanels
  });

  // Load current layout or use defaults
  const currentLayout = getCurrentLayout();

  // State for panel sizes - keyed by panel ID
  const [panelSizes, setPanelSizes] = useState<Record<string, number>>(() => {
    const sizes: Record<string, number> = {};
    panels.forEach((panel) => {
      const layoutSize =
        currentLayout?.panelSizes[`${panel.id}Width` as keyof typeof currentLayout.panelSizes];
      const defaultSize = panel.defaultSize ?? (panel.region === 'right' ? 340 : 60);
      sizes[panel.id] = loadPanelSize(panel.id, (layoutSize as number) ?? defaultSize);
    });
    return sizes;
  });

  // Track dragging state for visual feedback
  const [isDraggingHorizontal, setIsDraggingHorizontal] = useState(false);
  const [isDraggingVertical, setIsDraggingVertical] = useState(false);

  // Get current panel sizes for saving workspace
  const getCurrentPanelSizes = useCallback(() => {
    return {
      propertiesWidth: panelSizes['properties'] ?? 340,
      mediaLibraryWidth: panelSizes['mediaLibrary'] ?? 300,
      effectsLibraryWidth: panelSizes['effects'] ?? 300,
      historyWidth: panelSizes['history'] ?? 340,
      previewHeight: panelSizes['preview'] ?? 60,
    };
  }, [panelSizes]);

  // Handle ESC key to exit fullscreen
  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isFullscreen) {
        exitFullscreen();
      }
    },
    [isFullscreen, exitFullscreen]
  );

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [handleKeyDown]);

  // Listen for fullscreen change events to sync with browser fullscreen state
  useEffect(() => {
    const handleFullscreenChange = () => {
      const isInFullscreen = !!document.fullscreenElement;
      if (isInFullscreen !== isFullscreen) {
        useWorkspaceLayoutStore.setState({ isFullscreen: isInFullscreen });
      }
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, [isFullscreen]);

  // Persist panel sizes to localStorage
  useEffect(() => {
    Object.entries(panelSizes).forEach(([panelId, size]) => {
      savePanelSize(panelId, size);
    });
  }, [panelSizes]);

  // React to layout changes and apply panel sizes from the layout
  useEffect(() => {
    const layout = getCurrentLayout();
    if (layout) {
      const newSizes: Record<string, number> = {};
      panels.forEach((panel) => {
        const layoutSize = layout.panelSizes[`${panel.id}Width` as keyof typeof layout.panelSizes];
        if (layoutSize) {
          newSizes[panel.id] = layoutSize as number;
        }
      });
      if (Object.keys(newSizes).length > 0) {
        setPanelSizes((prev) => ({ ...prev, ...newSizes }));
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentLayoutId]);

  // Generic resize handler for vertical dividers (horizontal panel separation)
  const handleVerticalDividerResize =
    (panelId: string, minSize: number, maxSize: number, direction: 'left' | 'right') =>
    (e: React.MouseEvent) => {
      const startX = e.clientX;
      const startWidth = panelSizes[panelId] ?? 320;
      setIsDraggingHorizontal(true);

      const handleMouseMove = (moveEvent: MouseEvent) => {
        const delta =
          direction === 'left' ? moveEvent.clientX - startX : startX - moveEvent.clientX;
        let newWidth = Math.max(minSize, Math.min(maxSize, startWidth + delta));
        newWidth = snapToBreakpoint(newWidth, minSize, maxSize);
        setPanelSizes((prev) => ({ ...prev, [panelId]: newWidth }));
      };

      const handleMouseUp = () => {
        setIsDraggingHorizontal(false);
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    };

  // Resize handler for horizontal divider (vertical panel separation - preview/timeline)
  const handleHorizontalDividerResize = (topPanelId: string) => (e: React.MouseEvent) => {
    const container = (e.target as HTMLElement).parentElement?.parentElement;
    if (!container) return;

    const startY = e.clientY;
    const containerHeight = container.clientHeight;
    const startHeight = panelSizes[topPanelId] ?? 60;
    setIsDraggingVertical(true);

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const delta = moveEvent.clientY - startY;
      const deltaPercent = (delta / containerHeight) * 100;
      let newHeight = Math.max(40, Math.min(80, startHeight + deltaPercent));
      // Apply snap-to-breakpoint for percentage values
      const snapPoints = [40, 50, 60, 66, 70, 75, 80];
      const threshold = 3; // 3% threshold
      for (const point of snapPoints) {
        if (Math.abs(newHeight - point) < threshold) {
          newHeight = point;
          break;
        }
      }
      setPanelSizes((prev) => ({ ...prev, [topPanelId]: newHeight }));
    };

    const handleMouseUp = () => {
      setIsDraggingVertical(false);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  // Keyboard resize handler for vertical dividers
  const handleVerticalDividerKeyboard =
    (panelId: string, minSize: number, maxSize: number, direction: 'left' | 'right') =>
    (e: React.KeyboardEvent) => {
      if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
        e.preventDefault();
        const delta = (e.key === 'ArrowLeft') === (direction === 'left') ? -10 : 10;
        setPanelSizes((prev) => {
          const currentSize = prev[panelId] ?? 340;
          const newSize = Math.max(minSize, Math.min(maxSize, currentSize + delta));
          savePanelSize(panelId, newSize);
          return { ...prev, [panelId]: newSize };
        });
      }
    };

  // Keyboard resize handler for horizontal divider
  const handleHorizontalDividerKeyboard = (panelId: string) => (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
      e.preventDefault();
      const delta = e.key === 'ArrowUp' ? 5 : -5;
      setPanelSizes((prev) => {
        const currentSize = prev[panelId] ?? 60;
        const newSize = Math.max(40, Math.min(80, currentSize + delta));
        savePanelSize(panelId, newSize);
        return { ...prev, [panelId]: newSize };
      });
    }
  };

  // Helper to get panel modifier class
  const getPanelModifierClass = (panelId: string): string => {
    const modifierMap: Record<string, string> = {
      preview: 'aura-editor-panel--preview',
      timeline: 'aura-editor-panel--timeline',
      properties: 'aura-editor-panel--properties',
      mediaLibrary: 'aura-editor-panel--library',
      effects: 'aura-editor-panel--effects',
      history: 'aura-editor-panel--history',
    };
    return modifierMap[panelId] || '';
  };

  // Render panel with header and collapse functionality
  const renderPanelWithHeader = (panel: EditorLayoutPanelConfig) => {
    const isCollapsed = collapsedPanels[panel.id as keyof typeof collapsedPanels] ?? false;
    const content = renderPanel(panel.id);
    const panelModifier = getPanelModifierClass(panel.id);

    return (
      <div key={panel.id} className={`aura-editor-panel ${panelModifier}`}>
        <PanelHeader
          title={panel.title}
          isCollapsed={isCollapsed}
          onToggleCollapse={() => togglePanelCollapsed(panel.id as keyof typeof collapsedPanels)}
        />
        {!isCollapsed && <div className="aura-editor-panel__body">{content}</div>}
      </div>
    );
  };

  // Render vertical divider (for horizontal panel separation)
  const renderVerticalDivider = (
    panelId: string,
    minSize: number,
    maxSize: number,
    direction: 'left' | 'right'
  ) => (
    // Interactive resizer - intentionally uses mouse and keyboard events
    // eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions
    <div
      className={`${styles.dividerVertical} ${isDraggingHorizontal ? styles.dividerDragging : ''}`}
      onMouseDown={handleVerticalDividerResize(panelId, minSize, maxSize, direction)}
      role="separator"
      aria-orientation="vertical"
      aria-label={`Resize ${panelId} panel`}
      // Separator role is interactive and requires tabIndex for keyboard accessibility
      // eslint-disable-next-line jsx-a11y/no-noninteractive-tabindex
      tabIndex={0}
      onKeyDown={handleVerticalDividerKeyboard(panelId, minSize, maxSize, direction)}
    />
  );

  // Render horizontal divider (for vertical panel separation)
  const renderHorizontalDivider = (topPanelId: string) => (
    // Interactive resizer - intentionally uses mouse and keyboard events
    // eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions
    <div
      className={`${styles.dividerHorizontal} ${isDraggingVertical ? styles.dividerHorizontalDragging : ''}`}
      onMouseDown={handleHorizontalDividerResize(topPanelId)}
      role="separator"
      aria-orientation="horizontal"
      aria-label={`Resize ${topPanelId} panel`}
      // Separator role is interactive and requires tabIndex for keyboard accessibility
      // eslint-disable-next-line jsx-a11y/no-noninteractive-tabindex
      tabIndex={0}
      onKeyDown={handleHorizontalDividerKeyboard(topPanelId)}
    />
  );

  // Determine if panel is in left sidebar (media, effects)
  const isLeftSidebarPanel = (panel: EditorLayoutPanelConfig) => {
    return panel.id === 'mediaLibrary' || panel.id === 'effects';
  };

  // Get left sidebar panels (order: media, effects)
  const leftSidebarPanels = rightPanels.filter(isLeftSidebarPanel).sort((a, b) => {
    const order = ['mediaLibrary', 'effects'];
    return order.indexOf(a.id) - order.indexOf(b.id);
  });

  // Get right sidebar panels (order: properties, history)
  const rightSidebarPanels = rightPanels
    .filter((p) => !isLeftSidebarPanel(p))
    .sort((a, b) => {
      const order = ['properties', 'history'];
      return order.indexOf(a.id) - order.indexOf(b.id);
    });

  return (
    <div className={isFullscreen ? styles.fullscreenContainer : styles.editorShell}>
      {useTopMenuBar ? (
        <TopMenuBar
          onImportMedia={onImportMedia}
          onExportVideo={onExportVideo}
          onSaveProject={onSaveProject}
          onShowKeyboardShortcuts={onShowKeyboardShortcuts}
          getCurrentPanelSizes={getCurrentPanelSizes}
        />
      ) : (
        <MenuBar
          onImportMedia={onImportMedia}
          onExportVideo={onExportVideo}
          onShowKeyboardShortcuts={onShowKeyboardShortcuts}
          onSaveProject={onSaveProject}
          projectName={projectName}
          isDirty={isDirty}
          autosaveStatus={autosaveStatus}
          lastSaved={lastSaved}
        />
      )}
      <div className={styles.editorContent}>
        {/* Left Sidebar - Media Library + Effects */}
        {leftSidebarPanels.length > 0 && (
          <div className={styles.leftSidebar}>
            {leftSidebarPanels.map((panel, index) => {
              const isCollapsed =
                collapsedPanels[panel.id as keyof typeof collapsedPanels] ?? false;
              const minSize = panel.minSize ?? 300; // Increased default min size
              const maxSize = panel.maxSize ?? 450; // Increased default max size
              const panelWidth = panelSizes[panel.id] ?? panel.defaultSize ?? 320; // Increased default width

              return (
                <React.Fragment key={panel.id}>
                  <div
                    className={`${styles.leftSidebarPanel} aura-editor-region aura-editor-region--sidebar ${isCollapsed ? styles.panelCollapsed : ''}`}
                    style={{
                      width: isCollapsed ? `${COLLAPSED_PANEL_WIDTH}px` : `${panelWidth}px`,
                    }}
                  >
                    {renderPanelWithHeader(panel)}
                  </div>
                  {index < leftSidebarPanels.length - 1 &&
                    renderVerticalDivider(panel.id, minSize, maxSize, 'left')}
                </React.Fragment>
              );
            })}
            {/* Divider between left sidebar and center */}
            {leftSidebarPanels.length > 0 && (
              <div
                className={`${styles.dividerVertical} ${isDraggingHorizontal ? styles.dividerDragging : ''}`}
                style={{ cursor: 'ew-resize' }}
              />
            )}
          </div>
        )}

        {/* Center Region - Preview (top) + Timeline (bottom) */}
        <div className={styles.centerRegion}>
          {/* Top Region - Preview */}
          {topPanels.length > 0 &&
            topPanels.map((panel) => {
              const previewHeight = panelSizes[panel.id] ?? panel.defaultSize ?? 60;
              return (
                <React.Fragment key={panel.id}>
                  <div
                    className={`${styles.topRegionPanel} aura-editor-region aura-editor-region--top`}
                    style={{ flex: previewHeight }}
                  >
                    <div className="aura-editor-panel aura-editor-panel--preview">
                      {renderPanel(panel.id)}
                    </div>
                  </div>
                  {bottomPanels.length > 0 && renderHorizontalDivider(panel.id)}
                </React.Fragment>
              );
            })}

          {/* Bottom Region - Timeline */}
          {bottomPanels.length > 0 &&
            bottomPanels.map((panel) => {
              const previewHeight = panelSizes[topPanels[0]?.id] ?? 60;
              return (
                <div
                  key={panel.id}
                  className={`${styles.bottomRegionPanel} aura-editor-region aura-editor-region--bottom`}
                  style={{ flex: 100 - previewHeight }}
                >
                  <div className="aura-editor-panel aura-editor-panel--timeline">
                    {renderPanel(panel.id)}
                  </div>
                </div>
              );
            })}
        </div>

        {/* Right Sidebar - Properties + History */}
        {rightSidebarPanels.length > 0 && (
          <div className={styles.rightSidebar}>
            {/* Divider between center and right sidebar */}
            <div
              className={`${styles.dividerVertical} ${isDraggingHorizontal ? styles.dividerDragging : ''}`}
              style={{ cursor: 'ew-resize' }}
            />
            {rightSidebarPanels.map((panel, index) => {
              const isCollapsed =
                collapsedPanels[panel.id as keyof typeof collapsedPanels] ?? false;
              const minSize = panel.minSize ?? 320;
              const maxSize = panel.maxSize ?? 450;
              const panelWidth = panelSizes[panel.id] ?? panel.defaultSize ?? 340;

              return (
                <React.Fragment key={panel.id}>
                  {index > 0 && renderVerticalDivider(panel.id, minSize, maxSize, 'right')}
                  <div
                    className={`${styles.rightSidebarPanel} aura-editor-region aura-editor-region--sidebar ${isCollapsed ? styles.panelCollapsed : ''}`}
                    style={{
                      width: isCollapsed ? `${COLLAPSED_PANEL_WIDTH}px` : `${panelWidth}px`,
                    }}
                  >
                    {renderPanelWithHeader(panel)}
                  </div>
                </React.Fragment>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
