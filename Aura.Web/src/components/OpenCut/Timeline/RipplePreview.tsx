/**
 * RipplePreview Component
 *
 * Visual preview showing how clips will be affected by a ripple edit operation.
 * Displays arrows and ghost clips to indicate movement.
 */

import { makeStyles, mergeClasses, Text } from '@fluentui/react-components';
import { ArrowRight24Regular, ArrowLeft24Regular } from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import type { FC } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export type RippleDirection = 'left' | 'right';

export interface RippleClipPreview {
  /** Clip ID */
  clipId: string;
  /** Current position in pixels */
  currentPosition: number;
  /** New position after ripple in pixels */
  newPosition: number;
  /** Width of the clip in pixels */
  width: number;
  /** Clip name for tooltip */
  name: string;
}

export interface RipplePreviewProps {
  /** Whether the preview is visible */
  visible: boolean;
  /** Direction of the ripple effect */
  direction: RippleDirection;
  /** Amount of time shift in seconds */
  timeShift: number;
  /** Clips that will be affected */
  affectedClips: RippleClipPreview[];
  /** Optional className */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    right: 0,
    pointerEvents: 'none',
    zIndex: openCutTokens.zIndex.sticky - 2,
  },
  ghostClip: {
    position: 'absolute',
    top: '4px',
    bottom: '4px',
    backgroundColor: 'rgba(139, 92, 246, 0.3)',
    border: '2px dashed rgba(139, 92, 246, 0.6)',
    borderRadius: openCutTokens.radius.sm,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  arrowContainer: {
    position: 'absolute',
    top: '50%',
    transform: 'translateY(-50%)',
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  arrow: {
    color: openCutTokens.colors.snap,
    filter: `drop-shadow(0 0 4px ${openCutTokens.colors.snap})`,
  },
  shiftLabel: {
    position: 'absolute',
    top: '50%',
    transform: 'translateY(-50%)',
    backgroundColor: 'rgba(139, 92, 246, 0.9)',
    color: 'white',
    padding: '2px 8px',
    borderRadius: openCutTokens.radius.sm,
    fontSize: openCutTokens.typography.fontSize.xs,
    fontFamily: openCutTokens.typography.fontFamily.mono,
    fontWeight: openCutTokens.typography.fontWeight.medium,
    whiteSpace: 'nowrap',
    boxShadow: openCutTokens.shadows.sm,
  },
  clipName: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: 'rgba(255, 255, 255, 0.9)',
    fontWeight: openCutTokens.typography.fontWeight.medium,
    maxWidth: '100%',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    padding: '0 4px',
  },
});

function formatTimeShift(seconds: number): string {
  const absSeconds = Math.abs(seconds);
  const sign = seconds >= 0 ? '+' : '-';
  if (absSeconds < 1) {
    return `${sign}${Math.round(absSeconds * 1000)}ms`;
  }
  return `${sign}${absSeconds.toFixed(2)}s`;
}

const containerVariants = {
  hidden: {
    opacity: 0,
  },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.15,
      staggerChildren: 0.05,
    },
  },
  exit: {
    opacity: 0,
    transition: {
      duration: 0.1,
    },
  },
};

const clipVariants = {
  hidden: (direction: RippleDirection) => ({
    opacity: 0,
    x: direction === 'right' ? -20 : 20,
  }),
  visible: {
    opacity: 1,
    x: 0,
    transition: {
      duration: 0.2,
      ease: [0.25, 1, 0.5, 1] as [number, number, number, number],
    },
  },
  exit: {
    opacity: 0,
    transition: {
      duration: 0.1,
    },
  },
};

const arrowVariants = {
  hidden: {
    opacity: 0,
    scale: 0.5,
  },
  visible: {
    opacity: 1,
    scale: 1,
    transition: {
      duration: 0.15,
      ease: [0.34, 1.56, 0.64, 1] as [number, number, number, number],
    },
  },
};

export const RipplePreview: FC<RipplePreviewProps> = ({
  visible,
  direction,
  timeShift,
  affectedClips,
  className,
}) => {
  const styles = useStyles();

  return (
    <AnimatePresence>
      {visible && affectedClips.length > 0 && (
        <motion.div
          className={mergeClasses(styles.container, className)}
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          exit="exit"
        >
          {affectedClips.map((clip) => {
            const arrowPosition =
              direction === 'right'
                ? clip.currentPosition + clip.width / 2
                : clip.currentPosition + clip.width / 2 - 40;

            return (
              <motion.div key={clip.clipId} custom={direction} variants={clipVariants}>
                {/* Ghost clip at new position */}
                <div
                  className={styles.ghostClip}
                  style={{
                    left: clip.newPosition,
                    width: clip.width,
                  }}
                >
                  <Text className={styles.clipName}>{clip.name}</Text>
                </div>

                {/* Arrow indicating movement */}
                <motion.div
                  className={styles.arrowContainer}
                  style={{ left: arrowPosition }}
                  variants={arrowVariants}
                >
                  {direction === 'right' ? (
                    <ArrowRight24Regular className={styles.arrow} />
                  ) : (
                    <ArrowLeft24Regular className={styles.arrow} />
                  )}
                </motion.div>
              </motion.div>
            );
          })}

          {/* Time shift label */}
          {affectedClips.length > 0 && (
            <motion.div
              className={styles.shiftLabel}
              style={{
                left:
                  direction === 'right'
                    ? affectedClips[0].currentPosition + affectedClips[0].width + 8
                    : affectedClips[0].currentPosition - 60,
              }}
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: 0.1 }}
            >
              {formatTimeShift(timeShift)}
            </motion.div>
          )}
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default RipplePreview;
