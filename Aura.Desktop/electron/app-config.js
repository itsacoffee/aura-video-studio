/**
 * Application Configuration Module
 * Handles application-wide configuration and settings
 */

const Store = require('electron-store');
const path = require('path');

class AppConfig {
  constructor(app) {
    this.app = app;
    
    // Main configuration store
    this.store = new Store({
      name: 'aura-config',
      encryptionKey: process.env.CONFIG_ENCRYPTION_KEY || 'aura-video-studio-secure-key-v1',
      defaults: {
        setupComplete: false,
        firstRun: true,
        language: 'en',
        theme: 'dark',
        autoUpdate: true,
        telemetry: false,
        crashReporting: false,
        minimizeToTray: false, // Default to false - user must explicitly enable background mode
        startMinimized: false,
        hardwareAcceleration: true,
        crashCount: 0,
        lastCrashTime: null,
        safeMode: false
      }
    });

    // Secure storage for sensitive data (API keys, tokens)
    this.secureStore = new Store({
      name: 'aura-secure',
      encryptionKey: this._getEncryptionKey(),
      defaults: {}
    });

    // Recent files/projects store
    this.recentStore = new Store({
      name: 'aura-recent',
      defaults: {
        projects: [],
        maxRecentItems: 10
      }
    });
  }

  /**
   * Get configuration value
   */
  get(key, defaultValue) {
    return this.store.get(key, defaultValue);
  }

  /**
   * Set configuration value
   */
  set(key, value) {
    this.store.set(key, value);
  }

  /**
   * Get all configuration
   */
  getAll() {
    return this.store.store;
  }

  /**
   * Reset all configuration to defaults
   */
  reset() {
    this.store.clear();
  }

  /**
   * Get secure value (for API keys, tokens)
   */
  getSecure(key) {
    return this.secureStore.get(key);
  }

  /**
   * Set secure value (for API keys, tokens)
   */
  setSecure(key, value) {
    this.secureStore.set(key, value);
  }

  /**
   * Delete secure value
   */
  deleteSecure(key) {
    this.secureStore.delete(key);
  }

  /**
   * Add recent project
   */
  addRecentProject(projectPath, projectName) {
    const recent = this.recentStore.get('projects', []);
    const maxItems = this.recentStore.get('maxRecentItems', 10);

    // Remove if already exists
    const filtered = recent.filter(item => item.path !== projectPath);

    // Add to beginning
    filtered.unshift({
      path: projectPath,
      name: projectName,
      timestamp: Date.now()
    });

    // Limit to max items
    const limited = filtered.slice(0, maxItems);

    this.recentStore.set('projects', limited);
    return limited;
  }

  /**
   * Get recent projects
   */
  getRecentProjects() {
    return this.recentStore.get('projects', []);
  }

  /**
   * Clear recent projects
   */
  clearRecentProjects() {
    this.recentStore.set('projects', []);
  }

  /**
   * Remove recent project
   */
  removeRecentProject(projectPath) {
    const recent = this.recentStore.get('projects', []);
    const filtered = recent.filter(item => item.path !== projectPath);
    this.recentStore.set('projects', filtered);
    return filtered;
  }

  /**
   * Get application paths
   */
  getPaths() {
    return {
      userData: this.app.getPath('userData'),
      temp: this.app.getPath('temp'),
      home: this.app.getPath('home'),
      documents: this.app.getPath('documents'),
      videos: this.app.getPath('videos'),
      downloads: this.app.getPath('downloads'),
      desktop: this.app.getPath('desktop'),
      logs: path.join(this.app.getPath('userData'), 'logs'),
      cache: path.join(this.app.getPath('userData'), 'cache'),
      projects: path.join(this.app.getPath('documents'), 'Aura Projects')
    };
  }

  /**
   * Check if this is the first run
   */
  isFirstRun() {
    return this.get('firstRun', true);
  }

  /**
   * Check if setup is complete
   */
  isSetupComplete() {
    return this.get('setupComplete', false);
  }

  /**
   * Mark first run as complete
   */
  completeFirstRun() {
    this.set('firstRun', false);
  }

  /**
   * Mark setup as complete
   */
  completeSetup() {
    this.set('setupComplete', true);
    this.set('firstRun', false);
  }

  /**
   * Get encryption key for secure storage
   */
  _getEncryptionKey() {
    // In production, this should be generated per-machine
    // For now, use a consistent key
    if (process.env.AURA_ENCRYPTION_KEY) {
      return process.env.AURA_ENCRYPTION_KEY;
    }
    
    // Generate a machine-specific key based on machine ID
    const crypto = require('crypto');
    const os = require('os');
    const machineId = os.hostname() + os.platform() + os.arch();
    return crypto.createHash('sha256').update(machineId).digest('hex');
  }

  /**
   * Crash counter management
   */
  getCrashCount() {
    return this.get('crashCount', 0);
  }

  incrementCrashCount() {
    const count = this.getCrashCount() + 1;
    this.set('crashCount', count);
    this.set('lastCrashTime', Date.now());
    return count;
  }

  resetCrashCount() {
    this.set('crashCount', 0);
    this.set('lastCrashTime', null);
  }

  getLastCrashTime() {
    return this.get('lastCrashTime');
  }

  /**
   * Safe mode management
   */
  isSafeMode() {
    return this.get('safeMode', false);
  }

  enableSafeMode() {
    this.set('safeMode', true);
  }

  disableSafeMode() {
    this.set('safeMode', false);
  }

  /**
   * Check if app should enter safe mode based on crash history
   */
  shouldEnterSafeMode(maxCrashes = 3) {
    const crashCount = this.getCrashCount();
    const lastCrashTime = this.getLastCrashTime();
    
    // Reset crash count if last crash was more than 24 hours ago
    if (lastCrashTime && Date.now() - lastCrashTime > 24 * 60 * 60 * 1000) {
      this.resetCrashCount();
      return false;
    }
    
    // If already in safe mode, stay in safe mode
    if (this.isSafeMode()) {
      return true;
    }
    
    // If crash count exceeds threshold, enter safe mode
    if (crashCount >= maxCrashes) {
      return true;
    }
    
    return false;
  }

  /**
   * Complete config reset - deletes config file
   */
  deleteConfigFile() {
    const fs = require('fs');
    const configPath = this.store.path;
    
    if (fs.existsSync(configPath)) {
      fs.unlinkSync(configPath);
      return true;
    }
    return false;
  }

  /**
   * Get config file path
   */
  getConfigPath() {
    return this.store.path;
  }
}

module.exports = AppConfig;
