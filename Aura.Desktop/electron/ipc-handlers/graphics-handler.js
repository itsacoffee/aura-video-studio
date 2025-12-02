/**
 * Graphics IPC Handler
 * Handles graphics-related IPC messages from renderer
 */

const { ipcMain, screen } = require('electron');

class GraphicsHandler {
  constructor(windowManager, logger) {
    this.windowManager = windowManager;
    this.logger = logger || console;
    this._registerHandlers();
  }

  _registerHandlers() {
    // Get current window material
    ipcMain.handle('graphics:getMaterial', async () => {
      return {
        current: this.windowManager.getWindowMaterial(),
        supported: this.windowManager.isMicaSupported(),
      };
    });

    // Set window material
    ipcMain.handle('graphics:setMaterial', async (event, effect) => {
      const success = this.windowManager.setWindowMaterial(effect);
      this.logger.info('[Graphics] Set material', { effect, success });
      return success;
    });

    // Check if Mica is supported
    ipcMain.handle('graphics:isMicaSupported', async () => {
      return this.windowManager.isMicaSupported();
    });

    // Get system accent color
    ipcMain.handle('graphics:getAccentColor', async () => {
      return this.windowManager.micaManager?.getAccentColor() || null;
    });

    // Get DPI scaling info
    ipcMain.handle('graphics:getDpiInfo', async () => {
      const primaryDisplay = screen.getPrimaryDisplay();
      return {
        scaleFactor: primaryDisplay.scaleFactor,
        size: primaryDisplay.size,
        workArea: primaryDisplay.workArea,
        bounds: primaryDisplay.bounds,
      };
    });

    // Get all displays info (for multi-monitor DPI)
    ipcMain.handle('graphics:getAllDisplays', async () => {
      return screen.getAllDisplays().map((display) => ({
        id: display.id,
        scaleFactor: display.scaleFactor,
        size: display.size,
        workArea: display.workArea,
        bounds: display.bounds,
        isPrimary: display.id === screen.getPrimaryDisplay().id,
      }));
    });

    // Apply graphics settings from frontend
    ipcMain.handle('graphics:applySettings', async (event, settings) => {
      try {
        // Apply window material based on settings
        if (settings.transparency && this.windowManager.isMicaSupported()) {
          this.windowManager.setWindowMaterial(settings.blurEffects ? 'acrylic' : 'mica');
        } else {
          this.windowManager.setWindowMaterial('none');
        }

        this.logger.info('[Graphics] Settings applied', settings);
        return { success: true };
      } catch (error) {
        this.logger.error('[Graphics] Failed to apply settings', error);
        return { success: false, error: error.message };
      }
    });
  }

  /**
   * Cleanup handlers
   */
  dispose() {
    ipcMain.removeHandler('graphics:getMaterial');
    ipcMain.removeHandler('graphics:setMaterial');
    ipcMain.removeHandler('graphics:isMicaSupported');
    ipcMain.removeHandler('graphics:getAccentColor');
    ipcMain.removeHandler('graphics:getDpiInfo');
    ipcMain.removeHandler('graphics:getAllDisplays');
    ipcMain.removeHandler('graphics:applySettings');
  }
}

module.exports = { GraphicsHandler };
