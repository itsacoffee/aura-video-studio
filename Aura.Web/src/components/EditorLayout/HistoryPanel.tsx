/**
 * History Panel - Shows recent operations with undo/redo capabilities
 */

import { makeStyles, tokens, Button, Text, Title3, Divider } from '@fluentui/react-components';
import { History24Regular, ArrowUndo24Regular, ArrowRedo24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { CommandHistory } from '../../services/commandHistory';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalM,
    gap: tokens.spacingHorizontalS,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  historyList: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalS,
  },
  historyItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    cursor: 'pointer',
    borderRadius: tokens.borderRadiusMedium,
    transition: 'background-color 0.2s',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground2Hover,
    },
  },
  historyItemActive: {
    backgroundColor: tokens.colorNeutralBackground2Selected,
  },
  historyItemText: {
    flex: 1,
  },
  historyItemTime: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
  },
});

interface HistoryPanelProps {
  commandHistory: CommandHistory;
}

export function HistoryPanel({ commandHistory }: HistoryPanelProps) {
  const styles = useStyles();
  const [history, setHistory] = useState<Array<{ description: string; timestamp: Date }>>([]);
  const [canUndo, setCanUndo] = useState(false);
  const [canRedo, setCanRedo] = useState(false);

  // Update history when commands are executed
  useEffect(() => {
    const updateHistory = () => {
      setHistory(commandHistory.getUndoHistory());
    };

    const unsubscribe = commandHistory.subscribe((undo, redo) => {
      setCanUndo(undo);
      setCanRedo(redo);
      updateHistory();
    });

    updateHistory();
    return unsubscribe;
  }, [commandHistory]);

  const handleUndo = () => {
    commandHistory.undo();
  };

  const handleRedo = () => {
    commandHistory.redo();
  };

  const formatTime = (timestamp: Date): string => {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (seconds < 60) {
      return 'just now';
    } else if (minutes < 60) {
      return `${minutes}m ago`;
    } else if (hours < 24) {
      return `${hours}h ago`;
    } else {
      return timestamp.toLocaleDateString();
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <History24Regular />
          <Title3>History</Title3>
        </div>
        <div className={styles.actions}>
          <Button
            appearance="subtle"
            icon={<ArrowUndo24Regular />}
            disabled={!canUndo}
            onClick={handleUndo}
            title={canUndo ? `Undo: ${commandHistory.getUndoDescription()}` : 'Nothing to undo'}
          />
          <Button
            appearance="subtle"
            icon={<ArrowRedo24Regular />}
            disabled={!canRedo}
            onClick={handleRedo}
            title={canRedo ? `Redo: ${commandHistory.getRedoDescription()}` : 'Nothing to redo'}
          />
        </div>
      </div>
      <Divider />

      {history.length === 0 ? (
        <div className={styles.emptyState}>
          <History24Regular style={{ fontSize: '48px' }} />
          <Text>No actions yet</Text>
          <Text size={200}>Your editing history will appear here</Text>
        </div>
      ) : (
        <div className={styles.historyList}>
          {history.map((item, index) => (
            <div key={`${item.timestamp.getTime()}-${index}`} className={styles.historyItem}>
              <div className={styles.historyItemText}>
                <Text weight="semibold">{item.description}</Text>
                <div className={styles.historyItemTime}>{formatTime(item.timestamp)}</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
