/**
 * Context Menu IPC Handler
 *
 * Handles IPC communication for context menus between the renderer process
 * and the main Electron process. Registers handlers and manages menu callbacks.
 */

const { ipcMain, shell } = require('electron');
const { ContextMenuBuilder } = require('../context-menu-builder');

/**
 * Action types for each context menu type.
 * Maps menu types to their available callback action names.
 */
const ACTION_MAP = {
  'timeline-clip': [
    'onCut',
    'onCopy',
    'onPaste',
    'onDuplicate',
    'onSplit',
    'onDelete',
    'onRippleDelete',
    'onProperties'
  ],
  'timeline-track': [
    'onAddTrack',
    'onToggleLock',
    'onToggleMute',
    'onToggleSolo',
    'onRename',
    'onDelete'
  ],
  'timeline-empty': ['onPaste', 'onAddMarker', 'onSelectAll'],
  'media-asset': [
    'onAddToTimeline',
    'onPreview',
    'onRename',
    'onToggleFavorite',
    'onProperties',
    'onDelete'
  ],
  'ai-script': [
    'onRegenerate',
    'onExpand',
    'onShorten',
    'onGenerateBRoll',
    'onCopyText'
  ],
  'job-queue': ['onPause', 'onResume', 'onCancel', 'onViewLogs', 'onRetry'],
  'preview-window': [
    'onTogglePlayback',
    'onAddMarker',
    'onExportFrame',
    'onSetZoom'
  ],
  'ai-provider': ['onTestConnection', 'onViewStats', 'onSetDefault', 'onConfigure']
};

class ContextMenuHandler {
  /**
   * Create a new ContextMenuHandler instance.
   * @param {object} logger - Logger instance for debugging
   * @param {object} windowManager - WindowManager instance for window operations
   */
  constructor(logger, windowManager) {
    this.logger = logger || console;
    this.windowManager = windowManager;
    this.menuBuilder = new ContextMenuBuilder(logger);
  }

  /**
   * Register all context menu IPC handlers.
   */
  register() {
    this.logger.info('Registering context menu IPC handlers');

    // Main handler for showing context menus
    ipcMain.handle('context-menu:show', async (event, type, data) => {
      return this.handleShowContextMenu(event, type, data);
    });

    // Register utility action handlers
    this.registerActionHandlers();

    this.logger.info('Context menu IPC handlers registered');
  }

  /**
   * Handle the show context menu request from renderer.
   * @param {Electron.IpcMainInvokeEvent} event - IPC event
   * @param {string} type - Context menu type
   * @param {object} data - Menu data
   * @returns {Promise<{success: boolean, error?: string}>}
   */
  async handleShowContextMenu(event, type, data) {
    try {
      this.logger.debug('Showing context menu', { type, data });

      // Get the window that sent the request
      const window = this.getWindowByWebContents(event.sender);
      if (!window) {
        throw new Error('Could not find window for context menu');
      }

      // Create callbacks that send IPC messages back to the renderer
      const callbacks = this.createCallbacks(type, data, event.sender);

      // Build and show the menu
      const menu = this.menuBuilder.build(type, data, callbacks);
      menu.popup({ window });

      return { success: true };
    } catch (error) {
      this.logger.error('Error showing context menu', {
        type,
        error: error.message,
        stack: error.stack
      });
      return { success: false, error: error.message };
    }
  }

  /**
   * Get window by web contents.
   * @param {Electron.WebContents} webContents - The web contents
   * @returns {Electron.BrowserWindow|null}
   */
  getWindowByWebContents(webContents) {
    // First try using windowManager if available
    if (
      this.windowManager &&
      typeof this.windowManager.getWindowByWebContents === 'function'
    ) {
      return this.windowManager.getWindowByWebContents(webContents);
    }

    // Fallback: Try to get the main window
    if (
      this.windowManager &&
      typeof this.windowManager.getMainWindow === 'function'
    ) {
      const mainWindow = this.windowManager.getMainWindow();
      if (mainWindow && mainWindow.webContents === webContents) {
        return mainWindow;
      }
    }

    // Last resort: Use Electron's BrowserWindow.fromWebContents
    const { BrowserWindow } = require('electron');
    return BrowserWindow.fromWebContents(webContents);
  }

  /**
   * Create callback functions for menu actions.
   * Callbacks send IPC messages back to the renderer process.
   * @param {string} type - Context menu type
   * @param {object} data - Original menu data
   * @param {Electron.WebContents} sender - The web contents that requested the menu
   * @returns {object} Callback object with action handlers
   */
  createCallbacks(type, data, sender) {
    const callbacks = {};
    const actionTypes = this.getActionTypes(type);

    // Create a callback for each action type
    actionTypes.forEach((actionType) => {
      callbacks[actionType] = (...args) => {
        const channel = `context-menu:action:${type}:${actionType}`;
        this.logger.debug('Context menu action triggered', {
          channel,
          type,
          actionType,
          data
        });

        // Merge original data with any additional action data
        const actionData = args.length > 0 ? args : [];
        sender.send(channel, { ...data, actionArgs: actionData });
      };
    });

    // Add special handlers for OS integration actions
    callbacks.onRevealInOS = (actionData) => {
      if (actionData && actionData.filePath) {
        shell.showItemInFolder(actionData.filePath);
      }
    };

    callbacks.onOpenOutput = (actionData) => {
      if (actionData && actionData.outputPath) {
        shell.openPath(actionData.outputPath);
      }
    };

    callbacks.onRevealOutput = (actionData) => {
      if (actionData && actionData.outputPath) {
        shell.showItemInFolder(actionData.outputPath);
      }
    };

    return callbacks;
  }

  /**
   * Get the list of action types for a menu type.
   * @param {string} type - Context menu type
   * @returns {string[]} Array of action type names
   */
  getActionTypes(type) {
    return ACTION_MAP[type] || [];
  }

  /**
   * Register utility IPC handlers for common actions.
   */
  registerActionHandlers() {
    // Reveal file/folder in OS file explorer
    ipcMain.handle('context-menu:reveal-in-os', async (event, filePath) => {
      try {
        shell.showItemInFolder(filePath);
        return { success: true };
      } catch (error) {
        this.logger.error('Error revealing in OS', { filePath, error: error.message });
        return { success: false, error: error.message };
      }
    });

    // Open a file or path
    ipcMain.handle('context-menu:open-path', async (event, filePath) => {
      try {
        const result = await shell.openPath(filePath);
        // shell.openPath returns empty string on success, error string on failure
        if (result === '') {
          return { success: true };
        }
        return { success: false, error: result };
      } catch (error) {
        this.logger.error('Error opening path', { filePath, error: error.message });
        return { success: false, error: error.message };
      }
    });
  }
}

module.exports = { ContextMenuHandler };
