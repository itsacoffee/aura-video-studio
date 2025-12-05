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

import { makeStyles, tokens, TabList, Tab } from '@fluentui/react-components';
import { useEffect, useState, useCallback } from 'react';
import { useOpenCutKeyboardHandler } from '../../hooks/useOpenCutKeyboardHandler';
import { useOpenCutProjectStore } from '../../stores/opencutProject';
import { openCutTokens } from '../../styles/designTokens';
import { EffectsPanel } from './Effects';
import { MediaPanel } from './MediaPanel';
import { PreviewPanel } from './PreviewPanel';
import { PropertiesPanel } from './PropertiesPanel';
import { ResizablePanel } from './ResizablePanel';
import { Timeline } from './Timeline';
import { TransitionsPanel } from './Transitions';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
    fontFamily: openCutTokens.typography.fontFamily.body,
    fontSize: openCutTokens.typography.fontSize.base,
    lineHeight: openCutTokens.typography.lineHeight.normal.toString(),
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
    boxShadow: openCutTokens.shadows.sm,
    zIndex: openCutTokens.zIndex.dropdown,
  },
  leftPanelTabs: {
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
  },
  leftPanelContent: {
    flex: 1,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
  },
  centerPanel: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minWidth: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    zIndex: openCutTokens.zIndex.base,
  },
  rightPanel: {
    borderLeft: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    boxShadow: openCutTokens.shadows.sm,
    zIndex: openCutTokens.zIndex.dropdown,
  },
});

type LeftPanelTab = 'media' | 'effects' | 'transitions';

export function OpenCutEditor() {
  const styles = useStyles();
  const projectStore = useOpenCutProjectStore();

  // Panel sizes state
  const [leftPanelSize, setLeftPanelSize] = useState(320);
  const [rightPanelSize, setRightPanelSize] = useState(320);
  const [leftPanelTab, setLeftPanelTab] = useState<LeftPanelTab>('media');

  // Initialize keyboard shortcuts handler
  // In/out points are returned but will be used by future components
  const _keyboardState = useOpenCutKeyboardHandler({ enabled: true });

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

  const handleLeftPanelTabChange = useCallback((_: unknown, data: { value: unknown }) => {
    setLeftPanelTab(data.value as LeftPanelTab);
  }, []);

  const renderLeftPanelContent = () => {
    switch (leftPanelTab) {
      case 'media':
        return <MediaPanel />;
      case 'effects':
        return <EffectsPanel />;
      case 'transitions':
        return <TransitionsPanel />;
      default:
        return <MediaPanel />;
    }
  };

  return (
    <div className={styles.root}>
      {/* Main Content Area */}
      <div className={styles.mainContent}>
        {/* Left Panel - Media/Effects/Transitions (Resizable) */}
        <ResizablePanel
          direction="right"
          defaultSize={leftPanelSize}
          minSize={260}
          maxSize={480}
          className={styles.leftPanel}
          onResize={handleLeftPanelResize}
        >
          <div className={styles.leftPanelTabs}>
            <TabList
              selectedValue={leftPanelTab}
              onTabSelect={handleLeftPanelTabChange}
              size="small"
            >
              <Tab value="media">Media</Tab>
              <Tab value="effects">Effects</Tab>
              <Tab value="transitions">Transitions</Tab>
            </TabList>
          </div>
          <div className={styles.leftPanelContent}>{renderLeftPanelContent()}</div>
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
