/**
 * Window Manager Module
 * Handles window creation, state persistence, and lifecycle management
 */

const { BrowserWindow, screen, nativeImage } = require("electron");
const path = require("path");
const fs = require("fs");
const Store = require("electron-store");
const { getFallbackIcon } = require("./icon-fallbacks");

class WindowManager {
  constructor(app, isDev) {
    this.app = app;
    this.isDev = isDev;
    this.mainWindow = null;
    this.splashWindow = null;
    this.allowedWebOrigins = new Set();

    // Window state persistence
    this.windowStateStore = new Store({
      name: "window-state",
      defaults: {
        mainWindow: {
          width: 1920,
          height: 1080,
          x: undefined,
          y: undefined,
          isMaximized: false,
        },
      },
    });
  }

  /**
   * Create splash screen window
   */
  createSplashWindow() {
    this.splashWindow = new BrowserWindow({
      width: 600,
      height: 400,
      transparent: true,
      frame: false,
      alwaysOnTop: true,
      center: true,
      resizable: false,
      skipTaskbar: true,
      webPreferences: {
        nodeIntegration: true,
        contextIsolation: false,
      },
    });

    // Try the new splash.html first in electron directory
    const newSplashPath = path.join(__dirname, "splash.html");
    const assetsSplashPath = path.join(__dirname, "../assets", "splash.html");

    if (fs.existsSync(newSplashPath)) {
      this.splashWindow.loadFile(newSplashPath);
    } else if (fs.existsSync(assetsSplashPath)) {
      this.splashWindow.loadFile(assetsSplashPath);
    } else {
      // Fallback HTML splash
      this.splashWindow.loadURL(`data:text/html,
        <html>
          <head>
            <style>
              * { margin: 0; padding: 0; box-sizing: border-box; }
              body {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                display: flex;
                align-items: center;
                justify-content: center;
                height: 100vh;
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                color: white;
              }
              .container {
                text-align: center;
                animation: fadeIn 0.5s ease-in;
              }
              h1 {
                font-size: 42px;
                font-weight: 700;
                margin-bottom: 20px;
                text-shadow: 0 2px 10px rgba(0,0,0,0.2);
              }
              .spinner {
                width: 50px;
                height: 50px;
                margin: 30px auto;
                border: 4px solid rgba(255,255,255,0.3);
                border-top: 4px solid white;
                border-radius: 50%;
                animation: spin 1s linear infinite;
              }
              @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
              }
              @keyframes fadeIn {
                from { opacity: 0; transform: translateY(20px); }
                to { opacity: 1; transform: translateY(0); }
              }
              p {
                font-size: 16px;
                opacity: 0.9;
              }
            </style>
          </head>
          <body>
            <div class="container">
              <h1>Aura Video Studio</h1>
              <div class="spinner"></div>
              <p>Starting up...</p>
            </div>
          </body>
        </html>
      `);
    }

    return this.splashWindow;
  }

