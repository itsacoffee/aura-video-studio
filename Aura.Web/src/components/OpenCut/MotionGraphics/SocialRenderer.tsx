/**
 * SocialRenderer Component
 *
 * Specialized renderer for social media graphics including
 * subscribe buttons, like hearts, and follow badges.
 */

import { makeStyles } from '@fluentui/react-components';
import { useMemo } from 'react';
import type { FC } from 'react';
import type {
  MotionGraphicAsset,
  AppliedGraphic,
  AnimationState,
} from '../../../types/motionGraphics';
import { evaluateEasing } from '../../../utils/motionGraphicsAnimation';

export interface SocialRendererProps {
  /** The motion graphic asset definition */
  asset: MotionGraphicAsset;
  /** The applied graphic instance with customizations */
  graphic: AppliedGraphic;
  /** Current animation state */
  animationState: AnimationState;
  /** Canvas width */
  width: number;
  /** Canvas height */
  height: number;
}

const useStyles = makeStyles({
  subscribeButton: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '8px',
    padding: '12px 24px',
    borderRadius: '24px',
    fontSize: '16px',
    fontWeight: 700,
    color: '#FFFFFF',
    cursor: 'pointer',
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.3)',
  },
  bellIcon: {
    width: '20px',
    height: '20px',
  },
  heart: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  heartShape: {
    fontSize: '48px',
    lineHeight: 1,
  },
  particle: {
    position: 'absolute',
    width: '6px',
    height: '6px',
    borderRadius: '50%',
    pointerEvents: 'none',
  },
  badge: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '12px 20px',
    borderRadius: '28px',
    backgroundColor: 'rgba(0, 0, 0, 0.85)',
    boxShadow: '0 4px 16px rgba(0, 0, 0, 0.3)',
  },
  platformIcon: {
    width: '36px',
    height: '36px',
    borderRadius: '50%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: '#FFFFFF',
    fontSize: '18px',
  },
  handle: {
    fontSize: '18px',
    fontWeight: 600,
    color: '#FFFFFF',
  },
});

/**
 * Get platform icon and color
 */
function getPlatformConfig(platform: string): { icon: string; color: string } {
  switch (platform) {
    case 'instagram':
      return { icon: '📷', color: '#E1306C' };
    case 'twitter':
      return { icon: '𝕏', color: '#000000' };
    case 'tiktok':
      return { icon: '♪', color: '#FF0050' };
    case 'linkedin':
      return { icon: 'in', color: '#0077B5' };
    case 'youtube':
      return { icon: '▶', color: '#FF0000' };
    default:
      return { icon: '@', color: '#E1306C' };
  }
}

export const SocialRenderer: FC<SocialRendererProps> = ({ asset, graphic, animationState }) => {
  const styles = useStyles();

  // Get all customization values at the top
  const buttonColor = String(graphic.customValues['buttonColor'] ?? '#FF0000');
  const buttonText = String(graphic.customValues['buttonText'] ?? 'SUBSCRIBE');
  const showBell = graphic.customValues['showBell'] !== false;
  const heartColor = String(graphic.customValues['heartColor'] ?? '#EF4444');
  const size = Number(graphic.customValues['size'] ?? 60);
  const showParticles = graphic.customValues['showParticles'] !== false;
  const particleCount = Number(graphic.customValues['particleCount'] ?? 12);
  const handle = String(graphic.customValues['handle'] ?? '@yourhandle');
  const platform = String(graphic.customValues['platform'] ?? 'instagram');
  const platformColor = String(
    graphic.customValues['platformColor'] ?? getPlatformConfig(platform).color
  );
  const platformConfig = getPlatformConfig(platform);

  // Calculate animation - always called at top level
  const eased = evaluateEasing('easeOutBack', animationState.progress);

  const animatedStyle = useMemo((): React.CSSProperties => {
    if (animationState.phase === 'entry') {
      return {
        transform: `scale(${eased})`,
        opacity: Math.min(1, eased * 1.5),
      };
    }
    if (animationState.phase === 'exit') {
      return {
        transform: `scale(${animationState.progress})`,
        opacity: animationState.progress,
      };
    }
    return {
      transform: 'scale(1)',
      opacity: 1,
    };
  }, [animationState, eased]);

  // Generate particle positions - always called at top level
  const particles = useMemo(() => {
    if (!showParticles || animationState.phase !== 'hold') return [];
    const result = [];
    for (let i = 0; i < particleCount; i++) {
      const angle = (i / particleCount) * Math.PI * 2;
      const radius = size * 0.8 + Math.random() * 20;
      result.push({
        x: Math.cos(angle) * radius,
        y: Math.sin(angle) * radius,
        delay: i * 0.02,
        color: heartColor,
      });
    }
    return result;
  }, [showParticles, particleCount, size, heartColor, animationState.phase]);

  // Subscribe Button
  if (asset.id.includes('subscribe')) {
    return (
      <div
        className={styles.subscribeButton}
        style={{
          ...animatedStyle,
          backgroundColor: buttonColor,
        }}
      >
        {buttonText}
        {showBell && <span className={styles.bellIcon}>🔔</span>}
      </div>
    );
  }

  // Like Heart
  if (asset.id.includes('heart') || asset.id.includes('like')) {
    return (
      <div className={styles.heart} style={animatedStyle}>
        {/* Particles */}
        {particles.map((p, i) => (
          <div
            key={i}
            className={styles.particle}
            style={{
              backgroundColor: p.color,
              transform: `translate(${p.x}px, ${p.y}px)`,
              opacity: 0.6,
              animationDelay: `${p.delay}s`,
            }}
          />
        ))}
        {/* Heart */}
        <span
          className={styles.heartShape}
          style={{
            color: heartColor,
            fontSize: size,
          }}
        >
          ❤️
        </span>
      </div>
    );
  }

  // Follow Badge
  if (asset.id.includes('badge') || asset.id.includes('follow')) {
    return (
      <div className={styles.badge} style={animatedStyle}>
        <div className={styles.platformIcon} style={{ backgroundColor: platformColor }}>
          {platformConfig.icon}
        </div>
        <span className={styles.handle}>{handle}</span>
      </div>
    );
  }

  // Default fallback
  return (
    <div style={animatedStyle}>
      <span style={{ fontSize: 24, color: '#FFFFFF' }}>🌟</span>
    </div>
  );
};

export default SocialRenderer;
