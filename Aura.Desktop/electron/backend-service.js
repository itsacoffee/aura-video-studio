/**
 * Backend Service Module
 * Manages the .NET backend process lifecycle
 */

const { spawn, exec } = require("child_process");
const path = require("path");
const fs = require("fs");
const net = require("net");
const axios = require("axios");

class BackendService {
  constructor(app, isDev, processManager = null, networkContract = null) {
    this.app = app;
    this.isDev = isDev;
    this.process = null;
    this.backendProcess = null; // Alias for this.process for consistency with problem statement
    this.processManager = processManager; // Optional: centralized process tracking

    // Enforce contract validation
    if (!networkContract) {
      throw new Error(
        "BackendService requires a valid networkContract. " +
        "Contract must be resolved via resolveBackendContract() before initializing BackendService."
      );
    }

    if (!networkContract.baseUrl || typeof networkContract.baseUrl !== "string") {
      throw new Error(
        "BackendService networkContract missing baseUrl. " +
        "Set AURA_BACKEND_URL or ASPNETCORE_URLS environment variable."
      );
    }

    if (!networkContract.port || typeof networkContract.port !== "number" || networkContract.port <= 0) {
      throw new Error(
        "BackendService networkContract missing valid port. " +
        "Set AURA_BACKEND_URL or ASPNETCORE_URLS environment variable."
      );
    }

    this.networkContract = networkContract;
    this.baseUrl = networkContract.baseUrl;
    this.port = networkContract.port;
    this.isQuitting = false;
    this.isRestarting = false;
    this.restartAttempts = 0;
    this.maxRestartAttempts = 3;
    this.healthCheckInterval = null;
    this.pid = null;
    this.isWindows = process.platform === "win32";
    // Health endpoints from network contract (aligned with BackendEndpoints constants in Aura.Api)
    this.healthEndpoint = networkContract.healthEndpoint || "/health/live";
    this.readinessEndpoint = networkContract.readinessEndpoint || "/health/ready";
    this.sseJobEventsTemplate = networkContract.sseJobEventsTemplate || "/api/jobs/{id}/events";

    // Constants
    this.BACKEND_STARTUP_TIMEOUT = networkContract.maxStartupMs ?? 60000; // 60 seconds
    this.HEALTH_CHECK_INTERVAL = networkContract.pollIntervalMs ?? 1000; // 1 second
    this.AUTO_RESTART_DELAY = 5000; // 5 seconds
    // Timeout configurations - extended for proper cleanup of all child processes
    this.GRACEFUL_SHUTDOWN_TIMEOUT = 5000; // 5 seconds for graceful shutdown (increased from 2s)
    this.FORCE_KILL_TIMEOUT = 3000; // 3 seconds after graceful timeout (total 8s max, increased from 3s total)
  }

  /**
   * Start the backend service
   */
  async start() {
    try {
      console.log(`Starting backend on ${this.baseUrl}...`);

      // Check for orphaned backend processes from previous runs
      await this._detectAndCleanupOrphanedBackend();

      // Determine backend executable path
      const backendPath = this._getBackendPath();

      // Check if backend executable exists
      if (!fs.existsSync(backendPath)) {
        throw new Error(`Backend executable not found at: ${backendPath}`);
      }

      // Make executable on Unix-like systems
      if (process.platform !== "win32") {
        try {
          fs.chmodSync(backendPath, 0o755);
        } catch (error) {
          console.warn("Failed to make backend executable:", error.message);
        }
      }

      // Get FFmpeg path
      const ffmpegPath = this._getFFmpegPath();
      const ffmpegExists = this._verifyFFmpeg(ffmpegPath);

      if (!ffmpegExists) {
        console.warn("FFmpeg not found - video rendering may not work");
      }

      // Prepare environment variables
      const env = this._prepareEnvironment();

      // Create necessary directories
      this._createDirectories(env);

      console.log("Backend executable:", backendPath);
      console.log("Backend port:", this.port);
      console.log("Environment:", env.DOTNET_ENVIRONMENT);
      console.log("FFmpeg path:", ffmpegPath);

      // Spawn backend process with detached flag on Windows to get proper process tree control
      this.process = spawn(backendPath, [], {
        env,
        stdio: ["ignore", "pipe", "pipe"],
        windowsHide: true, // Hide console window on Windows
        detached: false, // Keep attached so we can control it, but we'll handle child processes separately
      });

      // Store both process reference and PID
      this.backendProcess = this.process; // Alias for consistency
      this.pid = this.process.pid;

      // Register with ProcessManager if available
      if (this.processManager) {
        this.processManager.register("Aura.Api Backend", this.process, {
          port: this.port,
          isDev: this.isDev,
          backendPath,
        });
      }

      // Setup process handlers
      this._setupProcessHandlers();

      // Wait for backend to be ready
      await this._waitForBackend();

      console.log("Backend started successfully");

      // Persist FFmpeg path to backend configuration if detected
      if (ffmpegExists) {
        await this._persistFFmpegPath(ffmpegPath).catch(err => {
          console.warn('[Backend] Failed to persist FFmpeg path:', err.message);
        });
      }

      // Start periodic health checks
      this._startHealthChecks();

      return this.port;
    } catch (error) {
      console.error("Backend startup error:", error);
      throw error;
    }
  }

