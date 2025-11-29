/**
 * OpenCut Manager
 * 
 * Ensures the OpenCut (CapCut-style) web app is running whenever
 * the Aura Electron shell is running.
 *
 * For development, it runs the Next.js dev server from the repo.
 * For packaged builds (Aura Video Studio-1.0.0-x64.exe), it runs the
 * standalone Next.js server from the bundled copy under resources/opencut.
 */

const { spawn, execSync } = require("child_process");
const path = require("path");
const fs = require("fs");
const http = require("http");
const { app } = require("electron");

class OpenCutManager {
  /**
   * @param {object} options
   * @param {import('./process-manager')} options.processManager
   * @param {object} options.logger
   */
  constructor({ processManager, logger }) {
    this.processManager = processManager;
    this.logger = logger || console;
    this.child = null;
    this.port = parseInt(process.env.OPENCUT_PORT || "3100", 10);
    this.isPackaged = app?.isPackaged ?? false;
    this.enabled = true;
    this.startAttempts = 0;
    this.maxStartAttempts = 3;
    this.healthCheckInterval = null;
    this.isStarting = false;
  }

  /**
   * Check if a command is available in PATH
   * @param {string} cmd 
   * @returns {boolean}
   */
  _isCommandAvailable(cmd) {
    try {
      if (process.platform === "win32") {
        execSync(`where ${cmd}`, { stdio: "ignore" });
      } else {
        execSync(`which ${cmd}`, { stdio: "ignore" });
      }
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Find the best available package manager/runner for dev mode
   * @returns {{command: string, args: string[]} | null}
   */
  _findDevCommand() {
    // Priority: bun > npx > npm
    if (this._isCommandAvailable("bun")) {
      return { command: "bun", args: ["run", "dev"] };
    }
    if (this._isCommandAvailable("npx")) {
      return { command: "npx", args: ["next", "dev"] };
    }
    if (this._isCommandAvailable("npm")) {
      return { command: "npm", args: ["run", "dev"] };
    }
    return null;
  }

  /**
   * Check if something is already listening on the OpenCut port
   * @returns {Promise<boolean>} true if the port is in use (something is responding)
   */
  async _isPortInUse() {
    return new Promise((resolve) => {
      const req = http.get(`http://127.0.0.1:${this.port}/`, (res) => {
        // Any HTTP response means something is listening on the port
        resolve(true);
      });
      req.on("error", (err) => {
        // ECONNREFUSED: nothing is listening on this port (port is free)
        // ENOTFOUND: host not found (port is free)
        // Other errors might mean port is in use but unresponsive
        if (err.code === "ECONNREFUSED" || err.code === "ENOTFOUND") {
          resolve(false); // Port is free
        } else {
          // For other errors (like ETIMEDOUT), assume port might be in use
          resolve(true);
        }
      });
      req.setTimeout(2000, () => {
        req.destroy();
        resolve(false); // Timeout with no error means nothing responding - port is free
      });
    });
  }

  /**
   * Check if OpenCut server is healthy (responding with success)
   * @returns {Promise<boolean>} true if server responds with 2xx or 3xx
   */
  async _healthCheck() {
    return new Promise((resolve) => {
      const req = http.get(`http://127.0.0.1:${this.port}/`, (res) => {
        // Only 2xx and 3xx are considered healthy
        resolve(res.statusCode >= 200 && res.statusCode < 400);
      });
      req.on("error", () => resolve(false));
      req.setTimeout(2000, () => {
        req.destroy();
        resolve(false);
      });
    });
  }

  /**
   * Start the OpenCut dev server if enabled.
   * This is a best‑effort helper; failures are logged but do not block Aura.
   */
  async start() {
    if (!this.enabled) {
      this.logger.info?.("OpenCutManager", "Auto‑start disabled; skipping OpenCut server.");
      return;
    }

    if (this.child) {
      this.logger.info?.("OpenCutManager", "OpenCut server already running, skipping start.");
      return;
    }

    if (this.isStarting) {
      this.logger.info?.("OpenCutManager", "OpenCut server start already in progress.");
      return;
    }

    this.isStarting = true;

    try {
      const openCutAppDir = this.isPackaged
        ? // Packaged app: OpenCut standalone build is bundled under resources/opencut
          path.join(process.resourcesPath, "opencut")
        : // Dev: run from the repo layout
          path.resolve(__dirname, "..", "..", "OpenCut", "apps", "web");

      // Verify OpenCut directory exists
      if (!fs.existsSync(openCutAppDir)) {
        this.logger.warn?.("OpenCutManager", "OpenCut directory not found, skipping server start", {
          dir: openCutAppDir,
        });
        this.isStarting = false;
        return;
      }

      const mode = this.isPackaged ? "production" : "development";

      this.logger.info?.("OpenCutManager", "Starting OpenCut server...", {
        dir: openCutAppDir,
        port: this.port,
        mode,
        attempt: this.startAttempts + 1,
      });

      let command;
      let args;
      let cwd = openCutAppDir;

      if (this.isPackaged) {
        // Packaged mode: run the standalone Next.js server directly with Node
        // The standalone build creates a server.js that can be run with Node
        const standaloneServerPath = path.join(openCutAppDir, "server.js");
        
        if (!fs.existsSync(standaloneServerPath)) {
          this.logger.warn?.("OpenCutManager", "OpenCut standalone server.js not found, skipping", {
            expectedPath: standaloneServerPath,
          });
          this.isStarting = false;
          return;
        }
        
        command = process.execPath; // Use the bundled Node (from Electron)
        args = [standaloneServerPath];
      } else {
        // Dev mode: find available package manager
        if (process.env.OPENCUT_COMMAND) {
          command = process.env.OPENCUT_COMMAND;
          args = process.env.OPENCUT_COMMAND_ARGS
            ? process.env.OPENCUT_COMMAND_ARGS.split(" ")
            : ["run", "dev"];
        } else {
          const devCmd = this._findDevCommand();
          if (!devCmd) {
            this.logger.warn?.("OpenCutManager", "No package manager found (bun/npx/npm). Skipping OpenCut server.");
            this.isStarting = false;
            return;
          }
          command = devCmd.command;
          args = devCmd.args;
        }
      }

      // Ensure the port is not already in use
      const portInUse = await this._isPortInUse();
      if (portInUse) {
        this.logger.info?.("OpenCutManager", `Port ${this.port} already in use. OpenCut may already be running.`);
        this.isStarting = false;
        return;
      }

      const child = spawn(command, args, {
        cwd,
        env: {
          ...process.env,
          PORT: String(this.port),
          NODE_ENV: this.isPackaged ? "production" : "development",
          // Disable Next.js telemetry for privacy
          NEXT_TELEMETRY_DISABLED: "1",
        },
        stdio: "pipe",
        // On Windows, use shell for npm/npx commands
        shell: process.platform === "win32" && (command === "npm" || command === "npx"),
      });

      this.child = child;
      this.startAttempts++;
      
      this.processManager?.register("OpenCutDevServer", child, {
        cwd,
        port: this.port,
        command: [command, ...args].join(" "),
      });

      child.stdout.on("data", (data) => {
        const text = data.toString();
        this.logger.info?.("OpenCutManager", "stdout", text.trim());
      });

      child.stderr.on("data", (data) => {
        const text = data.toString();
        // Next.js often writes info to stderr, so we log it as info unless it looks like an error
        if (text.toLowerCase().includes("error") || text.toLowerCase().includes("failed")) {
          this.logger.error?.("OpenCutManager", "stderr", text.trim());
        } else {
          this.logger.info?.("OpenCutManager", "stderr", text.trim());
        }
      });

      child.on("error", (error) => {
        this.logger.error?.("OpenCutManager", "Failed to start OpenCut server", {
          message: error.message,
        });
        this.child = null;
        this.isStarting = false;
        
        // Retry if we haven't exceeded max attempts
        if (this.startAttempts < this.maxStartAttempts) {
          this.logger.info?.("OpenCutManager", `Retrying start in 3 seconds (attempt ${this.startAttempts + 1}/${this.maxStartAttempts})...`);
          setTimeout(() => this.start(), 3000);
        }
      });

      child.on("exit", (code, signal) => {
        this.logger.info?.("OpenCutManager", "OpenCut server exited", { code, signal });
        this.child = null;
        this.isStarting = false;
        this._stopHealthCheck();
        
        // If it exited unexpectedly and we haven't exceeded max attempts, retry
        if (code !== 0 && signal !== "SIGTERM" && signal !== "SIGKILL" && this.startAttempts < this.maxStartAttempts) {
          this.logger.info?.("OpenCutManager", `Server crashed. Retrying in 3 seconds (attempt ${this.startAttempts + 1}/${this.maxStartAttempts})...`);
          setTimeout(() => this.start(), 3000);
        }
      });

      // Start health check after a delay to give server time to start
      setTimeout(() => {
        this._startHealthCheck();
      }, 5000);

      this.isStarting = false;
    } catch (error) {
      this.logger.error?.("OpenCutManager", "Unexpected error starting OpenCut server", {
        message: error.message,
      });
      this.isStarting = false;
    }
  }

  /**
   * Start periodic health checks
   */
  _startHealthCheck() {
    if (this.healthCheckInterval) return;
    
    this.healthCheckInterval = setInterval(async () => {
      if (!this.child) return;
      
      const healthy = await this._healthCheck();
      if (!healthy) {
        this.logger.warn?.("OpenCutManager", "Health check failed - server may be unresponsive");
      }
    }, 30000); // Check every 30 seconds
  }

  /**
   * Stop periodic health checks
   */
  _stopHealthCheck() {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
      this.healthCheckInterval = null;
    }
  }

  /**
   * Stop the OpenCut server
   */
  stop() {
    this._stopHealthCheck();
    
    if (this.child) {
      this.logger.info?.("OpenCutManager", "Stopping OpenCut server...");
      this.child.kill("SIGTERM");
      this.child = null;
    }
  }

  /**
   * Check if OpenCut is available (either server is running or files exist)
   * @returns {boolean}
   */
  isAvailable() {
    if (this.child) return true;
    
    const openCutAppDir = this.isPackaged
      ? path.join(process.resourcesPath, "opencut")
      : path.resolve(__dirname, "..", "..", "OpenCut", "apps", "web");
    
    return fs.existsSync(openCutAppDir);
  }

  /**
   * Returns the URL where OpenCut is expected to be available.
   */
  getUrl() {
    const host = process.env.OPENCUT_HOST || "http://127.0.0.1";
    return `${host}:${this.port}`;
  }

  /**
   * Reset start attempts counter (useful after successful operation)
   */
  resetAttempts() {
    this.startAttempts = 0;
  }
}

module.exports = OpenCutManager;
