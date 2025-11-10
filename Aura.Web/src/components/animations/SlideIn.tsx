import { motion, Variants } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
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
 * Animates content sliding in from a specified direction
 */
export function SlideIn({
  children,
  className,
  direction = 'fromBottom',
  delay = 0,
  duration = 0.25,
}: SlideInProps) {
  const prefersReducedMotion = useReducedMotion();

  if (prefersReducedMotion) {
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