  /**
   * Create main application window with state restoration
   */
  createMainWindow(preloadPath) {
    // Restore window state
    const savedState = this.windowStateStore.get("mainWindow");
    const primaryDisplay = screen.getPrimaryDisplay();
    const { height: screenHeight } = primaryDisplay.workAreaSize;

    // Validate saved position is still within screen bounds
    let x = savedState.x;
    let y = savedState.y;
    if (x !== undefined && y !== undefined) {
      const visible = screen.getAllDisplays().some((display) => {
        return (
          x >= display.bounds.x &&
          x < display.bounds.x + display.bounds.width &&
          y >= display.bounds.y &&
          y < display.bounds.y + display.bounds.height
        );
      });
      if (!visible) {
        x = undefined;
        y = undefined;
      }
    }

    // Track loading state for diagnostics
    this.loadingState = {
      startTime: null,
      didStartLoading: false,
      didFinishLoad: false,
      loadAttempts: 0,
      lastError: null,
      loadTimeout: null,
    };

    // Create window with saved or default state
    this.mainWindow = new BrowserWindow({
      width: savedState.width,
      height: savedState.height,
      minWidth: 1280,
      minHeight: 720,
      x: x,
      y: y,
      show: false, // Don't show until ready-to-show event
      backgroundColor: "#0F0F0F",
      icon: this._getAppIcon(),
      webPreferences: {
        preload: preloadPath,
        contextIsolation: true,
        nodeIntegration: false,
        sandbox: true,
        webSecurity: !this.isDev,
        devTools: true, // Allow devtools but don't open by default
        enableRemoteModule: false, // Remote module is deprecated
        spellcheck: true,
      },
      titleBarStyle: process.platform === "darwin" ? "hiddenInset" : "default",
      frame: true,
      autoHideMenuBar: false,
    });
    this.allowedWebOrigins = new Set();

    // Restore maximized state
    if (savedState.isMaximized) {
      this.mainWindow.maximize();
    }

    // Save window state on resize/move
    const saveWindowState = () => {
      if (this.mainWindow && !this.mainWindow.isDestroyed()) {
        const bounds = this.mainWindow.getBounds();
        const isMaximized = this.mainWindow.isMaximized();

        if (!isMaximized) {
          this.windowStateStore.set("mainWindow", {
            width: bounds.width,
            height: bounds.height,
            x: bounds.x,
            y: bounds.y,
            isMaximized: false,
          });
        } else {
          this.windowStateStore.set("mainWindow.isMaximized", true);
        }
      }
    };

    // Debounce window state saving
    let saveStateTimeout;
    const debouncedSave = () => {
      clearTimeout(saveStateTimeout);
      saveStateTimeout = setTimeout(saveWindowState, 500);
    };

    this.mainWindow.on("resize", debouncedSave);
    this.mainWindow.on("move", debouncedSave);
    this.mainWindow.on("maximize", saveWindowState);
    this.mainWindow.on("unmaximize", saveWindowState);

    // Set Content Security Policy
    this.mainWindow.webContents.session.webRequest.onHeadersReceived(
      (details, callback) => {
        callback({
          responseHeaders: {
            ...details.responseHeaders,
            "Content-Security-Policy": this._getCSP(),
          },
        });
      }
    );

    // Handle navigation - prevent navigation away from app
    this.mainWindow.webContents.on("will-navigate", (event, url) => {
      if (this._isAllowedNavigation(url)) {
        return;
      }
      event.preventDefault();
      console.warn("[WindowManager] Navigation blocked:", url);
    });

    // Prevent new window creation
    this.mainWindow.webContents.setWindowOpenHandler(({ url }) => {
      // Open in external browser
      require("electron").shell.openExternal(url);
      return { action: "deny" };
    });

    // Add detailed loading event handlers for diagnostics
    this.mainWindow.webContents.on("did-start-loading", () => {
      this.loadingState.startTime = Date.now();
      this.loadingState.didStartLoading = true;
      console.log(
        "[WindowManager] ✓ Loading started at:",
        new Date(this.loadingState.startTime).toISOString()
      );
    });

    this.mainWindow.webContents.on("did-finish-load", () => {
      this.loadingState.didFinishLoad = true;
      const duration = this.loadingState.startTime
        ? Date.now() - this.loadingState.startTime
        : 0;
      console.log(
        "[WindowManager] ✓ Loading finished successfully (duration: " +
          duration +
          "ms)"
      );

      // Clear timeout if loading succeeded
      if (this.loadingState.loadTimeout) {
        clearTimeout(this.loadingState.loadTimeout);
        this.loadingState.loadTimeout = null;
      }

      // Inject environment variables after successful load
      // Environment variables are now provided via the preload bridge
    });

    this.mainWindow.webContents.on(
      "did-fail-load",
      (event, errorCode, errorDescription, validatedURL, isMainFrame) => {
        console.error("[WindowManager] ✗ Failed to load page!");
        console.error("[WindowManager]   Error code:", errorCode);
        console.error("[WindowManager]   Error description:", errorDescription);
        console.error("[WindowManager]   URL:", validatedURL);
        console.error("[WindowManager]   Is main frame:", isMainFrame);
        console.error("[WindowManager]   Timestamp:", new Date().toISOString());

        if (isMainFrame) {
          console.error("[WindowManager] CRITICAL: Main frame failed to load!");

          this.loadingState.lastError = {
            errorCode,
            errorDescription,
            validatedURL,
            timestamp: new Date().toISOString(),
          };

          // Clear timeout on failure
          if (this.loadingState.loadTimeout) {
            clearTimeout(this.loadingState.loadTimeout);
            this.loadingState.loadTimeout = null;
          }

          // Attempt recovery if this was the first attempt
          if (this.loadingState.loadAttempts < 2) {
            console.log(
              "[WindowManager] Attempting recovery with fallback error page..."
            );
            this._loadErrorPage(errorCode, errorDescription, validatedURL);
          }
        }
      }
    );

    // Add console message handler to forward React console logs to Electron main
    this.mainWindow.webContents.on(
      "console-message",
      (event, level, message, line, sourceId) => {
        const levels = ["verbose", "info", "warning", "error"];
        const levelName = levels[level] || "log";
        const timestamp = new Date().toISOString();

        const logLine = `[Renderer:${levelName}] [${timestamp}] ${message}`;
        console.log(logLine);

        if (line && sourceId) {
          console.log(`[Renderer:${levelName}]   at ${sourceId}:${line}`);
        }
      }
    );

    // Add crashed event handler with recovery dialog
    this.mainWindow.webContents.on("crashed", (event, killed) => {
      console.error("[WindowManager] ✗ Renderer process crashed!");
      console.error("[WindowManager]   Killed:", killed);
      console.error("[WindowManager]   Timestamp:", new Date().toISOString());

      const { dialog } = require("electron");

      dialog
        .showMessageBox(this.mainWindow, {
          type: "error",
          title: "Renderer Process Crashed",
          message: "The application interface has crashed unexpectedly.",
          detail: `The renderer process ${
            killed ? "was killed" : "crashed"
          }. Would you like to reload the application?`,
          buttons: ["Reload", "Close Application"],
          defaultId: 0,
          cancelId: 1,
        })
        .then((result) => {
          if (result.response === 0) {
            console.log("[WindowManager] User chose to reload after crash");
            this._attemptLoad();
          } else {
            console.log(
              "[WindowManager] User chose to close application after crash"
            );
            this.app.quit();
          }
        });
    });

    // Attempt to load frontend with retry logic
    this._attemptLoad();

    // Show window when ready
    this.mainWindow.once("ready-to-show", () => {
      console.log("[WindowManager] Window ready to show");
      this.mainWindow.show();

      // Close splash screen
      if (this.splashWindow && !this.splashWindow.isDestroyed()) {
        this.splashWindow.close();
        this.splashWindow = null;
      }

      // Open DevTools in development mode
      if (this.isDev) {
        console.log(
          "[WindowManager] Development mode detected - opening DevTools"
        );
        this.mainWindow.webContents.openDevTools({ mode: "detach" });
      } else {
        console.log(
          "[WindowManager] Production mode - DevTools not auto-opened"
        );
      }
    });

    this.mainWindow.on("closed", () => {
      this.mainWindow = null;
    });

    return this.mainWindow;
  }

