import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  normalizeAspect,
  normalizeDensity,
  validateAndWarnEnums,
  normalizeEnumsForApi,
} from '../enumNormalizer';
import { loggingService } from '../../services/loggingService';

describe('EnumNormalizer', () => {
  let loggingWarnSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    loggingWarnSpy = vi.spyOn(loggingService, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    loggingWarnSpy.mockRestore();
  });

  describe('normalizeAspect', () => {
    it('should normalize legacy aspect ratios', () => {
      expect(normalizeAspect('16:9')).toBe('Widescreen16x9');
      expect(normalizeAspect('9:16')).toBe('Vertical9x16');
      expect(normalizeAspect('1:1')).toBe('Square1x1');
    });

    it('should preserve canonical aspect names', () => {
      expect(normalizeAspect('Widescreen16x9')).toBe('Widescreen16x9');
      expect(normalizeAspect('Vertical9x16')).toBe('Vertical9x16');
      expect(normalizeAspect('Square1x1')).toBe('Square1x1');
    });

    it('should handle whitespace', () => {
      expect(normalizeAspect(' 16:9 ')).toBe('Widescreen16x9');
      expect(normalizeAspect(' Widescreen16x9 ')).toBe('Widescreen16x9');
    });

    it('should default to Widescreen16x9 for unknown values', () => {
      expect(normalizeAspect('unknown')).toBe('Widescreen16x9');
      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('Unknown aspect value');
    });
  });

  describe('normalizeDensity', () => {
    it('should normalize legacy "Normal" to "Balanced"', () => {
      expect(normalizeDensity('Normal')).toBe('Balanced');
      expect(normalizeDensity('normal')).toBe('Balanced');
      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('deprecated');
    });

    it('should preserve canonical density values', () => {
      expect(normalizeDensity('Sparse')).toBe('Sparse');
      expect(normalizeDensity('Balanced')).toBe('Balanced');
      expect(normalizeDensity('Dense')).toBe('Dense');
    });

    it('should handle whitespace', () => {
      expect(normalizeDensity(' Balanced ')).toBe('Balanced');
    });

    it('should default to Balanced for unknown values', () => {
      expect(normalizeDensity('unknown')).toBe('Balanced');
      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('Unknown density value');
    });
  });

  describe('validateAndWarnEnums', () => {
    it('should warn about legacy aspect ratios', () => {
      const brief = { aspect: '16:9' as const };
      const planSpec = {};

      validateAndWarnEnums(brief, planSpec);

      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('legacy format');
    });

    it('should warn about deprecated density', () => {
      const brief = {};
      const planSpec = { density: 'Normal' as unknown as 'Balanced' };

      validateAndWarnEnums(brief, planSpec);

      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('deprecated');
    });

    it('should not warn for canonical values', () => {
      const brief = { aspect: 'Widescreen16x9' as const };
      const planSpec = { density: 'Balanced' as const };

      validateAndWarnEnums(brief, planSpec);

      expect(loggingWarnSpy).not.toHaveBeenCalled();
    });

    it('should handle empty objects', () => {
      validateAndWarnEnums({}, {});
      expect(loggingWarnSpy).not.toHaveBeenCalled();
    });

    it('should handle case sensitivity for density', () => {
      const brief = {};
      const planSpec = { density: 'NORMAL' as unknown as 'Balanced' };

      validateAndWarnEnums(brief, planSpec);

      expect(loggingWarnSpy).toHaveBeenCalled();
      const firstCall = loggingWarnSpy.mock.calls[0];
      expect(firstCall[0]).toContain('deprecated');
    });
  });

  describe('normalizeEnumsForApi', () => {
    it('should normalize both aspect and density', () => {
      const brief = { aspect: '16:9' as const, topic: 'Test' };
      const planSpec = { density: 'Normal' as unknown as 'Balanced', pacing: 'Fast' as const };

      const result = normalizeEnumsForApi(brief, planSpec);

      expect(result.brief.aspect).toBe('Widescreen16x9');
      expect(result.planSpec.density).toBe('Balanced');
      expect(result.brief.topic).toBe('Test');
      expect(result.planSpec.pacing).toBe('Fast');
    });

    it('should preserve canonical values', () => {
      const brief = { aspect: 'Widescreen16x9' as const };
      const planSpec = { density: 'Balanced' as const };

      const result = normalizeEnumsForApi(brief, planSpec);

      expect(result.brief.aspect).toBe('Widescreen16x9');
      expect(result.planSpec.density).toBe('Balanced');
    });

    it('should handle missing values', () => {
      const brief = {};
      const planSpec = {};

      const result = normalizeEnumsForApi(brief, planSpec);

      expect(result.brief.aspect).toBeUndefined();
      expect(result.planSpec.density).toBeUndefined();
    });

    it('should not mutate original objects', () => {
      const brief = { aspect: '16:9' as const, topic: 'Test' };
      const planSpec = { density: 'Normal' as unknown as 'Balanced' };

      normalizeEnumsForApi(brief, planSpec);

      expect(brief.aspect).toBe('16:9');
      expect(planSpec.density).toBe('Normal');
    });
  });
});
