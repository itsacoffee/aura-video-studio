/**
 * Tests for useDisplayEnvironment hook
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useDisplayEnvironment } from '../useDisplayEnvironment';

// Mock window properties
const mockWindowProperties = (props: {
  innerWidth?: number;
  innerHeight?: number;
  devicePixelRatio?: number;
  screen?: { width: number; height: number };
}) => {
  const original = {
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
    devicePixelRatio: window.devicePixelRatio,
    screen: window.screen,
  };

  Object.defineProperty(window, 'innerWidth', {
    writable: true,
    configurable: true,
    value: props.innerWidth ?? 1920,
  });

  Object.defineProperty(window, 'innerHeight', {
    writable: true,
    configurable: true,
    value: props.innerHeight ?? 1080,
  });

  Object.defineProperty(window, 'devicePixelRatio', {
    writable: true,
    configurable: true,
    value: props.devicePixelRatio ?? 1,
  });

  if (props.screen) {
    Object.defineProperty(window, 'screen', {
      writable: true,
      configurable: true,
      value: {
        width: props.screen.width,
        height: props.screen.height,
      },
    });
  }

  return () => {
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: original.innerWidth,
    });
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      configurable: true,
      value: original.innerHeight,
    });
    Object.defineProperty(window, 'devicePixelRatio', {
      writable: true,
      configurable: true,
      value: original.devicePixelRatio,
    });
    Object.defineProperty(window, 'screen', {
      writable: true,
      configurable: true,
      value: original.screen,
    });
  };
};

describe('useDisplayEnvironment', () => {
  let cleanup: (() => void) | null = null;

  afterEach(() => {
    if (cleanup) {
      cleanup();
      cleanup = null;
    }
  });

  describe('size class detection', () => {
    it('should detect compact size class for small viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 600,
        screen: { width: 800, height: 600 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.sizeClass).toBe('compact');
    });

    it('should detect regular size class for medium viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        screen: { width: 1440, height: 900 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.sizeClass).toBe('regular');
    });

    it('should detect expanded size class for large viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
        screen: { width: 2560, height: 1440 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.sizeClass).toBe('expanded');
    });
  });

  describe('aspect ratio detection', () => {
    it('should detect portrait aspect ratio', () => {
      cleanup = mockWindowProperties({
        innerWidth: 600,
        innerHeight: 900,
        screen: { width: 600, height: 900 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.aspectRatio).toBe('portrait');
    });

    it('should detect landscape aspect ratio', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.aspectRatio).toBe('landscape');
    });

    it('should detect ultrawide aspect ratio', () => {
      cleanup = mockWindowProperties({
        innerWidth: 3440,
        innerHeight: 1440,
        screen: { width: 3440, height: 1440 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.aspectRatio).toBe('ultrawide');
    });
  });

  describe('content columns calculation', () => {
    it('should return 1 column for small viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 600,
        screen: { width: 800, height: 600 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.contentColumns).toBe(1);
    });

    it('should return 2 columns for medium viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        screen: { width: 1440, height: 900 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.contentColumns).toBe(2);
    });

    it('should return 3 columns for large viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 2000,
        innerHeight: 1200,
        screen: { width: 2000, height: 1200 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.contentColumns).toBe(3);
    });

    it('should return 4 columns for very large viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 2800,
        innerHeight: 1600,
        screen: { width: 2800, height: 1600 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.contentColumns).toBe(4);
    });
  });

  describe('panel layout recommendation', () => {
    it('should recommend stacked layout for compact displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 600,
        screen: { width: 800, height: 600 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.panelLayout).toBe('stacked');
    });

    it('should recommend side-by-side layout for regular displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        screen: { width: 1440, height: 900 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.panelLayout).toBe('side-by-side');
    });

    it('should recommend three-panel layout for expanded ultrawide displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 3440,
        innerHeight: 1440,
        screen: { width: 3440, height: 1440 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.panelLayout).toBe('three-panel');
    });
  });

  describe('feature flags', () => {
    it('should not allow secondary panels on compact displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 600,
        screen: { width: 800, height: 600 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.canShowSecondaryPanels).toBe(false);
    });

    it('should allow secondary panels on regular displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        screen: { width: 1440, height: 900 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.canShowSecondaryPanels).toBe(true);
    });

    it('should prefer compact controls on short viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 700,
        screen: { width: 1440, height: 700 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.preferCompactControls).toBe(true);
    });
  });

  describe('density class', () => {
    it('should detect standard density for 1080p at 100% DPI', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        devicePixelRatio: 1,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.densityClass).toBe('high');
    });

    it('should detect high density for hi-DPI displays', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        devicePixelRatio: 2,
        screen: { width: 3840, height: 2160 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.densityClass).toBe('ultra');
    });
  });

  describe('resize handling', () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('should update on window resize after debounce', async () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useDisplayEnvironment());

      expect(result.current.viewportWidth).toBe(1920);

      // Update window dimensions
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1440,
      });

      // Trigger resize event
      act(() => {
        window.dispatchEvent(new Event('resize'));
      });

      // Fast-forward past debounce
      act(() => {
        vi.advanceTimersByTime(150);
      });

      expect(result.current.viewportWidth).toBe(1440);
    });
  });
});
