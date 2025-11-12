/**
 * Startup Logger Module
 * Provides structured JSON logging for application startup with performance tracking,
 * log rotation, and summary generation.
 */

const fs = require('fs');
const path = require('path');

class StartupLogger {
  constructor(app, options = {}) {
    this.app = app;
    this.debugMode = options.debugMode || false;
    
    // Startup tracking
    this.startTime = Date.now();
    this.steps = [];
    this.currentStep = null;
    this.logEntries = [];
    this.errors = [];
    
    // Paths
    this.logsDir = path.join(app.getPath('userData'), 'logs');
    this.timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    this.logFile = path.join(this.logsDir, `startup-${this.timestamp}.log`);
    this.summaryFile = path.join(this.logsDir, `startup-summary-${this.timestamp}.json`);
    
    // Initialize log directory and perform rotation
    this._initializeLogDirectory();
    this._performLogRotation();
    
    // Write initial header
    this._writeHeader();
  }

  /**
   * Initialize log directory
   */
  _initializeLogDirectory() {
    if (!fs.existsSync(this.logsDir)) {
      fs.mkdirSync(this.logsDir, { recursive: true });
    }
  }

  /**
   * Perform log rotation - keep only last 10 startup logs
   */
  _performLogRotation() {
    try {
      const files = fs.readdirSync(this.logsDir);
      
      // Filter startup log files
      const startupLogs = files
        .filter(f => f.startsWith('startup-') && f.endsWith('.log'))
        .map(f => ({
          name: f,
          path: path.join(this.logsDir, f),
          time: fs.statSync(path.join(this.logsDir, f)).mtime.getTime()
        }))
        .sort((a, b) => b.time - a.time); // Sort by most recent first
      
      // Keep only the most recent 9 (since we're about to create the 10th)
      const logsToDelete = startupLogs.slice(9);
      
      logsToDelete.forEach(log => {
        try {
          fs.unlinkSync(log.path);
          
          // Also delete corresponding summary file if it exists
          const summaryPath = log.path.replace('.log', '.json').replace('startup-', 'startup-summary-');
          if (fs.existsSync(summaryPath)) {
            fs.unlinkSync(summaryPath);
          }
        } catch (error) {
          console.warn(`Failed to delete old log file ${log.name}:`, error.message);
        }
      });
      
      if (logsToDelete.length > 0) {
        this._writeToConsoleAndLog('info', 'LogRotation', `Rotated ${logsToDelete.length} old startup log(s)`, {
          deletedFiles: logsToDelete.map(l => l.name)
        });
      }
    } catch (error) {
      console.error('Log rotation failed:', error);
    }
  }

  /**
   * Write log header
   */
  _writeHeader() {
    const header = {
      level: 'info',
      timestamp: new Date().toISOString(),
      component: 'StartupLogger',
      message: 'Application startup logging initialized',
      metadata: {
        appVersion: this.app.getVersion(),
        platform: process.platform,
        arch: process.arch,
        nodeVersion: process.versions.node,
        electronVersion: process.versions.electron,
        chromeVersion: process.versions.chrome,
        debugMode: this.debugMode,
        logFile: this.logFile,
        summaryFile: this.summaryFile
      }
    };
    
    this._writeLogEntry(header);
  }

  /**
   * Write a log entry to disk
   */
  _writeLogEntry(entry) {
    try {
      const logLine = JSON.stringify(entry) + '\n';
      fs.appendFileSync(this.logFile, logLine);
      this.logEntries.push(entry);
      
      // Also log to console in debug mode
      if (this.debugMode) {
        const consoleMsg = `[${entry.level.toUpperCase()}] ${entry.component}: ${entry.message}`;
        console.log(consoleMsg, entry.metadata || '');
      }
    } catch (error) {
      console.error('Failed to write log entry:', error);
    }
  }

