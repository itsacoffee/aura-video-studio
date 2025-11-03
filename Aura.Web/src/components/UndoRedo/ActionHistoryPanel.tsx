/**
 * Action History Panel
 * Displays the undo/redo history with timestamps and actions
 */

import {
  Drawer,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  Button,
  Text,
} from '@fluentui/react-components';
import { Dismiss24Regular, History24Regular } from '@fluentui/react-icons';
import { useUndoManager } from '../../state/undoManager';

export function ActionHistoryPanel() {
  const { showHistory, setHistoryVisible, getHistory } = useUndoManager();

  const history = getHistory();

  const formatTime = (date: Date) => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (seconds < 60) {
      return 'Just now';
    } else if (minutes < 60) {
      return `${minutes}m ago`;
    } else if (hours < 24) {
      return `${hours}h ago`;
    } else {
      return date.toLocaleDateString();
    }
  };

  return (
    <Drawer
      type="overlay"
      separator
      open={showHistory}
      onOpenChange={(_, { open }) => setHistoryVisible(open)}
      position="end"
      size="medium"
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={
            <Button
              appearance="subtle"
              aria-label="Close"
              icon={<Dismiss24Regular />}
              onClick={() => setHistoryVisible(false)}
            />
          }
        >
          Action History
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody>
        {history.length === 0 ? (
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              gap: '16px',
              padding: '32px',
              textAlign: 'center',
            }}
          >
            <History24Regular style={{ fontSize: '48px', opacity: 0.5 }} />
            <Text size={400} style={{ opacity: 0.7 }}>
              No actions yet
            </Text>
            <Text size={300} style={{ opacity: 0.6 }}>
              Your undo/redo history will appear here
            </Text>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {history.map((entry) => (
              <div
                key={entry.id}
                style={{
                  padding: '12px',
                  borderRadius: '4px',
                  backgroundColor: 'var(--colorNeutralBackground1)',
                  border: '1px solid var(--colorNeutralStroke1)',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '4px',
                }}
              >
                <Text weight="semibold" size={300}>
                  {entry.description}
                </Text>
                <Text size={200} style={{ opacity: 0.7 }}>
                  {formatTime(entry.timestamp)}
                </Text>
              </div>
            ))}
          </div>
        )}
      </DrawerBody>
    </Drawer>
  );
}
