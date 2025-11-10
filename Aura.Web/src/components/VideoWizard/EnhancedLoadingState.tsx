import { makeStyles, tokens, Spinner, Text } from '@fluentui/react-components';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXXL,
    animation: 'fadeIn 0.3s ease',
  },
  spinnerContainer: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  pulseRing: {
    position: 'absolute',
    width: '100px',
    height: '100px',
    borderRadius: '50%',
    border: `2px solid ${tokens.colorBrandBackground}`,
    animation: 'pulse 2s ease-out infinite',
  },
  messageContainer: {
    textAlign: 'center',
    maxWidth: '400px',
  },
  tips: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    maxWidth: '500px',
  },
  tipText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    lineHeight: '1.6',
  },
  '@keyframes fadeIn': {
    '0%': { opacity: 0 },
    '100%': { opacity: 1 },
  },
  '@keyframes pulse': {
    '0%': {
      transform: 'scale(0.8)',
      opacity: 0.8,
    },
    '50%': {
      transform: 'scale(1.2)',
      opacity: 0.3,
    },
    '100%': {
      transform: 'scale(1.6)',
      opacity: 0,
    },
  },
});

interface EnhancedLoadingStateProps {
  message?: string;
  showTip?: boolean;
}

const LOADING_TIPS = [
  'Tip: Use descriptive prompts for better results',
  'Tip: Shorter videos typically have higher engagement',
  'Tip: Add your brand logo in the Brand Kit section',
  'Tip: Preview voice samples before generating the full video',
  'Tip: Save your progress frequently to avoid losing work',
  'Tip: Use keyboard shortcuts for faster workflow',
];

export const EnhancedLoadingState: FC<EnhancedLoadingStateProps> = ({
  message = 'Loading...',
  showTip = true,
}) => {
  const styles = useStyles();
  const randomTip = LOADING_TIPS[Math.floor(Math.random() * LOADING_TIPS.length)];

  return (
    <div className={styles.container}>
      <div className={styles.spinnerContainer}>
        <div className={styles.pulseRing} />
        <Spinner size="extra-large" />
      </div>
      <div className={styles.messageContainer}>
        <Text weight="semibold" size={400}>
          {message}
        </Text>
      </div>
      {showTip && (
        <div className={styles.tips}>
          <Text className={styles.tipText}>💡 {randomTip}</Text>
        </div>
      )}
    </div>
  );
};
