/**
 * File Handler IPC
 *
 * Handles IPC communication for file operations between the renderer process
 * and the main Electron process. Provides file dialog and file system operations.
 */

const { ipcMain, dialog, BrowserWindow } = require('electron');
const fs = require('fs').promises;

/**
 * Convert various data types to a Buffer for file writing.
 * Handles Buffer, ArrayBuffer, serialized Buffer objects, and strings.
 * @param {Buffer|ArrayBuffer|string|Object} data - The data to convert
 * @returns {Buffer} The converted Buffer
 */
function toBuffer(data) {
  if (Buffer.isBuffer(data)) {
    return data;
  }

  if (data instanceof ArrayBuffer) {
    return Buffer.from(data);
  }

  // Handle serialized buffer from IPC (e.g., { type: 'Buffer', data: [...] })
  if (data && typeof data === 'object' && data.type === 'Buffer' && Array.isArray(data.data)) {
    return Buffer.from(data.data);
  }

  if (typeof data === 'string') {
    return Buffer.from(data, 'utf-8');
  }

  // Fallback: attempt to create buffer from any iterable/array-like
  return Buffer.from(data);
}

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
        const buffer = toBuffer(data);
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

module.exports = { FileHandler, toBuffer };
