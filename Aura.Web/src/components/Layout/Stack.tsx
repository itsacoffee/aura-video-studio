/**
 * Stack Component
 *
 * A layout primitive for vertical spacing between children.
 * Uses the CSS stack pattern with margin-top on sibling elements.
 */

import type { CSSProperties, ReactNode } from 'react';

/**
 * Stack spacing sizes
 */
export type StackSpace = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Stack component props
 */
export interface StackProps {
  /** Child elements to stack vertically */
  children: ReactNode;
  /** Spacing between children */
  space?: StackSpace;
  /** Custom spacing value (CSS value) */
  customSpace?: string;
  /** HTML element to render as */
  as?: 'div' | 'section' | 'article' | 'aside' | 'main' | 'nav' | 'header' | 'footer';
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
const spaceClassMap: Record<StackSpace, string> = {
  xs: 'stack-xs',
  sm: 'stack-sm',
  md: 'stack-md',
  lg: 'stack-lg',
  xl: 'stack-xl',
};

/**
 * Stack Component
 *
 * Creates vertical spacing between child elements using the CSS
 * lobotomized owl selector pattern (> * + *).
 *
 * @example
 * ```tsx
 * <Stack space="md">
 *   <Card>First</Card>
 *   <Card>Second</Card>
 *   <Card>Third</Card>
 * </Stack>
 * ```
 *
 * @example
 * ```tsx
 * // With custom spacing
 * <Stack customSpace="var(--space-6)">
 *   <Section>Content</Section>
 * </Stack>
 * ```
 */
export function Stack({
  children,
  space = 'md',
  customSpace,
  as: Component = 'div',
  className = '',
  style,
  'data-testid': testId,
}: StackProps) {
  const spaceClass = customSpace ? '' : spaceClassMap[space];
  const combinedClassName = `stack ${spaceClass} ${className}`.trim();

  const combinedStyle: CSSProperties = customSpace
    ? ({ ...style, '--stack-space': customSpace } as CSSProperties)
    : style || {};

  return (
    <Component className={combinedClassName} style={combinedStyle} data-testid={testId}>
      {children}
    </Component>
  );
}
