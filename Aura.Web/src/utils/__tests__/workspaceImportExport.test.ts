import { describe, it, expect } from 'vitest';
import type { WorkspaceLayout } from '../../services/workspaceLayoutService';
import type { WorkspaceExportFormat, WorkspaceBundle } from '../../types/workspace.types';
import {
  workspaceToExportFormat,
  exportFormatToWorkspace,
  validateWorkspaceFormat,
  validateWorkspaceBundle,
  generateUniqueWorkspaceName,
  sanitizeFilename,
  exportWorkspaceToJSON,
} from '../workspaceImportExport';

describe('workspaceImportExport', () => {
  const mockWorkspace: WorkspaceLayout = {
    id: 'test-workspace',
    name: 'Test Workspace',
    description: 'A test workspace',
    panelSizes: {
      propertiesWidth: 320,
      mediaLibraryWidth: 280,
      effectsLibraryWidth: 280,
      historyWidth: 320,
      previewHeight: 70,
    },
    visiblePanels: {
      properties: true,
      mediaLibrary: true,
      effects: false,
      history: false,
    },
  };

  describe('workspaceToExportFormat', () => {
    it('should convert workspace to export format', () => {
      const result = workspaceToExportFormat(mockWorkspace);

      expect(result.name).toBe('Test Workspace');
      expect(result.description).toBe('A test workspace');
      expect(result.version).toBe('1.0');
      expect(result.layout.properties.visible).toBe(true);
      expect(result.layout.properties.width).toBe('320px');
      expect(result.layout.mediaLibrary.visible).toBe(true);
      expect(result.layout.mediaLibrary.width).toBe('280px');
    });

    it('should include author when provided', () => {
      const result = workspaceToExportFormat(mockWorkspace, 'Test Author');

      expect(result.author).toBe('Test Author');
    });

    it('should have ISO date strings for created and modified', () => {
      const result = workspaceToExportFormat(mockWorkspace);

      expect(result.created).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
      expect(result.modified).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
    });
  });

  describe('exportFormatToWorkspace', () => {
    it('should convert export format to workspace', () => {
      const exportFormat = workspaceToExportFormat(mockWorkspace);
      const result = exportFormatToWorkspace(exportFormat, 'custom-123');

      expect(result.id).toBe('custom-123');
      expect(result.name).toBe('Test Workspace');
      expect(result.description).toBe('A test workspace');
      expect(result.panelSizes.propertiesWidth).toBe(320);
      expect(result.panelSizes.mediaLibraryWidth).toBe(280);
      expect(result.visiblePanels.properties).toBe(true);
      expect(result.visiblePanels.mediaLibrary).toBe(true);
    });

    it('should generate ID if not provided', () => {
      const exportFormat = workspaceToExportFormat(mockWorkspace);
      const result = exportFormatToWorkspace(exportFormat);

      expect(result.id).toMatch(/^custom-\d+/);
    });

    it('should handle missing or invalid sizes', () => {
      const exportFormat: WorkspaceExportFormat = {
        version: '1.0',
        name: 'Test',
        description: 'Test',
        created: new Date().toISOString(),
        modified: new Date().toISOString(),
        layout: {
          properties: { visible: true, collapsed: false },
          mediaLibrary: { visible: true, width: 'invalid', collapsed: false },
          effectsLibrary: { visible: false, collapsed: true },
          preview: { visible: true, collapsed: false },
          timeline: { visible: true, collapsed: false },
          history: { visible: false, collapsed: true },
        },
      };

      const result = exportFormatToWorkspace(exportFormat);

      expect(result.panelSizes.mediaLibraryWidth).toBe(280);
    });
  });

  describe('validateWorkspaceFormat', () => {
    it('should validate valid workspace format', () => {
      const exportFormat = workspaceToExportFormat(mockWorkspace);
      const result = validateWorkspaceFormat(exportFormat);

      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('should detect missing required fields', () => {
      const invalid = { version: '1.0' };
      const result = validateWorkspaceFormat(invalid);

      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
      const hasNameError = result.errors.some((e) => e.includes('name'));
      expect(hasNameError).toBe(true);
    });

    it('should detect invalid data types', () => {
      const result = validateWorkspaceFormat('not an object');

      expect(result.valid).toBe(false);
      expect(result.errors[0]).toContain('not a valid JSON object');
    });

    it('should detect missing panel configurations', () => {
      const invalid = {
        version: '1.0',
        name: 'Test',
        description: 'Test',
        layout: {
          properties: { visible: true, collapsed: false },
        },
      };
      const result = validateWorkspaceFormat(invalid);

      expect(result.valid).toBe(false);
      expect(result.errors.some((e) => e.includes('mediaLibrary'))).toBe(true);
    });

    it('should warn about version mismatch', () => {
      const exportFormat = workspaceToExportFormat(mockWorkspace);
      exportFormat.version = '2.0';
      const result = validateWorkspaceFormat(exportFormat);

      expect(result.warnings.length).toBeGreaterThan(0);
      const firstWarning = result.warnings[0];
      expect(firstWarning).toContain('version');
    });
  });

  describe('validateWorkspaceBundle', () => {
    it('should validate valid bundle', () => {
      const bundle: WorkspaceBundle = {
        version: '1.0',
        created: new Date().toISOString(),
        workspaces: [workspaceToExportFormat(mockWorkspace)],
      };
      const result = validateWorkspaceBundle(bundle);

      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('should detect missing workspaces array', () => {
      const invalid = { version: '1.0' };
      const result = validateWorkspaceBundle(invalid);

      expect(result.valid).toBe(false);
      expect(result.errors.some((e) => e.includes('workspaces'))).toBe(true);
    });

    it('should detect empty workspaces array', () => {
      const bundle = {
        version: '1.0',
        created: new Date().toISOString(),
        workspaces: [],
      };
      const result = validateWorkspaceBundle(bundle);

      expect(result.valid).toBe(false);
      expect(result.errors.some((e) => e.includes('no workspaces'))).toBe(true);
    });

    it('should validate each workspace in bundle', () => {
      const bundle: WorkspaceBundle = {
        version: '1.0',
        created: new Date().toISOString(),
        workspaces: [
          workspaceToExportFormat(mockWorkspace),
          { version: '1.0', name: 'Invalid' } as WorkspaceExportFormat,
        ],
      };
      const result = validateWorkspaceBundle(bundle);

      expect(result.valid).toBe(false);
      expect(result.errors.some((e) => e.includes('Workspace 2'))).toBe(true);
    });
  });

  describe('generateUniqueWorkspaceName', () => {
    it('should return original name if unique', () => {
      const result = generateUniqueWorkspaceName('My Workspace', ['Other Workspace']);

      expect(result).toBe('My Workspace');
    });

    it('should append (1) if name exists', () => {
      const result = generateUniqueWorkspaceName('My Workspace', ['My Workspace']);

      expect(result).toBe('My Workspace (1)');
    });

    it('should increment counter for multiple conflicts', () => {
      const result = generateUniqueWorkspaceName('My Workspace', [
        'My Workspace',
        'My Workspace (1)',
        'My Workspace (2)',
      ]);

      expect(result).toBe('My Workspace (3)');
    });
  });

  describe('sanitizeFilename', () => {
    it('should convert to lowercase', () => {
      const result = sanitizeFilename('MyWorkspace');

      expect(result).toBe('myworkspace');
    });

    it('should replace invalid characters with underscores', () => {
      const result = sanitizeFilename('My Workspace!@#$%');

      expect(result).toBe('my_workspace_');
    });

    it('should collapse multiple underscores', () => {
      const result = sanitizeFilename('my   workspace');

      expect(result).toBe('my_workspace');
    });

    it('should preserve valid characters', () => {
      const result = sanitizeFilename('my-workspace_v1.0');

      expect(result).toBe('my-workspace_v1.0');
    });
  });

  describe('exportWorkspaceToJSON', () => {
    it('should export workspace as formatted JSON string', () => {
      const result = exportWorkspaceToJSON(mockWorkspace);

      expect(result).toContain('"name": "Test Workspace"');
      expect(result).toContain('"version": "1.0"');
      expect(() => JSON.parse(result)).not.toThrow();
    });

    it('should include author when provided', () => {
      const result = exportWorkspaceToJSON(mockWorkspace, 'Test Author');

      expect(result).toContain('"author": "Test Author"');
    });
  });
});
