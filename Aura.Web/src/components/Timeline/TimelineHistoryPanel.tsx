/**
 * Timeline History Panel Component
 * Shows undo/redo history for timeline operations
 */

import {
  Button,
  Drawer,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  makeStyles,
  Text,
  tokens,
} from '@fluentui/react-components';
import { ArrowRedo24Regular, ArrowUndo24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useUndoManager } from '../../state/undoManager';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  historyList: {
    flex: 1,
    overflowY: 'auto',
    padding: tokens.spacingVerticalS,
  },
  historyItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    marginBottom: tokens.spacingVerticalXS,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    transition: 'background-color 0.2s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  historyItemCurrent: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
  historyItemFuture: {
    opacity: 0.5,
  },
  timestamp: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    padding: tokens.spacingVerticalXXXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
});

interface TimelineHistoryPanelProps {
  isOpen: boolean;
  onClose: () => void;
}

export function TimelineHistoryPanel({ isOpen, onClose }: TimelineHistoryPanelProps) {
  const styles = useStyles();
  const { getHistory, canUndo, canRedo, undo, redo, clear } = useUndoManager();
  const [history] = useState(() => getHistory());

  const handleUndo = async () => {
    await undo();
  };

  const handleRedo = () => {
    redo();
  };

  const handleClear = () => {
    if (confirm('Clear all history? This cannot be undone.')) {
      clear();
      onClose();
    }
  };

  const formatTime = (date: Date): string => {
    return new Intl.DateTimeFormat('en-US', {
      hour: 'numeric',
      minute: 'numeric',
      second: 'numeric',
    }).format(date);
  };

  return (
    <Drawer
      open={isOpen}
      onOpenChange={(_, { open }) => !open && onClose()}
      position="end"
      size="medium"
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={<Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onClose} />}
        >
          Timeline History
        </DrawerHeaderTitle>
      </DrawerHeader>
      <DrawerBody className={styles.root}>
        {history.length === 0 ? (
          <div className={styles.emptyState}>
            <Text size={400}>No history yet</Text>
            <Text size={300}>Actions you perform will appear here</Text>
          </div>
        ) : (
          <>
            <div className={styles.historyList}>
              {history.map((entry, index) => (
                <div
                  key={entry.id}
                  className={`${styles.historyItem} ${index === 0 ? styles.historyItemCurrent : ''}`}
                >
                  <div style={{ flex: 1 }}>
                    <Text weight="semibold">{entry.description}</Text>
                    <div>
                      <Text className={styles.timestamp}>{formatTime(entry.timestamp)}</Text>
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <div className={styles.actions}>
              <Button
                appearance="secondary"
                icon={<ArrowUndo24Regular />}
                disabled={!canUndo}
                onClick={handleUndo}
              >
                Undo
              </Button>
              <Button
                appearance="secondary"
                icon={<ArrowRedo24Regular />}
                disabled={!canRedo}
                onClick={handleRedo}
              >
                Redo
              </Button>
              <Button appearance="secondary" onClick={handleClear} disabled={history.length === 0}>
                Clear All
              </Button>
            </div>
          </>
        )}
      </DrawerBody>
    </Drawer>
  );
}
