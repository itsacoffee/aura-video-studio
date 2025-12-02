import { motion } from 'framer-motion';
import { ReactNode } from 'react';
import { useAnimationsDisabled } from '../../hooks/useAnimationsDisabled';
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
  const animationsDisabled = useAnimationsDisabled();

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
