/**
 * Skeleton Loading Components
 * Professional loading states with shimmer effects
 */

import { makeStyles } from '@fluentui/react-components';

const useStyles = makeStyles({
  skeleton: {
    backgroundColor: 'var(--panel-bg, var(--color-surface))',
    borderRadius: 'var(--border-radius-md)',
    overflow: 'hidden',
    position: 'relative',
    '::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      background:
        'linear-gradient(90deg, transparent, var(--panel-hover, rgba(255, 255, 255, 0.05)), transparent)',
      animation: 'shimmer 2s infinite',
    },
  },
  skeletonText: {
    height: '14px',
    marginBottom: 'var(--space-1)',
  },
  skeletonTitle: {
    height: '20px',
    width: '60%',
    marginBottom: 'var(--space-2)',
  },
  skeletonAvatar: {
    width: '40px',
    height: '40px',
    borderRadius: '50%',
  },
  skeletonButton: {
    width: '100px',
    height: '32px',
  },
  skeletonPanel: {
    height: '100%',
    padding: 'var(--space-2)',
  },
  '@keyframes shimmer': {
    '0%': {
      transform: 'translateX(-100%)',
    },
    '100%': {
      transform: 'translateX(100%)',
    },
  },
});

export function SkeletonText({ width = '100%' }: { width?: string }) {
  const styles = useStyles();
  return <div className={`${styles.skeleton} ${styles.skeletonText}`} style={{ width }} />;
}

export function SkeletonTitle() {
  const styles = useStyles();
  return <div className={`${styles.skeleton} ${styles.skeletonTitle}`} />;
}

export function SkeletonAvatar() {
  const styles = useStyles();
  return <div className={`${styles.skeleton} ${styles.skeletonAvatar}`} />;
}

export function SkeletonButton() {
  const styles = useStyles();
  return <div className={`${styles.skeleton} ${styles.skeletonButton}`} />;
}

export function SkeletonPanel() {
  const styles = useStyles();
  return (
    <div className={`${styles.skeleton} ${styles.skeletonPanel}`}>
      <SkeletonTitle />
      <SkeletonText width="90%" />
      <SkeletonText width="85%" />
      <SkeletonText width="75%" />
      <div style={{ marginTop: 'var(--space-3)' }}>
        <SkeletonButton />
      </div>
    </div>
  );
}

/**
 * Skeleton for media library items
 */
export function SkeletonMediaItem() {
  const styles = useStyles();
  return (
    <div style={{ padding: 'var(--space-1)', marginBottom: 'var(--space-1)' }}>
      <div
        className={styles.skeleton}
        style={{ width: '100%', height: '120px', marginBottom: 'var(--space-1)' }}
      />
      <SkeletonText width="80%" />
      <SkeletonText width="60%" />
    </div>
  );
}

/**
 * Skeleton for timeline items
 */
export function SkeletonTimelineItem() {
  const styles = useStyles();
  return (
    <div
      className={styles.skeleton}
      style={{ width: '150px', height: '60px', marginRight: 'var(--space-1)' }}
    />
  );
}

/**
 * Skeleton for properties panel
 */
export function SkeletonPropertiesPanel() {
  const styles = useStyles();
  return (
    <div className={styles.skeletonPanel}>
      <SkeletonTitle />
      <div style={{ marginBottom: 'var(--space-2)' }}>
        <SkeletonText width="40%" />
        <SkeletonText width="100%" />
      </div>
      <div style={{ marginBottom: 'var(--space-2)' }}>
        <SkeletonText width="40%" />
        <SkeletonText width="100%" />
      </div>
      <div style={{ marginBottom: 'var(--space-2)' }}>
        <SkeletonText width="40%" />
        <SkeletonText width="100%" />
      </div>
    </div>
  );
}
