import { motion, HTMLMotionProps } from 'framer-motion';
import { forwardRef, ReactNode, useState } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';

interface AnimatedInputProps extends Omit<HTMLMotionProps<'input'>, 'type'> {
  label?: string;
  error?: string;
  hint?: string;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  type?: string;
}

/**
 * Animated input component with smooth focus effects and validation states
 */
export const AnimatedInput = forwardRef<HTMLInputElement, AnimatedInputProps>(
  (
    { label, error, hint, leftIcon, rightIcon, className = '', type = 'text', disabled, ...props },
    ref
  ) => {
    const prefersReducedMotion = useReducedMotion();
    const [isFocused, setIsFocused] = useState(false);

    const inputVariants = {
      default: {
        scale: 1,
        boxShadow: '0 0 0 0px rgba(14, 165, 233, 0)',
      },
      focused: {
        scale: prefersReducedMotion ? 1 : 1.01,
        boxShadow: prefersReducedMotion
          ? '0 0 0 2px rgba(14, 165, 233, 0.2)'
          : '0 0 0 3px rgba(14, 165, 233, 0.2)',
        transition: {
          duration: 0.2,
          ease: 'easeOut' as const,
        },
      },
      error: {
        scale: prefersReducedMotion ? 1 : 1.01,
        boxShadow: prefersReducedMotion
          ? '0 0 0 2px rgba(239, 68, 68, 0.2)'
          : '0 0 0 3px rgba(239, 68, 68, 0.2)',
      },
    };

    const labelVariants = {
      default: {
        y: 0,
        scale: 1,
        color: 'var(--color-text-secondary)',
      },
      focused: {
        y: prefersReducedMotion ? 0 : -2,
        scale: 1,
        color: error ? 'var(--color-error)' : 'var(--color-primary)',
        transition: {
          duration: 0.2,
        },
      },
    };

    const baseInputStyles = `
      w-full px-3 py-2 text-base
      bg-white dark:bg-gray-800
      border-2 rounded-lg
      transition-colors
      focus:outline-none
      disabled:opacity-50 disabled:cursor-not-allowed
    `;

    const borderColor = error
      ? 'border-error-500 dark:border-error-400'
      : 'border-gray-300 dark:border-gray-600 focus:border-primary-500';

    return (
      <div className="w-full">
        {label && (
          <motion.label
            className="block text-sm font-medium mb-1.5"
            variants={prefersReducedMotion ? undefined : labelVariants}
            animate={isFocused ? 'focused' : 'default'}
          >
            {label}
          </motion.label>
        )}

        <div className="relative">
          {leftIcon && (
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">{leftIcon}</div>
          )}

          <motion.input
            ref={ref}
            type={type}
            className={`${baseInputStyles} ${borderColor} ${leftIcon ? 'pl-10' : ''} ${rightIcon ? 'pr-10' : ''} ${className}`}
            variants={prefersReducedMotion ? undefined : inputVariants}
            animate={error ? 'error' : isFocused ? 'focused' : 'default'}
            onFocus={(e) => {
              setIsFocused(true);
              props.onFocus?.(e);
            }}
            onBlur={(e) => {
              setIsFocused(false);
              props.onBlur?.(e);
            }}
            disabled={disabled}
            aria-invalid={error ? 'true' : 'false'}
            aria-describedby={error ? 'input-error' : hint ? 'input-hint' : undefined}
            {...props}
          />

          {rightIcon && (
            <div className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400">
              {rightIcon}
            </div>
          )}
        </div>

        {error && (
          <motion.p
            id="input-error"
            className="mt-1.5 text-sm text-error-600 dark:text-error-400"
            initial={{ opacity: 0, y: -5 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.2 }}
          >
            {error}
          </motion.p>
        )}

        {hint && !error && (
          <p id="input-hint" className="mt-1.5 text-sm text-gray-500 dark:text-gray-400">
            {hint}
          </p>
        )}
      </div>
    );
  }
);

AnimatedInput.displayName = 'AnimatedInput';
