import { ErrorCircle24Filled } from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { shakeVariants, scaleVariants } from '../../utils/animations';

interface ErrorAnimationProps {
  show: boolean;
  message?: string;
  onDismiss?: () => void;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Animated error feedback with shake effect
 * Draws attention to errors without being overwhelming
 */
export function ErrorAnimation({
  show,
  message = 'Something went wrong',
  onDismiss,
  size = 'md',
}: ErrorAnimationProps) {
  const prefersReducedMotion = useReducedMotion();

  const sizeMap = {
    sm: { icon: 32, text: 'text-sm' },
    md: { icon: 48, text: 'text-base' },
    lg: { icon: 64, text: 'text-lg' },
  };

  const { icon: iconSize, text: textSize } = sizeMap[size];

  return (
    <AnimatePresence>
      {show && (
        <motion.div
          className="flex flex-col items-center justify-center gap-3"
          initial="hidden"
          animate="visible"
          exit="exit"
          variants={scaleVariants}
        >
          <motion.div
            variants={prefersReducedMotion ? undefined : shakeVariants}
            animate={prefersReducedMotion ? undefined : 'shake'}
          >
            <ErrorCircle24Filled
              style={{
                width: iconSize,
                height: iconSize,
                color: 'var(--color-error)',
              }}
            />
          </motion.div>
          
          <motion.p
            className={`${textSize} font-medium text-error-600 dark:text-error-400 text-center max-w-md`}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.1 }}
          >
            {message}
          </motion.p>

          {onDismiss && (
            <motion.button
              className="mt-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
              onClick={onDismiss}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.3 }}
            >
              Dismiss
            </motion.button>
          )}
        </motion.div>
      )}
    </AnimatePresence>
  );
}
