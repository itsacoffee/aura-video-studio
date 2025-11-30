import { makeStyles, tokens, Button, Text, Spinner, Card } from '@fluentui/react-components';
import {
  Alert24Regular,
  Open24Regular,
  ArrowClockwise24Regular,
  Play24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
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
  setupContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalXL,
    padding: tokens.spacingHorizontalXXL,
    maxWidth: '800px',
    margin: '0 auto',
  },
  setupCard: {
    padding: tokens.spacingHorizontalXL,
    width: '100%',
  },
  setupSteps: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
  stepItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },
  stepNumber: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    borderRadius: '50%',
    width: '24px',
    height: '24px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    flexShrink: 0,
  },
  codeBlock: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingHorizontalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    overflowX: 'auto',
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
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  infoBox: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
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
    const timeoutId = setTimeout(() => controller.abort(), 3000); // Increased timeout

    const response = await fetch(healthUrl, {
      method: 'GET',
      signal: controller.signal,
      // Don't use credentials for health check
      credentials: 'omit',
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
    const timeoutId = setTimeout(() => controller.abort(), 3000); // Increased timeout

    // Use HEAD request to minimize data transfer
    await fetch(url, {
      method: 'HEAD',
      signal: controller.signal,
      mode: 'no-cors', // Use no-cors to avoid CORS issues
      credentials: 'omit',
    });

    clearTimeout(timeoutId);
    // With no-cors, we can't check status, but if we get here without error, server is likely up
    return true;
  } catch (error) {
    // If both checks fail, server is likely down or not started yet
    // This is non-critical - the iframe will still attempt to load
    console.info(
      '[OpenCut] Direct connection check failed (non-critical, iframe will still attempt to load):',
      error
    );
    return false;
  }
}

type ConnectionState =
  | 'checking'
  | 'starting'
  | 'loading'
  | 'loading-timeout'
  | 'connected'
  | 'error';
type StartupStatus = {
  message: string;
  isStarting: boolean;
  isRunning: boolean;
};

// Check if we're in Electron and have OpenCut IPC available
function isElectronWithOpenCut(): boolean {
  return (
    typeof window !== 'undefined' &&
    (window.aura?.opencut || window.electron?.opencut) !== undefined
  );
}

// Get OpenCut IPC API
function getOpenCutApi() {
  if (typeof window === 'undefined') return null;
  return window.aura?.opencut || window.electron?.opencut || null;
}

