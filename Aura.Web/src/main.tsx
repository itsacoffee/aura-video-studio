import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import './styles/windows11.css';
import { validateEnvironment } from './utils/validateEnv';
import { logWindowsEnvironment } from './utils/windowsUtils';

// Log Windows environment information for debugging
if (import.meta.env.DEV) {
  logWindowsEnvironment();
}

// Validate environment before rendering
try {
  validateEnvironment();
} catch (error) {
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

ReactDOM.createRoot(document.getElementById('root')!).render(<App />);

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
