/**
 * Masonry Grid Component
 *
 * Pinterest-style masonry layout that positions items in columns
 * with variable heights for Apple-level visual appeal.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import {
  useRef,
  useEffect,
  useState,
  Children,
  cloneElement,
  isValidElement,
  type ReactNode,
  type CSSProperties,
  type ReactElement,
} from 'react';
import { useDisplayEnvironment } from '../../hooks/useDisplayEnvironment';

/**
 * Position for masonry item
 */
interface MasonryPosition {
  x: number;
  y: number;
  width: number;
}

/**
 * Props for MasonryGrid component
 */
export interface MasonryGridProps {
  /** Grid items */
  children: ReactNode;
  /** Target column width in pixels */
  columnWidth?: number;
  /** Gap between items in pixels */
  gap?: number;
  /** Additional class name */
  className?: string;
  /** Test ID for testing */
  'data-testid'?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    width: '100%',
  },
  item: {
    position: 'absolute',
    transitionProperty: 'transform, opacity',
    transitionDuration: '0.3s',
    transitionTimingFunction: 'ease',
  },
});

/**
 * Masonry Grid Component
 *
 * Creates a Pinterest-style masonry layout that positions items
 * in columns with variable heights.
 *
 * Features:
 * - Automatic column calculation based on container width
 * - Items placed in shortest column for optimal packing
 * - Smooth animation on layout changes
 * - Responsive to viewport changes
 *
 * @example
 * ```tsx
 * <MasonryGrid columnWidth={300} gap={16}>
 *   <Card>Short content</Card>
 *   <Card>
 *     Much longer content that takes more vertical space
 *   </Card>
 *   <Card>Medium content here</Card>
 * </MasonryGrid>
 * ```
 */
export function MasonryGrid({
  children,
  columnWidth = 300,
  gap = 16,
  className,
  'data-testid': testId,
}: MasonryGridProps): React.ReactElement {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);
  const [positions, setPositions] = useState<MasonryPosition[]>([]);
  const [containerHeight, setContainerHeight] = useState(0);
  const display = useDisplayEnvironment();

  useEffect(() => {
    if (!containerRef.current) return;

    const containerWidth = containerRef.current.offsetWidth;
    const columnCount = Math.max(1, Math.floor((containerWidth + gap) / (columnWidth + gap)));
    const actualColumnWidth = (containerWidth - gap * (columnCount - 1)) / columnCount;

    // Track height of each column
    const columnHeights = new Array<number>(columnCount).fill(0);
    const itemElements = containerRef.current.querySelectorAll('[data-masonry-item]');
    const newPositions: MasonryPosition[] = [];

    itemElements.forEach((item) => {
      // Find shortest column
      const shortestColumnIndex = columnHeights.indexOf(Math.min(...columnHeights));
      const x = shortestColumnIndex * (actualColumnWidth + gap);
      const y = columnHeights[shortestColumnIndex];

      newPositions.push({ x, y, width: actualColumnWidth });

      // Update column height
      const itemHeight = (item as HTMLElement).offsetHeight;
      columnHeights[shortestColumnIndex] += itemHeight + gap;
    });

    setPositions(newPositions);
    const maxHeight = Math.max(...columnHeights);
    setContainerHeight(maxHeight > 0 ? maxHeight - gap : 0);
  }, [children, columnWidth, gap, display.viewportWidth]);

  const childArray = Children.toArray(children);

  return (
    <div
      ref={containerRef}
      className={mergeClasses(styles.container, className)}
      style={{ height: containerHeight }}
      data-testid={testId}
    >
      {childArray.map((child, index) => {
        if (!isValidElement(child)) return null;

        const position = positions[index] ?? { x: 0, y: 0, width: columnWidth };

        const childProps = child.props as Record<string, unknown>;
        const existingClassName =
          typeof childProps.className === 'string' ? childProps.className : '';
        const existingStyle = (childProps.style as CSSProperties) ?? {};

        // Preserve the child's key if it exists, otherwise fall back to index
        const key = child.key !== null ? child.key : index;

        return cloneElement(child as ReactElement, {
          key,
          'data-masonry-item': true,
          className: mergeClasses(styles.item, existingClassName),
          style: {
            ...existingStyle,
            transform: `translate(${position.x}px, ${position.y}px)`,
            width: `${position.width}px`,
          },
        });
      })}
    </div>
  );
}

export default MasonryGrid;
