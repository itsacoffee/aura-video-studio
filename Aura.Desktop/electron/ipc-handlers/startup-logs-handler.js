/**
 * Startup Logs IPC Handler
 * Provides access to startup logs and diagnostics
 */

const { ipcMain } = require('electron');
const fs = require('fs');
const path = require('path');

class StartupLogsHandler {
  constructor(app, startupLogger) {
    this.app = app;
    this.startupLogger = startupLogger;
    this.logsDir = path.join(app.getPath('userData'), 'logs');
  }

  /**
   * Register IPC handlers
   */
  register() {
    // Get latest startup log
    ipcMain.handle('startup-logs:get-latest', async () => {
      try {
        if (this.startupLogger) {
          return {
            success: true,
            logFile: this.startupLogger.getLogFile(),
            summaryFile: this.startupLogger.getSummaryFile()
          };
        }
        
        return {
          success: false,
          error: 'Startup logger not available'
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // Get startup summary
    ipcMain.handle('startup-logs:get-summary', async () => {
      try {
        if (!this.startupLogger) {
          return {
            success: false,
            error: 'Startup logger not available'
          };
        }

        const summaryFile = this.startupLogger.getSummaryFile();
        
        if (!fs.existsSync(summaryFile)) {
          return {
            success: false,
            error: 'Summary file not found'
          };
        }

        const summaryContent = fs.readFileSync(summaryFile, 'utf8');
        const summary = JSON.parse(summaryContent);

        return {
          success: true,
          summary
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // Get startup log content
    ipcMain.handle('startup-logs:get-log-content', async () => {
      try {
        if (!this.startupLogger) {
          return {
            success: false,
            error: 'Startup logger not available'
          };
        }

        const logFile = this.startupLogger.getLogFile();
        
        if (!fs.existsSync(logFile)) {
          return {
            success: false,
            error: 'Log file not found'
          };
        }

        const logContent = fs.readFileSync(logFile, 'utf8');
        const entries = logContent
          .trim()
          .split('\n')
          .filter(line => line.trim())
          .map(line => {
            try {
              return JSON.parse(line);
            } catch (e) {
              return { raw: line };
            }
          });

        return {
          success: true,
          entries,
          totalEntries: entries.length
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // List all startup logs
    ipcMain.handle('startup-logs:list', async () => {
      try {
        if (!fs.existsSync(this.logsDir)) {
          return {
            success: true,
            logs: []
          };
        }

        const files = fs.readdirSync(this.logsDir);
        
        const logs = files
          .filter(f => f.startsWith('startup-') && f.endsWith('.log'))
          .map(f => {
            const filePath = path.join(this.logsDir, f);
            const stats = fs.statSync(filePath);
            const summaryPath = filePath.replace('.log', '.json').replace('startup-', 'startup-summary-');
            
            return {
              name: f,
              path: filePath,
              size: stats.size,
              created: stats.birthtime,
              modified: stats.mtime,
              hasSummary: fs.existsSync(summaryPath),
              summaryPath: fs.existsSync(summaryPath) ? summaryPath : null
            };
          })
          .sort((a, b) => b.modified.getTime() - a.modified.getTime());

        return {
          success: true,
          logs
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // Read specific log file
    ipcMain.handle('startup-logs:read-file', async (event, filePath) => {
      try {
        if (!filePath || !filePath.startsWith(this.logsDir)) {
          return {
            success: false,
            error: 'Invalid file path'
          };
        }

        if (!fs.existsSync(filePath)) {
          return {
            success: false,
            error: 'File not found'
          };
        }

        const content = fs.readFileSync(filePath, 'utf8');
        
        // If it's a JSON file, parse it
        if (filePath.endsWith('.json')) {
          const parsed = JSON.parse(content);
          return {
            success: true,
            content: parsed,
            type: 'json'
          };
        }
        
        // Otherwise return as text
        return {
          success: true,
          content,
          type: 'text'
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    // Open logs directory
    ipcMain.handle('startup-logs:open-directory', async () => {
      try {
        const { shell } = require('electron');
        
        if (!fs.existsSync(this.logsDir)) {
          fs.mkdirSync(this.logsDir, { recursive: true });
        }
        
        await shell.openPath(this.logsDir);
        
        return {
          success: true,
          path: this.logsDir
        };
      } catch (error) {
        return {
          success: false,
          error: error.message
        };
      }
    });

    console.log('Startup logs IPC handlers registered');
  }

  /**
   * Unregister IPC handlers
   */
  unregister() {
    ipcMain.removeHandler('startup-logs:get-latest');
    ipcMain.removeHandler('startup-logs:get-summary');
    ipcMain.removeHandler('startup-logs:get-log-content');
    ipcMain.removeHandler('startup-logs:list');
    ipcMain.removeHandler('startup-logs:read-file');
    ipcMain.removeHandler('startup-logs:open-directory');
  }
}

module.exports = StartupLogsHandler;
