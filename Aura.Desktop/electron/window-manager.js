/**
 * Window Manager Module
 * Handles window creation, state persistence, and lifecycle management
 */

const { BrowserWindow, screen, nativeImage } = require('electron');
const path = require('path');
const fs = require('fs');
const Store = require('electron-store');
const { getFallbackIcon } = require('./icon-fallbacks');

class WindowManager {
  constructor(app, isDev) {
    this.app = app;
    this.isDev = isDev;
    this.mainWindow = null;
    this.splashWindow = null;
    
    // Window state persistence
    this.windowStateStore = new Store({
      name: 'window-state',
      defaults: {
        mainWindow: {
          width: 1920,
          height: 1080,
          x: undefined,
          y: undefined,
          isMaximized: false
        }
      }
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
        nodeIntegration: false,
        contextIsolation: true,
        sandbox: true
      }
    });

    const splashPath = path.join(__dirname, '../assets', 'splash.html');
    if (fs.existsSync(splashPath)) {
      this.splashWindow.loadFile(splashPath);
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
  createMainWindow(backendPort, preloadPath) {
    // Restore window state
    const savedState = this.windowStateStore.get('mainWindow');
    const primaryDisplay = screen.getPrimaryDisplay();
    const { width: screenWidth, height: screenHeight } = primaryDisplay.workAreaSize;

    // Validate saved position is still within screen bounds
    let x = savedState.x;
    let y = savedState.y;
    if (x !== undefined && y !== undefined) {
      const visible = screen.getAllDisplays().some(display => {
        return x >= display.bounds.x && 
               x < display.bounds.x + display.bounds.width &&
               y >= display.bounds.y && 
               y < display.bounds.y + display.bounds.height;
      });
      if (!visible) {
        x = undefined;
        y = undefined;
      }
    }

    // Create window with saved or default state
    this.mainWindow = new BrowserWindow({
      width: savedState.width,
      height: savedState.height,
      minWidth: 1280,
      minHeight: 720,
      x: x,
      y: y,
      show: false, // Don't show until ready-to-show event
      backgroundColor: '#0F0F0F',
      icon: this._getAppIcon(),
      webPreferences: {
        preload: preloadPath,
        contextIsolation: true,
        nodeIntegration: false,
        sandbox: true,
        webSecurity: !this.isDev,
        devTools: true, // Allow devtools but don't open by default
        enableRemoteModule: false, // Remote module is deprecated
        spellcheck: true
      },
      titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'default',
      frame: true,
      autoHideMenuBar: false
    });

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
          this.windowStateStore.set('mainWindow', {
            width: bounds.width,
            height: bounds.height,
            x: bounds.x,
            y: bounds.y,
            isMaximized: false
          });
        } else {
          this.windowStateStore.set('mainWindow.isMaximized', true);
        }
      }
    };

    // Debounce window state saving
    let saveStateTimeout;
    const debouncedSave = () => {
      clearTimeout(saveStateTimeout);
      saveStateTimeout = setTimeout(saveWindowState, 500);
    };

    this.mainWindow.on('resize', debouncedSave);
    this.mainWindow.on('move', debouncedSave);
    this.mainWindow.on('maximize', saveWindowState);
    this.mainWindow.on('unmaximize', saveWindowState);

    // Set Content Security Policy
    this.mainWindow.webContents.session.webRequest.onHeadersReceived((details, callback) => {
      callback({
        responseHeaders: {
          ...details.responseHeaders,
          'Content-Security-Policy': this._getCSP()
        }
      });
    });

    // Handle navigation - prevent navigation away from app
    this.mainWindow.webContents.on('will-navigate', (event, url) => {
      // Allow navigation within the app
      if (!url.startsWith('file://') && !url.startsWith('http://localhost')) {
        event.preventDefault();
        console.warn('Navigation blocked:', url);
      }
    });

    // Prevent new window creation
    this.mainWindow.webContents.setWindowOpenHandler(({ url }) => {
      // Open in external browser
      require('electron').shell.openExternal(url);
      return { action: 'deny' };
    });

    // Load frontend
    const frontendPath = this._getFrontendPath();
    console.log('Loading frontend from:', frontendPath);

    this.mainWindow.loadFile(frontendPath).then(() => {
      // Inject backend URL and environment info
      this.mainWindow.webContents.executeJavaScript(`
        window.AURA_BACKEND_URL = 'http://localhost:${backendPort}';
        window.AURA_IS_ELECTRON = true;
        window.AURA_IS_DEV = ${this.isDev};
        window.AURA_VERSION = '${this.app.getVersion()}';
      `);
    }).catch(error => {
      console.error('Failed to load frontend:', error);
      throw error;
    });

    // Show window when ready
    this.mainWindow.once('ready-to-show', () => {
      this.mainWindow.show();
      
      // Close splash screen
      if (this.splashWindow && !this.splashWindow.isDestroyed()) {
        this.splashWindow.close();
        this.splashWindow = null;
      }

      // Open DevTools in development mode
      if (this.isDev) {
        this.mainWindow.webContents.openDevTools({ mode: 'detach' });
      }
    });

    this.mainWindow.on('closed', () => {
      this.mainWindow = null;
    });

    return this.mainWindow;
  }

  /**
   * Handle window close event
   */
  handleWindowClose(event, isQuitting, minimizeToTray = true) {
    if (!isQuitting && minimizeToTray && process.platform === 'win32') {
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
      return [
        "default-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:* ws://localhost:*",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:*",
        "style-src 'self' 'unsafe-inline' http://localhost:*",
        "img-src 'self' data: blob: http://localhost:*",
        "font-src 'self' data:",
        "connect-src 'self' http://localhost:* ws://localhost:*",
        "media-src 'self' blob: http://localhost:*"
      ].join('; ');
    } else {
      // Strict CSP for production
      return [
        "default-src 'self'",
        "script-src 'self'",
        "style-src 'self' 'unsafe-inline'", // Allow inline styles for React
        "img-src 'self' data: blob:",
        "font-src 'self' data:",
        "connect-src 'self' http://localhost:*",
        "media-src 'self' blob:",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'",
        "upgrade-insecure-requests"
      ].join('; ');
    }
  }

  /**
   * Get the path to the app icon with comprehensive logging and fallback
   */
  _getAppIcon() {
    const iconName = process.platform === 'win32' ? 'icon.ico' : 
                     process.platform === 'darwin' ? 'icon.icns' : 
                     'icon.png';
    
    // Log environment info for debugging
    console.log('=== Icon Resolution Debug Info ===');
    console.log('Platform:', process.platform);
    console.log('Is packaged:', this.app.isPackaged);
    console.log('__dirname:', __dirname);
    console.log('app.getAppPath():', this.app.getAppPath());
    console.log('process.resourcesPath:', process.resourcesPath);
    console.log('Icon name:', iconName);
    
    // Try multiple paths in order of preference
    const iconPaths = [];
    
    if (this.app.isPackaged) {
      // Production: Try resources path first (ASAR or unpacked)
      iconPaths.push(path.join(process.resourcesPath, 'assets', 'icons', iconName));
      iconPaths.push(path.join(process.resourcesPath, 'app.asar.unpacked', 'assets', 'icons', iconName));
      iconPaths.push(path.join(this.app.getAppPath(), 'assets', 'icons', iconName));
    } else {
      // Development: Try relative to electron directory
      iconPaths.push(path.join(__dirname, '../assets', 'icons', iconName));
      iconPaths.push(path.join(process.cwd(), 'Aura.Desktop', 'assets', 'icons', iconName));
      iconPaths.push(path.join(process.cwd(), 'assets', 'icons', iconName));
    }
    
    // Try each path and return first that exists
    for (const iconPath of iconPaths) {
      console.log('Trying icon path:', iconPath);
      
      if (fs.existsSync(iconPath)) {
        console.log('✓ Found icon at:', iconPath);
        
        try {
          const icon = nativeImage.createFromPath(iconPath);
          
          if (!icon.isEmpty()) {
            console.log('✓ Icon loaded successfully');
            return icon;
          } else {
            console.warn('⚠ Icon file exists but loaded as empty image:', iconPath);
          }
        } catch (error) {
          console.error('✗ Error loading icon from path:', iconPath, error.message);
        }
      } else {
        console.log('✗ Icon not found at:', iconPath);
      }
    }
    
    // If no icon found, use base64 fallback
    console.warn('⚠ Using fallback icon - no icon files found');
    console.log('Searched paths:', iconPaths);
    
    return getFallbackIcon(nativeImage, '256');
  }

  /**
   * Get the path to the frontend files
   */
  _getFrontendPath() {
    if (this.isDev) {
      return path.join(__dirname, '../../Aura.Web/dist/index.html');
    } else {
      return path.join(process.resourcesPath, 'frontend', 'index.html');
    }
  }
}

module.exports = WindowManager;
