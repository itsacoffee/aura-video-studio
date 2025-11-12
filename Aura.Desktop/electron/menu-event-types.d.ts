/**
 * TypeScript definitions for Menu Event types
 * 
 * This ensures type safety between Electron preload and React components
 * for all menu event channels.
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
 * Menu event handler type with no parameters
 */
export type MenuEventHandler = () => void;

/**
 * Menu event handler with data parameter (for events like openRecentProject)
 */
export type MenuEventHandlerWithData<T = unknown> = (data: T) => void;

/**
 * Unsubscribe function returned by menu event listeners
 */
export type MenuEventUnsubscribe = () => void;

/**
 * Menu API interface exposed to renderer
 */
export interface MenuAPI {
  onNewProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenRecentProject: (callback: MenuEventHandlerWithData<{ path: string }>) => MenuEventUnsubscribe;
  onSaveProject: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onSaveProjectAs: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onImportVideo: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onImportAudio: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onImportImages: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onImportDocument: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onExportVideo: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onExportTimeline: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onFind: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenPreferences: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenProviderSettings: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenFFmpegConfig: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onClearCache: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onViewLogs: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onRunDiagnostics: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onOpenGettingStarted: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onShowKeyboardShortcuts: (callback: MenuEventHandler) => MenuEventUnsubscribe;
  onCheckForUpdates: (callback: MenuEventHandler) => MenuEventUnsubscribe;
}

/**
 * Validation result for menu event exposure
 */
export interface MenuEventValidationResult {
  valid: boolean;
  missingChannels: string[];
  unexposedChannels: string[];
}
