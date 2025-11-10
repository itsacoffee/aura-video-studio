import { motion } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { scaleVariants } from '../../utils/animations';

interface ScaleInProps {
  children: ReactNode;
  className?: string;
  delay?: number;
  duration?: number;
}

/**
 * Scale-in animation component
 * Perfect for modals, dialogs, and popovers
 */
export function ScaleIn({ children, className, delay = 0, duration = 0.25 }: ScaleInProps) {
  const prefersReducedMotion = useReducedMotion();

  if (prefersReducedMotion) {
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