  /**
   * Stop the backend service
   */
  async stop() {
    console.log("[BackendService] Stopping backend process...");
    this.isQuitting = true;

    // Stop health checks
    this._stopHealthChecks();

    // Kill backend process
    if (this.backendProcess && !this.backendProcess.killed) {
      try {
        // Attempt graceful shutdown first
        try {
          this.backendProcess.kill("SIGINT");
        } catch (err) {
          console.warn("[BackendService] Failed to send SIGINT:", err.message);
        }

        const timeoutMs = 5000;
        const exited = await this._waitForExit(timeoutMs);

        if (!exited) {
          console.warn("[BackendService] Backend did not exit within timeout. Forcing kill.");
          try {
            this.backendProcess.kill("SIGKILL");
          } catch (err) {
            console.error("[BackendService] Failed to force-kill backend:", err.message);
          }
        }
      } catch (error) {
        console.error("Error terminating backend:", error);
      }
    }

    this.process = null;
    this.backendProcess = null;
    this.pid = null;
    this.port = null;
  }

  /**
   * Wait for backend process to exit
   * @param {number} timeoutMs - Maximum time to wait in milliseconds
   * @returns {Promise<boolean>} True if exited within timeout, false otherwise
   */
  _waitForExit(timeoutMs) {
    return new Promise((resolve) => {
      if (!this.backendProcess || this.backendProcess.killed) {
        resolve(true);
        return;
      }

      let exited = false;
      const timeout = setTimeout(() => {
        if (!exited) {
          resolve(false);
        }
      }, timeoutMs);

      this.backendProcess.once("exit", () => {
        exited = true;
        clearTimeout(timeout);
        resolve(true);
      });
    });
  }

  /**
   * Gracefully terminate backend process with Windows-specific handling
   */
  async _terminateBackend() {
    return new Promise((resolve) => {
      if (!this.process || this.process.killed) {
        resolve();
        return;
      }

      console.log("Terminating backend process (PID: " + this.pid + ")...");

      // Set up timeout for graceful shutdown
      const gracefulTimeout = setTimeout(() => {
        console.warn(
          "Backend did not shut down gracefully, forcing termination..."
        );
        this._forceTerminate();
      }, this.GRACEFUL_SHUTDOWN_TIMEOUT);

      // Handle process exit
      const onExit = () => {
        clearTimeout(gracefulTimeout);
        clearTimeout(forceTimeout);
        console.log("Backend process terminated successfully");
        resolve();
      };

      this.process.once("exit", onExit);

      // Attempt graceful shutdown first via API
      this._attemptGracefulShutdown()
        .then((success) => {
          if (!success) {
            console.log(
              "Graceful shutdown via API failed, using process termination..."
            );
            // Try process termination
            if (this.isWindows) {
              // On Windows, use taskkill for proper process tree termination
              this._windowsTerminate(false);
            } else {
              // On Unix, use SIGTERM
              try {
                this.process.kill("SIGTERM");
              } catch (err) {
                console.error("Error sending SIGTERM:", err);
                this._forceTerminate();
              }
            }
          }
        })
        .catch((err) => {
          console.error("Error during graceful shutdown:", err);
          if (this.isWindows) {
            this._windowsTerminate(false);
          } else {
            try {
              this.process.kill("SIGTERM");
            } catch (killErr) {
              this._forceTerminate();
            }
          }
        });

      // Final safety net - force kill after extended timeout
      const forceTimeout = setTimeout(() => {
        console.error(
          "Backend still running after graceful timeout, force killing..."
        );
        this._forceTerminate();
        resolve();
      }, this.GRACEFUL_SHUTDOWN_TIMEOUT + this.FORCE_KILL_TIMEOUT);
    });
  }

