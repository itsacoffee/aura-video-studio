/**
 * Tray Manager Module
 * Manages Windows system tray integration
 */

const { Tray, Menu, nativeImage } = require('electron');
const path = require('path');
const fs = require('fs');

class TrayManager {
  constructor(app, windowManager, isDev) {
    this.app = app;
    this.windowManager = windowManager;
    this.isDev = isDev;
    this.tray = null;
    this.backendUrl = null;
    this.operationStatus = null;
  }

  /**
   * Create system tray
   */
  create() {
    const trayIconPath = this._getTrayIconPath();
    
    if (!trayIconPath || !fs.existsSync(trayIconPath)) {
      console.warn('Tray icon not found at path:', trayIconPath);
      console.warn('Tray will not be created. This is optional and the app will continue.');
      return null;
    }

    try {
      // Create tray with icon
      const icon = nativeImage.createFromPath(trayIconPath);
      
      // Verify icon was loaded successfully
      if (icon.isEmpty()) {
        console.warn('Tray icon is empty after loading from:', trayIconPath);
        console.warn('Tray will not be created. This is optional and the app will continue.');
        return null;
      }
      
      this.tray = new Tray(icon);
    } catch (error) {
      console.error('Failed to create system tray:', error.message);
      console.warn('Tray will not be created. This is optional and the app will continue.');
      return null;
    }
    
    // Set tooltip
    this.tray.setToolTip('Aura Video Studio');
    
    // Build and set context menu
    this.updateMenu();
    
    // Handle tray icon click
    this.tray.on('click', () => {
      this.windowManager.toggleMainWindow();
    });

    // Handle double click (Windows)
    this.tray.on('double-click', () => {
      this.windowManager.showMainWindow();
    });

    // Handle right click (show context menu automatically on Windows)
    if (process.platform === 'win32') {
      this.tray.on('right-click', () => {
        this.tray.popUpContextMenu();
      });
    }

    console.log('System tray created');
    return this.tray;
  }

  /**
   * Update tray context menu
   */
  updateMenu() {
    if (!this.tray) return;

    const contextMenu = Menu.buildFromTemplate(this._buildMenuTemplate());
    this.tray.setContextMenu(contextMenu);
  }

  /**
   * Set backend URL for display
   */
  setBackendUrl(url) {
    this.backendUrl = url;
    this.updateMenu();
  }

  /**
   * Set operation status
   */
  setOperationStatus(status) {
    this.operationStatus = status;
    this.updateMenu();
    
    // Update tooltip with status
    if (status) {
      this.tray.setToolTip(`Aura Video Studio - ${status}`);
    } else {
      this.tray.setToolTip('Aura Video Studio');
    }
  }

  /**
   * Show notification balloon (Windows)
   */
  showNotification(title, message, icon = null) {
    if (this.tray && process.platform === 'win32') {
      this.tray.displayBalloon({
        title: title,
        content: message,
        icon: icon,
        iconType: 'info'
      });
    }
  }

  /**
   * Destroy tray
   */
  destroy() {
    if (this.tray) {
      this.tray.destroy();
      this.tray = null;
      console.log('System tray destroyed');
    }
  }

  /**
   * Get tray instance
   */
  getTray() {
    return this.tray;
  }

  /**
   * Build menu template
   */
  _buildMenuTemplate() {
    const template = [
      {
        label: 'Show Aura Studio',
        click: () => {
          this.windowManager.showMainWindow();
        }
      },
      {
        label: 'Hide',
        click: () => {
          this.windowManager.hideMainWindow();
        }
      },
      { type: 'separator' }
    ];

    // Add backend status if available
    if (this.backendUrl) {
      template.push({
        label: `Backend: ${this.backendUrl}`,
        enabled: false
      });
    }

    // Add operation status if available
    if (this.operationStatus) {
      template.push({
        label: `Status: ${this.operationStatus}`,
        enabled: false
      });
      
      // Add cancel operation button if processing
      if (this.operationStatus.toLowerCase().includes('processing') || 
          this.operationStatus.toLowerCase().includes('generating')) {
        template.push({
          label: 'Cancel Current Operation',
          click: () => {
            this._sendToRenderer('tray:cancelOperation');
          }
        });
      }
      
      template.push({ type: 'separator' });
    }

    // Quick actions
    template.push(
      {
        label: 'New Project',
        click: () => {
          this.windowManager.showMainWindow();
          this._sendToRenderer('tray:newProject');
        }
      },
      {
        label: 'Open Project',
        click: () => {
          this.windowManager.showMainWindow();
          this._sendToRenderer('tray:openProject');
        }
      },
      { type: 'separator' }
    );

    // Settings and tools
    template.push(
      {
        label: 'Settings',
        click: () => {
          this.windowManager.showMainWindow();
          this._sendToRenderer('tray:openSettings');
        }
      },
      { type: 'separator' }
    );

    // Updates and help
    if (!this.isDev) {
      template.push(
        {
          label: 'Check for Updates',
          click: () => {
            this._sendToRenderer('tray:checkForUpdates');
          }
        },
        { type: 'separator' }
      );
    }

    // App info and version
    template.push(
      {
        label: `Version ${this.app.getVersion()}`,
        enabled: false
      },
      { type: 'separator' }
    );

    // Quit
    template.push({
      label: 'Quit Aura Studio',
      click: () => {
        this.app.quit();
      }
    });

    return template;
  }

  /**
   * Get tray icon path
   */
  _getTrayIconPath() {
    // Try to find tray-specific icon first
    let iconPath = path.join(__dirname, '../assets', 'icons', 'tray.png');
    if (fs.existsSync(iconPath)) {
      return iconPath;
    }

    // Fallback to main icon
    if (process.platform === 'win32') {
      iconPath = path.join(__dirname, '../assets', 'icons', 'icon.ico');
    } else if (process.platform === 'darwin') {
      iconPath = path.join(__dirname, '../assets', 'icons', 'icon.icns');
    } else {
      iconPath = path.join(__dirname, '../assets', 'icons', 'icon.png');
    }

    if (fs.existsSync(iconPath)) {
      return iconPath;
    }

    return null;
  }

  /**
   * Send event to renderer process
   */
  _sendToRenderer(channel, data = {}) {
    const window = this.windowManager.getMainWindow();
    if (window && !window.isDestroyed()) {
      window.webContents.send(channel, data);
    }
  }
}

module.exports = TrayManager;
