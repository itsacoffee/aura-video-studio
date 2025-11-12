/**
 * Configuration IPC Handlers
 * Handles application configuration and settings
 */

const { ipcMain } = require('electron');

class ConfigHandler {
  constructor(appConfig) {
    this.appConfig = appConfig;
  }

  /**
   * Register all configuration IPC handlers
   */
  register() {
    // Get configuration value
    ipcMain.handle('config:get', (event, key, defaultValue) => {
      try {
        return this.appConfig.get(key, defaultValue);
      } catch (error) {
        console.error('Error getting config:', error);
        throw error;
      }
    });

    // Set configuration value
    ipcMain.handle('config:set', (event, key, value) => {
      try {
        // Validate input
        if (typeof key !== 'string' || key.length === 0) {
          throw new Error('Invalid configuration key');
        }
        
        this.appConfig.set(key, value);
        return { success: true };
      } catch (error) {
        console.error('Error setting config:', error);
        throw error;
      }
    });

    // Get all configuration
    ipcMain.handle('config:getAll', () => {
      try {
        return this.appConfig.getAll();
      } catch (error) {
        console.error('Error getting all config:', error);
        throw error;
      }
    });

    // Reset configuration to defaults
    ipcMain.handle('config:reset', () => {
      try {
        this.appConfig.reset();
        return { success: true };
      } catch (error) {
        console.error('Error resetting config:', error);
        throw error;
      }
    });

    // Secure storage handlers
    ipcMain.handle('config:getSecure', (event, key) => {
      try {
        return this.appConfig.getSecure(key);
      } catch (error) {
        console.error('Error getting secure config:', error);
        throw error;
      }
    });

    ipcMain.handle('config:setSecure', (event, key, value) => {
      try {
        if (typeof key !== 'string' || key.length === 0) {
          throw new Error('Invalid secure configuration key');
        }
        
        this.appConfig.setSecure(key, value);
        return { success: true };
      } catch (error) {
        console.error('Error setting secure config:', error);
        throw error;
      }
    });

    ipcMain.handle('config:deleteSecure', (event, key) => {
      try {
        this.appConfig.deleteSecure(key);
        return { success: true };
      } catch (error) {
        console.error('Error deleting secure config:', error);
        throw error;
      }
    });

    // Recent projects handlers
    ipcMain.handle('config:addRecentProject', (event, projectPath, projectName) => {
      try {
        return this.appConfig.addRecentProject(projectPath, projectName);
      } catch (error) {
        console.error('Error adding recent project:', error);
        throw error;
      }
    });

    ipcMain.handle('config:getRecentProjects', () => {
      try {
        return this.appConfig.getRecentProjects();
      } catch (error) {
        console.error('Error getting recent projects:', error);
        throw error;
      }
    });

    ipcMain.handle('config:clearRecentProjects', () => {
      try {
        this.appConfig.clearRecentProjects();
        return { success: true };
      } catch (error) {
        console.error('Error clearing recent projects:', error);
        throw error;
      }
    });

    ipcMain.handle('config:removeRecentProject', (event, projectPath) => {
      try {
        return this.appConfig.removeRecentProject(projectPath);
      } catch (error) {
        console.error('Error removing recent project:', error);
        throw error;
      }
    });

    // Safe mode handlers
    ipcMain.handle('config:isSafeMode', () => {
      try {
        return this.appConfig.isSafeMode();
      } catch (error) {
        console.error('Error checking safe mode:', error);
        throw error;
      }
    });

    ipcMain.handle('config:getCrashCount', () => {
      try {
        return this.appConfig.getCrashCount();
      } catch (error) {
        console.error('Error getting crash count:', error);
        throw error;
      }
    });

    ipcMain.handle('config:resetCrashCount', () => {
      try {
        this.appConfig.resetCrashCount();
        return { success: true };
      } catch (error) {
        console.error('Error resetting crash count:', error);
        throw error;
      }
    });

    ipcMain.handle('config:deleteAndRestart', async () => {
      try {
        const { app } = require('electron');
        
        // Delete config file
        const deleted = this.appConfig.deleteConfigFile();
        
        if (!deleted) {
          throw new Error('Failed to delete config file');
        }
        
        // Schedule restart
        setTimeout(() => {
          app.relaunch();
          app.exit(0);
        }, 500);
        
        return { success: true, deleted };
      } catch (error) {
        console.error('Error deleting config and restarting:', error);
        throw error;
      }
    });

    ipcMain.handle('config:getConfigPath', () => {
      try {
        return this.appConfig.getConfigPath();
      } catch (error) {
        console.error('Error getting config path:', error);
        throw error;
      }
    });

    console.log('Configuration IPC handlers registered');
  }
}

module.exports = ConfigHandler;
