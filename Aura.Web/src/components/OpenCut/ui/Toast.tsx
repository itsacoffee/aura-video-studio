/**
 * OpenCut Toast Component
 *
 * Individual toast notification with enter/exit animations,
 * auto-dismiss progress bar, and action button support.
 */

import { makeStyles, mergeClasses, Button, tokens } from '@fluentui/react-components';
import {
  CheckmarkCircle20Regular,
  ErrorCircle20Regular,
  Info20Regular,
  Warning20Regular,
  Dismiss20Regular,
} from '@fluentui/react-icons';
import { useEffect, useState, useCallback, useRef, type FC } from 'react';
import { useReducedMotion } from '../../../hooks/useReducedMotion';
import { openCutTokens } from '../../../styles/designTokens';
import type { Toast as ToastData, ToastType } from '../../../stores/opencutToasts';

export interface ToastProps {
  /** Toast data */
  toast: ToastData;
  /** Callback when toast should be dismissed */
  onDismiss: (id: string) => void;
}

const useStyles = makeStyles({
  root: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: openCutTokens.spacing.sm,
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: openCutTokens.radius.lg,
    boxShadow: openCutTokens.shadows.lg,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    minWidth: '280px',
    maxWidth: '400px',
    position: 'relative',
    overflow: 'hidden',
  },
  entering: {
    '@keyframes toastEnter': {
      '0%': {
        opacity: 0,
        transform: 'translateX(100%)',
      },
      '100%': {
        opacity: 1,
        transform: 'translateX(0)',
      },
    },
    animationName: 'toastEnter',
    animationDuration: openCutTokens.animation.duration.normal,
    animationTimingFunction: openCutTokens.animation.easing.easeOut,
    animationFillMode: 'forwards',
  },
  exiting: {
    '@keyframes toastExit': {
      '0%': {
        opacity: 1,
        transform: 'translateX(0)',
      },
      '100%': {
        opacity: 0,
        transform: 'translateX(100%)',
      },
    },
    animationName: 'toastExit',
    animationDuration: openCutTokens.animation.duration.fast,
    animationTimingFunction: openCutTokens.animation.easing.easeIn,
    animationFillMode: 'forwards',
  },
  noAnimation: {
    animation: 'none',
    opacity: 1,
    transform: 'translateX(0)',
  },
  icon: {
    flexShrink: 0,
    width: '20px',
    height: '20px',
  },
  iconSuccess: {
    color: '#34C759',
  },
  iconError: {
    color: '#FF3B30',
  },
  iconWarning: {
    color: '#FF9500',
  },
  iconInfo: {
    color: '#007AFF',
  },
  content: {
    flex: 1,
    minWidth: 0,
  },
  title: {
    fontSize: openCutTokens.typography.fontSize.md,
    fontWeight: openCutTokens.typography.fontWeight.semibold,
    color: tokens.colorNeutralForeground1,
    lineHeight: openCutTokens.typography.lineHeight.tight,
    marginBottom: openCutTokens.spacing.xxs,
  },
  message: {
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground2,
    lineHeight: openCutTokens.typography.lineHeight.normal,
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    marginTop: openCutTokens.spacing.sm,
  },
  actionButton: {
    fontSize: openCutTokens.typography.fontSize.sm,
    padding: `${openCutTokens.spacing.xxs} ${openCutTokens.spacing.sm}`,
  },
  dismissButton: {
    flexShrink: 0,
    minWidth: '24px',
    height: '24px',
    padding: '0',
    color: tokens.colorNeutralForeground3,
    ':hover': {
      color: tokens.colorNeutralForeground1,
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  progressBar: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: '3px',
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
  },
  progressFill: {
    height: '100%',
    transition: 'width 100ms linear',
  },
  progressSuccess: {
    backgroundColor: '#34C759',
  },
  progressError: {
    backgroundColor: '#FF3B30',
  },
  progressWarning: {
    backgroundColor: '#FF9500',
  },
  progressInfo: {
    backgroundColor: '#007AFF',
  },
});

