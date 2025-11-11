/**
 * Enhanced Electron Preload Script
 * 
 * This script runs in a sandboxed context with access to both Node.js APIs
 * and the renderer process DOM. It exposes a safe, typed API to the renderer
 * via contextBridge with full security measures.
 */

const { contextBridge, ipcRenderer } = require('electron');

// Validate IPC channel names to prevent injection attacks
const VALID_CHANNELS = {
  // Configuration channels
  CONFIG: ['config:get', 'config:set', 'config:getAll', 'config:reset', 
           'config:getSecure', 'config:setSecure', 'config:deleteSecure',
           'config:addRecentProject', 'config:getRecentProjects', 'config:clearRecentProjects', 'config:removeRecentProject'],
  
  // Dialog channels
  DIALOG: ['dialog:openFolder', 'dialog:openFile', 'dialog:openMultipleFiles', 
           'dialog:saveFile', 'dialog:showMessage', 'dialog:showError'],
  
  // Shell channels
  SHELL: ['shell:openExternal', 'shell:openPath', 'shell:showItemInFolder', 'shell:trashItem'],
  
  // App channels
  APP: ['app:getVersion', 'app:getName', 'app:getPaths', 'app:getLocale', 
        'app:isPackaged', 'app:restart', 'app:quit'],
  
  // Window channels
  WINDOW: ['window:minimize', 'window:maximize', 'window:close', 'window:hide', 'window:show'],
  
  // Video generation channels
  VIDEO: ['video:generate:start', 'video:generate:pause', 'video:generate:resume', 
          'video:generate:cancel', 'video:generate:status', 'video:generate:list'],
  
  // Backend channels
  BACKEND: ['backend:getUrl', 'backend:health', 'backend:ping', 'backend:info', 
            'backend:version', 'backend:providerStatus', 'backend:ffmpegStatus',
            'backend:restart', 'backend:stop', 'backend:status',
            'backend:checkFirewall', 'backend:getFirewallRule', 'backend:getFirewallCommand'],
  
  // Update channels
  UPDATES: ['updates:check']
};

// Event channels that renderer can listen to
const VALID_EVENT_CHANNELS = [
  'video:progress',
  'video:error',
  'video:complete',
  'backend:healthUpdate',
  'backend:providerUpdate',
  'protocol:navigate',
  'menu:newProject',
  'menu:openProject',
  'menu:openRecentProject',
  'menu:saveProject',
  'menu:saveProjectAs',
  'menu:importVideo',
  'menu:importAudio',
  'menu:importImages',
  'menu:importDocument',
  'menu:exportVideo',
  'menu:exportTimeline',
  'menu:find',
  'menu:openPreferences',
  'menu:openProviderSettings',
  'menu:openFFmpegConfig',
  'menu:clearCache',
  'menu:viewLogs',
  'menu:runDiagnostics',
  'menu:openGettingStarted',
  'menu:showKeyboardShortcuts',
  'menu:checkForUpdates'
];

/**
 * Validate channel name
 */
function isValidChannel(channel) {
  return Object.values(VALID_CHANNELS).flat().includes(channel);
}

/**
 * Validate event channel name
 */
function isValidEventChannel(channel) {
  return VALID_EVENT_CHANNELS.includes(channel);
}

/**
 * Safe IPC invoke wrapper
 */
async function safeInvoke(channel, ...args) {
  if (!isValidChannel(channel)) {
    throw new Error(`Invalid IPC channel: ${channel}`);
  }
  return ipcRenderer.invoke(channel, ...args);
}

/**
 * Safe IPC event listener wrapper
 */
function safeOn(channel, callback) {
  if (!isValidEventChannel(channel)) {
    throw new Error(`Invalid event channel: ${channel}`);
  }
  
  const subscription = (event, ...args) => callback(...args);
  ipcRenderer.on(channel, subscription);
  
  // Return unsubscribe function
  return () => ipcRenderer.removeListener(channel, subscription);
}

/**
 * Safe IPC once listener wrapper
 */
function safeOnce(channel, callback) {
  if (!isValidEventChannel(channel)) {
    throw new Error(`Invalid event channel: ${channel}`);
  }
  
  ipcRenderer.once(channel, (event, ...args) => callback(...args));
}

