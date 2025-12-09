/**
 * Tests for DensityContext
 */

import { renderHook, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { DensityProvider, useDensity } from '../DensityContext';

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
const mockWindowProperties = (props: {
  innerWidth?: number;
  innerHeight?: number;
  devicePixelRatio?: number;
}) => {
  const original = {
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
    devicePixelRatio: window.devicePixelRatio,
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
  };
};

// Wrapper component for testing hooks with context
const createWrapper = () => {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <DensityProvider>{children}</DensityProvider>;
  };
};

describe('DensityContext', () => {
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

  describe('useDensity hook', () => {
    it('should throw error when used outside provider', () => {
      expect(() => {
        renderHook(() => useDensity());
      }).toThrow('useDensity must be used within DensityProvider');
    });

    it('should provide density context when used within provider', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current).toBeDefined();
      expect(result.current.density).toBeDefined();
      expect(result.current.setDensity).toBeDefined();
      expect(result.current.isAuto).toBeDefined();
    });
  });

  describe('auto density calculation', () => {
    it('should return compact density for small screens', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 600,
        innerHeight: 400,
        devicePixelRatio: 1,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.autoDensity).toBe('compact');
      expect(result.current.density).toBe('compact');
    });

    it('should return comfortable density for medium screens', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        devicePixelRatio: 1,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.autoDensity).toBe('comfortable');
    });

    it('should return spacious density for large screens', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
        devicePixelRatio: 1,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.autoDensity).toBe('spacious');
    });
  });

  describe('manual density override', () => {
    it('should allow setting manual density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.isAuto).toBe(true);

      act(() => {
        result.current.setDensity('compact');
      });

      expect(result.current.density).toBe('compact');
      expect(result.current.isAuto).toBe(false);
    });

    it('should persist density preference to localStorage', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setDensity('spacious');
      });

      expect(localStorageMock.setItem).toHaveBeenCalledWith('aura-density-preference', 'spacious');
    });

    it('should restore density preference from localStorage', () => {
      localStorageMock.store['aura-density-preference'] = 'compact';
      localStorageMock.getItem.mockImplementation(
        (key: string) => localStorageMock.store[key] || null
      );

      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.density).toBe('compact');
      expect(result.current.isAuto).toBe(false);
      expect(result.current.autoDensity).toBe('spacious');
    });

    it('should reset to auto density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      act(() => {
        result.current.setDensity('compact');
      });

      expect(result.current.isAuto).toBe(false);

      act(() => {
        result.current.setDensity('auto');
      });

      expect(result.current.isAuto).toBe(true);
      expect(result.current.density).toBe('spacious');
    });
  });

  describe('spacing scale', () => {
    it('should return 0.75 for compact density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 600,
        innerHeight: 400,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.spacingScale).toBe(0.75);
    });

    it('should return 1 for comfortable density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.spacingScale).toBe(1);
    });

    it('should return 1.25 for spacious density', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
      });

      const { result } = renderHook(() => useDensity(), {
        wrapper: createWrapper(),
      });

      expect(result.current.spacingScale).toBe(1.25);
    });
  });
});