  /**
   * Handle window close event
   */
  handleWindowClose(event, isQuitting, minimizeToTray = true) {
    if (!isQuitting && minimizeToTray && process.platform === "win32") {
      event.preventDefault();
      this.mainWindow.hide();
      return true; // Prevented default
    }
    return false; // Allow close
  }

  /**
   * Show main window
   */
  showMainWindow() {
    if (this.mainWindow) {
      if (this.mainWindow.isMinimized()) {
        this.mainWindow.restore();
      }
      this.mainWindow.show();
      this.mainWindow.focus();
    }
  }

  /**
   * Hide main window
   */
  hideMainWindow() {
    if (this.mainWindow) {
      this.mainWindow.hide();
    }
  }

  /**
   * Toggle main window visibility
   */
  toggleMainWindow() {
    if (this.mainWindow) {
      if (this.mainWindow.isVisible()) {
        this.hideMainWindow();
      } else {
        this.showMainWindow();
      }
    }
  }

  /**
   * Get main window instance
   */
  getMainWindow() {
    return this.mainWindow;
  }

  /**
   * Get splash window instance
   */
  getSplashWindow() {
    return this.splashWindow;
  }

  /**
   * Destroy all windows
   */
  destroyAll() {
    if (this.splashWindow && !this.splashWindow.isDestroyed()) {
      this.splashWindow.destroy();
      this.splashWindow = null;
    }
    if (this.mainWindow && !this.mainWindow.isDestroyed()) {
      this.mainWindow.destroy();
      this.mainWindow = null;
    }
  }

