/**
 * Text Component
 *
 * A fluid typography Text component that provides consistent text styling
 * based on the modular scale typography system.
 *
 * Features:
 * - Semantic variant mapping (body, caption, label, etc.)
 * - Color variants for text hierarchy
 * - Weight and alignment customization
 * - Truncation support (single line or line clamp)
 * - Polymorphic "as" prop for semantic HTML elements
 *
 * @example
 * ```tsx
 * <Text variant="body" color="primary">Primary body text</Text>
 * <Text variant="caption" color="secondary">Secondary caption</Text>
 * <Text variant="label" weight="medium">Form label</Text>
 * ```
 */

import { makeStyles, mergeClasses, shorthands } from '@fluentui/react-components';
import { forwardRef } from 'react';
import type { ReactNode, ElementType, ComponentPropsWithoutRef } from 'react';

/**
 * Text variant types - mapped to CSS classes from typography-system.css
 */
export type TextVariant = 'body' | 'body-sm' | 'caption' | 'label' | 'overline' | 'code';

/**
 * Text color types - semantic color options
 */
export type TextColor =
  | 'primary'
  | 'secondary'
  | 'tertiary'
  | 'accent'
  | 'success'
  | 'warning'
  | 'error';

/**
 * Font weight options
 */
export type TextWeight = 'light' | 'regular' | 'medium' | 'semibold' | 'bold';

/**
 * Text alignment options
 */
export type TextAlign = 'left' | 'center' | 'right';

/**
 * Default element mapping for each text variant
 */
const variantToElement: Record<TextVariant, ElementType> = {
  body: 'p',
  'body-sm': 'p',
  caption: 'span',
  label: 'label',
  overline: 'span',
  code: 'code',
};

/**
 * Props for the Text component
 */
export interface TextProps {
  /** Typography variant - controls size, weight, and line height */
  variant?: TextVariant;
  /** Text color semantic variant */
  color?: TextColor;
  /** Override font weight */
  weight?: TextWeight;
  /** Text alignment */
  align?: TextAlign;
  /** Enable truncation - true for single line, or number for line clamp */
  truncate?: boolean | 2 | 3;
  /** Override the default HTML element */
  as?: ElementType;
  /** Additional CSS class names */
  className?: string;
  /** Content */
  children: ReactNode;
}

/**
 * Fluent UI makeStyles for text styling
 */
const useStyles = makeStyles({
  base: {
    margin: 0,
    ...shorthands.padding(0),
  },
  // Variants
  body: {
    fontSize: 'var(--type-md)',
    fontWeight: 'var(--weight-regular)',
    lineHeight: 'var(--leading-normal)',
    letterSpacing: 'var(--tracking-normal)',
  },
  'body-sm': {
    fontSize: 'var(--type-sm)',
    fontWeight: 'var(--weight-regular)',
    lineHeight: 'var(--leading-normal)',
    letterSpacing: 'var(--tracking-normal)',
  },
  caption: {
    fontSize: 'var(--type-xs)',
    fontWeight: 'var(--weight-regular)',
    lineHeight: 'var(--leading-normal)',
    letterSpacing: 'var(--tracking-wide)',
  },
  label: {
    fontSize: 'var(--type-sm)',
    fontWeight: 'var(--weight-medium)',
    lineHeight: 'var(--leading-snug)',
    letterSpacing: 'var(--tracking-wide)',
    textTransform: 'none',
  },
  overline: {
    fontSize: 'var(--type-xs)',
    fontWeight: 'var(--weight-semibold)',
    lineHeight: 'var(--leading-snug)',
    letterSpacing: 'var(--tracking-wider)',
    textTransform: 'uppercase',
  },
  code: {
    fontFamily: "'SF Mono', 'Fira Code', 'Consolas', monospace",
    fontSize: 'calc(var(--type-md) * 0.9)',
    lineHeight: 'var(--leading-relaxed)',
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
  'color-success': {
    color: 'var(--color-success)',
  },
  'color-warning': {
    color: 'var(--color-warning)',
  },
  'color-error': {
    color: 'var(--color-error)',
  },
  // Weights
  'weight-light': {
    fontWeight: 'var(--weight-light)',
  },
  'weight-regular': {
    fontWeight: 'var(--weight-regular)',
  },
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
  lineClamp2: {
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
  lineClamp3: {
    display: '-webkit-box',
    WebkitLineClamp: 3,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
});

/**
 * Text component - fluid typography for body text, captions, labels, etc.
 *
 * Uses the modular scale typography system for consistent sizing across viewports.
 */
export const Text = forwardRef<HTMLElement, TextProps & ComponentPropsWithoutRef<'span'>>(
  (
    {
      variant = 'body',
      color = 'primary',
      weight,
      align,
      truncate,
      as,
      className,
      children,
      ...rest
    },
    ref
  ) => {
    const styles = useStyles();

    const Element = as || variantToElement[variant];

    const classes = mergeClasses(
      styles.base,
      styles[variant],
      styles[`color-${color}` as keyof typeof styles],
      weight && styles[`weight-${weight}` as keyof typeof styles],
      align && styles[`align-${align}` as keyof typeof styles],
      truncate === true && styles.truncate,
      truncate === 2 && styles.lineClamp2,
      truncate === 3 && styles.lineClamp3,
      className
    );

    return (
      <Element ref={ref} className={classes} {...rest}>
        {children}
      </Element>
    );
  }
);

Text.displayName = 'Text';

export default Text;
