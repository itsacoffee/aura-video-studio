/**
 * Backend Service Module
 * Manages the .NET backend process lifecycle
 */

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const net = require('net');
const axios = require('axios');

class BackendService {
  constructor(app, isDev) {
    this.app = app;
    this.isDev = isDev;
    this.process = null;
    this.port = null;
    this.isQuitting = false;
    this.restartAttempts = 0;
    this.maxRestartAttempts = 3;
    this.healthCheckInterval = null;
    
    // Constants
    this.BACKEND_STARTUP_TIMEOUT = 60000; // 60 seconds
    this.HEALTH_CHECK_INTERVAL = 1000; // 1 second
    this.AUTO_RESTART_DELAY = 5000; // 5 seconds
  }

  /**
   * Start the backend service
   */
  async start() {
    try {
      // Find available port
      this.port = await this._findAvailablePort();
      console.log(`Starting backend on port ${this.port}...`);

      // Determine backend executable path
      const backendPath = this._getBackendPath();
      
      // Check if backend executable exists
      if (!fs.existsSync(backendPath)) {
        throw new Error(`Backend executable not found at: ${backendPath}`);
      }

      // Make executable on Unix-like systems
      if (process.platform !== 'win32') {
        try {
          fs.chmodSync(backendPath, 0o755);
        } catch (error) {
          console.warn('Failed to make backend executable:', error.message);
        }
      }

      // Get FFmpeg path
      const ffmpegPath = this._getFFmpegPath();
      const ffmpegExists = this._verifyFFmpeg(ffmpegPath);
      
      if (!ffmpegExists) {
        console.warn('FFmpeg not found - video rendering may not work');
      }

      // Prepare environment variables
      const env = this._prepareEnvironment();

      // Create necessary directories
      this._createDirectories(env);

      console.log('Backend executable:', backendPath);
      console.log('Backend port:', this.port);
      console.log('Environment:', env.DOTNET_ENVIRONMENT);
      console.log('FFmpeg path:', ffmpegPath);

      // Spawn backend process
      this.process = spawn(backendPath, [], {
        env,
        stdio: ['ignore', 'pipe', 'pipe']
      });

      // Setup process handlers
      this._setupProcessHandlers();

      // Wait for backend to be ready
      await this._waitForBackend();

      console.log('Backend started successfully');
      
      // Start periodic health checks
      this._startHealthChecks();
      
      return this.port;
    } catch (error) {
      console.error('Backend startup error:', error);
      throw error;
    }
  }

  /**
   * Stop the backend service
   */
  stop() {
    console.log('Stopping backend service...');
    this.isQuitting = true;

    // Stop health checks
    this._stopHealthChecks();

    // Kill backend process
    if (this.process && !this.process.killed) {
      console.log('Terminating backend process...');
      this.process.kill('SIGTERM');
      
      // Force kill after 5 seconds if not stopped
      setTimeout(() => {
        if (this.process && !this.process.killed) {
          console.log('Force killing backend process...');
          this.process.kill('SIGKILL');
        }
      }, 5000);
    }

    this.process = null;
    this.port = null;
  }

  /**
   * Restart the backend service
   */
  async restart() {
    console.log('Restarting backend service...');
    this.stop();
    
    // Wait a bit before restarting
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    return this.start();
  }

  /**
   * Get backend port
   */
  getPort() {
    return this.port;
  }

  /**
   * Get backend URL
   */
  getUrl() {
    return this.port ? `http://localhost:${this.port}` : null;
  }

  /**
   * Check if backend is running
   */
  isRunning() {
    return this.process !== null && !this.process.killed;
  }

  /**
   * Find an available port for the backend server
   */
  async _findAvailablePort() {
    return new Promise((resolve, reject) => {
      const server = net.createServer();
      server.unref();
      server.on('error', reject);
      server.listen(0, () => {
        const { port } = server.address();
        server.close(() => resolve(port));
      });
    });
  }

  /**
   * Wait for the backend to become healthy
   */
  async _waitForBackend() {
    const maxAttempts = this.BACKEND_STARTUP_TIMEOUT / this.HEALTH_CHECK_INTERVAL;
    
    for (let i = 0; i < maxAttempts; i++) {
      try {
        const response = await axios.get(`http://localhost:${this.port}/health`, {
          timeout: 2000,
          validateStatus: () => true
        });
        
        if (response.status === 200) {
          console.log(`Backend is healthy at http://localhost:${this.port}`);
          return true;
        }
      } catch (error) {
        // Backend not ready yet, continue waiting
      }
      
      // Wait before next attempt
      await new Promise(resolve => setTimeout(resolve, this.HEALTH_CHECK_INTERVAL));
      
      if (i % 10 === 0 && i > 0) {
        console.log(`Still waiting for backend... (attempt ${i}/${maxAttempts})`);
      }
    }
    
    throw new Error(`Backend failed to start within ${this.BACKEND_STARTUP_TIMEOUT}ms`);
  }