  /**
   * Get Content Security Policy
   */
  _getCSP() {
    if (this.isDev) {
      // More permissive CSP for development
      console.log("[WindowManager] Using development CSP");
      return [
        "default-src 'self' 'unsafe-inline' 'unsafe-eval' http://127.0.0.1:* ws://127.0.0.1:* file: data: blob:",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://127.0.0.1:* file:",
        "style-src 'self' 'unsafe-inline' http://127.0.0.1:* file:",
        "img-src 'self' data: blob: http://127.0.0.1:* file:",
        "font-src 'self' data: file:",
        "connect-src 'self' http://127.0.0.1:* ws://127.0.0.1:*",
        "media-src 'self' blob: http://127.0.0.1:* file:",
      ].join("; ");
    } else {
      // Permissive CSP for production Electron (file:// protocol requires more flexibility)
      // Electron's sandboxed context provides security, so we can be more lenient with CSP
      console.log("[WindowManager] Using production CSP (Electron)");
      return [
        "default-src 'self' file: data: blob:",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' file:", // Need unsafe-inline/eval for bundled React app
        "style-src 'self' 'unsafe-inline' file:", // Allow inline styles for React
        "img-src 'self' data: blob: file: https:",
        "font-src 'self' data: file:",
        // CRITICAL: For file:// protocol, 'self' blocks HTTP. Must list HTTP origins explicitly BEFORE 'self'
        "connect-src http://127.0.0.1:* http://localhost:* ws://127.0.0.1:* ws://localhost:* 'self'",
        "media-src 'self' blob: file:",
        "worker-src 'self' blob:",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'",
      ].join("; ");
    }
  }

  /**
   * Get the path to the app icon with comprehensive logging and fallback
   */
  _getAppIcon() {
    const iconName =
      process.platform === "win32"
        ? "icon.ico"
        : process.platform === "darwin"
        ? "icon.icns"
        : "icon.png";

    // Log environment info for debugging
    console.log("=== Icon Resolution Debug Info ===");
    console.log("Platform:", process.platform);
    console.log("Is packaged:", this.app.isPackaged);
    console.log("__dirname:", __dirname);
    console.log("app.getAppPath():", this.app.getAppPath());
    console.log("process.resourcesPath:", process.resourcesPath);
    console.log("Icon name:", iconName);

    // Try multiple paths in order of preference
    const iconPaths = [];

    if (this.app.isPackaged) {
      // Production: Try resources path first (ASAR or unpacked)
      iconPaths.push(
        path.join(process.resourcesPath, "assets", "icons", iconName)
      );
      iconPaths.push(
        path.join(
          process.resourcesPath,
          "app.asar.unpacked",
          "assets",
          "icons",
          iconName
        )
      );
      iconPaths.push(
        path.join(this.app.getAppPath(), "assets", "icons", iconName)
      );
    } else {
      // Development: Try relative to electron directory
      iconPaths.push(path.join(__dirname, "../assets", "icons", iconName));
      iconPaths.push(
        path.join(process.cwd(), "Aura.Desktop", "assets", "icons", iconName)
      );
      iconPaths.push(path.join(process.cwd(), "assets", "icons", iconName));
    }

    // Try each path and return first that exists
    for (const iconPath of iconPaths) {
      console.log("Trying icon path:", iconPath);

      if (fs.existsSync(iconPath)) {
        console.log("✓ Found icon at:", iconPath);

        try {
          const icon = nativeImage.createFromPath(iconPath);

          if (!icon.isEmpty()) {
            console.log("✓ Icon loaded successfully");
            return icon;
          } else {
            console.warn(
              "⚠ Icon file exists but loaded as empty image:",
              iconPath
            );
          }
        } catch (error) {
          console.error(
            "✗ Error loading icon from path:",
            iconPath,
            error.message
          );
        }
      } else {
        console.log("✗ Icon not found at:", iconPath);
      }
    }

    // If no icon found, use base64 fallback
    console.warn("⚠ Using fallback icon - no icon files found");
    console.log("Searched paths:", iconPaths);

    return getFallbackIcon(nativeImage, "256");
  }

  /**
   * Get the path to the frontend files
   */
  _getFrontendPath() {
    if (this.isDev) {
      return path.join(__dirname, "../../Aura.Web/dist/index.html");
    } else {
      return path.join(process.resourcesPath, "frontend", "index.html");
    }
  }