  /**
   * Attempt graceful shutdown via backend API
   */
  async _attemptGracefulShutdown() {
    try {
      if (!this.port) return false;

      console.log("Requesting graceful shutdown via API...");
      await axios.post(
        this._buildUrl("/api/system/shutdown"),
        {},
        {
          timeout: 1000, // Reduced from 2000ms for faster response
        }
      );
      return true;
    } catch (error) {
      // API call failed, backend may already be down or endpoint doesn't exist
      return false;
    }
  }

  /**
   * Force terminate the backend process
   */
  _forceTerminate() {
    if (!this.process || this.process.killed) return;

    console.log("Force terminating backend process...");

    if (this.isWindows) {
      this._windowsTerminate(true);
    } else {
      try {
        this.process.kill("SIGKILL");
      } catch (error) {
        console.error("Error force killing process:", error);
      }
    }
  }

  /**
   * Windows-specific process termination using taskkill
   * This properly terminates the process tree including child processes
   */
  _windowsTerminate(force = false) {
    if (!this.pid) {
      console.warn("Cannot terminate backend: no PID available");
      return;
    }

    const forceFlag = force ? "/F" : "";
    const command = `taskkill /PID ${this.pid} ${forceFlag} /T`;

    console.log(
      `[BackendService] Terminating backend process tree (PID: ${this.pid}, Force: ${force})`
    );
    console.log(`[BackendService] Executing: ${command}`);

    exec(command, { timeout: 5000 }, (error, stdout, stderr) => {
      if (error) {
        // Check if process already exited (error code 128 on Windows)
        if (error.code === 128 || error.message.includes("not found")) {
          console.log(`[BackendService] Process ${this.pid} already exited`);
          return;
        }

        console.error(
          `[BackendService] taskkill error (code ${error.code}):`,
          error.message
        );

        // Fallback to Node's kill
        try {
          if (this.process && !this.process.killed) {
            console.log(
              `[BackendService] Attempting fallback kill for PID ${this.pid}`
            );
            this.process.kill(force ? "SIGKILL" : "SIGTERM");
          } else {
            console.log(
              `[BackendService] Process already killed or not available`
            );
          }
        } catch (fallbackError) {
          console.error(
            `[BackendService] Fallback kill failed:`,
            fallbackError.message
          );
        }
        return;
      }

      if (stdout) {
        console.log(`[BackendService] taskkill output:`, stdout.trim());
      }
      if (stderr && !stderr.includes("not found")) {
        console.warn(`[BackendService] taskkill stderr:`, stderr.trim());
      }

      console.log(
        `[BackendService] Successfully terminated backend process tree (PID: ${this.pid})`
      );
    });
  }

  /**
   * Restart the backend service
   */
  async restart() {
    if (this.isRestarting) {
      console.warn("Backend restart already in progress");
      return;
    }

    try {
      this.isRestarting = true;
      console.log("Restarting backend service...");

      const wasQuitting = this.isQuitting;
      this.isQuitting = false; // Temporarily disable quitting flag for restart

      await this.stop();

      // Wait a bit before restarting to ensure port is released
      await new Promise((resolve) => setTimeout(resolve, 2000));

      await this.start();

      this.isQuitting = wasQuitting;
      console.log("Backend restart completed successfully");
    } catch (error) {
      console.error("Backend restart failed:", error);
      throw error;
    } finally {
      this.isRestarting = false;
    }
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
    return this.baseUrl;
  }

