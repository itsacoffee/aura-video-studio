/**
 * LowerThirdRenderer Component
 *
 * Specialized renderer for lower third graphics optimized for
 * name/title text display with animated entry/exit sequences.
 */

import { makeStyles } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type { AppliedGraphic } from '../../../types/motionGraphics';

export interface LowerThirdRendererProps {
  graphic: AppliedGraphic;
  currentTime: number;
  width: number;
  height: number;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    left: 0,
    bottom: '10%',
    width: '100%',
    pointerEvents: 'none',
    padding: '0 5%',
    boxSizing: 'border-box',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    maxWidth: '40%',
  },
  name: {
    fontWeight: 700,
    color: '#ffffff',
    textShadow: '0 2px 4px rgba(0,0,0,0.5)',
  },
  title: {
    fontWeight: 400,
    color: '#a1a1aa',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  accentLine: {
    height: '2px',
    marginBottom: '8px',
    transformOrigin: 'left center',
  },
  background: {
    position: 'absolute',
    inset: 0,
    zIndex: -1,
  },
});

export const LowerThirdRenderer: FC<LowerThirdRendererProps> = ({
  graphic,
  currentTime,
  width,
  height,
}) => {
  const styles = useStyles();
  const graphicsStore = useMotionGraphicsStore();
  const asset = graphicsStore.getAsset(graphic.assetId);

  // Calculate animation progress
  const animationState = useMemo(() => {
    const relativeTime = currentTime - graphic.startTime;
    const enterDuration = 0.5;
    const exitDuration = 0.4;
    const holdDuration = graphic.duration - enterDuration - exitDuration;

    if (relativeTime < 0) return { phase: 'hidden' as const, progress: 0 };
    if (relativeTime > graphic.duration) return { phase: 'hidden' as const, progress: 0 };

    if (relativeTime < enterDuration) {
      return { phase: 'enter' as const, progress: relativeTime / enterDuration };
    }
    if (relativeTime < enterDuration + holdDuration) {
      return { phase: 'hold' as const, progress: 1 };
    }
    const exitProgress = (relativeTime - enterDuration - holdDuration) / exitDuration;
    return { phase: 'exit' as const, progress: Math.min(1, exitProgress) };
  }, [currentTime, graphic]);

  if (!asset || animationState.phase === 'hidden') {
    return null;
  }

  // Get customization values
  const name = (graphic.customValues.name as string) || 'John Smith';
  const title = (graphic.customValues.title as string) || 'Title';
  const accentColor = (graphic.customValues.accentColor as string) || '#3B82F6';
  const textColor = (graphic.customValues.textColor as string) || '#ffffff';

  // Calculate scale based on video dimensions
  const scale = Math.min(width / 1920, height / 1080);
  const nameFontSize = 28 * scale;
  const titleFontSize = 16 * scale;

  // Animation variants
  const containerVariants = {
    hidden: { opacity: 0, x: -30 },
    enter: { opacity: 1, x: 0 },
    hold: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: -30 },
  };

  const lineVariants = {
    hidden: { scaleX: 0 },
    enter: { scaleX: 1 },
    hold: { scaleX: 1 },
    exit: { scaleX: 0 },
  };

  return (
    <div className={styles.container}>
      <motion.div
        className={styles.content}
        initial="hidden"
        animate={animationState.phase}
        variants={containerVariants}
        transition={{ duration: 0.4, ease: [0.25, 1, 0.5, 1] }}
      >
        <motion.div
          className={styles.accentLine}
          style={{ backgroundColor: accentColor }}
          variants={lineVariants}
          transition={{ duration: 0.3, ease: [0.25, 1, 0.5, 1] }}
        />
        <div className={styles.name} style={{ fontSize: `${nameFontSize}px`, color: textColor }}>
          {name}
        </div>
        <div className={styles.title} style={{ fontSize: `${titleFontSize}px` }}>
          {title}
        </div>
      </motion.div>
    </div>
  );
};

export default LowerThirdRenderer;