  /**
   * Attempt to load frontend with retry logic and timeout
   */
  _attemptLoad() {
    this.loadingState.loadAttempts++;
    console.log(
      "[WindowManager] Load attempt #" + this.loadingState.loadAttempts
    );

    const targets = this._getFrontendTargets();
    if (targets.length === 0) {
      throw new Error("No frontend targets available");
    }

    // Set timeout for load (30 seconds)
    const firstTarget = targets[0];
    this.loadingState.loadTimeout = setTimeout(() => {
      if (!this.loadingState.didFinishLoad) {
        console.error("[WindowManager] ✗ Page load timeout (30 seconds)");
        console.error(
          "[WindowManager]   Start time:",
          new Date(this.loadingState.startTime).toISOString()
        );
        console.error(
          "[WindowManager]   Current time:",
          new Date().toISOString()
        );

        const { dialog } = require("electron");

        const logs = this._collectLoadingLogs();

        dialog
          .showMessageBox(this.mainWindow, {
            type: "error",
            title: "Loading Timeout",
            message: "The application failed to load within 30 seconds.",
            detail: `The page did not finish loading within the timeout period.\n\nLast known state:\n${logs}\n\nWould you like to try loading the error page?`,
            buttons: ["Load Error Page", "Close Application"],
            defaultId: 0,
            cancelId: 1,
          })
          .then((result) => {
            if (result.response === 0) {
              this._loadErrorPage(
                -7,
                "Load timeout (30 seconds)",
                this._describeTarget(firstTarget)
              );
            } else {
              this.app.quit();
            }
          });
      }
    }, 30000);

    this._loadTargetsSequentially(targets);
  }

  _loadTargetsSequentially(targets) {
    const [currentTarget, ...rest] = targets;
    if (!currentTarget) {
      console.error("[WindowManager] All load attempts failed");
      this._loadErrorPage(-1, "All load targets failed", "multiple targets");
      return;
    }

    console.log(
      "[WindowManager] Loading frontend from:",
      this._describeTarget(currentTarget)
    );
    if (currentTarget.type === "file") {
      console.log(
        "[WindowManager] Frontend path exists:",
        fs.existsSync(currentTarget.value)
      );
    }

    this._loadRendererTarget(currentTarget)
      .then(() => {
        console.log("[WindowManager] ✓ Renderer load initiated");
        const loadedURL = this.mainWindow.webContents.getURL();
        console.log("[WindowManager] Loaded URL:", loadedURL);
        console.log(
          "[WindowManager] URL protocol:",
          new URL(loadedURL).protocol
        );
        console.log("[WindowManager] Injecting global error handler...");
        return this.mainWindow.webContents.executeJavaScript(`
          (function() {
            console.log('[Injected] Installing global error handler...');

            window.addEventListener('error', function(event) {
              console.error('[Global Error Handler] Uncaught error:', {
                message: event.message,
                filename: event.filename,
                lineno: event.lineno,
                colno: event.colno,
                error: event.error ? {
                  name: event.error.name,
                  message: event.error.message,
                  stack: event.error.stack
                } : null
              });
            }, true);

            window.addEventListener('unhandledrejection', function(event) {
              console.error('[Global Error Handler] Unhandled promise rejection:', {
                reason: event.reason,
                promise: 'Promise'
              });
            });

            console.log('[Injected] ✓ Global error handlers installed');
          })();
        `);
      })
      .then(() => {
        console.log("[WindowManager] ✓ Error handler injected");
      })
      .catch((error) => {
        console.error(
          `[WindowManager] ✗ Failed to load target ${this._describeTarget(
            currentTarget
          )}:`,
          error
        );
        if (rest.length === 0) {
          this._loadErrorPage(
            -1,
            "All load targets failed",
            this._describeTarget(currentTarget)
          );
        } else {
          this._loadTargetsSequentially(rest);
        }
      });
  }

