import { makeStyles, tokens, Button, Text, Spinner } from '@fluentui/react-components';
import { Alert24Regular, Open24Regular, ArrowClockwise24Regular } from '@fluentui/react-icons';
import { useState, useEffect, useRef, useCallback } from 'react';
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
    flexShrink: 0,
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
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
  },
  iframe: {
    border: 'none',
    width: '100%',
    height: '100%',
    display: 'block',
  },
  iframeHidden: {
    display: 'none',
  },
  loadingContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingHorizontalXXL,
  },
  errorContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingHorizontalXXL,
  },
  warning: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingHorizontalXL,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

function getEffectiveOpenCutUrl(): string | null {
  if (env.openCutUrl) {
    return env.openCutUrl;
  }

  // When running inside the Electron shell, fall back to the local dev server
  // that the main process can auto-start (OpenCutManager).
  const isElectron = typeof navigator !== 'undefined' && navigator.userAgent.includes('Electron');

  if (isElectron) {
    return 'http://127.0.0.1:3100';
  }

  return null;
}

/**
 * Check if OpenCut server is responding
 * Uses a combination of health endpoint and direct connection test
 */
async function checkOpenCutHealth(url: string): Promise<boolean> {
  // First try the health endpoint (more reliable)
  try {
    const healthUrl = new URL('/api/health', url).toString();
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 2000);

    const response = await fetch(healthUrl, {
      method: 'GET',
      signal: controller.signal,
    });

    clearTimeout(timeoutId);
    if (response.ok) {
      return true;
    }
  } catch (error) {
    // Health endpoint failed, try direct connection
    console.info('[OpenCut] Health endpoint check failed, trying direct connection:', error);
  }

  // Fallback: Try direct connection (may fail due to CORS, but that's okay - we'll let iframe handle it)
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 2000);

    // Use HEAD request to minimize data transfer
    await fetch(url, {
      method: 'HEAD',
      signal: controller.signal,
      mode: 'no-cors', // Use no-cors to avoid CORS issues
    });

    clearTimeout(timeoutId);
    // With no-cors, we can't check status, but if we get here without error, server is likely up
    return true;
  } catch (error) {
    // If both checks fail, server is likely down
    console.info('[OpenCut] Direct connection check failed:', error);
    return false;
  }
}

type ConnectionState = 'checking' | 'loading' | 'connected' | 'error';

