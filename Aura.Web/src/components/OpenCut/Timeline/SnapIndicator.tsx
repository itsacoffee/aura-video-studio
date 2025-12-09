/**
 * SnapIndicator Component
 *
 * Visual indicator displayed when a clip snaps to a snap point.
 * Shows a vertical line at the snap position with a brief highlight animation.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { motion, AnimatePresence } from 'framer-motion';
import type { FC } from 'react';
import { openCutTokens } from '../../../styles/designTokens';

export type SnapType = 'clip-edge' | 'marker' | 'playhead' | 'time-zero';

export interface SnapIndicatorProps {
  /** Position in pixels from left of timeline */
  position: number;
  /** Whether the indicator is visible */
  visible: boolean;
  /** Type of snap point */
  snapType?: SnapType;
  /** Optional className */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    pointerEvents: 'none',
    zIndex: openCutTokens.zIndex.sticky - 3,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  line: {
    width: '2px',
    height: '100%',
    borderRadius: '1px',
  },
  lineClipEdge: {
    backgroundColor: openCutTokens.colors.snap,
    boxShadow: `0 0 8px ${openCutTokens.colors.snap}, 0 0 16px ${openCutTokens.colors.snap}40`,
  },
  lineMarker: {
    backgroundColor: openCutTokens.colors.marker,
    boxShadow: `0 0 8px ${openCutTokens.colors.marker}, 0 0 16px ${openCutTokens.colors.marker}40`,
  },
  linePlayhead: {
    backgroundColor: openCutTokens.colors.playhead,
    boxShadow: `0 0 8px ${openCutTokens.colors.playhead}, 0 0 16px ${openCutTokens.colors.playhead}40`,
  },
  lineTimeZero: {
    backgroundColor: '#22C55E',
    boxShadow: '0 0 8px #22C55E, 0 0 16px #22C55E40',
  },
  diamond: {
    position: 'absolute',
    top: '-4px',
    width: '8px',
    height: '8px',
    transform: 'rotate(45deg)',
    borderRadius: '2px',
  },
  diamondClipEdge: {
    backgroundColor: openCutTokens.colors.snap,
  },
  diamondMarker: {
    backgroundColor: openCutTokens.colors.marker,
  },
  diamondPlayhead: {
    backgroundColor: openCutTokens.colors.playhead,
  },
  diamondTimeZero: {
    backgroundColor: '#22C55E',
  },
});

const snapLineVariants = {
  hidden: {
    opacity: 0,
    scaleY: 0.5,
  },
  visible: {
    opacity: 1,
    scaleY: 1,
    transition: {
      duration: 0.15,
      ease: [0.25, 1, 0.5, 1] as [number, number, number, number],
    },
  },
  exit: {
    opacity: 0,
    scaleY: 0.5,
    transition: {
      duration: 0.1,
      ease: [0.5, 0, 0.75, 0] as [number, number, number, number],
    },
  },
};

const diamondVariants = {
  hidden: {
    opacity: 0,
    scale: 0,
  },
  visible: {
    opacity: 1,
    scale: 1,
    transition: {
      duration: 0.2,
      ease: [0.34, 1.56, 0.64, 1] as [number, number, number, number],
    },
  },
  exit: {
    opacity: 0,
    scale: 0,
    transition: {
      duration: 0.1,
      ease: [0.5, 0, 0.75, 0] as [number, number, number, number],
    },
  },
};

export const SnapIndicator: FC<SnapIndicatorProps> = ({
  position,
  visible,
  snapType = 'clip-edge',
  className,
}) => {
  const styles = useStyles();

  const lineClass = {
    'clip-edge': styles.lineClipEdge,
    marker: styles.lineMarker,
    playhead: styles.linePlayhead,
    'time-zero': styles.lineTimeZero,
  }[snapType];

  const diamondClass = {
    'clip-edge': styles.diamondClipEdge,
    marker: styles.diamondMarker,
    playhead: styles.diamondPlayhead,
    'time-zero': styles.diamondTimeZero,
  }[snapType];

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          className={mergeClasses(styles.container, className)}
          style={{ left: position - 1 }}
          initial="hidden"
          animate="visible"
          exit="exit"
          variants={snapLineVariants}
        >
          <motion.div
            className={mergeClasses(styles.diamond, diamondClass)}
            variants={diamondVariants}
          />
          <div className={mergeClasses(styles.line, lineClass)} />
        </motion.div>
      )}
    </AnimatePresence>
  );
};

export default SnapIndicator;
