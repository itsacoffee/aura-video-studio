/**
 * OpenCut Manager (DEPRECATED)
 * 
 * NOTE: This module is deprecated and no longer used.
 * 
 * OpenCut has been refactored to run natively as React components within
 * Aura.Web, eliminating the need for a separate Next.js server.
 * 
 * The old architecture embedded OpenCut via an iframe that loaded from a
 * separate Next.js server running on port 3100. This caused issues:
 * - Required starting and managing a separate Node.js process
 * - Showed loading spinners and connection errors
 * - Failed when the server wasn't available
 * 
 * The new architecture renders OpenCut components directly in Aura.Web:
 * - No separate server process needed
 * - Components load immediately
 * - No connection errors or health checks
 * - Better performance and reliability
 * 
 * This file is preserved for reference but all methods are now no-ops.
 * 
 * @deprecated OpenCut now runs natively in Aura.Web - server not needed
 */

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
    this.enabled = false; // Disabled - OpenCut runs natively now
    this.startAttempts = 0;
    this.maxStartAttempts = 3;
    this.healthCheckInterval = null;
    this.isStarting = false;
    
    // Log deprecation notice
    this.logger.info?.("OpenCutManager", "DEPRECATED: OpenCut now runs natively in Aura.Web - server management disabled");
  }

  /**
   * Start the OpenCut server (DEPRECATED - no-op)
   * @deprecated OpenCut now runs natively in Aura.Web
   */
  async start() {
    this.logger.info?.("OpenCutManager", "start() called but OpenCut server is no longer needed");
    this.logger.info?.("OpenCutManager", "OpenCut components now run natively in Aura.Web");
    return;
  }

  /**
   * Stop the OpenCut server (DEPRECATED - no-op)
   * @deprecated OpenCut now runs natively in Aura.Web
   */
  stop() {
    this.logger.info?.("OpenCutManager", "stop() called but OpenCut server was not running");
  }

  /**
   * Check if OpenCut is available
   * @returns {boolean} Always returns true since OpenCut runs natively
   */
  isAvailable() {
    // OpenCut is always available since it runs natively in Aura.Web
    return true;
  }

  /**
   * Returns the URL where OpenCut would be available (DEPRECATED)
   * @deprecated OpenCut now runs natively - URL not needed
   */
  getUrl() {
    // Return a placeholder URL - not actually used
    return `http://127.0.0.1:${this.port}`;
  }

  /**
   * Reset start attempts counter (DEPRECATED - no-op)
   * @deprecated OpenCut now runs natively in Aura.Web
   */
  resetAttempts() {
    this.startAttempts = 0;
  }

  /**
   * Get diagnostics information
   * @returns {object} Diagnostics information indicating native mode
   */
  async getDiagnostics() {
    return {
      mode: "native",
      deprecated: true,
      message: "OpenCut runs natively in Aura.Web - no server needed",
      serverPath: null,
      serverExists: false,
      openCutDirExists: false,
      portInUse: false,
      port: this.port,
      processRunning: false,
      isStarting: false,
      isPackaged: this.isPackaged,
      resourcesPath: this.isPackaged ? process.resourcesPath : null,
      checkedPaths: [],
      enabled: false,
      startAttempts: 0,
      maxStartAttempts: this.maxStartAttempts,
    };
  }
}

module.exports = OpenCutManager;