// Expose safe API to renderer process
contextBridge.exposeInMainWorld('electron', {
  // Configuration management
  config: {
    get: (key, defaultValue) => safeInvoke('config:get', key, defaultValue),
    set: (key, value) => safeInvoke('config:set', key, value),
    getAll: () => safeInvoke('config:getAll'),
    reset: () => safeInvoke('config:reset'),
    getSecure: (key) => safeInvoke('config:getSecure', key),
    setSecure: (key, value) => safeInvoke('config:setSecure', key, value),
    deleteSecure: (key) => safeInvoke('config:deleteSecure', key),
    addRecentProject: (path, name) => safeInvoke('config:addRecentProject', path, name),
    getRecentProjects: () => safeInvoke('config:getRecentProjects'),
    clearRecentProjects: () => safeInvoke('config:clearRecentProjects'),
    removeRecentProject: (path) => safeInvoke('config:removeRecentProject', path)
  },
  
  // File/folder dialogs
  dialog: {
    openFolder: () => safeInvoke('dialog:openFolder'),
    openFile: (options) => safeInvoke('dialog:openFile', options),
    openMultipleFiles: (options) => safeInvoke('dialog:openMultipleFiles', options),
    saveFile: (options) => safeInvoke('dialog:saveFile', options),
    showMessage: (options) => safeInvoke('dialog:showMessage', options),
    showError: (title, message) => safeInvoke('dialog:showError', title, message)
  },
  
  // Shell operations
  shell: {
    openExternal: (url) => safeInvoke('shell:openExternal', url),
    openPath: (path) => safeInvoke('shell:openPath', path),
    showItemInFolder: (path) => safeInvoke('shell:showItemInFolder', path),
    trashItem: (path) => safeInvoke('shell:trashItem', path)
  },
  
  // App information
  app: {
    getVersion: () => safeInvoke('app:getVersion'),
    getName: () => safeInvoke('app:getName'),
    getPaths: () => safeInvoke('app:getPaths'),
    getLocale: () => safeInvoke('app:getLocale'),
    isPackaged: () => safeInvoke('app:isPackaged'),
    restart: () => safeInvoke('app:restart'),
    quit: () => safeInvoke('app:quit')
  },
  
  // Window operations
  window: {
    minimize: () => safeInvoke('window:minimize'),
    maximize: () => safeInvoke('window:maximize'),
    close: () => safeInvoke('window:close'),
    hide: () => safeInvoke('window:hide'),
    show: () => safeInvoke('window:show')
  },
  
  // Video generation
  video: {
    generate: {
      start: (config) => safeInvoke('video:generate:start', config),
      pause: (generationId) => safeInvoke('video:generate:pause', generationId),
      resume: (generationId) => safeInvoke('video:generate:resume', generationId),
      cancel: (generationId) => safeInvoke('video:generate:cancel', generationId),
      status: (generationId) => safeInvoke('video:generate:status', generationId),
      list: () => safeInvoke('video:generate:list')
    },
    onProgress: (callback) => safeOn('video:progress', callback),
    onError: (callback) => safeOn('video:error', callback),
    onComplete: (callback) => safeOn('video:complete', callback)
  },
  
  // Backend service
  backend: {
    getUrl: () => safeInvoke('backend:getUrl'),
    health: () => safeInvoke('backend:health'),
    ping: () => safeInvoke('backend:ping'),
    info: () => safeInvoke('backend:info'),
    version: () => safeInvoke('backend:version'),
    providerStatus: () => safeInvoke('backend:providerStatus'),
    ffmpegStatus: () => safeInvoke('backend:ffmpegStatus'),
    restart: () => safeInvoke('backend:restart'),
    stop: () => safeInvoke('backend:stop'),
    status: () => safeInvoke('backend:status'),
    checkFirewall: () => safeInvoke('backend:checkFirewall'),
    getFirewallRule: () => safeInvoke('backend:getFirewallRule'),
    getFirewallCommand: () => safeInvoke('backend:getFirewallCommand'),
    onHealthUpdate: (callback) => safeOn('backend:healthUpdate', callback),
    onProviderUpdate: (callback) => safeOn('backend:providerUpdate', callback)
  },
  
  // Update management
  updates: {
    check: () => safeInvoke('updates:check')
  },
  
  // Protocol handling
  protocol: {
    onNavigate: (callback) => safeOn('protocol:navigate', callback)
  },
  
  // Menu actions
  menu: {
    onNewProject: (callback) => safeOn('menu:newProject', callback),
    onOpenProject: (callback) => safeOn('menu:openProject', callback),
    onOpenRecentProject: (callback) => safeOn('menu:openRecentProject', callback),
    onSaveProject: (callback) => safeOn('menu:saveProject', callback),
    onSaveProjectAs: (callback) => safeOn('menu:saveProjectAs', callback),
    onImportVideo: (callback) => safeOn('menu:importVideo', callback),
    onImportAudio: (callback) => safeOn('menu:importAudio', callback),
    onImportImages: (callback) => safeOn('menu:importImages', callback),
    onImportDocument: (callback) => safeOn('menu:importDocument', callback),
    onExportVideo: (callback) => safeOn('menu:exportVideo', callback),
    onExportTimeline: (callback) => safeOn('menu:exportTimeline', callback),
    onFind: (callback) => safeOn('menu:find', callback),
    onOpenPreferences: (callback) => safeOn('menu:openPreferences', callback),
    onOpenProviderSettings: (callback) => safeOn('menu:openProviderSettings', callback),
    onOpenFFmpegConfig: (callback) => safeOn('menu:openFFmpegConfig', callback),
    onClearCache: (callback) => safeOn('menu:clearCache', callback),
    onViewLogs: (callback) => safeOn('menu:viewLogs', callback),
    onRunDiagnostics: (callback) => safeOn('menu:runDiagnostics', callback),
    onOpenGettingStarted: (callback) => safeOn('menu:openGettingStarted', callback),
    onShowKeyboardShortcuts: (callback) => safeOn('menu:showKeyboardShortcuts', callback),
    onCheckForUpdates: (callback) => safeOn('menu:checkForUpdates', callback)
  },
  
  // Platform detection
  platform: {
    isElectron: true,
    os: process.platform,
    arch: process.arch,
    isWindows: process.platform === 'win32',
    isMac: process.platform === 'darwin',
    isLinux: process.platform === 'linux',
    versions: {
      node: process.versions.node,
      chrome: process.versions.chrome,
      electron: process.versions.electron
    }
  }
});

// Log preload script initialization
console.log('Enhanced preload script loaded');
console.log('Platform:', process.platform);
console.log('Architecture:', process.arch);
console.log('Electron version:', process.versions.electron);
console.log('Node version:', process.versions.node);
console.log('Chrome version:', process.versions.chrome);
console.log('Context isolation enabled: true');
console.log('Node integration disabled: true');
