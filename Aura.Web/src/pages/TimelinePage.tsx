import { useEffect, useMemo, useState } from 'react';
import { makeStyles, tokens, Title1, Text } from '@fluentui/react-components';
import { TimelineView } from '../components/Timeline/TimelineView';
import { OverlayPanel } from '../components/Overlays/OverlayPanel';
import { keyboardShortcutManager } from '../services/keyboardShortcutManager';
import { CommandHistory } from '../services/commandHistory';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalXXL,
    paddingBottom: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  content: {
    flex: 1,
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    padding: tokens.spacingVerticalL,
    overflow: 'hidden',
  },
  timeline: {
    flex: 3,
    overflow: 'hidden',
  },
  sidebar: {
    flex: 1,
    minWidth: '300px',
    maxWidth: '400px',
    overflow: 'auto',
  },
});

export function TimelinePage() {
  const styles = useStyles();
  
  // Command history for undo/redo
  const commandHistory = useMemo(() => new CommandHistory(50), []);
  const [canUndo, setCanUndo] = useState(false);
  const [canRedo, setCanRedo] = useState(false);

  // Subscribe to command history changes
  useEffect(() => {
    const unsubscribe = commandHistory.subscribe((undo, redo) => {
      setCanUndo(undo);
      setCanRedo(redo);
    });
    return unsubscribe;
  }, [commandHistory]);
  
  // Prevent unused variable warnings - these will be used for UI indicators
  void canUndo;
  void canRedo;

  // Register timeline shortcuts
  useEffect(() => {
    // Set the active context
    keyboardShortcutManager.setActiveContext('timeline');

    // Register timeline-specific shortcuts
    keyboardShortcutManager.registerMultiple([
      {
        id: 'timeline-undo',
        keys: 'Ctrl+Z',
        description: 'Undo',
        context: 'timeline',
        handler: (e) => {
          e.preventDefault();
          commandHistory.undo();
        },
      },
      {
        id: 'timeline-redo',
        keys: 'Ctrl+Y',
        description: 'Redo',
        context: 'timeline',
        handler: (e) => {
          e.preventDefault();
          commandHistory.redo();
        },
      },
      {
        id: 'timeline-redo-shift',
        keys: 'Ctrl+Shift+Z',
        description: 'Redo',
        context: 'timeline',
        handler: (e) => {
          e.preventDefault();
          commandHistory.redo();
        },
      },
      {
        id: 'timeline-play-pause',
        keys: 'Space',
        description: 'Play/Pause',
        context: 'timeline',
        handler: () => {
          // Placeholder for timeline play/pause functionality
        },
      },
      {
        id: 'timeline-split',
        keys: 'S',
        description: 'Split clip at playhead',
        context: 'timeline',
        handler: () => {
          // Placeholder for split functionality
        },
      },
      {
        id: 'timeline-zoom-in',
        keys: 'Plus',
        description: 'Zoom in timeline',
        context: 'timeline',
        handler: () => {
          // Placeholder for zoom in functionality
        },
      },
      {
        id: 'timeline-zoom-in-equals',
        keys: 'Equals',
        description: 'Zoom in timeline',
        context: 'timeline',
        handler: () => {
          // Placeholder for zoom in functionality
        },
      },
      {
        id: 'timeline-zoom-out',
        keys: 'Minus',
        description: 'Zoom out timeline',
        context: 'timeline',
        handler: () => {
          // Placeholder for zoom out functionality
        },
      },
      {
        id: 'timeline-delete-ripple',
        keys: 'Shift+Delete',
        description: 'Ripple delete (delete and close gap)',
        context: 'timeline',
        handler: () => {
          // Placeholder for ripple delete functionality
        },
      },
      {
        id: 'timeline-delete',
        keys: 'Delete',
        description: 'Delete selected clip',
        context: 'timeline',
        handler: () => {
          // Placeholder for delete functionality
        },
      },
      {
        id: 'timeline-home',
        keys: 'Home',
        description: 'Go to beginning',
        context: 'timeline',
        handler: () => {
          // Placeholder for go to beginning functionality
        },
      },
      {
        id: 'timeline-end',
        keys: 'End',
        description: 'Go to end',
        context: 'timeline',
        handler: () => {
          // Placeholder for go to end functionality
        },
      },
    ]);

    // Clean up on unmount
    return () => {
      keyboardShortcutManager.unregisterContext('timeline');
    };
  }, [commandHistory]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Timeline Editor</Title1>
        <Text className={styles.subtitle}>
          Edit clips, add overlays, and create chapter markers
        </Text>
      </div>

      <div className={styles.content}>
        <div className={styles.timeline}>
          <TimelineView />
        </div>
        <div className={styles.sidebar}>
          <OverlayPanel />
        </div>
      </div>
    </div>
  );
}
