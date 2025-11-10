import { motion, HTMLMotionProps } from 'framer-motion';
import { ReactNode } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { cardHoverVariants } from '../../utils/animations';

interface AnimatedCardProps extends Omit<HTMLMotionProps<'div'>, 'variants'> {
  children: ReactNode;
  variant?: 'elevated' | 'outlined' | 'filled';
  interactive?: boolean;
  onClick?: () => void;
}

/**
 * Animated card component with hover effects and multiple variants
 */
export function AnimatedCard({
  children,
  variant = 'elevated',
  interactive = false,
  onClick,
  className = '',
  ...props
}: AnimatedCardProps) {
  const prefersReducedMotion = useReducedMotion();

  const variantStyles = {
    elevated:
      'bg-white dark:bg-gray-800 shadow-md hover:shadow-lg border border-gray-200 dark:border-gray-700',
    outlined:
      'bg-transparent border-2 border-gray-300 dark:border-gray-600 hover:border-primary-500',
    filled: 'bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800',
  };

  const baseStyles = 'rounded-xl p-6 transition-all duration-200';
  const interactiveStyles = interactive || onClick ? 'cursor-pointer' : '';

  return (
    <motion.div
      className={`${baseStyles} ${variantStyles[variant]} ${interactiveStyles} ${className}`}
      variants={prefersReducedMotion || !interactive ? undefined : cardHoverVariants}
      initial="rest"
      whileHover={interactive || onClick ? 'hover' : undefined}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyPress={(e) => {
        if (onClick && (e.key === 'Enter' || e.key === ' ')) {
          e.preventDefault();
          onClick();
        }
      }}
      {...props}
    >
      {children}
    </motion.div>
  );
}

interface AnimatedCardHeaderProps {
  title: string;
  subtitle?: string;
  action?: ReactNode;
  className?: string;
}

export function AnimatedCardHeader({
  title,
  subtitle,
  action,
  className = '',
}: AnimatedCardHeaderProps) {
  return (
    <div className={`flex items-start justify-between mb-4 ${className}`}>
      <div className="flex-1">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{title}</h3>
        {subtitle && (
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{subtitle}</p>
        )}
      </div>
      {action && <div className="ml-4">{action}</div>}
    </div>
  );
}

interface AnimatedCardBodyProps {
  children: ReactNode;
  className?: string;
}

export function AnimatedCardBody({ children, className = '' }: AnimatedCardBodyProps) {
  return <div className={`text-gray-700 dark:text-gray-300 ${className}`}>{children}</div>;
}

interface AnimatedCardFooterProps {
  children: ReactNode;
  className?: string;
}

export function AnimatedCardFooter({ children, className = '' }: AnimatedCardFooterProps) {
  return (
    <div
      className={`mt-6 pt-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between gap-3 ${className}`}
    >
      {children}
    </div>
  );
}
