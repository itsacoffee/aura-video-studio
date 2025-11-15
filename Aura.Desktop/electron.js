const { app, BrowserWindow, ipcMain, Tray, Menu, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const { autoUpdater } = require('electron-updater');
const Store = require('electron-store');

// Import modular services
const BackendService = require('./electron/backend-service');

// Configure electron-store
const store = new Store({
  name: 'aura-config',
  encryptionKey: process.env.CONFIG_ENCRYPTION_KEY || 'aura-video-studio-secure-key-v1',
  defaults: {
    setupComplete: false,
    firstRun: true,
    language: 'en',
    theme: 'dark',
    autoUpdate: true,
    telemetry: false,
    crashReporting: false
  }
});

// Global variables
let mainWindow = null;
let splashWindow = null;
let tray = null;
let backendService = null;
let isQuitting = false;

// Constants
const IS_DEV = process.argv.includes('--dev') || !app.isPackaged;

/**
 * Start the backend service using BackendService module
 */
async function startBackend() {
  try {
    backendService = new BackendService(app, IS_DEV);
    const backendPort = await backendService.start();
    
    console.log('Backend started successfully on port:', backendPort);
    return backendPort;
    
  } catch (error) {
    console.error('Backend startup error:', error);
    
    // Provide detailed error message
    let errorDetails = `Failed to start the Aura backend server.\n\n`;
    errorDetails += `Error: ${error.message}\n\n`;
    
    if (error.message.includes('not found')) {
      errorDetails += `The backend executable was not found. Please ensure the application is properly installed.\n\n`;
    } else if (error.message.includes('Backend failed to start within')) {
      errorDetails += `The backend server did not respond to health checks within the timeout period.\n`;
      errorDetails += `This may indicate:\n`;
      errorDetails += `- Port is already in use\n`;
      errorDetails += `- Firewall is blocking the application\n`;
      errorDetails += `- Missing dependencies\n\n`;
    }
    
    errorDetails += `Logs location: ${path.join(app.getPath('userData'), 'logs')}\n`;
    
    dialog.showErrorBox('Startup Error', errorDetails);
    throw error;
  }
}

/**
 * Create splash screen window
 */
function createSplashScreen() {
  splashWindow = new BrowserWindow({
    width: 600,
    height: 400,
    transparent: true,
    frame: false,
    alwaysOnTop: true,
    center: true,
    resizable: false,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    }
  });
  
  const splashPath = path.join(__dirname, 'assets', 'splash.html');
  if (fs.existsSync(splashPath)) {
    splashWindow.loadFile(splashPath);
  } else {
    // Fallback: show blank splash with background color
    splashWindow.loadURL(`data:text/html,
      <html>
        <body style="margin:0;padding:0;background:#0F0F0F;display:flex;align-items:center;justify-content:center;color:#fff;font-family:system-ui;">
          <div style="text-align:center;">
            <h1 style="font-size:32px;margin-bottom:20px;">Aura Video Studio</h1>
            <p style="font-size:16px;opacity:0.8;">Starting up...</p>
          </div>
        </body>
      </html>
    `);
  }
  
  return splashWindow;
}

/**
 * Create main application window
 */
function createMainWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 768,
    show: false, // Don't show until ready
    backgroundColor: '#0F0F0F',
    icon: getAppIcon(),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      webSecurity: !IS_DEV,
      devTools: IS_DEV
    },
    titleBarStyle: process.platform === 'darwin' ? 'hiddenInset' : 'default',
    frame: true
  });
  
  // Load frontend
  const frontendPath = getFrontendPath();
  console.log('Loading frontend from:', frontendPath);
  
  mainWindow.loadFile(frontendPath).then(() => {
    // Inject backend URL and environment info
    const backendPort = backendService.getPort();
    mainWindow.webContents.executeJavaScript(`
      window.AURA_BACKEND_URL = 'http://localhost:${backendPort}';
      window.AURA_IS_ELECTRON = true;
      window.AURA_IS_DEV = ${IS_DEV};
      window.AURA_VERSION = '${app.getVersion()}';
    `);
    
    // Show main window and close splash
    mainWindow.show();
    if (splashWindow && !splashWindow.isDestroyed()) {
      splashWindow.close();
      splashWindow = null;
    }
    
    // Open DevTools in development
    if (IS_DEV) {
      mainWindow.webContents.openDevTools({ mode: 'detach' });
    }
    
    // Check if first run
    checkFirstRun();
    
  }).catch(error => {
    console.error('Failed to load frontend:', error);
    dialog.showErrorBox('Startup Error', `Failed to load application interface: ${error.message}`);
    app.quit();
  });
  
  // Handle window events
  mainWindow.on('close', (event) => {
    if (!isQuitting && process.platform === 'darwin') {
      event.preventDefault();
      mainWindow.hide();
    }
  });
  
  mainWindow.on('closed', () => {
    mainWindow = null;
  });
  
  // Handle external links
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: 'deny' };
  });
  
  return mainWindow;
}

