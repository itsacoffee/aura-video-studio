import { makeStyles, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
  skeleton: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    position: 'relative',
    overflow: 'hidden',
    '::before': {
      content: '""',
      position: 'absolute',
      top: 0,
      left: '-100%',
      width: '100%',
      height: '100%',
      background: `linear-gradient(90deg, transparent, ${tokens.colorNeutralBackground1Hover}, transparent)`,
      animationName: {
        '0%': {
          transform: 'translateX(0)',
        },
        '100%': {
          transform: 'translateX(200%)',
        },
      },
      animationDuration: '1.5s',
      animationIterationCount: 'infinite',
      animationTimingFunction: 'ease-in-out',
    },
  },
  listItem: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    alignItems: 'center',
  },
  avatar: {
    width: '40px',
    height: '40px',
    borderRadius: tokens.borderRadiusCircular,
    flexShrink: 0,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  title: {
    height: '16px',
    width: '60%',
  },
  subtitle: {
    height: '14px',
    width: '40%',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  action: {
    width: '32px',
    height: '32px',
  },
  divider: {
    height: '1px',
    backgroundColor: tokens.colorNeutralStroke2,
    margin: `0 ${tokens.spacingHorizontalM}`,
  },
});

interface SkeletonListProps {
  /**
   * Number of skeleton list items to render
   */
  count?: number;
  /**
   * Whether to show an avatar/icon section
   */
  showAvatar?: boolean;
  /**
   * Whether to show action buttons
   */
  showActions?: boolean;
  /**
   * Whether to show dividers between items
   */
  showDividers?: boolean;
  /**
   * ARIA label for accessibility
   */
  ariaLabel?: string;
}

/**
 * Skeleton list component for loading states
 * Displays animated placeholders that mimic list items
 */
export function SkeletonList({
  count = 3,
  showAvatar = true,
  showActions = true,
  showDividers = true,
  ariaLabel = 'Loading list',
}: SkeletonListProps) {
  const styles = useStyles();

  return (
    <div role="status" aria-label={ariaLabel} aria-busy="true">
      {Array.from({ length: count }, (_, index) => (
        <div key={index}>
          <div className={styles.listItem}>
            {showAvatar && <div className={`${styles.skeleton} ${styles.avatar}`} />}
            <div className={styles.content}>
              <div className={`${styles.skeleton} ${styles.title}`} />
              <div className={`${styles.skeleton} ${styles.subtitle}`} />
            </div>
            {showActions && (
              <div className={styles.actions}>
                <div className={`${styles.skeleton} ${styles.action}`} />
                <div className={`${styles.skeleton} ${styles.action}`} />
              </div>
            )}
          </div>
          {showDividers && index < count - 1 && <div className={styles.divider} />}
        </div>
      ))}
    </div>
  );
}
