/**
 * Tests for Auto-Save Service
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { autoSaveService } from '../autoSaveService';
import { createEmptyProject, ProjectFile } from '../../types/project';

describe('AutoSaveService', () => {
  let mockProject: ProjectFile;

  beforeEach(() => {
    // Stop any running auto-save (don't clear data here)
    autoSaveService.stop();
    
    // Clear localStorage before each test
    localStorage.clear();
    
    // Create a mock project
    mockProject = createEmptyProject('Test Project');
  });

  describe('Basic Operations', () => {
    it('should start auto-save service', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback);
      
      expect(autoSaveService.isRunning()).toBe(true);
      expect(callback).toHaveBeenCalled();
      
      // Should have saved on start
      const versions = autoSaveService.getVersions();
      expect(versions).toHaveLength(1);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should stop auto-save service', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback);
      autoSaveService.stop();
      
      expect(autoSaveService.isRunning()).toBe(false);
    });

    it('should perform manual save with modified data', () => {
      let callCount = 0;
      const callback = vi.fn(() => {
        const project = createEmptyProject('Test Project');
        // Modify to create different hash
        project.metadata.lastModifiedAt = new Date(Date.now() + callCount++ * 1000).toISOString();
        return project;
      });
      
      autoSaveService.start(callback); // First save on start
      
      const result = autoSaveService.saveNow(); // Second save with modified data
      expect(result).toBe(true);
      
      // Clean up
      autoSaveService.stop();
    });
  });

  describe('Version Management', () => {
    it('should save and retrieve versions', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback); // Saves on start
      
      const versions = autoSaveService.getVersions();
      expect(versions).toHaveLength(1);
      expect(versions[0].version).toBe(1);
      expect(versions[0].projectState.metadata.name).toEqual(mockProject.metadata.name);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should maintain maximum of 5 versions', () => {
      const callback = vi.fn(() => {
        // Return a modified project each time
        const project = createEmptyProject('Test Project');
        project.metadata.lastModifiedAt = new Date().toISOString();
        return project;
      });
      
      autoSaveService.start(callback);
      
      // Save 10 times
      for (let i = 0; i < 10; i++) {
        autoSaveService.saveNow();
      }
      
      const versions = autoSaveService.getVersions();
      expect(versions.length).toBeLessThanOrEqual(5);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should get latest version', () => {
      let callCount = 0;
      const callback = vi.fn(() => {
        const project = createEmptyProject('Test Project');
        project.metadata.lastModifiedAt = new Date(Date.now() + callCount++ * 1000).toISOString();
        return project;
      });
      
      autoSaveService.start(callback); // version 1
      autoSaveService.saveNow(); // version 2
      
      const latest = autoSaveService.getLatestVersion();
      expect(latest).not.toBeNull();
      expect(latest?.version).toBe(2);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should get specific version by number', () => {
      let callCount = 0;
      const callback = vi.fn(() => {
        const project = createEmptyProject('Test Project');
        project.metadata.lastModifiedAt = new Date(Date.now() + callCount++ * 1000).toISOString();
        return project;
      });
      
      autoSaveService.start(callback); // version 1
      autoSaveService.saveNow(); // version 2
      autoSaveService.saveNow(); // version 3
      
      const version2 = autoSaveService.getVersion(2);
      expect(version2).not.toBeNull();
      expect(version2?.version).toBe(2);
      
      // Clean up
      autoSaveService.stop();
    });
  });

  describe('Metadata', () => {
    it('should store and retrieve metadata', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback); // Saves on start
      
      const metadata = autoSaveService.getMetadata();
      expect(metadata).not.toBeNull();
      expect(metadata?.currentVersion).toBe(1);
      expect(metadata?.totalVersions).toBe(1);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should detect recoverable data', () => {
      const callback = vi.fn(() => mockProject);
      
      expect(autoSaveService.hasRecoverableData()).toBe(false);
      
      autoSaveService.start(callback); // Saves on start
      
      expect(autoSaveService.hasRecoverableData()).toBe(true);
      
      // Clean up
      autoSaveService.stop();
    });
  });

  describe('Change Detection', () => {
    it('should not save if project state has not changed', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback); // First save
      
      // Second save with same data should skip
      const result2 = autoSaveService.saveNow();
      expect(result2).toBe(false);
      
      // Should only have one version
      const versions = autoSaveService.getVersions();
      expect(versions).toHaveLength(1);
      
      // Clean up
      autoSaveService.stop();
    });

    it('should save if project state has changed', () => {
      let callCount = 0;
      const callback = vi.fn(() => {
        const project = createEmptyProject('Test Project');
        // Modify the project on each call
        project.clips.push({
          id: `clip-${callCount++}`,
          trackId: 'video1',
          startTime: 0,
          duration: 5,
          label: 'New Clip',
          type: 'video',
        });
        return project;
      });
      
      autoSaveService.start(callback); // First save
      
      // Second save with different data
      const result2 = autoSaveService.saveNow();
      expect(result2).toBe(true);
      
      // Should have two versions
      const versions = autoSaveService.getVersions();
      expect(versions).toHaveLength(2);
      
      // Clean up
      autoSaveService.stop();
    });
  });

  describe('Clear Operations', () => {
    it('should clear all saved data', () => {
      const callback = vi.fn(() => mockProject);
      autoSaveService.start(callback); // Saves on start
      expect(autoSaveService.hasRecoverableData()).toBe(true);
      
      // Clean up
      autoSaveService.stop();
      
      autoSaveService.clearAll();
      expect(autoSaveService.hasRecoverableData()).toBe(false);
      
      const versions = autoSaveService.getVersions();
      expect(versions).toHaveLength(0);
    });
  });
});
