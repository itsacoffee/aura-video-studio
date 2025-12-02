import { makeStyles, tokens, Button, Text } from '@fluentui/react-components';
import { Open24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { OpenCutEditor } from '../components/OpenCut';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  header: {
    padding: tokens.spacingHorizontalXL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalL,
    flexShrink: 0,
  },
  titleGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  editorContainer: {
    flex: 1,
    minHeight: 0,
    minWidth: 0,
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
});

/**
 * OpenCut Page
 *
 * This page renders the native OpenCut video editor directly as React components.
 * The editor runs entirely in the browser without requiring a separate server process.
 *
 * Previous implementation used an iframe to load OpenCut from a Next.js server
 * running on port 3100. That approach had several issues:
 * - Required starting and managing a separate server process
 * - Showed loading spinners and connection errors
 * - Failed when the server wasn't available
 *
 * The new implementation renders OpenCut natively, eliminating all server dependencies.
 */
export function OpenCutPage() {
  const styles = useStyles();
  const [showHeader, setShowHeader] = useState(true);

  const toggleHeader = useCallback(() => {
    setShowHeader((prev) => !prev);
  }, []);

  return (
    <div className={styles.root}>
      {showHeader && (
        <div className={styles.header}>
          <div className={styles.titleGroup}>
            <Text weight="semibold" size={500}>
              OpenCut
            </Text>
            <Text size={300} color="foreground2">
              Professional video editing, integrated into Aura Video Studio.
            </Text>
          </div>
          <div className={styles.actionButtons}>
            <Button appearance="subtle" icon={<Open24Regular aria-hidden />} onClick={toggleHeader}>
              Hide Header
            </Button>
          </div>
        </div>
      )}
      <div className={styles.editorContainer}>
        <OpenCutEditor />
      </div>
    </div>
  );
}