export function OpenCutPage() {
  const styles = useStyles();
  const effectiveUrl = getEffectiveOpenCutUrl();
  const [connectionState, setConnectionState] = useState<ConnectionState>('checking');
  const [retryCount, setRetryCount] = useState(0);
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const loadTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const handleIframeLoad = useCallback(() => {
    // Iframe loaded successfully
    console.info('[OpenCut] Iframe loaded successfully');
    if (loadTimeoutRef.current) {
      clearTimeout(loadTimeoutRef.current);
      loadTimeoutRef.current = null;
    }
    setConnectionState('connected');
  }, []);

  const startLoading = useCallback((url: string) => {
    if (!iframeRef.current) return;

    // Clear any existing timeout
    if (loadTimeoutRef.current) {
      clearTimeout(loadTimeoutRef.current);
    }

    setConnectionState('loading');
    iframeRef.current.src = url;

    // Set a timeout to detect if iframe fails to load
    loadTimeoutRef.current = setTimeout(() => {
      setConnectionState((current) => {
        // Only set to error if we're still loading (iframe didn't fire onLoad)
        if (current === 'loading' || current === 'checking') {
          console.warn('[OpenCut] Iframe load timeout after 10 seconds');
          return 'error';
        }
        return current;
      });
      loadTimeoutRef.current = null;
    }, 10000); // 10 second timeout
  }, []);

  const checkHealthAndLoad = useCallback(
    async (url: string, isRetry = false) => {
      if (!isRetry) {
        setConnectionState('checking');
      }

      // Start loading iframe immediately (health check may fail due to CORS but iframe might still work)
      startLoading(url);

      // Also do a health check in parallel (non-blocking, for informational purposes)
      checkOpenCutHealth(url)
        .then((isHealthy) => {
          if (!isHealthy) {
            console.warn('[OpenCut] Health check failed, but iframe may still load');
          } else {
            console.info('[OpenCut] Health check passed');
          }
        })
        .catch((error) => {
          console.info('[OpenCut] Health check error (non-critical):', error);
        });
    },
    [startLoading]
  );

  const handleRetry = useCallback(() => {
    if (!effectiveUrl) return;
    setRetryCount((prev) => prev + 1);
    // Reset iframe src to force reload
    if (iframeRef.current) {
      iframeRef.current.src = '';
      setTimeout(() => {
        checkHealthAndLoad(effectiveUrl, true);
      }, 100);
    } else {
      checkHealthAndLoad(effectiveUrl, true);
    }
  }, [effectiveUrl, checkHealthAndLoad]);

  useEffect(() => {
    if (!effectiveUrl) {
      setConnectionState('error');
      return;
    }

    // Initial load
    checkHealthAndLoad(effectiveUrl);

    return () => {
      if (loadTimeoutRef.current) {
        clearTimeout(loadTimeoutRef.current);
        loadTimeoutRef.current = null;
      }
    };
  }, [effectiveUrl, checkHealthAndLoad]);

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

  const isElectron = typeof navigator !== 'undefined' && navigator.userAgent.includes('Electron');
  const isLocalhost = effectiveUrl.includes('127.0.0.1') || effectiveUrl.includes('localhost');

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
        <div className={styles.actionButtons}>
          {connectionState === 'connected' && (
            <Button
              appearance="subtle"
              icon={<Open24Regular aria-hidden />}
              onClick={() => window.open(effectiveUrl, '_blank', 'noopener,noreferrer')}
            >
              Open in new tab
            </Button>
          )}
        </div>
      </div>
      <div className={styles.iframeContainer}>
        {connectionState === 'checking' && (
          <div className={styles.loadingContainer}>
            <Spinner size="large" label="Checking OpenCut server..." />
            <Text size={300} color="foreground2">
              Verifying connection to OpenCut server...
            </Text>
          </div>
        )}

        {connectionState === 'loading' && (
          <div className={styles.loadingContainer}>
            <Spinner size="large" label="Loading OpenCut editor..." />
            <Text size={300} color="foreground2">
              Starting the video editor...
            </Text>
          </div>
        )}

        {connectionState === 'error' && (
          <div className={styles.errorContainer}>
            <Alert24Regular
              style={{ fontSize: '48px', color: tokens.colorPaletteRedForeground1 }}
            />
            <div style={{ textAlign: 'center', maxWidth: '600px' }}>
              <Text
                weight="semibold"
                size={400}
                style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
              >
                Unable to connect to OpenCut server
              </Text>
              <Text
                size={300}
                color="foreground2"
                style={{ display: 'block', marginBottom: tokens.spacingVerticalM }}
              >
                {isLocalhost && isElectron
                  ? 'The OpenCut server should start automatically when running Aura Desktop. Please wait a moment and try again, or check if the server is running on port 3100.'
                  : isLocalhost
                    ? `The OpenCut server at ${effectiveUrl} is not responding. Please ensure the server is running.`
                    : `Unable to connect to ${effectiveUrl}. Please verify the server is accessible.`}
              </Text>
              {isLocalhost && (
                <Text
                  size={200}
                  color="foreground2"
                  style={{ display: 'block', marginBottom: tokens.spacingVerticalL }}
                >
                  {isElectron
                    ? "If the server doesn't start automatically, you may need to start it manually or check the application logs."
                    : 'Make sure the OpenCut development server is running. You can start it from the OpenCut directory with: bun run dev (or npm run dev)'}
                </Text>
              )}
            </div>
            <div className={styles.actionButtons}>
              <Button
                appearance="primary"
                icon={<ArrowClockwise24Regular aria-hidden />}
                onClick={handleRetry}
              >
                Retry Connection {retryCount > 0 && `(${retryCount})`}
              </Button>
              <Button
                appearance="secondary"
                icon={<Open24Regular aria-hidden />}
                onClick={() => window.open(effectiveUrl, '_blank', 'noopener,noreferrer')}
              >
                Open in new tab
              </Button>
            </div>
          </div>
        )}

        <iframe
          ref={iframeRef}
          title="OpenCut Editor"
          className={connectionState === 'error' ? styles.iframeHidden : styles.iframe}
          onLoad={handleIframeLoad}
          allow="camera; microphone; fullscreen; autoplay; encrypted-media; picture-in-picture"
          sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals allow-downloads"
        />
      </div>
    </div>
  );
}
