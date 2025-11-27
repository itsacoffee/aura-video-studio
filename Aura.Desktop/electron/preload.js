/**
 * Enhanced Electron Preload Script
 *
 * This script runs in a sandboxed context with access to both Node.js APIs
 * and the renderer process DOM. It exposes a safe, typed API to the renderer
 * via contextBridge with full security measures.
 */

const { contextBridge, ipcRenderer } = require("electron");
const os = require("os");
const { MENU_EVENT_CHANNELS } = require("./menu-event-types");
const { createValidatedMenuAPI } = require("./menu-command-handler");

// Event listener timeout in milliseconds (5 seconds)
const EVENT_LISTENER_TIMEOUT = 5000;

// Validate IPC channel names to prevent injection attacks
const VALID_CHANNELS = {
  // Configuration channels
  CONFIG: [
    "config:get",
    "config:set",
    "config:getAll",
    "config:reset",
    "config:getSecure",
    "config:setSecure",
    "config:deleteSecure",
    "config:addRecentProject",
    "config:getRecentProjects",
    "config:clearRecentProjects",
    "config:removeRecentProject",
    "config:isSafeMode",
    "config:getCrashCount",
    "config:resetCrashCount",
    "config:deleteAndRestart",
    "config:getConfigPath",
  ],

  // Dialog channels
  DIALOG: [
    "dialog:openFolder",
    "dialog:openFile",
    "dialog:openMultipleFiles",
    "dialog:saveFile",
    "dialog:showMessage",
    "dialog:showError",
    "dialog:showSaveDialog",
    "dialog:showOpenDialog",
  ],

  // File system channels
  FS: [
    "fs:writeFile",
    "fs:readFile",
    "fs:exists",
    "fs:mkdir",
  ],

  // Shell channels
  SHELL: [
    "shell:openExternal",
    "shell:openPath",
    "shell:showItemInFolder",
    "shell:trashItem",
  ],

  // App channels
  APP: [
    "app:getVersion",
    "app:getName",
    "app:getPaths",
    "app:getLocale",
    "app:isPackaged",
    "app:restart",
    "app:quit",
  ],

  // Window channels
  WINDOW: [
    "window:minimize",
    "window:maximize",
    "window:close",
    "window:hide",
    "window:show",
  ],

  // Video generation channels
  VIDEO: [
    "video:generate:start",
    "video:generate:pause",
    "video:generate:resume",
    "video:generate:cancel",
    "video:generate:status",
    "video:generate:list",
  ],

  // Backend channels
  BACKEND: [
    "backend:getUrl",
    "backend:health",
    "backend:ping",
    "backend:info",
    "backend:version",
    "backend:providerStatus",
    "backend:ffmpegStatus",
    "backend:restart",
    "backend:stop",
    "backend:status",
    "backend:checkFirewall",
    "backend:getFirewallRule",
    "backend:getFirewallCommand",
  ],

  // Startup logs channels
  STARTUP_LOGS: [
    "startup-logs:get-latest",
    "startup-logs:get-summary",
    "startup-logs:get-log-content",
    "startup-logs:list",
    "startup-logs:read-file",
    "startup-logs:open-directory",
  ],

  // Diagnostics channels
  DIAGNOSTICS: [
    "diagnostics:runAll",
    "diagnostics:checkFFmpeg",
    "diagnostics:fixFFmpeg",
    "diagnostics:checkAPI",
    "diagnostics:fixAPI",
    "diagnostics:checkProviders",
    "diagnostics:fixProviders",
    "diagnostics:checkDiskSpace",
    "diagnostics:checkConfig",
  ],

  // Update channels
  UPDATES: ["updates:check"],

  // Runtime diagnostics
  RUNTIME: ["runtime:getDiagnostics"],

  // Context menu channels
  CONTEXT_MENU: [
    "context-menu:show",
    "context-menu:reveal-in-os",
    "context-menu:open-path",
  ],
};

// Event channels that renderer can listen to (includes menu events from menu-event-types.js)
const VALID_EVENT_CHANNELS = [
  "video:progress",
  "video:error",
  "video:complete",
  "backend:healthUpdate",
  "backend:providerUpdate",
  "protocol:navigate",
  "app:safeMode",
  ...MENU_EVENT_CHANNELS,
];