export function OpenCutPage() {
  const styles = useStyles();
  const effectiveUrl = getEffectiveOpenCutUrl();
  const [connectionState, setConnectionState] = useState<ConnectionState>('checking');
  const [retryCount, setRetryCount] = useState(0);
  const [startupStatus, setStartupStatus] = useState<StartupStatus>({
    message: 'Checking OpenCut server...',
    isStarting: false,
    isRunning: false,
  });
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const loadTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const statusCheckIntervalRef = useRef<NodeJS.Timeout | null>(null);

  const handleIframeLoad = useCallback(() => {
    // Iframe loaded successfully (even if it's an error page, the iframe itself loaded)
    console.info('[OpenCut] Iframe loaded successfully');
    if (loadTimeoutRef.current) {
      clearTimeout(loadTimeoutRef.current);
      loadTimeoutRef.current = null;
    }
    setConnectionState('connected');
  }, []);

  const handleIframeError = useCallback(() => {
    // Iframe failed to load
    console.warn('[OpenCut] Iframe failed to load');
    if (loadTimeoutRef.current) {
      clearTimeout(loadTimeoutRef.current);
      loadTimeoutRef.current = null;
    }
    setConnectionState('error');
  }, []);

  const startLoading = useCallback((url: string) => {
    if (!iframeRef.current) return;

    // Clear any existing timeout
    if (loadTimeoutRef.current) {
      clearTimeout(loadTimeoutRef.current);
    }

    setConnectionState('loading');
    iframeRef.current.src = url;

    // Set a longer timeout to detect if iframe fails to load
    // OpenCut server may take time to start, especially on first load
    loadTimeoutRef.current = setTimeout(() => {
      setConnectionState((current) => {
        // Only hide overlay if we're still loading (iframe didn't fire onLoad)
        // The iframe will remain visible and may still load, so we hide the overlay
        if (current === 'loading' || current === 'checking') {
          console.warn(
            '[OpenCut] Iframe load timeout after 30 seconds, hiding overlay but keeping iframe visible'
          );
          // Hide the loading overlay so user can see if iframe is loading
          // The iframe will remain visible and may still load
          return 'loading-timeout'; // Hide overlay, but iframe remains visible
        }
        return current;
      });
      loadTimeoutRef.current = null;
    }, 30000); // 30 second timeout - OpenCut server may need time to start
  }, []);

  // Check status using IPC (Electron) or direct health check (web)
  const checkOpenCutStatus = useCallback(async (): Promise<{
    isRunning: boolean;
    isStarting: boolean;
    url: string | null;
  }> => {
    const opencutApi = getOpenCutApi();

    if (opencutApi) {
      // Use IPC bridge when in Electron
      try {
        const status = await opencutApi.status();
        return {
          isRunning: status.isRunning || false,
          isStarting: status.isStarting || false,
          url: status.url || null,
        };
      } catch (error) {
        console.warn('[OpenCut] IPC status check failed:', error);
        return { isRunning: false, isStarting: false, url: null };
      }
    } else {
      // Fallback to direct health check in web mode
      if (!effectiveUrl) {
        return { isRunning: false, isStarting: false, url: null };
      }
      const isHealthy = await checkOpenCutHealth(effectiveUrl);
      return {
        isRunning: isHealthy,
        isStarting: false,
        url: isHealthy ? effectiveUrl : null,
      };
    }
  }, [effectiveUrl]);

  // Wait for server to be ready using IPC or polling
  const waitForServerReady = useCallback(
    async (maxWaitMs = 30000): Promise<{ success: boolean; url: string | null }> => {
      const opencutApi = getOpenCutApi();

      if (opencutApi) {
        // Use IPC waitForReady when in Electron
        try {
          const result = await opencutApi.waitForReady(maxWaitMs);
          return {
            success: result.success || false,
            url: result.status?.url || null,
          };
        } catch (error) {
          console.warn('[OpenCut] IPC waitForReady failed:', error);
          return { success: false, url: null };
        }
      } else {
        // Fallback to polling in web mode
        const startTime = Date.now();
        while (Date.now() - startTime < maxWaitMs) {
          const status = await checkOpenCutStatus();
          if (status.isRunning && status.url) {
            return { success: true, url: status.url };
          }
          await new Promise((resolve) => setTimeout(resolve, 500));
        }
        return { success: false, url: null };
      }
    },
    [checkOpenCutStatus]
  );

  // Handle server that is already running
  const handleServerAlreadyRunning = useCallback(
    (statusUrl: string) => {
      console.info('[OpenCut] Server is already running, loading iframe...');
      setStartupStatus({
        message: 'Server ready, loading editor...',
        isStarting: false,
        isRunning: true,
      });
      setConnectionState('loading');
      startLoading(statusUrl);
    },
    [startLoading]
  );

  // Handle status update during polling
  const handleStatusUpdate = useCallback(
    (
      statusInterval: NodeJS.Timeout,
      currentStatus: { isStarting: boolean; isRunning: boolean; url: string | null }
    ) => {
      if (currentStatus.isStarting) {
        setStartupStatus({
          message: 'Starting OpenCut server...',
          isStarting: true,
          isRunning: false,
        });
        return;
      }

      if (currentStatus.isRunning) {
        clearInterval(statusInterval);
        setStartupStatus({
          message: 'Server ready, loading editor...',
          isStarting: false,
          isRunning: true,
        });
        setConnectionState('loading');
        if (currentStatus.url) {
          startLoading(currentStatus.url);
        }
      }
    },
    [startLoading]
  );

  // Handle server ready result
  const handleServerReadyResult = useCallback(
    (result: { success: boolean; url: string | null }, url: string) => {
      if (result.success && result.url) {
        console.info('[OpenCut] Server is ready, loading iframe...');
        setConnectionState('loading');
        setStartupStatus({ message: 'Loading editor...', isStarting: false, isRunning: true });
        startLoading(result.url);
        return;
      }

      console.warn('[OpenCut] Server did not become ready within timeout');
      setConnectionState('error');
      setStartupStatus({
        message: 'Server failed to start',
        isStarting: false,
        isRunning: false,
      });
      // Still try to load - server might have started but check failed
      startLoading(url);
    },
    [startLoading]
  );

  // Handle server that is currently starting
  const handleServerStarting = useCallback(
    async (url: string) => {
      console.info('[OpenCut] Server is starting, waiting for it to be ready...');
      setConnectionState('starting');
      setStartupStatus({
        message: 'Starting OpenCut server...',
        isStarting: true,
        isRunning: false,
      });

      // Start polling for status updates
      const statusInterval = setInterval(async () => {
        const currentStatus = await checkOpenCutStatus();
        handleStatusUpdate(statusInterval, currentStatus);
      }, 1000);

      statusCheckIntervalRef.current = statusInterval;

      // Wait for server to be ready
      const result = await waitForServerReady(30000);

      if (statusCheckIntervalRef.current) {
        clearInterval(statusCheckIntervalRef.current);
        statusCheckIntervalRef.current = null;
      }

      handleServerReadyResult(result, url);
    },
    [checkOpenCutStatus, handleStatusUpdate, waitForServerReady, handleServerReadyResult]
  );

  // Handle server that is not running - attempt to start it
  const handleServerNotRunning = useCallback(
    async (url: string, opencutApi: NonNullable<ReturnType<typeof getOpenCutApi>>) => {
      console.info('[OpenCut] Server is not running, attempting to start...');
      setConnectionState('starting');
      setStartupStatus({
        message: 'Starting OpenCut server...',
        isStarting: true,
        isRunning: false,
      });

      try {
        const startResult = await opencutApi.start();
        if (startResult.success && startResult.url) {
          console.info('[OpenCut] Server started successfully, loading iframe...');
          setConnectionState('loading');
          setStartupStatus({ message: 'Loading editor...', isStarting: false, isRunning: true });
          startLoading(startResult.url);
        } else {
          console.warn('[OpenCut] Failed to start server:', startResult.error);
          setConnectionState('error');
          setStartupStatus({
            message: startResult.error || 'Failed to start server',
            isStarting: false,
            isRunning: false,
          });
          // Still try to load - might be running already
          startLoading(url);
        }
      } catch (error) {
        console.error('[OpenCut] Error starting server:', error);
        setConnectionState('error');
        setStartupStatus({
          message: 'Error starting server',
          isStarting: false,
          isRunning: false,
        });
        // Still try to load
        startLoading(url);
      }
    },
    [startLoading]
  );

  // Handle Electron mode - check status and manage server lifecycle
  const handleElectronMode = useCallback(
    async (url: string, opencutApi: NonNullable<ReturnType<typeof getOpenCutApi>>) => {
      try {
        const status = await checkOpenCutStatus();

        if (status.isRunning && status.url) {
          handleServerAlreadyRunning(status.url);
          return;
        }

        if (status.isStarting) {
          await handleServerStarting(url);
        } else {
          await handleServerNotRunning(url, opencutApi);
        }
      } catch (error) {
        console.error('[OpenCut] Error checking server status:', error);
        // Fall through to normal loading
        setConnectionState('loading');
        startLoading(url);
      }
    },
    [
      checkOpenCutStatus,
      handleServerAlreadyRunning,
      handleServerStarting,
      handleServerNotRunning,
      startLoading,
    ]
  );

  // Handle web mode - start loading immediately and check health in parallel
  const handleWebMode = useCallback(
    (url: string) => {
      setConnectionState('loading');
      startLoading(url);

      // Do a health check in parallel (non-blocking)
      checkOpenCutHealth(url)
        .then((isHealthy) => {
          if (!isHealthy) {
            console.warn('[OpenCut] Health check failed, but iframe may still load');
          } else {
            console.info('[OpenCut] Health check passed');
          }
        })
        .catch((error) => {
          console.info(
            '[OpenCut] Health check error (non-critical, iframe will still attempt to load):',
            error
          );
        });
    },
    [startLoading]
  );

  const checkHealthAndLoad = useCallback(
    async (url: string, isRetry = false) => {
      if (!isRetry) {
        setConnectionState('checking');
        setStartupStatus({
          message: 'Checking OpenCut server...',
          isStarting: false,
          isRunning: false,
        });
      }

      const opencutApi = getOpenCutApi();
      const isElectron = isElectronWithOpenCut();

      if (isElectron && opencutApi) {
        await handleElectronMode(url, opencutApi);
      } else {
        handleWebMode(url);
      }
    },
    [handleElectronMode, handleWebMode]
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
      if (statusCheckIntervalRef.current) {
        clearInterval(statusCheckIntervalRef.current);
        statusCheckIntervalRef.current = null;
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

  // Keep for potential future use with Electron-specific behavior
  const _isElectron = typeof navigator !== 'undefined' && navigator.userAgent.includes('Electron');
  const _isLocalhost = effectiveUrl.includes('127.0.0.1') || effectiveUrl.includes('localhost');

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
        {/* Loading overlays - shown while checking, starting, or loading (but not after timeout) */}
        {(connectionState === 'checking' ||
          connectionState === 'starting' ||
          connectionState === 'loading') && (
          <div
            className={styles.loadingContainer}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              zIndex: 10,
              backgroundColor: tokens.colorNeutralBackground1,
            }}
          >
            <Spinner
              size="large"
              label={
                startupStatus.message ||
                (connectionState === 'checking'
                  ? 'Checking OpenCut server...'
                  : connectionState === 'starting'
                    ? 'Starting OpenCut server...'
                    : 'Loading OpenCut editor...')
              }
            />
            <Text size={300} color="foreground2">
              {startupStatus.message ||
                (connectionState === 'checking'
                  ? 'Verifying connection to OpenCut server...'
                  : connectionState === 'starting'
                    ? 'Please wait while the server starts. This may take a moment on first launch...'
                    : 'Starting the video editor...')}
            </Text>
          </div>
        )}

        {/* Minimal loading indicator after timeout - doesn't block iframe */}
        {connectionState === 'loading-timeout' && (
          <div
            style={{
              position: 'absolute',
              top: tokens.spacingVerticalM,
              left: tokens.spacingHorizontalM,
              zIndex: 5,
              padding: tokens.spacingHorizontalM,
              backgroundColor: tokens.colorNeutralBackground3,
              borderRadius: tokens.borderRadiusMedium,
              boxShadow: tokens.shadow4,
            }}
          >
            <Text size={200} color="foreground2">
              Still loading... The editor will appear when ready.
            </Text>
          </div>
        )}

        {/* Error overlay - shown only when connection definitively failed */}
        {connectionState === 'error' && (
          <div
            className={styles.setupContainer}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              zIndex: 10,
              backgroundColor: tokens.colorNeutralBackground1,
              overflow: 'auto',
            }}
          >
            <Card className={styles.setupCard}>
              <div style={{ textAlign: 'center', marginBottom: tokens.spacingVerticalL }}>
                <Play24Regular
                  style={{
                    fontSize: '48px',
                    color: tokens.colorBrandForeground1,
                    marginBottom: tokens.spacingVerticalM,
                  }}
                />
                <Text
                  weight="semibold"
                  size={500}
                  style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                >
                  Start OpenCut Server
                </Text>
                <Text size={300} color="foreground2">
                  OpenCut is a CapCut-inspired video editor. To use it, you need to start the
                  OpenCut development server first.
                </Text>
              </div>

              <div className={styles.setupSteps}>
                <div className={styles.stepItem}>
                  <div className={styles.stepNumber}>1</div>
                  <div>
                    <Text weight="semibold" size={300}>
                      Open a terminal/command prompt
                    </Text>
                    <Text size={200} color="foreground2" style={{ display: 'block' }}>
                      Navigate to your Aura Video Studio installation folder
                    </Text>
                  </div>
                </div>

                <div className={styles.stepItem}>
                  <div className={styles.stepNumber}>2</div>
                  <div>
                    <Text weight="semibold" size={300}>
                      Navigate to the OpenCut directory
                    </Text>
                    <div className={styles.codeBlock}>cd OpenCut/apps/web</div>
                  </div>
                </div>

                <div className={styles.stepItem}>
                  <div className={styles.stepNumber}>3</div>
                  <div>
                    <Text weight="semibold" size={300}>
                      Install dependencies (first time only)
                    </Text>
                    <div className={styles.codeBlock}>bun install</div>
                    <Text size={200} color="foreground2" style={{ display: 'block' }}>
                      or use: npm install
                    </Text>
                  </div>
                </div>

                <div className={styles.stepItem}>
                  <div className={styles.stepNumber}>4</div>
                  <div>
                    <Text weight="semibold" size={300}>
                      Start the development server
                    </Text>
                    <div className={styles.codeBlock}>bun run dev</div>
                    <Text size={200} color="foreground2" style={{ display: 'block' }}>
                      or use: npm run dev
                    </Text>
                  </div>
                </div>

                <div className={styles.stepItem}>
                  <div className={styles.stepNumber}>5</div>
                  <div>
                    <Text weight="semibold" size={300}>
                      Click &quot;Retry Connection&quot; below
                    </Text>
                    <Text size={200} color="foreground2" style={{ display: 'block' }}>
                      Once the server is running on port 3100, OpenCut will load here automatically
                    </Text>
                  </div>
                </div>
              </div>

              <div className={styles.infoBox}>
                <Info24Regular style={{ color: tokens.colorBrandForeground1, flexShrink: 0 }} />
                <div>
                  <Text size={200}>
                    <strong>Server URL:</strong> {effectiveUrl}
                  </Text>
                  <Text size={200} color="foreground2" style={{ display: 'block' }}>
                    OpenCut will automatically load in this window once the server is running.
                  </Text>
                </div>
              </div>
            </Card>

            <div className={styles.actionButtons}>
              <Button
                appearance="primary"
                size="large"
                icon={<ArrowClockwise24Regular aria-hidden />}
                onClick={handleRetry}
              >
                Retry Connection {retryCount > 0 && `(${retryCount})`}
              </Button>
              <Button
                appearance="secondary"
                size="large"
                icon={<Open24Regular aria-hidden />}
                onClick={() => window.open(effectiveUrl, '_blank', 'noopener,noreferrer')}
              >
                Open in Browser
              </Button>
            </div>
          </div>
        )}

        {/* Always show iframe - only hide error message overlay when connected */}
        <iframe
          ref={iframeRef}
          title="OpenCut Editor"
          className={styles.iframe}
          onLoad={handleIframeLoad}
          onError={handleIframeError}
          allow="camera; microphone; fullscreen; autoplay; encrypted-media; picture-in-picture"
          sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals allow-downloads"
        />
      </div>
    </div>
  );
}
