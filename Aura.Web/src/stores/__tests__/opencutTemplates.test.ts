/**
 * OpenCut Templates Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useTemplatesStore, BUILTIN_TEMPLATES } from '../opencutTemplates';
import type { TemplateData } from '../opencutTemplates';

describe('OpenCutTemplatesStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useTemplatesStore.setState({
      builtinTemplates: BUILTIN_TEMPLATES,
      userTemplates: [],
      selectedTemplateId: null,
    });
  });

  describe('Built-in Templates', () => {
    it('should have built-in templates', () => {
      const { builtinTemplates } = useTemplatesStore.getState();
      expect(builtinTemplates.length).toBeGreaterThan(0);
      expect(builtinTemplates.length).toBe(BUILTIN_TEMPLATES.length);
    });

    it('should have social vertical template', () => {
      const { getTemplate } = useTemplatesStore.getState();
      const template = getTemplate('social-vertical');
      expect(template).toBeDefined();
      expect(template?.name).toBe('Vertical Social Video');
      expect(template?.aspectRatio).toBe('9:16');
    });

    it('should have youtube landscape template', () => {
      const { getTemplate } = useTemplatesStore.getState();
      const template = getTemplate('youtube-landscape');
      expect(template).toBeDefined();
      expect(template?.name).toBe('Landscape Video');
      expect(template?.aspectRatio).toBe('16:9');
    });

    it('should have square promo template', () => {
      const { getTemplate } = useTemplatesStore.getState();
      const template = getTemplate('square-promo');
      expect(template).toBeDefined();
      expect(template?.aspectRatio).toBe('1:1');
    });

    it('should have markers in youtube landscape template', () => {
      const { getTemplate } = useTemplatesStore.getState();
      const template = getTemplate('youtube-landscape');
      expect(template?.data.markers.length).toBeGreaterThan(0);
    });
  });

  describe('Create Templates', () => {
    it('should create a user template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [
          {
            id: 't1',
            type: 'video',
            name: 'Test',
            order: 0,
            height: 56,
            muted: false,
            solo: false,
            locked: false,
            visible: true,
          },
        ],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const id = createTemplate('My Template', 'A test template', data, '16:9');

      const state = useTemplatesStore.getState();
      expect(state.userTemplates.length).toBe(1);
      expect(state.userTemplates[0].id).toBe(id);
      expect(state.userTemplates[0].name).toBe('My Template');
      expect(state.userTemplates[0].category).toBe('Custom');
    });

    it('should create template with custom category and tags', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      createTemplate('Tagged Template', 'Description', data, '16:9', 'Marketing', ['promo', 'ads']);

      const state = useTemplatesStore.getState();
      expect(state.userTemplates[0].category).toBe('Marketing');
      expect(state.userTemplates[0].tags).toEqual(['promo', 'ads']);
    });

    it('should set timestamps on creation', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const beforeCreate = new Date().toISOString();

      createTemplate(
        'Test',
        'Desc',
        { tracks: [], clips: [], effects: [], transitions: [], markers: [] },
        '16:9'
      );

      const state = useTemplatesStore.getState();
      const afterCreate = new Date().toISOString();

      expect(state.userTemplates[0].createdAt >= beforeCreate).toBe(true);
      expect(state.userTemplates[0].createdAt <= afterCreate).toBe(true);
    });
  });

  describe('Update Templates', () => {
    it('should update a user template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const id = createTemplate('Original', 'Original description', data, '16:9');
      const originalUpdatedAt = useTemplatesStore.getState().userTemplates[0].updatedAt;

      // Small delay to ensure different timestamp
      useTemplatesStore.getState().updateTemplate(id, { name: 'Updated', description: 'New desc' });

      const state = useTemplatesStore.getState();
      expect(state.userTemplates[0].name).toBe('Updated');
      expect(state.userTemplates[0].description).toBe('New desc');
      expect(state.userTemplates[0].updatedAt >= originalUpdatedAt).toBe(true);
    });

    it('should not update built-in templates', () => {
      const { updateTemplate, getTemplate } = useTemplatesStore.getState();
      const originalName = getTemplate('social-vertical')?.name;

      updateTemplate('social-vertical', { name: 'Hacked!' });

      // Built-in templates should remain unchanged
      const template = useTemplatesStore.getState().getTemplate('social-vertical');
      expect(template?.name).toBe(originalName);
    });
  });

  describe('Delete Templates', () => {
    it('should delete a user template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const id = createTemplate('To Delete', 'Will be deleted', data, '16:9');
      expect(useTemplatesStore.getState().userTemplates.length).toBe(1);

      useTemplatesStore.getState().deleteTemplate(id);
      expect(useTemplatesStore.getState().userTemplates.length).toBe(0);
    });

    it('should clear selection when deleting selected template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const id = createTemplate('Selected', 'Description', data, '16:9');
      useTemplatesStore.getState().selectTemplate(id);
      expect(useTemplatesStore.getState().selectedTemplateId).toBe(id);

      useTemplatesStore.getState().deleteTemplate(id);
      expect(useTemplatesStore.getState().selectedTemplateId).toBeNull();
    });

    it('should not affect selection when deleting non-selected template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const id1 = createTemplate('Template 1', 'Desc', data, '16:9');
      const id2 = createTemplate('Template 2', 'Desc', data, '16:9');

      useTemplatesStore.getState().selectTemplate(id1);
      useTemplatesStore.getState().deleteTemplate(id2);

      expect(useTemplatesStore.getState().selectedTemplateId).toBe(id1);
    });
  });

  describe('Duplicate Templates', () => {
    it('should duplicate a user template', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [
          {
            id: 't1',
            type: 'video',
            name: 'Original Track',
            order: 0,
            height: 56,
            muted: false,
            solo: false,
            locked: false,
            visible: true,
          },
        ],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const originalId = createTemplate('Original', 'Original desc', data, '16:9', 'Custom', [
        'tag1',
      ]);
      const copyId = useTemplatesStore.getState().duplicateTemplate(originalId);

      const state = useTemplatesStore.getState();
      expect(state.userTemplates.length).toBe(2);
      expect(copyId).not.toBe(originalId);

      const copy = state.getTemplate(copyId);
      expect(copy?.name).toBe('Original (Copy)');
      expect(copy?.data.tracks.length).toBe(1);
      expect(copy?.data.tracks[0].name).toBe('Original Track');
    });

    it('should duplicate a built-in template', () => {
      const { duplicateTemplate } = useTemplatesStore.getState();
      const copyId = duplicateTemplate('social-vertical');

      const state = useTemplatesStore.getState();
      expect(state.userTemplates.length).toBe(1);

      const copy = state.getTemplate(copyId);
      expect(copy?.name).toBe('Vertical Social Video (Copy)');
      expect(copy?.isBuiltin).toBe(false);
      expect(copy?.category).toBe('Custom');
    });

    it('should return empty string for non-existent template', () => {
      const { duplicateTemplate } = useTemplatesStore.getState();
      const result = duplicateTemplate('non-existent');
      expect(result).toBe('');
    });

    it('should create deep copy of data', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [
          {
            id: 't1',
            type: 'video',
            name: 'Track',
            order: 0,
            height: 56,
            muted: false,
            solo: false,
            locked: false,
            visible: true,
          },
        ],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      const originalId = createTemplate('Original', 'Desc', data, '16:9');
      const copyId = useTemplatesStore.getState().duplicateTemplate(originalId);

      // Modify the copy's data
      const copy = useTemplatesStore.getState().getTemplate(copyId);
      const modifiedData = { ...copy!.data };
      modifiedData.tracks[0].name = 'Modified Track';
      useTemplatesStore.getState().updateTemplate(copyId, { data: modifiedData });

      // Original should be unchanged
      const original = useTemplatesStore.getState().getTemplate(originalId);
      expect(original?.data.tracks[0].name).toBe('Track');
    });
  });

  describe('Apply Templates', () => {
    it('should return template data for applying', () => {
      const { applyTemplate } = useTemplatesStore.getState();
      const data = applyTemplate('social-vertical');

      expect(data).toBeDefined();
      expect(data?.tracks.length).toBeGreaterThan(0);
    });

    it('should return deep copy of template data', () => {
      const { applyTemplate, getTemplate } = useTemplatesStore.getState();
      const data = applyTemplate('social-vertical');
      const originalData = getTemplate('social-vertical')?.data;

      // Modify the returned data
      data!.tracks[0].name = 'Modified';

      // Original should be unchanged
      const checkOriginal = getTemplate('social-vertical')?.data;
      expect(checkOriginal?.tracks[0].name).toBe(originalData?.tracks[0].name);
    });

    it('should return null for non-existent template', () => {
      const { applyTemplate } = useTemplatesStore.getState();
      const data = applyTemplate('non-existent');
      expect(data).toBeNull();
    });
  });

  describe('Selection', () => {
    it('should select a template', () => {
      const { selectTemplate } = useTemplatesStore.getState();
      selectTemplate('social-vertical');

      expect(useTemplatesStore.getState().selectedTemplateId).toBe('social-vertical');
    });

    it('should clear selection', () => {
      const { selectTemplate } = useTemplatesStore.getState();
      selectTemplate('social-vertical');
      useTemplatesStore.getState().selectTemplate(null);

      expect(useTemplatesStore.getState().selectedTemplateId).toBeNull();
    });
  });

  describe('Query Operations', () => {
    it('should get templates by category', () => {
      const { getTemplatesByCategory } = useTemplatesStore.getState();
      const socialTemplates = getTemplatesByCategory('Social Media');

      expect(socialTemplates.length).toBeGreaterThan(0);
      expect(socialTemplates.every((t) => t.category === 'Social Media')).toBe(true);
    });

    it('should search templates by name', () => {
      const { searchTemplates } = useTemplatesStore.getState();
      const results = searchTemplates('Vertical');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((t) => t.name.includes('Vertical'))).toBe(true);
    });

    it('should search templates by description', () => {
      const { searchTemplates } = useTemplatesStore.getState();
      const results = searchTemplates('mobile-first');

      expect(results.length).toBeGreaterThan(0);
    });

    it('should search templates by tags', () => {
      const { searchTemplates } = useTemplatesStore.getState();
      const results = searchTemplates('short-form');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((t) => t.tags.includes('short-form'))).toBe(true);
    });

    it('should be case-insensitive when searching', () => {
      const { searchTemplates } = useTemplatesStore.getState();
      const results1 = searchTemplates('VERTICAL');
      const results2 = searchTemplates('vertical');

      expect(results1.length).toBe(results2.length);
    });

    it('should get all categories', () => {
      const { getCategories } = useTemplatesStore.getState();
      const categories = getCategories();

      expect(categories).toContain('Social Media');
      expect(categories.length).toBeGreaterThan(0);
    });

    it('should get all templates', () => {
      const { createTemplate } = useTemplatesStore.getState();
      const data: TemplateData = {
        tracks: [],
        clips: [],
        effects: [],
        transitions: [],
        markers: [],
      };

      createTemplate('User Template', 'Desc', data, '16:9');

      const allTemplates = useTemplatesStore.getState().getAllTemplates();
      expect(allTemplates.length).toBe(BUILTIN_TEMPLATES.length + 1);
    });
  });

  describe('Import/Export', () => {
    it('should export a template as JSON', () => {
      const { exportTemplate, getTemplate } = useTemplatesStore.getState();
      const json = exportTemplate('social-vertical');

      expect(json).toBeTruthy();
      const parsed = JSON.parse(json);
      expect(parsed.id).toBe('social-vertical');
      expect(parsed.name).toBe(getTemplate('social-vertical')?.name);
    });

    it('should return empty string for non-existent template', () => {
      const { exportTemplate } = useTemplatesStore.getState();
      const json = exportTemplate('non-existent');
      expect(json).toBe('');
    });

    it('should import a template from JSON', () => {
      const { exportTemplate } = useTemplatesStore.getState();
      const json = exportTemplate('social-vertical');

      const importedId = useTemplatesStore.getState().importTemplate(json);

      expect(importedId).toBeTruthy();
      const state = useTemplatesStore.getState();
      expect(state.userTemplates.length).toBe(1);
      expect(state.userTemplates[0].category).toBe('Imported');
      expect(state.userTemplates[0].isBuiltin).toBe(false);
    });

    it('should generate new ID on import', () => {
      const { exportTemplate } = useTemplatesStore.getState();
      const json = exportTemplate('social-vertical');

      const importedId = useTemplatesStore.getState().importTemplate(json);

      expect(importedId).not.toBe('social-vertical');
    });

    it('should return null for invalid JSON', () => {
      const result = useTemplatesStore.getState().importTemplate('not valid json');
      expect(result).toBeNull();
    });

    it('should return null for JSON missing required fields', () => {
      // Missing name field
      const invalidJson = JSON.stringify({ description: 'test', aspectRatio: '16:9' });
      const result = useTemplatesStore.getState().importTemplate(invalidJson);
      expect(result).toBeNull();
    });

    it('should return null for JSON with invalid data structure', () => {
      // Missing tracks array in data
      const invalidJson = JSON.stringify({
        name: 'Test',
        description: 'Test',
        aspectRatio: '16:9',
        data: { clips: [], effects: [], transitions: [], markers: [] },
      });
      const result = useTemplatesStore.getState().importTemplate(invalidJson);
      expect(result).toBeNull();
    });

    it('should update timestamps on import', () => {
      const { exportTemplate } = useTemplatesStore.getState();
      const json = exportTemplate('social-vertical');
      const beforeImport = new Date().toISOString();

      useTemplatesStore.getState().importTemplate(json);

      const state = useTemplatesStore.getState();
      const afterImport = new Date().toISOString();

      expect(state.userTemplates[0].createdAt >= beforeImport).toBe(true);
      expect(state.userTemplates[0].createdAt <= afterImport).toBe(true);
    });
  });
});
