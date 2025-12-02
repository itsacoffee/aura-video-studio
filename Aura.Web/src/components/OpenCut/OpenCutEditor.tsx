/**
 * OpenCut Native Editor
 *
 * A native React implementation of the OpenCut video editor that runs
 * directly in Aura.Web without requiring a separate server process.
 * This replaces the iframe-based approach with integrated components.
 *
 * Redesigned following Apple Human Interface Guidelines for a premium,
 * professional video editing experience with generous spacing, refined
 * typography, and elegant animations.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useEffect, useState, useCallback } from 'react';
import { useOpenCutProjectStore } from '../../stores/opencutProject';
import { MediaPanel } from './MediaPanel';
import { PreviewPanel } from './PreviewPanel';
import { PropertiesPanel } from './PropertiesPanel';
import { ResizablePanel } from './ResizablePanel';
import { Timeline } from './Timeline';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  mainContent: {
    display: 'flex',
    flex: 1,
    minHeight: 0,
    overflow: 'hidden',
  },
  leftPanel: {
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    boxShadow: tokens.shadow4,
    zIndex: 2,
  },
  centerPanel: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minWidth: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    zIndex: 1,
  },
  rightPanel: {
    borderLeft: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    boxShadow: tokens.shadow4,
    zIndex: 2,
  },
});

export function OpenCutEditor() {
  const styles = useStyles();
  const projectStore = useOpenCutProjectStore();

  // Panel sizes state
  const [leftPanelSize, setLeftPanelSize] = useState(320);
  const [rightPanelSize, setRightPanelSize] = useState(320);

  // Initialize project on mount
  useEffect(() => {
    if (!projectStore.activeProject) {
      projectStore.createProject('Untitled Project');
    }
  }, [projectStore]);

  const handleLeftPanelResize = useCallback((size: number) => {
    setLeftPanelSize(size);
  }, []);

  const handleRightPanelResize = useCallback((size: number) => {
    setRightPanelSize(size);
  }, []);

  return (
    <div className={styles.root}>
      {/* Main Content Area */}
      <div className={styles.mainContent}>
        {/* Left Panel - Media Library (Resizable) */}
        <ResizablePanel
          direction="right"
          defaultSize={leftPanelSize}
          minSize={260}
          maxSize={480}
          className={styles.leftPanel}
          onResize={handleLeftPanelResize}
        >
          <MediaPanel />
        </ResizablePanel>

        {/* Center Panel - Preview */}
        <div className={styles.centerPanel}>
          <PreviewPanel />
        </div>

        {/* Right Panel - Properties (Resizable) */}
        <ResizablePanel
          direction="left"
          defaultSize={rightPanelSize}
          minSize={260}
          maxSize={480}
          className={styles.rightPanel}
          onResize={handleRightPanelResize}
        >
          <PropertiesPanel />
        </ResizablePanel>
      </div>

      {/* Timeline (with built-in vertical resize) */}
      <Timeline />
    </div>
  );
}

export default OpenCutEditor;
