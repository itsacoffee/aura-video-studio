/**
 * Auto Grid Component
 *
 * Simple CSS Grid wrapper that automatically calculates column count
 * based on minimum item width using CSS auto-fit/auto-fill.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { type CSSProperties, type ReactNode } from 'react';

/**
 * Gap size presets
 */
export type AutoGridGap = 'tight' | 'normal' | 'wide';

/**
 * Props for AutoGrid component
 */
export interface AutoGridProps {
  /** Grid items */
  children: ReactNode;
  /** Minimum width for each item in pixels (default: 200) */
  minItemWidth?: number;
  /** Maximum width for each item in pixels (default: 1fr) */
  maxItemWidth?: number | 'flexible';
  /** Gap between items */
  gap?: AutoGridGap;
  /** Whether all items should have equal height */
  equalHeight?: boolean;
  /** Additional class name */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Test ID for testing */
  'data-testid'?: string;
}

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
  equalHeight: {
    '& > *': {
      height: '100%',
    },
  },
});

/**
 * Auto Grid Component
 *
 * A simple CSS Grid wrapper that automatically calculates
 * column count using CSS auto-fit with minmax.
 *
 * Features:
 * - Pure CSS responsive behavior (no JavaScript resize handling)
 * - Configurable min/max item widths
 * - Gap presets for consistent spacing
 * - Equal height option for card grids
 *
 * @example
 * ```tsx
 * // Basic usage
 * <AutoGrid minItemWidth={250} gap="normal">
 *   <Card>Item 1</Card>
 *   <Card>Item 2</Card>
 *   <Card>Item 3</Card>
 * </AutoGrid>
 *
 * // Fixed max width items
 * <AutoGrid minItemWidth={200} maxItemWidth={300}>
 *   {items.map(item => <Card key={item.id}>{item.name}</Card>)}
 * </AutoGrid>
 * ```
 */
export function AutoGrid({
  children,
  minItemWidth = 200,
  maxItemWidth = 'flexible',
  gap = 'normal',
  equalHeight = false,
  className,
  style,
  'data-testid': testId,
}: AutoGridProps): React.ReactElement {
  const styles = useStyles();

  const maxWidthValue = maxItemWidth === 'flexible' ? '1fr' : `${maxItemWidth}px`;

  const gridStyle: CSSProperties = {
    ...style,
    gridTemplateColumns: `repeat(auto-fit, minmax(min(${minItemWidth}px, 100%), ${maxWidthValue}))`,
  };

  const gapClass =
    gap === 'tight' ? styles.gapTight : gap === 'wide' ? styles.gapWide : styles.gapNormal;

  return (
    <div
      className={mergeClasses(styles.grid, gapClass, equalHeight && styles.equalHeight, className)}
      style={gridStyle}
      data-testid={testId}
    >
      {children}
    </div>
  );
}

export default AutoGrid;
