/**
 * Aura Video Studio - Electron Main Process
 * 
 * This is the main entry point for the Electron application.
 * It orchestrates all modules and manages the application lifecycle.
 */

const { app, dialog, protocol } = require('electron');
const path = require('path');
const fs = require('fs');
const { autoUpdater } = require('electron-updater');

// Import startup logging and diagnostics (MUST BE FIRST)
const StartupLogger = require('./startup-logger');
const StartupDiagnostics = require('./startup-diagnostics');

// Import application modules
const WindowManager = require('./window-manager');
const AppConfig = require('./app-config');
const BackendService = require('./backend-service');
const TrayManager = require('./tray-manager');
const MenuBuilder = require('./menu-builder');
const ProtocolHandler = require('./protocol-handler');
const WindowsSetupWizard = require('./windows-setup-wizard');

// Import IPC handlers
const ConfigHandler = require('./ipc-handlers/config-handler');
const SystemHandler = require('./ipc-handlers/system-handler');
const VideoHandler = require('./ipc-handlers/video-handler');
const BackendHandler = require('./ipc-handlers/backend-handler');
const FFmpegHandler = require('./ipc-handlers/ffmpeg-handler');
const StartupLogsHandler = require('./ipc-handlers/startup-logs-handler');

// ========================================
// Global State
// ========================================

let startupLogger = null;
let windowManager = null;
let appConfig = null;
let backendService = null;
let trayManager = null;
let menuBuilder = null;
let protocolHandler = null;
let windowsSetupWizard = null;

let ipcHandlers = {
  config: null,
  system: null,
  video: null,
  backend: null,
  ffmpeg: null,
  startupLogs: null
};

let isQuitting = false;
let isCleaningUp = false;
let crashCount = 0;
const MAX_CRASH_COUNT = 3;

// ========================================
// Environment Configuration
// ========================================

const IS_DEV = process.argv.includes('--dev') || !app.isPackaged;
const DEBUG_STARTUP = process.argv.includes('--debug-startup');
const APP_NAME = 'Aura Video Studio';

// Disable hardware acceleration if configured (helps with some Windows issues)
if (process.env.DISABLE_HARDWARE_ACCELERATION === 'true') {
  app.disableHardwareAcceleration();
}

// ========================================
// Protocol Registration (MUST BE BEFORE app.ready)
// ========================================

// Register custom protocol scheme as privileged
// This MUST be called before the app is ready according to Electron API
protocol.registerSchemesAsPrivileged([
  {
    scheme: ProtocolHandler.getProtocolScheme(),
    privileges: {
      standard: true,
      secure: true,
      supportFetchAPI: true
    }
  }
]);

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
  if (startupLogger) {
    startupLogger.stepStart('error-handling', 'ErrorHandling', 'Setting up error handlers');
  }
  
  // Handle uncaught exceptions
  process.on('uncaughtException', (error) => {
    console.error('Uncaught exception:', error);
    
    crashCount++;
    
    // Log error with structured logging
    if (startupLogger) {
      startupLogger.error('UncaughtException', 'Uncaught exception occurred', error, {
        crashCount,
        maxCrashCount: MAX_CRASH_COUNT
      });
    } else {
      logError('UncaughtException', error);
    }
    
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
    if (startupLogger) {
      startupLogger.error('UnhandledRejection', 'Unhandled promise rejection', reason instanceof Error ? reason : new Error(String(reason)));
    } else {
      logError('UnhandledRejection', reason);
    }
  });

  // Handle process warnings
  process.on('warning', (warning) => {
    console.warn('Process warning:', warning);
    if (startupLogger) {
      startupLogger.warn('ProcessWarning', warning.message, {
        name: warning.name,
        stack: warning.stack
      });
    }
  });
  
  if (startupLogger) {
    startupLogger.stepEnd('error-handling', true);
  }
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
  if (startupLogger) {
    startupLogger.stepStart('auto-updater', 'AutoUpdater', 'Configuring auto-updater');
  }
  
  if (IS_DEV) {
    console.log('Auto-updater disabled in development mode');
    if (startupLogger) {
      startupLogger.info('AutoUpdater', 'Auto-updater disabled in development mode');
      startupLogger.stepEnd('auto-updater', true);
    }
    return;
  }

  // Configure auto-updater
  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;

  // Update available
  autoUpdater.on('update-available', (info) => {
    console.log('Update available:', info.version);
    if (startupLogger) {
      startupLogger.info('AutoUpdater', 'Update available', { version: info.version });
    }

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
    if (startupLogger) {
      startupLogger.info('AutoUpdater', 'Application is up to date');
    }
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
    if (startupLogger) {
      startupLogger.info('AutoUpdater', 'Update downloaded', { version: info.version });
    }

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
    if (startupLogger) {
      startupLogger.error('AutoUpdater', 'Auto-updater error', error);
    }
  });

  // Check for updates on startup (if enabled)
  const autoUpdate = appConfig ? appConfig.get('autoUpdate', true) : true;
  if (autoUpdate) {
    setTimeout(() => {
      autoUpdater.checkForUpdates().catch(err => {
        console.error('Update check failed:', err);
        if (startupLogger) {
          startupLogger.error('AutoUpdater', 'Update check failed', err);
        }
      });
    }, 5000);
  }
  
  if (startupLogger) {
    startupLogger.stepEnd('auto-updater', true, { autoUpdate });
  }
}

