/**
 * useSpacing Hook
 *
 * Provides access to the proportional spacing system values.
 * Returns CSS custom property references that automatically scale
 * with the current density mode.
 */

import { useMemo } from 'react';
import { useDensity } from '../contexts/DensityContext';

/**
 * Spacing scale type with numeric and semantic values
 */
export interface SpacingScale {
  0: string;
  px: string;
  0.5: string;
  1: string;
  1.5: string;
  2: string;
  3: string;
  4: string;
  5: string;
  6: string;
  8: string;
  10: string;
  12: string;
  16: string;
  20: string;
  24: string;
}

/**
 * Semantic spacing values
 */
export interface SemanticSpacing {
  xs: string;
  sm: string;
  md: string;
  lg: string;
  xl: string;
}

/**
 * Component-specific spacing values
 */
export interface ComponentSpacing {
  padding: string;
  gap: string;
}

/**
 * Page spacing values
 */
export interface PageSpacing {
  padding: string;
  paddingX: string;
}

/**
 * Section spacing values
 */
export interface SectionSpacing {
  gap: string;
}

/**
 * Spacing utilities returned by useSpacing
 */
export interface SpacingUtilities {
  /** Base spacing unit reference */
  unit: string;
  /** Get computed spacing value for a multiplier */
  getSpacing: (multiplier: number) => string;
  /** Semantic inline (horizontal) spacing values */
  inline: SemanticSpacing;
  /** Semantic stack (vertical) spacing values */
  stack: SemanticSpacing;
  /** Card component spacing */
  card: ComponentSpacing;
  /** Page layout spacing */
  page: PageSpacing;
  /** Section spacing */
  section: SectionSpacing;
  /** Numeric spacing scale for makeStyles */
  scale: SpacingScale;
}

/**
 * Hook for accessing proportional spacing system values
 *
 * Provides CSS custom property references that automatically scale
 * with the current density mode set via DensityContext.
 *
 * @returns SpacingUtilities object with all spacing values
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const spacing = useSpacing();
 *
 *   return (
 *     <div style={{
 *       padding: spacing.card.padding,
 *       gap: spacing.inline.md,
 *       marginBottom: spacing.scale[4]
 *     }}>
 *       Content
 *     </div>
 *   );
 * }
 * ```
 */
export function useSpacing(): SpacingUtilities {
  const { spacingScale } = useDensity();

  return useMemo(
    () => ({
      // Base scale (CSS custom property references)
      unit: 'var(--space-unit)',

      // Computed values for JavaScript usage
      getSpacing: (multiplier: number) => `calc(var(--space-unit) * ${multiplier})`,

      // Semantic spacing
      inline: {
        xs: 'var(--space-inline-xs)',
        sm: 'var(--space-inline-sm)',
        md: 'var(--space-inline-md)',
        lg: 'var(--space-inline-lg)',
        xl: 'var(--space-inline-xl)',
      },

      stack: {
        xs: 'var(--space-stack-xs)',
        sm: 'var(--space-stack-sm)',
        md: 'var(--space-stack-md)',
        lg: 'var(--space-stack-lg)',
        xl: 'var(--space-stack-xl)',
      },

      // Component spacing
      card: {
        padding: 'var(--space-card-padding)',
        gap: 'var(--space-card-gap)',
      },

      page: {
        padding: 'var(--space-page-padding)',
        paddingX: 'var(--space-page-padding-x)',
      },

      section: {
        gap: 'var(--space-section-gap)',
      },

      // Numeric scale for makeStyles
      scale: {
        0: '0',
        px: '1px',
        0.5: 'var(--space-0-5)',
        1: 'var(--space-1)',
        1.5: 'var(--space-1-5)',
        2: 'var(--space-2)',
        3: 'var(--space-3)',
        4: 'var(--space-4)',
        5: 'var(--space-5)',
        6: 'var(--space-6)',
        8: 'var(--space-8)',
        10: 'var(--space-10)',
        12: 'var(--space-12)',
        16: 'var(--space-16)',
        20: 'var(--space-20)',
        24: 'var(--space-24)',
      },
    }),
    [spacingScale]
  );
}
