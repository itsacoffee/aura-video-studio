import { motion } from 'framer-motion';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { rotateVariants } from '../../utils/animations';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  color?: string;
  className?: string;
  label?: string;
}

/**
 * Animated loading spinner with accessibility support
 */
export function LoadingSpinner({
  size = 'md',
  color = 'var(--color-primary)',
  className = '',
  label = 'Loading...',
}: LoadingSpinnerProps) {
  const prefersReducedMotion = useReducedMotion();

  const sizeMap = {
    sm: '1rem',
    md: '1.5rem',
    lg: '2rem',
    xl: '3rem',
  };

  const spinnerSize = sizeMap[size];

  if (prefersReducedMotion) {
    return (
      <div
        className={`inline-flex items-center justify-center ${className}`}
        role="status"
        aria-label={label}
      >
        <div
          style={{
            width: spinnerSize,
            height: spinnerSize,
            border: `2px solid ${color}`,
            borderRadius: '50%',
            opacity: 0.3,
          }}
        />
        <span className="sr-only">{label}</span>
      </div>
    );
  }

  return (
    <div
      className={`inline-flex items-center justify-center ${className}`}
      role="status"
      aria-label={label}
    >
      <motion.div
        style={{
          width: spinnerSize,
          height: spinnerSize,
          border: `2px solid transparent`,
          borderTopColor: color,
          borderRightColor: color,
          borderRadius: '50%',
        }}
        variants={rotateVariants}
        animate="rotate"
      />
      <span className="sr-only">{label}</span>
    </div>
  );
}

interface LoadingDotsProps {
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  className?: string;
  label?: string;
}

/**
 * Animated loading dots
 */
export function LoadingDots({
  size = 'md',
  color = 'var(--color-primary)',
  className = '',
  label = 'Loading...',
}: LoadingDotsProps) {
  const prefersReducedMotion = useReducedMotion();

  const sizeMap = {
    sm: '0.375rem',
    md: '0.5rem',
    lg: '0.625rem',
  };

  const dotSize = sizeMap[size];

  const dotVariants = {
    initial: { y: 0 },
    animate: {
      y: [0, -10, 0],
      transition: {
        duration: 0.6,
        repeat: Infinity,
        ease: 'easeInOut' as const,
      },
    },
  };

  if (prefersReducedMotion) {
    return (
      <div
        className={`inline-flex items-center space-x-1 ${className}`}
        role="status"
        aria-label={label}
      >
        {[0, 1, 2].map((i) => (
          <div
            key={i}
            style={{
              width: dotSize,
              height: dotSize,
              backgroundColor: color,
              borderRadius: '50%',
            }}
          />
        ))}
        <span className="sr-only">{label}</span>
      </div>
    );
  }

  return (
    <div
      className={`inline-flex items-center space-x-1 ${className}`}
      role="status"
      aria-label={label}
    >
      {[0, 1, 2].map((i) => (
        <motion.div
          key={i}
          style={{
            width: dotSize,
            height: dotSize,
            backgroundColor: color,
            borderRadius: '50%',
          }}
          variants={dotVariants}
          initial="initial"
          animate="animate"
          transition={{
            delay: i * 0.15,
          }}
        />
      ))}
      <span className="sr-only">{label}</span>
    </div>
  );
}

interface LoadingBarProps {
  progress?: number;
  indeterminate?: boolean;
  height?: string;
  color?: string;
  backgroundColor?: string;
  className?: string;
  label?: string;
}

/**
 * Animated loading bar with determinate and indeterminate modes
 */
export function LoadingBar({
  progress = 0,
  indeterminate = false,
  height = '0.25rem',
  color = 'var(--color-primary)',
  backgroundColor = 'var(--color-surface)',
  className = '',
  label = 'Loading...',
}: LoadingBarProps) {
  const prefersReducedMotion = useReducedMotion();

  const barVariants = {
    indeterminate: {
      x: ['-100%', '100%'],
      transition: {
        duration: 1.5,
        repeat: Infinity,
        ease: 'easeInOut' as const,
      },
    },
    determinate: {
      width: `${progress}%`,
      transition: {
        duration: 0.3,
        ease: 'easeOut' as const,
      },
    },
  };

  return (
    <div
      className={`relative overflow-hidden ${className}`}
      style={{ height, backgroundColor, borderRadius: '999px' }}
      role="progressbar"
      aria-label={label}
      aria-valuenow={indeterminate ? undefined : progress}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      {indeterminate ? (
        <motion.div
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '50%',
            height: '100%',
            backgroundColor: color,
            borderRadius: '999px',
          }}
          variants={barVariants}
          animate={prefersReducedMotion ? undefined : 'indeterminate'}
        />
      ) : (
        <motion.div
          style={{
            height: '100%',
            backgroundColor: color,
            borderRadius: '999px',
          }}
          variants={barVariants}
          animate={prefersReducedMotion ? undefined : 'determinate'}
          initial={{ width: 0 }}
        />
      )}
    </div>
  );
}
