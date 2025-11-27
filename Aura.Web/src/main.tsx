import { webDarkTheme, webLightTheme } from '@fluentui/react-components';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import { ErrorBoundary } from './components/ErrorBoundary';
import { apiUrl } from './config/api';
import './index.css';
import { errorReportingService } from './services/errorReportingService';
import { loggingService } from './services/loggingService';
import './styles/component-overrides.css';
import './styles/windows11.css';
// Global UI density and spacing adjustments (see global.css for details and how to revert)
import './styles/global.css';
import { getAuraTheme } from './themes/auraTheme';
import { handleHttpErrorResponse, handleHttpError } from './utils/httpInterceptor';
import { validateEnvironment } from './utils/validateEnv';
import { logWindowsEnvironment } from './utils/windowsUtils';

// ===== GLOBAL ERROR HANDLERS (START) =====
window.addEventListener('unhandledrejection', (event) => {
  console.error('[Main] Unhandled promise rejection:', event.reason);

  const error = event.reason instanceof Error ? event.reason : new Error(String(event.reason));

  loggingService.error('Unhandled promise rejection', error, 'main', 'unhandledRejection', {
    promise: event.promise,
    reason: event.reason,
  });

  errorReportingService.error(
    'Unexpected Error',
    'An unexpected error occurred. The application will continue running.',
    error,
    {
      userAction: 'Unhandled promise rejection',
      actions: [
        {
          label: 'Reload Page',
          handler: () => window.location.reload(),
        },
      ],
    }
  );

  event.preventDefault();
});

window.addEventListener('error', (event) => {
  console.error('[Main] Uncaught error:', event.error || event.message);

  const error = event.error instanceof Error ? event.error : new Error(event.message);

  loggingService.error('Uncaught error', error, 'main', 'windowError', {
    filename: event.filename,
    lineno: event.lineno,
    colno: event.colno,
  });

  errorReportingService.error(
    'Application Error',
    'An error occurred that was not caught by the application.',
    error,
    {
      userAction: 'Uncaught error',
    }
  );
});
// ===== GLOBAL ERROR HANDLERS (END) =====

// ===== INITIALIZATION LOGGING (START) =====
console.info('[Main] ===== Aura Video Studio - React Initialization =====');
console.info('[Main] Timestamp:', new Date().toISOString());
console.info('[Main] Location:', window.location.href);
console.info('[Main] Protocol:', window.location.protocol);
console.info('[Main] User Agent:', navigator.userAgent);

// ===== DEFAULT ZOOM INITIALIZATION =====
// Set default zoom level to 90% for better readability on first load
// Uses CSS zoom property which is supported in all modern browsers (Chrome, Edge, Safari, Firefox 126+)
const initializeDefaultZoom = (): void => {
  const ZOOM_KEY = 'aura-zoom-level';
  const DEFAULT_ZOOM = 0.9; // 90% zoom for better content density
  
  // Check if user has a saved zoom preference
  const savedZoom = localStorage.getItem(ZOOM_KEY);
  
  // Use type assertion for the non-standard (but widely supported) zoom CSS property
  const htmlStyle = document.documentElement.style as CSSStyleDeclaration & { zoom: string };
  
  if (savedZoom === null) {
    // First load - apply default 90% zoom
    htmlStyle.zoom = String(DEFAULT_ZOOM);
    localStorage.setItem(ZOOM_KEY, String(DEFAULT_ZOOM));
    console.info(`[Main] Applied default zoom level: ${DEFAULT_ZOOM * 100}%`);
  } else {
    // User has a saved preference - apply it
    const zoomLevel = parseFloat(savedZoom);
    if (!isNaN(zoomLevel) && zoomLevel > 0 && zoomLevel <= 2) {
      htmlStyle.zoom = String(zoomLevel);
      console.info(`[Main] Applied saved zoom level: ${zoomLevel * 100}%`);
    }
  }
};

// Initialize zoom on script load
initializeDefaultZoom();

