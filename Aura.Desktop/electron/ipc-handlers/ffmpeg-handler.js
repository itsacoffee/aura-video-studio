/**
 * FFmpeg IPC Handlers
 * Delegates installation to the backend while providing local diagnostics.
 */

const { ipcMain, shell } = require("electron");
const { spawnSync } = require("child_process");
const path = require("path");
const fs = require("fs");
const os = require("os");
const axios = require("axios");

class FFmpegHandler {
  constructor(app, windowManager, backendUrl = null) {
    this.app = app;
    this.windowManager = windowManager;
    this.backendUrl = backendUrl;
    this.downloadProgress = 0;
    this.isInstalling = false;
  }

  /**
   * Register all FFmpeg IPC handlers
   */
  register() {
    ipcMain.handle("ffmpeg:checkStatus", async () => this._checkFFmpegStatus());
    ipcMain.handle("ffmpeg:install", async (_event, options = {}) =>
      this._installViaBackend(options)
    );
    ipcMain.handle("ffmpeg:getProgress", async () =>
      this._getBackendInstallStatus()
    );
    ipcMain.handle("ffmpeg:openDirectory", async () =>
      this._openDetectedDirectory()
    );

    console.log("FFmpeg IPC handlers registered");
  }

  /**
   * Check FFmpeg installation status
   * First tries backend API for consistent detection, then falls back to local checks
   */
  async _checkFFmpegStatus() {
    // Try to get status from backend first for consistent detection
    if (this.backendUrl) {
      try {
        const backendResponse = await axios.get(`${this.backendUrl}/api/ffmpeg/status`, {
          timeout: 5000 // 5 second timeout
        });
        
        if (backendResponse.data?.installed) {
          console.log('[FFmpeg] Using backend detection result:', backendResponse.data.path);
          return {
            installed: backendResponse.data.installed,
            version: backendResponse.data.version,
            path: backendResponse.data.path,
            source: backendResponse.data.source || 'Backend',
            binaries: {
              ffmpeg: true,
              ffprobe: true, // Assume ffprobe is available with ffmpeg
            },
          };
        }
      } catch (error) {
        console.warn('[FFmpeg] Backend status check failed, using local detection:', error.message);
      }
    }
    
    // Fallback to local detection
    const sources = [
      { label: "Bundled", resolver: () => this._resolveBundledBinary() },
      { label: "Managed", resolver: () => this._resolveManagedBinary() },
      { label: "PATH", resolver: () => this._resolvePathBinary() },
    ];

    for (const source of sources) {
      const candidate = source.resolver();
      if (!candidate || !candidate.ffmpeg) {
        continue;
      }

      const versionInfo = this._getVersionInfo(candidate.ffmpeg);
      if (!versionInfo) {
        continue;
      }

      return {
        installed: true,
        version: versionInfo.version,
        path: candidate.ffmpeg,
        source: source.label,
        binaries: {
          ffmpeg: true,
          ffprobe: Boolean(
            candidate.ffprobe && fs.existsSync(candidate.ffprobe)
          ),
        },
      };
    }

    return {
      installed: false,
      version: null,
      path: null,
      source: "unknown",
      binaries: {
        ffmpeg: false,
        ffprobe: false,
      },
    };
  }

  /**
   * Attempt to open the detected FFmpeg directory
   */
  async _openDetectedDirectory() {
    const status = await this._checkFFmpegStatus();
    if (!status.path) {
      throw new Error("FFmpeg directory not found");
    }

    const stats = fs.existsSync(status.path) ? fs.statSync(status.path) : null;
    const directory =
      stats && stats.isDirectory() ? status.path : path.dirname(status.path);

    if (!directory || !fs.existsSync(directory)) {
      throw new Error("FFmpeg directory not found");
    }

    await shell.openPath(directory);
    return { success: true };
  }

  /**
   * Delegate installation to backend API
   */
  async _installViaBackend(options = {}) {
    if (!this.backendUrl) {
      throw new Error(
        "Backend URL not available. Start the backend before installing FFmpeg."
      );
    }

    try {
      this.isInstalling = true;
      const response = await axios.post(
        `${this.backendUrl}/api/ffmpeg/install`,
        options,
        {
          timeout: 5 * 60 * 1000,
        }
      );
      return response.data;
    } catch (error) {
      const message = error?.response?.data?.message || error.message;
      throw new Error(`Backend FFmpeg installation failed: ${message}`);
    } finally {
      this.isInstalling = false;
    }
  }

  /**
   * Poll backend status endpoint for installation progress
   */
  async _getBackendInstallStatus() {
    if (!this.backendUrl) {
      return {
        progress: this.downloadProgress,
        isInstalling: this.isInstalling,
        state: "unknown",
      };
    }

    try {
      const response = await axios.get(
        `${this.backendUrl}/api/downloads/ffmpeg/status`,
        {
          timeout: 5000,
        }
      );

      const state =
        typeof response.data?.state === "string"
          ? response.data.state
          : "unknown";

      return {
        progress: response.data?.progressPercent ?? null,
        isInstalling: ["Installing", "Downloading"].includes(state),
        state,
        details: response.data,
      };
    } catch (error) {
      return {
        progress: this.downloadProgress,
        isInstalling: this.isInstalling,
        state: "unknown",
        error: error.message,
      };
    }
  }

