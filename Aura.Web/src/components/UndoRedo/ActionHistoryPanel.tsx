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
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Spinner,
} from '@fluentui/react-components';
import { Dismiss24Regular, History24Regular, ArrowDownload24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { exportActionHistory } from '../../services/api/actionsApi';
import { useUndoManager } from '../../state/undoManager';

export function ActionHistoryPanel() {
  const { showHistory, setHistoryVisible, getHistory } = useUndoManager();
  const [isExporting, setIsExporting] = useState(false);

  const history = getHistory();

  const handleExport = async (format: 'csv' | 'json') => {
    setIsExporting(true);
    try {
      const blob = await exportActionHistory({}, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `action-history-${new Date().toISOString().split('T')[0]}.${format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error('Failed to export action history:', error);
    } finally {
      setIsExporting(false);
    }
  };

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
            <div style={{ display: 'flex', gap: '8px' }}>
              <Menu>
                <MenuTrigger disableButtonEnhancement>
                  <Button
                    appearance="subtle"
                    icon={isExporting ? <Spinner size="tiny" /> : <ArrowDownload24Regular />}
                    disabled={isExporting}
                  >
                    Export
                  </Button>
                </MenuTrigger>
                <MenuPopover>
                  <MenuList>
                    <MenuItem onClick={() => handleExport('csv')}>Export as CSV</MenuItem>
                    <MenuItem onClick={() => handleExport('json')}>Export as JSON</MenuItem>
                  </MenuList>
                </MenuPopover>
              </Menu>
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<Dismiss24Regular />}
                onClick={() => setHistoryVisible(false)}
              />
            </div>
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
