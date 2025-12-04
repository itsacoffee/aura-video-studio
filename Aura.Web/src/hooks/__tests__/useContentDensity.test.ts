/**
 * Tests for useContentDensity hook
 */

import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useContentDensity } from '../useContentDensity';

// Mock localStorage
const mockLocalStorage = () => {
  const store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      Object.keys(store).forEach((key) => delete store[key]);
    }),
    store,
  };
};

// Mock window properties
const mockWindowProperties = (props: { innerWidth?: number; innerHeight?: number }) => {
  const original = {
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
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
  };
};

describe('useContentDensity', () => {
  let localStorageMock: ReturnType<typeof mockLocalStorage>;
  let cleanupWindow: (() => void) | null = null;

  beforeEach(() => {
    localStorageMock = mockLocalStorage();
    vi.stubGlobal('localStorage', localStorageMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    if (cleanupWindow) {
      cleanupWindow();
      cleanupWindow = null;
    }
  });

  describe('auto density calculation', () => {
    it('should return compact density for small height', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 700,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.autoDensity).toBe('compact');
      expect(result.current.density).toBe('compact');
    });

    it('should return compact density for small width', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.autoDensity).toBe('compact');
    });

    it('should return comfortable density for standard displays', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.autoDensity).toBe('comfortable');
    });

    it('should return spacious density for large displays', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.autoDensity).toBe('spacious');
    });
  });

  describe('manual density override', () => {
    it('should allow setting manual density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.isAuto).toBe(true);

      act(() => {
        result.current.setDensity('compact');
      });

      expect(result.current.density).toBe('compact');
      expect(result.current.isAuto).toBe(false);
    });

    it('should persist density to localStorage', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useContentDensity());

      act(() => {
        result.current.setDensity('spacious');
      });

      expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-content-density', 'spacious');
    });

    it('should restore density from localStorage', () => {
      localStorageMock.store['aura-content-density'] = 'compact';
      localStorageMock.getItem.mockImplementation(
        (key: string) => localStorageMock.store[key] || null
      );

      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useContentDensity());

      // Should use saved preference over auto
      expect(result.current.density).toBe('compact');
      expect(result.current.isAuto).toBe(false);
      expect(result.current.autoDensity).toBe('spacious'); // Auto would be spacious
    });
  });

  describe('reset to auto', () => {
    it('should reset to auto density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useContentDensity());

      // Set manual
      act(() => {
        result.current.setDensity('compact');
      });

      expect(result.current.isAuto).toBe(false);
      expect(result.current.density).toBe('compact');

      // Reset to auto
      act(() => {
        result.current.resetToAuto();
      });

      expect(result.current.isAuto).toBe(true);
      expect(result.current.density).toBe('spacious'); // Auto value
    });
  });

  describe('computed values', () => {
    it('should provide correct spacing multiplier for compact', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 800,
        innerHeight: 600,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.spacingMultiplier).toBe(0.75);
    });

    it('should provide correct spacing multiplier for comfortable', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.spacingMultiplier).toBe(1);
    });

    it('should provide correct spacing multiplier for spacious', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.spacingMultiplier).toBe(1.25);
    });

    it('should provide density class name', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
      });

      const { result } = renderHook(() => useContentDensity());

      expect(result.current.densityClass).toBe('density-comfortable');
    });
  });
});
