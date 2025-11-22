export interface ElectronAPI {
  getBackendUrl: () => Promise<string>;
  isBackendRunning: () => Promise<boolean>;
  restartBackend: () => Promise<{ success: boolean; error?: string }>;
  onBackendUrl: (callback: (url: string) => void) => void;
}

declare global {
  interface Window {
    electronAPI: ElectronAPI;
  }
}
