import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  getVideoTemplates,
  getSampleProjects,
  getExamplePrompts,
  getTutorialGuides,
  getTemplatesByCategory,
  getTemplatesByDifficulty,
  getBeginnerTemplates,
  searchTemplates,
  createProjectFromTemplate,
} from '../templatesAndSamplesService';

describe('templatesAndSamplesService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });

  describe('getVideoTemplates', () => {
    it('should return an array of video templates', () => {
      const templates = getVideoTemplates();

      expect(templates).toBeInstanceOf(Array);
      expect(templates.length).toBeGreaterThan(0);
      expect(templates[0]).toHaveProperty('id');
      expect(templates[0]).toHaveProperty('name');
      expect(templates[0]).toHaveProperty('description');
      expect(templates[0]).toHaveProperty('category');
    });

    it('should have templates with all required properties', () => {
      const templates = getVideoTemplates();

      templates.forEach((template) => {
        expect(template.id).toBeTruthy();
        expect(template.name).toBeTruthy();
        expect(template.description).toBeTruthy();
        expect(template.category).toBeTruthy();
        expect(template.duration).toBeGreaterThan(0);
        expect(template.difficulty).toMatch(/beginner|intermediate|advanced/);
        expect(template.promptExample).toBeTruthy();
        expect(template.tags).toBeInstanceOf(Array);
        expect(template.estimatedTime).toBeTruthy();
      });
    });
  });

  describe('getSampleProjects', () => {
    it('should return an array of sample projects', () => {
      const samples = getSampleProjects();

      expect(samples).toBeInstanceOf(Array);
      expect(samples.length).toBeGreaterThan(0);
      expect(samples[0]).toHaveProperty('id');
      expect(samples[0]).toHaveProperty('name');
      expect(samples[0]).toHaveProperty('template');
      expect(samples[0]).toHaveProperty('prompt');
      expect(samples[0]).toHaveProperty('learningPoints');
    });

    it('should have valid template references', () => {
      const samples = getSampleProjects();
      const templates = getVideoTemplates();

      samples.forEach((sample) => {
        expect(sample.template).toBeTruthy();
        const templateExists = templates.some((t) => t.id === sample.template.id);
        expect(templateExists).toBe(true);
      });
    });
  });

  describe('getExamplePrompts', () => {
    it('should return an array of example prompts', () => {
      const prompts = getExamplePrompts();

      expect(prompts).toBeInstanceOf(Array);
      expect(prompts.length).toBeGreaterThan(0);
      expect(prompts[0]).toHaveProperty('id');
      expect(prompts[0]).toHaveProperty('title');
      expect(prompts[0]).toHaveProperty('prompt');
      expect(prompts[0]).toHaveProperty('category');
      expect(prompts[0]).toHaveProperty('tips');
    });
  });

  describe('getTutorialGuides', () => {
    it('should return an array of tutorial guides', () => {
      const guides = getTutorialGuides();

      expect(guides).toBeInstanceOf(Array);
      expect(guides.length).toBeGreaterThan(0);
      expect(guides[0]).toHaveProperty('id');
      expect(guides[0]).toHaveProperty('title');
      expect(guides[0]).toHaveProperty('steps');
      expect(guides[0].steps).toBeInstanceOf(Array);
    });

    it('should have steps with required properties', () => {
      const guides = getTutorialGuides();

      guides.forEach((guide) => {
        guide.steps.forEach((step) => {
          expect(step.title).toBeTruthy();
          expect(step.description).toBeTruthy();
        });
      });
    });
  });

  describe('getTemplatesByCategory', () => {
    it('should filter templates by category', () => {
      const tutorialTemplates = getTemplatesByCategory('tutorial');

      expect(tutorialTemplates.length).toBeGreaterThan(0);
      tutorialTemplates.forEach((template) => {
        expect(template.category).toBe('tutorial');
      });
    });

    it('should return empty array for non-existent category', () => {
      type TemplateCategory =
        | 'youtube'
        | 'social-media'
        | 'educational'
        | 'marketing'
        | 'tutorial'
        | 'storytelling'
        | 'news'
        | 'entertainment';
      const templates = getTemplatesByCategory('nonexistent' as TemplateCategory);

      expect(templates).toEqual([]);
    });
  });

  describe('getTemplatesByDifficulty', () => {
    it('should filter templates by difficulty', () => {
      const beginnerTemplates = getTemplatesByDifficulty('beginner');

      expect(beginnerTemplates.length).toBeGreaterThan(0);
      beginnerTemplates.forEach((template) => {
        expect(template.difficulty).toBe('beginner');
      });
    });

    it('should return intermediate templates', () => {
      const intermediateTemplates = getTemplatesByDifficulty('intermediate');

      intermediateTemplates.forEach((template) => {
        expect(template.difficulty).toBe('intermediate');
      });
    });
  });

  describe('getBeginnerTemplates', () => {
    it('should return only beginner templates', () => {
      const beginnerTemplates = getBeginnerTemplates();

      expect(beginnerTemplates.length).toBeGreaterThan(0);
      beginnerTemplates.forEach((template) => {
        expect(template.difficulty).toBe('beginner');
      });
    });

    it('should return at most 3 templates', () => {
      const beginnerTemplates = getBeginnerTemplates();

      expect(beginnerTemplates.length).toBeLessThanOrEqual(3);
    });
  });

  describe('searchTemplates', () => {
    it('should find templates by name', () => {
      const results = searchTemplates('tutorial');

      expect(results.length).toBeGreaterThan(0);
      results.forEach((template) => {
        const matchesName = template.name.toLowerCase().includes('tutorial');
        const matchesDescription = template.description.toLowerCase().includes('tutorial');
        const matchesTags = template.tags.some((tag) => tag.toLowerCase().includes('tutorial'));

        expect(matchesName || matchesDescription || matchesTags).toBe(true);
      });
    });

    it('should be case-insensitive', () => {
      const lowerResults = searchTemplates('youtube');
      const upperResults = searchTemplates('YOUTUBE');

      expect(lowerResults.length).toBe(upperResults.length);
    });

    it('should return empty array for no matches', () => {
      const results = searchTemplates('xyznonexistent123');

      expect(results).toEqual([]);
    });
  });

  describe('createProjectFromTemplate', () => {
    it('should create a project successfully', async () => {
      const mockResponse = {
        success: true,
        projectId: 'test-project-id',
        message: 'Project created successfully',
      };

      global.fetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      } as Response);

      const result = await createProjectFromTemplate({
        templateId: 'youtube-tutorial',
        projectName: 'My Test Project',
      });

      expect(result.success).toBe(true);
      expect(result.projectId).toBe('test-project-id');
    });

    it('should handle API errors', async () => {
      global.fetch = vi.fn().mockResolvedValue({
        ok: false,
        statusText: 'Not Found',
      } as Response);

      const result = await createProjectFromTemplate({
        templateId: 'nonexistent',
      });

      expect(result.success).toBe(false);
      expect(result.message).toContain('Failed to create project');
    });

    it('should handle network errors', async () => {
      global.fetch = vi.fn().mockRejectedValue(new Error('Network error'));

      const result = await createProjectFromTemplate({
        templateId: 'youtube-tutorial',
      });

      expect(result.success).toBe(false);
      expect(result.message).toBe('Network error');
    });
  });

  describe('Template data quality', () => {
    it('should have unique template IDs', () => {
      const templates = getVideoTemplates();
      const ids = templates.map((t) => t.id);
      const uniqueIds = new Set(ids);

      expect(uniqueIds.size).toBe(ids.length);
    });

    it('should have valid categories', () => {
      const templates = getVideoTemplates();
      const validCategories = [
        'youtube',
        'social-media',
        'educational',
        'marketing',
        'tutorial',
        'storytelling',
        'news',
        'entertainment',
      ];

      templates.forEach((template) => {
        expect(validCategories).toContain(template.category);
      });
    });

    it('should have reasonable durations', () => {
      const templates = getVideoTemplates();

      templates.forEach((template) => {
        expect(template.duration).toBeGreaterThan(30); // At least 30 seconds
        expect(template.duration).toBeLessThan(600); // Less than 10 minutes
      });
    });

    it('should have non-empty tags', () => {
      const templates = getVideoTemplates();

      templates.forEach((template) => {
        expect(template.tags.length).toBeGreaterThan(0);
        template.tags.forEach((tag) => {
          expect(tag).toBeTruthy();
        });
      });
    });
  });
});
