/**
 * CalloutRenderer Component
 *
 * Specialized renderer for callout graphics including
 * arrow and pointer animations, circle highlight effects,
 * tooltip positioning, and attention animations.
 */

import { makeStyles } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type { AppliedGraphic } from '../../../types/motionGraphics';

export interface CalloutRendererProps {
  graphic: AppliedGraphic;
  currentTime: number;
  width: number;
  height: number;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    pointerEvents: 'none',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  focusCircle: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  ring: {
    position: 'absolute',
    borderRadius: '50%',
    border: '2px solid',
  },
  innerRing: {
    borderWidth: '2px',
  },
  outerRing: {
    borderWidth: '3px',
  },
  pulseRing: {
    position: 'absolute',
    borderRadius: '50%',
    border: '2px solid',
  },
  label: {
    position: 'absolute',
    top: '100%',
    marginTop: '12px',
    whiteSpace: 'nowrap',
    fontWeight: 600,
    textAlign: 'center',
  },
  arrow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  arrowLine: {
    height: '3px',
    transformOrigin: 'left center',
  },
  arrowHead: {
    width: 0,
    height: 0,
    borderTop: '8px solid transparent',
    borderBottom: '8px solid transparent',
    borderLeft: '12px solid',
  },
  tooltip: {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  tooltipBox: {
    padding: '12px 16px',
    borderRadius: '8px',
    maxWidth: '200px',
    textAlign: 'center',
  },
  tooltipPointer: {
    width: 0,
    height: 0,
    borderLeft: '8px solid transparent',
    borderRight: '8px solid transparent',
    borderTop: '10px solid',
  },
});

