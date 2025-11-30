/**
 * Smart Grid Component
 *
 * Content-aware grid that calculates optimal columns based on
 * item type, aspect ratio, and available space for Apple-level layouts.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { useMemo, useRef, useEffect, useState, type CSSProperties, type ReactNode } from 'react';
import { useDisplayEnvironment } from '../../hooks/useDisplayEnvironment';

/**
 * Item type presets with default sizing constraints
 */
export type SmartGridItemType = 'card' | 'thumbnail' | 'tile' | 'list-item';

/**
 * Gap size presets
 */
export type SmartGridGap = 'tight' | 'normal' | 'wide';

/**
 * Fill mode for CSS Grid
 */
export type SmartGridFillMode = 'auto-fill' | 'auto-fit';

/**
 * Alignment options
 */
export type SmartGridAlignment = 'start' | 'center' | 'end' | 'stretch';

/**
 * Props for SmartGrid component
 */
export interface SmartGridProps {
  /** Grid items */
  children: ReactNode;

  /** Content type hint for default sizing */
  itemType?: SmartGridItemType;
  /** Custom aspect ratio (width/height) */
  itemAspectRatio?: number;

  /** Minimum columns constraint */
  minColumns?: number;
  /** Maximum columns constraint */
  maxColumns?: number;
  /** Minimum item width in pixels */
  minItemWidth?: number;
  /** Maximum item width in pixels */
  maxItemWidth?: number;

  /** Gap between items */
  gap?: SmartGridGap;

  /** CSS Grid fill mode */
  fillMode?: SmartGridFillMode;
  /** Align items within grid cells */
  alignItems?: SmartGridAlignment;
  /** Justify items within grid cells */
  justifyItems?: SmartGridAlignment;

  /** Additional class name */
  className?: string;
  /** Test ID for testing */
  'data-testid'?: string;
}

/**
 * Default sizing constraints by item type
 */
const itemTypeDefaults: Record<
  SmartGridItemType,
  { minWidth: number; maxWidth: number; aspect: number }
> = {
  card: { minWidth: 280, maxWidth: 400, aspect: 1.2 },
  thumbnail: { minWidth: 150, maxWidth: 250, aspect: 1 },
  tile: { minWidth: 200, maxWidth: 350, aspect: 0.75 },
  'list-item': { minWidth: 300, maxWidth: 600, aspect: 4 },
};

/**
 * Gap pixel values
 */
const gapValues: Record<SmartGridGap, number> = {
  tight: 8,
  normal: 16,
  wide: 24,
};

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    width: '100%',
  },
  gapTight: {
    gap: '8px',
  },
  gapNormal: {
    gap: '16px',
  },
  gapWide: {
    gap: '24px',
  },
});

/**
 * Smart Grid Component
 *
 * Creates a content-aware responsive grid that automatically calculates
 * optimal column count based on item type and available space.
 *
 * Features:
 * - Content-type defaults for cards, thumbnails, tiles, list items
 * - Automatic column calculation with min/max constraints
 * - Smooth layout transitions on resize
 * - Density-aware spacing via gap presets
 *
 * @example
 * ```tsx
 * // Card grid with automatic sizing
 * <SmartGrid itemType="card" gap="normal">
 *   <Card>Item 1</Card>
 *   <Card>Item 2</Card>
 * </SmartGrid>
 *
 * // Thumbnail grid with custom constraints
 * <SmartGrid
 *   itemType="thumbnail"
 *   minColumns={2}
 *   maxColumns={6}
 *   gap="tight"
 * >
 *   {thumbnails.map(t => <Thumbnail key={t.id} src={t.src} />)}
 * </SmartGrid>
 * ```
 */
export function SmartGrid({
  children,
  itemType = 'card',
  itemAspectRatio,
  minColumns = 1,
  maxColumns = 6,
  minItemWidth,
  maxItemWidth,
  gap = 'normal',
  fillMode = 'auto-fill',
  alignItems = 'stretch',
  justifyItems = 'stretch',
  className,
  'data-testid': testId,
}: SmartGridProps): React.ReactElement {
  const styles = useStyles();
  const gridRef = useRef<HTMLDivElement>(null);
  const display = useDisplayEnvironment();
  const [calculatedColumns, setCalculatedColumns] = useState(1);

  // Get defaults based on item type
  const defaults = itemTypeDefaults[itemType];
  const effectiveMinWidth = minItemWidth ?? defaults.minWidth;
  const effectiveMaxWidth = maxItemWidth ?? defaults.maxWidth;
  // Use custom aspect ratio if provided, otherwise use the default for the item type
  const effectiveAspectRatio = itemAspectRatio ?? defaults.aspect;

  // Calculate optimal column count based on container width
  useEffect(() => {
    if (!gridRef.current) return;

    const containerWidth = gridRef.current.offsetWidth;
    const gapSize = gapValues[gap];

    // Calculate how many columns fit
    let columns = Math.floor((containerWidth + gapSize) / (effectiveMinWidth + gapSize));
    columns = Math.max(minColumns, Math.min(maxColumns, columns));

    setCalculatedColumns(columns);
  }, [display.viewportWidth, effectiveMinWidth, minColumns, maxColumns, gap]);

  // Build grid style
  const gridStyle = useMemo((): CSSProperties => {
    const maxWidthValue = effectiveMaxWidth === Infinity ? '1fr' : `${effectiveMaxWidth}px`;

    return {
      gridTemplateColumns: `repeat(${fillMode}, minmax(${effectiveMinWidth}px, ${maxWidthValue}))`,
      alignItems,
      justifyItems,
      '--smart-grid-aspect-ratio': effectiveAspectRatio,
    } as CSSProperties;
  }, [
    fillMode,
    effectiveMinWidth,
    effectiveMaxWidth,
    alignItems,
    justifyItems,
    effectiveAspectRatio,
  ]);

  const gapClass =
    gap === 'tight' ? styles.gapTight : gap === 'wide' ? styles.gapWide : styles.gapNormal;

  return (
    <div
      ref={gridRef}
      className={mergeClasses(styles.grid, gapClass, className)}
      style={gridStyle}
      data-columns={calculatedColumns}
      data-testid={testId}
    >
      {children}
    </div>
  );
}

export default SmartGrid;
