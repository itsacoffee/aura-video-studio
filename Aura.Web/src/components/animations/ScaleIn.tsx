import { motion } from 'framer-motion';
import { ReactNode } from 'react';
import { scaleVariants } from '../../utils/animations';

interface ScaleInProps {
  children: ReactNode;
  className?: string;
  delay?: number;
  duration?: number;
}

/**
 * Scale-in animation component
 * Respects system reduced motion preferences via CSS variables
 */
export function ScaleIn({ children, className, delay = 0, duration = 0.25 }: ScaleInProps) {
  // Get the computed duration from CSS variable which respects graphics settings
  const computedDuration =
    typeof document !== 'undefined'
      ? getComputedStyle(document.documentElement).getPropertyValue('--duration-normal').trim()
      : '250ms';
  const animationsDisabled = computedDuration === '0ms';

  if (animationsDisabled) {
    return <div className={className}>{children}</div>;
  }

  return (
    <motion.div
      className={className}
      initial="hidden"
      animate="visible"
      exit="exit"
      variants={scaleVariants}
      transition={{ delay, duration }}
    >
      {children}
    </motion.div>
  );
}