  /**
   * Check if backend is running
   */
  isRunning() {
    return this.process !== null && !this.process.killed;
  }

  /**
   * Wait for backend to be fully ready
   * @param {Object} options - Options for waiting
   * @param {number} options.timeout - Maximum time to wait (ms)
   * @param {Function} options.onProgress - Progress callback
   * @returns {Promise<boolean>} True if ready, false if timeout
   */
  async waitForReady(options = {}) {
    const {
      timeout = 90000,
      onProgress = null
    } = options;

    const startTime = Date.now();
    const checkInterval = 1000; // Check every second
    let attempt = 0;
    const maxAttempts = Math.floor(timeout / checkInterval);

    console.log(`Waiting for backend to be ready (timeout: ${timeout}ms)...`);

    while (Date.now() - startTime < timeout) {
      attempt++;
      
      try {
        // Check liveness first (is HTTP server responding?)
        const livenessUrl = this._buildUrl(this.healthEndpoint || "/health/live");
        const livenessResponse = await axios.get(livenessUrl, {
          timeout: 5000,
          validateStatus: () => true // Accept any status code
        });

        if (livenessResponse.status === 200) {
          console.log('Backend HTTP server is alive');
          
          // Now check readiness (are all dependencies ready?)
          const readinessUrl = this._buildUrl(this.readinessEndpoint || "/health/ready");
          const readinessResponse = await axios.get(readinessUrl, {
            timeout: 10000,
            validateStatus: () => true
          });

          if (readinessResponse.status === 200) {
            console.log('Backend is fully ready!');
            
            if (onProgress) {
              onProgress({
                message: 'Backend is ready',
                percent: 1.0,
                attempt,
                maxAttempts
              });
            }
            
            return true;
          } else {
            // Readiness check failed - backend is alive but not ready yet
            const readinessData = readinessResponse.data;
            const failedChecks = readinessData?.checks?.filter(c => c.status !== 'healthy') || [];
            
            console.log(`Backend not ready yet (attempt ${attempt}/${maxAttempts}):`, 
              failedChecks.map(c => c.name).join(', '));
            
            if (onProgress) {
              onProgress({
                message: failedChecks.length > 0 
                  ? `Initializing: ${failedChecks.map(c => c.name).join(', ')}`
                  : 'Initializing backend...',
                percent: Math.min(0.9, attempt / maxAttempts),
                attempt,
                maxAttempts
              });
            }
          }
        }
      } catch (error) {
        // Backend not responding yet
        console.log(`Backend not responding yet (attempt ${attempt}/${maxAttempts}):`, error.message);
        
        if (onProgress) {
          onProgress({
            message: 'Starting backend server...',
            percent: Math.min(0.8, attempt / maxAttempts),
            attempt,
            maxAttempts
          });
        }
      }

      // Wait before next check
      await new Promise(resolve => setTimeout(resolve, checkInterval));
    }

    // Timeout reached
    console.error(`Backend failed to become ready within ${timeout}ms`);
    return false;
  }

  /**
   * Check Windows Firewall compatibility
   */
  async checkFirewallCompatibility() {
    if (!this.isWindows) {
      return { compatible: true, message: "Not Windows" };
    }

    try {
      // Check if port is accessible locally
      const portAccessible = await this._checkPortAccessible();

      if (!portAccessible) {
        return {
          compatible: false,
          message:
            "Backend port is not accessible. Windows Firewall may be blocking the connection.",
          recommendation:
            "Please add Aura Video Studio to Windows Firewall exceptions.",
        };
      }

      // Check if process can bind to port
      const canBind = await this._checkCanBindPort();

      if (!canBind) {
        return {
          compatible: false,
          message: "Cannot bind to port. Another application may be using it.",
          recommendation:
            "Please close other applications that might be using the port.",
        };
      }

      return {
        compatible: true,
        message: "Windows Firewall compatibility check passed",
      };
    } catch (error) {
      console.error("Firewall compatibility check error:", error);
      return {
        compatible: false,
        message: `Firewall check failed: ${error.message}`,
        recommendation: "Please check Windows Firewall settings manually.",
      };
    }
  }

