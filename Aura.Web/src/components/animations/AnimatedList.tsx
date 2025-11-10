import { motion, Variants } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { staggerContainer, staggerItem } from '../../utils/animations';

interface AnimatedListProps {
  children: ReactNode;
  className?: string;
  staggerDelay?: number;
  as?: 'div' | 'ul' | 'ol';
}

/**
 * Animated list component with staggered children animations
 * Respects reduced motion preferences
 */
export function AnimatedList({
  children,
  className,
  staggerDelay = 0.05,
  as: Component = 'div',
}: AnimatedListProps) {
  const prefersReducedMotion = useReducedMotion();

  const containerVariants: Variants = prefersReducedMotion
    ? { hidden: { opacity: 0 }, visible: { opacity: 1, transition: { duration: 0.01 } } }
    : {
        ...staggerContainer,
        visible: {
          ...staggerContainer.visible,
          transition: {
            ...staggerContainer.visible.transition,
            staggerChildren: staggerDelay,
          },
        },
      };

  const MotionComponent = motion[Component];

  return (
    <MotionComponent
      className={className}
      initial="hidden"
      animate="visible"
      variants={containerVariants}
    >
      {children}
    </MotionComponent>
  );
}

interface AnimatedListItemProps {
  children: ReactNode;
  className?: string;
  as?: 'div' | 'li';
}

/**
 * Individual item for AnimatedList
 * Should be used as direct children of AnimatedList
 */
export function AnimatedListItem({
  children,
  className,
  as: Component = 'div',
}: AnimatedListItemProps) {
  const prefersReducedMotion = useReducedMotion();

  const itemVariants: Variants = prefersReducedMotion
    ? { hidden: { opacity: 0 }, visible: { opacity: 1, transition: { duration: 0.01 } } }
    : staggerItem;

  const MotionComponent = motion[Component];

  return (
    <MotionComponent className={className} variants={itemVariants}>
      {children}
    </MotionComponent>
  );
}