  _loadRendererTarget(target) {
    if (target.type === "url") {
      this._registerAllowedOrigin(target.value);
      console.log("[WindowManager] Loading from URL:", target.value);
      return this.mainWindow.loadURL(target.value);
    }

    console.log("[WindowManager] Loading from file:", target.value);
    console.log("[WindowManager] File exists:", fs.existsSync(target.value));

    // Verify the HTML file contains the expected assets
    if (fs.existsSync(target.value)) {
      const htmlContent = fs.readFileSync(target.value, "utf-8");
      const hasScriptTags = htmlContent.includes("<script");
      const hasModuleSrc = htmlContent.includes('type="module"');
      console.log("[WindowManager] HTML has script tags:", hasScriptTags);
      console.log("[WindowManager] HTML has module scripts:", hasModuleSrc);

      // Check if there are asset files in the same directory
      const htmlDir = path.dirname(target.value);
      const assetsDir = path.join(htmlDir, "assets");
      if (fs.existsSync(assetsDir)) {
        const assetFiles = fs.readdirSync(assetsDir);
        const jsFiles = assetFiles.filter((f) => f.endsWith(".js"));
        console.log(
          "[WindowManager] Found",
          jsFiles.length,
          "JS files in assets/"
        );
      } else {
        console.warn("[WindowManager] Assets directory not found:", assetsDir);
      }
    }

    return this.mainWindow.loadFile(target.value);
  }

  _getFrontendTargets() {
    const targets = [];

    if (this.isDev) {
      const devServers = this._getDevServerTargets();
      devServers.forEach((url) => {
        targets.push({ type: "url", value: url });
      });
    }

    const filePaths = this._getFrontendPaths();
    filePaths.forEach((filePath) => {
      targets.push({ type: "file", value: filePath });
    });

    return targets;
  }

  _getDevServerTargets() {
    const targets = new Set();
    const explicitUrl =
      process.env.AURA_VITE_DEV_SERVER_URL || process.env.VITE_DEV_SERVER_URL;
    if (explicitUrl) {
      const normalized = this._normalizeDevServerUrl(explicitUrl);
      if (normalized) {
        targets.add(normalized);
      }
    }

    const defaultPorts = new Set(["5173"]);
    if (process.env.AURA_VITE_DEV_SERVER_PORT) {
      defaultPorts.add(String(process.env.AURA_VITE_DEV_SERVER_PORT));
    }

    defaultPorts.forEach((port) => {
      targets.add(`http://127.0.0.1:${port}`);
      targets.add(`http://localhost:${port}`);
    });

    return Array.from(targets);
  }

  _normalizeDevServerUrl(value) {
    if (!value) {
      return null;
    }
    let normalized = value.trim();
    if (!normalized) {
      return null;
    }
    if (!/^https?:\/\//i.test(normalized)) {
      normalized = `http://${normalized}`;
    }
    try {
      const parsed = new URL(normalized);
      parsed.hash = "";
      if (!parsed.pathname || parsed.pathname === "/") {
        parsed.pathname = "/";
      }
      return parsed.toString().replace(/\/$/, "");
    } catch (error) {
      console.warn(
        "[WindowManager] Invalid dev server URL configured:",
        value,
        error.message
      );
      return null;
    }
  }

  /**
   * Get all possible frontend paths (primary and fallbacks)
   */
  _getFrontendPaths() {
    const paths = [];

    if (this.isDev) {
      // Development paths
      paths.push(path.join(__dirname, "../../Aura.Web/dist/index.html"));
      paths.push(path.join(process.cwd(), "Aura.Web/dist/index.html"));
      paths.push(path.join(__dirname, "../Aura.Web/dist/index.html"));
    } else {
      // Production paths
      paths.push(path.join(process.resourcesPath, "frontend", "index.html"));
      paths.push(
        path.join(
          process.resourcesPath,
          "app.asar.unpacked",
          "frontend",
          "index.html"
        )
      );
      paths.push(path.join(this.app.getAppPath(), "frontend", "index.html"));
    }

    return paths;
  }

  _describeTarget(target) {
    if (!target) {
      return "unknown target";
    }
    if (target.type === "url") {
      return `dev server (${target.value})`;
    }
    return target.value;
  }

  _registerAllowedOrigin(targetUrl) {
    if (!targetUrl) {
      return;
    }
    try {
      const parsed = new URL(targetUrl);
      if (!this.allowedWebOrigins) {
        this.allowedWebOrigins = new Set();
      }
      this.allowedWebOrigins.add(parsed.origin);
    } catch (error) {
      console.warn(
        "[WindowManager] Unable to register allowed origin:",
        targetUrl,
        error.message
      );
    }
  }

  _isAllowedNavigation(url) {
    if (!url) {
      return false;
    }
    if (url.startsWith("file://")) {
      return true;
    }
    if (!this.allowedWebOrigins || this.allowedWebOrigins.size === 0) {
      return false;
    }
    for (const origin of this.allowedWebOrigins) {
      if (origin && url.startsWith(origin)) {
        return true;
      }
    }
    return false;
  }