  _resolveBundledBinary() {
    const bundledPath = this._getBundledBinaryDirectory();
    if (!bundledPath) {
      return null;
    }

    return this._resolveCandidate(bundledPath);
  }

  _resolveManagedBinary() {
    const managedRoot = this._getManagedInstallRoot();
    if (!fs.existsSync(managedRoot)) {
      return null;
    }

    const entries = fs
      .readdirSync(managedRoot)
      .map((entry) => path.join(managedRoot, entry))
      .filter((entryPath) => {
        try {
          return fs.statSync(entryPath).isDirectory();
        } catch {
          return false;
        }
      })
      .sort()
      .reverse();

    for (const versionDir of entries) {
      const manifestPath = path.join(versionDir, "install.json");
      if (fs.existsSync(manifestPath)) {
        try {
          const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
          const ffmpegPath = manifest.ffmpegPath || manifest.FfmpegPath;
          const ffprobePath = manifest.ffprobePath || manifest.FfprobePath;

          if (ffmpegPath && fs.existsSync(ffmpegPath)) {
            return {
              ffmpeg: ffmpegPath,
              ffprobe:
                ffprobePath && fs.existsSync(ffprobePath) ? ffprobePath : null,
            };
          }
        } catch (error) {
          console.warn(
            "[FFmpeg] Failed to parse managed install manifest:",
            error.message
          );
        }
      }

      const fallback = this._resolveCandidate(path.join(versionDir, "bin"));
      if (fallback) {
        return fallback;
      }
    }

    return null;
  }

  _resolvePathBinary() {
    const pathBinary = this._findBinaryOnPath();
    if (!pathBinary) {
      return null;
    }

    const ffprobeGuess =
      process.platform === "win32"
        ? path.join(path.dirname(pathBinary), "ffprobe.exe")
        : path.join(path.dirname(pathBinary), "ffprobe");

    return {
      ffmpeg: pathBinary,
      ffprobe: fs.existsSync(ffprobeGuess) ? ffprobeGuess : null,
    };
  }

  _resolveCandidate(candidatePath) {
    if (!candidatePath) {
      return null;
    }

    const ffmpegBinary = this._resolveBinary(candidatePath, "ffmpeg");
    if (!ffmpegBinary) {
      return null;
    }

    const ffprobeBinary = this._resolveBinary(candidatePath, "ffprobe");

    return {
      ffmpeg: ffmpegBinary,
      ffprobe: ffprobeBinary,
    };
  }

  _resolveBinary(basePath, binaryName) {
    const executable =
      process.platform === "win32" ? `${binaryName}.exe` : binaryName;
    const candidates = [];

    try {
      const stat = fs.existsSync(basePath) ? fs.statSync(basePath) : null;
      if (!stat) {
        return null;
      }

      if (stat.isFile()) {
        if (basePath.toLowerCase().endsWith(executable.toLowerCase())) {
          candidates.push(basePath);
        }
      } else if (stat.isDirectory()) {
        candidates.push(path.join(basePath, executable));
        candidates.push(path.join(basePath, "bin", executable));
      }
    } catch {
      return null;
    }

    for (const candidate of candidates) {
      if (candidate && fs.existsSync(candidate)) {
        try {
          if (fs.statSync(candidate).isFile()) {
            return candidate;
          }
        } catch {
          // Ignore inaccessible files
        }
      }
    }

    return null;
  }

  _findBinaryOnPath() {
    const executable = "ffmpeg";
    const locatorCommand = process.platform === "win32" ? "where" : "which";

    try {
      const result = spawnSync(locatorCommand, [executable], {
        encoding: "utf8",
        timeout: 3000,
      });

      if (result.status !== 0 || !result.stdout) {
        return null;
      }

      const lines = result.stdout
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter(Boolean);

      for (const line of lines) {
        if (fs.existsSync(line)) {
          return line;
        }
      }
    } catch (error) {
      console.warn("[FFmpeg] Failed to locate ffmpeg on PATH:", error.message);
    }

    return null;
  }

  _getVersionInfo(ffmpegPath) {
    try {
      const result = spawnSync(ffmpegPath, ["-version"], {
        encoding: "utf8",
        timeout: 3000,
      });

      if (result.status !== 0 || !result.stdout) {
        return null;
      }

      const match = result.stdout.match(/ffmpeg version ([^\s]+)/i);
      if (!match) {
        return null;
      }

      return {
        version: match[1],
        raw: result.stdout,
      };
    } catch (error) {
      console.warn(
        "[FFmpeg] Failed to execute ffmpeg -version:",
        error.message
      );
      return null;
    }
  }

  _getBundledBinaryDirectory() {
    if (this.app.isPackaged) {
      return path.join(process.resourcesPath, "ffmpeg", "win-x64", "bin");
    }

    const desktopPath = path.join(__dirname, "../..");
    return path.join(desktopPath, "resources", "ffmpeg", "win-x64", "bin");
  }

  _getManagedInstallRoot() {
    const localAppData = this._getLocalAppData();
    return path.join(localAppData, "Aura", "Tools", "ffmpeg");
  }

  _getLocalAppData() {
    if (process.platform === "win32" && process.env.LOCALAPPDATA) {
      return process.env.LOCALAPPDATA;
    }

    if (process.platform === "darwin") {
      return path.join(os.homedir(), "Library", "Application Support");
    }

    return path.join(os.homedir(), ".local", "share");
  }
}

module.exports = FFmpegHandler;
