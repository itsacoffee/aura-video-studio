/**
 * Shutdown Orchestrator Module
 * Coordinates graceful shutdown of all application components
 */

const axios = require('axios');
const { exec } = require('child_process');

class ShutdownOrchestrator {
  constructor(app, logger) {
    this.app = app;
    this.logger = logger;
    this.isShuttingDown = false;
    this.shutdownStartTime = null;
    
    // Component references (will be set by main process)
    this.backendService = null;
    this.windowManager = null;
    this.trayManager = null;
    
    // Configuration - aggressive timeouts for fast shutdown
    this.GRACEFUL_TIMEOUT_MS = 2000;  // 2 seconds for graceful
    this.COMPONENT_TIMEOUT_MS = 1500; // 1.5 seconds per component
    this.FORCE_KILL_TIMEOUT_MS = 1000; // 1 second before force kill
    this.ABSOLUTE_TIMEOUT_MS = 4000;   // 4 seconds absolute maximum
    
    this.isWindows = process.platform === 'win32';
  }

  /**
   * Set component references
   */
  setComponents({ backendService, windowManager, trayManager, processManager }) {
    this.backendService = backendService;
    this.windowManager = windowManager;
    this.trayManager = trayManager;
    this.processManager = processManager;
  }

  /**
   * Check if active renders are in progress
   */
  async checkActiveRenders() {
    if (!this.backendService || !this.backendService.isRunning()) {
      return { hasActiveRenders: false, jobCount: 0 };
    }

    try {
      const port = this.backendService.getPort();
      const response = await axios.get(`http://localhost:${port}/api/jobs/active`, {
        timeout: 2000
      });
      
      const activeJobs = response.data?.jobs || [];
      return {
        hasActiveRenders: activeJobs.length > 0,
        jobCount: activeJobs.length,
        jobs: activeJobs
      };
    } catch (error) {
      this.logger.warn('Failed to check active renders:', error.message);
      return { hasActiveRenders: false, jobCount: 0 };
    }
  }

  /**
   * Show confirmation dialog if active renders exist
   */
  async showActiveRenderWarning(jobCount) {
    const { dialog } = require('electron');
    const mainWindow = this.windowManager?.getMainWindow();

    const result = await dialog.showMessageBox(mainWindow, {
      type: 'warning',
      title: 'Active Renders in Progress',
      message: `${jobCount} video generation job(s) are currently running.`,
      detail: 'Quitting now will cancel these jobs and you may lose progress. What would you like to do?',
      buttons: ['Cancel Quit', 'Wait for Completion', 'Force Quit'],
      defaultId: 0,
      cancelId: 0,
      noLink: true
    });

    return {
      action: result.response === 0 ? 'cancel' : 
              result.response === 1 ? 'wait' : 
              'force'
    };
  }

  /**
   * Failsafe: Kill all Aura-related processes by name
   * Used as last resort if normal shutdown fails
   */
  async killAllAuraProcesses() {
    this.logger.warn('FAILSAFE: Attempting to kill all Aura-related processes...');
    
    return new Promise((resolve) => {
      if (this.isWindows) {
        // Windows: Use taskkill to find and kill all processes matching patterns
        const patterns = [
          'Aura.Api.exe',
          'dotnet.exe',  // May be hosting Aura.Api
          'ffmpeg.exe',  // FFmpeg processes spawned by backend
          'Aura Video Studio.exe'
        ];
        
        // Build a command that kills all matching processes
        // Note: We'll only kill child processes, not our own Electron process
        const commands = patterns.map(pattern => 
          `taskkill /F /FI "IMAGENAME eq ${pattern}" /FI "PID ne ${process.pid}" 2>nul`
        ).join(' & ');
        
        exec(commands, (error, stdout, stderr) => {
          if (error) {
            this.logger.warn('Process kill command had errors (may be normal if processes already exited):', error.message);
          }
          if (stdout) this.logger.info('Failsafe kill output:', stdout);
          if (stderr) this.logger.debug('Failsafe kill stderr:', stderr);
          resolve();
        });
      } else {
        // Unix: Use pkill to find and kill processes by name
        const patterns = ['Aura.Api', 'ffmpeg'];
        let killedCount = 0;
        
        const killPromises = patterns.map(pattern => 
          new Promise((resolveKill) => {
            exec(`pkill -9 -f "${pattern}"`, (error) => {
              if (!error) killedCount++;
              resolveKill();
            });
          })
        );
        
        Promise.all(killPromises).then(() => {
          this.logger.info(`Failsafe killed ${killedCount} process types`);
          resolve();
        });
      }
    });
  }

