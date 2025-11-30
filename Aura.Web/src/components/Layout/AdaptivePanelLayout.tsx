/**
 * Adaptive Panel Layout Component
 *
 * Apple-level intelligent panel layout that adapts to display size,
 * showing/hiding panels and adjusting layout automatically.
 */

import React, { useState, useCallback, useEffect, type ReactNode, type CSSProperties } from 'react';
import { makeStyles, tokens } from '@fluentui/react-components';
import { useAdaptiveLayoutContext } from '../../contexts/AdaptiveLayoutContext';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'row',
    height: '100%',
    width: '100%',
    overflow: 'hidden',
    position: 'relative',
  },
  containerStacked: {
    flexDirection: 'column',
  },
  sidebar: {
    display: 'flex',
    flexDirection: 'column',
    flexShrink: 0,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    transition: 'width 200ms ease-out, transform 200ms ease-out',
    overflow: 'hidden',
    zIndex: 100,
  },
  sidebarOverlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    bottom: 0,
    transform: 'translateX(-100%)',
    boxShadow: tokens.shadow16,
    zIndex: 1000,
    '&[data-open="true"]': {
      transform: 'translateX(0)',
    },
  },
  sidebarBackdrop: {
    position: 'fixed',
    inset: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.4)',
    zIndex: 999,
    opacity: 0,
    visibility: 'hidden',
    transition: 'opacity 200ms ease-out, visibility 200ms ease-out',
    '&[data-visible="true"]': {
      opacity: 1,
      visibility: 'visible',
    },
  },
  sidebarCollapsed: {
    width: 'var(--layout-sidebar-collapsed-width, 48px)',
  },
  mainContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minWidth: 0,
    overflow: 'hidden',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0,
  },
  contentInner: {
    maxWidth: 'var(--layout-content-max-width)',
    marginLeft: 'auto',
    marginRight: 'auto',
    padding: 'var(--layout-content-padding)',
    width: '100%',
    height: '100%',
    boxSizing: 'border-box',
  },
  inspector: {
    display: 'flex',
    flexDirection: 'column',
    flexShrink: 0,
    backgroundColor: tokens.colorNeutralBackground2,
    borderLeft: `1px solid ${tokens.colorNeutralStroke1}`,
    transition: 'width 200ms ease-out, opacity 200ms ease-out',
    overflow: 'hidden',
  },
  inspectorHidden: {
    width: '0 !important',
    opacity: 0,
    visibility: 'hidden',
  },
  inspectorBottom: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    borderLeft: 'none',
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    zIndex: 50,
  },
  timeline: {
    flexShrink: 0,
    backgroundColor: tokens.colorNeutralBackground3,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    transition: 'height 200ms ease-out',
    overflow: 'hidden',
  },
  timelineCollapsed: {
    height: '36px !important',
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
  },
});

/**
 * Props for AdaptivePanelLayout
 */
export interface AdaptivePanelLayoutProps {
  /** Sidebar content (navigation, tools, etc.) */
  sidebar?: ReactNode;
  /** Main content area */
  content: ReactNode;
  /** Inspector/detail panel content */
  inspector?: ReactNode;
  /** Timeline content (for video editor) */
  timeline?: ReactNode;
  /** Whether sidebar is collapsed (controlled) */
  sidebarCollapsed?: boolean;
  /** Callback when sidebar collapse state changes */
  onSidebarCollapsedChange?: (collapsed: boolean) => void;
  /** Whether timeline is collapsed (controlled) */
  timelineCollapsed?: boolean;
  /** Callback when timeline collapse state changes */
  onTimelineCollapsedChange?: (collapsed: boolean) => void;
  /** Additional class name for container */
  className?: string;
  /** Additional inline styles for container */
  style?: CSSProperties;
}

/**
 * Adaptive Panel Layout
 *
 * Intelligently arranges panels based on available screen space,
 * implementing Apple-level adaptive layout behavior.
 *
 * Features:
 * - Automatic sidebar collapse on compact displays
 * - Overlay sidebar on mobile with backdrop
 * - Three-panel layout on expanded displays
 * - Collapsible timeline with smooth transitions
 * - Respects user preferences for collapsed state
 *
 * @example
 * ```tsx
 * <AdaptivePanelLayout
 *   sidebar={<NavigationSidebar />}
 *   content={<MainContent />}
 *   inspector={<DetailInspector />}
 *   timeline={<VideoTimeline />}
 * />
 * ```
 */
