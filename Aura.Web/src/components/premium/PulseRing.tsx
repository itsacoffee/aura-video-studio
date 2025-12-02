/**
 * PulseRing Component
 * Pulsing ring animation for attention/loading states
 */

import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { motion, AnimatePresence } from 'framer-motion';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  wrapper: {
    position: 'relative',
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  ring: {
    position: 'absolute',
    borderRadius: '50%',
    border: `2px solid ${tokens.colorBrandBackground}`,
    pointerEvents: 'none',
  },
  staticRing: {
    position: 'absolute',
    borderRadius: '50%',
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    pointerEvents: 'none',
  },
  content: {
    position: 'relative',
    zIndex: 1,
  },
});

interface PulseRingProps {
  children: React.ReactNode;
  className?: string;
  active?: boolean;
  size?: number;
  color?: string;
  duration?: number;
}

export function PulseRing({
  children,
  className,
  active = true,
  size = 40,
  color,
  duration = 1.5,
}: PulseRingProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();

  const showPulse = active && animationsEnabled && settings.effects.microInteractions;

  return (
    <div className={mergeClasses(styles.wrapper, className)} style={{ width: size, height: size }}>
      <AnimatePresence>
        {showPulse && (
          <>
            <motion.div
              className={styles.ring}
              style={{
                width: size,
                height: size,
                borderColor: color ?? tokens.colorBrandBackground,
              }}
              initial={{ scale: 1, opacity: 0.8 }}
              animate={{
                scale: [1, 1.5, 2],
                opacity: [0.8, 0.4, 0],
              }}
              transition={{
                duration,
                repeat: Infinity,
                ease: 'easeOut',
              }}
            />
            <motion.div
              className={styles.ring}
              style={{
                width: size,
                height: size,
                borderColor: color ?? tokens.colorBrandBackground,
              }}
              initial={{ scale: 1, opacity: 0.8 }}
              animate={{
                scale: [1, 1.5, 2],
                opacity: [0.8, 0.4, 0],
              }}
              transition={{
                duration,
                repeat: Infinity,
                ease: 'easeOut',
                delay: duration / 2,
              }}
            />
          </>
        )}
      </AnimatePresence>
      {!showPulse && active && (
        <div
          className={styles.staticRing}
          style={{
            width: size,
            height: size,
            borderColor: color ?? tokens.colorNeutralStroke1,
          }}
        />
      )}
      <div className={styles.content}>{children}</div>
    </div>
  );
}
