/**
 * Adaptive Container Component
 *
 * A responsive container that constrains content width based on
 * display environment while maintaining consistent padding.
 */

import { makeStyles } from '@fluentui/react-components';
import React, { type ReactNode, type CSSProperties, type ElementType } from 'react';
import { useAdaptiveLayoutContext } from '../../contexts/AdaptiveLayoutContext';

const useStyles = makeStyles({
  container: {
    width: '100%',
    marginLeft: 'auto',
    marginRight: 'auto',
    boxSizing: 'border-box',
  },
});

/**
 * Container size presets
 */
export type ContainerSize = 'narrow' | 'default' | 'wide' | 'full';

/**
 * Props for AdaptiveContainer
 */
export interface AdaptiveContainerProps {
  /** Container content */
  children: ReactNode;
  /** Container size preset */
  size?: ContainerSize;
  /** Custom maximum width in pixels (overrides size preset) */
  maxWidth?: number;
  /** Whether to add horizontal padding */
  padded?: boolean;
  /** Whether to center the container */
  centered?: boolean;
  /** Additional class name */
  className?: string;
  /** Additional inline styles */
  style?: CSSProperties;
  /** HTML element to render as */
  as?: ElementType;
  /** Accessibility role */
  role?: string;
  /** Aria label */
  'aria-label'?: string;
}

/**
 * Get max width for size preset
 */
function getMaxWidthForSize(size: ContainerSize, layoutMaxWidth: number | 'full'): number | 'full' {
  switch (size) {
    case 'narrow':
      return 800;
    case 'wide':
      return typeof layoutMaxWidth === 'number' ? Math.min(2000, layoutMaxWidth * 1.2) : 2000;
    case 'full':
      return 'full';
    case 'default':
    default:
      return layoutMaxWidth;
  }
}

/**
 * Adaptive Container
 *
 * Constrains content to appropriate width based on display size,
 * following Apple's design philosophy of readable content widths.
 *
 * Features:
 * - Responsive max-width based on display environment
 * - Size presets for common use cases
 * - Density-aware padding
 * - Can render as any HTML element
 *
 * @example
 * ```tsx
 * // Default container
 * <AdaptiveContainer>
 *   <Content />
 * </AdaptiveContainer>
 *
 * // Narrow container for forms
 * <AdaptiveContainer size="narrow" padded>
 *   <Form />
 * </AdaptiveContainer>
 *
 * // Full-width container
 * <AdaptiveContainer size="full">
 *   <FullWidthContent />
 * </AdaptiveContainer>
 *
 * // As a section element
 * <AdaptiveContainer as="section" role="region" aria-label="Main content">
 *   <Article />
 * </AdaptiveContainer>
 * ```
 */
export function AdaptiveContainer({
  children,
  size = 'default',
  maxWidth: customMaxWidth,
  padded = true,
  centered = true,
  className,
  style,
  as: Component = 'div',
  role,
  'aria-label': ariaLabel,
}: AdaptiveContainerProps): React.ReactElement {
  const styles = useStyles();
  const layout = useAdaptiveLayoutContext();

  // Determine max width
  const maxWidthValue =
    customMaxWidth !== undefined
      ? customMaxWidth
      : getMaxWidthForSize(size, layout.content.maxWidth);

  const maxWidthStyle = maxWidthValue === 'full' ? '100%' : `${maxWidthValue}px`;

  // Calculate padding based on density
  const paddingValue = padded ? layout.content.padding : 0;

  const containerStyle: CSSProperties = {
    maxWidth: maxWidthStyle,
    paddingLeft: `${paddingValue}px`,
    paddingRight: `${paddingValue}px`,
    marginLeft: centered ? 'auto' : undefined,
    marginRight: centered ? 'auto' : undefined,
    ...style,
  };

  return (
    <Component
      className={`${styles.container} ${className || ''}`}
      style={containerStyle}
      role={role}
      aria-label={ariaLabel}
    >
      {children}
    </Component>
  );
}

export default AdaptiveContainer;
