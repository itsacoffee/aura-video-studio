/**
 * Heading Component
 *
 * A fluid typography Heading component that provides consistent heading styling
 * based on the modular scale typography system.
 *
 * Features:
 * - Semantic heading levels (h1-h4, display)
 * - Automatic HTML element mapping
 * - Color variants for text hierarchy
 * - Weight customization
 * - Truncation support
 *
 * @example
 * ```tsx
 * <Heading level="h1">Page Title</Heading>
 * <Heading level="h2" color="secondary">Section Heading</Heading>
 * <Heading level="display">Hero Text</Heading>
 * ```
 */

import { makeStyles, mergeClasses, shorthands } from '@fluentui/react-components';
import { forwardRef } from 'react';
import type { ReactNode, ElementType, ComponentPropsWithoutRef } from 'react';

/**
 * Heading level types - mapped to CSS classes from typography-system.css
 */
export type HeadingLevel = 'display' | 'h1' | 'h2' | 'h3' | 'h4';

/**
 * Heading color types - semantic color options
 */
export type HeadingColor = 'primary' | 'secondary' | 'tertiary' | 'accent';

/**
 * Font weight options
 */
export type HeadingWeight = 'medium' | 'semibold' | 'bold';

/**
 * Text alignment options
 */
export type HeadingAlign = 'left' | 'center' | 'right';

/**
 * Default element mapping for each heading level
 */
const levelToElement: Record<HeadingLevel, ElementType> = {
  display: 'h1',
  h1: 'h1',
  h2: 'h2',
  h3: 'h3',
  h4: 'h4',
};

/**
 * Props for the Heading component
 */
export interface HeadingProps {
  /** Heading level - controls size, weight, and line height */
  level?: HeadingLevel;
  /** Heading color semantic variant */
  color?: HeadingColor;
  /** Override font weight */
  weight?: HeadingWeight;
  /** Text alignment */
  align?: HeadingAlign;
  /** Enable truncation for single line */
  truncate?: boolean;
  /** Override the default HTML element */
  as?: ElementType;
  /** Additional CSS class names */
  className?: string;
  /** Content */
  children: ReactNode;
}

/**
 * Fluent UI makeStyles for heading styling
 */
const useStyles = makeStyles({
  base: {
    margin: 0,
    ...shorthands.padding(0),
  },
  // Levels
  display: {
    fontSize: 'var(--type-4xl)',
    fontWeight: 'var(--weight-bold)',
    lineHeight: 'var(--leading-tight)',
    letterSpacing: 'var(--tracking-tight)',
  },
  h1: {
    fontSize: 'var(--type-3xl)',
    fontWeight: 'var(--weight-semibold)',
    lineHeight: 'var(--leading-tight)',
    letterSpacing: 'var(--tracking-tight)',
  },
  h2: {
    fontSize: 'var(--type-2xl)',
    fontWeight: 'var(--weight-semibold)',
    lineHeight: 'var(--leading-snug)',
    letterSpacing: 'var(--tracking-tight)',
  },
  h3: {
    fontSize: 'var(--type-xl)',
    fontWeight: 'var(--weight-medium)',
    lineHeight: 'var(--leading-snug)',
    letterSpacing: 'var(--tracking-normal)',
  },
  h4: {
    fontSize: 'var(--type-lg)',
    fontWeight: 'var(--weight-medium)',
    lineHeight: 'var(--leading-snug)',
    letterSpacing: 'var(--tracking-normal)',
  },
  // Colors
  'color-primary': {
    color: 'var(--text-primary)',
  },
  'color-secondary': {
    color: 'var(--text-secondary)',
  },
  'color-tertiary': {
    color: 'var(--text-tertiary)',
  },
  'color-accent': {
    color: 'var(--color-primary-500)',
  },
  // Weights
  'weight-medium': {
    fontWeight: 'var(--weight-medium)',
  },
  'weight-semibold': {
    fontWeight: 'var(--weight-semibold)',
  },
  'weight-bold': {
    fontWeight: 'var(--weight-bold)',
  },
  // Alignment
  'align-left': {
    textAlign: 'left',
  },
  'align-center': {
    textAlign: 'center',
  },
  'align-right': {
    textAlign: 'right',
  },
  // Truncation
  truncate: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
});

/**
 * Heading component - fluid typography for page and section headings
 *
 * Uses the modular scale typography system for consistent sizing across viewports.
 */
export const Heading = forwardRef<
  HTMLHeadingElement,
  HeadingProps & ComponentPropsWithoutRef<'h1'>
>(
  (
    { level = 'h1', color = 'primary', weight, align, truncate, as, className, children, ...rest },
    ref
  ) => {
    const styles = useStyles();

    const Element = as || levelToElement[level];

    const classes = mergeClasses(
      styles.base,
      styles[level],
      styles[`color-${color}` as keyof typeof styles],
      weight && styles[`weight-${weight}` as keyof typeof styles],
      align && styles[`align-${align}` as keyof typeof styles],
      truncate && styles.truncate,
      className
    );

    return (
      <Element ref={ref} className={classes} {...rest}>
        {children}
      </Element>
    );
  }
);

Heading.displayName = 'Heading';

export default Heading;
