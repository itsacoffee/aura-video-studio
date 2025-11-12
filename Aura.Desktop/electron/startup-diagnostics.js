/**
 * Startup Diagnostics Module
 * Runs comprehensive health checks on application startup and logs results
 */

const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');
const { promisify } = require('util');
const net = require('net');

const execAsync = promisify(exec);

class StartupDiagnostics {
  constructor(app, logger) {
    this.app = app;
    this.logger = logger;
    this.checks = [];
  }

  /**
   * Check if a port is available
   */
  async _checkPort(port) {
    return new Promise((resolve) => {
      const server = net.createServer();
      
      server.once('error', () => {
        resolve(false);
      });
      
      server.once('listening', () => {
        server.close();
        resolve(true);
      });
      
      server.listen(port);
    });
  }

  /**
   * Check disk space
   */
  async _checkDiskSpace() {
    try {
      const userDataPath = this.app.getPath('userData');
      
      if (process.platform === 'win32') {
        const drive = userDataPath.substring(0, 2);
        const { stdout } = await execAsync(`wmic logicaldisk where "DeviceID='${drive}' get FreeSpace,Size /format:list"`);
        
        const freeSpaceMatch = stdout.match(/FreeSpace=(\d+)/);
        const totalSpaceMatch = stdout.match(/Size=(\d+)/);
        
        if (freeSpaceMatch && totalSpaceMatch) {
          const freeSpace = parseInt(freeSpaceMatch[1]);
          const totalSpace = parseInt(totalSpaceMatch[1]);
          const freeSpaceGB = (freeSpace / 1024 / 1024 / 1024).toFixed(2);
          const totalSpaceGB = (totalSpace / 1024 / 1024 / 1024).toFixed(2);
          const percentFree = ((freeSpace / totalSpace) * 100).toFixed(1);
          
          return {
            available: true,
            freeSpace: freeSpaceGB + ' GB',
            totalSpace: totalSpaceGB + ' GB',
            percentFree: percentFree + '%',
            adequate: freeSpace > 1024 * 1024 * 1024 // At least 1GB free
          };
        }
      } else {
        const { stdout } = await execAsync(`df -k "${userDataPath}"`);
        const lines = stdout.trim().split('\n');
        
        if (lines.length > 1) {
          const parts = lines[1].split(/\s+/);
          const available = parseInt(parts[3]) * 1024; // Convert KB to bytes
          const freeSpaceGB = (available / 1024 / 1024 / 1024).toFixed(2);
          
          return {
            available: true,
            freeSpace: freeSpaceGB + ' GB',
            adequate: available > 1024 * 1024 * 1024
          };
        }
      }
      
      return { available: false, error: 'Could not determine disk space' };
    } catch (error) {
      return { available: false, error: error.message };
    }
  }

  /**
   * Check memory availability
   */
  _checkMemory() {
    const totalMemory = require('os').totalmem();
    const freeMemory = require('os').freemem();
    const usedMemory = totalMemory - freeMemory;
    
    const totalGB = (totalMemory / 1024 / 1024 / 1024).toFixed(2);
    const freeGB = (freeMemory / 1024 / 1024 / 1024).toFixed(2);
    const usedGB = (usedMemory / 1024 / 1024 / 1024).toFixed(2);
    const percentUsed = ((usedMemory / totalMemory) * 100).toFixed(1);
    
    return {
      available: true,
      total: totalGB + ' GB',
      free: freeGB + ' GB',
      used: usedGB + ' GB',
      percentUsed: percentUsed + '%',
      adequate: freeMemory > 512 * 1024 * 1024 // At least 512MB free
    };
  }

  /**
   * Check if required directories exist and are writable
   */
  async _checkDirectories() {
    const directories = [
      { name: 'userData', path: this.app.getPath('userData') },
      { name: 'temp', path: this.app.getPath('temp') },
      { name: 'logs', path: path.join(this.app.getPath('userData'), 'logs') },
      { name: 'cache', path: this.app.getPath('cache') }
    ];
    
    const results = [];
    
    for (const dir of directories) {
      try {
        // Check if directory exists
        const exists = fs.existsSync(dir.path);
        
        if (!exists) {
          // Try to create it
          fs.mkdirSync(dir.path, { recursive: true });
        }
        
        // Test write access
        const testFile = path.join(dir.path, `.write-test-${Date.now()}`);
        fs.writeFileSync(testFile, 'test');
        fs.unlinkSync(testFile);
        
        results.push({
          name: dir.name,
          path: dir.path,
          exists: true,
          writable: true,
          status: 'ok'
        });
      } catch (error) {
        results.push({
          name: dir.name,
          path: dir.path,
          exists: fs.existsSync(dir.path),
          writable: false,
          status: 'error',
          error: error.message
        });
      }
    }
    
    return results;
  }

  /**
   * Check FFmpeg availability
   */
  async _checkFFmpeg() {
    try {
      const { stdout, stderr } = await execAsync('ffmpeg -version');
      const versionMatch = stdout.match(/ffmpeg version ([^\s]+)/);
      
      return {
        available: true,
        version: versionMatch ? versionMatch[1] : 'unknown',
        status: 'ok'
      };
    } catch (error) {
      return {
        available: false,
        status: 'not found',
        error: 'FFmpeg not found in PATH'
      };
    }
  }

  /**
   * Check .NET runtime availability
   */
  async _checkDotNet() {
    try {
      const { stdout } = await execAsync('dotnet --version');
      const version = stdout.trim();
      
      return {
        available: true,
        version,
        status: 'ok'
      };
    } catch (error) {
      return {
        available: false,
        status: 'not found',
        error: '.NET runtime not found'
      };
    }
  }

