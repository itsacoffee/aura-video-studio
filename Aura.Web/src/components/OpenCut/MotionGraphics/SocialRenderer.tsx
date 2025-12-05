/**
 * SocialRenderer Component
 *
 * Specialized renderer for social media graphics including
 * button animations, icon rendering, and particle effects.
 */

import { makeStyles } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type { AppliedGraphic } from '../../../types/motionGraphics';

export interface SocialRendererProps {
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
  subscribeButton: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '8px',
    padding: '12px 24px',
    borderRadius: '4px',
    fontWeight: 700,
    textTransform: 'uppercase',
    letterSpacing: '1px',
    color: '#ffffff',
    cursor: 'pointer',
  },
  bellIcon: {
    fontSize: '20px',
  },
  heartContainer: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  particle: {
    position: 'absolute',
    width: '6px',
    height: '6px',
    borderRadius: '50%',
  },
  followBadge: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '10px',
    padding: '14px 28px',
    borderRadius: '999px',
    fontWeight: 600,
    color: '#ffffff',
  },
});

// SVG Heart path
const HeartSvg: FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill={color}
    xmlns="http://www.w3.org/2000/svg"
  >
    <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
  </svg>
);

// Bell Icon SVG
const BellSvg: FC<{ size: number; color: string }> = ({ size, color }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
    <path d="M13.73 21a2 2 0 0 1-3.46 0" />
  </svg>
);

// Platform colors
const platformColors: Record<string, string> = {
  twitter: '#1DA1F2',
  instagram: '#E4405F',
  tiktok: '#000000',
  youtube: '#FF0000',
  linkedin: '#0A66C2',
};

export const SocialRenderer: FC<SocialRendererProps> = ({
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
    const enterDuration = 0.3;
    const exitDuration = 0.2;
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

  // Calculate position and scale (moved outside conditionals)
  const posX = ((graphic.positionX ?? 50) / 100) * width;
  const posY = ((graphic.positionY ?? 50) / 100) * height;
  const scale = Math.min(width / 1920, height / 1080);

  // Get customization values for all types (moved outside conditionals)
  const heartColor = (graphic.customValues.color as string) || '#EF4444';

  // Generate particles for heart animation (moved outside conditionals - always computed)
  const particles = useMemo(() => {
    return Array.from({ length: 8 }, (_, i) => ({
      id: i,
      angle: (i * 45 * Math.PI) / 180,
      delay: i * 0.05,
      color: i % 2 === 0 ? heartColor : '#FF69B4',
    }));
  }, [heartColor]);

  if (!asset || !animationState.visible) {
    return null;
  }

  // Subscribe button
  if (asset.id.includes('subscribe')) {
    const buttonText = (graphic.customValues.text as string) || 'SUBSCRIBE';
    const buttonColor = (graphic.customValues.buttonColor as string) || '#EF4444';
    const showBell = graphic.customValues.showBell !== false;

    return (
      <motion.div
        className={styles.container}
        style={{ left: posX, top: posY, transform: 'translate(-50%, -50%)' }}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{
          opacity: animationState.phase === 'exit' ? 0 : 1,
          scale:
            animationState.phase === 'enter'
              ? [0.8, 1.1, 1]
              : animationState.phase === 'exit'
                ? 0.8
                : 1,
        }}
        transition={{ duration: 0.3, ease: [0.34, 1.56, 0.64, 1] }}
      >
        <motion.div
          className={styles.subscribeButton}
          style={{
            backgroundColor: buttonColor,
            fontSize: 16 * scale,
            padding: `${12 * scale}px ${24 * scale}px`,
          }}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
        >
          {buttonText}
          {showBell && (
            <motion.span
              className={styles.bellIcon}
              animate={{ rotate: [0, 15, -15, 10, -10, 0] }}
              transition={{ delay: 0.5, duration: 0.5, repeat: Infinity, repeatDelay: 2 }}
            >
              <BellSvg size={20 * scale} color="#ffffff" />
            </motion.span>
          )}
        </motion.div>
      </motion.div>
    );
  }

  // Like heart
  if (asset.id.includes('like-heart')) {
    const showParticles = graphic.customValues.showParticles !== false;

    const heartSize = 60 * scale;
    const particleDistance = 50 * scale;

    return (
      <motion.div
        className={styles.container}
        style={{ left: posX, top: posY, transform: 'translate(-50%, -50%)' }}
        initial={{ opacity: 0 }}
        animate={{ opacity: animationState.phase === 'exit' ? 0 : 1 }}
        transition={{ duration: 0.2 }}
      >
        <div className={styles.heartContainer}>
          <motion.div
            initial={{ scale: 0 }}
            animate={{
              scale: animationState.phase === 'enter' ? [0, 1.3, 1] : 1,
            }}
            transition={{ duration: 0.4, ease: [0.34, 1.56, 0.64, 1] }}
          >
            <HeartSvg size={heartSize} color={heartColor} />
          </motion.div>

          {/* Particles */}
          {showParticles &&
            animationState.phase !== 'hidden' &&
            particles.map((particle) => (
              <motion.div
                key={particle.id}
                className={styles.particle}
                style={{ backgroundColor: particle.color }}
                initial={{ opacity: 0, x: 0, y: 0 }}
                animate={{
                  opacity: [0, 1, 0],
                  x: Math.cos(particle.angle) * particleDistance,
                  y: Math.sin(particle.angle) * particleDistance,
                }}
                transition={{
                  duration: 0.6,
                  delay: 0.2 + particle.delay,
                  ease: 'easeOut',
                }}
              />
            ))}
        </div>
      </motion.div>
    );
  }

  // Follow badge
  if (asset.id.includes('follow-badge')) {
    const handle = (graphic.customValues.handle as string) || '@username';
    const platform = (graphic.customValues.platform as string) || 'twitter';
    const badgeColor =
      (graphic.customValues.badgeColor as string) || platformColors[platform] || '#1DA1F2';

    return (
      <motion.div
        className={styles.container}
        style={{ left: posX, top: posY, transform: 'translate(-50%, -50%)' }}
        initial={{ opacity: 0, y: 20 }}
        animate={{
          opacity: animationState.phase === 'exit' ? 0 : 1,
          y: animationState.phase === 'exit' ? 20 : 0,
        }}
        transition={{ duration: 0.4, ease: [0.25, 1, 0.5, 1] }}
      >
        <div
          className={styles.followBadge}
          style={{
            backgroundColor: badgeColor,
            fontSize: 18 * scale,
            padding: `${14 * scale}px ${28 * scale}px`,
          }}
        >
          {handle}
        </div>
      </motion.div>
    );
  }

  return null;
};

export default SocialRenderer;
