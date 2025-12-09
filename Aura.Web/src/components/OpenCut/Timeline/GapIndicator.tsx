/**
 * GapIndicator Component
 *
 * Visual indicator for gaps between clips in the timeline.
 * Displays a highlighted region with a "close gap" button on hover.
 */

import { makeStyles, mergeClasses, Button, Tooltip, Text } from '@fluentui/react-components';
import { ArrowSync24Regular } from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export interface GapIndicatorProps {
  /** Start position in pixels from left of timeline */
  startPosition: number;
  /** End position in pixels from left of timeline */
  endPosition: number;
  /** Gap duration in seconds */
  duration: number;
  /** Track ID this gap belongs to */
  trackId: string;
  /** Gap start time in seconds */
  gapStart: number;
  /** Gap end time in seconds */
  gapEnd: number;
  /** Whether the gap indicator is visible */
  visible: boolean;
  /** Callback when close gap button is clicked */
  onCloseGap?: (trackId: string, gapStart: number, gapEnd: number) => void;
  /** Optional className */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: '4px',
    bottom: '4px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    pointerEvents: 'auto',
    zIndex: openCutTokens.zIndex.base,
    borderRadius: openCutTokens.radius.sm,
    transition: `background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
  },
  containerIdle: {
    backgroundColor: 'rgba(139, 92, 246, 0.1)',
    border: '1px dashed rgba(139, 92, 246, 0.3)',
  },
  containerHovered: {
    backgroundColor: 'rgba(139, 92, 246, 0.2)',
    border: '1px dashed rgba(139, 92, 246, 0.5)',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '4px',
    padding: '4px',
  },
  durationText: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: openCutTokens.colors.snap,
    fontFamily: openCutTokens.typography.fontFamily.mono,
    fontWeight: openCutTokens.typography.fontWeight.medium,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    padding: '2px 6px',
    borderRadius: openCutTokens.radius.xs,
    whiteSpace: 'nowrap',
  },
  closeButton: {
    minWidth: '28px',
    minHeight: '28px',
    padding: '4px',
    backgroundColor: 'rgba(139, 92, 246, 0.9)',
    color: 'white',
    ':hover': {
      backgroundColor: 'rgba(139, 92, 246, 1)',
    },
  },
  closeButtonSmall: {
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
});

function formatGapDuration(seconds: number): string {
  if (seconds < 1) {
    return `${Math.round(seconds * 1000)}ms`;
  }
  if (seconds < 60) {
    return `${seconds.toFixed(2)}s`;
  }
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins}:${secs.toFixed(2).padStart(5, '0')}`;
}

const containerVariants = {
  hidden: {
    opacity: 0,
    scale: 0.95,
  },
  visible: {
    opacity: 1,
    scale: 1,
    transition: {
      duration: 0.15,
      ease: [0.25, 1, 0.5, 1] as [number, number, number, number],
    },
  },
  exit: {
    opacity: 0,
    scale: 0.95,
    transition: {
      duration: 0.1,
      ease: [0.5, 0, 0.75, 0] as [number, number, number, number],
    },
  },
};

const buttonVariants = {
  hidden: {
    opacity: 0,
    scale: 0.8,
  },
  visible: {
    opacity: 1,
    scale: 1,
    transition: {
      delay: 0.1,
      duration: 0.15,
      ease: [0.34, 1.56, 0.64, 1] as [number, number, number, number],
    },
  },
  exit: {
    opacity: 0,
    scale: 0.8,
    transition: {
      duration: 0.1,
    },
  },
};

export const GapIndicator: FC<GapIndicatorProps> = ({
  startPosition,
  endPosition,
  duration,
  trackId,
  gapStart,
  gapEnd,
  visible,
  onCloseGap,
  className,
}) => {
  const styles = useStyles();
  const [isHovered, setIsHovered] = useState(false);

  const width = Math.max(endPosition - startPosition, 0);
  const isNarrow = width < 60;

  const handleMouseEnter = useCallback(() => {
    setIsHovered(true);
  }, []);

  const handleMouseLeave = useCallback(() => {
    setIsHovered(false);
  }, []);

  const handleCloseGap = useCallback(
    (e: ReactMouseEvent) => {
      e.stopPropagation();
      onCloseGap?.(trackId, gapStart, gapEnd);
    },
    [onCloseGap, trackId, gapStart, gapEnd]
  );

  if (width < 10) {
    return null;
  }

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          className={mergeClasses(
            styles.container,
            isHovered ? styles.containerHovered : styles.containerIdle,
            className
          )}
          style={{
            left: startPosition,
            width: width,
          }}
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
          initial="hidden"
          animate="visible"
          exit="exit"
          variants={containerVariants}
          role="region"
          aria-label={`Gap of ${formatGapDuration(duration)}`}
        >
          <div className={styles.content}>
            {!isNarrow && (
              <Text className={styles.durationText}>{formatGapDuration(duration)}</Text>
            )}
            <AnimatePresence>
              {isHovered && (
                <motion.div
                  variants={buttonVariants}
                  initial="hidden"
                  animate="visible"
                  exit="exit"
                >
                  <Tooltip content="Close gap" relationship="label">
                    <Button
                      appearance="subtle"
                      icon={<ArrowSync24Regular />}
                      className={mergeClasses(
                        styles.closeButton,
                        isNarrow && styles.closeButtonSmall
                      )}
                      onClick={handleCloseGap}
                      aria-label="Close gap"
                    />
                  </Tooltip>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default GapIndicator;
