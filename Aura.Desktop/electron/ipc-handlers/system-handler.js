/**
 * System IPC Handlers
 * Handles system operations like dialogs, shell, and app info
 */

const { ipcMain, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs');

class SystemHandler {
  constructor(app, windowManager, appConfig) {
    this.app = app;
    this.windowManager = windowManager;
    this.appConfig = appConfig;
    
    // Rate limiting for IPC calls
    this.rateLimits = new Map();
    this.maxCallsPerSecond = 10;
  }

  /**
   * Check rate limit for IPC call
   */
  checkRateLimit(channel) {
    const now = Date.now();
    const limit = this.rateLimits.get(channel);
    
    if (!limit) {
      this.rateLimits.set(channel, { count: 1, resetAt: now + 1000 });
      return true;
    }
    
    if (now > limit.resetAt) {
      this.rateLimits.set(channel, { count: 1, resetAt: now + 1000 });
      return true;
    }
    
    if (limit.count >= this.maxCallsPerSecond) {
      console.warn(`Rate limit exceeded for channel: ${channel}`);
      return false;
    }
    
    limit.count++;
    return true;
  }

  /**
   * Validate file path to prevent path traversal
   */
  validatePath(filePath) {
    if (!filePath || typeof filePath !== 'string') {
      return false;
    }

    // Normalize path
    const normalized = path.normalize(filePath);
    
    // Check for path traversal attempts
    if (normalized.includes('..')) {
      console.warn('Path traversal attempt detected:', filePath);
      return false;
    }

    return true;
  }

  /**
   * Register all system IPC handlers
   */
  register() {
    // File/folder dialogs
    ipcMain.handle('dialog:openFolder', async (event) => {
      if (!this.checkRateLimit('dialog:openFolder')) {
        throw new Error('Rate limit exceeded');
      }

      try {
        const result = await dialog.showOpenDialog(this.windowManager.getMainWindow(), {
          properties: ['openDirectory', 'createDirectory']
        });
        
        if (result.canceled || result.filePaths.length === 0) {
          return null;
        }
        
        return result.filePaths[0];
      } catch (error) {
        console.error('Error opening folder dialog:', error);
        throw error;
      }
    });

    ipcMain.handle('dialog:openFile', async (event, options = {}) => {
      if (!this.checkRateLimit('dialog:openFile')) {
        throw new Error('Rate limit exceeded');
      }

      try {
        const result = await dialog.showOpenDialog(this.windowManager.getMainWindow(), {
          properties: ['openFile'],
          filters: options.filters || [],
          title: options.title || 'Open File'
        });
        
        if (result.canceled || result.filePaths.length === 0) {
          return null;
        }
        
        return result.filePaths[0];
      } catch (error) {
        console.error('Error opening file dialog:', error);
        throw error;
      }
    });

    ipcMain.handle('dialog:openMultipleFiles', async (event, options = {}) => {
      if (!this.checkRateLimit('dialog:openMultipleFiles')) {
        throw new Error('Rate limit exceeded');
      }

      try {
        const result = await dialog.showOpenDialog(this.windowManager.getMainWindow(), {
          properties: ['openFile', 'multiSelections'],
          filters: options.filters || [],
          title: options.title || 'Open Files'
        });
        
        if (result.canceled || result.filePaths.length === 0) {
          return [];
        }
        
        return result.filePaths;
      } catch (error) {
        console.error('Error opening multiple files dialog:', error);
        throw error;
      }
    });

    ipcMain.handle('dialog:saveFile', async (event, options = {}) => {
      if (!this.checkRateLimit('dialog:saveFile')) {
        throw new Error('Rate limit exceeded');
      }

      try {
        const result = await dialog.showSaveDialog(this.windowManager.getMainWindow(), {
          filters: options.filters || [],
          defaultPath: options.defaultPath,
          title: options.title || 'Save File'
        });
        
        if (result.canceled || !result.filePath) {
          return null;
        }
        
        return result.filePath;
      } catch (error) {
        console.error('Error opening save dialog:', error);
        throw error;
      }
    });

    ipcMain.handle('dialog:showMessage', async (event, options = {}) => {
      try {
        const result = await dialog.showMessageBox(this.windowManager.getMainWindow(), {
          type: options.type || 'info',
          title: options.title || 'Message',
          message: options.message || '',
          detail: options.detail,
          buttons: options.buttons || ['OK'],
          defaultId: options.defaultId || 0,
          cancelId: options.cancelId
        });
        
        return result.response;
      } catch (error) {
        console.error('Error showing message dialog:', error);
        throw error;
      }
    });

    ipcMain.handle('dialog:showError', async (event, title, message) => {
      try {
        dialog.showErrorBox(title, message);
        return true;
      } catch (error) {
        console.error('Error showing error dialog:', error);
        throw error;
      }
    });

    // Shell operations (with validation)
    ipcMain.handle('shell:openExternal', async (event, url) => {
      try {
        // Validate URL
        if (!url || typeof url !== 'string') {
          throw new Error('Invalid URL');
        }

        // Only allow http, https, and mailto protocols
        if (!url.match(/^(https?|mailto):/)) {
          throw new Error('Invalid protocol');
        }

        await shell.openExternal(url);
        return { success: true };
      } catch (error) {
        console.error('Error opening external URL:', error);
        throw error;
      }
    });

    ipcMain.handle('shell:openPath', async (event, filePath) => {
      try {
        // Validate path
        if (!this.validatePath(filePath)) {
          throw new Error('Invalid file path');
        }

        // Check if path exists
        if (!fs.existsSync(filePath)) {
          throw new Error('Path does not exist');
        }

        await shell.openPath(filePath);
        return { success: true };
      } catch (error) {
        console.error('Error opening path:', error);
        throw error;
      }
    });

    ipcMain.handle('shell:showItemInFolder', async (event, filePath) => {
      try {
        // Validate path
        if (!this.validatePath(filePath)) {
          throw new Error('Invalid file path');
        }

        // Check if path exists
        if (!fs.existsSync(filePath)) {
          throw new Error('Path does not exist');
        }

        shell.showItemInFolder(filePath);
        return { success: true };
      } catch (error) {
        console.error('Error showing item in folder:', error);
        throw error;
      }
    });

    ipcMain.handle('shell:trashItem', async (event, filePath) => {
      try {
        // Validate path
        if (!this.validatePath(filePath)) {
          throw new Error('Invalid file path');
        }

        // Check if path exists
        if (!fs.existsSync(filePath)) {
          throw new Error('Path does not exist');
        }

        await shell.trashItem(filePath);
        return { success: true };
      } catch (error) {
        console.error('Error moving item to trash:', error);
        throw error;
      }
    });

    // App info
    ipcMain.handle('app:getVersion', () => {
      return this.app.getVersion();
    });

    ipcMain.handle('app:getName', () => {
      return this.app.getName();
    });

    ipcMain.handle('app:getPaths', () => {
      return this.appConfig.getPaths();
    });

    ipcMain.handle('app:getLocale', () => {
      return this.app.getLocale();
    });

    ipcMain.handle('app:isPackaged', () => {
      return this.app.isPackaged;
    });

    // Window operations
    ipcMain.handle('window:minimize', () => {
      const window = this.windowManager.getMainWindow();
      if (window) {
        window.minimize();
        return { success: true };
      }
      throw new Error('No main window');
    });

    ipcMain.handle('window:maximize', () => {
      const window = this.windowManager.getMainWindow();
      if (window) {
        if (window.isMaximized()) {
          window.unmaximize();
        } else {
          window.maximize();
        }
        return { success: true, isMaximized: window.isMaximized() };
      }
      throw new Error('No main window');
    });

    ipcMain.handle('window:close', () => {
      const window = this.windowManager.getMainWindow();
      if (window) {
        window.close();
        return { success: true };
      }
      throw new Error('No main window');
    });

    ipcMain.handle('window:hide', () => {
      this.windowManager.hideMainWindow();
      return { success: true };
    });

    ipcMain.handle('window:show', () => {
      this.windowManager.showMainWindow();
      return { success: true };
    });

    // App restart
    ipcMain.handle('app:restart', () => {
      this.app.relaunch();
      this.app.exit();
    });

    // App quit
    ipcMain.handle('app:quit', () => {
      this.app.quit();
    });

    console.log('System IPC handlers registered');
  }
}

module.exports = SystemHandler;