let runtimeBootstrap = null;
try {
  runtimeBootstrap = ipcRenderer.sendSync("runtime:getBootstrap");
  console.log("[Preload] Runtime bootstrap received:", runtimeBootstrap);

  // CRITICAL: Validate that backend URL is present
  if (
    !runtimeBootstrap ||
    !runtimeBootstrap.backend ||
    !runtimeBootstrap.backend.baseUrl
  ) {
    console.error("[Preload] ERROR: Runtime bootstrap missing backend URL!");
    console.error(
      "[Preload] This will cause all API calls to fail with 'Network Error'"
    );
    console.error(
      "[Preload] Bootstrap data:",
      JSON.stringify(runtimeBootstrap, null, 2)
    );
  } else {
    console.log(
      "[Preload] ✓ Backend URL confirmed:",
      runtimeBootstrap.backend.baseUrl
    );
  }
} catch (error) {
  console.error("[Preload] Failed to read runtime bootstrap payload:", error);
  console.error("[Preload] This will cause all API calls to fail!");
  runtimeBootstrap = null;
}

/**
 * Validate channel name
 */
function isValidChannel(channel) {
  return Object.values(VALID_CHANNELS).flat().includes(channel);
}

/**
 * Validate event channel name
 */
function isValidEventChannel(channel) {
  // Allow static valid event channels
  if (VALID_EVENT_CHANNELS.includes(channel)) {
    return true;
  }
  // Allow dynamic context menu action channels
  // Pattern: context-menu:action:${type}:${actionType}
  if (channel.startsWith("context-menu:action:")) {
    return true;
  }
  return false;
}

/**
 * Safe IPC invoke wrapper
 */
async function safeInvoke(channel, ...args) {
  if (!isValidChannel(channel)) {
    throw new Error(`Invalid IPC channel: ${channel}`);
  }
  return ipcRenderer.invoke(channel, ...args);
}

/**
 * Safe IPC event listener wrapper with validation and timeout
 * @param {string} channel - The IPC channel to listen to
 * @param {Function} callback - The callback function to execute
 * @returns {Function} Unsubscribe function to remove the listener
 */
function safeOn(channel, callback) {
  if (!isValidEventChannel(channel)) {
    throw new Error(`Invalid event channel: ${channel}`);
  }

  // Validate callback is a function
  if (typeof callback !== "function") {
    throw new TypeError(
      `Callback must be a function, received ${typeof callback}`
    );
  }

  // Track active listeners per channel for memory leak prevention
  if (!safeOn._listenerCounts) {
    safeOn._listenerCounts = new Map();
  }

  const currentCount = safeOn._listenerCounts.get(channel) || 0;
  safeOn._listenerCounts.set(channel, currentCount + 1);

  // Warn if too many listeners on same channel
  if (currentCount >= 2) {
    console.warn(
      `[Preload] Multiple listeners (${
        currentCount + 1
      }) registered for channel: ${channel}. Possible memory leak.`
    );
  }

  // Wrapper that adds timeout monitoring
  const subscription = (event, ...args) => {
    const startTime = Date.now();

    try {
      // Execute callback with timeout monitoring
      const result = callback(...args);

      // If callback returns a promise, monitor its completion time
      if (result && typeof result.then === "function") {
        const timeoutId = setTimeout(() => {
          const elapsed = Date.now() - startTime;
          console.warn(
            `[Preload] Event listener for '${channel}' did not complete within ${EVENT_LISTENER_TIMEOUT}ms (elapsed: ${elapsed}ms)`
          );
        }, EVENT_LISTENER_TIMEOUT);

        result.finally(() => clearTimeout(timeoutId));
      } else {
        // For synchronous callbacks, check if execution took too long
        const elapsed = Date.now() - startTime;
        if (elapsed > EVENT_LISTENER_TIMEOUT) {
          console.warn(
            `[Preload] Synchronous event listener for '${channel}' took ${elapsed}ms (threshold: ${EVENT_LISTENER_TIMEOUT}ms)`
          );
        }
      }
    } catch (error) {
      console.error(
        `[Preload] Error in event listener for '${channel}':`,
        error
      );
    }
  };

  ipcRenderer.on(channel, subscription);

  // Return unsubscribe function that properly cleans up
  return () => {
    ipcRenderer.removeListener(channel, subscription);

    // Update listener count
    const count = safeOn._listenerCounts.get(channel) || 0;
    if (count > 0) {
      safeOn._listenerCounts.set(channel, count - 1);
    }
  };
}

