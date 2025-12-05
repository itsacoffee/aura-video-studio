/**
 * TitleRenderer Component
 *
 * Specialized renderer for animated title graphics including
 * cinematic effects, glitch effects, and elegant animations.
 */

import { makeStyles } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type { AppliedGraphic } from '../../../types/motionGraphics';

export interface TitleRendererProps {
  graphic: AppliedGraphic;
  currentTime: number;
  width: number;
  height: number;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    inset: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    pointerEvents: 'none',
    overflow: 'hidden',
  },
  titleGroup: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '16px',
    textAlign: 'center',
  },
  mainTitle: {
    fontWeight: 800,
    letterSpacing: '0.1em',
    textTransform: 'uppercase',
    textShadow: '0 4px 12px rgba(0,0,0,0.5)',
  },
  subtitle: {
    fontWeight: 400,
    letterSpacing: '0.2em',
  },
  decorativeLine: {
    height: '1px',
    backgroundColor: 'currentColor',
    opacity: 0.5,
  },
  glitchWrapper: {
    position: 'relative',
  },
  glitchText: {
    position: 'relative',
  },
  glitchLayer: {
    position: 'absolute',
    inset: 0,
    pointerEvents: 'none',
  },
  elegantTitle: {
    fontFamily: 'Georgia, "Times New Roman", serif',
    letterSpacing: '0.05em',
    textShadow: '0 2px 8px rgba(0,0,0,0.3)',
  },
  elegantSubtitle: {
    fontFamily: 'Georgia, "Times New Roman", serif',
    letterSpacing: '0.15em',
  },
  decorativeWrapper: {
    display: 'flex',
    alignItems: 'center',
    gap: '20px',
    marginTop: '8px',
  },
});