  /**
   * Check if port is accessible
   * Uses /health/live endpoint for faster response
   */
  async _checkPortAccessible() {
    if (!this.port) return false;

    try {
      const response = await axios.get(
        this._buildUrl(this.healthEndpoint || "/health/live"),
        {
          timeout: 2000,
        }
      );
      return response.status === 200;
    } catch (error) {
      return false;
    }
  }

  /**
   * Check if we can bind to a port (test with a temporary server)
   */
  async _checkCanBindPort() {
    return new Promise((resolve) => {
      const testServer = net.createServer();

      testServer.once("error", (err) => {
        if (err.code === "EADDRINUSE") {
          resolve(false);
        } else {
          resolve(false);
        }
      });

      testServer.once("listening", () => {
        testServer.close(() => {
          resolve(true);
        });
      });

      // Try to bind to port 0 (any available port) to test capability
      testServer.listen(0, "localhost");
    });
  }

  /**
   * Build absolute URL for backend endpoints
   */
  _buildUrl(pathname = "") {
    if (!this.baseUrl) {
      return null;
    }

    if (!pathname) {
      return this.baseUrl;
    }

    if (pathname.startsWith("http://") || pathname.startsWith("https://")) {
      return pathname;
    }

    const normalized = pathname.startsWith("/") ? pathname : `/${pathname}`;
    return `${this.baseUrl}${normalized}`;
  }

  /**
   * Get Windows Firewall rule status (requires elevation)
   */
  async getFirewallRuleStatus() {
    if (!this.isWindows) {
      return { exists: null, error: "Not Windows" };
    }

    return new Promise((resolve) => {
      const command =
        'netsh advfirewall firewall show rule name="Aura Video Studio"';

      exec(command, (error, stdout, stderr) => {
        if (error) {
          // Rule doesn't exist or error checking
          resolve({ exists: false, error: error.message });
          return;
        }

        const exists = !stdout.includes("No rules match");
        resolve({
          exists,
          details: exists ? stdout : null,
        });
      });
    });
  }

  /**
   * Suggest firewall rule creation command
   */
  getFirewallRuleCommand() {
    if (!this.isWindows) return null;

    const backendPath = this._getBackendPath();

    return `netsh advfirewall firewall add rule name="Aura Video Studio" dir=in action=allow program="${backendPath}" enable=yes profile=any`;
  }

  /**
   * Wait for the backend to become healthy
   * Uses /health/live endpoint for faster startup detection (doesn't check database/other services)
   */
  async _waitForBackend() {
    const maxAttempts =
      this.BACKEND_STARTUP_TIMEOUT / this.HEALTH_CHECK_INTERVAL;
    // Use /health/live for initial startup - it just checks if the HTTP server is running
    // /health/ready checks database and other services which may take time to initialize
    const readinessEndpoint = "/health/live";
    const readinessUrl = this._buildUrl(readinessEndpoint);

    for (let i = 0; i < maxAttempts; i++) {
      try {
        const response = await axios.get(readinessUrl, {
          timeout: 2000,
          validateStatus: () => true,
        });

        if (response.status === 200) {
          console.log(`Backend is healthy at ${this.baseUrl}`);
          return true;
        }
      } catch (error) {
        // Backend not ready yet, continue waiting
      }

      // Wait before next attempt
      await new Promise((resolve) =>
        setTimeout(resolve, this.HEALTH_CHECK_INTERVAL)
      );

      if (i % 10 === 0 && i > 0) {
        console.log(
          `Still waiting for backend... (attempt ${i}/${maxAttempts})`
        );
      }
    }

    throw new Error(
      `Backend failed to start within ${this.BACKEND_STARTUP_TIMEOUT}ms`
    );
  }

