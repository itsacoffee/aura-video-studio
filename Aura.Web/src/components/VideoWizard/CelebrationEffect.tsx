import { makeStyles, tokens } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    pointerEvents: 'none',
    zIndex: 9999,
    overflow: 'hidden',
  },
  confetti: {
    position: 'absolute',
    width: '10px',
    height: '10px',
    opacity: 0,
  },
  successPulse: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: '100px',
    height: '100px',
    borderRadius: '50%',
    backgroundColor: tokens.colorPaletteGreenBackground2,
    opacity: 0,
    animation: 'successPulse 0.8s ease-out',
  },
  '@keyframes successPulse': {
    '0%': {
      transform: 'translate(-50%, -50%) scale(0)',
      opacity: 0.8,
    },
    '100%': {
      transform: 'translate(-50%, -50%) scale(3)',
      opacity: 0,
    },
  },
  '@keyframes confettiFall': {
    '0%': {
      transform: 'translateY(-100vh) rotate(0deg)',
      opacity: 1,
    },
    '100%': {
      transform: 'translateY(100vh) rotate(720deg)',
      opacity: 0,
    },
  },
});

interface ConfettiParticle {
  id: number;
  left: number;
  color: string;
  delay: number;
  duration: number;
  size: number;
}

interface CelebrationEffectProps {
  show: boolean;
  onComplete?: () => void;
  type?: 'confetti' | 'pulse' | 'both';
  duration?: number;
}

export const CelebrationEffect: FC<CelebrationEffectProps> = ({
  show,
  onComplete,
  type = 'both',
  duration = 3000,
}) => {
  const styles = useStyles();
  const [confettiParticles, setConfettiParticles] = useState<ConfettiParticle[]>([]);
  const [showEffect, setShowEffect] = useState(false);

  useEffect(() => {
    if (show) {
      setShowEffect(true);

      // Generate confetti particles
      if (type === 'confetti' || type === 'both') {
        const colors = [
          tokens.colorPaletteBlueForeground2,
          tokens.colorPaletteGreenForeground1,
          tokens.colorPaletteYellowForeground1,
          tokens.colorPaletteRedForeground1,
          tokens.colorPalettePurpleForeground2,
        ];

        const particles: ConfettiParticle[] = [];
        for (let i = 0; i < 50; i++) {
          particles.push({
            id: i,
            left: Math.random() * 100,
            color: colors[Math.floor(Math.random() * colors.length)],
            delay: Math.random() * 500,
            duration: 2000 + Math.random() * 1000,
            size: 8 + Math.random() * 6,
          });
        }
        setConfettiParticles(particles);
      }

      // Clear effect after duration
      const timer = setTimeout(() => {
        setShowEffect(false);
        setConfettiParticles([]);
        onComplete?.();
      }, duration);

      return () => clearTimeout(timer);
    }
  }, [show, type, duration, onComplete]);

  if (!showEffect) return null;

  return (
    <div className={styles.container}>
      {(type === 'pulse' || type === 'both') && <div className={styles.successPulse} />}

      {(type === 'confetti' || type === 'both') &&
        confettiParticles.map((particle) => (
          <div
            key={particle.id}
            className={styles.confetti}
            style={{
              left: `${particle.left}%`,
              backgroundColor: particle.color,
              width: `${particle.size}px`,
              height: `${particle.size}px`,
              animation: `confettiFall ${particle.duration}ms ease-in ${particle.delay}ms`,
            }}
          />
        ))}
    </div>
  );
};
