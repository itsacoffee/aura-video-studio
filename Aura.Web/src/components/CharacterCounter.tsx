/**
 * Character counter component for form inputs
 */

import { Text, makeStyles, tokens } from '@fluentui/react-components';
import type { FC } from 'react';

interface CharacterCounterProps {
  current: number;
  max: number;
  warningThreshold?: number; // Percentage at which to show warning (default 90%)
}

const useStyles = makeStyles({
  counter: {
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXXS,
  },
  normal: {
    color: tokens.colorNeutralForeground3,
  },
  warning: {
    color: tokens.colorPaletteYellowForeground1,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
  },
});

export const CharacterCounter: FC<CharacterCounterProps> = ({
  current,
  max,
  warningThreshold = 90,
}) => {
  const styles = useStyles();
  const percentage = (current / max) * 100;
  const remaining = max - current;

  let className = styles.normal;
  let message = `${current.toLocaleString()} / ${max.toLocaleString()} characters`;

  if (current > max) {
    className = styles.error;
    message = `Exceeds limit by ${Math.abs(remaining).toLocaleString()} characters`;
  } else if (percentage >= warningThreshold) {
    className = styles.warning;
    message = `${remaining.toLocaleString()} characters remaining`;
  }

  return (
    <Text className={`${styles.counter} ${className}`} size={200}>
      {message}
    </Text>
  );
};
