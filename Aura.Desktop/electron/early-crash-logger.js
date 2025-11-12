/**
 * Early Crash Logger
 * Writes crash information to disk BEFORE any other initialization
 * This ensures we capture crashes that happen during startup
 */

const fs = require('fs');
const path = require('path');

class EarlyCrashLogger {
  constructor(app) {
    this.app = app;
    this.isInitialized = false;
    this.logsDir = null;
    this.crashFile = null;
    
    try {
      // Initialize immediately
      this._initialize();
      this.isInitialized = true;
    } catch (error) {
      // Last resort - log to console
      console.error('CRITICAL: Failed to initialize early crash logger:', error);
    }
  }

  /**
   * Initialize the crash logger
   */
  _initialize() {
    // Get logs directory
    this.logsDir = path.join(this.app.getPath('userData'), 'logs');
    
    // Ensure directory exists
    if (!fs.existsSync(this.logsDir)) {
      fs.mkdirSync(this.logsDir, { recursive: true });
    }

    // Create crash log file
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    this.crashFile = path.join(this.logsDir, `crash-log-${timestamp}.log`);

    // Write initial header
    this._writeInitialHeader();
  }

  /**
   * Write initial header to crash log
   */
  _writeInitialHeader() {
    const header = [
      '='.repeat(80),
      'AURA VIDEO STUDIO - CRASH LOG',
      '='.repeat(80),
      `Timestamp: ${new Date().toISOString()}`,
      `Platform: ${process.platform}`,
      `Arch: ${process.arch}`,
      `Node: ${process.versions.node}`,
      `Electron: ${process.versions.electron}`,
      `Chrome: ${process.versions.chrome}`,
      `App Version: ${this.app.getVersion()}`,
      `User Data: ${this.app.getPath('userData')}`,
      `Process ID: ${process.pid}`,
      '='.repeat(80),
      '',
      'This log file captures early startup crashes and critical errors.',
      'If the application fails to start, check this file for details.',
      '',
      '='.repeat(80),
      ''
    ].join('\n');

    try {
      fs.writeFileSync(this.crashFile, header);
    } catch (error) {
      console.error('Failed to write crash log header:', error);
    }
  }

  /**
   * Log a crash or critical error
   */
  logCrash(type, message, error = null, metadata = {}) {
    if (!this.isInitialized) {
      console.error('Early crash logger not initialized, logging to console only');
      console.error(`[${type}] ${message}`, error, metadata);
      return;
    }

    try {
      const timestamp = new Date().toISOString();
      const entry = [
        '',
        '=' + '-'.repeat(78) + '=',
        `[${timestamp}] ${type.toUpperCase()}: ${message}`,
        '=' + '-'.repeat(78) + '='
      ];

      // Add error details if present
      if (error) {
        entry.push('');
        entry.push('ERROR DETAILS:');
        entry.push(`  Name: ${error.name || 'Error'}`);
        entry.push(`  Message: ${error.message || String(error)}`);
        
        if (error.code) {
          entry.push(`  Code: ${error.code}`);
        }
        
        if (error.stack) {
          entry.push('');
          entry.push('STACK TRACE:');
          entry.push(error.stack);
        } else {
          entry.push('  (No stack trace available)');
        }
      }

      // Add metadata if present
      if (metadata && Object.keys(metadata).length > 0) {
        entry.push('');
        entry.push('ADDITIONAL INFORMATION:');
        for (const [key, value] of Object.entries(metadata)) {
          entry.push(`  ${key}: ${JSON.stringify(value)}`);
        }
      }

      entry.push('');

      // Write to file
      fs.appendFileSync(this.crashFile, entry.join('\n'));

      // Also log to console
      console.error(`[${type}] ${message}`, error || '', metadata);
    } catch (writeError) {
      console.error('Failed to write to crash log:', writeError);
      console.error('Original error:', type, message, error);
    }
  }

  /**
   * Log uncaught exception
   */
  logUncaughtException(error) {
    this.logCrash('UNCAUGHT_EXCEPTION', 'Uncaught exception occurred', error, {
      processId: process.pid,
      uptime: process.uptime(),
      memoryUsage: process.memoryUsage()
    });
  }

  /**
   * Log unhandled rejection
   */
  logUnhandledRejection(reason) {
    const error = reason instanceof Error ? reason : new Error(String(reason));
    this.logCrash('UNHANDLED_REJECTION', 'Unhandled promise rejection', error, {
      processId: process.pid,
      uptime: process.uptime(),
      memoryUsage: process.memoryUsage()
    });
  }

  /**
   * Log initialization failure
   */
  logInitializationFailure(step, error, metadata = {}) {
    this.logCrash('INITIALIZATION_FAILURE', `Initialization failed at step: ${step}`, error, {
      initializationStep: step,
      ...metadata
    });
  }

  /**
   * Log startup completion
   */
  logStartupComplete() {
    if (!this.isInitialized) {
      return;
    }

    try {
      const footer = [
        '',
        '='.repeat(80),
        'STARTUP COMPLETED SUCCESSFULLY',
        `Timestamp: ${new Date().toISOString()}`,
        `Uptime: ${process.uptime().toFixed(2)}s`,
        '='.repeat(80),
        '',
        'Application started successfully. This crash log will be kept for debugging purposes.',
        'No critical errors were detected during startup.',
        ''
      ].join('\n');

      fs.appendFileSync(this.crashFile, footer);
    } catch (error) {
      console.error('Failed to write startup completion to crash log:', error);
    }
  }

  /**
   * Get crash log file path
   */
  getCrashLogPath() {
    return this.crashFile;
  }

  /**
   * Get logs directory
   */
  getLogsDirectory() {
    return this.logsDir;
  }

  /**
   * Install global error handlers
   */
  installGlobalHandlers() {
    if (!this.isInitialized) {
      console.error('Cannot install global handlers - early crash logger not initialized');
      return;
    }

    // Uncaught exception handler
    process.on('uncaughtException', (error) => {
      this.logUncaughtException(error);
    });

    // Unhandled rejection handler
    process.on('unhandledRejection', (reason) => {
      this.logUnhandledRejection(reason);
    });

    // Process warnings
    process.on('warning', (warning) => {
      this.logCrash('PROCESS_WARNING', warning.message, warning, {
        warningName: warning.name
      });
    });

    console.log('Early crash logger: Global error handlers installed');
    this.logCrash('INFO', 'Early crash logger initialized and global handlers installed', null, {
      crashLogPath: this.crashFile,
      logsDirectory: this.logsDir
    });
  }
}

module.exports = EarlyCrashLogger;
