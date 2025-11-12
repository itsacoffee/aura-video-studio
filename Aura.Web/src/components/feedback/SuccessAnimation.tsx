import { CheckmarkCircle24Filled } from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { celebrationVariants, scaleVariants } from '../../utils/animations';

interface SuccessAnimationProps {
  show: boolean;
  message?: string;
  onComplete?: () => void;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Animated success feedback with celebration effect
 * Perfect for showing success states after actions
 */
export function SuccessAnimation({
  show,
  message = 'Success!',
  onComplete,
  size = 'md',
}: SuccessAnimationProps) {
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
          onAnimationComplete={onComplete}
        >
          <motion.div
            variants={prefersReducedMotion ? undefined : celebrationVariants}
            initial="initial"
            animate="animate"
          >
            <CheckmarkCircle24Filled
              style={{
                width: iconSize,
                height: iconSize,
                color: 'var(--color-success)',
              }}
            />
          </motion.div>
          
          <motion.p
            className={`${textSize} font-medium text-success-600 dark:text-success-400`}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2 }}
          >
            {message}
          </motion.p>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
