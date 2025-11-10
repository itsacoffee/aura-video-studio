const { app, BrowserWindow, ipcMain, Tray, Menu, dialog, shell } = require('electron');
const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const net = require('net');
const { autoUpdater } = require('electron-updater');
const Store = require('electron-store');
const axios = require('axios');

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
let backendProcess = null;
let backendPort = null;
let isQuitting = false;

// Constants
const IS_DEV = process.argv.includes('--dev') || !app.isPackaged;
const BACKEND_STARTUP_TIMEOUT = 60000; // 60 seconds
const HEALTH_CHECK_INTERVAL = 1000; // 1 second

/**
 * Find an available port for the backend server
 */
async function findAvailablePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.unref();
    server.on('error', reject);
    server.listen(0, () => {
      const { port } = server.address();
      server.close(() => resolve(port));
    });
  });
}

/**
 * Wait for the backend to become healthy
 */
async function waitForBackend(port, maxAttempts = 60) {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const response = await axios.get(`http://localhost:${port}/health`, {
        timeout: 2000,
        validateStatus: () => true
      });
      
      if (response.status === 200) {
        console.log(`Backend is healthy at http://localhost:${port}`);
        return true;
      }
    } catch (error) {
      // Backend not ready yet, continue waiting
    }
    
    // Wait before next attempt
    await new Promise(resolve => setTimeout(resolve, HEALTH_CHECK_INTERVAL));
    
    if (i % 10 === 0 && i > 0) {
      console.log(`Still waiting for backend... (attempt ${i}/${maxAttempts})`);
    }
  }
  
  throw new Error(`Backend failed to start within ${BACKEND_STARTUP_TIMEOUT}ms`);
}

/**
 * Get the path to bundled FFmpeg binaries
 */
function getFFmpegPath() {
  let ffmpegBinPath;
  
  if (IS_DEV) {
    // In development, look for FFmpeg in resources directory
    const platform = process.platform;
    if (platform === 'win32') {
      ffmpegBinPath = path.join(__dirname, 'resources', 'ffmpeg', 'win-x64', 'bin');
    } else if (platform === 'darwin') {
      ffmpegBinPath = path.join(__dirname, 'resources', 'ffmpeg', 'osx-x64', 'bin');
    } else {
      ffmpegBinPath = path.join(__dirname, 'resources', 'ffmpeg', 'linux-x64', 'bin');
    }
  } else {
    // In production, use the bundled FFmpeg from resources
    const platform = process.platform;
    if (platform === 'win32') {
      ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'win-x64', 'bin');
    } else if (platform === 'darwin') {
      ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'osx-x64', 'bin');
    } else {
      ffmpegBinPath = path.join(process.resourcesPath, 'ffmpeg', 'linux-x64', 'bin');
    }
  }
  
  return ffmpegBinPath;
}

/**
 * Verify FFmpeg installation
 */
function verifyFFmpeg(ffmpegPath) {
  const ffmpegExe = process.platform === 'win32' ? 'ffmpeg.exe' : 'ffmpeg';
  const ffmpegFullPath = path.join(ffmpegPath, ffmpegExe);
  
  if (!fs.existsSync(ffmpegFullPath)) {
    console.warn(`FFmpeg not found at: ${ffmpegFullPath}`);
    return false;
  }
  
  console.log('FFmpeg found at:', ffmpegFullPath);
  return true;
}

/**
 * Start the bundled ASP.NET Core backend
 */
