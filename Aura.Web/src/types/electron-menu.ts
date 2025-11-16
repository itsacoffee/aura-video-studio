/**
 * Shared TypeScript Menu Event Types
 *
 * This file provides TypeScript definitions that match the menu event system
 * defined in menu-event-types.js and exposed through preload.js
 *
 * These types ensure type safety between:
 * - Electron main process (menu-builder.js)
 * - Preload script (preload.js)
 * - React components (useElectronMenuEvents.ts)
 */

/**
 * All valid menu event channel names
 */
export type MenuEventChannel =
  | 'menu:newProject'
  | 'menu:openProject'
  | 'menu:openRecentProject'
  | 'menu:saveProject'
  | 'menu:saveProjectAs'
  | 'menu:importVideo'
  | 'menu:importAudio'
  | 'menu:importImages'
  | 'menu:importDocument'
  | 'menu:exportVideo'
  | 'menu:exportTimeline'
  | 'menu:find'
  | 'menu:openPreferences'
  | 'menu:openProviderSettings'
  | 'menu:openFFmpegConfig'
  | 'menu:clearCache'
  | 'menu:viewLogs'
  | 'menu:runDiagnostics'
  | 'menu:openGettingStarted'
  | 'menu:showKeyboardShortcuts'
  | 'menu:checkForUpdates';

/**
 * Standard menu event handler with no parameters
 */
export type MenuEventHandler = () => void;

/**
 * Menu event handler with typed data parameter
 */
export type MenuEventHandlerWithData<T = unknown> = (data: T) => void;

/**
 * Unsubscribe function returned by all menu event listeners
 * Call this function to remove the event listener and prevent memory leaks
 */
export type MenuEventUnsubscribe = () => void;

/**
 * Enhanced menu event payload with validation and tracking metadata
 */
export interface EnhancedMenuPayload {
  _correlationId?: string;
  _timestamp?: string;
  _command?: {
    label: string;
    category: string;
    description: string;
  };
  _validationError?: string;
  _validationIssues?: Array<{ path: string[]; message: string }>;
  _error?: string;
  [key: string]: unknown;
}

/**
 * Data payload for openRecentProject event
 */
export interface OpenRecentProjectData extends EnhancedMenuPayload {
  path: string;
  name?: string;
}

/**
 * Complete Menu API interface exposed through window.electron.menu
 *
 * CRITICAL: This interface MUST match the actual implementation in preload.js
 * Every method here corresponds to a menu event channel in MENU_EVENT_CHANNELS
 */
export interface MenuAPI {
  /** Triggered when user selects File > New Project */
  onNewProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Open Project */
  onOpenProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects a recent project from File > Open Recent */
  onOpenRecentProject: (
    callback: MenuEventHandlerWithData<OpenRecentProjectData>
  ) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Save */
  onSaveProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Save As */
  onSaveProjectAs: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Import > Video */
  onImportVideo: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Import > Audio */
  onImportAudio: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Import > Images */
  onImportImages: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Import > Document */
  onImportDocument: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Export > Video */
  onExportVideo: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects File > Export > Timeline */
  onExportTimeline: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Edit > Find */
  onFind: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Edit > Preferences or Tools > Preferences */
  onOpenPreferences: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Tools > Provider Settings */
  onOpenProviderSettings: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Tools > FFmpeg Configuration */
  onOpenFFmpegConfig: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Tools > Clear Cache */
  onClearCache: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Tools > View Logs */
  onViewLogs: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Tools > Run Diagnostics */
  onRunDiagnostics: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Help > Getting Started Guide */
  onOpenGettingStarted: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Help > Keyboard Shortcuts */
  onShowKeyboardShortcuts: (callback: MenuEventHandler) => MenuEventUnsubscribe;

  /** Triggered when user selects Help > Check for Updates */
  onCheckForUpdates: (callback: MenuEventHandler) => MenuEventUnsubscribe;
}

export interface AuraEnvInfo {
  isElectron: boolean;
  isDev: boolean;
  mode: string;
  version: string;
  isPackaged: boolean;
}

export interface AuraPlatformInfo {
  os: string;
  arch: string;
  release: string;
  isWindows: boolean;
  isMac: boolean;
  isLinux: boolean;
  versions: {
    node: string;
    chrome: string;
    electron: string;
  };
}

export interface SafeModeStatus {
  enabled: boolean;
  crashCount: number;
  disabledFeatures: string[];
}