// Check for Electron environment
console.info('[Main] Checking Electron environment...');
const desktopDiagnostics =
  window.aura?.runtime?.getCachedDiagnostics?.() ??
  window.desktopBridge?.getCachedDiagnostics?.() ??
  null;
const diagnosticsBackend =
  desktopDiagnostics && typeof desktopDiagnostics === 'object' && 'backend' in desktopDiagnostics
    ? (desktopDiagnostics.backend as Record<string, unknown> | undefined)
    : undefined;
console.info('[Main] window.aura exists:', typeof (window as Window).aura !== 'undefined');
console.info('[Main] desktop bridge available:', !!window.desktopBridge);
console.info('[Main] aura backend URL:', diagnosticsBackend?.baseUrl);
console.info('[Main] aura environment:', desktopDiagnostics?.environment);
console.info('[Main] Legacy AURA_BACKEND_URL:', window.AURA_BACKEND_URL);

// Normalize relative /api requests so they work in Electron's file:// origin
// Also handles 401 and 500 errors globally
if (typeof window !== 'undefined' && typeof window.fetch === 'function') {
  const originalFetch = window.fetch.bind(window);

  window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
    let resolvedUrl: string;
    let finalInput: RequestInfo | URL;

    // Normalize URL
    if (typeof input === 'string') {
      if (input.startsWith('/api/')) {
        resolvedUrl = apiUrl(input);
        finalInput = resolvedUrl;
      } else {
        resolvedUrl = input;
        finalInput = input;
      }
    } else if (input instanceof URL) {
      if (input.pathname.startsWith('/api/')) {
        resolvedUrl = apiUrl(`${input.pathname}${input.search || ''}`);
        finalInput = resolvedUrl;
      } else {
        resolvedUrl = input.href;
        finalInput = input;
      }
    } else if (input instanceof Request) {
      const requestUrl = new URL(input.url, window.location.origin);
      if (requestUrl.pathname.startsWith('/api/')) {
        resolvedUrl = apiUrl(`${requestUrl.pathname}${requestUrl.search}`);
        finalInput = new Request(resolvedUrl, input);
      } else {
        resolvedUrl = input.url;
        finalInput = input;
      }
    } else {
      resolvedUrl = String(input);
      finalInput = input;
    }

    try {
      const response = await originalFetch(finalInput, init);

      // Handle HTTP error status codes (401, 500, etc.) for API calls
      if (resolvedUrl.includes('/api/')) {
        return await handleHttpErrorResponse(response, resolvedUrl);
      }

      return response;
    } catch (error) {
      // Handle network errors for API calls
      if (resolvedUrl.includes('/api/')) {
        handleHttpError(error, resolvedUrl);
      }
      throw error;
    }
  };
}

// Log environment variables
console.info('[Main] import.meta.env.MODE:', import.meta.env.MODE);
console.info('[Main] import.meta.env.DEV:', import.meta.env.DEV);
console.info('[Main] import.meta.env.PROD:', import.meta.env.PROD);

// Log Windows environment information for debugging
if (import.meta.env.DEV) {
  console.info('[Main] Running Windows environment logging...');
  logWindowsEnvironment();
}

console.info('[Main] Starting environment validation...');

// Validate environment before rendering
try {
  validateEnvironment();
  console.info('[Main] ✓ Environment validation passed');
} catch (error) {
  console.error('[Main] ✗ Environment validation failed:', error);
  // Display error in DOM with clear styling
  const root = document.getElementById('root');
  if (root) {
    root.innerHTML = `
      <div style="
        font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
        max-width: 800px;
        margin: 60px auto;
        padding: 40px;
        background: #fff3cd;
        border: 2px solid #ffc107;
        border-radius: 8px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      ">
        <h1 style="color: #856404; margin: 0 0 20px 0; font-size: 24px;">
          ⚠️ Configuration Error
        </h1>
        <div style="
          background: white;
          padding: 20px;
          border-radius: 4px;
          border-left: 4px solid #ffc107;
          margin-bottom: 20px;
        ">
          <pre style="
            white-space: pre-wrap;
            word-wrap: break-word;
            margin: 0;
            color: #333;
            font-size: 14px;
            line-height: 1.6;
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
          ">${error instanceof Error ? error.message : String(error)}</pre>
        </div>
        <div style="
          background: #f8f9fa;
          padding: 15px;
          border-radius: 4px;
          font-size: 13px;
          color: #666;
        ">
          <strong>For detailed instructions, check the browser console (F12)</strong>
        </div>
      </div>
    `;
  }

  // Also log to console for developers
  console.error('Environment validation failed:', error);

  // Stop execution - don't render the app
  throw error;
}

