/**
 * TypeScript Type Definitions for Electron IPC
 * These types should be imported in the React frontend for type safety
 */

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

export interface AuraRuntimeDiagnostics {
  backend?: Record<string, unknown> | null;
  environment?: Record<string, unknown> | null;
  os?: Record<string, unknown> | null;
  paths?: Record<string, unknown> | null;
  [key: string]: unknown;
}

export interface AuraRuntimeAPI {
  getDiagnostics(): Promise<AuraRuntimeDiagnostics | null>;
  refresh(): Promise<AuraRuntimeDiagnostics | null>;
  getCachedDiagnostics(): AuraRuntimeDiagnostics | null;
  onBackendHealthUpdate(callback: (status: HealthStatus) => void): () => void;
  onBackendProviderUpdate(callback: (status: ProviderStatus) => void): () => void;
}

export interface AuraBackendAPI {
  getBaseUrl(): Promise<string | null>;
  getUrl(): Promise<string | null>;
  health(): Promise<HealthStatus>;
  ping(): Promise<PingResult>;
  info(): Promise<BackendInfo>;
  version(): Promise<VersionInfo>;
  providerStatus(): Promise<ProviderStatus>;
  ffmpegStatus(): Promise<FFmpegStatus>;
  restart(): Promise<unknown>;
  stop(): Promise<unknown>;
  status(): Promise<{
    running: boolean;
    port: number | null;
    url: string | null;
  }>;
  checkFirewall(): Promise<Record<string, unknown>>;
  getFirewallRule(): Promise<Record<string, unknown>>;
  getFirewallCommand(): Promise<string>;
  onHealthUpdate(callback: (status: HealthStatus) => void): () => void;
  onProviderUpdate(callback: (status: ProviderStatus) => void): () => void;
}

export interface AuraFFmpegAPI {
  checkStatus(): Promise<FFmpegStatus>;
  install(options?: Record<string, unknown>): Promise<unknown>;
  getProgress(): Promise<Record<string, unknown>>;
  openDirectory(): Promise<{ success: boolean }>;
}

export interface AuraDialogsAPI {
  openFolder(): Promise<string | null>;
  openFile(options?: DialogOptions): Promise<string | null>;
  openMultipleFiles(options?: DialogOptions): Promise<string[]>;
  saveFile(options?: SaveDialogOptions): Promise<string | null>;
  showMessage(options: MessageBoxOptions): Promise<number>;
  showError(title: string, message: string): Promise<boolean>;
}

export interface AuraShellAPI {
  openExternal(url: string): Promise<{ success: boolean }>;
  openPath(path: string): Promise<{ success: boolean }>;
  showItemInFolder(path: string): Promise<{ success: boolean }>;
  trashItem(path: string): Promise<{ success: boolean }>;
}

export interface AuraAppAPI {
  getVersion(): Promise<string>;
  getName(): Promise<string>;
  getPaths(): Promise<AppPaths>;
  getLocale(): Promise<string>;
  isPackaged(): Promise<boolean>;
  restart(): Promise<void>;
  quit(): Promise<void>;
}

export interface AuraWindowAPI {
  minimize(): Promise<{ success: boolean }>;
  maximize(): Promise<{ success: boolean; isMaximized: boolean }>;
  close(): Promise<{ success: boolean }>;
  hide(): Promise<{ success: boolean }>;
  show(): Promise<{ success: boolean }>;
}

export interface AuraVideoAPI {
  generate: {
    start(config: VideoGenerationConfig): Promise<VideoGeneration>;
    pause(generationId: string): Promise<unknown>;
    resume(generationId: string): Promise<unknown>;
    cancel(generationId: string): Promise<unknown>;
    status(generationId: string): Promise<VideoGenerationStatus>;
    list(): Promise<VideoGeneration[]>;
  };
  onProgress(callback: (data: VideoProgressData) => void): () => void;
  onError(callback: (data: VideoErrorData) => void): () => void;
  onComplete(callback: (data: VideoCompleteData) => void): () => void;
}

export interface AuraConfigAPI {
  get<T = unknown>(key: string, defaultValue?: T): Promise<T>;
  set(key: string, value: unknown): Promise<{ success: boolean }>;
  getAll(): Promise<Record<string, unknown>>;
  reset(): Promise<{ success: boolean }>;
  getSecure<T = unknown>(key: string): Promise<T>;
  setSecure(key: string, value: unknown): Promise<{ success: boolean }>;
  deleteSecure(key: string): Promise<{ success: boolean }>;
  addRecentProject(path: string, name: string): Promise<RecentProject[]>;
  getRecentProjects(): Promise<RecentProject[]>;
  clearRecentProjects(): Promise<{ success: boolean }>;
  removeRecentProject(path: string): Promise<RecentProject[]>;
  isSafeMode(): Promise<boolean>;
  getCrashCount(): Promise<number>;
  resetCrashCount(): Promise<{ success: boolean }>;
  deleteAndRestart(): Promise<unknown>;
  getConfigPath(): Promise<string>;
}

export interface AuraDiagnosticsAPI {
  runAll(): Promise<unknown>;
  checkFFmpeg(): Promise<unknown>;
  fixFFmpeg(): Promise<unknown>;
  checkAPI(): Promise<unknown>;
  fixAPI(): Promise<unknown>;
  checkProviders(): Promise<unknown>;
  fixProviders(): Promise<unknown>;
  checkDiskSpace(): Promise<unknown>;
  checkConfig(): Promise<unknown>;
}

export interface AuraUpdatesAPI {
  check(): Promise<UpdateCheckResult>;
}

export interface AuraProtocolAPI {
  onNavigate(callback: (data: ProtocolNavigateData) => void): () => void;
}

export interface AuraStartupLogsAPI {
  getLatest(): Promise<unknown>;
  getSummary(): Promise<unknown>;
  getLogContent(): Promise<unknown>;
  list(): Promise<unknown>;
  readFile(filePath: string): Promise<unknown>;
  openDirectory(): Promise<unknown>;
}

export interface AuraSystemAPI {
  getEnvironmentInfo(): Promise<{
    environment: Record<string, unknown> | null;
    os: Record<string, unknown> | null;
    paths: Record<string, unknown> | null;
    platform: AuraPlatformInfo;
  }>;
  getPaths(): Promise<AppPaths>;
}

export interface AuraAPI {
  env: AuraEnvInfo;
  platform: AuraPlatformInfo;
  runtime: AuraRuntimeAPI;
  backend: AuraBackendAPI;
  ffmpeg: AuraFFmpegAPI;
  dialogs: AuraDialogsAPI;
  shell: AuraShellAPI;
  app: AuraAppAPI;
  window: AuraWindowAPI;
  video: AuraVideoAPI;
  config: AuraConfigAPI;
  diagnostics: AuraDiagnosticsAPI;
  updates: AuraUpdatesAPI;
  protocol: AuraProtocolAPI;
  menu?: MenuAPI;
  startupLogs: AuraStartupLogsAPI;
  safeMode: {
    onStatus(callback: (status: SafeModeStatus) => void): () => void;
  };
  system: AuraSystemAPI;
  events: {
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
    aura?: AuraAPI;
    electron: AuraAPI;
    AURA_BACKEND_URL?: string;
    AURA_IS_ELECTRON?: boolean;
    AURA_IS_DEV?: boolean;
    AURA_VERSION?: string;
  }
}

export {};
