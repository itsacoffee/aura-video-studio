/**
 * File Handler IPC
 *
 * Handles IPC communication for file operations between the renderer process
 * and the main Electron process. Provides file dialog and file system operations.
 */

const { ipcMain, dialog, BrowserWindow } = require('electron');
const fs = require('fs').promises;

class FileHandler {
  /**
   * Create a new FileHandler instance.
   * @param {object} logger - Logger instance for debugging
   */
  constructor(logger) {
    this.logger = logger || console;
  }

  /**
   * Register all file operation IPC handlers.
   */
  register() {
    this.logger.info('Registering file operation IPC handlers');

    // Show save dialog
    ipcMain.handle('dialog:showSaveDialog', async (event, options) => {
      try {
        const window = BrowserWindow.fromWebContents(event.sender);
        const result = await dialog.showSaveDialog(window, options);
        return result;
      } catch (error) {
        this.logger.error('Failed to show save dialog', { error: error.message });
        return { canceled: true, filePath: undefined };
      }
    });

    // Show open dialog
    ipcMain.handle('dialog:showOpenDialog', async (event, options) => {
      try {
        const window = BrowserWindow.fromWebContents(event.sender);
        const result = await dialog.showOpenDialog(window, options);
        return result;
      } catch (error) {
        this.logger.error('Failed to show open dialog', { error: error.message });
        return { canceled: true, filePaths: [] };
      }
    });

    // Write file
    ipcMain.handle('fs:writeFile', async (event, filePath, data) => {
      try {
        // Handle Buffer or ArrayBuffer data
        let buffer;
        if (Buffer.isBuffer(data)) {
          buffer = data;
        } else if (data instanceof ArrayBuffer) {
          buffer = Buffer.from(data);
        } else if (data && typeof data === 'object' && data.type === 'Buffer') {
          // Handle serialized buffer from IPC
          buffer = Buffer.from(data.data);
        } else if (typeof data === 'string') {
          buffer = Buffer.from(data, 'utf-8');
        } else {
          buffer = Buffer.from(data);
        }

        await fs.writeFile(filePath, buffer);
        this.logger.info('File written successfully', { path: filePath, size: buffer.length });
        return { success: true };
      } catch (error) {
        this.logger.error('Failed to write file', { path: filePath, error: error.message });
        return { success: false, error: error.message };
      }
    });

    // Read file
    ipcMain.handle('fs:readFile', async (event, filePath) => {
      try {
        const data = await fs.readFile(filePath);
        return { success: true, data };
      } catch (error) {
        this.logger.error('Failed to read file', { path: filePath, error: error.message });
        return { success: false, error: error.message };
      }
    });

    // Check if file exists
    ipcMain.handle('fs:exists', async (event, filePath) => {
      try {
        await fs.access(filePath);
        return { success: true, exists: true };
      } catch {
        return { success: true, exists: false };
      }
    });

    // Create directory
    ipcMain.handle('fs:mkdir', async (event, dirPath, options = {}) => {
      try {
        await fs.mkdir(dirPath, { recursive: true, ...options });
        return { success: true };
      } catch (error) {
        this.logger.error('Failed to create directory', { path: dirPath, error: error.message });
        return { success: false, error: error.message };
      }
    });

    this.logger.info('File operation IPC handlers registered');
  }
}

module.exports = { FileHandler };
