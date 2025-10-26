/**
 * PanelTabs Component
 * Tabbed interface for stacked panels with drag-to-reorder support
 */

import { useState, ReactNode } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import {
  Dismiss20Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
  },
  tabList: {
    display: 'flex',
    alignItems: 'center',
    backgroundColor: 'var(--panel-header-bg, var(--color-surface))',
    borderBottom: `1px solid var(--panel-border, ${tokens.colorNeutralStroke1})`,
    gap: '2px',
    padding: '0 var(--space-0)',
    minHeight: '32px',
    userSelect: 'none',
  },
  tab: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-1)',
    height: '30px',
    padding: '0 var(--space-2)',
    fontSize: '13px',
    backgroundColor: 'transparent',
    border: 'none',
    borderRadius: 'var(--border-radius-sm) var(--border-radius-sm) 0 0',
    color: 'var(--color-text-secondary)',
    cursor: 'pointer',
    transition: 'all var(--transition-button)',
    position: 'relative',
    ':hover': {
      backgroundColor: 'var(--panel-hover, var(--color-surface))',
      color: 'var(--color-text-primary)',
    },
  },
  tabActive: {
    backgroundColor: 'var(--panel-bg, var(--color-background))',
    color: 'var(--color-text-primary)',
    fontWeight: 600,
    '::after': {
      content: '""',
      position: 'absolute',
      bottom: 0,
      left: 0,
      right: 0,
      height: '2px',
      backgroundColor: 'var(--color-primary)',
    },
  },
  tabDragging: {
    opacity: 0.5,
  },
  closeButton: {
    minWidth: 'auto',
    width: '16px',
    height: '16px',
    padding: 0,
    borderRadius: '2px',
    opacity: 0,
    transition: 'opacity var(--transition-fast)',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
    },
    '@media (hover: hover)': {
      '.tab:hover &': {
        opacity: 1,
      },
    },
  },
  tabActiveCloseButton: {
    opacity: 0.7,
    ':hover': {
      opacity: 1,
    },
  },
  content: {
    flex: 1,
    overflow: 'auto',
    backgroundColor: 'var(--panel-bg, var(--color-background))',
  },
});

export interface TabItem {
  id: string;
  label: string;
  content: ReactNode;
  closable?: boolean;
}

interface PanelTabsProps {
  tabs: TabItem[];
  defaultActiveTab?: string;
  onTabChange?: (tabId: string) => void;
  onTabClose?: (tabId: string) => void;
  onTabReorder?: (tabs: TabItem[]) => void;
}

export function PanelTabs({
  tabs,
  defaultActiveTab,
  onTabChange,
  onTabClose,
  onTabReorder,
}: PanelTabsProps) {
  const styles = useStyles();
  const [activeTab, setActiveTab] = useState(defaultActiveTab || tabs[0]?.id || '');
  const [draggedTab, setDraggedTab] = useState<string | null>(null);
  const [dragOverTab, setDragOverTab] = useState<string | null>(null);

  const handleTabClick = (tabId: string) => {
    setActiveTab(tabId);
    onTabChange?.(tabId);
  };

  const handleTabClose = (e: React.MouseEvent, tabId: string) => {
    e.stopPropagation();
    onTabClose?.(tabId);
    
    // If closing active tab, switch to another tab
    if (tabId === activeTab) {
      const currentIndex = tabs.findIndex(t => t.id === tabId);
      const nextTab = tabs[currentIndex + 1] || tabs[currentIndex - 1];
      if (nextTab) {
        setActiveTab(nextTab.id);
        onTabChange?.(nextTab.id);
      }
    }
  };

  const handleDragStart = (e: React.DragEvent, tabId: string) => {
    setDraggedTab(tabId);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent, tabId: string) => {
    e.preventDefault();
    if (draggedTab && draggedTab !== tabId) {
      setDragOverTab(tabId);
    }
  };

  const handleDragEnd = () => {
    if (draggedTab && dragOverTab && draggedTab !== dragOverTab) {
      const draggedIndex = tabs.findIndex(t => t.id === draggedTab);
      const targetIndex = tabs.findIndex(t => t.id === dragOverTab);
      
      const newTabs = [...tabs];
      const [removed] = newTabs.splice(draggedIndex, 1);
      newTabs.splice(targetIndex, 0, removed);
      
      onTabReorder?.(newTabs);
    }
    
    setDraggedTab(null);
    setDragOverTab(null);
  };

  const activeTabContent = tabs.find(t => t.id === activeTab)?.content;

  return (
    <div className={styles.container}>
      <div className={styles.tabList}>
        {tabs.map((tab) => (
          <div
            key={tab.id}
            className={`${styles.tab} ${
              tab.id === activeTab ? styles.tabActive : ''
            } ${tab.id === draggedTab ? styles.tabDragging : ''}`}
            onClick={() => handleTabClick(tab.id)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                handleTabClick(tab.id);
              }
            }}
            draggable
            onDragStart={(e) => handleDragStart(e, tab.id)}
            onDragOver={(e) => handleDragOver(e, tab.id)}
            onDragEnd={handleDragEnd}
            role="tab"
            aria-selected={tab.id === activeTab}
            tabIndex={0}
          >
            <span>{tab.label}</span>
            {tab.closable && (
              <Tooltip content="Close tab" relationship="label">
                <Button
                  appearance="subtle"
                  className={`${styles.closeButton} ${
                    tab.id === activeTab ? styles.tabActiveCloseButton : ''
                  }`}
                  icon={<Dismiss20Regular />}
                  onClick={(e) => handleTabClose(e, tab.id)}
                  aria-label={`Close ${tab.label}`}
                />
              </Tooltip>
            )}
          </div>
        ))}
      </div>
      <div className={styles.content} role="tabpanel">
        {activeTabContent}
      </div>
    </div>
  );
}