// ===== BACKEND WAIT SCREEN HELPER FUNCTIONS (START) =====
// These must be declared before startReactApp() to avoid TDZ issues

const backendStatusContainerStyles = `
  font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle at top, #0f172a, #020617);
  color: white;
  text-align: center;
  padding: 0 32px;
`;

function getBackendStatusMarkup(state: 'waiting' | 'ready' | 'failed', error?: unknown): string {
  const titleByState: Record<typeof state, string> = {
    waiting: 'Starting Aura Services…',
    ready: 'Aura Services Ready',
    failed: 'Unable to Reach Local Backend',
  };

  const bodyByState: Record<typeof state, string> = {
    waiting: 'Connecting to the local AI services. This usually takes a few seconds…',
    ready: 'Launching the studio UI…',
    failed:
      'Aura could not connect to the local backend service. Please leave the app open while we retry or click the button below.',
  };

  const errorDetail =
    state === 'failed' && error
      ? `<pre style="
            background: rgba(255,255,255,0.08);
            padding: 12px;
            border-radius: 6px;
            font-size: 13px;
            max-width: 560px;
            margin: 16px auto 0;
            white-space: pre-wrap;
          ">${error instanceof Error ? error.message : String(error)}</pre>`
      : '';

  const retryButton =
    state === 'failed'
      ? `<button
          style="
            margin-top: 24px;
            padding: 10px 24px;
            border-radius: 999px;
            border: none;
            cursor: pointer;
            font-size: 15px;
            font-weight: 600;
            color: #0f172a;
          "
          onclick="window.location.reload()"
        >
          Retry Connection
        </button>`
      : '';

  return `
    <div>
      <div style="font-size: 32px; font-weight: 600; margin-bottom: 16px;">
        ${titleByState[state]}
      </div>
      <div style="font-size: 16px; color: rgba(255,255,255,0.85); max-width: 520px; margin: 0 auto;">
        ${bodyByState[state]}
      </div>
      ${errorDetail}
      ${retryButton}
    </div>
  `;
}

async function renderBackendWaitScreen(rootEl: HTMLElement): Promise<void> {
  const statusContainer = document.createElement('div');
  statusContainer.style.cssText = backendStatusContainerStyles;
  rootEl.replaceChildren(statusContainer);

  statusContainer.innerHTML = getBackendStatusMarkup('waiting');

  try {
    await waitForBackendReady();
    statusContainer.innerHTML = getBackendStatusMarkup('ready');
  } catch (error) {
    statusContainer.innerHTML = getBackendStatusMarkup('failed', error);
    throw error;
  }
}

const BACKEND_WAIT_TIMEOUT_MS = 45_000;
const BACKEND_WAIT_POLL_INTERVAL_MS = 1_000;

async function waitForBackendReady(): Promise<void> {
  const start = Date.now();

  while (Date.now() - start < BACKEND_WAIT_TIMEOUT_MS) {
    const ready = await probeBackendOnce();
    if (ready) {
      await sleep(250);
      console.info('[Main] Aura backend is ready.');
      return;
    }

    await sleep(BACKEND_WAIT_POLL_INTERVAL_MS);
  }

  throw new Error(
    `Aura backend did not become ready within ${Math.round(
      BACKEND_WAIT_TIMEOUT_MS / 1000
    )} seconds.`
  );
}