  /**
   * Check Node.js version
   */
  _checkNodeVersion() {
    const nodeVersion = process.versions.node;
    const majorVersion = parseInt(nodeVersion.split('.')[0]);
    
    return {
      available: true,
      version: nodeVersion,
      adequate: majorVersion >= 18,
      status: majorVersion >= 18 ? 'ok' : 'outdated'
    };
  }

  /**
   * Check system platform and architecture
   */
  _checkPlatform() {
    return {
      platform: process.platform,
      arch: process.arch,
      isWindows: process.platform === 'win32',
      isMac: process.platform === 'darwin',
      isLinux: process.platform === 'linux',
      supported: ['win32', 'darwin', 'linux'].includes(process.platform),
      status: 'ok'
    };
  }

  /**
   * Run all diagnostic checks
   */
  async runDiagnostics() {
    this.logger.info('StartupDiagnostics', 'Running startup diagnostics...');
    
    const diagnostics = {
      timestamp: new Date().toISOString(),
      checks: {}
    };
    
    // Platform check
    this.logger.debug('StartupDiagnostics', 'Checking platform...');
    diagnostics.checks.platform = this._checkPlatform();
    
    // Node.js version check
    this.logger.debug('StartupDiagnostics', 'Checking Node.js version...');
    diagnostics.checks.nodeVersion = this._checkNodeVersion();
    
    // Memory check
    this.logger.debug('StartupDiagnostics', 'Checking memory...');
    diagnostics.checks.memory = this._checkMemory();
    
    // Disk space check
    this.logger.debug('StartupDiagnostics', 'Checking disk space...');
    diagnostics.checks.diskSpace = await this._checkDiskSpace();
    
    // Directories check
    this.logger.debug('StartupDiagnostics', 'Checking directories...');
    diagnostics.checks.directories = await this._checkDirectories();
    
    // FFmpeg check (non-critical)
    this.logger.debug('StartupDiagnostics', 'Checking FFmpeg...');
    diagnostics.checks.ffmpeg = await this._checkFFmpeg();
    
    // .NET check (non-critical, only if backend is local)
    this.logger.debug('StartupDiagnostics', 'Checking .NET runtime...');
    diagnostics.checks.dotnet = await this._checkDotNet();
    
    // Port availability check (default backend port)
    this.logger.debug('StartupDiagnostics', 'Checking port availability...');
    const defaultPort = 5005;
    const portAvailable = await this._checkPort(defaultPort);
    diagnostics.checks.port = {
      port: defaultPort,
      available: portAvailable,
      status: portAvailable ? 'ok' : 'in use'
    };
    
    // Determine overall health
    diagnostics.healthy = this._evaluateHealth(diagnostics.checks);
    diagnostics.warnings = this._collectWarnings(diagnostics.checks);
    diagnostics.errors = this._collectErrors(diagnostics.checks);
    
    // Log results
    this.logger.info('StartupDiagnostics', 'Diagnostics completed', {
      healthy: diagnostics.healthy,
      warnings: diagnostics.warnings.length,
      errors: diagnostics.errors.length
    });
    
    // Log warnings
    diagnostics.warnings.forEach(warning => {
      this.logger.warn('StartupDiagnostics', warning.message, warning.details);
    });
    
    // Log errors
    diagnostics.errors.forEach(error => {
      this.logger.error('StartupDiagnostics', error.message, null, error.details);
    });
    
    return diagnostics;
  }

  /**
   * Evaluate overall system health
   */
  _evaluateHealth(checks) {
    // Critical checks that must pass
    const criticalChecks = [
      checks.platform.supported,
      checks.nodeVersion.adequate,
      checks.memory.adequate,
      checks.directories.every(d => d.status === 'ok')
    ];
    
    return criticalChecks.every(check => check === true);
  }

  /**
   * Collect warnings from diagnostic results
   */
  _collectWarnings(checks) {
    const warnings = [];
    
    if (!checks.diskSpace.adequate) {
      warnings.push({
        message: 'Low disk space detected',
        details: { freeSpace: checks.diskSpace.freeSpace }
      });
    }
    
    if (!checks.memory.adequate) {
      warnings.push({
        message: 'Low memory available',
        details: { freeMemory: checks.memory.free }
      });
    }
    
    if (!checks.ffmpeg.available) {
      warnings.push({
        message: 'FFmpeg not found - video rendering will not work',
        details: { status: checks.ffmpeg.status }
      });
    }
    
    if (!checks.dotnet.available) {
      warnings.push({
        message: '.NET runtime not found - backend may not start',
        details: { status: checks.dotnet.status }
      });
    }
    
    if (!checks.port.available) {
      warnings.push({
        message: `Port ${checks.port.port} is already in use`,
        details: { port: checks.port.port }
      });
    }
    
    return warnings;
  }

  /**
   * Collect errors from diagnostic results
   */
  _collectErrors(checks) {
    const errors = [];
    
    if (!checks.platform.supported) {
      errors.push({
        message: 'Unsupported platform',
        details: { platform: checks.platform.platform }
      });
    }
    
    if (!checks.nodeVersion.adequate) {
      errors.push({
        message: 'Node.js version too old',
        details: { 
          currentVersion: checks.nodeVersion.version,
          requiredVersion: '18.x or higher'
        }
      });
    }
    
    const failedDirectories = checks.directories.filter(d => d.status !== 'ok');
    if (failedDirectories.length > 0) {
      errors.push({
        message: 'Critical directories not accessible',
        details: { 
          directories: failedDirectories.map(d => ({
            name: d.name,
            path: d.path,
            error: d.error
          }))
        }
      });
    }
    
    return errors;
  }
}

module.exports = StartupDiagnostics;
