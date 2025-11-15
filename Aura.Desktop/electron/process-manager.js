/**
 * Process Manager Module
 * Centralized tracking and management of all child processes spawned by the Electron app
 * 
 * This module provides:
 * - Centralized registry of all child processes
 * - Diagnostic logging for process lifecycle events
 * - Cleanup utilities to ensure no orphaned processes
 * - Process tree termination on Windows
 */

const { exec } = require('child_process');
const { EventEmitter } = require('events');

class ProcessManager extends EventEmitter {
  constructor(logger) {
    super();
    this.logger = logger || console;
    this.processes = new Map(); // Map<processId, ProcessInfo>
    this.isWindows = process.platform === 'win32';
    
    // Track our own PID to avoid killing ourselves
    this.electronPid = process.pid;
    
    this.logger.info?.('ProcessManager', 'Initialized', {
      platform: process.platform,
      electronPid: this.electronPid
    });
  }

  /**
   * Register a child process for tracking
   * @param {string} name - Human-readable process name
   * @param {ChildProcess} process - Node.js child process
   * @param {Object} metadata - Additional metadata
   */
  register(name, process, metadata = {}) {
    if (!process || !process.pid) {
      this.logger.warn?.('ProcessManager', 'Cannot register process without PID', { name });
      return;
    }

    const processInfo = {
      name,
      pid: process.pid,
      startTime: Date.now(),
      metadata,
      process
    };

    this.processes.set(process.pid, processInfo);

    this.logger.info?.('ProcessManager', 'Process registered', {
      name,
      pid: process.pid,
      metadata
    });

    // Listen for process exit
    process.on('exit', (code, signal) => {
      this.unregister(process.pid, code, signal);
    });

    this.emit('process-registered', processInfo);
  }

  /**
   * Unregister a process (called when it exits)
   * @param {number} pid - Process ID
   * @param {number} code - Exit code
   * @param {string} signal - Signal that caused exit
   */
  unregister(pid, code, signal) {
    const processInfo = this.processes.get(pid);
    if (!processInfo) {
      return;
    }

    const lifetime = Date.now() - processInfo.startTime;

    this.logger.info?.('ProcessManager', 'Process exited', {
      name: processInfo.name,
      pid,
      code,
      signal,
      lifetimeMs: lifetime
    });

    this.processes.delete(pid);
    this.emit('process-exited', { ...processInfo, code, signal, lifetime });
  }

  /**
   * Get all tracked processes
   * @returns {Array} Array of process information objects
   */
  getAllProcesses() {
    return Array.from(this.processes.values());
  }

  /**
   * Get process count
   * @returns {number} Number of tracked processes
   */
  getProcessCount() {
    return this.processes.size;
  }

  /**
   * Get process by PID
   * @param {number} pid - Process ID
   * @returns {Object|null} Process information or null
   */
  getProcess(pid) {
    return this.processes.get(pid) || null;
  }

  /**
   * Check if a process is tracked
   * @param {number} pid - Process ID
   * @returns {boolean} True if tracked
   */
  hasProcess(pid) {
    return this.processes.has(pid);
  }

  /**
   * Terminate a specific process
   * @param {number} pid - Process ID
   * @param {boolean} force - Force kill immediately
   * @returns {Promise<boolean>} True if terminated successfully
   */
  async terminate(pid, force = false) {
    const processInfo = this.processes.get(pid);
    if (!processInfo) {
      this.logger.warn?.('ProcessManager', 'Cannot terminate untracked process', { pid });
      return false;
    }

    this.logger.info?.('ProcessManager', 'Terminating process', {
      name: processInfo.name,
      pid,
      force
    });

    try {
      if (this.isWindows) {
        await this._windowsTerminate(pid, force);
      } else {
        await this._unixTerminate(processInfo.process, force);
      }
      return true;
    } catch (error) {
      this.logger.error?.('ProcessManager', 'Failed to terminate process', error, {
        name: processInfo.name,
        pid
      });
      return false;
    }
  }

