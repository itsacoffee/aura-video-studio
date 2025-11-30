/**
 * OpenCut Server IPC Handlers
 * Handles OpenCut server status, startup, and health checks
 */

const { ipcMain } = require('electron');
const http = require('http');

class OpenCutHandler {
  constructor(openCutManager) {
    this.openCutManager = openCutManager;
    this.statusCache = {
      isRunning: false,
      isStarting: false,
      url: null,
      port: null,
      lastCheck: null,
    };
  }

  /**
   * Update OpenCutManager reference
   */
  setOpenCutManager(manager) {
    this.openCutManager = manager;
  }

  /**
   * Check if OpenCut server is running by attempting a connection
   */
  async _checkServerHealth(port) {
    return new Promise((resolve) => {
      const req = http.get(`http://127.0.0.1:${port}/api/health`, (res) => {
        // Server is responding
        resolve({
          healthy: res.statusCode >= 200 && res.statusCode < 400,
          statusCode: res.statusCode,
        });
      });
      req.on('error', () => {
        // Server is not responding
        resolve({ healthy: false, statusCode: null });
      });
      req.setTimeout(2000, () => {
        req.destroy();
        resolve({ healthy: false, statusCode: null });
      });
    });
  }

  /**
   * Get current status of OpenCut server
   */
  async _getStatus() {
    if (!this.openCutManager) {
      return {
        isRunning: false,
        isStarting: false,
        isAvailable: false,
        url: null,
        port: null,
        error: 'OpenCutManager not initialized',
      };
    }

    const port = this.openCutManager.port || 3100;
    const url = this.openCutManager.getUrl();
    const isStarting = this.openCutManager.isStarting || false;
    const hasChild = this.openCutManager.child !== null;

    // If manager says it's starting or has a child process, check health
    if (isStarting || hasChild) {
      const health = await this._checkServerHealth(port);
      return {
        isRunning: health.healthy,
        isStarting: isStarting && !health.healthy,
        isAvailable: this.openCutManager.isAvailable(),
        url: health.healthy ? url : null,
        port,
        statusCode: health.statusCode,
      };
    }

    // Check if server is running even if manager doesn't think so
    const health = await this._checkServerHealth(port);
    return {
      isRunning: health.healthy,
      isStarting: false,
      isAvailable: this.openCutManager.isAvailable(),
      url: health.healthy ? url : null,
      port,
      statusCode: health.statusCode,
    };
  }

  /**
   * Wait for server to be ready with timeout
   */
  async _waitForServerReady(maxWaitMs = 30000, checkIntervalMs = 500) {
    const startTime = Date.now();
    
    while (Date.now() - startTime < maxWaitMs) {
      const status = await this._getStatus();
      
      if (status.isRunning) {
        return { success: true, status };
      }
      
      // If not starting and not running, server likely failed
      if (!status.isStarting && !status.isRunning) {
        // Give it one more chance to check if server just started
        await new Promise(resolve => setTimeout(resolve, checkIntervalMs));
        const finalStatus = await this._getStatus();
        if (finalStatus.isRunning) {
          return { success: true, status: finalStatus };
        }
        break;
      }
      
      await new Promise(resolve => setTimeout(resolve, checkIntervalMs));
    }
    
    const finalStatus = await this._getStatus();
    return { success: false, status: finalStatus };
  }

  /**
   * Register all OpenCut IPC handlers
   */
  register() {
    // Get OpenCut server status
    ipcMain.handle('opencut:status', async () => {
      try {
        const status = await this._getStatus();
        this.statusCache = {
          ...status,
          lastCheck: Date.now(),
        };
        return this.statusCache;
      } catch (error) {
        console.error('Error getting OpenCut status:', error);
        return {
          isRunning: false,
          isStarting: false,
          isAvailable: false,
          url: null,
          port: null,
          error: error.message,
        };
      }
    });

    // Get OpenCut server URL
    ipcMain.handle('opencut:getUrl', () => {
      if (!this.openCutManager) {
        return null;
      }
      return this.openCutManager.getUrl();
    });

    // Check if OpenCut is available
    ipcMain.handle('opencut:isAvailable', () => {
      if (!this.openCutManager) {
        return false;
      }
      return this.openCutManager.isAvailable();
    });

    // Start OpenCut server
    ipcMain.handle('opencut:start', async () => {
      try {
        if (!this.openCutManager) {
          return {
            success: false,
            error: 'OpenCutManager not initialized',
          };
        }

        // Check if already running
        const status = await this._getStatus();
        if (status.isRunning) {
          return {
            success: true,
            alreadyRunning: true,
            url: status.url,
          };
        }

        // Start the server
        await this.openCutManager.start();

        // Wait for server to be ready (with timeout)
        const result = await this._waitForServerReady(30000, 500);
        
        if (result.success) {
          return {
            success: true,
            url: result.status.url,
            port: result.status.port,
          };
        } else {
          return {
            success: false,
            error: 'Server started but did not become ready within timeout',
            status: result.status,
          };
        }
      } catch (error) {
        console.error('Error starting OpenCut server:', error);
        return {
          success: false,
          error: error.message,
        };
      }
    });

    // Stop OpenCut server
    ipcMain.handle('opencut:stop', () => {
      try {
        if (!this.openCutManager) {
          return {
            success: false,
            error: 'OpenCutManager not initialized',
          };
        }

        this.openCutManager.stop();
        return {
          success: true,
        };
      } catch (error) {
        console.error('Error stopping OpenCut server:', error);
        return {
          success: false,
          error: error.message,
        };
      }
    });

    // Wait for OpenCut server to be ready
    ipcMain.handle('opencut:waitForReady', async (event, maxWaitMs = 30000) => {
      try {
        const result = await this._waitForServerReady(maxWaitMs, 500);
        return result;
      } catch (error) {
        console.error('Error waiting for OpenCut server:', error);
        return {
          success: false,
          status: await this._getStatus(),
          error: error.message,
        };
      }
    });

    // Health check
    ipcMain.handle('opencut:health', async () => {
      try {
        if (!this.openCutManager) {
          return {
            healthy: false,
            error: 'OpenCutManager not initialized',
          };
        }

        const port = this.openCutManager.port || 3100;
        const health = await this._checkServerHealth(port);
        return health;
      } catch (error) {
        return {
          healthy: false,
          error: error.message,
        };
      }
    });
  }

  /**
   * Unregister all handlers (cleanup)
   */
  unregister() {
    ipcMain.removeHandler('opencut:status');
    ipcMain.removeHandler('opencut:getUrl');
    ipcMain.removeHandler('opencut:isAvailable');
    ipcMain.removeHandler('opencut:start');
    ipcMain.removeHandler('opencut:stop');
    ipcMain.removeHandler('opencut:waitForReady');
    ipcMain.removeHandler('opencut:health');
  }
}

module.exports = OpenCutHandler;