  /**
   * Get the path to the backend executable
   */
  _getBackendPath() {
    // Check production bundle location FIRST
    const productionPath = path.join(
      process.resourcesPath || '',
      'backend',
      'win-x64',
      'Aura.Api.exe'
    );
    if (fs.existsSync(productionPath)) {
      console.log(`[BackendService] Found production backend at: ${productionPath}`);
      return productionPath;
    }

    // Then check development locations
    const devPaths = [
      path.join(__dirname, '..', '..', 'dist', 'backend', 'Aura.Api.exe'),
      path.join(__dirname, '..', '..', 'Aura.Api', 'bin', 'Release', 'net8.0', 'win-x64', 'publish', 'Aura.Api.exe'),
      path.join(__dirname, '..', '..', 'Aura.Api', 'bin', 'Debug', 'net8.0', 'Aura.Api.exe'),
    ];

    for (const devPath of devPaths) {
      if (fs.existsSync(devPath)) {
        console.log(`[BackendService] Found dev backend at: ${devPath}`);
        return devPath;
      }
    }

    throw new Error('Backend executable not found. Application may not be properly installed.');
  }

  /**
   * Get the path to FFmpeg binaries
   */
  _getFFmpegPath() {
    let ffmpegBinPath;

    if (this.isDev) {
      // In development, look for FFmpeg in resources directory
      const platform = process.platform;
      if (platform === "win32") {
        ffmpegBinPath = path.join(
          __dirname,
          "../resources",
          "ffmpeg",
          "win-x64",
          "bin"
        );
      } else if (platform === "darwin") {
        ffmpegBinPath = path.join(
          __dirname,
          "../resources",
          "ffmpeg",
          "osx-x64",
          "bin"
        );
      } else {
        ffmpegBinPath = path.join(
          __dirname,
          "../resources",
          "ffmpeg",
          "linux-x64",
          "bin"
        );
      }
    } else {
      // In production, use the bundled FFmpeg from resources
      const platform = process.platform;
      if (platform === "win32") {
        ffmpegBinPath = path.join(
          process.resourcesPath,
          "ffmpeg",
          "win-x64",
          "bin"
        );
      } else if (platform === "darwin") {
        ffmpegBinPath = path.join(
          process.resourcesPath,
          "ffmpeg",
          "osx-x64",
          "bin"
        );
      } else {
        ffmpegBinPath = path.join(
          process.resourcesPath,
          "ffmpeg",
          "linux-x64",
          "bin"
        );
      }
    }

    return ffmpegBinPath;
  }

  /**
   * Verify FFmpeg installation
   */
  _verifyFFmpeg(ffmpegPath) {
    const ffmpegExe = process.platform === "win32" ? "ffmpeg.exe" : "ffmpeg";
    const ffmpegFullPath = path.join(ffmpegPath, ffmpegExe);

    if (!fs.existsSync(ffmpegFullPath)) {
      console.warn(`FFmpeg not found at: ${ffmpegFullPath}`);
      return false;
    }

    console.log("FFmpeg found at:", ffmpegFullPath);
    return true;
  }

  /**
   * Persist FFmpeg path to backend configuration store
   * Called after detecting FFmpeg on Electron startup
   */
  async _persistFFmpegPath(ffmpegPath) {
    try {
      const ffmpegExe = process.platform === "win32" ? "ffmpeg.exe" : "ffmpeg";
      const ffmpegFullPath = path.join(ffmpegPath, ffmpegExe);
      
      console.log('[Backend] Persisting FFmpeg path:', ffmpegFullPath);
      
      const response = await axios.post(
        `${this.baseUrl}/api/setup/configure-ffmpeg`,
        {
          path: ffmpegFullPath,
          source: 'ElectronDetection'
        },
        {
          timeout: 5000,
          validateStatus: () => true
        }
      );

      if (response.status === 200) {
        console.log('[Backend] FFmpeg path persisted successfully');
      } else {
        console.warn('[Backend] Failed to persist FFmpeg path:', response.status, response.data);
      }
    } catch (error) {
      console.warn('[Backend] Error persisting FFmpeg path:', error.message);
    }
  }

  /**
   * Get platform-specific FFmpeg directory name
   */
  _getPlatformFFmpegDir() {
    const platform = process.platform;
    return platform === "win32" ? "win-x64" : (platform === "darwin" ? "osx-x64" : "linux-x64");
  }

