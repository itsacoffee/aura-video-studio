import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.tsx';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// Clear initialization timeout - app has successfully hydrated
if (window.__initTimeout) {
  clearTimeout(window.__initTimeout);
  console.log('[Init] Application initialized successfully');
}

// Add type declaration for the global timeout
declare global {
  interface Window {
    __initTimeout?: number;
  }
}
