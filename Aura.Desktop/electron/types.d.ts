/**
 * TypeScript Type Definitions for Electron IPC
 * These types should be imported in the React frontend for type safety
 */

export interface ElectronAPI {
  // Configuration
  config: {
    get<T = any>(key: string, defaultValue?: T): Promise<T>;
    set(key: string, value: any): Promise<{ success: boolean }>;
    getAll(): Promise<Record<string, any>>;
    reset(): Promise<{ success: boolean }>;
    getSecure(key: string): Promise<any>;
    setSecure(key: string, value: any): Promise<{ success: boolean }>;
    deleteSecure(key: string): Promise<{ success: boolean }>;
    addRecentProject(path: string, name: string): Promise<RecentProject[]>;
    getRecentProjects(): Promise<RecentProject[]>;
    clearRecentProjects(): Promise<{ success: boolean }>;
    removeRecentProject(path: string): Promise<RecentProject[]>;
  };

  // Dialogs
  dialog: {
    openFolder(): Promise<string | null>;
    openFile(options?: DialogOptions): Promise<string | null>;
    openMultipleFiles(options?: DialogOptions): Promise<string[]>;
    saveFile(options?: SaveDialogOptions): Promise<string | null>;
    showMessage(options: MessageBoxOptions): Promise<number>;
    showError(title: string, message: string): Promise<boolean>;
  };

  // Shell operations
  shell: {
    openExternal(url: string): Promise<{ success: boolean }>;
    openPath(path: string): Promise<{ success: boolean }>;
    showItemInFolder(path: string): Promise<{ success: boolean }>;
    trashItem(path: string): Promise<{ success: boolean }>;
  };

  // App information
  app: {
    getVersion(): Promise<string>;
    getName(): Promise<string>;
    getPaths(): Promise<AppPaths>;
    getLocale(): Promise<string>;
    isPackaged(): Promise<boolean>;
    restart(): Promise<void>;
    quit(): Promise<void>;
  };

  // Window operations
  window: {
    minimize(): Promise<{ success: boolean }>;
    maximize(): Promise<{ success: boolean; isMaximized: boolean }>;
    close(): Promise<{ success: boolean }>;
    hide(): Promise<{ success: boolean }>;
    show(): Promise<{ success: boolean }>;
  };

  // Video generation
  video: {
    generate: {
      start(config: VideoGenerationConfig): Promise<VideoGeneration>;
      pause(generationId: string): Promise<any>;
      resume(generationId: string): Promise<any>;
      cancel(generationId: string): Promise<any>;
      status(generationId: string): Promise<VideoGenerationStatus>;
      list(): Promise<VideoGeneration[]>;
    };
    onProgress(callback: (data: VideoProgressData) => void): () => void;
    onError(callback: (data: VideoErrorData) => void): () => void;
    onComplete(callback: (data: VideoCompleteData) => void): () => void;
  };

  // Backend service
  backend: {
    getUrl(): Promise<string>;
    health(): Promise<HealthStatus>;
    ping(): Promise<PingResult>;
    info(): Promise<BackendInfo>;
    version(): Promise<VersionInfo>;
    providerStatus(): Promise<ProviderStatus>;
    ffmpegStatus(): Promise<FFmpegStatus>;
    onHealthUpdate(callback: (status: HealthStatus) => void): () => void;
    onProviderUpdate(callback: (status: ProviderStatus) => void): () => void;
  };

  // Updates
  updates: {
    check(): Promise<UpdateCheckResult>;
  };

  // Protocol
  protocol: {
    onNavigate(callback: (data: ProtocolNavigateData) => void): () => void;
  };

  // Menu actions
  menu: {
    onNewProject(callback: () => void): () => void;
    onOpenProject(callback: () => void): () => void;
    onOpenRecentProject(callback: (data: { path: string }) => void): () => void;
    onSaveProject(callback: () => void): () => void;
    onSaveProjectAs(callback: () => void): () => void;
    onImportVideo(callback: () => void): () => void;
    onImportAudio(callback: () => void): () => void;
    onImportImages(callback: () => void): () => void;
    onImportDocument(callback: () => void): () => void;
    onExportVideo(callback: () => void): () => void;
    onExportTimeline(callback: () => void): () => void;
    onFind(callback: () => void): () => void;
    onOpenPreferences(callback: () => void): () => void;
    onOpenProviderSettings(callback: () => void): () => void;
    onOpenFFmpegConfig(callback: () => void): () => void;
    onClearCache(callback: () => void): () => void;
    onViewLogs(callback: () => void): () => void;
    onRunDiagnostics(callback: () => void): () => void;
    onOpenGettingStarted(callback: () => void): () => void;
    onShowKeyboardShortcuts(callback: () => void): () => void;
    onCheckForUpdates(callback: () => void): () => void;
  };

  // Platform info
  platform: {
    isElectron: true;
    os: string;
    arch: string;
    isWindows: boolean;
    isMac: boolean;
    isLinux: boolean;
    versions: {
      node: string;
      chrome: string;
      electron: string;
    };
  };
}

// Supporting Types

export interface RecentProject {
  path: string;
  name: string;
  timestamp: number;
}

export interface DialogOptions {
  title?: string;
  filters?: FileFilter[];
}

export interface SaveDialogOptions extends DialogOptions {
  defaultPath?: string;
}

export interface FileFilter {
  name: string;
  extensions: string[];
}

export interface MessageBoxOptions {
  type?: 'info' | 'warning' | 'error' | 'question';
  title?: string;
  message: string;
  detail?: string;
  buttons?: string[];
  defaultId?: number;
  cancelId?: number;
}

export interface AppPaths {
  userData: string;
  temp: string;
  home: string;
  documents: string;
  videos: string;
  downloads: string;
  desktop: string;
  logs: string;
  cache: string;
  projects: string;
}

export interface VideoGenerationConfig {
  [key: string]: any;
}

export interface VideoGeneration {
  id: string;
  generationId?: string;
  [key: string]: any;
}

export interface VideoGenerationStatus {
  [key: string]: any;
}

export interface VideoProgressData {
  generationId: string;
  progress: any;
}

export interface VideoErrorData {
  generationId: string;
  error: string;
}

export interface VideoCompleteData {
  generationId: string;
  result: any;
}

export interface HealthStatus {
  status: 'healthy' | 'unhealthy';
  data?: any;
  error?: string;
}

export interface PingResult {
  success: boolean;
  responseTime?: number;
  error?: string;
}

export interface BackendInfo {
  [key: string]: any;
}

export interface VersionInfo {
  [key: string]: any;
}

export interface ProviderStatus {
  [key: string]: any;
}

export interface FFmpegStatus {
  [key: string]: any;
}

export interface UpdateCheckResult {
  available: boolean;
  message?: string;
  [key: string]: any;
}

export interface ProtocolNavigateData {
  action: string;
  params: Record<string, string>;
  originalUrl: string;
}

// Global type augmentation for window object
declare global {
  interface Window {
    electron: ElectronAPI;
    AURA_BACKEND_URL?: string;
    AURA_IS_ELECTRON?: boolean;
    AURA_IS_DEV?: boolean;
    AURA_VERSION?: string;
  }
}

export {};
