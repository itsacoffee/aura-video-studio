/**
 * Auto-Save Service
 * Automatically saves project state to localStorage every 30 seconds
 * Preserves last 5 versions for recovery
 */

import { ProjectFile } from '../types/project';
import { loggingService } from './loggingService';

const AUTOSAVE_KEY_PREFIX = 'aura-autosave';
const AUTOSAVE_INTERVAL = 30000; // 30 seconds
const MAX_VERSIONS = 5;

export interface AutoSaveVersion {
  timestamp: string;
  projectState: ProjectFile;
  version: number;
}

export interface AutoSaveMetadata {
  currentVersion: number;
  lastSaveTime: string;
  totalVersions: number;
}

class AutoSaveService {
  private intervalId: number | null = null;
  private saveCallback: (() => ProjectFile | null) | null = null;
  private isEnabled = false;
  private lastSaveHash: string | null = null;

  /**
   * Start auto-save with a callback to get current project state
   */
  public start(callback: () => ProjectFile | null): void {
    if (this.intervalId) {
      loggingService.warn('Auto-save already running', 'autoSaveService', 'start');
      return;
    }

    this.saveCallback = callback;
    this.isEnabled = true;

    // Save immediately on start
    this.performSave();

    // Then save every 30 seconds
    this.intervalId = window.setInterval(() => {
      this.performSave();
    }, AUTOSAVE_INTERVAL);

    loggingService.info('Auto-save started', 'autoSaveService', 'start');
  }

  /**
   * Stop auto-save
   */
  public stop(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
      this.isEnabled = false;
      loggingService.info('Auto-save stopped', 'autoSaveService', 'stop');
    }
  }

  /**
   * Perform a manual save
   */
  public saveNow(): boolean {
    return this.performSave();
  }

  /**
   * Internal method to perform the save operation
   */
  private performSave(): boolean {
    if (!this.saveCallback || !this.isEnabled) {
      return false;
    }

    try {
      const projectState = this.saveCallback();
      
      if (!projectState) {
        loggingService.debug('No project state to save', 'autoSaveService', 'performSave');
        return false;
      }

      // Check if state has changed by comparing hash
      const currentHash = this.hashProjectState(projectState);
      if (currentHash === this.lastSaveHash) {
        loggingService.debug('Project state unchanged, skipping save', 'autoSaveService', 'performSave');
        return false;
      }

      // Get existing versions
      const versions = this.getVersions();
      
      // Create new version
      const newVersion: AutoSaveVersion = {
        timestamp: new Date().toISOString(),
        projectState,
        version: versions.length > 0 ? versions[0].version + 1 : 1,
      };

      // Add new version at the beginning
      versions.unshift(newVersion);

      // Keep only last MAX_VERSIONS
      const trimmedVersions = versions.slice(0, MAX_VERSIONS);

      // Save to localStorage
      this.saveVersions(trimmedVersions);
      this.updateMetadata(newVersion.version, newVersion.timestamp, trimmedVersions.length);

      this.lastSaveHash = currentHash;

      loggingService.info(
        `Project auto-saved (version ${newVersion.version})`,
        'autoSaveService',
        'performSave',
        { version: newVersion.version, totalVersions: trimmedVersions.length }
      );

      return true;
    } catch (error) {
      loggingService.error(
        'Failed to auto-save project',
        error as Error,
        'autoSaveService',
        'performSave'
      );
      return false;
    }
  }

  /**
   * Get all saved versions
   */
  public getVersions(): AutoSaveVersion[] {
    try {
      const key = `${AUTOSAVE_KEY_PREFIX}-versions`;
      const data = localStorage.getItem(key);
      if (!data) {
        return [];
      }
      return JSON.parse(data);
    } catch (error) {
      loggingService.error(
        'Failed to load auto-save versions',
        error as Error,
        'autoSaveService',
        'getVersions'
      );
      return [];
    }
  }

  /**
   * Get the most recent version
   */
  public getLatestVersion(): AutoSaveVersion | null {
    const versions = this.getVersions();
    return versions.length > 0 ? versions[0] : null;
  }

  /**
   * Get a specific version by version number
   */
  public getVersion(versionNumber: number): AutoSaveVersion | null {
    const versions = this.getVersions();
    return versions.find(v => v.version === versionNumber) || null;
  }

  /**
   * Get auto-save metadata
   */
  public getMetadata(): AutoSaveMetadata | null {
    try {
      const key = `${AUTOSAVE_KEY_PREFIX}-metadata`;
      const data = localStorage.getItem(key);
      if (!data) {
        return null;
      }
      return JSON.parse(data);
    } catch (error) {
      loggingService.error(
        'Failed to load auto-save metadata',
        error as Error,
        'autoSaveService',
        'getMetadata'
      );
      return null;
    }
  }

  /**
   * Clear all auto-save data
   */
  public clearAll(): void {
    try {
      localStorage.removeItem(`${AUTOSAVE_KEY_PREFIX}-versions`);
      localStorage.removeItem(`${AUTOSAVE_KEY_PREFIX}-metadata`);
      this.lastSaveHash = null;
      loggingService.info('Auto-save data cleared', 'autoSaveService', 'clearAll');
    } catch (error) {
      loggingService.error(
        'Failed to clear auto-save data',
        error as Error,
        'autoSaveService',
        'clearAll'
      );
    }
  }

  /**
   * Check if there are unsaved changes that can be recovered
   */
  public hasRecoverableData(): boolean {
    const metadata = this.getMetadata();
    return metadata !== null && metadata.totalVersions > 0;
  }

  /**
   * Save versions to localStorage
   */
  private saveVersions(versions: AutoSaveVersion[]): void {
    const key = `${AUTOSAVE_KEY_PREFIX}-versions`;
    localStorage.setItem(key, JSON.stringify(versions));
  }

  /**
   * Update metadata
   */
  private updateMetadata(currentVersion: number, lastSaveTime: string, totalVersions: number): void {
    const key = `${AUTOSAVE_KEY_PREFIX}-metadata`;
    const metadata: AutoSaveMetadata = {
      currentVersion,
      lastSaveTime,
      totalVersions,
    };
    localStorage.setItem(key, JSON.stringify(metadata));
  }

  /**
   * Create a hash of the project state for change detection
   */
  private hashProjectState(project: ProjectFile): string {
    // Simple hash based on key properties
    const keyData = {
      clipCount: project.clips.length,
      lastModified: project.metadata.lastModifiedAt,
      trackCount: project.tracks.length,
      mediaCount: project.mediaLibrary.length,
    };
    return JSON.stringify(keyData);
  }

  /**
   * Check if auto-save is currently enabled
   */
  public isRunning(): boolean {
    return this.isEnabled && this.intervalId !== null;
  }
}

// Export singleton instance
export const autoSaveService = new AutoSaveService();
