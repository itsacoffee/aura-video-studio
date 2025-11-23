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
  constructor(
    app,
    isDev,
    processManager = null,
    networkContract = null,
    logger = null
  ) {
    this.app = app;
    this.isDev = isDev;
    this.process = null;
    this.backendProcess = null; // Alias for this.process for consistency with problem statement
    this.processManager = processManager; // Optional: centralized process tracking
    this.logger = logger || console; // Use provided logger or fallback to console

    // Enforce contract validation
    if (!networkContract) {
      throw new Error(
        "BackendService requires a valid networkContract. " +
          "Contract must be resolved via resolveBackendContract() before initializing BackendService."
      );
    }

    if (
      !networkContract.baseUrl ||
      typeof networkContract.baseUrl !== "string"
    ) {
      throw new Error(
        "BackendService networkContract missing baseUrl. " +
          "Set AURA_BACKEND_URL or ASPNETCORE_URLS environment variable."
      );
    }

    if (
      !networkContract.port ||
      typeof networkContract.port !== "number" ||
      networkContract.port <= 0
    ) {
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
    this.readinessEndpoint =
      networkContract.readinessEndpoint || "/health/ready";
    this.sseJobEventsTemplate =
      networkContract.sseJobEventsTemplate || "/api/jobs/{id}/events";

    // Constants
    this.BACKEND_STARTUP_TIMEOUT = networkContract.maxStartupMs ?? 60000; // 60 seconds
    this.HEALTH_CHECK_INTERVAL = networkContract.pollIntervalMs ?? 1000; // 1 second
    this.AUTO_RESTART_DELAY = 5000; // 5 seconds
    // Timeout configurations - extended for proper cleanup of all child processes
    this.GRACEFUL_SHUTDOWN_TIMEOUT = 5000; // 5 seconds for graceful shutdown (increased from 2s)
    this.FORCE_KILL_TIMEOUT = 3000; // 3 seconds after graceful timeout (total 8s max, increased from 3s total)
  }

  /**
   * Start the backend service with retry logic
   */
  async start() {
    const maxRetries = 3;
    let lastError = null;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        if (attempt > 1) {
          const backoffMs = Math.min(1000 * Math.pow(2, attempt - 2), 5000);
          console.log(
            `[BackendService] Retry attempt ${attempt}/${maxRetries} after ${backoffMs}ms backoff...`
          );
          await new Promise((resolve) => setTimeout(resolve, backoffMs));
        }

        await this._startInternal();
        console.log(`[BackendService] Backend started successfully on attempt ${attempt}`);
        return this.port;
      } catch (error) {
        lastError = error;
        console.error(
          `[BackendService] Startup attempt ${attempt}/${maxRetries} failed:`,
          error.message
        );

        // Don't retry for certain unrecoverable errors
        if (
          error.message.includes("Backend executable not found") ||
          error.message.includes(".NET runtime") ||
          error.message.includes("missing required dependencies")
        ) {
          console.error(
            "[BackendService] Unrecoverable error detected, skipping retries"
          );
          throw error;
        }

        // Clean up failed process if it exists
        if (this.process && !this.process.killed) {
          try {
            this.process.kill("SIGKILL");
          } catch (killErr) {
            console.warn("[BackendService] Failed to kill failed process:", killErr.message);
          }
        }
        this.process = null;
        this.backendProcess = null;
        this.pid = null;
      }
    }

    // All retries failed
    throw new Error(
      `Backend failed to start after ${maxRetries} attempts. Last error: ${lastError.message}`
    );
  }

  /**
   * Internal startup implementation with comprehensive validation
   */
  async _startInternal() {
    try {
      console.log(`[BackendService] Starting backend on ${this.baseUrl}...`);

      // PRE-STARTUP VALIDATION PHASE
      console.log("[BackendService] Running pre-startup validation checks...");

      // 1. Check for orphaned backend processes from previous runs
      await this._detectAndCleanupOrphanedBackend();

      // 2. Validate .NET runtime availability
      const dotnetValidation = await this._validateDotnetRuntime();
      if (!dotnetValidation.available) {
        throw new Error(
          `Backend startup failed: .NET runtime not found or incompatible. ${dotnetValidation.error || ""}\n` +
          `Required: .NET 8.0 SDK or Runtime\n` +
          `Install from: https://dotnet.microsoft.com/download/dotnet/8.0`
        );
      }
      console.log(`[BackendService] ✓ .NET runtime validated: ${dotnetValidation.version}`);

      // 3. Determine backend executable path
      let backendPath = null;
      let useDotnetRun = false;

      try {
        backendPath = this._getBackendPath();
      } catch (e) {
        if (this.isDev) {
          console.log(
            "[BackendService] Backend executable not found, falling back to 'dotnet run'"
          );
          useDotnetRun = true;
        } else {
          throw new Error(
            `Backend executable not found: ${e.message}\n` +
            `Expected location: ${this._getExpectedBackendPath()}\n` +
            `This indicates the application was not properly installed or the backend build is missing.`
          );
        }
      }

      // 4. Validate backend executable exists and is accessible
      if (!useDotnetRun) {
        const exeValidation = this._validateBackendExecutable(backendPath);
        if (!exeValidation.valid) {
          throw new Error(
            `Backend executable validation failed: ${exeValidation.error}\n` +
            `Path: ${backendPath}\n` +
            `${exeValidation.suggestion || ""}`
          );
        }
        console.log(`[BackendService] ✓ Backend executable validated: ${backendPath}`);
      }

      // 5. Make executable on Unix-like systems
      if (!useDotnetRun && process.platform !== "win32") {
        try {
          fs.chmodSync(backendPath, 0o755);
          console.log("[BackendService] ✓ Executable permissions set");
        } catch (error) {
          console.warn("[BackendService] ⚠ Failed to make backend executable:", error.message);
        }
      }

      // 6. Check port availability
      const portCheck = await this._checkPortAvailability(this.port);
      if (!portCheck.available) {
        throw new Error(
          `Backend startup failed: Port ${this.port} is already in use.\n` +
          `${portCheck.conflictInfo || ""}\n` +
          `Please close the application using this port and try again.`
        );
      }
      console.log(`[BackendService] ✓ Port ${this.port} is available`);

      // 7. Validate FFmpeg availability (warning only, not fatal)
      const ffmpegPath = this._getFFmpegPath();
      const ffmpegExists = this._verifyFFmpeg(ffmpegPath);

      if (!ffmpegExists) {
        console.warn(
          "[BackendService] ⚠ FFmpeg not found - video rendering may not work\n" +
          `  Expected: ${ffmpegPath}\n` +
          "  Video generation features will be disabled until FFmpeg is installed."
        );
      } else {
        console.log(`[BackendService] ✓ FFmpeg validated: ${ffmpegPath}`);
      }

      // STARTUP PHASE
      console.log("[BackendService] Pre-startup validation complete, starting backend process...");

      // Prepare environment variables
      const env = this._prepareEnvironment();

      // Create necessary directories
      this._createDirectories(env);

      console.log("[BackendService] =".repeat(30));
      console.log("[BackendService] BACKEND STARTUP CONFIGURATION");
      console.log("[BackendService] =".repeat(30));
      console.log("[BackendService]   URL:", this.baseUrl);
      console.log("[BackendService]   Port:", this.port);
      console.log("[BackendService]   Environment:", env.DOTNET_ENVIRONMENT);
      console.log("[BackendService]   Is Packaged:", !this.isDev);
      console.log("[BackendService]   FFmpeg path:", ffmpegPath || "(not found - video features disabled)");
      console.log("[BackendService]   Health endpoint:", this.healthEndpoint);
      console.log("[BackendService]   Readiness endpoint:", this.readinessEndpoint);
      console.log("[BackendService]   ASPNETCORE_URLS:", env.ASPNETCORE_URLS);
      console.log("[BackendService]   Command:", useDotnetRun ? "dotnet run" : backendPath);
      console.log("[BackendService]   Data Path:", env.AURA_DATA_PATH);
      console.log("[BackendService]   Logs Path:", env.AURA_LOGS_PATH);
      console.log("[BackendService] =".repeat(30));

      if (useDotnetRun) {
        // Run via dotnet run
        const apiProject = path.resolve(
          __dirname,
          "../../Aura.Api/Aura.Api.csproj"
        );
        console.log("[BackendService] Starting backend via dotnet run...");
        console.log("[BackendService] Project:", apiProject);

        // Use --urls to explicitly set the listening address, bypassing launchSettings.json
        this.process = spawn(
          "dotnet",
          [
            "run",
            "--no-launch-profile",
            "--project",
            apiProject,
            "--urls",
            this.baseUrl,
          ],
          {
            env,
            stdio: ["ignore", "pipe", "pipe"],
            windowsHide: true,
            detached: false,
          }
        );
      } else {
        // Spawn backend executable
        console.log("[BackendService] Spawning backend executable:", backendPath);
        this.process = spawn(backendPath, [], {
          env,
          stdio: ["ignore", "pipe", "pipe"],
          windowsHide: true, // Hide console window on Windows
          detached: false, // Keep attached so we can control it, but we'll handle child processes separately
        });
      }

      // Verify process spawned successfully
      if (!this.process || !this.process.pid) {
        throw new Error(
          "Backend process failed to spawn. The process object is invalid.\n" +
          "This may indicate a system-level issue preventing process creation."
        );
      }

      // Store both process reference and PID
      this.backendProcess = this.process; // Alias for consistency
      this.pid = this.process.pid;

      console.log(`[BackendService] ✓ Backend process spawned (PID: ${this.pid})`);
      console.log(`[BackendService] ℹ Backend will be available at: ${this.baseUrl}`);
      console.log(`[BackendService] ℹ Frontend should connect to: ${this.baseUrl}/health/live for health checks`);
      console.log(`[BackendService] ℹ Waiting for backend to start listening... (max ${this.BACKEND_STARTUP_TIMEOUT/1000}s)`);

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

      // HEALTH CHECK PHASE
      console.log("[BackendService] Waiting for backend to become healthy...");

      // Wait for backend to be ready
      await this._waitForBackend();

      console.log("[BackendService] ✓ Backend health check passed");

      // Persist FFmpeg path to backend configuration if detected
      if (ffmpegExists) {
        await this._persistFFmpegPath(ffmpegPath).catch((err) => {
          console.warn("[Backend] Failed to persist FFmpeg path:", err.message);
        });
      }

      // Start periodic health checks
      this._startHealthChecks();

      console.log("[BackendService] ✓ Backend started successfully");
    } catch (error) {
      // Enhanced error logging with context
      console.error("[BackendService] Backend startup failed:", error.message);

      // Add diagnostic information to the error
      if (error.diagnostics === undefined) {
        error.diagnostics = {
          port: this.port,
          baseUrl: this.baseUrl,
          isDev: this.isDev,
          processSpawned: !!this.process,
          processPid: this.process?.pid || null,
          processExited: this.process?.exitCode !== null || this.process?.signalCode !== null,
          startupOutput: this.startupOutputLines?.slice(0, 20).join("\n") || "(none)",
          errorOutput: this.errorOutputLines?.slice(0, 20).join("\n") || "(none)",
        };
      }

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
          console.warn(
            "[BackendService] Backend did not exit within timeout. Forcing kill."
          );
          try {
            this.backendProcess.kill("SIGKILL");
          } catch (err) {
            console.error(
              "[BackendService] Failed to force-kill backend:",
              err.message
            );
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
   * Wait for backend to be ready with enhanced health checks and exponential backoff
   * @param {Object} options - Wait options
   * @param {number} options.timeout - Maximum wait time in ms (default: 90000)
   * @param {Function} options.onProgress - Progress callback
   * @returns {Promise<boolean>} True if ready, false if timeout
   */
  async waitForReady({ timeout = 90000, onProgress = null } = {}) {
    const startTime = Date.now();
    const healthCheckUrl = this._buildUrl("/health");
    let lastError = null;
    let attemptCount = 0;
    let consecutiveFailures = 0;
    const maxAttempts = Math.floor(timeout / 1000); // One attempt per second

    this.logger.info?.(
      "BackendService",
      `Waiting for backend health check at: ${healthCheckUrl}`
    );

    while (Date.now() - startTime < timeout) {
      attemptCount++;

      try {
        // Check if process is still running
        if (this.process && this.process.killed) {
          this.logger.error?.("BackendService", "Backend process was killed");
          return false;
        }

        // IMPROVED: Try health check with more lenient status validation during startup
        // Accept any 2xx status code (200-299) not just 200, as backend may return 204 or other success codes
        const response = await axios.get(healthCheckUrl, {
          timeout: 5000,
          validateStatus: (status) => status >= 200 && status < 300, // Accept all 2xx responses
        });

        if (response.status >= 200 && response.status < 300) {
          this.logger.info?.("BackendService", "Backend health check passed", {
            attemptCount,
            statusCode: response.status,
            elapsedMs: Date.now() - startTime,
            healthData: response.data,
          });

          // Call progress callback with success
          if (onProgress) {
            onProgress({
              percent: 100,
              message: "Backend ready",
              phase: "complete",
            });
          }

          return true;
        }
      } catch (error) {
        lastError = error;
        consecutiveFailures++;

        // Log with more context on process state
        if (attemptCount === 1 || attemptCount % 10 === 0) {
          this.logger.debug?.(
            "BackendService",
            `Health check attempt ${attemptCount}/${maxAttempts}`,
            {
              error: error.message,
              errorCode: error.code,
              elapsedMs: Date.now() - startTime,
              processAlive: this.process && !this.process.killed,
              consecutiveFailures,
            }
          );
        }

        // Call progress callback
        if (onProgress) {
          const progress = Math.min(95, (attemptCount / maxAttempts) * 100);
          onProgress({
            percent: progress,
            message: `Waiting for backend (attempt ${attemptCount})...`,
            phase: "health-check",
          });
        }
      }

      // IMPROVED: Exponential backoff with cap
      // Use exponential backoff for first few attempts, then settle to 1 second intervals
      // This reduces load on the backend during startup while still checking frequently enough
      let delay = 1000; // Default 1 second
      if (consecutiveFailures < 5) {
        // First 5 failures: exponential backoff (500ms, 1s, 2s, 4s, 8s cap)
        delay = Math.min(500 * Math.pow(2, consecutiveFailures), 8000);
      }
      
      await new Promise((resolve) => setTimeout(resolve, delay));
      
      // Reset consecutive failures if we've had a successful attempt recently
      if (consecutiveFailures > 10) {
        consecutiveFailures = Math.max(5, consecutiveFailures - 1);
      }
    }

    // Timeout reached - provide detailed diagnostics
    const processAlive = this.process && !this.process.killed;
    const startupOutput = this.startupOutputLines?.join("\n") || "(no output captured)";
    const errorOutput = this.errorOutputLines?.join("\n") || "(no errors captured)";
    
    this.logger.error?.("BackendService", "Backend health check timeout", {
      attemptCount,
      timeoutMs: timeout,
      lastError: lastError?.message,
      lastErrorCode: lastError?.code,
      processRunning: processAlive,
      startupOutputPreview: startupOutput.substring(0, 500),
      errorOutputPreview: errorOutput.substring(0, 500),
    });

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

    let lastError = null;
    let consecutiveFailures = 0;

    for (let i = 0; i < maxAttempts; i++) {
      // Check if process has actually exited (not just if kill was called)
      if (
        this.process &&
        (this.process.exitCode !== null || this.process.signalCode !== null)
      ) {
        const startupOutput =
          this.startupOutputLines.join("\n") || "(no output captured)";
        const errorOutput =
          this.errorOutputLines.join("\n") || "(no errors captured)";
        const errorMessage =
          `Backend process exited during startup (exitCode: ${this.process.exitCode}, signal: ${this.process.signalCode}).\n` +
          `Startup output: ${startupOutput}\n` +
          `Error output: ${errorOutput}`;
        throw new Error(errorMessage);
      }

      try {
        const response = await axios.get(readinessUrl, {
          timeout: 2000,
          validateStatus: () => true, // Accept any status code
        });

        // IMPROVED: Accept any 2xx status code, not just 200
        // During startup, backend may return 204 or other success codes
        if (response.status >= 200 && response.status < 300) {
          console.log(`[Backend] Backend is healthy at ${this.baseUrl} (status: ${response.status})`);
          return true;
        } else if (response.status >= 500 && response.status < 600) {
          // 5xx errors indicate backend is running but having internal issues
          console.warn(
            `[Backend] Backend returned server error ${response.status} - may still be initializing`
          );
          consecutiveFailures++;
        } else {
          console.log(
            `[Backend] Health check returned status ${response.status}`
          );
        }
      } catch (error) {
        lastError = error;
        consecutiveFailures++;
        
        // Backend not ready yet, continue waiting
        if (error.code === "ECONNREFUSED") {
          // Connection refused - backend not listening yet (most common during startup)
          if (i % 10 === 0 && i > 0) {
            console.log(
              `[Backend] Still waiting for backend to start listening (attempt ${i + 1}/${maxAttempts})`
            );
          }
        } else if (error.code === "ETIMEDOUT") {
          console.log(
            `[Backend] Health check timeout (attempt ${i + 1}/${maxAttempts})`
          );
        } else {
          console.log(`[Backend] Health check error: ${error.message} (code: ${error.code || 'none'})`);
        }
      }

      // IMPROVED: Exponential backoff for first few attempts, then normal interval
      let delay = this.HEALTH_CHECK_INTERVAL;
      if (consecutiveFailures < 5) {
        // First 5 failures: shorter delays with exponential backoff
        // This allows faster detection when backend starts quickly
        delay = Math.min(500 * Math.pow(1.5, consecutiveFailures), this.HEALTH_CHECK_INTERVAL);
      }

      // Wait before next attempt
      await new Promise((resolve) =>
        setTimeout(resolve, delay)
      );

      if (i % 10 === 0 && i > 0) {
        console.log(
          `[Backend] Still waiting for backend... (attempt ${i}/${maxAttempts})`
        );
      }
    }

    // Provide detailed error message with classification
    const startupOutput =
      this.startupOutputLines?.join("\n") || "(no output captured)";
    const errorOutput =
      this.errorOutputLines?.join("\n") || "(no errors captured)";
    const processRunning =
      this.process &&
      this.process.exitCode === null &&
      this.process.signalCode === null;

    // Classify the failure type for better error messages
    let failureType = "TIMEOUT";
    let userGuidance = "The backend took too long to start.";

    if (!processRunning) {
      failureType = "PROCESS_EXITED";
      userGuidance = "The backend process exited unexpectedly during startup.";
    } else if (lastError && lastError.code === "ECONNREFUSED") {
      failureType = "BINDING_FAILED";
      userGuidance = "The backend process is running but failed to bind to the port.";
    } else if (lastError && lastError.code === "ETIMEDOUT") {
      failureType = "HEALTH_CHECK_TIMEOUT";
      userGuidance = "The backend is not responding to health checks.";
    }

    // Build error message with clear sections
    const logsPath = path.join(this.app.getPath("userData"), "logs");
    const errorMessage =
      `Backend startup failed (${failureType})\n\n` +
      `${userGuidance}\n\n` +
      `Technical Details:\n` +
      `- Health check URL: ${readinessUrl}\n` +
      `- Timeout: ${this.BACKEND_STARTUP_TIMEOUT}ms\n` +
      `- Last error: ${lastError ? lastError.message + (lastError.code ? ` (${lastError.code})` : '') : "None"}\n` +
      `- Process running: ${processRunning}\n` +
      `- Process PID: ${this.process?.pid || "N/A"}\n\n` +
      `Startup output (last 500 chars):\n${startupOutput.slice(-500)}\n\n` +
      `Error output (last 500 chars):\n${errorOutput.slice(-500)}\n\n` +
      `Troubleshooting:\n` +
      `1. Check if port ${this.port} is available\n` +
      `2. Verify .NET 8.0 runtime is installed\n` +
      `3. Check Windows Firewall settings\n` +
      `4. Review logs in: ${logsPath}`;

    throw new Error(errorMessage);
  }

  /**
   * Get the path to the backend executable
   */
  _getBackendPath() {
    // Check production bundle location FIRST
    const productionPath = path.join(
      process.resourcesPath || "",
      "backend",
      "win-x64",
      "Aura.Api.exe"
    );
    if (fs.existsSync(productionPath)) {
      console.log(
        `[BackendService] Found production backend at: ${productionPath}`
      );
      return productionPath;
    }

    // Then check development locations
    const devPaths = [
      path.join(__dirname, "..", "..", "dist", "backend", "Aura.Api.exe"),
      path.join(
        __dirname,
        "..",
        "..",
        "Aura.Api",
        "bin",
        "Release",
        "net8.0",
        "win-x64",
        "publish",
        "Aura.Api.exe"
      ),
      path.join(
        __dirname,
        "..",
        "..",
        "Aura.Api",
        "bin",
        "Debug",
        "net8.0",
        "Aura.Api.exe"
      ),
    ];

    for (const devPath of devPaths) {
      if (fs.existsSync(devPath)) {
        console.log(`[BackendService] Found dev backend at: ${devPath}`);
        return devPath;
      }
    }

    throw new Error(
      "Backend executable not found. Application may not be properly installed."
    );
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

      console.log(
        "[Backend] Persisting FFmpeg path to backend config:",
        ffmpegFullPath
      );

      // Validate FFmpeg executable exists
      if (!fs.existsSync(ffmpegFullPath)) {
        console.error(
          "[Backend] FFmpeg executable not found at:",
          ffmpegFullPath
        );
        return;
      }

      const response = await axios.post(
        `${this.baseUrl}/api/setup/configure-ffmpeg`,
        {
          path: ffmpegFullPath,
          source: "ElectronDetection",
        },
        {
          timeout: 5000,
          validateStatus: () => true,
        }
      );

      if (response.status === 200) {
        console.log(
          "[Backend] ✓ FFmpeg path persisted successfully to backend config"
        );
        console.log("[Backend] ✓ FFmpeg should now be detected in wizard");
      } else {
        console.warn(
          "[Backend] ⚠ Failed to persist FFmpeg path (status:",
          response.status,
          ")"
        );
        if (response.data) {
          console.warn("[Backend]   Response:", response.data);
        }
      }
    } catch (error) {
      console.warn("[Backend] ⚠ Error persisting FFmpeg path:", error.message);
      if (error.response) {
        console.warn("[Backend]   Response status:", error.response.status);
        console.warn("[Backend]   Response data:", error.response.data);
      }
    }
  }

  /**
   * Get platform-specific FFmpeg directory name
   */
  _getPlatformFFmpegDir() {
    const platform = process.platform;
    return platform === "win32"
      ? "win-x64"
      : platform === "darwin"
      ? "osx-x64"
      : "linux-x64";
  }

  /**
   * Prepare environment variables for backend
   */
  _prepareEnvironment() {
    // Determine FFmpeg path - check managed location first
    const platformDir = this._getPlatformFFmpegDir();

    const ffmpegPaths = [
      // Managed FFmpeg in resources (installed/portable mode)
      path.join(process.resourcesPath || "", "ffmpeg", platformDir, "bin"),
      // Development mode FFmpeg
      path.join(
        this.app.getAppPath(),
        "..",
        "Aura.Desktop",
        "resources",
        "ffmpeg",
        platformDir,
        "bin"
      ),
      // Bundled FFmpeg in app directory
      path.join(
        this.app.getAppPath(),
        "resources",
        "ffmpeg",
        platformDir,
        "bin"
      ),
    ];

    let ffmpegPath = null;
    const ffmpegExe = process.platform === "win32" ? "ffmpeg.exe" : "ffmpeg";

    console.log("[BackendService] Searching for FFmpeg in candidate paths:");
    for (const candidatePath of ffmpegPaths) {
      console.log("[BackendService]   Checking:", candidatePath);
      const ffmpegFullPath = path.join(candidatePath, ffmpegExe);
      if (fs.existsSync(ffmpegFullPath)) {
        ffmpegPath = candidatePath;
        console.log("[BackendService] ✓ Found FFmpeg at:", ffmpegPath);
        console.log("[BackendService] ✓ Full path:", ffmpegFullPath);
        break;
      } else {
        console.log("[BackendService] ✗ Not found at:", candidatePath);
      }
    }

    if (!ffmpegPath) {
      console.warn(
        "[BackendService] Managed FFmpeg not found in any expected location."
      );
      console.warn(
        "[BackendService] Backend will search system PATH for FFmpeg."
      );
      console.warn(
        "[BackendService] To install managed FFmpeg, run build-desktop.ps1"
      );
    }

    const env = {
      ...process.env,
      ASPNETCORE_URLS: this.baseUrl,
      DOTNET_ENVIRONMENT: this.isDev ? "Development" : "Production",
      ASPNETCORE_DETAILEDERRORS: this.isDev ? "true" : "false",
      LOGGING__LOGLEVEL__DEFAULT: this.isDev ? "Debug" : "Information",
      // Set paths for user data
      AURA_DATA_PATH: this.app.getPath("userData"),
      AURA_LOGS_PATH: path.join(this.app.getPath("userData"), "logs"),
      AURA_TEMP_PATH: path.join(this.app.getPath("temp"), "aura-video-studio"),
      // Set FFMPEG_PATH only if found (full path to ffmpeg.exe, not just bin directory)
      ...(ffmpegPath && {
        FFMPEG_PATH: path.join(ffmpegPath, ffmpegExe),
        AURA_FFMPEG_PATH: path.join(ffmpegPath, ffmpegExe),
        FFMPEG_BINARIES_PATH: ffmpegPath,
      }),
    };

    console.log("[BackendService] Environment variables configured:");
    console.log("[BackendService]   ASPNETCORE_URLS:", env.ASPNETCORE_URLS);
    console.log(
      "[BackendService]   DOTNET_ENVIRONMENT:",
      env.DOTNET_ENVIRONMENT
    );
    console.log("[BackendService]   AURA_DATA_PATH:", env.AURA_DATA_PATH);
    console.log(
      "[BackendService]   FFMPEG_PATH:",
      env.FFMPEG_PATH || "(not set)"
    );

    return env;
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
    // Track startup output for diagnostics using arrays for efficiency
    this.startupOutputLines = [];
    this.errorOutputLines = [];
    this.startupOutputSize = 0;
    this.errorOutputSize = 0;
    const MAX_OUTPUT_SIZE = 5000; // 5KB limit

    // Handle backend output
    this.process.stdout.on("data", (data) => {
      const message = data.toString().trim();
      if (message) {
        console.log(`[Backend] ${message}`);

        // Capture startup output for diagnostics (first 5KB only)
        // Track size incrementally for O(1) performance
        if (this.startupOutputSize < MAX_OUTPUT_SIZE) {
          this.startupOutputLines.push(message);
          this.startupOutputSize += message.length + 1; // +1 for newline
        }

        // Log important startup messages
        if (
          message.includes("Now listening on:") ||
          message.includes("Application started") ||
          message.includes("Content root path:")
        ) {
          console.log("[Backend] Startup message detected:", message);
        }
      }
    });

    this.process.stderr.on("data", (data) => {
      const message = data.toString().trim();
      if (message) {
        console.error(`[Backend Error] ${message}`);

        // Capture error output for diagnostics (first 5KB only)
        // Track size incrementally for O(1) performance
        if (this.errorOutputSize < MAX_OUTPUT_SIZE) {
          this.errorOutputLines.push(message);
          this.errorOutputSize += message.length + 1; // +1 for newline
        }
      }
    });

    // Handle backend exit
    this.process.on("exit", (code, signal) => {
      console.log(
        `[Backend] Process exited with code ${code} and signal ${signal}`
      );

      // Log captured output if backend failed during startup
      if (code !== 0 && code !== null) {
        const startupOutput =
          this.startupOutputLines.join("\n") || "(no output captured)";
        const errorOutput =
          this.errorOutputLines.join("\n") || "(no errors captured)";
        console.error("[Backend] Startup output:", startupOutput);
        console.error("[Backend] Error output:", errorOutput);
      }

      // Don't restart if we're intentionally quitting or already restarting
      if (!this.isQuitting && !this.isRestarting && code !== 0) {
        // Backend crashed unexpectedly
        console.error("[Backend] Backend crashed unexpectedly!");

        // Attempt auto-restart
        if (this.restartAttempts < this.maxRestartAttempts) {
          this.restartAttempts++;
          console.log(
            `[Backend] Attempting to restart backend (${this.restartAttempts}/${this.maxRestartAttempts})...`
          );

          setTimeout(() => {
            this.restart().catch((error) => {
              console.error("[Backend] Failed to restart backend:", error);
            });
          }, this.AUTO_RESTART_DELAY);
        } else {
          console.error(
            "[Backend] Max restart attempts reached. Backend will not auto-restart."
          );
          // Emit event for main process to handle
          if (this.app) {
            this.app.emit("backend-crash");
          }
        }
      }
    });

    this.process.on("error", (error) => {
      console.error("[Backend] Backend process error:", error);
      console.error("[Backend] Error details:", {
        message: error.message,
        code: error.code,
        errno: error.errno,
        syscall: error.syscall,
        path: error.path,
      });
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
    console.log(
      `[OrphanDetection] Checking for orphaned backend on port ${this.port}...`
    );

    let found = 0;
    let terminated = 0;
    let failed = 0;

    // Check if port is already in use by trying to connect
    const portInUse = await this._isPortInUse(this.port);

    if (!portInUse) {
      console.log("[OrphanDetection] Port is available, no cleanup needed");
      console.log(
        `[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${failed} failed`
      );
      return;
    }

    console.warn(
      `[OrphanDetection] Port ${this.port} is already in use, attempting cleanup...`
    );
    found = 1; // At least one process using the port

    // Try to find and kill orphaned Aura.Api processes
    const cleanupResult = await this._killOrphanedBackendProcesses();
    terminated = cleanupResult.terminated;
    failed = cleanupResult.failed;

    // Wait a moment for port to be released
    await new Promise((resolve) => setTimeout(resolve, 1000));

    // Verify port is now available
    const stillInUse = await this._isPortInUse(this.port);
    if (stillInUse) {
      console.error(
        `[OrphanDetection] Failed to cleanup port ${this.port}. Manual intervention may be required.`
      );
      console.log(
        `[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${
          failed + 1
        } failed`
      );
      throw new Error(
        `Port ${this.port} is still in use after cleanup. Please close any running Aura processes in Task Manager.`
      );
    }

    console.log("[OrphanDetection] Cleanup successful, port is now available");
    console.log(
      `[OrphanDetection] Orphan cleanup: ${found} found, ${terminated} terminated, ${failed} failed`
    );
  }

  /**
   * Check if a port is in use by attempting to connect
   */
  async _isPortInUse(port) {
    return new Promise((resolve) => {
      const testSocket = net.createConnection({ port, host: "127.0.0.1" });

      testSocket.on("connect", () => {
        testSocket.end();
        resolve(true); // Port is in use
      });

      testSocket.on("error", (err) => {
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
          'taskkill /F /IM "ffmpeg.exe" /FI "WINDOWTITLE eq Aura*" 2>nul',
        ];

        let completed = 0;
        commands.forEach((cmd) => {
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
            console.log("[OrphanDetection] Killed orphaned backend processes");
            terminated++;
          }

          exec('pkill -9 -f "ffmpeg.*aura"', (error2) => {
            if (!error2) {
              console.log("[OrphanDetection] Killed orphaned FFmpeg processes");
              terminated++;
            }

            resolve({ terminated, failed });
          });
        });
      }
    });
  }

  /**
   * Validate .NET runtime availability
   * @returns {Promise<{available: boolean, version?: string, error?: string}>}
   */
  async _validateDotnetRuntime() {
    return new Promise((resolve) => {
      exec("dotnet --version", { timeout: 5000 }, (error, stdout, stderr) => {
        if (error) {
          resolve({
            available: false,
            error: `.NET runtime not detected. Error: ${error.message}`,
          });
          return;
        }

        const version = stdout.trim();
        // Check if version is 8.x.x or higher
        const versionMatch = version.match(/^(\d+)\.(\d+)/);
        if (!versionMatch) {
          resolve({
            available: false,
            error: `Unable to parse .NET version: ${version}`,
          });
          return;
        }

        const majorVersion = parseInt(versionMatch[1], 10);
        if (majorVersion < 8) {
          resolve({
            available: false,
            error: `Incompatible .NET version: ${version}. Required: 8.0 or higher`,
          });
          return;
        }

        resolve({
          available: true,
          version: version,
        });
      });
    });
  }

  /**
   * Validate backend executable
   * @param {string} backendPath - Path to backend executable
   * @returns {{valid: boolean, error?: string, suggestion?: string}}
   */
  _validateBackendExecutable(backendPath) {
    // Check if file exists
    if (!fs.existsSync(backendPath)) {
      return {
        valid: false,
        error: "Backend executable file not found",
        suggestion:
          "The application may not be properly installed. Try reinstalling Aura Video Studio.",
      };
    }

    // Check if it's a file (not directory)
    const stats = fs.statSync(backendPath);
    if (!stats.isFile()) {
      return {
        valid: false,
        error: "Backend path is not a file",
        suggestion: `Expected a file but found ${stats.isDirectory() ? "a directory" : "something else"} at: ${backendPath}`,
      };
    }

    // Check file permissions on Unix
    if (process.platform !== "win32") {
      try {
        fs.accessSync(backendPath, fs.constants.X_OK);
      } catch (err) {
        return {
          valid: false,
          error: "Backend executable lacks execute permissions",
          suggestion: `Run: chmod +x "${backendPath}"`,
        };
      }
    }

    // Check file size (should be > 1KB)
    if (stats.size < 1024) {
      return {
        valid: false,
        error: "Backend executable file is too small (possibly corrupted)",
        suggestion: "Try reinstalling Aura Video Studio.",
      };
    }

    return { valid: true };
  }

  /**
   * Check if a port is available
   * @param {number} port - Port to check
   * @returns {Promise<{available: boolean, conflictInfo?: string}>}
   */
  async _checkPortAvailability(port) {
    return new Promise((resolve) => {
      const testServer = net.createServer();

      testServer.once("error", (err) => {
        if (err.code === "EADDRINUSE") {
          // Port is in use - try to identify what's using it
          this._identifyPortUser(port).then((info) => {
            resolve({
              available: false,
              conflictInfo: info || `Another application is using port ${port}`,
            });
          });
        } else {
          resolve({
            available: false,
            conflictInfo: `Port check failed: ${err.message}`,
          });
        }
      });

      testServer.once("listening", () => {
        testServer.close(() => {
          resolve({ available: true });
        });
      });

      testServer.listen(port, "127.0.0.1");
    });
  }

  /**
   * Try to identify what process is using a port
   * @param {number} port - Port number
   * @returns {Promise<string>}
   */
  async _identifyPortUser(port) {
    return new Promise((resolve) => {
      if (this.isWindows) {
        exec(`netstat -ano | findstr :${port}`, (error, stdout) => {
          if (error || !stdout) {
            resolve(null);
            return;
          }

          const lines = stdout.trim().split("\n");
          const pidMatch = lines[0]?.match(/LISTENING\s+(\d+)/);
          if (pidMatch) {
            const pid = pidMatch[1];
            exec(
              `tasklist /FI "PID eq ${pid}" /FO CSV /NH`,
              (err2, stdout2) => {
                if (!err2 && stdout2) {
                  const processName = stdout2.split(",")[0]?.replace(/"/g, "");
                  resolve(
                    `Port ${port} is in use by: ${processName} (PID: ${pid})`
                  );
                } else {
                  resolve(`Port ${port} is in use by process PID: ${pid}`);
                }
              }
            );
          } else {
            resolve(null);
          }
        });
      } else {
        exec(`lsof -i :${port} -sTCP:LISTEN`, (error, stdout) => {
          if (error || !stdout) {
            resolve(null);
            return;
          }

          const lines = stdout.trim().split("\n");
          if (lines.length > 1) {
            const parts = lines[1].split(/\s+/);
            const processName = parts[0];
            const pid = parts[1];
            resolve(`Port ${port} is in use by: ${processName} (PID: ${pid})`);
          } else {
            resolve(null);
          }
        });
      }
    });
  }

  /**
   * Get expected backend path for error messages
   * @returns {string}
   */
  _getExpectedBackendPath() {
    const productionPath = path.join(
      process.resourcesPath || "(unknown)",
      "backend",
      "win-x64",
      "Aura.Api.exe"
    );
    return productionPath;
  }
}

module.exports = BackendService;
