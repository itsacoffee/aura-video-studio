/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_APP_VERSION: string;
  readonly VITE_APP_NAME: string;
  readonly VITE_ENV: string;
  readonly VITE_ENABLE_ANALYTICS: string;
  readonly VITE_ENABLE_DEBUG: string;
  readonly VITE_ENABLE_DEV_TOOLS: string;
  readonly DEV: boolean;
  readonly PROD: boolean;
  readonly MODE: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

// Extend Window interface for Electron and browser APIs
interface Window {
  electron?: {
    selectFolder: () => Promise<string | null>;
    openPath: (path: string) => Promise<void>;
    openExternal: (url: string) => Promise<void>;
    backend?: {
      getUrl(): Promise<string>;
    };
  };
  // Global variables set by Electron
  AURA_BACKEND_URL?: string;
  AURA_IS_ELECTRON?: boolean;
  AURA_IS_DEV?: boolean;
  AURA_VERSION?: string;
}

// Extend HTMLInputElement for webkitdirectory attribute
interface HTMLInputElement {
  webkitdirectory?: boolean;
}
