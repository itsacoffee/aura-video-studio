/**
 * Tray Manager Module
 * Manages Windows system tray integration
 */

const { Tray, Menu, nativeImage } = require('electron');
const path = require('path');
const fs = require('fs');
const { getFallbackIcon } = require('./icon-fallbacks');

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
    
    console.log('=== Tray Icon Resolution ===');
    console.log('Resolved tray icon path:', trayIconPath);
    
    let icon = null;
    
    // Try to load icon from file path
    if (trayIconPath && fs.existsSync(trayIconPath)) {
      console.log('✓ Tray icon file exists at:', trayIconPath);
      
      try {
        icon = nativeImage.createFromPath(trayIconPath);
        
        if (icon.isEmpty()) {
          console.warn('⚠ Tray icon loaded but is empty, will use fallback');
          icon = null;
        } else {
          console.log('✓ Tray icon loaded successfully from file');
        }
      } catch (error) {
        console.error('✗ Failed to load tray icon from file:', error.message);
        console.error('Error stack:', error.stack);
        icon = null;
      }
    } else {
      console.warn('⚠ Tray icon file not found at:', trayIconPath);
    }
    
    // Use fallback icon if file loading failed
    if (!icon) {
      console.log('Using base64 fallback icon for tray');
      icon = getFallbackIcon(nativeImage, '32');
      
      if (icon.isEmpty()) {
        console.error('✗ Even fallback icon is empty, tray creation will fail');
        console.warn('Tray will not be created. This is optional and the app will continue.');
        return null;
      }
    }

    try {
      this.tray = new Tray(icon);
      console.log('✓ System tray created successfully');
    } catch (error) {
      console.error('✗ Failed to create system tray:', error.message);
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

    console.log('System tray configured successfully');
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
   * Get tray icon path with comprehensive path resolution
   */
  _getTrayIconPath() {
    console.log('=== Tray Icon Path Resolution Debug ===');
    console.log('Platform:', process.platform);
    console.log('Is packaged:', this.app.isPackaged);
    console.log('__dirname:', __dirname);
    console.log('app.getAppPath():', this.app.getAppPath());
    
    const iconPaths = [];
    
    // Tray-specific icon name (small, optimized for tray)
    const trayIconName = 'tray.png';
    
    // Fallback icon names by platform
    let fallbackIconName;
    if (process.platform === 'win32') {
      fallbackIconName = 'icon.ico';
    } else if (process.platform === 'darwin') {
      fallbackIconName = 'icon.icns';
    } else {
      fallbackIconName = 'icon.png';
    }
    
    if (this.app.isPackaged) {
      // Production: Try resources path
      iconPaths.push(path.join(process.resourcesPath, 'assets', 'icons', trayIconName));
      iconPaths.push(path.join(process.resourcesPath, 'assets', 'icons', fallbackIconName));
      iconPaths.push(path.join(process.resourcesPath, 'app.asar.unpacked', 'assets', 'icons', trayIconName));
      iconPaths.push(path.join(process.resourcesPath, 'app.asar.unpacked', 'assets', 'icons', fallbackIconName));
    } else {
      // Development: Try relative paths
      iconPaths.push(path.join(__dirname, '../assets', 'icons', trayIconName));
      iconPaths.push(path.join(__dirname, '../assets', 'icons', fallbackIconName));
      iconPaths.push(path.join(process.cwd(), 'Aura.Desktop', 'assets', 'icons', trayIconName));
      iconPaths.push(path.join(process.cwd(), 'Aura.Desktop', 'assets', 'icons', fallbackIconName));
    }
    
    // Try each path
    for (const iconPath of iconPaths) {
      console.log('Checking tray icon path:', iconPath);
      
      if (fs.existsSync(iconPath)) {
        console.log('✓ Found tray icon at:', iconPath);
        return iconPath;
      }
    }
    
    console.warn('⚠ No tray icon file found, will use base64 fallback');
    console.log('Searched paths:', iconPaths);
    
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
