/**
 * OpenCut Manager
 * 
 * Ensures the OpenCut (CapCut-style) web app is running whenever
 * the Aura Electron shell is running.
 *
 * For development, it runs the Next.js dev server from the repo.
 * For packaged builds (Aura Video Studio-1.0.0-x64.exe), it runs the
 * production server from the bundled copy under resources/opencut.
 */

const { spawn } = require("child_process");
const path = require("path");
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
    this.port = process.env.OPENCUT_PORT || 3100;
    this.isPackaged = app?.isPackaged ?? false;
    this.enabled = true;
  }

  /**
   * Start the OpenCut dev server if enabled.
   * This is a best‑effort helper; failures are logged but do not block Aura.
   */
  start() {
    if (!this.enabled) {
      this.logger.info?.("OpenCutManager", "Auto‑start disabled; skipping OpenCut server.");
      return;
    }

    if (this.child) {
      this.logger.info?.("OpenCutManager", "OpenCut server already running, skipping start.");
      return;
    }

    try {
      const openCutAppDir = this.isPackaged
        ? // Packaged app: OpenCut is bundled under resources/opencut
          path.join(process.resourcesPath, "opencut")
        : // Dev: run from the repo layout
          path.resolve(__dirname, "..", "..", "OpenCut", "apps", "web");

      const mode = this.isPackaged ? "production" : "development";

      this.logger.info?.("OpenCutManager", "Starting OpenCut server...", {
        dir: openCutAppDir,
        port: this.port,
        mode,
      });

      // OpenCut uses bun as its package manager; callers can override via OPENCUT_COMMAND if needed.
      const command = process.env.OPENCUT_COMMAND || "bun";
      const args = process.env.OPENCUT_COMMAND_ARGS
        ? process.env.OPENCUT_COMMAND_ARGS.split(" ")
        : this.isPackaged
          ? ["run", "start"]
          : ["run", "dev"];

      const child = spawn(command, args, {
        cwd: openCutAppDir,
        env: {
          ...process.env,
          PORT: String(this.port),
        },
        stdio: "pipe",
      });

      this.child = child;
      this.processManager?.register("OpenCutDevServer", child, {
        cwd: openCutAppDir,
        port: this.port,
        command: [command, ...args].join(" "),
      });

      child.stdout.on("data", (data) => {
        const text = data.toString();
        this.logger.info?.("OpenCutManager", "stdout", text.trim());
      });

      child.stderr.on("data", (data) => {
        const text = data.toString();
        this.logger.error?.("OpenCutManager", "stderr", text.trim());
      });

      child.on("error", (error) => {
        this.logger.error?.("OpenCutManager", "Failed to start OpenCut server", {
          message: error.message,
        });
      });

      child.on("exit", (code, signal) => {
        this.logger.info?.("OpenCutManager", "OpenCut server exited", { code, signal });
        this.child = null;
      });
    } catch (error) {
      this.logger.error?.("OpenCutManager", "Unexpected error starting OpenCut server", {
        message: error.message,
      });
    }
  }

  /**
   * Returns the URL where OpenCut is expected to be available.
   */
  getUrl() {
    const host = process.env.OPENCUT_HOST || "http://127.0.0.1";
    return `${host}:${this.port}`;
  }
}

module.exports = OpenCutManager;


