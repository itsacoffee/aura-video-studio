/**
 * FFmpeg IPC Handlers
 * Handles FFmpeg installation and management with proper elevation
 */

const { ipcMain, shell } = require('electron');
const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const https = require('https');

class FFmpegHandler {
  constructor(app, windowManager) {
    this.app = app;
    this.windowManager = windowManager;
    this.downloadProgress = 0;
    this.isInstalling = false;
  }

  /**
   * Register all FFmpeg IPC handlers
   */
  register() {
    // Check FFmpeg status
    ipcMain.handle('ffmpeg:checkStatus', async () => {
      return this._checkFFmpegStatus();
    });

    // Install FFmpeg (with elevation if needed)
    ipcMain.handle('ffmpeg:install', async (event, options = {}) => {
      if (this.isInstalling) {
        throw new Error('FFmpeg installation already in progress');
      }

      try {
        this.isInstalling = true;
        return await this._installFFmpeg(options);
      } finally {
        this.isInstalling = false;
      }
    });

    // Get FFmpeg installation progress
    ipcMain.handle('ffmpeg:getProgress', () => {
      return {
        progress: this.downloadProgress,
        isInstalling: this.isInstalling
      };
    });

    // Open FFmpeg directory
    ipcMain.handle('ffmpeg:openDirectory', async () => {
      const ffmpegPath = this._getFFmpegPath();
      if (fs.existsSync(ffmpegPath)) {
        await shell.openPath(ffmpegPath);
        return { success: true };
      }
      throw new Error('FFmpeg directory not found');
    });

    console.log('FFmpeg IPC handlers registered');
  }

  /**
   * Check FFmpeg installation status
   */
  _checkFFmpegStatus() {
    const ffmpegPath = this._getFFmpegPath();
    const ffmpegExe = path.join(ffmpegPath, 'ffmpeg.exe');
    const ffprobeExe = path.join(ffmpegPath, 'ffprobe.exe');

    const installed = fs.existsSync(ffmpegExe) && fs.existsSync(ffprobeExe);
    
    let version = null;
    if (installed) {
      try {
        const { execSync } = require('child_process');
        const versionOutput = execSync(`"${ffmpegExe}" -version`, { 
          encoding: 'utf8',
          timeout: 5000 
        });
        const match = versionOutput.match(/ffmpeg version ([\d.]+)/);
        if (match) {
          version = match[1];
        }
      } catch (error) {
        console.error('Failed to get FFmpeg version:', error.message);
      }
    }

    return {
      installed,
      version,
      path: ffmpegPath,
      binaries: {
        ffmpeg: fs.existsSync(ffmpegExe),
        ffprobe: fs.existsSync(ffprobeExe)
      }
    };
  }

  /**
   * Get FFmpeg installation path
   */
  _getFFmpegPath() {
    // Use resources directory in production, or dev resources in development
    if (this.app.isPackaged) {
      return path.join(process.resourcesPath, 'ffmpeg', 'win-x64', 'bin');
    } else {
      const desktopPath = path.join(__dirname, '../..');
      return path.join(desktopPath, 'resources', 'ffmpeg', 'win-x64', 'bin');
    }
  }

  /**
   * Install FFmpeg with proper elevation
   */
  async _installFFmpeg(options = {}) {
    const { useElevation = true } = options;
    
    console.log('Starting FFmpeg installation...');
    
    // For packaged app, FFmpeg should be bundled
    if (this.app.isPackaged) {
      // Check if FFmpeg is already bundled
      const status = this._checkFFmpegStatus();
      if (status.installed) {
        return {
          success: true,
          message: 'FFmpeg is already installed',
          status
        };
      }

      throw new Error('FFmpeg is not bundled with this installation. Please reinstall the application.');
    }

    // For development, download FFmpeg
    const ffmpegPath = this._getFFmpegPath();
    const ffmpegDir = path.dirname(ffmpegPath);
    
    // Create directories
    if (!fs.existsSync(ffmpegDir)) {
      fs.mkdirSync(ffmpegDir, { recursive: true });
    }

    // Use PowerShell script to download FFmpeg
    const scriptPath = path.join(__dirname, '../../scripts/download-ffmpeg-windows.ps1');
    
    if (!fs.existsSync(scriptPath)) {
      throw new Error('FFmpeg download script not found');
    }

    // Check if elevation is needed
    const needsElevation = useElevation && !this._isRunningAsAdmin();
    
    if (needsElevation) {
      // Request elevation via PowerShell
      return await this._installWithElevation(scriptPath);
    } else {
      // Run without elevation
      return await this._installWithoutElevation(scriptPath);
    }
  }

