/**
 * EmptyState Component
 *
 * A reusable empty state component following Apple HIG principles.
 * Features engaging visuals, clear messaging, and optional action buttons.
 */

import { makeStyles, tokens, Text, Button, mergeClasses } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import type { FC, ReactNode, JSX } from 'react';

export interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
    icon?: JSX.Element;
  };
  secondaryAction?: {
    label: string;
    onClick: () => void;
  };
  size?: 'small' | 'medium' | 'large';
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    textAlign: 'center',
    height: '100%',
    minHeight: '160px',
  },
  containerSmall: {
    padding: tokens.spacingVerticalL,
    gap: tokens.spacingVerticalM,
  },
  containerMedium: {
    padding: tokens.spacingVerticalXL,
    gap: tokens.spacingVerticalL,
  },
  containerLarge: {
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalXL,
  },
  iconWrapper: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: tokens.colorNeutralBackground3,
    color: tokens.colorNeutralForeground3,
    transition: 'transform 300ms ease-out, background-color 200ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground4,
    },
  },
  iconSmall: {
    width: '48px',
    height: '48px',
    '& svg': {
      width: '24px',
      height: '24px',
    },
  },
  iconMedium: {
    width: '72px',
    height: '72px',
    '& svg': {
      width: '32px',
      height: '32px',
    },
  },
  iconLarge: {
    width: '96px',
    height: '96px',
    '& svg': {
      width: '48px',
      height: '48px',
    },
  },
  textContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    maxWidth: '280px',
  },
  title: {
    color: tokens.colorNeutralForeground1,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    lineHeight: tokens.lineHeightBase300,
  },
  actionsContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
});

export const EmptyState: FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  secondaryAction,
  size = 'medium',
  className,
}) => {
  const styles = useStyles();

  const containerSizeClass =
    size === 'small'
      ? styles.containerSmall
      : size === 'large'
        ? styles.containerLarge
        : styles.containerMedium;

  const iconSizeClass =
    size === 'small' ? styles.iconSmall : size === 'large' ? styles.iconLarge : styles.iconMedium;

  return (
    <div className={mergeClasses(styles.container, containerSizeClass, className)}>
      <motion.div
        className={mergeClasses(styles.iconWrapper, iconSizeClass)}
        initial={{ scale: 0.8, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{ duration: 0.3, ease: 'easeOut' }}
      >
        {icon}
      </motion.div>

      <motion.div
        className={styles.textContainer}
        initial={{ y: 8, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ duration: 0.3, delay: 0.1, ease: 'easeOut' }}
      >
        <Text
          weight="semibold"
          size={size === 'small' ? 300 : size === 'large' ? 500 : 400}
          className={styles.title}
        >
          {title}
        </Text>
        {description && (
          <Text size={size === 'small' ? 200 : 300} className={styles.description}>
            {description}
          </Text>
        )}
      </motion.div>

      {(action || secondaryAction) && (
        <motion.div
          className={styles.actionsContainer}
          initial={{ y: 8, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ duration: 0.3, delay: 0.2, ease: 'easeOut' }}
        >
          {action && (
            <Button
              appearance="primary"
              icon={action.icon}
              onClick={action.onClick}
              size={size === 'small' ? 'small' : 'medium'}
            >
              {action.label}
            </Button>
          )}
          {secondaryAction && (
            <Button
              appearance="subtle"
              onClick={secondaryAction.onClick}
              size={size === 'small' ? 'small' : 'medium'}
            >
              {secondaryAction.label}
            </Button>
          )}
        </motion.div>
      )}
    </div>
  );
};

export default EmptyState;
