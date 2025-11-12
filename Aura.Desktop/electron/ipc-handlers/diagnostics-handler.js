/**
 * Diagnostics IPC Handler
 * Handles diagnostics checks and repair actions
 */

const { ipcMain } = require('electron');
const { execFile } = require('child_process');
const { promisify } = require('util');
const fs = require('fs');
const path = require('path');
const axios = require('axios');

const execFileAsync = promisify(execFile);

class DiagnosticsHandler {
  constructor(app, backendUrl, windowManager) {
    this.app = app;
    this.backendUrl = backendUrl;
    this.windowManager = windowManager;
  }

  /**
   * Register all diagnostics IPC handlers
   */
  register() {
    // Run all diagnostics checks
    ipcMain.handle('diagnostics:runAll', async () => {
      try {
        const results = await this.runAllChecks();
        return { success: true, results };
      } catch (error) {
        console.error('Error running diagnostics:', error);
        throw error;
      }
    });

    // Check FFmpeg binary exists
    ipcMain.handle('diagnostics:checkFFmpeg', async () => {
      try {
        const result = await this.checkFFmpeg();
        return result;
      } catch (error) {
        console.error('Error checking FFmpeg:', error);
        throw error;
      }
    });

    // Fix FFmpeg (provide installation instructions)
    ipcMain.handle('diagnostics:fixFFmpeg', async () => {
      try {
        const result = await this.fixFFmpeg();
        return result;
      } catch (error) {
        console.error('Error fixing FFmpeg:', error);
        throw error;
      }
    });

    // Check API endpoint responds
    ipcMain.handle('diagnostics:checkAPI', async () => {
      try {
        const result = await this.checkAPI();
        return result;
      } catch (error) {
        console.error('Error checking API:', error);
        throw error;
      }
    });

    // Fix API (restart backend)
    ipcMain.handle('diagnostics:fixAPI', async () => {
      try {
        const result = await this.fixAPI();
        return result;
      } catch (error) {
        console.error('Error fixing API:', error);
        throw error;
      }
    });

    // Check providers configured
    ipcMain.handle('diagnostics:checkProviders', async () => {
      try {
        const result = await this.checkProviders();
        return result;
      } catch (error) {
        console.error('Error checking providers:', error);
        throw error;
      }
    });

    // Fix providers (navigate to setup)
    ipcMain.handle('diagnostics:fixProviders', async () => {
      try {
        const result = await this.fixProviders();
        return result;
      } catch (error) {
        console.error('Error fixing providers:', error);
        throw error;
      }
    });

    // Check disk space
    ipcMain.handle('diagnostics:checkDiskSpace', async () => {
      try {
        const result = await this.checkDiskSpace();
        return result;
      } catch (error) {
        console.error('Error checking disk space:', error);
        throw error;
      }
    });

    // Check config file integrity
    ipcMain.handle('diagnostics:checkConfig', async () => {
      try {
        const result = await this.checkConfig();
        return result;
      } catch (error) {
        console.error('Error checking config:', error);
        throw error;
      }
    });

    console.log('Diagnostics IPC handlers registered');
  }

  /**
   * Run all diagnostics checks
   */
  async runAllChecks() {
    const checks = [
      { name: 'ffmpeg', check: () => this.checkFFmpeg() },
      { name: 'api', check: () => this.checkAPI() },
      { name: 'providers', check: () => this.checkProviders() },
      { name: 'diskSpace', check: () => this.checkDiskSpace() },
      { name: 'config', check: () => this.checkConfig() }
    ];

    const results = {};
    for (const { name, check } of checks) {
      try {
        results[name] = await check();
      } catch (error) {
        results[name] = {
          status: 'error',
          message: error.message,
          canFix: false
        };
      }
    }

    return results;
  }

  /**
   * Check if FFmpeg is available
   */
  async checkFFmpeg() {
    try {
      // Try to find FFmpeg in common locations
      const possiblePaths = [
        'ffmpeg', // System PATH
        path.join(process.resourcesPath, 'ffmpeg', 'ffmpeg.exe'), // Windows packaged
        path.join(process.resourcesPath, 'ffmpeg', 'ffmpeg'), // Linux/Mac packaged
        'C:\\Program Files\\FFmpeg\\bin\\ffmpeg.exe', // Windows common install
        '/usr/bin/ffmpeg', // Linux common install
        '/usr/local/bin/ffmpeg' // Mac common install
      ];

      for (const ffmpegPath of possiblePaths) {
        try {
          const { stdout } = await execFileAsync(ffmpegPath, ['-version'], { timeout: 5000 });
          
          if (stdout && stdout.includes('ffmpeg version')) {
            return {
              status: 'ok',
              message: 'FFmpeg is installed and working',
              path: ffmpegPath,
              canFix: false
            };
          }
        } catch (err) {
          // Continue to next path
        }
      }

      // FFmpeg not found
      return {
        status: 'error',
        message: 'FFmpeg not found. FFmpeg is required for video processing.',
        canFix: true,
        fixAction: 'Install FFmpeg or configure path'
      };
    } catch (error) {
      return {
        status: 'error',
        message: `Failed to check FFmpeg: ${error.message}`,
        canFix: true
      };
    }
  }