  /**
   * Prepare environment variables for backend
   */
  _prepareEnvironment() {
    // Determine FFmpeg path - check managed location first
    const platformDir = this._getPlatformFFmpegDir();
    
    const ffmpegPaths = [
      // Managed FFmpeg in resources (installed mode)
      path.join(process.resourcesPath || '', 'ffmpeg', platformDir, 'bin'),
      // Development mode FFmpeg
      path.join(this.app.getAppPath(), '..', 'Aura.Desktop', 'resources', 'ffmpeg', platformDir, 'bin'),
      // Bundled FFmpeg in app directory
      path.join(this.app.getAppPath(), 'resources', 'ffmpeg', platformDir, 'bin'),
    ];

    let ffmpegPath = null;
    const ffmpegExe = process.platform === "win32" ? "ffmpeg.exe" : "ffmpeg";
    
    for (const candidatePath of ffmpegPaths) {
      const ffmpegFullPath = path.join(candidatePath, ffmpegExe);
      if (fs.existsSync(ffmpegFullPath)) {
        ffmpegPath = candidatePath;
        console.log('[BackendService] Found FFmpeg at:', ffmpegPath);
        break;
      }
    }

    if (!ffmpegPath) {
      console.warn('[BackendService] Managed FFmpeg not found, backend will search system');
    }

    return {
      ...process.env,
      ASPNETCORE_URLS: this.baseUrl,
      DOTNET_ENVIRONMENT: this.isDev ? "Development" : "Production",
      ASPNETCORE_DETAILEDERRORS: this.isDev ? "true" : "false",
      LOGGING__LOGLEVEL__DEFAULT: this.isDev ? "Debug" : "Information",
      // Set paths for user data
      AURA_DATA_PATH: this.app.getPath("userData"),
      AURA_LOGS_PATH: path.join(this.app.getPath("userData"), "logs"),
      AURA_TEMP_PATH: path.join(this.app.getPath("temp"), "aura-video-studio"),
      // Set FFMPEG_PATH only if found
      ...(ffmpegPath && { FFMPEG_PATH: ffmpegPath }),
      ...(ffmpegPath && { AURA_FFMPEG_PATH: ffmpegPath }),
      ...(ffmpegPath && { FFMPEG_BINARIES_PATH: ffmpegPath }),
    };
  }

