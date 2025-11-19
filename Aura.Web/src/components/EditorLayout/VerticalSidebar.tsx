import { makeStyles, Button } from '@fluentui/react-components';
import { ChevronLeft20Regular, ChevronRight20Regular } from '@fluentui/react-icons';
import React, { ReactNode } from 'react';
import '../../styles/editor-design-tokens.css';
import { SidebarTab, SidebarTabProps } from './SidebarTab';

const useStyles = makeStyles({
  sidebar: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: 'var(--color-bg-panel)',
    borderRight: '1px solid var(--color-border-subtle)',
    transition: 'width var(--transition-base)',
    overflow: 'hidden',
  },
  sidebarRight: {
    borderRight: 'none',
    borderLeft: '1px solid var(--color-border-subtle)',
  },
  sidebarExpanded: {
    minWidth: 'var(--sidebar-expanded-width-min)',
  },
  sidebarCollapsed: {
    width: 'var(--sidebar-collapsed-width)',
    minWidth: 'var(--sidebar-collapsed-width)',
  },
  tabList: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    gap: 'var(--space-xs)',
    padding: 'var(--space-sm)',
    overflow: 'auto',
  },
  collapseButtonContainer: {
    padding: 'var(--space-sm)',
    borderTop: '1px solid var(--color-border-subtle)',
  },
  collapseButton: {
    width: '100%',
    minHeight: 'var(--target-size-min)',
    justifyContent: 'center',
    color: 'var(--color-text-secondary)',
    transition: 'all var(--transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
      color: 'var(--color-text-primary)',
    },
  },
  panelContent: {
    flex: 1,
    overflow: 'auto',
    backgroundColor: 'var(--color-bg-panel)',
  },
});

export interface VerticalSidebarProps {
  /** Tabs to display in the sidebar */
  tabs: Omit<SidebarTabProps, 'isCollapsed' | 'onClick'>[];
  /** Currently active tab ID */
  activeTabId: string | null;
  /** Whether the sidebar is collapsed */
  isCollapsed: boolean;
  /** Position of the sidebar */
  position?: 'left' | 'right';
  /** Callback when a tab is clicked */
  onTabClick: (tabId: string) => void;
  /** Callback when collapse button is clicked */
  onToggleCollapse: () => void;
  /** Content to render for the active panel */
  renderPanel?: (tabId: string) => ReactNode;
  /** Additional class name */
  className?: string;
}

/**
 * VerticalSidebar Component
 *
 * A vertical sidebar with tabs for switching between different panels.
 * Fixes the truncated label issue by ensuring full labels are always readable.
 *
 * Features:
 * - Expanded mode: icon + full label + optional shortcut
 * - Collapsed mode: icon only with tooltip
 * - Active state indication with accent border
 * - Smooth transitions between states
 * - Keyboard accessible
 */
export function VerticalSidebar({
  tabs,
  activeTabId,
  isCollapsed,
  position = 'left',
  onTabClick,
  onToggleCollapse,
  className,
}: VerticalSidebarProps) {
  const styles = useStyles();

  const sidebarClassName = [
    styles.sidebar,
    isCollapsed ? styles.sidebarCollapsed : styles.sidebarExpanded,
    position === 'right' && styles.sidebarRight,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  const collapseIcon =
    position === 'left' ? (
      isCollapsed ? (
        <ChevronRight20Regular />
      ) : (
        <ChevronLeft20Regular />
      )
    ) : isCollapsed ? (
      <ChevronLeft20Regular />
    ) : (
      <ChevronRight20Regular />
    );

  const collapseLabel = isCollapsed ? 'Expand sidebar' : 'Collapse sidebar';

  return (
    <div className={sidebarClassName} role="complementary" aria-label={`${position} sidebar`}>
      <div className={styles.tabList} role="tablist">
        {tabs.map((tab) => (
          <SidebarTab
            key={tab.id}
            {...tab}
            isCollapsed={isCollapsed}
            onClick={() => onTabClick(tab.id)}
            isActive={tab.id === activeTabId}
          />
        ))}
      </div>

      <div className={styles.collapseButtonContainer}>
        <Button
          appearance="subtle"
          icon={collapseIcon}
          onClick={onToggleCollapse}
          className={styles.collapseButton}
          aria-label={collapseLabel}
          title={collapseLabel}
        />
      </div>
    </div>
  );
}