/**
 * Get the path to the app icon
 */
function getAppIcon() {
  const iconName = process.platform === 'win32' ? 'icon.ico' : 
                   process.platform === 'darwin' ? 'icon.icns' : 
                   'icon.png';
  return path.join(__dirname, 'assets', 'icons', iconName);
}

/**
 * Get the path to the frontend files
 */
function getFrontendPath() {
  if (IS_DEV) {
    return path.join(__dirname, '../Aura.Web/dist/index.html');
  } else {
    return path.join(process.resourcesPath, 'frontend', 'index.html');
  }
}

/**
 * Check if this is the first run and navigate to setup wizard
 */
function checkFirstRun() {
  const setupComplete = store.get('setupComplete', false);
  const firstRun = store.get('firstRun', true);
  
  if (!setupComplete || firstRun) {
    console.log('First run detected, navigating to setup wizard...');
    // Navigate to setup wizard route
    mainWindow.webContents.executeJavaScript(`
      if (window.location.hash !== '#/setup') {
        window.location.hash = '#/setup';
      }
    `);
  }
}

/**
 * Create system tray icon and menu
 */
function createSystemTray() {
  const trayIconPath = path.join(__dirname, 'assets', 'icons', 'tray.png');
  
  // Fallback to main icon if tray icon doesn't exist
  const iconPath = fs.existsSync(trayIconPath) ? trayIconPath : getAppIcon();
  
  tray = new Tray(iconPath);
  
  updateTrayMenu();
  
  tray.setToolTip('Aura Video Studio');
  
  // Click tray icon to show/hide window
  tray.on('click', () => {
    if (mainWindow) {
      if (mainWindow.isVisible()) {
        mainWindow.hide();
      } else {
        mainWindow.show();
        mainWindow.focus();
      }
    }
  });
  
  return tray;
}

/**
 * Update system tray menu
 */
function updateTrayMenu() {
  if (!tray) return;
  
  const backendPort = backendService ? backendService.getPort() : 'not running';
  
  const contextMenu = Menu.buildFromTemplate([
    {
      label: 'Show Aura Studio',
      click: () => {
        if (mainWindow) {
          mainWindow.show();
          mainWindow.focus();
        }
      }
    },
    {
      label: 'Hide',
      click: () => {
        if (mainWindow) {
          mainWindow.hide();
        }
      }
    },
    { type: 'separator' },
    {
      label: `Backend: http://localhost:${backendPort}`,
      enabled: false
    },
    { type: 'separator' },
    {
      label: 'Check for Updates',
      click: () => {
        autoUpdater.checkForUpdates();
      }
    },
    {
      label: 'Open Logs Folder',
      click: () => {
        const logsPath = path.join(app.getPath('userData'), 'logs');
        shell.openPath(logsPath);
      }
    },
    { type: 'separator' },
    {
      label: `Version ${app.getVersion()}`,
      enabled: false
    },
    { type: 'separator' },
    {
      label: 'Quit',
      click: () => {
        isQuitting = true;
        app.quit();
      }
    }
  ]);
  
  tray.setContextMenu(contextMenu);
}

/**
 * Setup auto-updater
 */
