import { motion, Variants } from 'framer-motion';
import { ReactNode } from 'react';
import { slideVariants } from '../../utils/animations';

interface SlideInProps {
  children: ReactNode;
  className?: string;
  direction?: 'fromTop' | 'fromBottom' | 'fromLeft' | 'fromRight';
  delay?: number;
  duration?: number;
}

/**
 * Slide-in animation component
 * Respects system reduced motion preferences via CSS variables
 */
export function SlideIn({
  children,
  className,
  direction = 'fromBottom',
  delay = 0,
  duration = 0.25,
}: SlideInProps) {
  // Get the computed duration from CSS variable which respects graphics settings
  const computedDuration =
    typeof document !== 'undefined'
      ? getComputedStyle(document.documentElement).getPropertyValue('--duration-normal').trim()
      : '250ms';
  const animationsDisabled = computedDuration === '0ms';

  if (animationsDisabled) {
    return <div className={className}>{children}</div>;
  }

  const variants: Variants = slideVariants[direction];

  return (
    <motion.div
      className={className}
      initial="hidden"
      animate="visible"
      exit="exit"
      variants={variants}
      transition={{ delay, duration }}
    >
      {children}
    </motion.div>
  );
}
