/**
 * Backend Service IPC Handlers
 * Handles backend health checks and status updates
 */

const { ipcMain } = require('electron');
const axios = require('axios');

class BackendHandler {
  constructor(backendUrl, backendService = null, healthEndpoint = '/api/health') {
    this.backendUrl = backendUrl;
    this.backendService = backendService;
    this.healthCheckInterval = null;
    this.lastHealthStatus = 'unknown';
    this.healthEndpoint = healthEndpoint || '/api/health';
    this.livenessEndpoint = '/health/live';
  }

  /**
   * Update backend URL
   */
  setBackendUrl(url) {
    this.backendUrl = url;
  }

  /**
   * Set backend service reference for control operations
   */
  setBackendService(service) {
    this.backendService = service;
  }

  /**
   * Register all backend IPC handlers
   */
  register() {
    // Get backend URL
    ipcMain.handle('backend:getUrl', () => {
      return this.backendUrl;
    });

    // Health check
    ipcMain.handle('backend:health', async () => {
      try {
        const response = await axios.get(this._buildUrl(this.healthEndpoint), {
          timeout: 5000
        });
        
        this.lastHealthStatus = 'healthy';
        return {
          status: 'healthy',
          data: response.data
        };
      } catch (error) {
        this.lastHealthStatus = 'unhealthy';
        return {
          status: 'unhealthy',
          error: error.message
        };
      }
    });

    // Ping/Pong for quick connectivity check
    ipcMain.handle('backend:ping', async () => {
      try {
        const startTime = Date.now();
        await axios.get(this._buildUrl(this.livenessEndpoint), {
          timeout: 2000
        });
        const responseTime = Date.now() - startTime;
        
        return {
          success: true,
          responseTime
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // Get backend info
    ipcMain.handle('backend:info', async () => {
      try {
        const response = await axios.get(`${this.backendUrl}/api/info`, {
          timeout: 5000
        });
        
        return response.data;
      } catch (error) {
        console.error('Error getting backend info:', error);
        throw new Error(`Failed to get backend info: ${error.message}`);
      }
    });

    // Get backend version
    ipcMain.handle('backend:version', async () => {
      try {
        const response = await axios.get(`${this.backendUrl}/api/version`, {
          timeout: 5000
        });
        
        return response.data;
      } catch (error) {
        console.error('Error getting backend version:', error);
        throw new Error(`Failed to get backend version: ${error.message}`);
      }
    });

    // Get provider status
    ipcMain.handle('backend:providerStatus', async () => {
      try {
        const response = await axios.get(`${this.backendUrl}/api/providers/status`, {
          timeout: 10000
        });
        
        return response.data;
      } catch (error) {
        console.error('Error getting provider status:', error);
        throw new Error(`Failed to get provider status: ${error.message}`);
      }
    });

    // Check FFmpeg status
    ipcMain.handle('backend:ffmpegStatus', async () => {
      try {
        const response = await axios.get(`${this.backendUrl}/api/ffmpeg/status`, {
          timeout: 5000
        });
        
        return response.data;
      } catch (error) {
        console.error('Error getting FFmpeg status:', error);
        throw new Error(`Failed to get FFmpeg status: ${error.message}`);
      }
    });

    // Restart backend
    ipcMain.handle('backend:restart', async () => {
      if (!this.backendService) {
        throw new Error('Backend service not available');
      }

      try {
        console.log('Restart requested via IPC');
        await this.backendService.restart();
        this.backendUrl = this.backendService.getUrl();
        return {
          success: true,
          url: this.backendUrl
        };
      } catch (error) {
        console.error('Error restarting backend:', error);
        throw new Error(`Failed to restart backend: ${error.message}`);
      }
    });

    // Stop backend
    ipcMain.handle('backend:stop', async () => {
      if (!this.backendService) {
        throw new Error('Backend service not available');
      }

      try {
        console.log('Stop requested via IPC');
        await this.backendService.stop();
        return { success: true };
      } catch (error) {
        console.error('Error stopping backend:', error);
        throw new Error(`Failed to stop backend: ${error.message}`);
      }
    });

    // Get backend status
    ipcMain.handle('backend:status', () => {
      if (!this.backendService) {
        return { running: false, error: 'Service not available' };
      }

      return {
        running: this.backendService.isRunning(),
        port: this.backendService.getPort(),
        url: this.backendService.getUrl()
      };
    });

    // Check Windows Firewall compatibility
    ipcMain.handle('backend:checkFirewall', async () => {
      if (!this.backendService) {
        throw new Error('Backend service not available');
      }

      try {
        return await this.backendService.checkFirewallCompatibility();
      } catch (error) {
        console.error('Error checking firewall:', error);
        throw new Error(`Failed to check firewall: ${error.message}`);
      }
    });

    // Get Windows Firewall rule status
    ipcMain.handle('backend:getFirewallRule', async () => {
      if (!this.backendService) {
        throw new Error('Backend service not available');
      }

      try {
        return await this.backendService.getFirewallRuleStatus();
      } catch (error) {
        console.error('Error getting firewall rule:', error);
        throw new Error(`Failed to get firewall rule: ${error.message}`);
      }
    });

    // Get firewall rule creation command
    ipcMain.handle('backend:getFirewallCommand', () => {
      if (!this.backendService) {
        throw new Error('Backend service not available');
      }

      return this.backendService.getFirewallRuleCommand();
    });

    console.log('Backend IPC handlers registered');
  }

  /**
   * Start periodic health checks
   */
  startHealthChecks(window, interval = 30000) {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }

    this.healthCheckInterval = setInterval(async () => {
      try {
        const response = await axios.get(this._buildUrl(this.healthEndpoint), {
          timeout: 5000
        });
        
        const newStatus = 'healthy';
        if (this.lastHealthStatus !== newStatus) {
          this.lastHealthStatus = newStatus;
          this.sendHealthUpdate(window, { status: 'healthy', data: response.data });
        }
      } catch (error) {
        const newStatus = 'unhealthy';
        if (this.lastHealthStatus !== newStatus) {
          this.lastHealthStatus = newStatus;
          this.sendHealthUpdate(window, { status: 'unhealthy', error: error.message });
        }
      }
    }, interval);
  }

  /**
   * Stop health checks
   */
  stopHealthChecks() {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
      this.healthCheckInterval = null;
    }
  }

  /**
   * Send health status update to renderer
   */
  sendHealthUpdate(window, status) {
    if (window && !window.isDestroyed()) {
      window.webContents.send('backend:healthUpdate', status);
    }
  }

  /**
   * Send provider status update to renderer
   */
  sendProviderUpdate(window, providerStatus) {
    if (window && !window.isDestroyed()) {
      window.webContents.send('backend:providerUpdate', providerStatus);
    }
  }

  _buildUrl(pathname = '') {
    if (!this.backendUrl) {
      return pathname;
    }

    if (!pathname) {
      return this.backendUrl;
    }

    if (pathname.startsWith('http://') || pathname.startsWith('https://')) {
      return pathname;
    }

    const normalized = pathname.startsWith('/') ? pathname : `/${pathname}`;
    return `${this.backendUrl}${normalized}`;
  }
}

module.exports = BackendHandler;