  /**
   * Get the path to the backend executable
   */
  _getBackendPath() {
    if (this.isDev) {
      // In development, use the compiled backend from Aura.Api/bin
      const platform = process.platform;
      if (platform === 'win32') {
        return path.join(__dirname, '../../Aura.Api/bin/Debug/net8.0/Aura.Api.exe');
      } else {
        return path.join(__dirname, '../../Aura.Api/bin/Debug/net8.0/Aura.Api');
      }
    } else {
      // In production, use the bundled backend from resources
      if (process.platform === 'win32') {
        return path.join(process.resourcesPath, 'backend', 'win-x64', 'Aura.Api.exe');
      } else if (process.platform === 'darwin') {
        return path.join(process.resourcesPath, 'backend', 'osx-x64', 'Aura.Api');
      } else {
        return path.join(process.resourcesPath, 'backend', 'linux-x64', 'Aura.Api');
      }
    }
  }

  /**
   * Get the path to FFmpeg binaries
   */
  _getFFmpegPath() {
    let ffmpegBinPath;
    
    if (this.isDev) {
      // In development, look for FFmpeg in resources directory
      const platform = process.platform;
      if (platform === 'win32') {
        ffmpegBinPath = path.join(__dirname, '../resources', 'ffmpeg', 'win-x64', 'bin');
      } else if (platform === 'darwin') {
        ffmpegBinPath = path.join(__dirname, '../resources', 'ffmpeg', 'osx-x64', 'bin');
      } else {
        ffmpegBinPath = path.join(__dirname, '../resources', 'ffmpeg', 'linux-x64', 'bin');
      }
    } else {
      // In production, use the bundled FFmpeg from resources
      const platform = process.platform;
      if (platform === 'win32') {
        ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'win-x64', 'bin');
      } else if (platform === 'darwin') {
        ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'osx-x64', 'bin');
      } else {
        ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'linux-x64', 'bin');
      }
    }
    
    return ffmpegBinPath;
  }

  /**
   * Verify FFmpeg installation
   */
  _verifyFFmpeg(ffmpegPath) {
    const ffmpegExe = process.platform === 'win32' ? 'ffmpeg.exe' : 'ffmpeg';
    const ffmpegFullPath = path.join(ffmpegPath, ffmpegExe);
    
    if (!fs.existsSync(ffmpegFullPath)) {
      console.warn(`FFmpeg not found at: ${ffmpegFullPath}`);
      return false;
    }
    
    console.log('FFmpeg found at:', ffmpegFullPath);
    return true;
  }

  /**
   * Prepare environment variables for backend
   */
  _prepareEnvironment() {
    const ffmpegPath = this._getFFmpegPath();
    
    return {
      ...process.env,
      ASPNETCORE_URLS: `http://localhost:${this.port}`,
      DOTNET_ENVIRONMENT: this.isDev ? 'Development' : 'Production',
      ASPNETCORE_DETAILEDERRORS: this.isDev ? 'true' : 'false',
      LOGGING__LOGLEVEL__DEFAULT: this.isDev ? 'Debug' : 'Information',
      // Set paths for user data
      AURA_DATA_PATH: this.app.getPath('userData'),
      AURA_LOGS_PATH: path.join(this.app.getPath('userData'), 'logs'),
      AURA_TEMP_PATH: path.join(this.app.getPath('temp'), 'aura-video-studio'),
      // Set FFmpeg path for backend
      FFMPEG_PATH: ffmpegPath,
      FFMPEG_BINARIES_PATH: ffmpegPath
    };
  }

  /**
   * Create necessary directories
   */
  _createDirectories(env) {
    const directories = [env.AURA_DATA_PATH, env.AURA_LOGS_PATH, env.AURA_TEMP_PATH];
    directories.forEach(dir => {
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
    });
  }

  /**
   * Setup process event handlers
   */
  _setupProcessHandlers() {
    // Handle backend output
    this.process.stdout.on('data', (data) => {
      const message = data.toString().trim();
      if (message) {
        console.log(`[Backend] ${message}`);
      }
    });

    this.process.stderr.on('data', (data) => {
      const message = data.toString().trim();
      if (message) {
        console.error(`[Backend Error] ${message}`);
      }
    });

    // Handle backend exit
    this.process.on('exit', (code, signal) => {
      console.log(`Backend exited with code ${code} and signal ${signal}`);
      
      if (!this.isQuitting && code !== 0) {
        // Backend crashed unexpectedly
        console.error('Backend crashed unexpectedly!');
        
        // Attempt auto-restart
        if (this.restartAttempts < this.maxRestartAttempts) {
          this.restartAttempts++;
          console.log(`Attempting to restart backend (${this.restartAttempts}/${this.maxRestartAttempts})...`);
          
          setTimeout(() => {
            this.restart().catch(error => {
              console.error('Failed to restart backend:', error);
            });
          }, this.AUTO_RESTART_DELAY);
        } else {
          console.error('Max restart attempts reached. Backend will not auto-restart.');
        }
      }
    });

    this.process.on('error', (error) => {
      console.error('Backend process error:', error);
    });
  }

  /**
   * Start periodic health checks
   */
  _startHealthChecks() {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }

    this.healthCheckInterval = setInterval(async () => {
      try {
        await axios.get(`http://localhost:${this.port}/health`, {
          timeout: 5000
        });
        // Reset restart attempts on successful health check
        this.restartAttempts = 0;
      } catch (error) {
        console.warn('Backend health check failed:', error.message);
      }
    }, 30000); // Check every 30 seconds
  }

  /**
   * Stop health checks
   */
  _stopHealthChecks() {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
      this.healthCheckInterval = null;
    }
  }
}

module.exports = BackendService;
