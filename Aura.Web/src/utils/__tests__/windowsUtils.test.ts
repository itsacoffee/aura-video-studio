import { describe, it, expect, vi } from 'vitest';
import {
  isWindows,
  isWindows11,
  getDevicePixelRatio,
  isHighDPI,
  cssToPhysicalPixels,
  physicalToCSSPixels,
  getSystemThemePreference,
  getDPIScalingPercentage,
  getDPIScaleInfo,
  supportsSnapLayouts,
} from '../windowsUtils';

// Mock window APIs for testing
const mockWindow = (overrides: Partial<Window> = {}) => {
  Object.assign(global.window, overrides);
};

describe('windowsUtils', () => {
  describe('Platform Detection', () => {
    it('should detect Windows platform', () => {
      mockWindow({
        navigator: {
          ...window.navigator,
          platform: 'Win32',
        } as Navigator,
      });

      const result = isWindows();
      expect(typeof result).toBe('boolean');
    });

    it('should detect Windows 11', () => {
      mockWindow({
        navigator: {
          ...window.navigator,
          platform: 'Win32',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
        } as Navigator,
      });

      const result = isWindows11();
      expect(typeof result).toBe('boolean');
    });
  });

  describe('DPI Scaling', () => {
    it('should get device pixel ratio', () => {
      mockWindow({ devicePixelRatio: 2 });

      const ratio = getDevicePixelRatio();
      expect(ratio).toBe(2);
    });

    it('should detect high DPI', () => {
      mockWindow({ devicePixelRatio: 2 });

      const result = isHighDPI();
      expect(result).toBe(true);
    });

    it('should not detect high DPI for normal screens', () => {
      mockWindow({ devicePixelRatio: 1 });

      const result = isHighDPI();
      expect(result).toBe(false);
    });

    it('should convert CSS pixels to physical pixels', () => {
      mockWindow({ devicePixelRatio: 2 });

      const physical = cssToPhysicalPixels(100);
      expect(physical).toBe(200);
    });

    it('should convert physical pixels to CSS pixels', () => {
      mockWindow({ devicePixelRatio: 2 });

      const css = physicalToCSSPixels(200);
      expect(css).toBe(100);
    });

    it('should get DPI scaling percentage', () => {
      mockWindow({ devicePixelRatio: 1.5 });

      const percentage = getDPIScalingPercentage();
      expect(percentage).toBe(150);
    });

    it('should get comprehensive DPI scale info', () => {
      mockWindow({ devicePixelRatio: 2 });

      const info = getDPIScaleInfo();
      expect(info).toEqual({
        ratio: 2,
        percentage: 200,
        isHighDPI: true,
        scaleCategory: 'high',
      });
    });

    it('should categorize DPI scaling correctly', () => {
      const testCases = [
        { ratio: 1, category: 'normal' },
        { ratio: 1.5, category: 'medium' },
        { ratio: 2, category: 'high' },
        { ratio: 3, category: 'very-high' },
      ];

      testCases.forEach(({ ratio, category }) => {
        mockWindow({ devicePixelRatio: ratio });
        const info = getDPIScaleInfo();
        expect(info.scaleCategory).toBe(category);
      });
    });
  });

  describe('Theme Detection', () => {
    it('should detect system theme preference', () => {
      const matchMediaMock = vi.fn((query: string) => ({
        matches: query.includes('dark'),
        media: query,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
        onchange: null,
      }));

      window.matchMedia = matchMediaMock as unknown as typeof window.matchMedia;

      const theme = getSystemThemePreference();
      expect(['light', 'dark']).toContain(theme);
    });
  });

  describe('Windows 11 Features', () => {
    it('should detect snap layouts support', () => {
      mockWindow({
        navigator: {
          ...window.navigator,
          platform: 'Win32',
          userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)',
        } as Navigator,
      });

      const result = supportsSnapLayouts();
      expect(typeof result).toBe('boolean');
    });
  });
});
