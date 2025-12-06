/**
 * Tests for useUIScale hook
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useUIScale, useUIScaleCSS } from '../useUIScale';

// Helper to mock window dimensions
const mockWindowDimensions = (width: number, height: number) => {
  Object.defineProperty(window, 'innerWidth', {
    writable: true,
    configurable: true,
    value: width,
  });

  Object.defineProperty(window, 'innerHeight', {
    writable: true,
    configurable: true,
    value: height,
  });
};

// Helper to trigger resize event
const triggerResize = () => {
  window.dispatchEvent(new Event('resize'));
};

describe('useUIScale', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  describe('default configuration', () => {
    it('should calculate scale for 1920x1080 base at full HD', () => {
      mockWindowDimensions(1920, 1080);

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBe(1);
      expect(result.current.windowWidth).toBe(1920);
      expect(result.current.windowHeight).toBe(1080);
      expect(result.current.scaledWidth).toBe(1920);
      expect(result.current.scaledHeight).toBe(1080);
    });

    it('should scale up for 4K display (3840x2160)', () => {
      mockWindowDimensions(3840, 2160);

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBe(2);
      expect(result.current.windowWidth).toBe(3840);
      expect(result.current.windowHeight).toBe(2160);
      expect(result.current.scaledWidth).toBe(1920);
      expect(result.current.scaledHeight).toBe(1080);
    });

    it('should scale down for smaller window (960x540)', () => {
      mockWindowDimensions(960, 540);

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBe(0.5);
      expect(result.current.windowWidth).toBe(960);
      expect(result.current.windowHeight).toBe(540);
      expect(result.current.scaledWidth).toBe(1920);
      expect(result.current.scaledHeight).toBe(1080);
    });

    it('should handle 30% of screen (576x324)', () => {
      mockWindowDimensions(576, 324);

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBeCloseTo(0.3, 1);
      expect(result.current.windowWidth).toBe(576);
      expect(result.current.windowHeight).toBe(324);
    });
  });

  describe('fill mode (default)', () => {
    it('should use max scale for fill behavior', () => {
      // Wider aspect ratio (ultrawide)
      mockWindowDimensions(2560, 1080);

      const { result } = renderHook(() => useUIScale({ mode: 'fill' }));

      // Should use height scale (1.0) instead of width scale (~1.33)
      // because max(2560/1920, 1080/1080) = max(1.33, 1.0) = 1.33
      const expectedScale = Math.max(2560 / 1920, 1080 / 1080);
      expect(result.current.scale).toBeCloseTo(expectedScale, 2);
    });

    it('should fill viewport with no letterboxing', () => {
      mockWindowDimensions(1600, 900);

      const { result } = renderHook(() => useUIScale({ mode: 'fill' }));

      const scaleX = 1600 / 1920;
      const scaleY = 900 / 1080;
      const expectedScale = Math.max(scaleX, scaleY);

      expect(result.current.scale).toBeCloseTo(expectedScale, 2);
    });
  });

  describe('contain mode', () => {
    it('should use min scale for contain behavior', () => {
      // Wider aspect ratio
      mockWindowDimensions(2560, 1080);

      const { result } = renderHook(() => useUIScale({ mode: 'contain' }));

      // Should use height scale (1.0) instead of width scale (~1.33)
      const expectedScale = Math.min(2560 / 1920, 1080 / 1080);
      expect(result.current.scale).toBe(expectedScale);
    });

    it('should contain content with potential letterboxing', () => {
      mockWindowDimensions(1600, 900);

      const { result } = renderHook(() => useUIScale({ mode: 'contain' }));

      const scaleX = 1600 / 1920;
      const scaleY = 900 / 1080;
      const expectedScale = Math.min(scaleX, scaleY);

      expect(result.current.scale).toBeCloseTo(expectedScale, 2);
    });
  });

  describe('custom base dimensions', () => {
    it('should use custom base width and height', () => {
      mockWindowDimensions(3840, 2160);

      const { result } = renderHook(() => useUIScale({ baseWidth: 3840, baseHeight: 2160 }));

      expect(result.current.scale).toBe(1);
      expect(result.current.scaledWidth).toBe(3840);
      expect(result.current.scaledHeight).toBe(2160);
    });

    it('should calculate correctly with 1280x720 base', () => {
      mockWindowDimensions(1920, 1080);

      const { result } = renderHook(() => useUIScale({ baseWidth: 1280, baseHeight: 720 }));

      expect(result.current.scale).toBe(1.5);
      expect(result.current.scaledWidth).toBe(1280);
      expect(result.current.scaledHeight).toBe(720);
    });
  });

  describe('resize handling', () => {
    it('should update scale when window is resized', () => {
      mockWindowDimensions(1920, 1080);

      const { result } = renderHook(() => useUIScale({ debounceDelay: 100 }));

      expect(result.current.scale).toBe(1);

      // Resize window
      act(() => {
        mockWindowDimensions(960, 540);
        triggerResize();
      });

      // Fast forward debounce timer
      act(() => {
        vi.advanceTimersByTime(100);
      });

      expect(result.current.scale).toBe(0.5);
    });

    it('should debounce rapid resize events', () => {
      mockWindowDimensions(1920, 1080);

      const { result } = renderHook(() => useUIScale({ debounceDelay: 200 }));

      expect(result.current.scale).toBe(1);

      // Multiple rapid resizes
      act(() => {
        mockWindowDimensions(1000, 500);
        triggerResize();

        vi.advanceTimersByTime(50);

        mockWindowDimensions(1200, 600);
        triggerResize();

        vi.advanceTimersByTime(50);

        mockWindowDimensions(1600, 900);
        triggerResize();
      });

      // Should not update yet (debouncing)
      expect(result.current.scale).toBe(1);

      // Fast forward past debounce
      act(() => {
        vi.advanceTimersByTime(200);
      });

      // Should update to final size
      expect(result.current.windowWidth).toBe(1600);
      expect(result.current.windowHeight).toBe(900);
    });

    it('should use custom debounce delay', () => {
      mockWindowDimensions(1920, 1080);

      const { result } = renderHook(() => useUIScale({ debounceDelay: 500 }));

      act(() => {
        mockWindowDimensions(960, 540);
        triggerResize();
      });

      // Should not update after 200ms
      act(() => {
        vi.advanceTimersByTime(200);
      });
      expect(result.current.scale).toBe(1);

      // Should update after 500ms
      act(() => {
        vi.advanceTimersByTime(300);
      });

      expect(result.current.scale).toBe(0.5);
    });
  });

  describe('edge cases', () => {
    it('should handle very small windows', () => {
      mockWindowDimensions(320, 240);

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBeGreaterThan(0);
      // With fill mode, uses max(320/1920, 240/1080) = max(0.167, 0.222) = 0.222
      const expectedScale = Math.max(320 / 1920, 240 / 1080);
      expect(result.current.scale).toBeCloseTo(expectedScale, 2);
    });

    it('should handle very large windows', () => {
      mockWindowDimensions(7680, 4320); // 8K

      const { result } = renderHook(() => useUIScale());

      expect(result.current.scale).toBe(4);
    });

    it('should handle portrait orientation', () => {
      mockWindowDimensions(1080, 1920);

      const { result } = renderHook(() => useUIScale());

      const scaleX = 1080 / 1920;
      const scaleY = 1920 / 1080;
      const expectedScale = Math.max(scaleX, scaleY);

      expect(result.current.scale).toBeCloseTo(expectedScale, 2);
    });
  });

  describe('cleanup', () => {
    it('should remove resize listener on unmount', () => {
      mockWindowDimensions(1920, 1080);

      const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener');

      const { unmount } = renderHook(() => useUIScale());

      unmount();

      expect(removeEventListenerSpy).toHaveBeenCalledWith('resize', expect.any(Function));
    });
  });
});

describe('useUIScaleCSS', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    // Clear any existing custom properties
    document.documentElement.style.removeProperty('--ui-scale');
    document.documentElement.style.removeProperty('--ui-scaled-width');
    document.documentElement.style.removeProperty('--ui-scaled-height');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
    // Clean up custom properties
    document.documentElement.style.removeProperty('--ui-scale');
    document.documentElement.style.removeProperty('--ui-scaled-width');
    document.documentElement.style.removeProperty('--ui-scaled-height');
  });

  describe('CSS custom properties', () => {
    it('should set CSS custom properties on mount', () => {
      mockWindowDimensions(1920, 1080);

      renderHook(() => useUIScaleCSS());

      const root = document.documentElement;

      expect(root.style.getPropertyValue('--ui-scale')).toBe('1');
      expect(root.style.getPropertyValue('--ui-scaled-width')).toBe('1920px');
      expect(root.style.getPropertyValue('--ui-scaled-height')).toBe('1080px');
    });

    it('should update CSS custom properties when scale changes', () => {
      mockWindowDimensions(1920, 1080);

      renderHook(() => useUIScaleCSS({ debounceDelay: 100 }));

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--ui-scale')).toBe('1');

      // Resize
      act(() => {
        mockWindowDimensions(960, 540);
        triggerResize();
        vi.advanceTimersByTime(100);
      });

      expect(root.style.getPropertyValue('--ui-scale')).toBe('0.5');
      expect(root.style.getPropertyValue('--ui-scaled-width')).toBe('1920px');
      expect(root.style.getPropertyValue('--ui-scaled-height')).toBe('1080px');
    });

    it('should remove CSS custom properties on unmount', () => {
      mockWindowDimensions(1920, 1080);

      const { unmount } = renderHook(() => useUIScaleCSS());

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--ui-scale')).toBe('1');

      unmount();

      expect(root.style.getPropertyValue('--ui-scale')).toBe('');
      expect(root.style.getPropertyValue('--ui-scaled-width')).toBe('');
      expect(root.style.getPropertyValue('--ui-scaled-height')).toBe('');
    });
  });

  describe('integration with useUIScale', () => {
    it('should return same values as useUIScale', () => {
      mockWindowDimensions(1920, 1080);

      const { result: scaleResult } = renderHook(() => useUIScale());
      const { result: cssResult } = renderHook(() => useUIScaleCSS());

      expect(cssResult.current.scale).toBe(scaleResult.current.scale);
      expect(cssResult.current.scaledWidth).toBe(scaleResult.current.scaledWidth);
      expect(cssResult.current.scaledHeight).toBe(scaleResult.current.scaledHeight);
    });
  });
});
