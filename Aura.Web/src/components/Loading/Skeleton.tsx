import { motion } from 'framer-motion';
import { CSSProperties } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  borderRadius?: string | number;
  className?: string;
  variant?: 'text' | 'circular' | 'rectangular' | 'rounded';
}

/**
 * Skeleton loading component with shimmer animation
 * Perfect for loading placeholders
 */
export function Skeleton({
  width = '100%',
  height = '1rem',
  borderRadius,
  className = '',
  variant = 'text',
}: SkeletonProps) {
  const prefersReducedMotion = useReducedMotion();

  const getVariantStyles = (): CSSProperties => {
    switch (variant) {
      case 'circular':
        return {
          borderRadius: '50%',
          width: typeof width === 'number' ? `${width}px` : width,
          height: typeof width === 'number' ? `${width}px` : width,
        };
      case 'rounded':
        return {
          borderRadius: '0.5rem',
        };
      case 'text':
        return {
          borderRadius: '0.25rem',
        };
      case 'rectangular':
      default:
        return {
          borderRadius: borderRadius || '0.125rem',
        };
    }
  };

  const styles: CSSProperties = {
    width: variant === 'circular' ? width : width,
    height: variant === 'circular' ? width : height,
    display: 'inline-block',
    backgroundColor: 'var(--color-surface)',
    ...getVariantStyles(),
  };

  const shimmerAnimation = prefersReducedMotion
    ? {}
    : {
        backgroundImage:
          'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.1), transparent)',
        backgroundSize: '200% 100%',
        backgroundRepeat: 'no-repeat',
      };

  return (
    <motion.div
      className={`skeleton ${className}`}
      style={{ ...styles, ...shimmerAnimation }}
      animate={
        prefersReducedMotion
          ? {}
          : {
              backgroundPosition: ['200% 0', '-200% 0'],
            }
      }
      transition={
        prefersReducedMotion
          ? {}
          : {
              duration: 2,
              repeat: Infinity,
              ease: 'linear',
            }
      }
      aria-busy="true"
      aria-live="polite"
    />
  );
}

interface SkeletonTextProps {
  lines?: number;
  className?: string;
}

/**
 * Skeleton for multiple lines of text
 */
export function SkeletonText({ lines = 3, className = '' }: SkeletonTextProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      {Array.from({ length: lines }).map((_, index) => (
        <Skeleton
          key={index}
          width={index === lines - 1 ? '80%' : '100%'}
          height="0.875rem"
          variant="text"
        />
      ))}
    </div>
  );
}

interface SkeletonCardProps {
  hasImage?: boolean;
  hasAvatar?: boolean;
  className?: string;
}

/**
 * Skeleton for card-like content
 */
export function SkeletonCard({
  hasImage = true,
  hasAvatar = false,
  className = '',
}: SkeletonCardProps) {
  return (
    <div className={`p-4 space-y-4 bg-var(--color-surface) rounded-lg ${className}`}>
      {hasImage && <Skeleton height="12rem" variant="rectangular" />}

      <div className="space-y-3">
        {hasAvatar && (
          <div className="flex items-center space-x-3">
            <Skeleton variant="circular" width="2.5rem" />
            <div className="flex-1">
              <Skeleton width="40%" height="1rem" />
            </div>
          </div>
        )}

        <Skeleton width="60%" height="1.5rem" />
        <SkeletonText lines={2} />
      </div>
    </div>
  );
}

interface SkeletonListProps {
  count?: number;
  className?: string;
}

/**
 * Skeleton for list items
 */
export function SkeletonList({ count = 5, className = '' }: SkeletonListProps) {
  return (
    <div className={`space-y-3 ${className}`}>
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="flex items-center space-x-3">
          <Skeleton variant="circular" width="2.5rem" />
          <div className="flex-1 space-y-2">
            <Skeleton width="80%" height="1rem" />
            <Skeleton width="60%" height="0.75rem" />
          </div>
        </div>
      ))}
    </div>
  );
}

interface SkeletonTableProps {
  rows?: number;
  columns?: number;
  className?: string;
}

/**
 * Skeleton for table data
 */
export function SkeletonTable({ rows = 5, columns = 4, className = '' }: SkeletonTableProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      {/* Header */}
      <div className="flex space-x-4 pb-2 border-b border-var(--color-border)">
        {Array.from({ length: columns }).map((_, index) => (
          <div key={index} className="flex-1">
            <Skeleton height="1rem" />
          </div>
        ))}
      </div>

      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex space-x-4 py-2">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <div key={colIndex} className="flex-1">
              <Skeleton height="0.875rem" />
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