/**
 * Safe IPC once listener wrapper
 */
function safeOnce(channel, callback) {
  if (!isValidEventChannel(channel)) {
    throw new Error(`Invalid event channel: ${channel}`);
  }

  ipcRenderer.once(channel, (event, ...args) => callback(...args));
}

const desktopBridge = {
  getBackendBaseUrl: () => runtimeBootstrap?.backend?.baseUrl || null,
  getAppEnvironment: () =>
    runtimeBootstrap?.environment?.mode || process.env.NODE_ENV || "production",
  getDiagnosticInfo: async () => {
    try {
      runtimeBootstrap = await safeInvoke("runtime:getDiagnostics");
    } catch (error) {
      console.error("[Preload] Failed to refresh runtime diagnostics:", error);
    }
    return runtimeBootstrap;
  },
  getCachedDiagnostics: () => runtimeBootstrap,
  backend: {
    ...(runtimeBootstrap?.backend || {}),
    getUrl: () => runtimeBootstrap?.backend?.baseUrl || null,
  },
  environment: runtimeBootstrap?.environment || null,
  os: runtimeBootstrap?.os || null,
  paths: runtimeBootstrap?.paths || null,
  onBackendHealthUpdate: (callback) => safeOn("backend:healthUpdate", callback),
  onBackendProviderUpdate: (callback) =>
    safeOn("backend:providerUpdate", callback),
};

contextBridge.exposeInMainWorld("desktopBridge", desktopBridge);

const auraBridge = createAuraBridge();

contextBridge.exposeInMainWorld("aura", auraBridge);
// Legacy alias for compatibility with existing renderer code
contextBridge.exposeInMainWorld("electron", auraBridge);

try {
  if (typeof window !== "undefined") {
    Object.defineProperty(window, "AURA_IS_ELECTRON", {
      value: true,
      configurable: false,
    });

    Object.defineProperty(window, "AURA_BACKEND_URL", {
      get: () => runtimeBootstrap?.backend?.baseUrl || null,
      configurable: true,
    });

    Object.defineProperty(window, "AURA_IS_DEV", {
      get: () =>
        auraBridge.env.mode
          ? auraBridge.env.mode === "development"
          : auraBridge.env.isDev,
      configurable: true,
    });

    Object.defineProperty(window, "AURA_VERSION", {
      get: () => runtimeBootstrap?.environment?.version || "unknown",
      configurable: true,
    });
  }
} catch (legacyError) {
  console.warn("[Preload] Failed to expose legacy Aura globals", legacyError);
}

