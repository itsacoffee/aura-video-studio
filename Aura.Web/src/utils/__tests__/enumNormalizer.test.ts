import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  normalizeAspect,
  normalizeDensity,
  validateAndWarnEnums,
  normalizeEnumsForApi,
} from '../enumNormalizer';

describe('EnumNormalizer', () => {
  let consoleWarnSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
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
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Unknown aspect value')
      );
    });
  });

  describe('normalizeDensity', () => {
    it('should normalize legacy "Normal" to "Balanced"', () => {
      expect(normalizeDensity('Normal')).toBe('Balanced');
      expect(normalizeDensity('normal')).toBe('Balanced');
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('deprecated')
      );
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
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Unknown density value')
      );
    });
  });

  describe('validateAndWarnEnums', () => {
    it('should warn about legacy aspect ratios', () => {
      const brief = { aspect: '16:9' as const };
      const planSpec = {};
      
      validateAndWarnEnums(brief, planSpec);
      
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('legacy format')
      );
    });

    it('should warn about deprecated density', () => {
      const brief = {};
      const planSpec = { density: 'Normal' as any };
      
      validateAndWarnEnums(brief, planSpec);
      
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('deprecated')
      );
    });

    it('should not warn for canonical values', () => {
      const brief = { aspect: 'Widescreen16x9' as const };
      const planSpec = { density: 'Balanced' as const };
      
      validateAndWarnEnums(brief, planSpec);
      
      expect(consoleWarnSpy).not.toHaveBeenCalled();
    });

    it('should handle empty objects', () => {
      validateAndWarnEnums({}, {});
      expect(consoleWarnSpy).not.toHaveBeenCalled();
    });

    it('should handle case sensitivity for density', () => {
      const brief = {};
      const planSpec = { density: 'NORMAL' as any };
      
      validateAndWarnEnums(brief, planSpec);
      
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('deprecated')
      );
    });
  });

  describe('normalizeEnumsForApi', () => {
    it('should normalize both aspect and density', () => {
      const brief = { aspect: '16:9' as const, topic: 'Test' };
      const planSpec = { density: 'Normal' as any, pacing: 'Fast' as const };
      
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
      const planSpec = { density: 'Normal' as any };
      
      normalizeEnumsForApi(brief, planSpec);
      
      expect(brief.aspect).toBe('16:9');
      expect(planSpec.density).toBe('Normal');
    });
  });
});
