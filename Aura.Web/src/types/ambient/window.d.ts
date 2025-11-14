/**
 * Ambient declarations for Window and global extensions
 *
 * This file extends the global Window interface to include:
 * - Electron API (window.electron)
 * - Aura runtime globals (AURA_IS_ELECTRON, AURA_BACKEND_URL, etc.)
 * - Browser API extensions (webkitdirectory for HTMLInputElement)
 */

import type { ElectronAPI } from '../electron-menu';

declare global {
  interface Window {
    /**
     * Electron API exposed through preload script
     * Available only when running in Electron desktop app
     */
    electron?: ElectronAPI;

    /**
     * Backend API URL (injected by Electron or build process)
     */
    AURA_BACKEND_URL?: string;

    /**
     * Flag indicating whether running in Electron
     */
    AURA_IS_ELECTRON?: boolean;

    /**
     * Flag indicating development mode
     */
    AURA_IS_DEV?: boolean;

    /**
     * Application version string
     */
    AURA_VERSION?: string;
  }

  interface HTMLInputElement {
    /**
     * Webkit-specific directory picker attribute
     * Used for folder selection in file inputs
     */
    webkitdirectory?: boolean;
  }
}

export {};