  /**
   * Check if running as administrator
   */
  _isRunningAsAdmin() {
    if (process.platform !== 'win32') {
      return false;
    }

    try {
      const { execSync } = require('child_process');
      // Check for admin rights by trying to write to system32
      execSync('fsutil dirty query %systemdrive% >nul', { 
        stdio: 'ignore',
        timeout: 1000 
      });
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Install FFmpeg with elevation
   */
  async _installWithElevation(scriptPath) {
    return new Promise((resolve, reject) => {
      console.log('Installing FFmpeg with elevation...');

      // Use PowerShell with elevation
      const psCommand = [
        'Start-Process',
        'powershell.exe',
        '-ArgumentList',
        `"-NoProfile -ExecutionPolicy Bypass -File \\"${scriptPath}\\""`,
        '-Verb', 'RunAs',
        '-Wait'
      ].join(' ');

      const proc = spawn('powershell.exe', [
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-Command', psCommand
      ], {
        stdio: ['ignore', 'pipe', 'pipe']
      });

      let stdout = '';
      let stderr = '';

      proc.stdout.on('data', (data) => {
        stdout += data.toString();
        console.log('[FFmpeg Install]', data.toString().trim());
      });

      proc.stderr.on('data', (data) => {
        stderr += data.toString();
        console.error('[FFmpeg Install Error]', data.toString().trim());
      });

      proc.on('close', (code) => {
        if (code === 0) {
          const status = this._checkFFmpegStatus();
          resolve({
            success: true,
            message: 'FFmpeg installed successfully',
            status
          });
        } else {
          reject(new Error(`FFmpeg installation failed with code ${code}\n${stderr}`));
        }
      });

      proc.on('error', (error) => {
        reject(new Error(`Failed to start FFmpeg installation: ${error.message}`));
      });
    });
  }

  /**
   * Install FFmpeg without elevation
   */
  async _installWithoutElevation(scriptPath) {
    return new Promise((resolve, reject) => {
      console.log('Installing FFmpeg without elevation...');

      const proc = spawn('powershell.exe', [
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', scriptPath
      ], {
        stdio: ['ignore', 'pipe', 'pipe']
      });

      let stdout = '';
      let stderr = '';

      proc.stdout.on('data', (data) => {
        stdout += data.toString();
        const message = data.toString().trim();
        console.log('[FFmpeg Install]', message);
        
        // Parse progress if available
        const progressMatch = message.match(/(\d+)%/);
        if (progressMatch) {
          this.downloadProgress = parseInt(progressMatch[1]);
        }
      });

      proc.stderr.on('data', (data) => {
        stderr += data.toString();
        console.error('[FFmpeg Install Error]', data.toString().trim());
      });

      proc.on('close', (code) => {
        if (code === 0) {
          const status = this._checkFFmpegStatus();
          resolve({
            success: true,
            message: 'FFmpeg installed successfully',
            status
          });
        } else {
          reject(new Error(`FFmpeg installation failed with code ${code}\n${stderr}`));
        }
      });

      proc.on('error', (error) => {
        reject(new Error(`Failed to start FFmpeg installation: ${error.message}`));
      });
    });
  }
}

module.exports = FFmpegHandler;
