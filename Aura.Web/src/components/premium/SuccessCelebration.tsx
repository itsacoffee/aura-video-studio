/**
 * SuccessCelebration Component
 * Confetti/sparkle animation for successful actions
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { motion, AnimatePresence } from 'framer-motion';
import { useEffect, useState, useCallback, useMemo } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  container: {
    position: 'fixed',
    inset: '0',
    pointerEvents: 'none',
    zIndex: 9999,
    overflow: 'hidden',
  },
  particle: {
    position: 'absolute',
    width: '10px',
    height: '10px',
    borderRadius: '50%',
  },
});

interface Particle {
  id: number;
  x: number;
  y: number;
  color: string;
  size: number;
  delay: number;
  targetX: number;
  targetY: number;
  rotation: number;
}

interface SuccessCelebrationProps {
  trigger: boolean;
  onComplete?: () => void;
  particleCount?: number;
  colors?: string[];
  originX?: number;
  originY?: number;
}

const DEFAULT_COLORS = [
  tokens.colorPaletteGreenBackground3,
  tokens.colorPaletteBlueBackground2,
  tokens.colorPaletteYellowBackground3,
  tokens.colorPalettePurpleBackground2,
];

export function SuccessCelebration({
  trigger,
  onComplete,
  particleCount = 30,
  colors = DEFAULT_COLORS,
  originX = 50,
  originY = 50,
}: SuccessCelebrationProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();
  const [particles, setParticles] = useState<Particle[]>([]);
  const [isActive, setIsActive] = useState(false);

  const generateParticles = useCallback(() => {
    const newParticles: Particle[] = Array.from({ length: particleCount }, (_, i) => ({
      id: i,
      x: originX,
      y: originY,
      color: colors[i % colors.length],
      size: Math.random() * 8 + 4,
      delay: Math.random() * 0.2,
      targetX: originX + (Math.random() - 0.5) * 60,
      targetY: originY + (Math.random() - 0.5) * 60,
      rotation: Math.random() * 360,
    }));
    return newParticles;
  }, [particleCount, colors, originX, originY]);

  useEffect(() => {
    if (trigger && animationsEnabled && settings.effects.microInteractions) {
      const newParticles = generateParticles();
      setParticles(newParticles);
      setIsActive(true);

      const timer = setTimeout(() => {
        setIsActive(false);
        setParticles([]);
        onComplete?.();
      }, 2000);

      return () => clearTimeout(timer);
    }
  }, [
    trigger,
    animationsEnabled,
    settings.effects.microInteractions,
    generateParticles,
    onComplete,
  ]);

  const memoizedParticles = useMemo(() => particles, [particles]);

  if (!animationsEnabled || !settings.effects.microInteractions) {
    return null;
  }

  return (
    <AnimatePresence>
      {isActive && (
        <div className={styles.container}>
          {memoizedParticles.map((particle) => (
            <motion.div
              key={particle.id}
              className={styles.particle}
              style={{
                backgroundColor: particle.color,
                width: particle.size,
                height: particle.size,
              }}
              initial={{
                left: `${particle.x}%`,
                top: `${particle.y}%`,
                scale: 0,
                opacity: 1,
              }}
              animate={{
                left: `${particle.targetX}%`,
                top: `${particle.targetY}%`,
                scale: [0, 1.5, 1],
                opacity: [1, 1, 0],
                rotate: particle.rotation,
              }}
              exit={{ opacity: 0 }}
              transition={{
                duration: 1.5,
                delay: particle.delay,
                ease: [0.4, 0, 0.2, 1],
              }}
            />
          ))}
        </div>
      )}
    </AnimatePresence>
  );
}