// ========================================
// IPC Handlers Registration
// ========================================

/**
 * Register all IPC handlers
 */
function registerIpcHandlers() {
  if (startupLogger) {
    startupLogger.stepStart('ipc-handlers', 'IPC', 'Registering IPC handlers');
  }
  
  console.log('Registering IPC handlers...');

  try {
    // Config handler
    ipcHandlers.config = new ConfigHandler(appConfig);
    ipcHandlers.config.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'Config handler registered');
    }

    // System handler
    ipcHandlers.system = new SystemHandler(app, windowManager, appConfig);
    ipcHandlers.system.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'System handler registered');
    }

    // Video handler
    const backendUrl = backendService.getUrl();
    ipcHandlers.video = new VideoHandler(backendUrl);
    ipcHandlers.video.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'Video handler registered', { backendUrl });
    }

    // Backend handler (pass backendService for control operations)
    ipcHandlers.backend = new BackendHandler(backendUrl, backendService);
    ipcHandlers.backend.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'Backend handler registered');
    }

    // Start backend health checks
    const mainWindow = windowManager.getMainWindow();
    if (mainWindow) {
      ipcHandlers.backend.startHealthChecks(mainWindow);
      if (startupLogger) {
        startupLogger.debug('IPC', 'Backend health checks started');
      }
    }

    // FFmpeg handler
    ipcHandlers.ffmpeg = new FFmpegHandler(app, windowManager);
    ipcHandlers.ffmpeg.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'FFmpeg handler registered');
    }

    // Startup logs handler
    ipcHandlers.startupLogs = new StartupLogsHandler(app, startupLogger);
    ipcHandlers.startupLogs.register();
    if (startupLogger) {
      startupLogger.debug('IPC', 'Startup logs handler registered');
    }

    console.log('IPC handlers registered successfully');
    
    if (startupLogger) {
      startupLogger.stepEnd('ipc-handlers', true, {
        handlersRegistered: Object.keys(ipcHandlers).length
      });
    }
  } catch (error) {
    if (startupLogger) {
      startupLogger.error('IPC', 'Failed to register IPC handlers', error);
      startupLogger.stepEnd('ipc-handlers', false, { error: error.message });
    }
    throw error;
  }
}

// ========================================
// First Run Check
// ========================================

/**
 * Check if this is the first run and run Windows-specific setup if needed
 */