export interface AuraAPI {
  menu?: MenuAPI;
  env?: AuraEnvInfo;
  platform?: AuraPlatformInfo;
  runtime?: {
    getDiagnostics(): Promise<Record<string, unknown> | null>;
    refresh(): Promise<Record<string, unknown> | null>;
    getCachedDiagnostics(): Record<string, unknown> | null;
    onBackendHealthUpdate(callback: (status: unknown) => void): () => void;
    onBackendProviderUpdate(callback: (status: unknown) => void): () => void;
  };
  backend?: {
    getBaseUrl(): Promise<string | null>;
    getUrl(): Promise<string | null>;
    status(): Promise<Record<string, unknown>>;
    health(): Promise<Record<string, unknown>>;
    ping(): Promise<Record<string, unknown>>;
    info(): Promise<Record<string, unknown>>;
    version(): Promise<Record<string, unknown>>;
    providerStatus(): Promise<Record<string, unknown>>;
    ffmpegStatus(): Promise<Record<string, unknown>>;
    restart(): Promise<unknown>;
    stop(): Promise<unknown>;
    checkFirewall(): Promise<Record<string, unknown>>;
    getFirewallRule(): Promise<Record<string, unknown>>;
    getFirewallCommand(): Promise<string>;
    onHealthUpdate(callback: (status: unknown) => void): () => void;
    onProviderUpdate(callback: (status: unknown) => void): () => void;
  };
  ffmpeg?: {
    checkStatus(): Promise<Record<string, unknown>>;
    install(options?: Record<string, unknown>): Promise<unknown>;
    getProgress(): Promise<Record<string, unknown>>;
    openDirectory(): Promise<{ success: boolean }>;
  };
  dialogs?: {
    openFolder(): Promise<string | null>;
    openFile(options?: Record<string, unknown>): Promise<string | null>;
    openMultipleFiles(options?: Record<string, unknown>): Promise<string[]>;
    saveFile(options?: Record<string, unknown>): Promise<string | null>;
    showMessage(options: Record<string, unknown>): Promise<number>;
    showError(title: string, message: string): Promise<boolean>;
  };
  shell?: {
    openExternal(url: string): Promise<{ success: boolean }>;
    openPath(path: string): Promise<{ success: boolean }>;
    showItemInFolder(path: string): Promise<{ success: boolean }>;
    trashItem(path: string): Promise<{ success: boolean }>;
  };
  app?: {
    getVersion(): Promise<string>;
    getName(): Promise<string>;
    getPaths(): Promise<Record<string, unknown>>;
    getLocale(): Promise<string>;
    isPackaged(): Promise<boolean>;
    restart(): Promise<void>;
    quit(): Promise<void>;
  };
  window?: {
    minimize(): Promise<{ success: boolean }>;
    maximize(): Promise<{ success: boolean; isMaximized: boolean }>;
    close(): Promise<{ success: boolean }>;
    hide(): Promise<{ success: boolean }>;
    show(): Promise<{ success: boolean }>;
  };
  video?: {
    generate: {
      start(config: Record<string, unknown>): Promise<Record<string, unknown>>;
      pause(generationId: string): Promise<unknown>;
      resume(generationId: string): Promise<unknown>;
      cancel(generationId: string): Promise<unknown>;
      status(generationId: string): Promise<Record<string, unknown>>;
      list(): Promise<Record<string, unknown>[]>;
    };
    onProgress(callback: (data: unknown) => void): () => void;
    onError(callback: (data: unknown) => void): () => void;
    onComplete(callback: (data: unknown) => void): () => void;
  };
  config?: {
    get<T = unknown>(key: string, defaultValue?: T): Promise<T>;
    set(key: string, value: unknown): Promise<{ success: boolean }>;
    getAll(): Promise<Record<string, unknown>>;
    reset(): Promise<{ success: boolean }>;
    getSecure<T = unknown>(key: string): Promise<T>;
    setSecure(key: string, value: unknown): Promise<{ success: boolean }>;
    deleteSecure(key: string): Promise<{ success: boolean }>;
    addRecentProject(path: string, name: string): Promise<Array<Record<string, unknown>>>;
    getRecentProjects(): Promise<Array<Record<string, unknown>>>;
    clearRecentProjects(): Promise<{ success: boolean }>;
    removeRecentProject(path: string): Promise<Array<Record<string, unknown>>>;
    isSafeMode(): Promise<boolean>;
    getCrashCount(): Promise<number>;
    resetCrashCount(): Promise<{ success: boolean }>;
    deleteAndRestart(): Promise<unknown>;
    getConfigPath(): Promise<string>;
  };
  diagnostics?: {
    runAll(): Promise<unknown>;
    checkFFmpeg(): Promise<unknown>;
    fixFFmpeg(): Promise<unknown>;
    checkAPI(): Promise<unknown>;
    fixAPI(): Promise<unknown>;
    checkProviders(): Promise<unknown>;
    fixProviders(): Promise<unknown>;
    checkDiskSpace(): Promise<unknown>;
    checkConfig(): Promise<unknown>;
  };
  updates?: {
    check(): Promise<Record<string, unknown>>;
  };
  protocol?: {
    onNavigate(callback: (data: Record<string, unknown>) => void): () => void;
  };
  startupLogs?: {
    getLatest(): Promise<unknown>;
    getSummary(): Promise<unknown>;
    getLogContent(): Promise<unknown>;
    list(): Promise<unknown>;
    readFile(filePath: string): Promise<unknown>;
    openDirectory(): Promise<unknown>;
  };
  safeMode?: {
    onStatus(callback: (status: SafeModeStatus) => void): () => void;
  };
  system?: {
    getEnvironmentInfo(): Promise<Record<string, unknown> | null>;
    getPaths(): Promise<Record<string, unknown>>;
  };
  events?: {
    on<T = unknown>(channel: string, callback: (data: T) => void): () => void;
    once<T = unknown>(channel: string, callback: (data: T) => void): void;
  };
  invoke?<T = unknown>(channel: string, ...args: unknown[]): Promise<T>;
  on?<T = unknown>(channel: string, callback: (data: T) => void): () => void;
  once?<T = unknown>(channel: string, callback: (data: T) => void): void;
  selectFolder?(): Promise<string | null>;
  openPath?(path: string): Promise<{ success: boolean }>;
  openExternal?(url: string): Promise<{ success: boolean }>;
}

export type ElectronAPI = AuraAPI;
