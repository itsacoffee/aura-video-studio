import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import './styles/windows11.css';
import { validateEnvironment } from './utils/validateEnv';
import { logWindowsEnvironment } from './utils/windowsUtils';

// ===== INITIALIZATION LOGGING (START) =====
console.log('[Main] ===== Aura Video Studio - React Initialization =====');
console.log('[Main] Timestamp:', new Date().toISOString());
console.log('[Main] Location:', window.location.href);
console.log('[Main] Protocol:', window.location.protocol);
console.log('[Main] User Agent:', navigator.userAgent);

// Check for Electron environment
console.log('[Main] Checking Electron environment...');
console.log('[Main] window.electron exists:', typeof (window as Window).electron !== 'undefined');
console.log('[Main] AURA_IS_ELECTRON:', window.AURA_IS_ELECTRON);
console.log('[Main] AURA_BACKEND_URL:', window.AURA_BACKEND_URL);
console.log('[Main] AURA_IS_DEV:', window.AURA_IS_DEV);
console.log('[Main] AURA_VERSION:', window.AURA_VERSION);

// Check for root element
const rootElement = document.getElementById('root');
console.log('[Main] Root element exists:', rootElement !== null);
if (rootElement) {
  console.log('[Main] Root element ready:', rootElement.innerHTML.length === 0);
}

// Log environment variables
console.log('[Main] import.meta.env.MODE:', import.meta.env.MODE);
console.log('[Main] import.meta.env.DEV:', import.meta.env.DEV);
console.log('[Main] import.meta.env.PROD:', import.meta.env.PROD);

// Log Windows environment information for debugging
if (import.meta.env.DEV) {
  console.log('[Main] Running Windows environment logging...');
  logWindowsEnvironment();
}

console.log('[Main] Starting environment validation...');

// Validate environment before rendering
try {
  validateEnvironment();
  console.log('[Main] ✓ Environment validation passed');
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

console.log('[Main] Creating React root...');
const rootEl = document.getElementById('root');
if (!rootEl) {
  console.error('[Main] ✗ FATAL: Root element not found!');
  throw new Error('Root element #root not found in DOM');
}

console.log('[Main] Calling ReactDOM.createRoot...');
const root = ReactDOM.createRoot(rootEl);

console.log('[Main] Rendering App component...');
root.render(<App />);

console.log('[Main] ✓ React render call completed');
console.log('[Main] Waiting for React hydration to complete...');
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