async function probeBackendOnce(): Promise<boolean> {
  if (typeof window === 'undefined') {
    return false;
  }

  if (window.aura?.backend?.health) {
    try {
      const ipcHealth = await window.aura.backend.health();
      const status = ipcHealth?.status ?? ipcHealth?.state;
      if (ipcHealth?.healthy === true || /healthy/i.test(String(status ?? ''))) {
        return true;
      }
    } catch (error) {
      console.warn('[Main] Backend IPC health probe failed:', error);
    }
  }

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 4_000);
    const response = await fetch(apiUrl('/api/health/ready'), {
      method: 'GET',
      cache: 'no-store',
      signal: controller.signal,
    });
    clearTimeout(timeoutId);

    if (response.ok) {
      return true;
    }
  } catch (error) {
    console.warn('[Main] Backend HTTP readiness probe failed:', error);
  }

  return false;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function showFatalBootstrapError(rootEl: HTMLElement, error: unknown): void {
  rootEl.innerHTML = `
    <div style="
      font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
      max-width: 840px;
      margin: 60px auto;
      padding: 40px;
      background: #fff1f2;
      border: 2px solid #fb7185;
      border-radius: 12px;
      box-shadow: 0 10px 30px rgba(15,23,42,0.15);
      text-align: center;
    ">
      <h1 style="color: #be123c; margin-bottom: 16px;">Unable to Start Aura</h1>
      <p style="color: #1f2937; font-size: 16px;">
        ${error instanceof Error ? error.message : String(error)}
      </p>
      <p style="color: #4b5563; font-size: 14px; margin-top: 12px;">
        Please ensure the Aura backend is not blocked by antivirus or firewall software,
        then click Retry.
      </p>
      <button
        style="
          margin-top: 24px;
          padding: 12px 32px;
          border-radius: 999px;
          border: none;
          cursor: pointer;
          font-size: 15px;
          font-weight: 600;
          background: linear-gradient(135deg, #ec4899, #9333ea);
          color: white;
        "
        onclick="window.location.reload()"
      >
        Retry Startup
      </button>
    </div>
  `;
}

function shouldWaitForBackend(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  if (import.meta.env.VITE_SKIP_BACKEND_WAIT === 'true') {
    return false;
  }

  return Boolean(window.aura?.backend || window.desktopBridge);
}

// ===== BACKEND WAIT SCREEN HELPER FUNCTIONS (END) =====

let rootElement: HTMLElement | null = null;

void startReactApp().catch((error) => {
  console.error('[Main] ✗ Failed to bootstrap Aura UI:', error);
  if (rootElement) {
    showFatalBootstrapError(rootElement, error);
  }
});

