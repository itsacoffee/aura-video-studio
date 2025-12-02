import { motion, Variants } from 'framer-motion';
import { ReactNode } from 'react';
import { useAnimationsDisabled } from '../../hooks/useAnimationsDisabled';
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
  const animationsDisabled = useAnimationsDisabled();

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