async function startBackend() {
  try {
    // Find available port
    backendPort = await findAvailablePort();
    console.log(`Starting backend on port ${backendPort}...`);
    
    // Determine backend executable path
    let backendPath;
    if (IS_DEV) {
      // In development, use the compiled backend from Aura.Api/bin
      const platform = process.platform;
      if (platform === 'win32') {
        backendPath = path.join(__dirname, '../Aura.Api/bin/Debug/net8.0/Aura.Api.exe');
      } else {
        backendPath = path.join(__dirname, '../Aura.Api/bin/Debug/net8.0/Aura.Api');
      }
    } else {
      // In production, use the bundled backend from resources
      if (process.platform === 'win32') {
        backendPath = path.join(process.resourcesPath, 'backend', 'win-x64', 'Aura.Api.exe');
      } else if (process.platform === 'darwin') {
        backendPath = path.join(process.resourcesPath, 'backend', 'osx-x64', 'Aura.Api');
      } else {
        backendPath = path.join(process.resourcesPath, 'backend', 'linux-x64', 'Aura.Api');
      }
    }
    
    // Check if backend executable exists
    if (!fs.existsSync(backendPath)) {
      throw new Error(`Backend executable not found at: ${backendPath}`);
    }
    
    // Make executable on Unix-like systems
    if (process.platform !== 'win32') {
      try {
        fs.chmodSync(backendPath, 0o755);
      } catch (error) {
        console.warn('Failed to make backend executable:', error.message);
      }
    }
    
    // Get FFmpeg path
    const ffmpegPath = getFFmpegPath();
    const ffmpegExists = verifyFFmpeg(ffmpegPath);
    
    if (!ffmpegExists) {
      console.warn('FFmpeg not found - video rendering may not work');
    }
    
    // Prepare environment variables
    const env = {
      ...process.env,
      ASPNETCORE_URLS: `http://localhost:${backendPort}`,
      DOTNET_ENVIRONMENT: IS_DEV ? 'Development' : 'Production',
      ASPNETCORE_DETAILEDERRORS: IS_DEV ? 'true' : 'false',
      LOGGING__LOGLEVEL__DEFAULT: IS_DEV ? 'Debug' : 'Information',
      // Set paths for user data
      AURA_DATA_PATH: app.getPath('userData'),
      AURA_LOGS_PATH: path.join(app.getPath('userData'), 'logs'),
      AURA_TEMP_PATH: path.join(app.getPath('temp'), 'aura-video-studio'),
      // Set FFmpeg path for backend
      FFMPEG_PATH: ffmpegPath,
      FFMPEG_BINARIES_PATH: ffmpegPath
    };
    
    // Create necessary directories
    const directories = [env.AURA_DATA_PATH, env.AURA_LOGS_PATH, env.AURA_TEMP_PATH];
    directories.forEach(dir => {
      if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
      }
    });
    
    console.log('Backend executable:', backendPath);
    console.log('Backend port:', backendPort);
    console.log('Environment:', env.DOTNET_ENVIRONMENT);
    console.log('FFmpeg path:', ffmpegPath);
    
    // Spawn backend process
    backendProcess = spawn(backendPath, [], { 
      env,
      stdio: ['ignore', 'pipe', 'pipe']
    });
    
    // Handle backend output
    backendProcess.stdout.on('data', (data) => {
      const message = data.toString().trim();
      if (message) {
        console.log(`[Backend] ${message}`);
      }
    });
    
    backendProcess.stderr.on('data', (data) => {
      const message = data.toString().trim();
      if (message) {
        console.error(`[Backend Error] ${message}`);
      }
    });
    
    // Handle backend exit
    backendProcess.on('exit', (code, signal) => {
      console.log(`Backend exited with code ${code} and signal ${signal}`);
      
      if (!isQuitting && code !== 0) {
        // Backend crashed unexpectedly
        dialog.showErrorBox(
          'Backend Error',
          `The Aura backend server has stopped unexpectedly (exit code: ${code}). The application will now close.`
        );
        app.quit();
      }
    });
    
    backendProcess.on('error', (error) => {
      console.error('Failed to start backend:', error);
      throw error;
    });
    
    // Wait for backend to be ready
    await waitForBackend(backendPort);
    
    console.log('Backend started successfully');
    return backendPort;
    
  } catch (error) {
    console.error('Backend startup error:', error);
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
    return `http://localhost:${backendPort}`;
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
function cleanup() {
  console.log('Cleaning up...');
  
  // Kill backend process
  if (backendProcess && !backendProcess.killed) {
    console.log('Stopping backend...');
    backendProcess.kill();
    backendProcess = null;
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

app.on('before-quit', (event) => {
  isQuitting = true;
  cleanup();
});

app.on('will-quit', () => {
  cleanup();
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
