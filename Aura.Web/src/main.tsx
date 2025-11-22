import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import './styles/component-overrides.css';
import './styles/windows11.css';
import { ErrorBoundary } from './components/ErrorBoundary';
import { apiUrl } from './config/api';
import { errorReportingService } from './services/errorReportingService';
import { loggingService } from './services/loggingService';
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
if (typeof window !== 'undefined' && typeof window.fetch === 'function') {
  const originalFetch = window.fetch.bind(window);

  window.fetch = (input: RequestInfo | URL, init?: RequestInit) => {
    if (typeof input === 'string' && input.startsWith('/api/')) {
      return originalFetch(apiUrl(input), init);
    }

    if (input instanceof URL && input.pathname.startsWith('/api/')) {
      const absoluteUrl = apiUrl(`${input.pathname}${input.search || ''}`);
      return originalFetch(absoluteUrl, init);
    }

    if (input instanceof Request && input.url.startsWith('/api/')) {
      const requestUrl = new URL(input.url, window.location.origin);
      const absoluteUrl = apiUrl(`${requestUrl.pathname}${requestUrl.search}`);
      const rewrittenRequest = new Request(absoluteUrl, input);
      return originalFetch(rewrittenRequest, init);
    }

    return originalFetch(input, init);
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

  // Clear initialization timeout - app has successfully hydrated
  if (window.__initTimeout) {
    clearTimeout(window.__initTimeout);
  }
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

// ===== INITIALIZATION LOGGING (END) =====

// Add type declaration for the global timeout
declare global {
  interface Window {
    __initTimeout?: number;
  }
}
