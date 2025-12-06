/**
 * OpenCut Toast Container
 *
 * Container component that renders all active toasts.
 * Handles stacking, positioning, and animations.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import type { FC } from 'react';
import { useOpenCutToastsStore } from '../../../stores/opencutToasts';
import { openCutTokens } from '../../../styles/designTokens';
import { Toast } from './Toast';

export interface ToastContainerProps {
  /** Position of the toast container on screen */
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
  /** Additional CSS class */
  className?: string;
}

const useStyles = makeStyles({
  root: {
    position: 'fixed',
    zIndex: openCutTokens.zIndex.toast,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
    pointerEvents: 'none',
    maxHeight: 'calc(100vh - 48px)',
    overflowY: 'auto',
    overflowX: 'hidden',
  },
  topRight: {
    top: openCutTokens.spacing.xl,
    right: openCutTokens.spacing.xl,
  },
  topLeft: {
    top: openCutTokens.spacing.xl,
    left: openCutTokens.spacing.xl,
  },
  bottomRight: {
    bottom: openCutTokens.spacing.xl,
    right: openCutTokens.spacing.xl,
    flexDirection: 'column-reverse',
  },
  bottomLeft: {
    bottom: openCutTokens.spacing.xl,
    left: openCutTokens.spacing.xl,
    flexDirection: 'column-reverse',
  },
  toastWrapper: {
    pointerEvents: 'auto',
  },
});

/**
 * Container for displaying toast notifications
 * Renders all active toasts from the OpenCut toast store
 */
export const ToastContainer: FC<ToastContainerProps> = ({ position = 'top-right', className }) => {
  const styles = useStyles();
  const { toasts, removeToast } = useOpenCutToastsStore();

  if (toasts.length === 0) {
    return null;
  }

  const positionClass = {
    'top-right': styles.topRight,
    'top-left': styles.topLeft,
    'bottom-right': styles.bottomRight,
    'bottom-left': styles.bottomLeft,
  }[position];

  return (
    <div
      className={mergeClasses(styles.root, positionClass, className)}
      aria-label="Notifications"
      role="region"
    >
      {toasts.map((toast) => (
        <div key={toast.id} className={styles.toastWrapper}>
          <Toast toast={toast} onDismiss={removeToast} />
        </div>
      ))}
    </div>
  );
};

export default ToastContainer;
