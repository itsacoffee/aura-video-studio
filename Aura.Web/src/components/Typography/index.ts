/**
 * Typography Components Barrel Export
 *
 * Central export point for all typography-related components.
 * These components implement the fluid typography system based on
 * modular scale with viewport-responsive interpolation.
 *
 * @example
 * ```tsx
 * import { Text, Heading } from '@/components/Typography';
 *
 * <Heading level="h1">Page Title</Heading>
 * <Text variant="body">Body content</Text>
 * <Text variant="caption" color="secondary">Caption text</Text>
 * ```
 */

// Text component for body text, captions, labels, etc.
export {
  Text,
  type TextProps,
  type TextVariant,
  type TextColor,
  type TextWeight,
  type TextAlign,
} from './Text';

// Heading component for page and section headings
export {
  Heading,
  type HeadingProps,
  type HeadingLevel,
  type HeadingColor,
  type HeadingWeight,
  type HeadingAlign,
} from './Heading';