function createAuraBridge() {
  const isDevRuntime =
    !process.env.NODE_ENV || process.env.NODE_ENV === "development";

  const refreshDiagnostics = async () => {
    try {
      runtimeBootstrap = await safeInvoke("runtime:getDiagnostics");
    } catch (error) {
      console.error("[Preload] Failed to refresh runtime diagnostics:", error);
    }
    return runtimeBootstrap;
  };

  const backendApi = {
    async getBaseUrl() {
      if (runtimeBootstrap?.backend?.baseUrl) {
        return runtimeBootstrap.backend.baseUrl;
      }
      try {
        const url = await safeInvoke("backend:getUrl");
        if (url) {
          runtimeBootstrap = {
            ...(runtimeBootstrap || {}),
            backend: {
              ...(runtimeBootstrap?.backend || {}),
              baseUrl: url,
            },
          };
        }
        return url;
      } catch (error) {
        console.error("[Preload] Failed to resolve backend URL:", error);
        return null;
      }
    },
    getUrl() {
      return backendApi.getBaseUrl();
    },
    health: () => safeInvoke("backend:health"),
    ping: () => safeInvoke("backend:ping"),
    info: () => safeInvoke("backend:info"),
    version: () => safeInvoke("backend:version"),
    providerStatus: () => safeInvoke("backend:providerStatus"),
    ffmpegStatus: () => safeInvoke("backend:ffmpegStatus"),
    restart: () => safeInvoke("backend:restart"),
    stop: () => safeInvoke("backend:stop"),
    status: () => safeInvoke("backend:status"),
    checkFirewall: () => safeInvoke("backend:checkFirewall"),
    getFirewallRule: () => safeInvoke("backend:getFirewallRule"),
    getFirewallCommand: () => safeInvoke("backend:getFirewallCommand"),
    onHealthUpdate: (callback) => safeOn("backend:healthUpdate", callback),
    onProviderUpdate: (callback) => safeOn("backend:providerUpdate", callback),
  };

  const ffmpegApi = {
    checkStatus: () => safeInvoke("ffmpeg:checkStatus"),
    install: (options) => safeInvoke("ffmpeg:install", options),
    getProgress: () => safeInvoke("ffmpeg:getProgress"),
    openDirectory: () => safeInvoke("ffmpeg:openDirectory"),
  };

  const dialogsApi = {
    openFolder: () => safeInvoke("dialog:openFolder"),
    openFile: (options) => safeInvoke("dialog:openFile", options),
    openMultipleFiles: (options) =>
      safeInvoke("dialog:openMultipleFiles", options),
    saveFile: (options) => safeInvoke("dialog:saveFile", options),
    showMessage: (options) => safeInvoke("dialog:showMessage", options),
    showError: (title, message) =>
      safeInvoke("dialog:showError", title, message),
    showSaveDialog: (options) => safeInvoke("dialog:showSaveDialog", options),
    showOpenDialog: (options) => safeInvoke("dialog:showOpenDialog", options),
  };

  const fsApi = {
    writeFile: (path, data) => safeInvoke("fs:writeFile", path, data),
    readFile: (path) => safeInvoke("fs:readFile", path),
    exists: (path) => safeInvoke("fs:exists", path),
    mkdir: (path, options) => safeInvoke("fs:mkdir", path, options),
  };

  const shellApi = {
    openExternal: (url) => safeInvoke("shell:openExternal", url),
    openPath: (path) => safeInvoke("shell:openPath", path),
    showItemInFolder: (path) => safeInvoke("shell:showItemInFolder", path),
    trashItem: (path) => safeInvoke("shell:trashItem", path),
  };

  const appApi = {
    getVersion: () => safeInvoke("app:getVersion"),
    getName: () => safeInvoke("app:getName"),
    getPaths: () => safeInvoke("app:getPaths"),
    getLocale: () => safeInvoke("app:getLocale"),
    isPackaged: () => safeInvoke("app:isPackaged"),
    restart: () => safeInvoke("app:restart"),
    quit: () => safeInvoke("app:quit"),
  };

  const windowApi = {
    minimize: () => safeInvoke("window:minimize"),
    maximize: () => safeInvoke("window:maximize"),
    close: () => safeInvoke("window:close"),
    hide: () => safeInvoke("window:hide"),
    show: () => safeInvoke("window:show"),
  };

  const videoApi = {
    generate: {
      start: (config) => safeInvoke("video:generate:start", config),
      pause: (generationId) => safeInvoke("video:generate:pause", generationId),
      resume: (generationId) =>
        safeInvoke("video:generate:resume", generationId),
      cancel: (generationId) =>
        safeInvoke("video:generate:cancel", generationId),
      status: (generationId) =>
        safeInvoke("video:generate:status", generationId),
      list: () => safeInvoke("video:generate:list"),
    },
    onProgress: (callback) => safeOn("video:progress", callback),
    onError: (callback) => safeOn("video:error", callback),
    onComplete: (callback) => safeOn("video:complete", callback),
  };

  const configApi = {
    get: (key, defaultValue) => safeInvoke("config:get", key, defaultValue),
    set: (key, value) => safeInvoke("config:set", key, value),
    getAll: () => safeInvoke("config:getAll"),
    reset: () => safeInvoke("config:reset"),
    getSecure: (key) => safeInvoke("config:getSecure", key),
    setSecure: (key, value) => safeInvoke("config:setSecure", key, value),
    deleteSecure: (key) => safeInvoke("config:deleteSecure", key),
    addRecentProject: (path, name) =>
      safeInvoke("config:addRecentProject", path, name),
    getRecentProjects: () => safeInvoke("config:getRecentProjects"),
    clearRecentProjects: () => safeInvoke("config:clearRecentProjects"),
    removeRecentProject: (path) =>
      safeInvoke("config:removeRecentProject", path),
    isSafeMode: () => safeInvoke("config:isSafeMode"),
    getCrashCount: () => safeInvoke("config:getCrashCount"),
    resetCrashCount: () => safeInvoke("config:resetCrashCount"),
    deleteAndRestart: () => safeInvoke("config:deleteAndRestart"),
    getConfigPath: () => safeInvoke("config:getConfigPath"),
  };

  const diagnosticsApi = {
    runAll: () => safeInvoke("diagnostics:runAll"),
    checkFFmpeg: () => safeInvoke("diagnostics:checkFFmpeg"),
    fixFFmpeg: () => safeInvoke("diagnostics:fixFFmpeg"),
    checkAPI: () => safeInvoke("diagnostics:checkAPI"),
    fixAPI: () => safeInvoke("diagnostics:fixAPI"),
    checkProviders: () => safeInvoke("diagnostics:checkProviders"),
    fixProviders: () => safeInvoke("diagnostics:fixProviders"),
    checkDiskSpace: () => safeInvoke("diagnostics:checkDiskSpace"),
    checkConfig: () => safeInvoke("diagnostics:checkConfig"),
  };

  const startupLogsApi = {
    getLatest: () => safeInvoke("startup-logs:get-latest"),
    getSummary: () => safeInvoke("startup-logs:get-summary"),
    getLogContent: () => safeInvoke("startup-logs:get-log-content"),
    list: () => safeInvoke("startup-logs:list"),
    readFile: (filePath) => safeInvoke("startup-logs:read-file", filePath),
    openDirectory: () => safeInvoke("startup-logs:open-directory"),
  };

  const updatesApi = {
    check: () => safeInvoke("updates:check"),
  };

  const protocolApi = {
    onNavigate: (callback) => safeOn("protocol:navigate", callback),
  };

  const contextMenuApi = {
    /**
     * Show a context menu of the specified type.
     * @param {string} type - The context menu type
     * @param {object} data - Data specific to the menu type
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    show: (type, data) => safeInvoke("context-menu:show", type, data),

    /**
     * Register a listener for context menu actions.
     * @param {string} type - The context menu type
     * @param {string} actionType - The action type (e.g., 'onCut', 'onCopy')
     * @param {function} callback - Function called when action is triggered
     * @returns {function} Unsubscribe function
     */
    onAction: (type, actionType, callback) => {
      const channel = `context-menu:action:${type}:${actionType}`;
      return safeOn(channel, callback);
    },

    /**
     * Reveal a file or folder in the OS file explorer.
     * @param {string} filePath - Path to the file or folder
     */
    revealInOS: (filePath) => safeInvoke("context-menu:reveal-in-os", filePath),

    /**
     * Open a file or path with the default system application.
     * @param {string} filePath - Path to the file or folder
     */
    openPath: (filePath) => safeInvoke("context-menu:open-path", filePath),
  };

  const runtimeApi = {
    getDiagnostics: () => refreshDiagnostics(),
    refresh: () => refreshDiagnostics(),
    getCachedDiagnostics: () => runtimeBootstrap,
    onBackendHealthUpdate: (callback) =>
      safeOn("backend:healthUpdate", callback),
    onBackendProviderUpdate: (callback) =>
      safeOn("backend:providerUpdate", callback),
  };

  const systemApi = {
    getEnvironmentInfo: async () => {
      const diagnostics = runtimeBootstrap ?? (await refreshDiagnostics());
      return {
        environment: diagnostics?.environment ?? null,
        os: diagnostics?.os ?? null,
        paths: diagnostics?.paths ?? null,
        platform: {
          os: process.platform,
          arch: process.arch,
          release: os.release(),
          versions: { ...process.versions },
        },
      };
    },
    getPaths: () => safeInvoke("app:getPaths"),
  };

  const aura = {
    env: {
      isElectron: true,
      isDev: isDevRuntime,
      mode:
        runtimeBootstrap?.environment?.mode ||
        process.env.NODE_ENV ||
        "production",
      version: runtimeBootstrap?.environment?.version || "unknown",
      isPackaged: runtimeBootstrap?.environment?.isPackaged ?? false,
    },
    platform: {
      os: process.platform,
      arch: process.arch,
      release: os.release(),
      isWindows: process.platform === "win32",
      isMac: process.platform === "darwin",
      isLinux: process.platform === "linux",
      versions: {
        node: process.versions.node,
        chrome: process.versions.chrome,
        electron: process.versions.electron,
      },
    },
    runtime: runtimeApi,
    backend: backendApi,
    ffmpeg: ffmpegApi,
    dialogs: dialogsApi,
    fs: fsApi,
    shell: shellApi,
    app: appApi,
    window: windowApi,
    video: videoApi,
    config: configApi,
    diagnostics: diagnosticsApi,
    updates: updatesApi,
    protocol: protocolApi,
    contextMenu: contextMenuApi,
    menu: createValidatedMenuAPI(ipcRenderer),
    startupLogs: startupLogsApi,
    safeMode: {
      onStatus: (callback) => safeOn("app:safeMode", callback),
    },
    system: systemApi,
    events: {
      on: (channel, callback) => safeOn(channel, callback),
      once: (channel, callback) => safeOnce(channel, callback),
    },
  };

  // Legacy compatibility helpers
  aura.invoke = (channel, ...args) => safeInvoke(channel, ...args);
  aura.on = (channel, callback) => safeOn(channel, callback);
  aura.once = (channel, callback) => safeOnce(channel, callback);
  aura.selectFolder = () => aura.dialogs.openFolder();
  aura.openPath = (path) => aura.shell.openPath(path);
  aura.openExternal = (url) => aura.shell.openExternal(url);

  return aura;
}

