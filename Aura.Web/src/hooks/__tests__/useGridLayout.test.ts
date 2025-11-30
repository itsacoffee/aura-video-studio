/**
 * Tests for useGridLayout Hook
 */

import { renderHook } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  useGridLayout,
  createGridStyles,
  useGridItemWidth,
  useGridIsMobile,
} from '../useGridLayout';

// Mock useDisplayEnvironment hook
vi.mock('../useDisplayEnvironment', () => ({
  useDisplayEnvironment: vi.fn(() => ({
    viewportWidth: 1200,
    viewportHeight: 800,
    sizeClass: 'regular',
    densityClass: 'standard',
  })),
}));

// Mock useContentDensity hook
vi.mock('../useContentDensity', () => ({
  useContentDensity: vi.fn(() => ({
    density: 'comfortable',
    spacingMultiplier: 1,
    fontSizeAdjustment: 0,
  })),
}));

describe('useGridLayout hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should return grid layout configuration', () => {
    const { result } = renderHook(() => useGridLayout());

    expect(result.current).toHaveProperty('columns');
    expect(result.current).toHaveProperty('columnWidth');
    expect(result.current).toHaveProperty('gap');
    expect(result.current).toHaveProperty('isMobile');
    expect(result.current).toHaveProperty('gridTemplateColumns');
  });

  it('should use content type defaults', () => {
    const { result: cardResult } = renderHook(() => useGridLayout({ contentType: 'card' }));
    const { result: thumbnailResult } = renderHook(() =>
      useGridLayout({ contentType: 'thumbnail' })
    );

    // Card has larger minimum width than thumbnail
    expect(cardResult.current.columnWidth).toBeGreaterThan(0);
    expect(thumbnailResult.current.columnWidth).toBeGreaterThan(0);
  });

  it('should respect minColumns constraint', () => {
    const { result } = renderHook(() =>
      useGridLayout({
        minColumns: 2,
        minColumnWidth: 100,
      })
    );

    expect(result.current.columns).toBeGreaterThanOrEqual(2);
  });

  it('should respect maxColumns constraint', () => {
    const { result } = renderHook(() =>
      useGridLayout({
        maxColumns: 3,
        minColumnWidth: 50,
      })
    );

    expect(result.current.columns).toBeLessThanOrEqual(3);
  });

  it('should use custom gap when specified', () => {
    const { result } = renderHook(() =>
      useGridLayout({
        gap: 24,
      })
    );

    expect(result.current.gap).toBe(24);
  });

  it('should calculate gap based on density when auto', () => {
    const { result } = renderHook(() =>
      useGridLayout({
        gap: 'auto',
      })
    );

    // With spacingMultiplier of 1, auto gap should be 16
    expect(result.current.gap).toBe(16);
  });

  it('should generate valid gridTemplateColumns', () => {
    const { result } = renderHook(() => useGridLayout());

    expect(result.current.gridTemplateColumns).toBeTruthy();
    expect(typeof result.current.gridTemplateColumns).toBe('string');
  });

  it('should handle different content types', () => {
    const contentTypes = ['card', 'thumbnail', 'tile', 'list-item', 'custom'] as const;

    contentTypes.forEach((contentType) => {
      const { result } = renderHook(() => useGridLayout({ contentType }));

      expect(result.current.columns).toBeGreaterThan(0);
      expect(result.current.columnWidth).toBeGreaterThan(0);
    });
  });
});

describe('createGridStyles helper', () => {
  it('should create valid CSS style object', () => {
    const layout = {
      columns: 3,
      columnWidth: 300,
      gap: 16,
      isMobile: false,
      gridTemplateColumns: 'repeat(3, 1fr)',
    };

    const styles = createGridStyles(layout);

    expect(styles.display).toBe('grid');
    expect(styles.gridTemplateColumns).toBe('repeat(3, 1fr)');
    expect(styles.gap).toBe('16px');
  });
});

describe('useGridItemWidth helper', () => {
  it('should return column width from layout', () => {
    const layout = {
      columns: 3,
      columnWidth: 300,
      gap: 16,
      isMobile: false,
      gridTemplateColumns: 'repeat(3, 1fr)',
    };

    const width = useGridItemWidth(layout);

    expect(width).toBe(300);
  });
});

describe('useGridIsMobile helper', () => {
  it('should return mobile state from layout', () => {
    const mobileLayout = {
      columns: 1,
      columnWidth: 300,
      gap: 16,
      isMobile: true,
      gridTemplateColumns: '1fr',
    };

    const desktopLayout = {
      columns: 3,
      columnWidth: 300,
      gap: 16,
      isMobile: false,
      gridTemplateColumns: 'repeat(3, 1fr)',
    };

    expect(useGridIsMobile(mobileLayout)).toBe(true);
    expect(useGridIsMobile(desktopLayout)).toBe(false);
  });
});
