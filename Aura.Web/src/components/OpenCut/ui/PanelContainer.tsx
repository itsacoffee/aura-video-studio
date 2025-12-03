/**
 * PanelContainer Component
 *
 * A consistent container for all panels with header, loading, and empty states.
 * Provides a unified look and feel across the OpenCut editor panels.
 */

import { makeStyles, tokens, Text, Spinner, mergeClasses } from '@fluentui/react-components';
import { AnimatePresence, motion } from 'framer-motion';
import type { FC, ReactNode } from 'react';
import { openCutTokens, motionVariants, defaultTransition } from '../../../styles/designTokens';
import { useReducedMotion } from '../../../hooks/useReducedMotion';

export interface PanelContainerProps {
  /** Panel title displayed in the header */
  title: string;
  /** Optional subtitle or additional info */
  subtitle?: string;
  /** Icon displayed before the title */
  icon?: ReactNode;
  /** Action buttons displayed in the header */
  actions?: ReactNode;
  /** Panel content */
  children: ReactNode;
  /** Show loading state with spinner */
  isLoading?: boolean;
  /** Loading message to display */
  loadingMessage?: string;
  /** Show empty state */
  isEmpty?: boolean;
  /** Empty state configuration */
  emptyState?: {
    icon?: ReactNode;
    title: string;
    description?: string;
    action?: ReactNode;
  };
  /** Additional class name for the container */
  className?: string;
  /** Whether the panel content should be scrollable */
  scrollable?: boolean;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.panelPadding}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
    gap: openCutTokens.spacing.sm,
    flexShrink: 0,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    flex: 1,
    minWidth: 0,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
    display: 'flex',
    alignItems: 'center',
    flexShrink: 0,
  },
  titleContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    minWidth: 0,
  },
  title: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    flexShrink: 0,
  },
  content: {
    flex: 1,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
  },
  contentScrollable: {
    overflow: 'auto',
  },
  contentInner: {
    padding: openCutTokens.spacing.panelPadding,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sectionGap,
    flex: 1,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    flex: 1,
    gap: openCutTokens.spacing.md,
    padding: openCutTokens.spacing.xl,
  },
  loadingMessage: {
    color: tokens.colorNeutralForeground3,
  },
  emptyContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    flex: 1,
    gap: openCutTokens.spacing.md,
    padding: openCutTokens.spacing.xl,
    textAlign: 'center',
  },
  emptyIcon: {
    fontSize: '48px',
    color: tokens.colorNeutralForeground4,
    marginBottom: openCutTokens.spacing.sm,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyTitle: {
    color: tokens.colorNeutralForeground2,
  },
  emptyDescription: {
    color: tokens.colorNeutralForeground3,
    maxWidth: '280px',
  },
  emptyAction: {
    marginTop: openCutTokens.spacing.sm,
  },
});

/**
 * PanelContainer provides a consistent wrapper for all editor panels.
 * It includes header with title/actions, loading states, and empty states.
 */
export const PanelContainer: FC<PanelContainerProps> = ({
  title,
  subtitle,
  icon,
  actions,
  children,
  isLoading = false,
  loadingMessage = 'Loading...',
  isEmpty = false,
  emptyState,
  className,
  scrollable = true,
}) => {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();

  const transition = prefersReducedMotion ? { duration: 0 } : defaultTransition;

  const renderContent = () => {
    if (isLoading) {
      return (
        <motion.div
          className={styles.loadingContainer}
          {...(prefersReducedMotion ? {} : motionVariants.fadeIn)}
          transition={transition}
        >
          <Spinner size="medium" />
          <Text size={200} className={styles.loadingMessage}>
            {loadingMessage}
          </Text>
        </motion.div>
      );
    }

    if (isEmpty && emptyState) {
      return (
        <motion.div
          className={styles.emptyContainer}
          {...(prefersReducedMotion ? {} : motionVariants.fadeIn)}
          transition={transition}
        >
          {emptyState.icon && <div className={styles.emptyIcon}>{emptyState.icon}</div>}
          <Text weight="medium" size={300} className={styles.emptyTitle}>
            {emptyState.title}
          </Text>
          {emptyState.description && (
            <Text size={200} className={styles.emptyDescription}>
              {emptyState.description}
            </Text>
          )}
          {emptyState.action && <div className={styles.emptyAction}>{emptyState.action}</div>}
        </motion.div>
      );
    }

    return (
      <div className={styles.contentInner}>
        <AnimatePresence mode="wait">{children}</AnimatePresence>
      </div>
    );
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          {icon && <span className={styles.headerIcon}>{icon}</span>}
          <div className={styles.titleContainer}>
            <Text weight="semibold" size={400} className={styles.title}>
              {title}
            </Text>
            {subtitle && (
              <Text size={200} className={styles.subtitle}>
                {subtitle}
              </Text>
            )}
          </div>
        </div>
        {actions && <div className={styles.headerActions}>{actions}</div>}
      </div>
      <div className={mergeClasses(styles.content, scrollable && styles.contentScrollable)}>
        {renderContent()}
      </div>
    </div>
  );
};

export default PanelContainer;
