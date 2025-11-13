import React, { ComponentType, memo } from 'react';
import { shallowCompareIgnoringFunctions } from '@/utils/performanceUtils';

/**
 * Higher-order component that wraps a component with React.memo and custom comparison
 * Use this for expensive components that render frequently
 */
export function withOptimizedRendering<P extends object>(
  Component: ComponentType<P>,
  customCompare?: (prevProps: Readonly<P>, nextProps: Readonly<P>) => boolean
): ComponentType<P> {
  const MemoizedComponent = memo(Component, customCompare);
  MemoizedComponent.displayName = `Optimized(${Component.displayName || Component.name || 'Component'})`;
  return MemoizedComponent;
}

/**
 * Performance-optimized list item component
 * Use this as a wrapper for items in large lists
 */
interface OptimizedListItemProps {
  id: string | number;
  children: React.ReactNode;
}

export const OptimizedListItem: React.FC<OptimizedListItemProps> = memo(
  (props) => {
    return <>{props.children}</>;
  },
  (prev, next) => prev.id === next.id && prev.children === next.children
);

OptimizedListItem.displayName = 'OptimizedListItem';

/**
 * HOC for components that should only update when specific props change
 */
export function withSelectiveUpdates<P extends object>(
  Component: ComponentType<P>,
  propsToWatch: Array<keyof P>
): ComponentType<P> {
  return withOptimizedRendering(Component, (prevProps, nextProps) => {
    return propsToWatch.every((prop) => prevProps[prop] === nextProps[prop]);
  });
}

export { shallowCompareIgnoringFunctions };
