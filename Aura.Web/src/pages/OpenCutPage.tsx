import { makeStyles, tokens } from '@fluentui/react-components';
import { OpenCutEditor, OpenCutErrorBoundary } from '../components/OpenCut';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  editorContainer: {
    flex: 1,
    minHeight: 0,
    minWidth: 0,
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
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
/**
 * OpenCut Page
 *
 * The header is hidden by default to maximize screen real estate for editing.
 * Users can reveal it if needed using keyboard shortcuts or menu options.
 */
export function OpenCutPage() {
  const styles = useStyles();

  return (
    <div className={styles.root}>
      <div className={styles.editorContainer}>
        <OpenCutErrorBoundary>
          <OpenCutEditor />
        </OpenCutErrorBoundary>
      </div>
    </div>
  );
}
