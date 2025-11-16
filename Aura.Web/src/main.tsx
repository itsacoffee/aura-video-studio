import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import './styles/windows11.css';
import { errorReportingService } from './services/errorReportingService';
import { loggingService } from './services/loggingService';
import { validateEnvironment } from './utils/validateEnv';
import { logWindowsEnvironment } from './utils/windowsUtils';
import { apiUrl } from './config/api';

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
console.info('[Main] window.aura exists:', typeof (window as Window).aura !== 'undefined');
console.info('[Main] desktop bridge available:', !!window.desktopBridge);
console.info('[Main] aura backend URL:', desktopDiagnostics?.backend?.baseUrl);
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

// Check for root element
const rootElement = document.getElementById('root');
console.info('[Main] Root element exists:', rootElement !== null);
if (rootElement) {
  console.info('[Main] Root element ready:', rootElement.innerHTML.length === 0);
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

console.info('[Main] Creating React root...');
const rootEl = document.getElementById('root');
if (!rootEl) {
  console.error('[Main] ✗ FATAL: Root element not found!');
  throw new Error('Root element #root not found in DOM');
}

console.info('[Main] Calling ReactDOM.createRoot...');
const root = ReactDOM.createRoot(rootEl);

console.info('[Main] Rendering App component...');
root.render(<App />);

console.info('[Main] ✓ React render call completed');
console.info('[Main] Waiting for React hydration to complete...');
// ===== INITIALIZATION LOGGING (END) =====

// Clear initialization timeout - app has successfully hydrated
if (window.__initTimeout) {
  clearTimeout(window.__initTimeout);
}

// Add type declaration for the global timeout
declare global {
  interface Window {
    __initTimeout?: number;
  }
}
