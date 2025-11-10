/**
 * Electron Preload Script
 * 
 * This script runs in a sandboxed context with access to both Node.js APIs
 * and the renderer process DOM. It exposes a safe API to the renderer via
 * contextBridge.
 */

const { contextBridge, ipcRenderer } = require('electron');

// Expose safe API to renderer process
contextBridge.exposeInMainWorld('electron', {
  // Configuration management
  config: {
    get: (key) => ipcRenderer.invoke('config:get', key),
    set: (key, value) => ipcRenderer.invoke('config:set', key, value),
    getAll: () => ipcRenderer.invoke('config:getAll'),
    reset: () => ipcRenderer.invoke('config:reset')
  },
  
  // File/folder dialogs
  dialog: {
    openFolder: () => ipcRenderer.invoke('dialog:openFolder'),
    openFile: (options) => ipcRenderer.invoke('dialog:openFile', options),
    saveFile: (options) => ipcRenderer.invoke('dialog:saveFile', options)
  },
  
  // Shell operations
  shell: {
    openExternal: (url) => ipcRenderer.invoke('shell:openExternal', url),
    openPath: (path) => ipcRenderer.invoke('shell:openPath', path)
  },
  
  // App information
  app: {
    getVersion: () => ipcRenderer.invoke('app:getVersion'),
    getPaths: () => ipcRenderer.invoke('app:getPaths'),
    getBackendUrl: () => ipcRenderer.invoke('app:getBackendUrl'),
    restart: () => ipcRenderer.invoke('app:restart')
  },
  
  // Update management
  updates: {
    check: () => ipcRenderer.invoke('updates:check')
  },
  
  // Platform detection
  platform: {
    isElectron: true,
    os: process.platform,
    arch: process.arch,
    versions: {
      node: process.versions.node,
      chrome: process.versions.chrome,
      electron: process.versions.electron
    }
  }
});

console.log('Preload script loaded');
console.log('Platform:', process.platform);
console.log('Architecture:', process.arch);
console.log('Electron version:', process.versions.electron);
console.log('Node version:', process.versions.node);
console.log('Chrome version:', process.versions.chrome);
