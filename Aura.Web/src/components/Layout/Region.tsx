/**
 * Region Component
 *
 * A layout primitive for sections with consistent vertical rhythm.
 * Provides vertical padding to create distinct content regions.
 */

import type { CSSProperties, ReactNode } from 'react';

/**
 * Region spacing sizes
 */
export type RegionSpace = 'sm' | 'md' | 'lg' | 'xl';

/**
 * Region component props
 */
export interface RegionProps {
  /** Child elements within the region */
  children: ReactNode;
  /** Vertical padding size */
  space?: RegionSpace;
  /** Custom spacing value (CSS value) */
  customSpace?: string;
  /** HTML element to render as */
  as?: 'div' | 'section' | 'article' | 'aside' | 'main';
  /** Additional class names */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** Test ID for testing */
  'data-testid'?: string;
  /** Aria label by (for accessibility) */
  'aria-labelledby'?: string;
}

/**
 * Map of spacing sizes to CSS class names
 */
const spaceClassMap: Record<RegionSpace, string> = {
  sm: 'region-sm',
  md: '',
  lg: 'region-lg',
  xl: 'region-xl',
};

/**
 * Region Component
 *
 * Creates a distinct content region with consistent vertical padding.
 * Useful for separating major sections of a page.
 *
 * @example
 * ```tsx
 * <Region space="lg">
 *   <h2>Features</h2>
 *   <FeatureGrid />
 * </Region>
 * ```
 *
 * @example
 * ```tsx
 * // As a semantic section
 * <Region as="section" space="xl" aria-labelledby="about-heading">
 *   <h2 id="about-heading">About Us</h2>
 *   <p>Content...</p>
 * </Region>
 * ```
 */
export function Region({
  children,
  space = 'md',
  customSpace,
  as: Component = 'div',
  className = '',
  style,
  'data-testid': testId,
  'aria-labelledby': ariaLabelledBy,
}: RegionProps) {
  const spaceClass = customSpace ? undefined : spaceClassMap[space];
  const combinedClassName = ['region', spaceClass, className].filter(Boolean).join(' ');

  const combinedStyle: CSSProperties = customSpace
    ? ({ ...style, '--region-space': customSpace } as CSSProperties)
    : style || {};

  return (
    <Component
      className={combinedClassName}
      style={combinedStyle}
      data-testid={testId}
      aria-labelledby={ariaLabelledBy}
    >
      {children}
    </Component>
  );
}
