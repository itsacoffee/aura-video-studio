/**
 * FadeIn Component
 * Smooth fade-in animation for content loading
 */

import { makeStyles } from '@fluentui/react-components';
import { ReactNode, useState, useEffect } from 'react';

const useStyles = makeStyles({
  fadeIn: {
    animation: 'fadeIn var(--transition-slow) ease-out',
  },
  '@keyframes fadeIn': {
    from: {
      opacity: 0,
      transform: 'translateY(8px)',
    },
    to: {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
});

interface FadeInProps {
  children: ReactNode;
  delay?: number;
  className?: string;
}

export function FadeIn({ children, delay = 0, className = '' }: FadeInProps) {
  const styles = useStyles();
  const [isVisible, setIsVisible] = useState(delay === 0);

  useEffect(() => {
    if (delay > 0) {
      const timer = setTimeout(() => {
        setIsVisible(true);
      }, delay);
      return () => clearTimeout(timer);
    }
  }, [delay]);

  if (!isVisible) {
    return null;
  }

  return <div className={`${styles.fadeIn} ${className}`}>{children}</div>;
}