export const TitleRenderer: FC<TitleRendererProps> = ({ graphic, currentTime, width, height }) => {
  const styles = useStyles();
  const graphicsStore = useMotionGraphicsStore();
  const asset = graphicsStore.getAsset(graphic.assetId);

  // Calculate animation state
  const animationState = useMemo(() => {
    const relativeTime = currentTime - graphic.startTime;
    const enterDuration = 0.8;
    const exitDuration = 0.5;
    const holdDuration = graphic.duration - enterDuration - exitDuration;

    if (relativeTime < 0 || relativeTime > graphic.duration) {
      return { visible: false, phase: 'hidden' as const, progress: 0, relativeTime: 0 };
    }

    if (relativeTime < enterDuration) {
      return {
        visible: true,
        phase: 'enter' as const,
        progress: relativeTime / enterDuration,
        relativeTime,
      };
    }
    if (relativeTime < enterDuration + holdDuration) {
      return { visible: true, phase: 'hold' as const, progress: 1, relativeTime };
    }
    const exitProgress = (relativeTime - enterDuration - holdDuration) / exitDuration;
    return {
      visible: true,
      phase: 'exit' as const,
      progress: Math.min(1, exitProgress),
      relativeTime,
    };
  }, [currentTime, graphic]);

  if (!asset || !animationState.visible) {
    return null;
  }

  const scale = Math.min(width / 1920, height / 1080);

  // Cinematic reveal title
  if (asset.id.includes('cinematic-reveal')) {
    const title = (graphic.customValues.title as string) || 'EPIC TITLE';
    const subtitle = (graphic.customValues.subtitle as string) || 'A Compelling Subtitle';
    const titleColor = (graphic.customValues.titleColor as string) || '#ffffff';

    const titleSize = 72 * scale;
    const subtitleSize = 18 * scale;

    return (
      <motion.div
        className={styles.container}
        initial={{ opacity: 0 }}
        animate={{ opacity: animationState.phase === 'exit' ? 0 : 1 }}
        transition={{ duration: 0.5 }}
      >
        <div className={styles.titleGroup}>
          {/* Main Title */}
          <motion.div
            className={styles.mainTitle}
            style={{ color: titleColor, fontSize: titleSize }}
            initial={{ opacity: 0, y: 50, scale: 0.9 }}
            animate={{
              opacity: animationState.phase === 'exit' ? 0 : 1,
              y: 0,
              scale: 1,
            }}
            transition={{
              duration: 0.8,
              ease: [0.25, 1, 0.5, 1],
            }}
          >
            {title.split('').map((char, i) => (
              <motion.span
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.03, duration: 0.4 }}
              >
                {char}
              </motion.span>
            ))}
          </motion.div>

          {/* Subtitle */}
          <motion.div
            className={styles.subtitle}
            style={{ color: '#a1a1aa', fontSize: subtitleSize }}
            initial={{ opacity: 0, y: 20 }}
            animate={{
              opacity: animationState.phase === 'exit' ? 0 : 1,
              y: 0,
            }}
            transition={{ delay: 0.5, duration: 0.5 }}
          >
            {subtitle}
          </motion.div>
        </div>
      </motion.div>
    );
  }

  // Glitch distort title
  if (asset.id.includes('glitch-distort')) {
    const title = (graphic.customValues.title as string) || 'GLITCH';
    const color = (graphic.customValues.color as string) || '#ffffff';
    const intensity = (graphic.customValues.glitchIntensity as number) || 50;

    const titleSize = 80 * scale;
    const glitchOffset = (intensity / 100) * 5;

    // Generate random glitch offsets
    const glitchX = Math.sin(animationState.relativeTime * 50) * glitchOffset;
    const glitchY = Math.cos(animationState.relativeTime * 50) * glitchOffset * 0.5;

    return (
      <motion.div
        className={styles.container}
        initial={{ opacity: 0 }}
        animate={{ opacity: animationState.phase === 'exit' ? 0 : 1 }}
        transition={{ duration: 0.3 }}
      >
        <div className={styles.glitchWrapper}>
          {/* Red layer */}
          <motion.div
            className={styles.glitchLayer}
            style={{
              color: '#ff0000',
              fontSize: titleSize,
              fontWeight: 900,
              letterSpacing: '0.1em',
              mixBlendMode: 'screen',
            }}
            animate={{
              x: [0, glitchX, -glitchX, 0],
              opacity: [0.8, 0.9, 0.7, 0.8],
            }}
            transition={{ duration: 0.1, repeat: Infinity }}
          >
            {title}
          </motion.div>

          {/* Cyan layer */}
          <motion.div
            className={styles.glitchLayer}
            style={{
              color: '#00ffff',
              fontSize: titleSize,
              fontWeight: 900,
              letterSpacing: '0.1em',
              mixBlendMode: 'screen',
            }}
            animate={{
              x: [0, -glitchX, glitchX, 0],
              opacity: [0.8, 0.7, 0.9, 0.8],
            }}
            transition={{ duration: 0.1, repeat: Infinity }}
          >
            {title}
          </motion.div>

          {/* Main text */}
          <motion.div
            className={styles.glitchText}
            style={{
              color,
              fontSize: titleSize,
              fontWeight: 900,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
            }}
            animate={{
              x: [0, glitchX * 0.5, -glitchX * 0.5, 0],
              y: [0, glitchY, -glitchY, 0],
            }}
            transition={{ duration: 0.15, repeat: Infinity }}
          >
            {title}
          </motion.div>
        </div>
      </motion.div>
    );
  }

  // Elegant serif title
  if (asset.id.includes('elegant-serif')) {
    const title = (graphic.customValues.title as string) || 'Beautiful Moments';
    const date = (graphic.customValues.date as string) || 'December 14, 2024';
    const accentColor = (graphic.customValues.accentColor as string) || '#D4AF37';

    const titleSize = 56 * scale;
    const dateSize = 20 * scale;
    const lineWidth = 80 * scale;

    return (
      <motion.div
        className={styles.container}
        initial={{ opacity: 0 }}
        animate={{ opacity: animationState.phase === 'exit' ? 0 : 1 }}
        transition={{ duration: 0.5 }}
      >
        <div className={styles.titleGroup}>
          {/* Main Title */}
          <motion.div
            className={styles.elegantTitle}
            style={{ color: '#ffffff', fontSize: titleSize }}
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8, ease: [0.25, 1, 0.5, 1] }}
          >
            {title}
          </motion.div>

          {/* Decorative lines and date */}
          <motion.div
            className={styles.decorativeWrapper}
            initial={{ opacity: 0, scaleX: 0 }}
            animate={{ opacity: 1, scaleX: 1 }}
            transition={{ delay: 0.4, duration: 0.6 }}
          >
            <div
              className={styles.decorativeLine}
              style={{ width: lineWidth, backgroundColor: accentColor }}
            />
            <motion.div
              className={styles.elegantSubtitle}
              style={{ color: accentColor, fontSize: dateSize }}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.6, duration: 0.4 }}
            >
              {date}
            </motion.div>
            <div
              className={styles.decorativeLine}
              style={{ width: lineWidth, backgroundColor: accentColor }}
            />
          </motion.div>
        </div>
      </motion.div>
    );
  }

  return null;
};

export default TitleRenderer;