async function startReactApp(): Promise<void> {
  console.info('[Main] Creating React root...');
  rootElement = document.getElementById('root');
  console.info('[Main] Root element exists:', rootElement !== null);

  if (!rootElement) {
    throw new Error('Root element #root not found in DOM');
  }

  console.info('[Main] Root element ready:', rootElement.innerHTML.length === 0);

  if (shouldWaitForBackend()) {
    await renderBackendWaitScreen(rootElement);
  } else {
    console.info('[Main] Backend wait skipped (non-Electron environment).');
  }

  console.info('[Main] Calling ReactDOM.createRoot...');
  const root = ReactDOM.createRoot(rootElement);

  console.info('[Main] Rendering App component with ErrorBoundary...');
  console.info('[Main] Current state:', {
    rootElementExists: !!rootElement,
    rootElementEmpty: rootElement?.innerHTML.length === 0,
    timestamp: new Date().toISOString(),
  });

  root.render(
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  );

  console.info('[Main] ✓ React render call completed');
  console.info('[Main] React should now hydrate and call App component');

  // Black screen prevention: Comprehensive monitoring and recovery
  // Respects user's theme preference from localStorage
  const ensureRootBackground = () => {
    if (rootElement) {
      const computedStyle = window.getComputedStyle(rootElement);
      const bgColor = computedStyle.backgroundColor;
      // If background is black, transparent, or not set, apply a safe default
      if (
        bgColor === 'rgba(0, 0, 0, 0)' ||
        bgColor === 'transparent' ||
        bgColor === 'rgb(0, 0, 0)' ||
        !bgColor ||
        bgColor === 'initial'
      ) {
        // Get user's theme preference from localStorage
        const savedDarkMode = localStorage.getItem('darkMode');
        const isDarkMode =
          savedDarkMode !== null
            ? JSON.parse(savedDarkMode)
            : window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

        // Get theme name preference (defaults to 'aura')
        const themeName = localStorage.getItem('themeName') || 'aura';

        // Get the appropriate theme based on user preference
        const theme =
          themeName === 'aura'
            ? getAuraTheme(isDarkMode)
            : isDarkMode
              ? webDarkTheme
              : webLightTheme;

        // Use theme background color with fallback
        const safeBgColor = theme.colorNeutralBackground1 || (isDarkMode ? '#1e1e1e' : '#ffffff');

        rootElement.style.backgroundColor = safeBgColor;
        document.body.style.backgroundColor = safeBgColor;
        console.warn(
          `[Main] Applied fallback background (${isDarkMode ? 'dark' : 'light'} theme) to prevent black screen`
        );
      }
    }
  };

  // Black screen detection: Check if the app has rendered content
  // CRITICAL: Do not trigger during setup wizard - it may take time to render
  const detectBlackScreen = () => {
    if (!rootElement) return;

    // Wait a bit for React to render
    setTimeout(() => {
      // Check if we're in the setup wizard - if so, skip black screen detection
      // The wizard may take time to render, especially during step transitions
      const isWizardActive =
        rootElement!.querySelector('[data-wizard-active]') !== null ||
        rootElement!.textContent?.includes('Welcome to Aura Video Studio') ||
        rootElement!.textContent?.includes("Let's get you set up") ||
        window.location.hash.includes('onboarding') ||
        document.body.getAttribute('data-wizard-active') === 'true';

      if (isWizardActive) {
        console.info('[Main] Setup wizard detected - skipping black screen detection');
        return;
      }

      const hasChildren = rootElement!.children.length > 0;
      const hasTextContent = rootElement!.textContent && rootElement!.textContent.trim().length > 0;
      const computedStyle = window.getComputedStyle(rootElement!);
      const bgColor = computedStyle.backgroundColor;
      const isCompletelyBlack = bgColor === 'rgb(0, 0, 0)';

      // If root has no children, no text, and is black - we have a black screen
      // But only if we're NOT in the wizard
      if (!hasChildren && !hasTextContent && isCompletelyBlack) {
        console.error('[Main] ⚠️ BLACK SCREEN DETECTED: Root element is empty and black!');
        console.error('[Main] Root element state:', {
          childrenCount: rootElement!.children.length,
          textContent: rootElement!.textContent?.substring(0, 100),
          backgroundColor: bgColor,
          innerHTML: rootElement!.innerHTML.substring(0, 200),
        });

        // Try to recover by forcing a reload
        console.warn('[Main] Attempting recovery by reloading page...');
        window.location.reload();
      }
    }, 10000); // Increased to 10 seconds to give wizard more time to render
  };

  // Check immediately and after delays
  ensureRootBackground();
  setTimeout(ensureRootBackground, 1000);
  setTimeout(ensureRootBackground, 3000);

  // Detect black screens after React has had time to render
  detectBlackScreen();

  // Clear initialization timeout - app has successfully hydrated
  if (window.__initTimeout) {
    clearTimeout(window.__initTimeout);
  }
}

// ===== INITIALIZATION LOGGING (END) =====

// Add type declaration for the global timeout
declare global {
  interface Window {
    __initTimeout?: number;
  }
}
