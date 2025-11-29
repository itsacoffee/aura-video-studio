import { makeStyles, tokens, Button, Text } from '@fluentui/react-components';
import { Alert24Regular, Open24Regular } from '@fluentui/react-icons';
import { env } from '../config/env';

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
  },
  titleGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  iframeContainer: {
    flex: 1,
    minHeight: 0,
    minWidth: 0,
  },
  iframe: {
    border: 'none',
    width: '100%',
    height: '100%',
  },
  warning: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingHorizontalXL,
  },
});

function getEffectiveOpenCutUrl(): string | null {
  if (env.openCutUrl) {
    return env.openCutUrl;
  }

  // When running inside the Electron shell, fall back to the local dev server
  // that the main process can auto-start (OpenCutManager).
  const isElectron =
    typeof navigator !== 'undefined' && navigator.userAgent.includes('Electron');

  if (isElectron) {
    return 'http://127.0.0.1:3100';
  }

  return null;
}

export function OpenCutPage() {
  const styles = useStyles();
  const effectiveUrl = getEffectiveOpenCutUrl();

  if (!effectiveUrl) {
    return (
      <div className={styles.root}>
        <div className={styles.header}>
          <div className={styles.titleGroup}>
            <Text weight="semibold" size={500}>
              OpenCut
            </Text>
            <Text size={300} color="foreground2">
              Configure the OpenCut URL to embed the CapCut-style editor inside Aura.
            </Text>
          </div>
        </div>
        <div className={styles.warning}>
          <Alert24Regular aria-hidden />
          <div>
            <Text weight="semibold" size={300}>
              OpenCut URL not available
            </Text>
            <Text as="div" size={200} color="foreground2">
              Set the <code>VITE_OPENCUT_URL</code> environment variable to the URL where the
              OpenCut web app is hosted (for example, <code>http://localhost:3100</code>), or run
              Aura from the desktop app so it can start a local OpenCut server automatically.
            </Text>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <div className={styles.titleGroup}>
          <Text weight="semibold" size={500}>
            OpenCut
          </Text>
          <Text size={300} color="foreground2">
            CapCut-inspired video editor, integrated into Aura.
          </Text>
        </div>
        {effectiveUrl && (
          <Button
            appearance="subtle"
            icon={<Open24Regular aria-hidden />}
            onClick={() => window.open(effectiveUrl, '_blank', 'noopener,noreferrer')}
          >
            Open in new tab
          </Button>
        )}
      </div>
      <div className={styles.iframeContainer}>
        {effectiveUrl && (
          <iframe title="OpenCut Editor" src={effectiveUrl} className={styles.iframe} />
        )}
      </div>
    </div>
  );
}