  /**
   * Load error page when main load fails
   */
  _loadErrorPage(errorCode, errorDescription, attemptedPath) {
    console.log("[WindowManager] Loading error page...");

    const errorPagePath = path.join(__dirname, "../assets/error.html");
    console.log("[WindowManager] Error page path:", errorPagePath);
    console.log(
      "[WindowManager] Error page exists:",
      fs.existsSync(errorPagePath)
    );

    if (!fs.existsSync(errorPagePath)) {
      console.error(
        "[WindowManager] ✗ Error page not found, creating inline error page"
      );
      this._loadInlineErrorPage(errorCode, errorDescription, attemptedPath);
      return;
    }

    this.mainWindow
      .loadFile(errorPagePath)
      .then(() => {
        console.log("[WindowManager] ✓ Error page loaded");

        // Inject error information
        return this.mainWindow.webContents.executeJavaScript(`
          window.AURA_VERSION = '${this.app.getVersion()}';
          window.AURA_LOGS_PATH = '${path
            .join(this.app.getPath("userData"), "logs")
            .replace(/\\/g, "\\\\")}';
          window.AURA_ERROR_INFO = {
            errorCode: ${errorCode},
            errorDescription: '${errorDescription.replace(/'/g, "\\'")}',
            attemptedPath: '${attemptedPath.replace(/\\/g, "\\\\")}',
            url: '${this.mainWindow.webContents.getURL()}'
          };
        `);
      })
      .then(() => {
        console.log("[WindowManager] ✓ Error information injected");
      })
      .catch((error) => {
        console.error("[WindowManager] ✗ Failed to load error page:", error);
        this._loadInlineErrorPage(errorCode, errorDescription, attemptedPath);
      });
  }

  /**
   * Load inline error page as last resort
   */
  _loadInlineErrorPage(errorCode, errorDescription, attemptedPath) {
    const logsPath = path.join(this.app.getPath("userData"), "logs");

    this.mainWindow.loadURL(`data:text/html,
      <html>
        <head>
          <style>
            body {
              background: #1a1a2e;
              color: #fff;
              font-family: system-ui;
              padding: 40px;
              display: flex;
              align-items: center;
              justify-content: center;
              min-height: 100vh;
              margin: 0;
            }
            .container {
              max-width: 600px;
              text-align: center;
            }
            h1 { color: #ff6b6b; font-size: 32px; margin-bottom: 20px; }
            p { line-height: 1.6; margin-bottom: 15px; }
            .error-box {
              background: rgba(255,107,107,0.1);
              border: 1px solid rgba(255,107,107,0.3);
              border-radius: 8px;
              padding: 15px;
              margin: 20px 0;
              font-family: monospace;
              font-size: 13px;
              text-align: left;
            }
            button {
              background: #667eea;
              color: white;
              border: none;
              padding: 12px 30px;
              border-radius: 8px;
              font-size: 16px;
              cursor: pointer;
              margin: 10px;
            }
            button:hover { background: #5568d3; }
          </style>
        </head>
        <body>
          <div class="container">
            <h1>⚠️ Failed to Load Application</h1>
            <p>Aura Video Studio encountered an error while loading.</p>
            <div class="error-box">
              Error Code: ${errorCode}<br>
              Description: ${errorDescription}<br>
              Path: ${attemptedPath}<br>
              Logs: ${logsPath}
            </div>
            <button onclick="window.location.reload()">Retry</button>
          </div>
        </body>
      </html>
    `);
  }

  /**
   * Collect loading logs for diagnostics
   */
  _collectLoadingLogs() {
    const logs = [];
    logs.push(`Did start loading: ${this.loadingState.didStartLoading}`);
    logs.push(`Did finish load: ${this.loadingState.didFinishLoad}`);
    logs.push(`Load attempts: ${this.loadingState.loadAttempts}`);

    if (this.loadingState.startTime) {
      logs.push(
        `Start time: ${new Date(this.loadingState.startTime).toISOString()}`
      );
      logs.push(`Elapsed: ${Date.now() - this.loadingState.startTime}ms`);
    }

    if (this.loadingState.lastError) {
      logs.push(
        `Last error: Code ${this.loadingState.lastError.errorCode} - ${this.loadingState.lastError.errorDescription}`
      );
    }

    return logs.join("\n");
  }
}

module.exports = WindowManager;
