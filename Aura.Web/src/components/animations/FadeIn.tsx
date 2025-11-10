import { motion } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { fadeVariants } from '../../utils/animations';

interface FadeInProps {
  children: ReactNode;
  className?: string;
  delay?: number;
  duration?: number;
}

/**
 * Simple fade-in animation component
 * Useful for quick animations without needing to configure variants
 */
export function FadeIn({ children, className, delay = 0, duration = 0.25 }: FadeInProps) {
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
      variants={fadeVariants}
      transition={{ delay, duration }}
    >
      {children}
    </motion.div>
  );
}