/**
 * Get the icon component for a toast type
 */
function getToastIcon(type: ToastType) {
  switch (type) {
    case 'success':
      return CheckmarkCircle20Regular;
    case 'error':
      return ErrorCircle20Regular;
    case 'warning':
      return Warning20Regular;
    case 'info':
    default:
      return Info20Regular;
  }
}

/**
 * Toast notification component with animations and progress bar
 */
export const Toast: FC<ToastProps> = ({ toast, onDismiss }) => {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();
  const [isExiting, setIsExiting] = useState(false);
  const [progress, setProgress] = useState(100);
  const [isPaused, setIsPaused] = useState(false);
  const startTimeRef = useRef<number>(Date.now());
  const remainingTimeRef = useRef<number>(toast.duration ?? 5000);

  const Icon = getToastIcon(toast.type);

  const handleDismiss = useCallback(() => {
    setIsExiting(true);
    // Wait for exit animation before removing
    setTimeout(
      () => {
        onDismiss(toast.id);
      },
      prefersReducedMotion ? 0 : 150
    );
  }, [onDismiss, toast.id, prefersReducedMotion]);

  // Progress bar and auto-dismiss
  useEffect(() => {
    const duration = toast.duration ?? 5000;
    if (duration <= 0) return;

    const updateProgress = () => {
      if (isPaused) return;

      const elapsed = Date.now() - startTimeRef.current;
      const remaining = Math.max(0, remainingTimeRef.current - elapsed);
      const progressValue = (remaining / duration) * 100;

      setProgress(progressValue);

      if (remaining <= 0) {
        handleDismiss();
      }
    };

    const interval = setInterval(updateProgress, 100);
    return () => clearInterval(interval);
  }, [toast.duration, isPaused, handleDismiss]);

  // Handle pause/resume
  const handleMouseEnter = useCallback(() => {
    setIsPaused(true);
    // Save remaining time when pausing
    const elapsed = Date.now() - startTimeRef.current;
    remainingTimeRef.current = Math.max(0, remainingTimeRef.current - elapsed);
  }, []);

  const handleMouseLeave = useCallback(() => {
    setIsPaused(false);
    // Reset start time when resuming
    startTimeRef.current = Date.now();
  }, []);

  const iconClassName = mergeClasses(
    styles.icon,
    toast.type === 'success' && styles.iconSuccess,
    toast.type === 'error' && styles.iconError,
    toast.type === 'warning' && styles.iconWarning,
    toast.type === 'info' && styles.iconInfo
  );

  const progressClassName = mergeClasses(
    styles.progressFill,
    toast.type === 'success' && styles.progressSuccess,
    toast.type === 'error' && styles.progressError,
    toast.type === 'warning' && styles.progressWarning,
    toast.type === 'info' && styles.progressInfo
  );

  return (
    <div
      className={mergeClasses(
        styles.root,
        prefersReducedMotion ? styles.noAnimation : isExiting ? styles.exiting : styles.entering
      )}
      role="alert"
      aria-live="polite"
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <Icon className={iconClassName} />

      <div className={styles.content}>
        <div className={styles.title}>{toast.title}</div>
        {toast.message && <div className={styles.message}>{toast.message}</div>}

        {toast.action && (
          <div className={styles.actions}>
            <Button
              appearance="primary"
              size="small"
              className={styles.actionButton}
              onClick={toast.action.onClick}
            >
              {toast.action.label}
            </Button>
          </div>
        )}
      </div>

      <Button
        appearance="transparent"
        icon={<Dismiss20Regular />}
        className={styles.dismissButton}
        onClick={handleDismiss}
        aria-label="Dismiss notification"
      />

      {/* Progress bar for auto-dismiss */}
      {(toast.duration ?? 5000) > 0 && (
        <div className={styles.progressBar}>
          <div className={progressClassName} style={{ width: `${progress}%` }} />
        </div>
      )}
    </div>
  );
};

export default Toast;
