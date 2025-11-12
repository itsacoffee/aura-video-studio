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
 * Data payload for openRecentProject event
 */
export interface OpenRecentProjectData {
  path: string;
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
  onOpenRecentProject: (callback: MenuEventHandlerWithData<OpenRecentProjectData>) => MenuEventUnsubscribe;
  
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

/**
 * Complete Electron API exposed to renderer through window.electron
 */
export interface ElectronAPI {
  menu?: MenuAPI;
  // Other Electron APIs would be defined here (config, dialog, shell, etc.)
}

/**
 * Augment global Window interface with Electron API
 */
declare global {
  interface Window {
    electron?: ElectronAPI;
  }
}