function setupAutoUpdater() {
  if (IS_DEV) {
    console.log('Auto-updater disabled in development mode');
    return;
  }
  
  // Configure auto-updater
  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;
  
  // Check for updates on startup (if enabled)
  const autoUpdate = store.get('autoUpdate', true);
  if (autoUpdate) {
    setTimeout(() => {
      autoUpdater.checkForUpdates().catch(err => {
        console.error('Update check failed:', err);
      });
    }, 5000);
  }
  
  // Update available
  autoUpdater.on('update-available', (info) => {
    console.log('Update available:', info.version);
    
    dialog.showMessageBox(mainWindow, {
      type: 'info',
      title: 'Update Available',
      message: `A new version (${info.version}) is available!`,
      detail: 'Would you like to download it now? The update will be installed when you restart the application.',
      buttons: ['Download', 'Later'],
      defaultId: 0,
      cancelId: 1
    }).then((result) => {
      if (result.response === 0) {
        autoUpdater.downloadUpdate();
      }
    });
  });
  
  // Update not available
  autoUpdater.on('update-not-available', () => {
    console.log('Application is up to date');
  });
  
  // Download progress
  autoUpdater.on('download-progress', (progressObj) => {
    const percent = Math.round(progressObj.percent);
    console.log(`Download progress: ${percent}%`);
    
    // Update window title with progress
    if (mainWindow) {
      mainWindow.setTitle(`Aura Video Studio - Downloading update: ${percent}%`);
    }
  });
  
  // Update downloaded
  autoUpdater.on('update-downloaded', (info) => {
    console.log('Update downloaded:', info.version);
    
    // Reset window title
    if (mainWindow) {
      mainWindow.setTitle('Aura Video Studio');
    }
    
    dialog.showMessageBox(mainWindow, {
      type: 'info',
      title: 'Update Ready',
      message: `Version ${info.version} has been downloaded.`,
      detail: 'The update will be installed when you restart Aura Video Studio. Would you like to restart now?',
      buttons: ['Restart Now', 'Later'],
      defaultId: 0,
      cancelId: 1
    }).then((result) => {
      if (result.response === 0) {
        isQuitting = true;
        autoUpdater.quitAndInstall();
      }
    });
  });
  
  // Update error
  autoUpdater.on('error', (error) => {
    console.error('Auto-updater error:', error);
  });
}

/**
 * Initialize IPC handlers
 */
function setupIpcHandlers() {
  // Configuration management
  ipcMain.handle('config:get', (event, key) => {
    return store.get(key);
  });
  
  ipcMain.handle('config:set', (event, key, value) => {
    store.set(key, value);
    return true;
  });
  
  ipcMain.handle('config:getAll', () => {
    return store.store;
  });
  
  ipcMain.handle('config:reset', () => {
    store.clear();
    return true;
  });
  
  // File/folder dialogs
  ipcMain.handle('dialog:openFolder', async () => {
    const result = await dialog.showOpenDialog(mainWindow, {
      properties: ['openDirectory', 'createDirectory']
    });
    return result.filePaths[0];
  });
  
  ipcMain.handle('dialog:openFile', async (event, options = {}) => {
    const result = await dialog.showOpenDialog(mainWindow, {
      properties: ['openFile'],
      filters: options.filters || []
    });
    return result.filePaths[0];
  });
  
  ipcMain.handle('dialog:saveFile', async (event, options = {}) => {
    const result = await dialog.showSaveDialog(mainWindow, {
      filters: options.filters || [],
      defaultPath: options.defaultPath
    });
    return result.filePath;
  });
  
  // Shell operations
  ipcMain.handle('shell:openExternal', async (event, url) => {
    await shell.openExternal(url);
    return true;
  });
  
  ipcMain.handle('shell:openPath', async (event, path) => {
    await shell.openPath(path);
    return true;
  });
  
  // App info
  ipcMain.handle('app:getVersion', () => {
    return app.getVersion();
  });
  
  ipcMain.handle('app:getPaths', () => {
    return {
      userData: app.getPath('userData'),
      temp: app.getPath('temp'),
      home: app.getPath('home'),
      documents: app.getPath('documents'),
      videos: app.getPath('videos'),
      downloads: app.getPath('downloads')
    };
  });
  
  ipcMain.handle('app:getBackendUrl', () => {
    return backendService ? backendService.getUrl() : null;
  });
  
  // Backend management
  ipcMain.handle('backend:getUrl', () => {
    return backendService ? backendService.getUrl() : null;
  });
  
  ipcMain.handle('backend:status', () => {
    if (!backendService) {
      return { running: false, port: null };
    }
    return {
      running: backendService.isRunning(),
      port: backendService.getPort(),
      url: backendService.getUrl()
    };
  });
  
  ipcMain.handle('backend:restart', async () => {
    if (!backendService) {
      throw new Error('Backend service not initialized');
    }
    await backendService.restart();
    return { success: true, url: backendService.getUrl() };
  });
  
  ipcMain.handle('backend:checkFirewall', async () => {
    if (!backendService) {
      throw new Error('Backend service not initialized');
    }
    return await backendService.checkFirewallCompatibility();
  });
  
  ipcMain.handle('backend:getFirewallRule', async () => {
    if (!backendService) {
      throw new Error('Backend service not initialized');
    }
    return await backendService.getFirewallRuleStatus();
  });
  
  ipcMain.handle('backend:getFirewallCommand', () => {
    if (!backendService) {
      throw new Error('Backend service not initialized');
    }
    return backendService.getFirewallRuleCommand();
  });
  
  // Update checking
  ipcMain.handle('updates:check', async () => {
    if (IS_DEV) {
      return { available: false, message: 'Updates disabled in development' };
    }
    const result = await autoUpdater.checkForUpdates();
    return result;
  });
  
  // Restart app
  ipcMain.handle('app:restart', () => {
    isQuitting = true;
    app.relaunch();
    app.exit();
  });
}