  /**
   * Initiate graceful shutdown sequence
   */
  async initiateShutdown(options = {}) {
    if (this.isShuttingDown) {
      this.logger.warn('Shutdown already in progress');
      return { success: false, reason: 'already-shutting-down' };
    }

    this.isShuttingDown = true;
    this.shutdownStartTime = Date.now();

    const force = options.force || false;
    const skipChecks = options.skipChecks || false;

    this.logger.info('='.repeat(60));
    this.logger.info(`Initiating shutdown (Force: ${force}, SkipChecks: ${skipChecks}, AbsoluteTimeout: ${this.ABSOLUTE_TIMEOUT_MS}ms)`);
    this.logger.info('='.repeat(60));

    try {
      // Wrap entire shutdown in absolute timeout
      const shutdownResult = await Promise.race([
        this._executeShutdownSteps(force, skipChecks),
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Absolute shutdown timeout exceeded')), this.ABSOLUTE_TIMEOUT_MS)
        )
      ]);
      
      // Check if user cancelled
      if (shutdownResult && shutdownResult.reason === 'user-cancelled') {
        return shutdownResult;
      }

      const elapsed = Date.now() - this.shutdownStartTime;
      this.logger.info('='.repeat(60));
      this.logger.info(`Shutdown completed successfully in ${elapsed}ms`);
      this.logger.info('='.repeat(60));

      return { success: true, elapsed };

    } catch (error) {
      const elapsed = Date.now() - this.shutdownStartTime;
      this.logger.error(`Shutdown error after ${elapsed}ms: ${error.message}`);
      
      // Activate failsafe: kill all Aura processes by name
      this.logger.error('Activating failsafe process termination...');
      try {
        await this.killAllAuraProcesses();
      } catch (failsafeError) {
        this.logger.error('Failsafe also failed:', failsafeError.message);
      }
      
      return { success: false, error: error.message, elapsed };
    }
  }

  /**
   * Execute shutdown steps
   */
  async _executeShutdownSteps(force, skipChecks) {
    // Step 1: Check for active renders (unless skipped)
    if (!skipChecks && !force) {
      const renderCheck = await this.checkActiveRenders();
      
      if (renderCheck.hasActiveRenders) {
        this.logger.warn(`Found ${renderCheck.jobCount} active render(s)`);
        
        const userChoice = await this.showActiveRenderWarning(renderCheck.jobCount);
        
        if (userChoice.action === 'cancel') {
          this.logger.info('User cancelled shutdown');
          this.isShuttingDown = false;
          return { success: false, reason: 'user-cancelled' };
        } else if (userChoice.action === 'wait') {
          this.logger.info('User chose to wait for renders to complete');
          await this.waitForRenders();
        }
      }
    }

    // Step 2: Close windows gracefully
    const windowStep = await this.closeWindows();
    this.logger.info(`Step 1/5 Complete: ${windowStep}`);

    // Step 3: Signal backend to shutdown
    const backendSignalStep = await this.signalBackendShutdown();
    this.logger.info(`Step 2/5 Complete: ${backendSignalStep}`);

    // Step 4: Stop backend service
    const backendStep = await this.stopBackend(force);
    this.logger.info(`Step 3/5 Complete: ${backendStep}`);

    // Step 5: Terminate all tracked child processes
    const processStep = await this.terminateAllProcesses(force);
    this.logger.info(`Step 4/5 Complete: ${processStep}`);

    // Step 6: Cleanup resources
    const cleanupStep = await this.cleanup();
    this.logger.info(`Step 5/5 Complete: ${cleanupStep}`);
  }

  /**
   * Wait for active renders to complete
   */
  async waitForRenders() {
    this.logger.info('Waiting for active renders to complete...');
    
    const checkInterval = 5000;
    const maxWaitTime = 300000; // 5 minutes
    const startTime = Date.now();

    while (Date.now() - startTime < maxWaitTime) {
      const renderCheck = await this.checkActiveRenders();
      
      if (!renderCheck.hasActiveRenders) {
        this.logger.info('All renders completed');
        return;
      }

      this.logger.info(`Still waiting... ${renderCheck.jobCount} job(s) active`);
      await new Promise(resolve => setTimeout(resolve, checkInterval));
    }

    this.logger.warn('Timeout waiting for renders, proceeding with shutdown');
  }

  /**
   * Close all windows gracefully
   */
  async closeWindows() {
    this.logger.info('Closing application windows...');

    try {
      if (this.windowManager) {
        const mainWindow = this.windowManager.getMainWindow();
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.close();
        }

        const splashWindow = this.windowManager.getSplashWindow();
        if (splashWindow && !splashWindow.isDestroyed()) {
          splashWindow.close();
        }
      }

      if (this.trayManager) {
        this.trayManager.destroy();
      }

      return 'Windows closed';
    } catch (error) {
      this.logger.error('Error closing windows:', error);
      return `Windows close error: ${error.message}`;
    }
  }

  /**
   * Signal backend to shutdown via API
   */
  async signalBackendShutdown() {
    if (!this.backendService || !this.backendService.isRunning()) {
      return 'Backend not running';
    }

    try {
      const port = this.backendService.getPort();
      this.logger.info(`Signaling backend shutdown via API (port ${port})...`);
      
      await axios.post(`http://localhost:${port}/api/system/shutdown`, {}, {
        timeout: 3000
      });

      return 'Backend shutdown signal sent';
    } catch (error) {
      if (error.code === 'ECONNREFUSED' || error.code === 'ETIMEDOUT') {
        return 'Backend already stopped';
      }
      this.logger.warn('Failed to signal backend shutdown:', error.message);
      return `Backend signal failed: ${error.message}`;
    }
  }

  /**
   * Stop backend service
   */
  async stopBackend(force = false) {
    if (!this.backendService) {
      return 'No backend service';
    }

    try {
      this.logger.info(`Stopping backend service (Force: ${force})...`);
      
      const timeout = force ? this.FORCE_KILL_TIMEOUT_MS : this.COMPONENT_TIMEOUT_MS;
      
      const stopPromise = this.backendService.stop();
      const timeoutPromise = new Promise((resolve, reject) => {
        setTimeout(() => reject(new Error('Backend stop timeout')), timeout);
      });

      await Promise.race([stopPromise, timeoutPromise]);

      return 'Backend stopped';
    } catch (error) {
      if (error.message.includes('timeout')) {
        this.logger.warn('Backend stop timeout, attempting force kill...');
        await this.forceKillBackend();
        return 'Backend force killed after timeout';
      }
      this.logger.error('Error stopping backend:', error);
      return `Backend stop error: ${error.message}`;
    }
  }

  /**
   * Terminate all tracked child processes
   */
  async terminateAllProcesses(force = false) {
    if (!this.processManager) {
      return 'No process manager available';
    }

    const processCount = this.processManager.getProcessCount();
    
    if (processCount === 0) {
      return 'No child processes to terminate';
    }

    this.logger.info(`Terminating ${processCount} tracked child process(es)...`);

    try {
      const timeout = force ? this.FORCE_KILL_TIMEOUT_MS : this.COMPONENT_TIMEOUT_MS;
      const results = await this.processManager.terminateAll(timeout);

      if (results.success) {
        return `Terminated ${results.terminated} process(es)`;
      } else {
        return `Terminated ${results.terminated} process(es), ${results.failed} failed`;
      }
    } catch (error) {
      this.logger.error('Error terminating processes:', error);
      return `Process termination error: ${error.message}`;
    }
  }

  /**
   * Force kill backend and child processes
   */
  async forceKillBackend() {
    if (!this.backendService) {
      return;
    }

    const pid = this.backendService.pid;
    if (!pid) {
      return;
    }

    this.logger.warn(`Force killing backend process tree (PID: ${pid})`);

    if (this.isWindows) {
      return new Promise((resolve) => {
        exec(`taskkill /F /T /PID ${pid}`, (error, stdout, stderr) => {
          if (error) {
            this.logger.error('Force kill error:', error);
          } else {
            this.logger.info('Backend process tree terminated');
          }
          resolve();
        });
      });
    } else {
      try {
        process.kill(-pid, 'SIGKILL');
        this.logger.info('Backend process group terminated');
      } catch (error) {
        this.logger.error('Force kill error:', error);
      }
    }
  }

  /**
   * Cleanup temporary files and resources
   */
  async cleanup() {
    this.logger.info('Performing cleanup...');

    try {
      const fs = require('fs');
      const tempPath = require('path').join(this.app.getPath('temp'), 'aura-video-studio');
      
      if (fs.existsSync(tempPath)) {
        fs.rmSync(tempPath, { recursive: true, force: true });
        this.logger.info('Temporary files cleaned up');
      }

      return 'Cleanup complete';
    } catch (error) {
      this.logger.warn('Cleanup error:', error.message);
      return `Cleanup partial: ${error.message}`;
    }
  }

  /**
   * Force shutdown - last resort
   */
  async forceShutdown() {
    this.logger.error('Initiating force shutdown...');

    try {
      if (this.backendService) {
        await this.forceKillBackend();
      }

      if (this.windowManager) {
        const mainWindow = this.windowManager.getMainWindow();
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.destroy();
        }
      }

      await this.cleanup();

    } catch (error) {
      this.logger.error('Force shutdown error:', error);
    }
  }

  /**
   * Get shutdown status
   */
  getStatus() {
    return {
      isShuttingDown: this.isShuttingDown,
      elapsedMs: this.shutdownStartTime ? Date.now() - this.shutdownStartTime : 0
    };
  }
}

module.exports = ShutdownOrchestrator;