export function AdaptivePanelLayout({
  sidebar,
  content,
  inspector,
  timeline,
  sidebarCollapsed: controlledSidebarCollapsed,
  onSidebarCollapsedChange,
  timelineCollapsed: controlledTimelineCollapsed,
  onTimelineCollapsedChange,
  className,
  style,
}: AdaptivePanelLayoutProps): React.ReactElement {
  const styles = useStyles();
  const layout = useAdaptiveLayoutContext();
  const { display } = layout;

  // Internal state for uncontrolled mode
  const [internalSidebarCollapsed, setInternalSidebarCollapsed] = useState(false);
  const [internalTimelineCollapsed, setInternalTimelineCollapsed] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(false); // For overlay mode

  // Determine actual collapsed states
  const sidebarCollapsed =
    controlledSidebarCollapsed !== undefined
      ? controlledSidebarCollapsed
      : internalSidebarCollapsed;
  const timelineCollapsed =
    controlledTimelineCollapsed !== undefined
      ? controlledTimelineCollapsed
      : internalTimelineCollapsed;

  // Handlers
  const handleSidebarCollapse = useCallback(
    (collapsed: boolean) => {
      if (onSidebarCollapsedChange) {
        onSidebarCollapsedChange(collapsed);
      } else {
        setInternalSidebarCollapsed(collapsed);
      }
    },
    [onSidebarCollapsedChange]
  );

  const handleTimelineCollapse = useCallback(
    (collapsed: boolean) => {
      if (onTimelineCollapsedChange) {
        onTimelineCollapsedChange(collapsed);
      } else {
        setInternalTimelineCollapsed(collapsed);
      }
    },
    [onTimelineCollapsedChange]
  );

  // Auto-collapse sidebar on compact displays
  useEffect(() => {
    if (display.sizeClass === 'compact' && !sidebarCollapsed) {
      handleSidebarCollapse(true);
    }
  }, [display.sizeClass, sidebarCollapsed, handleSidebarCollapse]);

  // Layout decisions based on display environment
  const showSidebarExpanded = display.sizeClass !== 'compact' && !sidebarCollapsed;
  const sidebarIsOverlay = layout.sidebar.isOverlay;
  const showInspectorInline =
    display.panelLayout === 'three-panel' && layout.inspector.visible && inspector;
  const showTimelineCollapsed = timelineCollapsed || display.preferCompactControls;

  // Compute sidebar width
  const sidebarWidth =
    showSidebarExpanded && !sidebarIsOverlay
      ? layout.sidebar.width === 'collapsed'
        ? layout.sidebar.collapsedWidth
        : layout.sidebar.width
      : layout.sidebar.collapsedWidth;

  // Compute inspector width
  const inspectorWidth = showInspectorInline ? layout.inspector.width : 0;

  // Compute timeline height
  const timelineHeight =
    timeline && !showTimelineCollapsed
      ? layout.timeline.height === 'auto'
        ? 'auto'
        : layout.timeline.height
      : timeline
        ? 36
        : 0;

  const containerClasses = [
    styles.container,
    display.panelLayout === 'stacked' ? styles.containerStacked : '',
    className || '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div
      className={containerClasses}
      style={
        {
          '--panel-sidebar-width': `${sidebarWidth}px`,
          '--panel-inspector-width': `${inspectorWidth}px`,
          '--panel-timeline-height':
            typeof timelineHeight === 'number' ? `${timelineHeight}px` : timelineHeight,
          ...style,
        } as CSSProperties
      }
      data-size-class={display.sizeClass}
      data-panel-layout={display.panelLayout}
    >
      {/* Sidebar backdrop for overlay mode */}
      {sidebarIsOverlay && sidebar && (
        <div
          className={styles.sidebarBackdrop}
          data-visible={sidebarOpen}
          onClick={() => setSidebarOpen(false)}
          role="button"
          tabIndex={-1}
          aria-label="Close sidebar"
          onKeyDown={(e) => {
            if (e.key === 'Escape') setSidebarOpen(false);
          }}
        />
      )}

      {/* Sidebar */}
      {sidebar && (
        <aside
          className={`${styles.sidebar} ${
            sidebarIsOverlay ? styles.sidebarOverlay : ''
          } ${!showSidebarExpanded && !sidebarIsOverlay ? styles.sidebarCollapsed : ''}`}
          style={
            !sidebarIsOverlay
              ? {
                  width:
                    showSidebarExpanded && layout.sidebar.width !== 'collapsed'
                      ? `${layout.sidebar.width}px`
                      : `${layout.sidebar.collapsedWidth}px`,
                }
              : { width: `${layout.sidebar.width === 'collapsed' ? 280 : layout.sidebar.width}px` }
          }
          data-expanded={showSidebarExpanded || (sidebarIsOverlay && sidebarOpen)}
          data-open={sidebarIsOverlay ? sidebarOpen : undefined}
          aria-label="Sidebar"
        >
          {sidebar}
        </aside>
      )}

      {/* Main content area */}
      <div className={styles.mainContent}>
        <main className={styles.content}>
          <div className={styles.contentInner}>{content}</div>
        </main>

        {/* Timeline */}
        {timeline && (
          <footer
            className={`${styles.timeline} ${showTimelineCollapsed ? styles.timelineCollapsed : ''}`}
            style={{
              height: typeof timelineHeight === 'number' ? `${timelineHeight}px` : timelineHeight,
            }}
            data-collapsed={showTimelineCollapsed}
            aria-label="Timeline"
          >
            {showTimelineCollapsed ? (
              <button
                type="button"
                onClick={() => handleTimelineCollapse(false)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    handleTimelineCollapse(false);
                  }
                }}
                aria-label="Expand timeline"
                style={{
                  width: '100%',
                  height: '100%',
                  background: 'transparent',
                  border: 'none',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                {timeline}
              </button>
            ) : (
              timeline
            )}
          </footer>
        )}
      </div>

      {/* Inspector panel */}
      {inspector && (
        <aside
          className={`${styles.inspector} ${!showInspectorInline ? styles.inspectorHidden : ''} ${
            layout.inspector.position === 'bottom' ? styles.inspectorBottom : ''
          }`}
          style={{
            width: showInspectorInline ? `${layout.inspector.width}px` : 0,
          }}
          data-visible={showInspectorInline}
          aria-label="Inspector"
          aria-hidden={!showInspectorInline}
        >
          {inspector}
        </aside>
      )}
    </div>
  );
}

export default AdaptivePanelLayout;
