import { makeStyles } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import type { FC, ReactNode } from 'react';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    width: '100%',
  },
  content: {
    width: '100%',
  },
  entering: {
    animation: 'slideInRight 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  exiting: {
    animation: 'slideOutLeft 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  '@keyframes slideInRight': {
    '0%': {
      opacity: 0,
      transform: 'translateX(30px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateX(0)',
    },
  },
  '@keyframes slideOutLeft': {
    '0%': {
      opacity: 1,
      transform: 'translateX(0)',
    },
    '100%': {
      opacity: 0,
      transform: 'translateX(-30px)',
    },
  },
});

interface AnimatedStepTransitionProps {
  children: ReactNode;
  stepKey: string | number;
}

export const AnimatedStepTransition: FC<AnimatedStepTransitionProps> = ({ children, stepKey }) => {
  const styles = useStyles();
  const [animationState, setAnimationState] = useState<'entering' | 'stable'>('entering');
  const [currentKey, setCurrentKey] = useState(stepKey);

  useEffect(() => {
    if (currentKey !== stepKey) {
      setAnimationState('entering');
      setCurrentKey(stepKey);
    }
  }, [stepKey, currentKey]);

  useEffect(() => {
    if (animationState === 'entering') {
      const timer = setTimeout(() => {
        setAnimationState('stable');
      }, 400);
      return () => clearTimeout(timer);
    }
  }, [animationState]);

  return (
    <div className={styles.container}>
      <div
        className={`${styles.content} ${animationState === 'entering' ? styles.entering : ''}`}
        key={stepKey}
      >
        {children}
      </div>
    </div>
  );
};
