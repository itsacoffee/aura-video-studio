import { makeStyles } from '@fluentui/react-components';
import {
  FolderRegular,
  BoxMultipleRegular,
  SettingsRegular,
  HistoryRegular,
  ImageMultipleRegular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import '../../styles/editor-design-tokens.css';
import { StatusBadge } from './StatusBadge';
import { TimelineToolbar } from './TimelineToolbar';
import { VerticalSidebar } from './VerticalSidebar';
import { ViewerEmptyState } from './ViewerEmptyState';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    backgroundColor: 'var(--color-bg-app)',
    color: 'var(--color-text-primary)',
  },
  mainContent: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  centerRegion: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    overflow: 'hidden',
  },
  viewerSection: {
    flex: 1,
    display: 'flex',
    backgroundColor: '#000000',
    borderBottom: '1px solid var(--color-border-subtle)',
  },
  timelineSection: {
    height: '300px',
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: 'var(--color-bg-panel)',
  },
  panel: {
    backgroundColor: 'var(--color-bg-panel)',
    borderBottom: '1px solid var(--color-border-subtle)',
    padding: 'var(--space-lg)',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    height: '44px',
    backgroundColor: 'var(--color-bg-panel-header)',
    borderBottom: '1px solid var(--color-border-subtle)',
    padding: '0 var(--space-lg)',
  },
  toolbarLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-md)',
  },
  toolbarRight: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-md)',
  },
  title: {
    fontSize: 'var(--font-size-lg)',
    fontWeight: 'var(--font-weight-semibold)',
    color: 'var(--color-text-primary)',
  },
});

/**
 * EditorLayoutDemo Component
 *
 * Demonstrates the new workflow-oriented design system and components.
 * This shows how the redesigned editor panels work together.
 */
export function EditorLayoutDemo() {
  const styles = useStyles();
  const [leftSidebarCollapsed, setLeftSidebarCollapsed] = useState(false);
  const [rightSidebarCollapsed, setRightSidebarCollapsed] = useState(false);
  const [activeLeftTab, setActiveLeftTab] = useState<string>('media');
  const [activeRightTab, setActiveRightTab] = useState<string>('properties');
  const [activeTool, setActiveTool] = useState<
    'select' | 'trim' | 'ripple' | 'razor' | 'slip' | 'hand'
  >('select');
  const [rippleMode, setRippleMode] = useState(false);
  const [snapping, setSnapping] = useState(true);
  const [magnetic, setMagnetic] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(50);

  const leftTabs = [
    {
      id: 'media',
      label: 'Media',
      icon: <FolderRegular />,
      isActive: activeLeftTab === 'media',
      shortcut: 'Shift+1',
    },
    {
      id: 'effects',
      label: 'Effects',
      icon: <BoxMultipleRegular />,
      isActive: activeLeftTab === 'effects',
      shortcut: 'Shift+2',
    },
    {
      id: 'assets',
      label: 'Assets',
      icon: <ImageMultipleRegular />,
      isActive: activeLeftTab === 'assets',
      shortcut: 'Shift+3',
    },
  ];

  const rightTabs = [
    {
      id: 'properties',
      label: 'Properties',
      icon: <SettingsRegular />,
      isActive: activeRightTab === 'properties',
      shortcut: 'Shift+4',
    },
    {
      id: 'history',
      label: 'History',
      icon: <HistoryRegular />,
      isActive: activeRightTab === 'history',
      shortcut: 'Shift+5',
    },
  ];

  return (
    <div className={styles.container}>
      {/* Top Toolbar */}
      <div className={styles.toolbar}>
        <div className={styles.toolbarLeft}>
          <span className={styles.title}>Aura Video Studio - Redesign Demo</span>
        </div>
        <div className={styles.toolbarRight}>
          <StatusBadge label="FPS" value="30/30" variant="success" />
          <StatusBadge
            label="Cache"
            value="85%"
            variant="info"
            tooltip="Video cache at 85% capacity"
          />
        </div>
      </div>

      {/* Main Content Area */}
      <div className={styles.mainContent}>
        {/* Left Sidebar */}
        <VerticalSidebar
          tabs={leftTabs}
          activeTabId={activeLeftTab}
          isCollapsed={leftSidebarCollapsed}
          position="left"
          onTabClick={setActiveLeftTab}
          onToggleCollapse={() => setLeftSidebarCollapsed(!leftSidebarCollapsed)}
        />

        {/* Center Region: Viewer + Timeline */}
        <div className={styles.centerRegion}>
          {/* Viewer */}
          <div className={styles.viewerSection}>
            <ViewerEmptyState
              onImportMedia={() => alert('Import media clicked')}
              isDraggingOver={false}
            />
          </div>

          {/* Timeline */}
          <div className={styles.timelineSection}>
            <TimelineToolbar
              activeTool={activeTool}
              onToolChange={setActiveTool}
              rippleMode={rippleMode}
              onRippleModeToggle={() => setRippleMode(!rippleMode)}
              snapping={snapping}
              onSnappingToggle={() => setSnapping(!snapping)}
              magnetic={magnetic}
              onMagneticToggle={() => setMagnetic(!magnetic)}
              timecode="00:00:10 / 00:01:30"
              onTimecodeClick={() => alert('Timecode clicked')}
              zoomLevel={zoomLevel}
              onZoomChange={setZoomLevel}
              onZoomIn={() => setZoomLevel(Math.min(100, zoomLevel + 10))}
              onZoomOut={() => setZoomLevel(Math.max(0, zoomLevel - 10))}
              onZoomFit={() => setZoomLevel(50)}
              onUndo={() => alert('Undo')}
              onRedo={() => alert('Redo')}
            />
            <div className={styles.panel}>
              <p style={{ color: 'var(--color-text-secondary)', textAlign: 'center' }}>
                Timeline tracks would appear here
              </p>
            </div>
          </div>
        </div>

        {/* Right Sidebar */}
        <VerticalSidebar
          tabs={rightTabs}
          activeTabId={activeRightTab}
          isCollapsed={rightSidebarCollapsed}
          position="right"
          onTabClick={setActiveRightTab}
          onToggleCollapse={() => setRightSidebarCollapsed(!rightSidebarCollapsed)}
        />
      </div>
    </div>
  );
}
