/**
 * Cluster Component
 *
 * A layout primitive for horizontal spacing with wrapping.
 * Uses CSS flexbox with gap for consistent spacing.
 */

import type { CSSProperties, ReactNode } from 'react';

/**
 * Cluster spacing sizes
 */
export type ClusterSpace = 'xs' | 'sm' | 'md' | 'lg';

/**
 * Cluster alignment options
 */
export type ClusterAlign = 'start' | 'center' | 'end' | 'baseline' | 'stretch';

/**
 * Cluster justify options
 */
export type ClusterJustify = 'start' | 'center' | 'end' | 'space-between' | 'space-around';

/**
 * Cluster component props
 */
export interface ClusterProps {
  /** Child elements to arrange horizontally */
  children: ReactNode;
  /** Spacing between children */
  space?: ClusterSpace;
  /** Custom spacing value (CSS value) */
  customSpace?: string;
  /** Vertical alignment of children */
  align?: ClusterAlign;
  /** Horizontal justification of children */
  justify?: ClusterJustify;
  /** HTML element to render as */
  as?: 'div' | 'section' | 'nav' | 'ul' | 'ol';
  /** Additional class names */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Test ID for testing */
  'data-testid'?: string;
}

/**
 * Map of spacing sizes to CSS class names
 */
const spaceClassMap: Record<ClusterSpace, string> = {
  xs: 'cluster-xs',
  sm: 'cluster-sm',
  md: 'cluster-md',
  lg: 'cluster-lg',
};

/**
 * Map of alignment values to CSS values
 */
const alignMap: Record<ClusterAlign, string> = {
  start: 'flex-start',
  center: 'center',
  end: 'flex-end',
  baseline: 'baseline',
  stretch: 'stretch',
};

/**
 * Map of justify values to CSS values
 */
const justifyMap: Record<ClusterJustify, string> = {
  start: 'flex-start',
  center: 'center',
  end: 'flex-end',
  'space-between': 'space-between',
  'space-around': 'space-around',
};

/**
 * Cluster Component
 *
 * Creates horizontal spacing between child elements with automatic
 * wrapping when space is constrained.
 *
 * @example
 * ```tsx
 * <Cluster space="sm">
 *   <Tag>React</Tag>
 *   <Tag>TypeScript</Tag>
 *   <Tag>CSS</Tag>
 * </Cluster>
 * ```
 *
 * @example
 * ```tsx
 * // With custom alignment
 * <Cluster space="md" align="baseline" justify="space-between">
 *   <Logo />
 *   <Nav />
 * </Cluster>
 * ```
 */
export function Cluster({
  children,
  space = 'md',
  customSpace,
  align = 'center',
  justify = 'start',
  as: Component = 'div',
  className = '',
  style,
  'data-testid': testId,
}: ClusterProps) {
  const spaceClass = customSpace ? '' : spaceClassMap[space];
  const combinedClassName = `cluster ${spaceClass} ${className}`.trim();

  const combinedStyle: CSSProperties = {
    ...(customSpace ? { '--cluster-space': customSpace } : {}),
    ...(align !== 'center' ? { alignItems: alignMap[align] } : {}),
    ...(justify !== 'start' ? { justifyContent: justifyMap[justify] } : {}),
    ...style,
  } as CSSProperties;

  return (
    <Component className={combinedClassName} style={combinedStyle} data-testid={testId}>
      {children}
    </Component>
  );
}
