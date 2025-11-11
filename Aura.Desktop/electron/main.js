/**
 * Aura Video Studio - Electron Main Process
 * 
 * This is the main entry point for the Electron application.
 * It orchestrates all modules and manages the application lifecycle.
 */

const { app, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const { autoUpdater } = require('electron-updater');

// Import application modules
const WindowManager = require('./window-manager');
const AppConfig = require('./app-config');
const BackendService = require('./backend-service');
const TrayManager = require('./tray-manager');
const MenuBuilder = require('./menu-builder');
const ProtocolHandler = require('./protocol-handler');

// Import IPC handlers
const ConfigHandler = require('./ipc-handlers/config-handler');
const SystemHandler = require('./ipc-handlers/system-handler');
const VideoHandler = require('./ipc-handlers/video-handler');
const BackendHandler = require('./ipc-handlers/backend-handler');
const FFmpegHandler = require('./ipc-handlers/ffmpeg-handler');

// ========================================
// Global State
// ========================================

let windowManager = null;
let appConfig = null;
let backendService = null;
let trayManager = null;
let menuBuilder = null;
let protocolHandler = null;

let ipcHandlers = {
  config: null,
  system: null,
  video: null,
  backend: null,
  ffmpeg: null
};

let isQuitting = false;
let isCleaningUp = false;
let crashCount = 0;
const MAX_CRASH_COUNT = 3;

// ========================================
// Environment Configuration
// ========================================

const IS_DEV = process.argv.includes('--dev') || !app.isPackaged;
const APP_NAME = 'Aura Video Studio';

// Disable hardware acceleration if configured (helps with some Windows issues)
if (process.env.DISABLE_HARDWARE_ACCELERATION === 'true') {
  app.disableHardwareAcceleration();
}

// ========================================
// Single Instance Lock
// ========================================

const gotTheLock = app.requestSingleInstanceLock();

if (!gotTheLock) {
  console.log('Another instance is already running. Exiting...');
  app.quit();
} else {
  app.on('second-instance', (event, commandLine, workingDirectory) => {
    console.log('Second instance detected, focusing main window...');
    
    // Focus main window
    if (windowManager) {
      const mainWindow = windowManager.getMainWindow();
      if (mainWindow) {
        if (mainWindow.isMinimized()) mainWindow.restore();
        mainWindow.focus();
      }
    }

    // Handle protocol URL if present
    const protocolUrl = commandLine.find(arg => arg.startsWith('aura://'));
    if (protocolUrl && protocolHandler) {
      protocolHandler.handleProtocolUrl(protocolUrl);
    }
  });
}

// ========================================
// Error Handling & Logging
// ========================================

/**
 * Setup error handling
 */
function setupErrorHandling() {
  // Handle uncaught exceptions
  process.on('uncaughtException', (error) => {
    console.error('Uncaught exception:', error);
    
    crashCount++;
    
    // Log error
    logError('UncaughtException', error);
    
    // Show error dialog
    if (crashCount < MAX_CRASH_COUNT) {
      dialog.showErrorBox(
        'Application Error',
        `An unexpected error occurred:\n\n${error.message}\n\n` +
        `The application will attempt to continue, but you may need to restart.\n` +
        `If this problem persists, please report it.\n\n` +
        `Error logs: ${path.join(app.getPath('userData'), 'logs')}`
      );
    } else {
      dialog.showErrorBox(
        'Critical Error',
        `Multiple critical errors have occurred.\n\n` +
        `The application will now close to prevent data corruption.\n\n` +
        `Error logs: ${path.join(app.getPath('userData'), 'logs')}\n\n` +
        `Please report this issue on GitHub.`
      );
      app.quit();
    }
  });

  // Handle unhandled promise rejections
  process.on('unhandledRejection', (reason, promise) => {
    console.error('Unhandled rejection at:', promise, 'reason:', reason);
    
    // Log error
    logError('UnhandledRejection', reason);
  });

  // Handle process warnings
  process.on('warning', (warning) => {
    console.warn('Process warning:', warning);
  });
}

/**
 * Log error to file
 */
function logError(type, error) {
  try {
    const logsDir = path.join(app.getPath('userData'), 'logs');
    if (!fs.existsSync(logsDir)) {
      fs.mkdirSync(logsDir, { recursive: true });
    }

    const logFile = path.join(logsDir, `crash-${Date.now()}.log`);
    const errorInfo = {
      type,
      timestamp: new Date().toISOString(),
      message: error.message || String(error),
      stack: error.stack || 'No stack trace available',
      platform: process.platform,
      arch: process.arch,
      electronVersion: process.versions.electron,
      nodeVersion: process.versions.node,
      appVersion: app.getVersion()
    };

    fs.writeFileSync(logFile, JSON.stringify(errorInfo, null, 2));
    console.log('Error logged to:', logFile);
  } catch (logError) {
    console.error('Failed to log error:', logError);
  }
}

// ========================================
// Auto Updater Configuration
// ========================================

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

  // Update available
  autoUpdater.on('update-available', (info) => {
    console.log('Update available:', info.version);

    const mainWindow = windowManager.getMainWindow();
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

    const mainWindow = windowManager.getMainWindow();
    if (mainWindow) {
      mainWindow.setTitle(`${APP_NAME} - Downloading update: ${percent}%`);
    }
  });

  // Update downloaded
  autoUpdater.on('update-downloaded', (info) => {
    console.log('Update downloaded:', info.version);

    const mainWindow = windowManager.getMainWindow();
    if (mainWindow) {
      mainWindow.setTitle(APP_NAME);
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

  // Check for updates on startup (if enabled)
  const autoUpdate = appConfig ? appConfig.get('autoUpdate', true) : true;
  if (autoUpdate) {
    setTimeout(() => {
      autoUpdater.checkForUpdates().catch(err => {
        console.error('Update check failed:', err);
      });
    }, 5000);
  }
}

// ========================================
// IPC Handlers Registration
// ========================================

/**
 * Register all IPC handlers
 */
function registerIpcHandlers() {
  console.log('Registering IPC handlers...');

  // Config handler
  ipcHandlers.config = new ConfigHandler(appConfig);
  ipcHandlers.config.register();

  // System handler
  ipcHandlers.system = new SystemHandler(app, windowManager, appConfig);
  ipcHandlers.system.register();

  // Video handler
  const backendUrl = backendService.getUrl();
  ipcHandlers.video = new VideoHandler(backendUrl);
  ipcHandlers.video.register();

  // Backend handler (pass backendService for control operations)
  ipcHandlers.backend = new BackendHandler(backendUrl, backendService);
  ipcHandlers.backend.register();

  // Start backend health checks
  const mainWindow = windowManager.getMainWindow();
  if (mainWindow) {
    ipcHandlers.backend.startHealthChecks(mainWindow);
  }

  // FFmpeg handler
  ipcHandlers.ffmpeg = new FFmpegHandler(app, windowManager);
  ipcHandlers.ffmpeg.register();

  console.log('IPC handlers registered successfully');
}

// ========================================
// First Run Check
// ========================================

/**
 * Check if this is the first run and navigate to setup wizard
 */
function checkFirstRun() {
  const setupComplete = appConfig.isSetupComplete();
  const firstRun = appConfig.isFirstRun();

  if (!setupComplete || firstRun) {
    console.log('First run detected, navigating to setup wizard...');
    
    const mainWindow = windowManager.getMainWindow();
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.executeJavaScript(`
        if (window.location.hash !== '#/setup') {
          window.location.hash = '#/setup';
        }
      `);
    }
  }
}

// ========================================
// Cleanup
// ========================================

/**
 * Cleanup before quit
 */
async function cleanup() {
  console.log('Cleaning up application resources...');

  try {
    // Stop backend health checks
    if (ipcHandlers.backend) {
      ipcHandlers.backend.stopHealthChecks();
    }

    // Stop backend service (now async for proper Windows process termination)
    if (backendService) {
      console.log('Stopping backend service...');
      await backendService.stop();
      console.log('Backend service stopped');
    }

    // Destroy tray
    if (trayManager) {
      trayManager.destroy();
    }

    // Cleanup temp files
    const tempPath = path.join(app.getPath('temp'), 'aura-video-studio');
    if (fs.existsSync(tempPath)) {
      try {
        fs.rmSync(tempPath, { recursive: true, force: true });
        console.log('Temp files cleaned up');
      } catch (error) {
        console.warn('Failed to cleanup temp files:', error.message);
      }
    }

    console.log('Cleanup completed');
  } catch (error) {
    console.error('Error during cleanup:', error);
  }
}

// ========================================
// Application Lifecycle
// ========================================

/**
 * Main application startup
 */
async function startApplication() {
  try {
    console.log('='.repeat(60));
    console.log(`${APP_NAME} Starting...`);
    console.log('='.repeat(60));
    console.log('Version:', app.getVersion());
    console.log('Platform:', process.platform);
    console.log('Architecture:', process.arch);
    console.log('Development Mode:', IS_DEV);
    console.log('Node:', process.versions.node);
    console.log('Electron:', process.versions.electron);
    console.log('Chrome:', process.versions.chrome);
    console.log('User Data:', app.getPath('userData'));
    console.log('='.repeat(60));

    // Initialize app config
    appConfig = new AppConfig(app);
    console.log('✓ App configuration initialized');

    // Initialize window manager
    windowManager = new WindowManager(app, IS_DEV);
    console.log('✓ Window manager initialized');

    // Show splash screen
    windowManager.createSplashWindow();
    console.log('✓ Splash screen displayed');

    // Initialize protocol handler
    protocolHandler = new ProtocolHandler(windowManager);
    protocolHandler.register();
    console.log('✓ Protocol handler registered');

    // Start backend service
    backendService = new BackendService(app, IS_DEV);
    await backendService.start();
    console.log('✓ Backend service started on port:', backendService.getPort());

    // Register IPC handlers
    registerIpcHandlers();
    console.log('✓ IPC handlers registered');

    // Create main window
    const preloadPath = path.join(__dirname, 'preload.js');
    windowManager.createMainWindow(backendService.getPort(), preloadPath);
    console.log('✓ Main window created');

    // Handle window close event
    const mainWindow = windowManager.getMainWindow();
    mainWindow.on('close', (event) => {
      const prevented = windowManager.handleWindowClose(
        event,
        isQuitting,
        appConfig.get('minimizeToTray', true)
      );
      
      if (prevented && !isQuitting) {
        // Show tray notification
        if (trayManager && process.platform === 'win32') {
          trayManager.showNotification(
            APP_NAME,
            'Application is still running in the system tray'
          );
        }
      }
    });

    // Create system tray
    trayManager = new TrayManager(app, windowManager, IS_DEV);
    trayManager.create();
    trayManager.setBackendUrl(backendService.getUrl());
    console.log('✓ System tray created');

    // Build application menu
    menuBuilder = new MenuBuilder(app, windowManager, appConfig, IS_DEV);
    menuBuilder.buildMenu();
    console.log('✓ Application menu created');

    // Setup auto-updater
    setupAutoUpdater();
    console.log('✓ Auto-updater configured');

    // Check for first run
    mainWindow.once('ready-to-show', () => {
      checkFirstRun();
      protocolHandler.checkPendingUrl();
    });

    console.log('='.repeat(60));
    console.log(`${APP_NAME} Started Successfully!`);
    console.log('='.repeat(60));

  } catch (error) {
    console.error('Startup failed:', error);

    // Close splash if it exists
    if (windowManager) {
      const splashWindow = windowManager.getSplashWindow();
      if (splashWindow && !splashWindow.isDestroyed()) {
        splashWindow.close();
      }
    }

    dialog.showErrorBox(
      'Startup Error',
      `Failed to start ${APP_NAME}:\n\n${error.message}\n\n` +
      `Please check the logs for more information:\n${path.join(app.getPath('userData'), 'logs')}`
    );

    app.quit();
  }
}

// ========================================
// App Event Handlers
// ========================================

// Setup error handling as early as possible
setupErrorHandling();

// App is ready
app.whenReady().then(startApplication);

// Activate (macOS)
app.on('activate', () => {
  // On macOS, re-create window when dock icon is clicked
  if (!windowManager || !windowManager.getMainWindow()) {
    if (!isQuitting) {
      startApplication();
    }
  } else {
    windowManager.showMainWindow();
  }
});

// All windows closed
app.on('window-all-closed', () => {
  // On macOS, apps stay active until user quits explicitly
  if (process.platform !== 'darwin') {
    isQuitting = true;
    app.quit();
  }
});

// Before quit
app.on('before-quit', async (event) => {
  console.log('Application is quitting...');
  
  // Prevent multiple cleanup calls
  if (isCleaningUp) {
    return;
  }
  
  isQuitting = true;
  isCleaningUp = true;
  
  // Prevent immediate quit to allow async cleanup
  event.preventDefault();
  
  try {
    // Perform cleanup with timeout
    await Promise.race([
      cleanup(),
      new Promise((_, reject) => 
        setTimeout(() => reject(new Error('Cleanup timeout')), 30000)
      )
    ]);
  } catch (error) {
    console.error('Cleanup error or timeout:', error);
  } finally {
    // Now actually quit
    app.exit(0);
  }
});

// Will quit
app.on('will-quit', () => {
  console.log('Application will quit');
});

// Quit
app.on('quit', (event, exitCode) => {
  console.log(`Application quit with exit code: ${exitCode}`);
});

// GPU process crashed (attempt recovery)
app.on('gpu-process-crashed', (event, killed) => {
  console.error('GPU process crashed, killed:', killed);
  
  if (!killed) {
    console.log('Attempting to recover from GPU process crash...');
    // Electron will attempt to restart the GPU process automatically
  }
});

// Renderer process crashed (attempt recovery)
app.on('render-process-gone', (event, webContents, details) => {
  console.error('Renderer process gone:', details);
  
  if (details.reason !== 'clean-exit') {
    const mainWindow = windowManager ? windowManager.getMainWindow() : null;
    
    if (mainWindow && !mainWindow.isDestroyed()) {
      dialog.showMessageBox(mainWindow, {
        type: 'error',
        title: 'Renderer Process Crashed',
        message: 'The application interface has crashed.',
        detail: `Reason: ${details.reason}\n\nWould you like to reload the application?`,
        buttons: ['Reload', 'Quit'],
        defaultId: 0,
        cancelId: 1
      }).then((result) => {
        if (result.response === 0) {
          mainWindow.reload();
        } else {
          app.quit();
        }
      });
    }
  }
});

// Child process gone (backend)
app.on('child-process-gone', (event, details) => {
  console.error('Child process gone:', details);
  
  if (details.type === 'Utility' && details.reason !== 'clean-exit') {
    console.warn('A utility process has crashed, but the app should continue functioning');
  }
});

// Backend crash handler
app.on('backend-crash', () => {
  console.error('Backend has crashed after max restart attempts');
  
  const mainWindow = windowManager ? windowManager.getMainWindow() : null;
  if (mainWindow && !mainWindow.isDestroyed()) {
    dialog.showMessageBox(mainWindow, {
      type: 'error',
      title: 'Backend Service Error',
      message: 'The Aura backend service has stopped unexpectedly and could not be restarted.',
      detail: 'The application needs to close. Please check the logs and try restarting.\n\n' +
              `Logs: ${path.join(app.getPath('userData'), 'logs')}`,
      buttons: ['Close Application'],
      defaultId: 0
    }).then(() => {
      isQuitting = true;
      app.quit();
    });
  } else {
    isQuitting = true;
    app.quit();
  }
});

// ========================================
// Exports (for testing)
// ========================================

module.exports = {
  app,
  windowManager,
  appConfig,
  backendService,
  trayManager,
  menuBuilder,
  protocolHandler
};