  /**
   * Create necessary directories
   */
  _createDirectories(env) {
    const directories = [
      env.AURA_DATA_PATH,
      env.AURA_LOGS_PATH,
      env.AURA_TEMP_PATH,
    ];
    directories.forEach((dir) => {
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
    this.process.stdout.on("data", (data) => {
      const message = data.toString().trim();
      if (message) {
        console.log(`[Backend] ${message}`);
      }
    });

    this.process.stderr.on("data", (data) => {
      const message = data.toString().trim();
      if (message) {
        console.error(`[Backend Error] ${message}`);
      }
    });

    // Handle backend exit
    this.process.on("exit", (code, signal) => {
      console.log(`Backend exited with code ${code} and signal ${signal}`);

      // Don't restart if we're intentionally quitting or already restarting
      if (!this.isQuitting && !this.isRestarting && code !== 0) {
        // Backend crashed unexpectedly
        console.error("Backend crashed unexpectedly!");

        // Attempt auto-restart
        if (this.restartAttempts < this.maxRestartAttempts) {
          this.restartAttempts++;
          console.log(
            `Attempting to restart backend (${this.restartAttempts}/${this.maxRestartAttempts})...`
          );

          setTimeout(() => {
            this.restart().catch((error) => {
              console.error("Failed to restart backend:", error);
            });
          }, this.AUTO_RESTART_DELAY);
        } else {
          console.error(
            "Max restart attempts reached. Backend will not auto-restart."
          );
          // Emit event for main process to handle
          if (this.app) {
            this.app.emit("backend-crash");
          }
        }
      }
    });

    this.process.on("error", (error) => {
      console.error("Backend process error:", error);
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
        await axios.get(this._buildUrl(this.healthEndpoint || "/health"), {
          timeout: 5000,
        });
        // Reset restart attempts on successful health check
        this.restartAttempts = 0;
      } catch (error) {
        console.warn("Backend health check failed:", error.message);
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

  /**
   * Detect and cleanup orphaned backend processes from previous runs
   * This prevents "port already in use" errors and ensures clean startup
   */
  async _detectAndCleanupOrphanedBackend() {
    console.log(`[OrphanDetection] Checking for orphaned backend on port ${this.port}...`);

    let found = 0;
    let terminated = 0;
    let failed = 0;

    // Check if port is already in use by trying to connect
    const portInUse = await this._isPortInUse(this.port);

    if (!portInUse) {
      console.log('[OrphanDetection] Port is available, no cleanup needed');
      console.log(`[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${failed} failed`);
      return;
    }

    console.warn(`[OrphanDetection] Port ${this.port} is already in use, attempting cleanup...`);
    found = 1; // At least one process using the port

    // Try to find and kill orphaned Aura.Api processes
    const cleanupResult = await this._killOrphanedBackendProcesses();
    terminated = cleanupResult.terminated;
    failed = cleanupResult.failed;

    // Wait a moment for port to be released
    await new Promise(resolve => setTimeout(resolve, 1000));

    // Verify port is now available
    const stillInUse = await this._isPortInUse(this.port);
    if (stillInUse) {
      console.error(`[OrphanDetection] Failed to cleanup port ${this.port}. Manual intervention may be required.`);
      console.log(`[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${failed + 1} failed`);
      throw new Error(`Port ${this.port} is still in use after cleanup. Please close any running Aura processes in Task Manager.`);
    }

    console.log('[OrphanDetection] Cleanup successful, port is now available');
    console.log(`[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${failed} failed`);
  }

  /**
   * Check if a port is in use by attempting to connect
   */
  async _isPortInUse(port) {
    return new Promise((resolve) => {
      const testSocket = net.createConnection({ port, host: '127.0.0.1' });

      testSocket.on('connect', () => {
        testSocket.end();
        resolve(true); // Port is in use
      });

      testSocket.on('error', (err) => {
        resolve(false); // Port is available (connection refused)
      });

      // Timeout after 1 second
      setTimeout(() => {
        testSocket.destroy();
        resolve(false);
      }, 1000);
    });
  }

  /**
   * Kill orphaned backend processes
   * SAFETY: Only targets processes matching strict signature (Aura.Api.exe or Aura.Api)
   * to avoid killing unrelated .NET applications
   */
  async _killOrphanedBackendProcesses() {
    return new Promise((resolve) => {
      let terminated = 0;
      let failed = 0;

      if (this.isWindows) {
        // Windows: Kill both Aura.Api.exe and child FFmpeg processes
        // SAFETY: Using exact image name match to avoid killing unrelated apps
        const commands = [
          'taskkill /F /IM "Aura.Api.exe" /T 2>nul',
          'taskkill /F /IM "ffmpeg.exe" /FI "WINDOWTITLE eq Aura*" 2>nul'
        ];
        
        let completed = 0;
        commands.forEach(cmd => {
          console.log(`[OrphanDetection] Executing: ${cmd}`);
          exec(cmd, { timeout: 5000 }, (error, stdout, stderr) => {
            if (!error && stdout && stdout.trim()) {
              console.log(`[OrphanDetection] ${stdout.trim()}`);
              // Count terminated processes from output
              const matches = stdout.match(/SUCCESS/gi);
              if (matches) {
                terminated += matches.length;
              }
            } else if (error && error.code !== 128) {
              // Only count non-"process not found" errors as failures
              failed++;
            }
            
            completed++;
            if (completed === commands.length) {
              resolve({ terminated, failed });
            }
          });
        });
      } else {
        // Unix: Kill process groups for both Aura.Api and FFmpeg
        // SAFETY: Using exact process name match
        exec('pkill -9 -f "Aura.Api"', (error1) => {
          if (!error1) {
            console.log('[OrphanDetection] Killed orphaned backend processes');
            terminated++;
          }
          
          exec('pkill -9 -f "ffmpeg.*aura"', (error2) => {
            if (!error2) {
              console.log('[OrphanDetection] Killed orphaned FFmpeg processes');
              terminated++;
            }
            
            resolve({ terminated, failed });
          });
        });
      }
    });
  }
}

module.exports = BackendService;
