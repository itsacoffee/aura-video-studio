import { motion, HTMLMotionProps, Variants } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { createVariants, fadeVariants } from '../../utils/animations';

interface AnimatedDivProps extends Omit<HTMLMotionProps<'div'>, 'variants'> {
  children: ReactNode;
  variants?: Variants;
  animation?: 'fade' | 'slide' | 'scale' | 'custom';
  delay?: number;
  duration?: number;
}

/**
 * Animated div component that respects reduced motion preferences
 * Provides common animation presets and supports custom variants
 */
export function AnimatedDiv({
  children,
  variants,
  animation = 'fade',
  delay = 0,
  duration,
  initial = 'hidden',
  animate = 'visible',
  exit = 'exit',
  ...props
}: AnimatedDivProps) {
  const prefersReducedMotion = useReducedMotion();

  // Use provided variants or default to fade
  const animationVariants = variants || fadeVariants;
  const finalVariants = createVariants(animationVariants, prefersReducedMotion);

  return (
    <motion.div
      initial={initial}
      animate={animate}
      exit={exit}
      variants={finalVariants}
      transition={{
        delay,
        ...(duration && { duration }),
      }}
      {...props}
    >
      {children}
    </motion.div>
  );
}
