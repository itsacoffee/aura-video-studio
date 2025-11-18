import { motion, AnimatePresence } from 'framer-motion';
import { ReactNode, useState, useRef, useEffect } from 'react';
import { useReducedMotion } from '../../hooks/useReducedMotion';
import { tooltipVariants } from '../../utils/animations';

interface AnimatedTooltipProps {
  content: ReactNode;
  children: ReactNode;
  placement?: 'top' | 'bottom' | 'left' | 'right';
  delay?: number;
  className?: string;
}

/**
 * Animated tooltip with smooth appearance
 * Automatically positions itself based on available space
 */
export function AnimatedTooltip({
  content,
  children,
  placement = 'top',
  delay = 200,
  className = '',
}: AnimatedTooltipProps) {
  const prefersReducedMotion = useReducedMotion();
  const [isVisible, setIsVisible] = useState(false);
  const [position, setPosition] = useState(placement);
  const timeoutRef = useRef<NodeJS.Timeout>();
  const triggerRef = useRef<HTMLDivElement>(null);

  const placementStyles = {
    top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
    bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
    left: 'right-full top-1/2 -translate-y-1/2 mr-2',
    right: 'left-full top-1/2 -translate-y-1/2 ml-2',
  };

  const arrowStyles = {
    top: 'top-full left-1/2 -translate-x-1/2 border-t-gray-900 dark:border-t-gray-700',
    bottom: 'bottom-full left-1/2 -translate-x-1/2 border-b-gray-900 dark:border-b-gray-700',
    left: 'left-full top-1/2 -translate-y-1/2 border-l-gray-900 dark:border-l-gray-700',
    right: 'right-full top-1/2 -translate-y-1/2 border-r-gray-900 dark:border-r-gray-700',
  };

  useEffect(() => {
    if (!isVisible || !triggerRef.current) return;

    const rect = triggerRef.current.getBoundingClientRect();
    const viewport = {
      width: window.innerWidth,
      height: window.innerHeight,
    };

    // Auto-adjust placement based on available space
    let newPlacement = placement;

    if (placement === 'top' && rect.top < 100) {
      newPlacement = 'bottom';
    } else if (placement === 'bottom' && rect.bottom > viewport.height - 100) {
      newPlacement = 'top';
    } else if (placement === 'left' && rect.left < 100) {
      newPlacement = 'right';
    } else if (placement === 'right' && rect.right > viewport.width - 100) {
      newPlacement = 'left';
    }

    setPosition(newPlacement);
  }, [isVisible, placement]);

  const handleMouseEnter = () => {
    timeoutRef.current = setTimeout(() => {
      setIsVisible(true);
    }, delay);
  };

  const handleMouseLeave = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    setIsVisible(false);
  };

  return (
    <div
      ref={triggerRef}
      className="relative inline-block"
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onFocus={handleMouseEnter}
      onBlur={handleMouseLeave}
    >
      {children}

      <AnimatePresence>
        {isVisible && (
          <motion.div
            className={`absolute ${placementStyles[position]} z-50 pointer-events-none ${className}`}
            variants={prefersReducedMotion ? undefined : tooltipVariants}
            initial="hidden"
            animate="visible"
            exit="exit"
          >
            <div className="relative px-3 py-2 text-sm text-white bg-gray-900 dark:bg-gray-700 rounded-lg shadow-xl whitespace-nowrap">
              {content}

              {/* Arrow */}
              <div
                className={`absolute w-0 h-0 border-4 border-transparent ${arrowStyles[position]}`}
              />
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
