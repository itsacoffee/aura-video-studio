/**
 * Protocol Handler Module
 * Handles custom protocol (aura://) registration and deep linking
 */

const { protocol, app } = require('electron');
const url = require('url');
const path = require('path');

class ProtocolHandler {
  constructor(windowManager) {
    this.windowManager = windowManager;
    this.protocolScheme = 'aura';
    this.pendingUrl = null;
  }

  /**
   * Register custom protocol handler
   */
  register() {
    // Set as default protocol client for aura://
    if (process.defaultApp) {
      if (process.argv.length >= 2) {
        app.setAsDefaultProtocolClient(this.protocolScheme, process.execPath, [path.resolve(process.argv[1])]);
      }
    } else {
      app.setAsDefaultProtocolClient(this.protocolScheme);
    }

    // Handle protocol on Windows/Linux
    app.on('second-instance', (event, commandLine, workingDirectory) => {
      // Someone tried to run a second instance, focus our window instead
      const window = this.windowManager.getMainWindow();
      if (window) {
        if (window.isMinimized()) window.restore();
        window.focus();
      }

      // Check if protocol URL was passed
      const protocolUrl = commandLine.find(arg => arg.startsWith(`${this.protocolScheme}://`));
      if (protocolUrl) {
        this.handleProtocolUrl(protocolUrl);
      }
    });

    // Handle protocol on macOS
    app.on('open-url', (event, url) => {
      event.preventDefault();
      if (url.startsWith(`${this.protocolScheme}://`)) {
        this.handleProtocolUrl(url);
      }
    });

    // Register file protocol for loading local files
    protocol.registerSchemesAsPrivileged([
      {
        scheme: this.protocolScheme,
        privileges: {
          standard: true,
          secure: true,
          supportFetchAPI: true
        }
      }
    ]);

    console.log(`Protocol handler registered for ${this.protocolScheme}://`);
  }

  /**
   * Handle protocol URL
   */
  handleProtocolUrl(protocolUrl) {
    console.log('Protocol URL received:', protocolUrl);

    try {
      // Validate and sanitize URL
      const parsed = this.parseProtocolUrl(protocolUrl);
      if (!parsed) {
        console.warn('Invalid protocol URL:', protocolUrl);
        return;
      }

      const window = this.windowManager.getMainWindow();
      if (window && !window.isDestroyed()) {
        // Send to renderer process
        window.webContents.send('protocol:navigate', parsed);
        
        // Focus window
        if (window.isMinimized()) window.restore();
        window.focus();
      } else {
        // Store for later when window is created
        this.pendingUrl = parsed;
      }
    } catch (error) {
      console.error('Error handling protocol URL:', error);
    }
  }

  /**
   * Parse and validate protocol URL
   */
  parseProtocolUrl(protocolUrl) {
    try {
      // Remove protocol scheme
      const urlWithoutProtocol = protocolUrl.replace(/^aura:\/\//, '');
      
      // Parse URL
      const parsed = new URL(`http://${urlWithoutProtocol}`);
      
      // Extract components
      const action = parsed.hostname.toLowerCase();
      const params = {};
      
      parsed.searchParams.forEach((value, key) => {
        // Sanitize parameter values
        params[key] = this.sanitizeValue(value);
      });

      // Validate action
      const validActions = [
        'open',        // aura://open?path=/path/to/project
        'create',      // aura://create?template=basic
        'generate',    // aura://generate?script=...
        'settings',    // aura://settings
        'help',        // aura://help
        'about'        // aura://about
      ];

      if (!validActions.includes(action)) {
        console.warn('Invalid protocol action:', action);
        return null;
      }

      return {
        action,
        params,
        originalUrl: protocolUrl
      };
    } catch (error) {
      console.error('Error parsing protocol URL:', error);
      return null;
    }
  }

  /**
   * Sanitize parameter value
   */
  sanitizeValue(value) {
    if (typeof value !== 'string') {
      return '';
    }

    // Remove any potentially dangerous characters
    return value
      .replace(/[<>]/g, '')  // Remove HTML tags
      .replace(/javascript:/gi, '')  // Remove javascript: protocol
      .replace(/on\w+=/gi, '')  // Remove event handlers
      .trim();
  }

  /**
   * Check for pending protocol URL
   */
  checkPendingUrl() {
    if (this.pendingUrl) {
      const window = this.windowManager.getMainWindow();
      if (window && !window.isDestroyed()) {
        window.webContents.send('protocol:navigate', this.pendingUrl);
        this.pendingUrl = null;
      }
    }
  }

  /**
   * Create protocol URL
   */
  static createUrl(action, params = {}) {
    const query = new URLSearchParams(params).toString();
    return `aura://${action}${query ? '?' + query : ''}`;
  }
}

module.exports = ProtocolHandler;
