/**
 * Grid Layout Hook
 *
 * Provides intelligent grid layout calculations based on container
 * dimensions, item constraints, and content type for Apple-level
 * adaptive grid layouts.
 */

import { useState, useEffect, useMemo, type RefObject } from 'react';
import { useContentDensity } from './useContentDensity';
import { useDisplayEnvironment } from './useDisplayEnvironment';

/**
 * Content type hints for automatic sizing
 */
export type GridContentType = 'card' | 'thumbnail' | 'tile' | 'list-item' | 'custom';

/**
 * Grid layout configuration
 */
export interface GridLayoutConfig {
  /** Number of columns */
  columns: number;
  /** Calculated column width in pixels */
  columnWidth: number;
  /** Gap between items in pixels */
  gap: number;
  /** Whether layout is in mobile/single-column mode */
  isMobile: boolean;
  /** CSS grid-template-columns value */
  gridTemplateColumns: string;
}

/**
 * Options for useGridLayout hook
 */
export interface UseGridLayoutOptions {
  /** Content type hint for automatic sizing */
  contentType?: GridContentType;
  /** Minimum column width in pixels */
  minColumnWidth?: number;
  /** Maximum column width in pixels */
  maxColumnWidth?: number;
  /** Minimum number of columns */
  minColumns?: number;
  /** Maximum number of columns */
  maxColumns?: number;
  /** Gap between items (or 'auto' for density-based) */
  gap?: number | 'auto';
  /** Container ref for width calculation (optional, uses viewport if not provided) */
  containerRef?: RefObject<HTMLElement>;
  /** Container padding to subtract from available width */
  containerPadding?: number;
}

/**
 * Default sizing constraints by content type
 */
const contentTypeDefaults: Record<GridContentType, { min: number; max: number }> = {
  card: { min: 280, max: 400 },
  thumbnail: { min: 150, max: 250 },
  tile: { min: 200, max: 350 },
  'list-item': { min: 300, max: 600 },
  custom: { min: 200, max: 400 },
};

/**
 * Calculate grid layout based on available width and constraints
 */
function calculateGridLayout(
  availableWidth: number,
  minColumnWidth: number,
  maxColumnWidth: number,
  minColumns: number,
  maxColumns: number,
  gap: number
): GridLayoutConfig {
  // Handle edge case where available width is too small
  if (availableWidth <= 0) {
    return {
      columns: minColumns,
      columnWidth: minColumnWidth,
      gap,
      isMobile: true,
      gridTemplateColumns: '1fr',
    };
  }

  // Calculate maximum possible columns
  const maxPossibleColumns = Math.floor((availableWidth + gap) / (minColumnWidth + gap));
  const columns = Math.max(minColumns, Math.min(maxColumns, maxPossibleColumns));

  // Calculate actual column width
  const totalGapWidth = gap * (columns - 1);
  const availableForColumns = availableWidth - totalGapWidth;
  const rawColumnWidth = availableForColumns / columns;

  // Clamp column width
  const columnWidth = Math.max(minColumnWidth, Math.min(maxColumnWidth, rawColumnWidth));

  // Determine if mobile mode
  const isMobile = columns === 1 || availableWidth < minColumnWidth * 2;

  // Build grid-template-columns value
  let gridTemplateColumns: string;
  if (isMobile) {
    gridTemplateColumns = '1fr';
  } else if (maxColumnWidth < Infinity) {
    gridTemplateColumns = `repeat(${columns}, minmax(${minColumnWidth}px, ${maxColumnWidth}px))`;
  } else {
    gridTemplateColumns = `repeat(${columns}, 1fr)`;
  }

  return {
    columns,
    columnWidth,
    gap,
    isMobile,
    gridTemplateColumns,
  };
}