async function checkFirstRun() {
  if (startupLogger) {
    startupLogger.stepStart('first-run-check', 'FirstRun', 'Checking first run status');
  }
  
  const setupComplete = appConfig.isSetupComplete();
  const firstRun = appConfig.isFirstRun();

  if (startupLogger) {
    startupLogger.debug('FirstRun', 'First run check', { setupComplete, firstRun });
  }

  // Run Windows-specific setup wizard on first run (Windows only)
  if ((firstRun || !setupComplete) && process.platform === 'win32') {
    console.log('First run detected on Windows, running Windows setup wizard...');
    if (startupLogger) {
      startupLogger.info('FirstRun', 'Running Windows setup wizard');
    }
    
    // Initialize Windows setup wizard
    windowsSetupWizard = new WindowsSetupWizard(app, windowManager, appConfig);
    
    // Check if Windows setup has been completed
    if (!windowsSetupWizard.isSetupComplete()) {
      try {
        const setupResult = await windowsSetupWizard.runSetup();
        
        if (!setupResult.success) {
          console.warn('Windows setup wizard encountered issues:', setupResult);
          
          if (startupLogger) {
            startupLogger.warn('FirstRun', 'Windows setup encountered issues', {
              errors: setupResult.results?.errors || []
            });
          }
          
          // Show warning to user if there were critical errors
          if (setupResult.results && setupResult.results.errors.length > 0) {
            const mainWindow = windowManager.getMainWindow();
            if (mainWindow && !mainWindow.isDestroyed()) {
              dialog.showMessageBox(mainWindow, {
                type: 'warning',
                title: 'Setup Incomplete',
                message: 'Some setup steps could not be completed',
                detail: 
                  'Aura Video Studio has started, but some setup steps encountered issues:\n\n' +
                  setupResult.results.errors.join('\n') +
                  '\n\nThe application may not function correctly. Please check the setup guide.',
                buttons: ['Continue Anyway']
              });
            }
          }
        } else {
          console.log('✓ Windows setup wizard completed successfully');
          if (startupLogger) {
            startupLogger.info('FirstRun', 'Windows setup completed successfully');
          }
        }
      } catch (error) {
        console.error('Windows setup wizard error:', error);
        if (startupLogger) {
          startupLogger.error('FirstRun', 'Windows setup wizard error', error);
        }
      }
    } else {
      // Run quick compatibility check on subsequent launches
      try {
        const quickCheck = await windowsSetupWizard.quickCheck();
        if (!quickCheck.compatible) {
          console.warn('Windows compatibility issues detected:', quickCheck.issues);
          
          if (startupLogger) {
            startupLogger.warn('FirstRun', 'Compatibility issues detected', {
              issues: quickCheck.issues
            });
          }
          
          // Show warning for critical issues
          const criticalIssues = quickCheck.issues.filter(i => i.type === 'error');
          if (criticalIssues.length > 0) {
            const mainWindow = windowManager.getMainWindow();
            if (mainWindow && !mainWindow.isDestroyed()) {
              dialog.showMessageBox(mainWindow, {
                type: 'warning',
                title: 'Compatibility Issues',
                message: 'System compatibility issues detected',
                detail: criticalIssues.map(i => `• ${i.message}\n  Action: ${i.action}`).join('\n\n'),
                buttons: ['OK']
              });
            }
          }
        }
      } catch (error) {
        console.error('Quick compatibility check error:', error);
        if (startupLogger) {
          startupLogger.error('FirstRun', 'Quick compatibility check error', error);
        }
      }
    }
  }

  // Navigate to setup wizard in UI if not complete
  if (!setupComplete || firstRun) {
    console.log('First run detected, navigating to setup wizard...');
    
    if (startupLogger) {
      startupLogger.info('FirstRun', 'Navigating to setup wizard in UI');
    }
    
    const mainWindow = windowManager.getMainWindow();
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.executeJavaScript(`
        if (window.location.hash !== '#/setup') {
          window.location.hash = '#/setup';
        }
      `);
    }
  }
  
  if (startupLogger) {
    startupLogger.stepEnd('first-run-check', true, { setupComplete, firstRun });
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
  
  if (startupLogger) {
    startupLogger.info('Cleanup', 'Starting application cleanup');
  }

  try {
    // Stop backend health checks
    if (ipcHandlers.backend) {
      ipcHandlers.backend.stopHealthChecks();
      if (startupLogger) {
        startupLogger.debug('Cleanup', 'Backend health checks stopped');
      }
    }

    // Stop backend service (now async for proper Windows process termination)
    if (backendService) {
      console.log('Stopping backend service...');
      if (startupLogger) {
        startupLogger.info('Cleanup', 'Stopping backend service');
      }
      await backendService.stop();
      console.log('Backend service stopped');
      if (startupLogger) {
        startupLogger.info('Cleanup', 'Backend service stopped successfully');
      }
    }

    // Destroy tray
    if (trayManager) {
      trayManager.destroy();
      if (startupLogger) {
        startupLogger.debug('Cleanup', 'System tray destroyed');
      }
    }

    // Cleanup temp files
    const tempPath = path.join(app.getPath('temp'), 'aura-video-studio');
    if (fs.existsSync(tempPath)) {
      try {
        fs.rmSync(tempPath, { recursive: true, force: true });
        console.log('Temp files cleaned up');
        if (startupLogger) {
          startupLogger.debug('Cleanup', 'Temp files cleaned up', { tempPath });
        }
      } catch (error) {
        console.warn('Failed to cleanup temp files:', error.message);
        if (startupLogger) {
          startupLogger.warn('Cleanup', 'Failed to cleanup temp files', { 
            error: error.message,
            tempPath 
          });
        }
      }
    }

    console.log('Cleanup completed');
    if (startupLogger) {
      startupLogger.info('Cleanup', 'Application cleanup completed successfully');
    }
  } catch (error) {
    console.error('Error during cleanup:', error);
    if (startupLogger) {
      startupLogger.error('Cleanup', 'Error during cleanup', error);
    }
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
    console.log('Debug Startup:', DEBUG_STARTUP);
    console.log('Node:', process.versions.node);
    console.log('Electron:', process.versions.electron);
    console.log('Chrome:', process.versions.chrome);
    console.log('User Data:', app.getPath('userData'));
    console.log('='.repeat(60));

    // Initialize startup logger (BEFORE anything else)
    startupLogger = new StartupLogger(app, { debugMode: DEBUG_STARTUP });
    startupLogger.info('Main', 'Application startup initiated', {
      version: app.getVersion(),
      platform: process.platform,
      arch: process.arch,
      isDev: IS_DEV,
      debugStartup: DEBUG_STARTUP,
      userDataPath: app.getPath('userData')
    });

    // Run startup diagnostics
    const diagnostics = new StartupDiagnostics(app, startupLogger);
    const diagnosticsResults = await startupLogger.trackAsync(
      'diagnostics',
      'StartupDiagnostics',
      'Running startup diagnostics',
      () => diagnostics.runDiagnostics()
    );

    // Warn if diagnostics found issues
    if (!diagnosticsResults.healthy) {
      startupLogger.warn('StartupDiagnostics', 'System health check detected issues', {
        errors: diagnosticsResults.errors.length,
        warnings: diagnosticsResults.warnings.length
      });
    }

    // Initialize app config
    appConfig = startupLogger.trackSync(
      'app-config',
      'AppConfig',
      'Initializing application configuration',
      () => new AppConfig(app)
    );
    console.log('✓ App configuration initialized');

    // Initialize window manager
    windowManager = startupLogger.trackSync(
      'window-manager',
      'WindowManager',
      'Initializing window manager',
      () => new WindowManager(app, IS_DEV)
    );
    console.log('✓ Window manager initialized');

    // Show splash screen
    startupLogger.stepStart('splash-screen', 'WindowManager', 'Creating splash screen');
    windowManager.createSplashWindow();
    console.log('✓ Splash screen displayed');
    startupLogger.stepEnd('splash-screen', true);

    // Initialize protocol handler
    protocolHandler = startupLogger.trackSync(
      'protocol-handler',
      'ProtocolHandler',
      'Initializing protocol handler',
      () => {
        const handler = new ProtocolHandler(windowManager);
        handler.register();
        return handler;
      }
    );
    console.log('✓ Protocol handler registered');

    // Start backend service
    backendService = await startupLogger.trackAsync(
      'backend-service',
      'BackendService',
      'Starting backend service',
      async () => {
        const service = new BackendService(app, IS_DEV);
        await service.start();
        console.log('✓ Backend service started on port:', service.getPort());
        return service;
      }
    );

    // Register IPC handlers
    registerIpcHandlers();
    console.log('✓ IPC handlers registered');

    // Create main window
    startupLogger.stepStart('main-window', 'WindowManager', 'Creating main window');
    const preloadPath = path.join(__dirname, 'preload.js');
    windowManager.createMainWindow(backendService.getPort(), preloadPath);
    console.log('✓ Main window created');
    startupLogger.stepEnd('main-window', true, { 
      port: backendService.getPort(),
      preloadPath 
    });

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

    // Create system tray (optional, non-critical)
    startupLogger.stepStart('system-tray', 'TrayManager', 'Creating system tray');
    trayManager = new TrayManager(app, windowManager, IS_DEV);
    const trayCreated = trayManager.create();
    if (trayCreated) {
      trayManager.setBackendUrl(backendService.getUrl());
      console.log('✓ System tray created');
      startupLogger.stepEnd('system-tray', true);
    } else {
      console.log('⚠ System tray not created (icon not found, but app will continue)');
      startupLogger.stepEnd('system-tray', true, { 
        created: false, 
        reason: 'icon not found' 
      });
    }

    // Build application menu
    menuBuilder = startupLogger.trackSync(
      'app-menu',
      'MenuBuilder',
      'Building application menu',
      () => {
        const builder = new MenuBuilder(app, windowManager, appConfig, IS_DEV);
        builder.buildMenu();
        console.log('✓ Application menu created');
        return builder;
      }
    );

    // Setup auto-updater
    setupAutoUpdater();
    console.log('✓ Auto-updater configured');

    // Check for first run (async to support Windows setup wizard)
    mainWindow.once('ready-to-show', async () => {
      try {
        await checkFirstRun();
      } catch (error) {
        console.error('First run check error:', error);
        if (startupLogger) {
          startupLogger.error('FirstRun', 'First run check error', error);
        }
      }
      protocolHandler.checkPendingUrl();
      
      // Finalize startup logging
      if (startupLogger) {
        const summary = startupLogger.finalize();
        
        // Keep DevTools open in debug startup mode
        if (DEBUG_STARTUP && mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.openDevTools();
          startupLogger.info('Main', 'DevTools opened (debug-startup mode)');
        }
        
        // Log summary to console
        console.log('='.repeat(60));
        console.log('STARTUP SUMMARY');
        console.log('='.repeat(60));
        console.log(`Status: ${summary.success ? 'SUCCESS' : 'COMPLETED WITH ERRORS'}`);
        console.log(`Duration: ${summary.totalDurationSeconds}s`);
        console.log(`Steps: ${summary.statistics.successfulSteps}/${summary.statistics.totalSteps} successful`);
        console.log(`Log File: ${startupLogger.getLogFile()}`);
        console.log(`Summary: ${startupLogger.getSummaryFile()}`);
        console.log('='.repeat(60));
      }
    });

    console.log('='.repeat(60));
    console.log(`${APP_NAME} Started Successfully!`);
    console.log('='.repeat(60));

  } catch (error) {
    console.error('Startup failed:', error);
    
    // Log to startup logger if available
    if (startupLogger) {
      startupLogger.error('Main', 'Application startup failed', error, {
        fatal: true
      });
      startupLogger.finalize();
    }

    // Close splash if it exists
    if (windowManager) {
      const splashWindow = windowManager.getSplashWindow();
      if (splashWindow && !splashWindow.isDestroyed()) {
        splashWindow.close();
      }
    }

    const logsDir = startupLogger 
      ? startupLogger.getLogsDirectory() 
      : path.join(app.getPath('userData'), 'logs');

    dialog.showErrorBox(
      'Startup Error',
      `Failed to start ${APP_NAME}:\n\n${error.message}\n\n` +
      `Stack trace:\n${error.stack}\n\n` +
      `Please check the logs for more information:\n${logsDir}`
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
  protocolHandler,
  startupLogger
};
