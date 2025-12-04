/**
 * Fluid Grid Component
 *
 * Apple-level fluid grid that intelligently calculates column count
 * based on available space and minimum item width.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import React, { useMemo, type ReactNode, type CSSProperties } from 'react';
import { useAdaptiveLayoutContext } from '../../contexts/AdaptiveLayoutContext';

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    width: '100%',
  },
  gridEqualHeight: {
    '& > *': {
      height: '100%',
    },
  },
});

/**
 * Gap size presets
 */
export type GapSize = 'tight' | 'normal' | 'wide';

/**
 * Props for FluidGrid
 */
export interface FluidGridProps {
  /** Grid items */
  children: ReactNode;
  /** Minimum width for each item in pixels */
  minItemWidth?: number;
  /** Maximum number of columns */
  maxColumns?: number;
  /** Gap between items */
  gap?: GapSize | number;
  /** Whether all items should have equal height */
  equalHeight?: boolean;
  /** Additional class name */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Role for accessibility */
  role?: string;
  /** Aria label for accessibility */
  'aria-label'?: string;
}

/**
 * Convert gap preset to pixel value
 */
function getGapPixels(gap: GapSize | number, baseGap: number): number {
  if (typeof gap === 'number') {
    return gap;
  }

  switch (gap) {
    case 'tight':
      return Math.round(baseGap * 0.75);
    case 'wide':
      return Math.round(baseGap * 1.5);
    case 'normal':
    default:
      return baseGap;
  }
}

/**
 * Fluid Grid Component
 *
 * Creates a responsive grid that automatically adjusts column count
 * based on available space and minimum item width constraints.
 *
 * Features:
 * - Automatic column calculation using CSS Grid auto-fit
 * - Respects minimum item width for readability
 * - Optional max column limit
 * - Density-aware gap sizing
 * - Equal height option for card grids
 *
 * @example
 * ```tsx
 * // Basic usage
 * <FluidGrid minItemWidth={300} gap="normal">
 *   <Card>Item 1</Card>
 *   <Card>Item 2</Card>
 *   <Card>Item 3</Card>
 * </FluidGrid>
 *
 * // With max columns
 * <FluidGrid minItemWidth={280} maxColumns={3} gap="wide">
 *   {items.map(item => <Card key={item.id}>{item.name}</Card>)}
 * </FluidGrid>
 * ```
 */
export function FluidGrid({
  children,
  minItemWidth = 300,
  maxColumns = 4,
  gap = 'normal',
  equalHeight = true,
  className,
  style,
  role,
  'aria-label': ariaLabel,
}: FluidGridProps): React.ReactElement {
  const styles = useStyles();
  const layout = useAdaptiveLayoutContext();

  // Calculate gap in pixels
  const gapPixels = useMemo(() => getGapPixels(gap, layout.grid.gap), [gap, layout.grid.gap]);

  // Calculate the effective minimum width based on viewport
  const effectiveMinWidth = useMemo(() => {
    const { display } = layout;

    // On compact displays, allow items to be smaller
    if (display.sizeClass === 'compact') {
      return Math.min(minItemWidth, display.viewportWidth - 32);
    }

    return minItemWidth;
  }, [minItemWidth, layout]);

  // Determine if we should use fixed columns or auto-fit
  const useFixedColumns = useMemo(() => {
    const { display } = layout;
    // Use fixed columns on compact displays for predictability
    return display.sizeClass === 'compact';
  }, [layout]);

  // Calculate fixed column count when needed
  const fixedColumns = useMemo(() => {
    const { display } = layout;
    const availableWidth = display.viewportWidth - 32; // Account for padding
    const calculatedColumns = Math.floor(availableWidth / effectiveMinWidth);
    return Math.max(1, Math.min(calculatedColumns, maxColumns));
  }, [layout, effectiveMinWidth, maxColumns]);

  // Build grid template columns style
  const gridTemplateColumns = useMemo(() => {
    if (useFixedColumns) {
      return `repeat(${fixedColumns}, 1fr)`;
    }

    // Use auto-fit with minmax for fluid behavior
    // The max is 1fr to ensure equal column widths when possible
    return `repeat(auto-fit, minmax(min(${effectiveMinWidth}px, 100%), 1fr))`;
  }, [useFixedColumns, fixedColumns, effectiveMinWidth]);

  const gridClasses = [styles.grid, equalHeight ? styles.gridEqualHeight : '', className || '']
    .filter(Boolean)
    .join(' ');

  return (
    <div
      className={gridClasses}
      style={{
        gridTemplateColumns,
        gap: `${gapPixels}px`,
        ...style,
      }}
      role={role}
      aria-label={ariaLabel}
    >
      {children}
    </div>
  );
}

export default FluidGrid;