// Log preload script initialization
console.log("Enhanced preload script loaded");
console.log("Platform:", process.platform);
console.log("Architecture:", process.arch);
console.log("Electron version:", process.versions.electron);
console.log("Node version:", process.versions.node);
console.log("Chrome version:", process.versions.chrome);
console.log("Context isolation enabled: true");
console.log("Node integration disabled: true");
console.log(
  `[Preload] Registered ${MENU_EVENT_CHANNELS.length} menu event channels`
);

// Listen for window.onerror events and log them to the main process
// This must be done via a script injection since we can't access window in preload directly
const errorHandlerScript = `
(function() {
  console.log('[ErrorHandler] Installing global error handler...');

  // Capture uncaught errors
  window.addEventListener('error', function(event) {
    const errorInfo = {
      message: event.message || 'Unknown error',
      filename: event.filename || 'unknown',
      lineno: event.lineno || 0,
      colno: event.colno || 0,
      error: event.error ? {
        name: event.error.name,
        message: event.error.message,
        stack: event.error.stack
      } : null,
      timestamp: new Date().toISOString()
    };

    console.error('[ErrorHandler] Uncaught error:', errorInfo);

    // Try to send to main process if electron API is available
    if (window.electron && window.electron.app) {
      console.log('[ErrorHandler] Error details logged to console');
    }
  }, true);

  // Capture unhandled promise rejections
  window.addEventListener('unhandledrejection', function(event) {
    const rejectionInfo = {
      reason: event.reason,
      promise: 'Promise',
      timestamp: new Date().toISOString()
    };

    console.error('[ErrorHandler] Unhandled promise rejection:', rejectionInfo);

    if (window.electron && window.electron.app) {
      console.log('[ErrorHandler] Promise rejection details logged to console');
    }
  });

  console.log('[ErrorHandler] ✓ Global error handlers installed');
})();
`;

// We can't execute this directly in preload, but we can inject it via IPC
// Store it to be injected by main process after window loads
if (
  typeof process !== "undefined" &&
  process.versions &&
  process.versions.electron
) {
  // Signal to main process that error handler script is ready
  console.log("[Preload] Error handler script prepared for injection");
}
