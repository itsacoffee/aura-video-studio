import { makeStyles, tokens, Card } from '@fluentui/react-components';

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
  card: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    height: '24px',
    width: '70%',
  },
  subheader: {
    height: '16px',
    width: '50%',
  },
  content: {
    height: '80px',
    width: '100%',
  },
  footer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  footerItem: {
    height: '32px',
    width: '80px',
  },
});

interface SkeletonCardProps {
  /**
   * Number of skeleton cards to render
   */
  count?: number;
  /**
   * Whether to show a footer section
   */
  showFooter?: boolean;
  /**
   * Whether to show a subheader section
   */
  showSubheader?: boolean;
  /**
   * ARIA label for accessibility
   */
  ariaLabel?: string;
}

/**
 * Skeleton card component for loading states
 * Displays an animated placeholder that mimics a card layout
 */
export function SkeletonCard({
  count = 1,
  showFooter = true,
  showSubheader = true,
  ariaLabel = 'Loading content',
}: SkeletonCardProps) {
  const styles = useStyles();

  const cards = Array.from({ length: count }, (_, index) => (
    <Card key={index} className={styles.card} role="status" aria-label={ariaLabel} aria-busy="true">
      <div className={`${styles.skeleton} ${styles.header}`} />
      {showSubheader && <div className={`${styles.skeleton} ${styles.subheader}`} />}
      <div className={`${styles.skeleton} ${styles.content}`} />
      {showFooter && (
        <div className={styles.footer}>
          <div className={`${styles.skeleton} ${styles.footerItem}`} />
          <div className={`${styles.skeleton} ${styles.footerItem}`} />
        </div>
      )}
    </Card>
  ));

  return <>{cards}</>;
}
