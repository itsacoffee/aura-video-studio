import type { FC } from 'react';
import './AnimatedIcon.css';

interface AnimatedIconProps {
  size?: number;
  variant?: 'default' | 'fire' | 'ice' | 'energy';
  animated?: boolean;
  className?: string;
}

/**
 * Animated Icon Component with Fire/Ice Effects
 * Inspired by ui2.png mock with premium visual effects
 * Features:
 * - Fire effect (warm orange glow with particles)
 * - Ice effect (cool blue crystalline glow)
 * - Energy effect (purple lightning with brand gradient)
 * - Smooth animations and particle systems
 * - Respects prefers-reduced-motion
 */
export const AnimatedIcon: FC<AnimatedIconProps> = ({
  size = 120,
  variant = 'default',
  animated = true,
  className = '',
}) => {
  const classes = [
    'animated-icon',
    `animated-icon--${variant}`,
    animated && 'animated-icon--active',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes} style={{ width: size, height: size }}>
      {/* Background glow effects */}
      <div className="animated-icon__glow animated-icon__glow--outer" />
      <div className="animated-icon__glow animated-icon__glow--inner" />

      {/* Particle effects */}
      {animated && variant === 'fire' && (
        <div className="animated-icon__particles animated-icon__particles--fire">
          {Array.from({ length: 12 }).map((_, i) => (
            <div
              key={i}
              className="animated-icon__particle"
              style={{
                left: `${45 + Math.random() * 10}%`,
                animationDelay: `${Math.random() * 2}s`,
                animationDuration: `${2 + Math.random()}s`,
              }}
            />
          ))}
        </div>
      )}

      {animated && variant === 'ice' && (
        <div className="animated-icon__particles animated-icon__particles--ice">
          {Array.from({ length: 8 }).map((_, i) => (
            <div
              key={i}
              className="animated-icon__particle animated-icon__particle--ice"
              style={{
                left: `${20 + Math.random() * 60}%`,
                top: `${20 + Math.random() * 60}%`,
                animationDelay: `${Math.random() * 3}s`,
              }}
            />
          ))}
        </div>
      )}

      {animated && variant === 'energy' && (
        <>
          <div className="animated-icon__lightning animated-icon__lightning--1" />
          <div className="animated-icon__lightning animated-icon__lightning--2" />
          <div className="animated-icon__lightning animated-icon__lightning--3" />
        </>
      )}

      {/* Main icon SVG */}
      <svg
        className="animated-icon__svg"
        viewBox="0 0 120 120"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <defs>
          <linearGradient id={`iconGradient-${variant}`} x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#ff6b35" />
            <stop offset="50%" stopColor="#6366F1" />
            <stop offset="100%" stopColor="#3b82f6" />
          </linearGradient>
          <filter id="iconGlow">
            <feGaussianBlur stdDeviation="4" result="coloredBlur" />
            <feMerge>
              <feMergeNode in="coloredBlur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* Outer decorative ring */}
        <circle
          cx="60"
          cy="60"
          r="56"
          fill="none"
          stroke={`url(#iconGradient-${variant})`}
          strokeWidth="2"
          opacity="0.3"
          className="animated-icon__ring"
        />

        {/* Main icon shape */}
        <path
          d="M 60 20 L 90 50 L 90 70 L 60 90 L 30 70 L 30 50 Z"
          fill={`url(#iconGradient-${variant})`}
          filter="url(#iconGlow)"
          className="animated-icon__shape"
        />

        {/* Inner cutout */}
        <path d="M 45 55 L 60 45 L 75 55 L 75 65 L 60 75 L 45 65 Z" fill="#0F0F0F" />

        {/* Variant-specific overlays */}
        {variant === 'fire' && (
          <>
            <path
              d="M 60 35 L 70 50 L 70 60 L 60 70 L 50 60 L 50 50 Z"
              fill="#FF6B35"
              opacity="0.4"
              className="animated-icon__flame"
            />
            <circle cx="60" cy="50" r="8" fill="#FF8960" opacity="0.6" />
          </>
        )}

        {variant === 'ice' && (
          <>
            <path
              d="M 60 30 L 65 45 L 60 50 L 55 45 Z"
              fill="#3B82F6"
              opacity="0.5"
              className="animated-icon__crystal animated-icon__crystal--1"
            />
            <path
              d="M 70 55 L 75 60 L 70 70 L 65 60 Z"
              fill="#60A5FA"
              opacity="0.5"
              className="animated-icon__crystal animated-icon__crystal--2"
            />
            <path
              d="M 45 60 L 50 55 L 55 70 L 45 65 Z"
              fill="#93C5FD"
              opacity="0.5"
              className="animated-icon__crystal animated-icon__crystal--3"
            />
          </>
        )}

        {variant === 'energy' && (
          <>
            <circle cx="60" cy="40" r="6" fill="#6366F1" opacity="0.8" />
            <circle cx="70" cy="60" r="5" fill="#818CF8" opacity="0.6" />
            <circle cx="50" cy="60" r="5" fill="#818CF8" opacity="0.6" />
            <circle cx="60" cy="70" r="4" fill="#A78BFA" opacity="0.5" />
          </>
        )}
      </svg>
    </div>
  );
};
