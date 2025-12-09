/**
 * Tests for useSpacing hook
 */

import { renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { DensityProvider } from '../../contexts/DensityContext';
import { useSpacing } from '../useSpacing';

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

describe('useSpacing', () => {
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

  describe('unit value', () => {
    it('should return CSS custom property reference for unit', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.unit).toBe('var(--space-unit)');
    });
  });

  describe('getSpacing function', () => {
    it('should return calc expression for multiplier', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.getSpacing(2)).toBe('calc(var(--space-unit) * 2)');
      expect(result.current.getSpacing(4.5)).toBe('calc(var(--space-unit) * 4.5)');
    });
  });

  describe('inline spacing', () => {
    it('should return CSS custom property references for inline spacing', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.inline.xs).toBe('var(--space-inline-xs)');
      expect(result.current.inline.sm).toBe('var(--space-inline-sm)');
      expect(result.current.inline.md).toBe('var(--space-inline-md)');
      expect(result.current.inline.lg).toBe('var(--space-inline-lg)');
      expect(result.current.inline.xl).toBe('var(--space-inline-xl)');
    });
  });

  describe('stack spacing', () => {
    it('should return CSS custom property references for stack spacing', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.stack.xs).toBe('var(--space-stack-xs)');
      expect(result.current.stack.sm).toBe('var(--space-stack-sm)');
      expect(result.current.stack.md).toBe('var(--space-stack-md)');
      expect(result.current.stack.lg).toBe('var(--space-stack-lg)');
      expect(result.current.stack.xl).toBe('var(--space-stack-xl)');
    });
  });

  describe('card spacing', () => {
    it('should return CSS custom property references for card spacing', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.card.padding).toBe('var(--space-card-padding)');
      expect(result.current.card.gap).toBe('var(--space-card-gap)');
    });
  });

  describe('page spacing', () => {
    it('should return CSS custom property references for page spacing', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.page.padding).toBe('var(--space-page-padding)');
      expect(result.current.page.paddingX).toBe('var(--space-page-padding-x)');
    });
  });

  describe('section spacing', () => {
    it('should return CSS custom property reference for section gap', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.section.gap).toBe('var(--space-section-gap)');
    });
  });

  describe('scale values', () => {
    it('should return all scale values as CSS custom property references', () => {
      cleanupWindow = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
      });

      const { result } = renderHook(() => useSpacing(), {
        wrapper: createWrapper(),
      });

      expect(result.current.scale[0]).toBe('0');
      expect(result.current.scale.px).toBe('1px');
      expect(result.current.scale[0.5]).toBe('var(--space-0-5)');
      expect(result.current.scale[1]).toBe('var(--space-1)');
      expect(result.current.scale[1.5]).toBe('var(--space-1-5)');
      expect(result.current.scale[2]).toBe('var(--space-2)');
      expect(result.current.scale[3]).toBe('var(--space-3)');
      expect(result.current.scale[4]).toBe('var(--space-4)');
      expect(result.current.scale[8]).toBe('var(--space-8)');
      expect(result.current.scale[16]).toBe('var(--space-16)');
      expect(result.current.scale[24]).toBe('var(--space-24)');
    });
  });
});