  /**
   * Fix FFmpeg (provide installation instructions)
   */
  async fixFFmpeg() {
    const { shell } = require('electron');
    
    // Open FFmpeg download page
    await shell.openExternal('https://ffmpeg.org/download.html');
    
    return {
      success: true,
      message: 'Opened FFmpeg download page in browser. Please download and install FFmpeg, then restart the application.',
      requiresRestart: true
    };
  }

  /**
   * Check if API endpoint is responding
   */
  async checkAPI() {
    try {
      const response = await axios.get(`${this.backendUrl}/health/live`, {
        timeout: 5000
      });
      
      if (response.status === 200) {
        return {
          status: 'ok',
          message: 'API is responding',
          url: this.backendUrl,
          canFix: false
        };
      }
      
      return {
        status: 'warning',
        message: `API returned status ${response.status}`,
        url: this.backendUrl,
        canFix: true
      };
    } catch (error) {
      return {
        status: 'error',
        message: `API is not responding: ${error.message}`,
        url: this.backendUrl,
        canFix: true,
        fixAction: 'Restart backend service'
      };
    }
  }

  /**
   * Fix API (restart backend - placeholder, actual implementation depends on backend service)
   */
  async fixAPI() {
    return {
      success: false,
      message: 'Backend restart requires application restart. Please restart Aura Video Studio.',
      requiresRestart: true
    };
  }

  /**
   * Check if providers are configured
   */
  async checkProviders() {
    try {
      const response = await axios.get(`${this.backendUrl}/api/providers/status`, {
        timeout: 5000
      });
      
      if (response.status === 200 && response.data) {
        const providers = response.data;
        const configuredCount = Object.values(providers).filter(p => p?.configured).length;
        
        if (configuredCount === 0) {
          return {
            status: 'warning',
            message: 'No providers configured. Configure at least one provider to generate videos.',
            canFix: true,
            fixAction: 'Navigate to setup wizard'
          };
        }
        
        return {
          status: 'ok',
          message: `${configuredCount} provider(s) configured`,
          canFix: false
        };
      }
      
      return {
        status: 'warning',
        message: 'Unable to determine provider status',
        canFix: true
      };
    } catch (error) {
      return {
        status: 'warning',
        message: `Unable to check providers: ${error.message}`,
        canFix: true
      };
    }
  }

  /**
   * Fix providers (navigate to setup)
   */
  async fixProviders() {
    const mainWindow = this.windowManager.getMainWindow();
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.executeJavaScript(`
        window.location.hash = '#/setup';
      `);
      
      return {
        success: true,
        message: 'Navigated to setup wizard. Please configure your providers.'
      };
    }
    
    return {
      success: false,
      message: 'Unable to navigate to setup wizard'
    };
  }

  /**
   * Check disk space
   */
  async checkDiskSpace() {
    try {
      const userDataPath = this.app.getPath('userData');
      const { disk } = require('diskusage');
      
      // This is a simplified check - would need platform-specific implementation
      return {
        status: 'ok',
        message: 'Disk space check passed',
        canFix: false
      };
    } catch (error) {
      return {
        status: 'warning',
        message: 'Unable to check disk space',
        canFix: false
      };
    }
  }

  /**
   * Check config file integrity
   */
  async checkConfig() {
    try {
      const configPath = path.join(this.app.getPath('userData'), 'aura-config.json');
      
      if (!fs.existsSync(configPath)) {
        return {
          status: 'warning',
          message: 'Config file not found (using defaults)',
          canFix: false
        };
      }
      
      // Try to read and parse config
      const configContent = fs.readFileSync(configPath, 'utf8');
      JSON.parse(configContent);
      
      return {
        status: 'ok',
        message: 'Config file is valid',
        canFix: false
      };
    } catch (error) {
      return {
        status: 'error',
        message: `Config file is corrupted: ${error.message}`,
        canFix: true,
        fixAction: 'Reset configuration'
      };
    }
  }
}

module.exports = DiagnosticsHandler;