  /**
   * Write to both console and log file
   */
  _writeToConsoleAndLog(level, component, message, metadata = {}) {
    const entry = {
      level,
      timestamp: new Date().toISOString(),
      component,
      message,
      metadata
    };
    
    this._writeLogEntry(entry);
    
    // Always write important messages to console
    if (level === 'error' || level === 'warn') {
      console[level](`[${component}] ${message}`, metadata);
    }
  }

  /**
   * Log a message
   */
  log(level, component, message, metadata = {}) {
    this._writeToConsoleAndLog(level, component, message, metadata);
  }

  /**
   * Log info message
   */
  info(component, message, metadata = {}) {
    this.log('info', component, message, metadata);
  }

  /**
   * Log warning message
   */
  warn(component, message, metadata = {}) {
    this.log('warn', component, message, metadata);
  }

  /**
   * Log error message
   */
  error(component, message, errorObj = null, metadata = {}) {
    const enrichedMetadata = { ...metadata };
    
    if (errorObj) {
      enrichedMetadata.error = {
        message: errorObj.message || String(errorObj),
        stack: errorObj.stack || 'No stack trace available',
        name: errorObj.name || 'Error',
        code: errorObj.code
      };
    }
    
    this._writeToConsoleAndLog('error', component, message, enrichedMetadata);
    this.errors.push({
      component,
      message,
      error: errorObj,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Log debug message (only in debug mode)
   */
  debug(component, message, metadata = {}) {
    if (this.debugMode) {
      this.log('debug', component, message, metadata);
    }
  }

  /**
   * Start tracking an initialization step
   */
  stepStart(stepName, component, description) {
    const startTime = Date.now();
    
    this.currentStep = {
      name: stepName,
      component,
      description,
      startTime,
      startTimestamp: new Date().toISOString()
    };
    
    this.info(component, `Starting: ${description}`, {
      step: stepName,
      startTime: this.currentStep.startTimestamp
    });
    
    return this.currentStep;
  }

  /**
   * End tracking an initialization step
   */
  stepEnd(stepName, success = true, metadata = {}) {
    if (!this.currentStep || this.currentStep.name !== stepName) {
      this.warn('StartupLogger', `Attempted to end step '${stepName}' but current step is '${this.currentStep?.name || 'none'}'`);
      return;
    }
    
    const endTime = Date.now();
    const duration = endTime - this.currentStep.startTime;
    
    this.currentStep.endTime = endTime;
    this.currentStep.endTimestamp = new Date().toISOString();
    this.currentStep.duration = duration;
    this.currentStep.success = success;
    this.currentStep.metadata = metadata;
    
    // Log completion
    const level = success ? 'info' : 'error';
    const status = success ? 'completed' : 'failed';
    
    this.log(level, this.currentStep.component, `${this.currentStep.description} ${status}`, {
      step: stepName,
      duration: `${duration}ms`,
      success,
      ...metadata
    });
    
    // Warn if step took more than 2 seconds
    if (duration > 2000) {
      this.warn('PerformanceWarning', `Step '${stepName}' took longer than 2 seconds`, {
        step: stepName,
        duration: `${duration}ms`,
        threshold: '2000ms'
      });
    }
    
    this.steps.push({ ...this.currentStep });
    this.currentStep = null;
  }

  /**
   * Track an async function with automatic start/end logging
   */
  async trackAsync(stepName, component, description, asyncFn) {
    this.stepStart(stepName, component, description);
    
    try {
      const result = await asyncFn();
      this.stepEnd(stepName, true);
      return result;
    } catch (error) {
      this.error(component, `${description} failed`, error, { step: stepName });
      this.stepEnd(stepName, false, { error: error.message });
      throw error;
    }
  }

  /**
   * Track a synchronous function with automatic start/end logging
   */
  trackSync(stepName, component, description, syncFn) {
    this.stepStart(stepName, component, description);
    
    try {
      const result = syncFn();
      this.stepEnd(stepName, true);
      return result;
    } catch (error) {
      this.error(component, `${description} failed`, error, { step: stepName });
      this.stepEnd(stepName, false, { error: error.message });
      throw error;
    }
  }

  /**
   * Generate and write summary file
   */
  writeSummary() {
    const totalDuration = Date.now() - this.startTime;
    const successfulSteps = this.steps.filter(s => s.success);
    const failedSteps = this.steps.filter(s => !s.success);
    
    const summary = {
      startTime: new Date(this.startTime).toISOString(),
      endTime: new Date().toISOString(),
      totalDuration: `${totalDuration}ms`,
      totalDurationSeconds: (totalDuration / 1000).toFixed(2),
      success: failedSteps.length === 0 && this.errors.length === 0,
      
      statistics: {
        totalSteps: this.steps.length,
        successfulSteps: successfulSteps.length,
        failedSteps: failedSteps.length,
        totalErrors: this.errors.length,
        totalLogEntries: this.logEntries.length
      },
      
      steps: this.steps.map(step => ({
        name: step.name,
        component: step.component,
        description: step.description,
        duration: `${step.duration}ms`,
        success: step.success,
        startTime: step.startTimestamp,
        endTime: step.endTimestamp,
        metadata: step.metadata
      })),
      
      errors: this.errors.map(error => ({
        component: error.component,
        message: error.message,
        timestamp: error.timestamp,
        error: error.error ? {
          message: error.error.message,
          stack: error.error.stack,
          name: error.error.name
        } : null
      })),
      
      performance: {
        slowSteps: this.steps
          .filter(s => s.duration > 2000)
          .map(s => ({
            name: s.name,
            component: s.component,
            duration: `${s.duration}ms`
          }))
      },
      
      system: {
        platform: process.platform,
        arch: process.arch,
        nodeVersion: process.versions.node,
        electronVersion: process.versions.electron,
        appVersion: this.app.getVersion(),
        debugMode: this.debugMode
      },
      
      logFiles: {
        detailedLog: this.logFile,
        summary: this.summaryFile
      }
    };
    
    try {
      fs.writeFileSync(this.summaryFile, JSON.stringify(summary, null, 2));
      this.info('StartupLogger', 'Startup summary written', {
        summaryFile: this.summaryFile,
        totalDuration: summary.totalDuration,
        success: summary.success
      });
    } catch (error) {
      console.error('Failed to write summary file:', error);
    }
    
    return summary;
  }

  /**
   * Finalize logging and write summary
   */
  finalize() {
    const summary = this.writeSummary();
    
    this.info('StartupLogger', 'Application startup logging finalized', {
      totalDuration: summary.totalDuration,
      totalSteps: summary.statistics.totalSteps,
      successfulSteps: summary.statistics.successfulSteps,
      failedSteps: summary.statistics.failedSteps,
      success: summary.success
    });
    
    // Log final status to console
    console.log('='.repeat(60));
    console.log(`Startup ${summary.success ? 'SUCCESSFUL' : 'COMPLETED WITH ERRORS'}`);
    console.log(`Total Duration: ${summary.totalDurationSeconds}s`);
    console.log(`Steps: ${summary.statistics.successfulSteps}/${summary.statistics.totalSteps} successful`);
    if (summary.statistics.failedSteps > 0) {
      console.log(`Failed Steps: ${summary.statistics.failedSteps}`);
    }
    if (summary.errors.length > 0) {
      console.log(`Errors: ${summary.errors.length}`);
    }
    console.log(`Logs: ${this.logFile}`);
    console.log(`Summary: ${this.summaryFile}`);
    console.log('='.repeat(60));
    
    return summary;
  }

  /**
   * Get log file path
   */
  getLogFile() {
    return this.logFile;
  }

  /**
   * Get summary file path
   */
  getSummaryFile() {
    return this.summaryFile;
  }

  /**
   * Get logs directory
   */
  getLogsDirectory() {
    return this.logsDir;
  }
}

module.exports = StartupLogger;
