import { motion } from 'framer-motion';
import { useReducedMotion } from '../../hooks/useReducedMotion';

interface ProgressIndicatorProps {
  progress: number;
  label?: string;
  showPercentage?: boolean;
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  variant?: 'bar' | 'circle';
  className?: string;
}

/**
 * Animated progress indicator with bar and circular variants
 */
export function ProgressIndicator({
  progress,
  label,
  showPercentage = true,
  size = 'md',
  color = 'var(--color-primary)',
  variant = 'bar',
  className = '',
}: ProgressIndicatorProps) {
  const prefersReducedMotion = useReducedMotion();
  const clampedProgress = Math.min(Math.max(progress, 0), 100);

  if (variant === 'circle') {
    return <CircularProgress progress={clampedProgress} size={size} color={color} />;
  }

  const heightMap = {
    sm: '0.25rem',
    md: '0.375rem',
    lg: '0.5rem',
  };

  const height = heightMap[size];

  return (
    <div className={`w-full ${className}`}>
      {(label || showPercentage) && (
        <div className="flex justify-between items-center mb-2">
          {label && (
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{label}</span>
          )}
          {showPercentage && (
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
              {Math.round(clampedProgress)}%
            </span>
          )}
        </div>
      )}

      <div
        className="relative overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700"
        style={{ height }}
      >
        <motion.div
          className="h-full rounded-full"
          style={{ backgroundColor: color, boxShadow: `0 0 10px ${color}40` }}
          initial={{ width: 0 }}
          animate={{ width: `${clampedProgress}%` }}
          transition={prefersReducedMotion ? { duration: 0 } : { duration: 0.5, ease: 'easeOut' }}
        />
      </div>
    </div>
  );
}

interface CircularProgressProps {
  progress: number;
  size: 'sm' | 'md' | 'lg';
  color: string;
}

function CircularProgress({ progress, size, color }: CircularProgressProps) {
  const prefersReducedMotion = useReducedMotion();

  const sizeMap = {
    sm: { dimension: 40, strokeWidth: 3, fontSize: 'text-xs' },
    md: { dimension: 64, strokeWidth: 4, fontSize: 'text-sm' },
    lg: { dimension: 96, strokeWidth: 5, fontSize: 'text-base' },
  };

  const { dimension, strokeWidth, fontSize } = sizeMap[size];
  const radius = (dimension - strokeWidth) / 2;
  const circumference = radius * 2 * Math.PI;
  const offset = circumference - (progress / 100) * circumference;

  return (
    <div className="relative inline-flex items-center justify-center">
      <svg width={dimension} height={dimension} className="transform -rotate-90">
        {/* Background circle */}
        <circle
          cx={dimension / 2}
          cy={dimension / 2}
          r={radius}
          stroke="currentColor"
          strokeWidth={strokeWidth}
          fill="transparent"
          className="text-gray-200 dark:text-gray-700"
        />

        {/* Progress circle */}
        <motion.circle
          cx={dimension / 2}
          cy={dimension / 2}
          r={radius}
          stroke={color}
          strokeWidth={strokeWidth}
          fill="transparent"
          strokeLinecap="round"
          initial={{ strokeDashoffset: circumference }}
          animate={{ strokeDashoffset: offset }}
          transition={prefersReducedMotion ? { duration: 0 } : { duration: 0.5, ease: 'easeOut' }}
          style={{
            strokeDasharray: circumference,
          }}
        />
      </svg>

      <div className="absolute inset-0 flex items-center justify-center">
        <span className={`${fontSize} font-semibold`} style={{ color }}>
          {Math.round(progress)}%
        </span>
      </div>
    </div>
  );
}