/**
 * Custom hook for intelligent grid layout calculations
 *
 * Provides calculated grid layout properties based on container
 * dimensions, content type, and responsive design constraints.
 *
 * @param options Configuration options for the grid layout
 * @returns GridLayoutConfig object with calculated layout properties
 *
 * @example
 * ```tsx
 * // Basic usage with content type
 * const layout = useGridLayout({ contentType: 'card' });
 *
 * return (
 *   <div style={{
 *     display: 'grid',
 *     gridTemplateColumns: layout.gridTemplateColumns,
 *     gap: layout.gap,
 *   }}>
 *     {items.map(item => <Card key={item.id}>{item.name}</Card>)}
 *   </div>
 * );
 *
 * // Advanced usage with container ref
 * const containerRef = useRef<HTMLDivElement>(null);
 * const layout = useGridLayout({
 *   contentType: 'thumbnail',
 *   containerRef,
 *   minColumns: 2,
 *   maxColumns: 6,
 * });
 *
 * return (
 *   <div ref={containerRef} style={{ gridTemplateColumns: layout.gridTemplateColumns }}>
 *     ...
 *   </div>
 * );
 * ```
 */
export function useGridLayout(options: UseGridLayoutOptions = {}): GridLayoutConfig {
  const {
    contentType = 'card',
    minColumnWidth,
    maxColumnWidth,
    minColumns = 1,
    maxColumns = 6,
    gap: gapOption = 'auto',
    containerRef,
    containerPadding = 0,
  } = options;

  const display = useDisplayEnvironment();
  const { spacingMultiplier } = useContentDensity();

  // Get defaults from content type
  const defaults = contentTypeDefaults[contentType];
  const effectiveMinWidth = minColumnWidth ?? defaults.min;
  const effectiveMaxWidth = maxColumnWidth ?? defaults.max;

  // Calculate gap based on density
  const effectiveGap = useMemo(() => {
    if (gapOption === 'auto') {
      return Math.round(16 * spacingMultiplier);
    }
    return gapOption;
  }, [gapOption, spacingMultiplier]);

  // Track container width
  const [containerWidth, setContainerWidth] = useState<number>(() => {
    if (containerRef?.current) {
      return containerRef.current.offsetWidth - containerPadding;
    }
    return display.viewportWidth - containerPadding;
  });

  // Update container width on resize
  useEffect(() => {
    const updateWidth = () => {
      if (containerRef?.current) {
        setContainerWidth(containerRef.current.offsetWidth - containerPadding);
      } else {
        setContainerWidth(display.viewportWidth - containerPadding);
      }
    };

    updateWidth();
  }, [containerRef, containerPadding, display.viewportWidth]);

  // Set up ResizeObserver for container
  useEffect(() => {
    if (!containerRef?.current) return;

    const element = containerRef.current;
    const resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (entry) {
        setContainerWidth(entry.contentRect.width - containerPadding);
      }
    });

    resizeObserver.observe(element);

    return () => {
      resizeObserver.disconnect();
    };
  }, [containerRef, containerPadding]);

  // Calculate layout
  return useMemo(
    () =>
      calculateGridLayout(
        containerWidth,
        effectiveMinWidth,
        effectiveMaxWidth,
        minColumns,
        maxColumns,
        effectiveGap
      ),
    [containerWidth, effectiveMinWidth, effectiveMaxWidth, minColumns, maxColumns, effectiveGap]
  );
}

/**
 * Hook to get grid item width based on layout
 */
export function useGridItemWidth(layout: GridLayoutConfig): number {
  return layout.columnWidth;
}

/**
 * Hook to check if grid is in mobile mode
 */
export function useGridIsMobile(layout: GridLayoutConfig): boolean {
  return layout.isMobile;
}

/**
 * Helper to create grid style object
 */
export function createGridStyles(layout: GridLayoutConfig): React.CSSProperties {
  return {
    display: 'grid',
    gridTemplateColumns: layout.gridTemplateColumns,
    gap: `${layout.gap}px`,
  };
}
