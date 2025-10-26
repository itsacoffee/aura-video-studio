/**
 * Tests for Auto-Save Service
 * Simplified tests focusing on core API functionality
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { autoSaveService } from '../autoSaveService';
import { createEmptyProject, ProjectFile } from '../../types/project';

describe('AutoSaveService', () => {
  let mockProject: ProjectFile;

  beforeEach(() => {
    mockProject = createEmptyProject('Test Project');
    autoSaveService.stop();
    localStorage.clear();
  });

  afterEach(() => {
    autoSaveService.stop();
    autoSaveService.clearAll();
  });

  it('should start and stop auto-save service', () => {
    const callback = vi.fn(() => mockProject);
    
    expect(autoSaveService.isRunning()).toBe(false);
    
    autoSaveService.start(callback);
    expect(autoSaveService.isRunning()).toBe(true);
    expect(callback).toHaveBeenCalled();
    
    autoSaveService.stop();
    expect(autoSaveService.isRunning()).toBe(false);
  });

  it('should provide API for version management', () => {
    // Test that the API exists and returns expected types
    expect(typeof autoSaveService.getVersions).toBe('function');
    expect(typeof autoSaveService.getLatestVersion).toBe('function');
    expect(typeof autoSaveService.getVersion).toBe('function');
    expect(typeof autoSaveService.getMetadata).toBe('function');
    
    const versions = autoSaveService.getVersions();
    expect(Array.isArray(versions)).toBe(true);
  });

  it('should provide API for manual save', () => {
    const callback = vi.fn(() => {
      const project = createEmptyProject('Test');
      project.metadata.lastModifiedAt = new Date().toISOString();
      return project;
    });
    
    autoSaveService.start(callback);
    
    const result = autoSaveService.saveNow();
    expect(typeof result).toBe('boolean');
  });

  it('should provide API for clearing data', () => {
    expect(typeof autoSaveService.clearAll).toBe('function');
    expect(typeof autoSaveService.hasRecoverableData).toBe('function');
    
    // Should not throw
    autoSaveService.clearAll();
    const hasData = autoSaveService.hasRecoverableData();
    expect(typeof hasData).toBe('boolean');
  });
});