/**
 * Cleanup before quit
 */
async function cleanup() {
  console.log('Cleaning up...');
  
  // Stop backend service
  if (backendService) {
    console.log('Stopping backend service...');
    try {
      await backendService.stop();
    } catch (error) {
      console.error('Error stopping backend service:', error);
    }
    backendService = null;
  }
  
  // Cleanup temp files
  const tempPath = path.join(app.getPath('temp'), 'aura-video-studio');
  if (fs.existsSync(tempPath)) {
    try {
      fs.rmSync(tempPath, { recursive: true, force: true });
    } catch (error) {
      console.warn('Failed to cleanup temp files:', error.message);
    }
  }
}

// ========================================
// Application Lifecycle
// ========================================

app.whenReady().then(async () => {
  try {
    console.log('='.repeat(60));
    console.log('Aura Video Studio Starting...');
    console.log('='.repeat(60));
    console.log('Version:', app.getVersion());
    console.log('Platform:', process.platform);
    console.log('Architecture:', process.arch);
    console.log('Development Mode:', IS_DEV);
    console.log('User Data:', app.getPath('userData'));
    console.log('='.repeat(60));
    
    // Show splash screen
    createSplashScreen();
    
    // Start backend
    await startBackend();
    
    // Setup IPC handlers
    setupIpcHandlers();
    
    // Create main window
    createMainWindow();
    
    // Create system tray
    createSystemTray();
    
    // Setup auto-updater
    setupAutoUpdater();
    
    console.log('='.repeat(60));
    console.log('Aura Video Studio Started Successfully!');
    console.log('='.repeat(60));
    
  } catch (error) {
    console.error('Startup failed:', error);
    
    // Close splash if it exists
    if (splashWindow && !splashWindow.isDestroyed()) {
      splashWindow.close();
    }
    
    dialog.showErrorBox(
      'Startup Error',
      `Failed to start Aura Video Studio:\n\n${error.message}\n\nPlease check the logs for more information.`
    );
    
    app.quit();
  }
});

app.on('activate', () => {
  // On macOS, re-create window when dock icon is clicked
  if (mainWindow === null && !isQuitting) {
    createMainWindow();
  } else if (mainWindow) {
    mainWindow.show();
  }
});

app.on('window-all-closed', () => {
  // On macOS, apps stay active until user quits explicitly
  if (process.platform !== 'darwin') {
    isQuitting = true;
    app.quit();
  }
});

// Track if cleanup has been initiated to prevent multiple cleanup attempts
let cleanupInitiated = false;

app.on('before-quit', (event) => {
  // Only prevent quit once to perform cleanup
  if (!cleanupInitiated) {
    event.preventDefault();
    cleanupInitiated = true;
    isQuitting = true;
    
    console.log('App quit requested, performing cleanup...');
    
    // Set a timeout to force quit if cleanup takes too long
    // Backend service max timeout is 3 seconds (2s graceful + 1s force)
    // Set app force quit to 5 seconds to allow backend proper cleanup time plus buffer
    const forceQuitTimeout = setTimeout(() => {
      console.warn('Cleanup timeout reached, forcing quit...');
      process.exit(0);
    }, 5000);
    
    // Perform async cleanup
    cleanup()
      .then(() => {
        console.log('Cleanup completed successfully');
        clearTimeout(forceQuitTimeout);
        app.quit();
      })
      .catch((error) => {
        console.error('Cleanup failed:', error);
        clearTimeout(forceQuitTimeout);
        app.quit();
      });
  }
});

// Handle crashes
process.on('uncaughtException', (error) => {
  console.error('Uncaught exception:', error);
  
  const crashReporting = store.get('crashReporting', false);
  if (crashReporting) {
    // Send crash report (implement your crash reporting service)
    console.log('Crash report would be sent here');
  }
});

process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled rejection at:', promise, 'reason:', reason);
});

console.log('Electron main process loaded');