  /**
   * Terminate all tracked processes
   * @param {number} timeout - Timeout in ms for graceful shutdown
   * @returns {Promise<Object>} Results of termination
   */
  async terminateAll(timeout = 5000) {
    const processes = this.getAllProcesses();
    
    if (processes.length === 0) {
      this.logger.info?.('ProcessManager', 'No processes to terminate');
      return { success: true, terminated: 0, failed: 0 };
    }

    this.logger.info?.('ProcessManager', 'Terminating all processes', {
      count: processes.length,
      timeoutMs: timeout
    });

    const results = {
      success: true,
      terminated: 0,
      failed: 0,
      details: []
    };

    // First, try graceful termination
    const gracefulPromises = processes.map(async (info) => {
      try {
        await this.terminate(info.pid, false);
        results.terminated++;
        results.details.push({ pid: info.pid, name: info.name, status: 'graceful' });
      } catch (error) {
        this.logger.warn?.('ProcessManager', 'Graceful termination failed', {
          pid: info.pid,
          name: info.name,
          error: error.message
        });
        results.details.push({ pid: info.pid, name: info.name, status: 'graceful-failed' });
      }
    });

    // Wait for graceful termination with timeout
    try {
      await Promise.race([
        Promise.all(gracefulPromises),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Graceful termination timeout')), timeout)
        )
      ]);
    } catch (error) {
      this.logger.warn?.('ProcessManager', 'Graceful termination timeout, forcing...', {
        error: error.message
      });
    }

    // Force kill any remaining processes
    const remaining = this.getAllProcesses();
    if (remaining.length > 0) {
      this.logger.warn?.('ProcessManager', 'Force killing remaining processes', {
        count: remaining.length
      });

      for (const info of remaining) {
        try {
          await this.terminate(info.pid, true);
          results.terminated++;
          results.details.push({ pid: info.pid, name: info.name, status: 'forced' });
        } catch (error) {
          results.failed++;
          results.success = false;
          results.details.push({ 
            pid: info.pid, 
            name: info.name, 
            status: 'failed',
            error: error.message 
          });
        }
      }
    }

    this.logger.info?.('ProcessManager', 'All processes terminated', results);
    return results;
  }

  /**
   * Windows-specific process termination using taskkill
   * @param {number} pid - Process ID
   * @param {boolean} force - Use /F flag for force kill
   * @returns {Promise<void>}
   */
  _windowsTerminate(pid, force) {
    return new Promise((resolve, reject) => {
      const forceFlag = force ? '/F' : '';
      const command = `taskkill /PID ${pid} ${forceFlag} /T`;
      
      this.logger.debug?.('ProcessManager', 'Executing taskkill', { command });

      exec(command, (error, stdout, stderr) => {
        if (error) {
          // Process may have already exited
          if (error.code === 128 || stderr.includes('not found')) {
            this.logger.debug?.('ProcessManager', 'Process already exited', { pid });
            resolve();
          } else {
            reject(error);
          }
        } else {
          this.logger.debug?.('ProcessManager', 'taskkill success', { pid, stdout });
          resolve();
        }
      });
    });
  }

  /**
   * Unix-specific process termination using signals
   * @param {ChildProcess} process - Node.js child process
   * @param {boolean} force - Use SIGKILL instead of SIGTERM
   * @returns {Promise<void>}
   */
  _unixTerminate(process, force) {
    return new Promise((resolve, reject) => {
      const signal = force ? 'SIGKILL' : 'SIGTERM';
      
      this.logger.debug?.('ProcessManager', 'Sending signal', { 
        pid: process.pid, 
        signal 
      });

      try {
        process.kill(signal);
        
        // Wait for process to exit
        const timeout = setTimeout(() => {
          reject(new Error('Process did not exit after signal'));
        }, 5000);

        process.once('exit', () => {
          clearTimeout(timeout);
          resolve();
        });
      } catch (error) {
        reject(error);
      }
    });
  }

  /**
   * Get diagnostic information about all processes
   * @returns {Object} Diagnostic information
   */
  getDiagnostics() {
    const processes = this.getAllProcesses();
    const now = Date.now();

    return {
      processCount: processes.length,
      processes: processes.map(info => ({
        name: info.name,
        pid: info.pid,
        uptimeMs: now - info.startTime,
        metadata: info.metadata
      })),
      platform: process.platform,
      electronPid: this.electronPid
    };
  }

  /**
   * Cleanup and reset
   */
  cleanup() {
    this.logger.info?.('ProcessManager', 'Cleaning up process manager');
    this.processes.clear();
    this.removeAllListeners();
  }
}

module.exports = ProcessManager;