export const CalloutRenderer: FC<CalloutRendererProps> = ({
  graphic,
  currentTime,
  width,
  height,
}) => {
  const styles = useStyles();
  const graphicsStore = useMotionGraphicsStore();
  const asset = graphicsStore.getAsset(graphic.assetId);

  // Calculate animation state
  const animationState = useMemo(() => {
    const relativeTime = currentTime - graphic.startTime;
    const enterDuration = 0.4;
    const exitDuration = 0.3;
    const holdDuration = graphic.duration - enterDuration - exitDuration;

    if (relativeTime < 0 || relativeTime > graphic.duration) {
      return { visible: false, phase: 'hidden' as const, progress: 0 };
    }

    if (relativeTime < enterDuration) {
      return { visible: true, phase: 'enter' as const, progress: relativeTime / enterDuration };
    }
    if (relativeTime < enterDuration + holdDuration) {
      return { visible: true, phase: 'hold' as const, progress: 1 };
    }
    const exitProgress = (relativeTime - enterDuration - holdDuration) / exitDuration;
    return { visible: true, phase: 'exit' as const, progress: Math.min(1, exitProgress) };
  }, [currentTime, graphic]);

  if (!asset || !animationState.visible) {
    return null;
  }

  // Get customization values
  const color = (graphic.customValues.color as string) || '#3B82F6';
  const label = (graphic.customValues.label as string) || '';
  const showLabel = graphic.customValues.showLabel !== false;

  // Calculate position
  const posX = ((graphic.positionX ?? 50) / 100) * width;
  const posY = ((graphic.positionY ?? 50) / 100) * height;
  const scale = Math.min(width / 1920, height / 1080);

  // Render based on asset type
  if (asset.id.includes('focus-circle')) {
    return (
      <motion.div
        className={styles.container}
        style={{ left: posX, top: posY, transform: 'translate(-50%, -50%)' }}
        initial={{ opacity: 0, scale: 0.5 }}
        animate={{
          opacity: animationState.phase === 'exit' ? 0 : 1,
          scale: animationState.phase === 'enter' ? 1 : animationState.phase === 'exit' ? 0.5 : 1,
        }}
        transition={{ duration: 0.3, ease: [0.25, 1, 0.5, 1] }}
      >
        <div className={styles.focusCircle}>
          {/* Outer ring */}
          <motion.div
            className={`${styles.ring} ${styles.outerRing}`}
            style={{
              width: 80 * scale,
              height: 80 * scale,
              borderColor: color,
            }}
            animate={{ scale: [1, 1.1, 1] }}
            transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
          />
          {/* Inner ring */}
          <motion.div
            className={`${styles.ring} ${styles.innerRing}`}
            style={{
              width: 50 * scale,
              height: 50 * scale,
              borderColor: color,
              backgroundColor: `${color}33`,
            }}
          />
          {/* Pulse ring */}
          <motion.div
            className={styles.pulseRing}
            style={{
              width: 60 * scale,
              height: 60 * scale,
              borderColor: color,
            }}
            animate={{ scale: [1, 1.5], opacity: [0.5, 0] }}
            transition={{ duration: 1.5, repeat: Infinity, ease: 'easeOut' }}
          />
          {/* Label */}
          {showLabel && label && (
            <motion.div
              className={styles.label}
              style={{ color, fontSize: 14 * scale }}
              initial={{ opacity: 0, y: -5 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.2, duration: 0.3 }}
            >
              {label}
            </motion.div>
          )}
        </div>
      </motion.div>
    );
  }

  if (asset.id.includes('arrow-pointer')) {
    const direction = (graphic.customValues.direction as string) || 'right';
    const rotation =
      direction === 'left' ? 180 : direction === 'up' ? -90 : direction === 'down' ? 90 : 0;

    return (
      <motion.div
        className={styles.container}
        style={{
          left: posX,
          top: posY,
          transform: `translate(-50%, -50%) rotate(${rotation}deg)`,
        }}
        initial={{ opacity: 0, x: -20 }}
        animate={{
          opacity: animationState.phase === 'exit' ? 0 : 1,
          x: 0,
        }}
        transition={{ duration: 0.3, ease: [0.25, 1, 0.5, 1] }}
      >
        <div className={styles.arrow}>
          <motion.div
            className={styles.arrowLine}
            style={{ backgroundColor: color, width: 60 * scale }}
            initial={{ scaleX: 0 }}
            animate={{ scaleX: 1 }}
            transition={{ duration: 0.4, ease: [0.25, 1, 0.5, 1] }}
          />
          <motion.div
            className={styles.arrowHead}
            style={{ borderLeftColor: color }}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.3, duration: 0.2 }}
          />
        </div>
        {showLabel && label && (
          <motion.div
            style={{
              position: 'absolute',
              top: '120%',
              left: '50%',
              transform: `translateX(-50%) rotate(${-rotation}deg)`,
              color,
              fontSize: 14 * scale,
              fontWeight: 600,
              whiteSpace: 'nowrap',
            }}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.4, duration: 0.3 }}
          >
            {label}
          </motion.div>
        )}
      </motion.div>
    );
  }

  if (asset.id.includes('tooltip')) {
    const text = (graphic.customValues.text as string) || 'Tooltip text';
    const bgColor = (graphic.customValues.bgColor as string) || '#1F2937';
    const textColor = (graphic.customValues.textColor as string) || '#ffffff';

    return (
      <motion.div
        className={styles.container}
        style={{ left: posX, top: posY, transform: 'translate(-50%, -100%)' }}
        initial={{ opacity: 0, y: 10 }}
        animate={{
          opacity: animationState.phase === 'exit' ? 0 : 1,
          y: 0,
        }}
        transition={{ duration: 0.3, ease: [0.25, 1, 0.5, 1] }}
      >
        <div className={styles.tooltip}>
          <div
            className={styles.tooltipBox}
            style={{
              backgroundColor: bgColor,
              color: textColor,
              fontSize: 14 * scale,
            }}
          >
            {text}
          </div>
          <div className={styles.tooltipPointer} style={{ borderTopColor: bgColor }} />
        </div>
      </motion.div>
    );
  }

  return null;
};

export default CalloutRenderer;
