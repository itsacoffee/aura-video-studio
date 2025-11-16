/**
 * Ambient declarations for Window and global extensions
 *
 * This file extends the global Window interface to include:
 * - Electron API (window.electron)
 * - Aura runtime globals (AURA_IS_ELECTRON, AURA_BACKEND_URL, etc.)
 * - Browser API extensions (webkitdirectory for HTMLInputElement)
 */

import type { ElectronAPI } from '../electron-menu';

export interface DesktopBridgeBackendInfo {
  baseUrl: string;
  port?: number;
  protocol?: string;
  managedByElectron?: boolean;
  healthEndpoint?: string;
  readinessEndpoint?: string;
  pid?: number | null;
}

export interface DesktopBridgeEnvironmentInfo {
  mode?: string;
  isPackaged?: boolean;
  version?: string;
}

export interface DesktopBridgeDiagnostics {
  backend?: DesktopBridgeBackendInfo | null;
  environment?: DesktopBridgeEnvironmentInfo | null;
  os?: {
    platform?: string;
    release?: string;
    arch?: string;
    hostname?: string;
  } | null;
  paths?: {
    userData?: string;
    temp?: string;
    logs?: string;
  } | null;
  [key: string]: unknown;
}

export interface DesktopBridge {
  getBackendBaseUrl(): string | null;
  getAppEnvironment(): string;
  getDiagnosticInfo(): Promise<DesktopBridgeDiagnostics | null>;
  getCachedDiagnostics(): DesktopBridgeDiagnostics | null;
  backend?: DesktopBridgeBackendInfo | null;
  environment?: DesktopBridgeEnvironmentInfo | null;
  os?: DesktopBridgeDiagnostics['os'];
  paths?: DesktopBridgeDiagnostics['paths'];
  onBackendHealthUpdate?(callback: (...args: unknown[]) => void): () => void;
  onBackendProviderUpdate?(callback: (...args: unknown[]) => void): () => void;
}

declare global {
  interface Window {
    /**
     * Electron API exposed through preload script
     * Available only when running in Electron desktop app
     */
    aura?: ElectronAPI;
    electron?: ElectronAPI;

    /**
     * Strongly-typed runtime bridge exposed by the preload script
     */
    desktopBridge?: DesktopBridge;

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

export { };

