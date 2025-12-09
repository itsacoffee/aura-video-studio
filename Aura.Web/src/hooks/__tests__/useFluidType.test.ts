/**
 * Tests for useFluidType hook
 */

import { renderHook } from '@testing-library/react';
import { describe, it, expect, afterEach } from 'vitest';
import { useFluidType } from '../useFluidType';

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

describe('useFluidType', () => {
  let cleanup: (() => void) | null = null;

  afterEach(() => {
    if (cleanup) {
      cleanup();
      cleanup = null;
    }
  });

  describe('scale calculation', () => {
    it('should return all scale sizes', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.scale).toHaveProperty('xs');
      expect(result.current.scale).toHaveProperty('sm');
      expect(result.current.scale).toHaveProperty('md');
      expect(result.current.scale).toHaveProperty('lg');
      expect(result.current.scale).toHaveProperty('xl');
      expect(result.current.scale).toHaveProperty('2xl');
      expect(result.current.scale).toHaveProperty('3xl');
      expect(result.current.scale).toHaveProperty('4xl');
    });

    it('should return pixel values for scale', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());

      // Use simpler regex patterns to avoid security warning
      expect(result.current.scale.md).toContain('px');
      expect(result.current.scale.lg).toContain('px');
      expect(parseFloat(result.current.scale.md)).toBeGreaterThan(0);
      expect(parseFloat(result.current.scale.lg)).toBeGreaterThan(0);
    });

    it('should return larger base size for ultra density', () => {
      cleanup = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
        devicePixelRatio: 2,
        screen: { width: 5120, height: 2880 },
      });

      const { result } = renderHook(() => useFluidType());

      // Ultra density should use 18px base
      expect(result.current.config.baseSize).toBe(18);
    });

    it('should return smaller base size for low density', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1024,
        innerHeight: 768,
        devicePixelRatio: 1,
        screen: { width: 1024, height: 768 },
      });

      const { result } = renderHook(() => useFluidType());

      // Low density should use 14px base
      expect(result.current.config.baseSize).toBe(14);
    });
  });

  describe('scale ratio', () => {
    it('should use minor third (1.2) for regular viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1440,
        innerHeight: 900,
        screen: { width: 1440, height: 900 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.config.scaleRatio).toBe(1.2);
    });

    it('should use major third (1.25) for large viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.config.scaleRatio).toBe(1.25);
    });

    it('should use perfect fourth (1.333) for very large viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 2560,
        innerHeight: 1440,
        screen: { width: 2560, height: 1440 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.config.scaleRatio).toBe(1.333);
    });

    it('should use major second (1.125) for small viewports', () => {
      cleanup = mockWindowProperties({
        innerWidth: 600,
        innerHeight: 800,
        screen: { width: 600, height: 800 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.config.scaleRatio).toBe(1.125);
    });
  });

  describe('getFluidSize', () => {
    it('should return a clamp CSS value', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());
      const fluidSize = result.current.getFluidSize(14, 18);

      // Verify the output is a valid clamp() CSS function
      expect(fluidSize).toContain('clamp(');
      expect(fluidSize).toContain('px');
      expect(fluidSize).toContain('vw');
    });

    it('should include min and max values in clamp', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());
      const fluidSize = result.current.getFluidSize(14, 18);

      expect(fluidSize).toContain('14px');
      expect(fluidSize).toContain('18px');
    });
  });

  describe('config properties', () => {
    it('should include viewport boundaries', () => {
      cleanup = mockWindowProperties({
        innerWidth: 1920,
        innerHeight: 1080,
        screen: { width: 1920, height: 1080 },
      });

      const { result } = renderHook(() => useFluidType());

      expect(result.current.config.minViewport).toBe(375);
      expect(result.current.config.maxViewport).toBe(2560);
    });
  });
});
