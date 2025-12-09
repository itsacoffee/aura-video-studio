/**
 * PanelSection Component
 *
 * Collapsible sections within panels with smooth animations.
 * Provides expandable/collapsible content with consistent styling.
 */

import { makeStyles, tokens, Text, mergeClasses } from '@fluentui/react-components';
import { ChevronDown20Regular } from '@fluentui/react-icons';
import { AnimatePresence, motion } from 'framer-motion';
import { useState, useCallback, useId } from 'react';
import type { FC, ReactNode } from 'react';
import { useReducedMotion } from '../../../hooks/useReducedMotion';
import { openCutTokens } from '../../../styles/designTokens';

export interface PanelSectionProps {
  /** Section title */
  title: string;
  /** Icon displayed before the title */
  icon?: ReactNode;
  /** Whether the section is initially expanded */
  defaultExpanded?: boolean;
  /** Controlled expanded state */
  expanded?: boolean;
  /** Callback when expanded state changes */
  onExpandedChange?: (expanded: boolean) => void;
  /** Action buttons displayed in the section header */
  actions?: ReactNode;
  /** Section content */
  children: ReactNode;
  /** Additional class name */
  className?: string;
  /** Whether the section is collapsible */
  collapsible?: boolean;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.md,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: openCutTokens.spacing.md,
    cursor: 'pointer',
    userSelect: 'none',
    transition: `background-color ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '-2px',
    },
  },
  headerNonCollapsible: {
    cursor: 'default',
    ':hover': {
      backgroundColor: 'transparent',
    },
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    flex: 1,
    minWidth: 0,
  },
  icon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    alignItems: 'center',
    flexShrink: 0,
  },
  title: {
    color: tokens.colorNeutralForeground2,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
    flexShrink: 0,
  },
  chevron: {
    color: tokens.colorNeutralForeground3,
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    display: 'flex',
    alignItems: 'center',
  },
  chevronExpanded: {
    transform: 'rotate(180deg)',
  },
  contentWrapper: {
    overflow: 'hidden',
  },
  content: {
    padding: `0 ${openCutTokens.spacing.md} ${openCutTokens.spacing.md}`,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
  },
});

/**
 * PanelSection provides a collapsible section within panels.
 * Supports controlled and uncontrolled modes with smooth animations.
 */
export const PanelSection: FC<PanelSectionProps> = ({
  title,
  icon,
  defaultExpanded = true,
  expanded: controlledExpanded,
  onExpandedChange,
  actions,
  children,
  className,
  collapsible = true,
}) => {
  const styles = useStyles();
  const prefersReducedMotion = useReducedMotion();
  const headingId = useId();
  const contentId = useId();

  const [internalExpanded, setInternalExpanded] = useState(defaultExpanded);

  // Use controlled state if provided, otherwise use internal state
  const isExpanded = controlledExpanded !== undefined ? controlledExpanded : internalExpanded;

  const handleToggle = useCallback(() => {
    if (!collapsible) return;

    const newExpanded = !isExpanded;

    if (controlledExpanded === undefined) {
      setInternalExpanded(newExpanded);
    }

    onExpandedChange?.(newExpanded);
  }, [collapsible, isExpanded, controlledExpanded, onExpandedChange]);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent) => {
      if (!collapsible) return;

      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        handleToggle();
      }
    },
    [collapsible, handleToggle]
  );

  const handleActionsClick = useCallback((event: React.MouseEvent) => {
    // Prevent section toggle when clicking actions
    event.stopPropagation();
  }, []);

  const contentVariants = {
    collapsed: {
      height: 0,
      opacity: 0,
    },
    expanded: {
      height: 'auto',
      opacity: 1,
    },
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div
        className={mergeClasses(styles.header, !collapsible && styles.headerNonCollapsible)}
        onClick={handleToggle}
        onKeyDown={handleKeyDown}
        role={collapsible ? 'button' : undefined}
        tabIndex={collapsible ? 0 : undefined}
        aria-expanded={collapsible ? isExpanded : undefined}
        aria-controls={collapsible ? contentId : undefined}
        id={headingId}
      >
        <div className={styles.headerLeft}>
          {icon && <span className={styles.icon}>{icon}</span>}
          <Text weight="semibold" size={200} className={styles.title}>
            {title}
          </Text>
        </div>
        <div className={styles.headerRight}>
          {actions && (
            <div onClick={handleActionsClick} onKeyDown={(e) => e.stopPropagation()}>
              {actions}
            </div>
          )}
          {collapsible && (
            <span className={mergeClasses(styles.chevron, isExpanded && styles.chevronExpanded)}>
              <ChevronDown20Regular />
            </span>
          )}
        </div>
      </div>

      <AnimatePresence initial={false}>
        {isExpanded && (
          <motion.div
            id={contentId}
            role="region"
            aria-labelledby={headingId}
            className={styles.contentWrapper}
            initial={prefersReducedMotion ? false : 'collapsed'}
            animate="expanded"
            exit={prefersReducedMotion ? undefined : 'collapsed'}
            variants={contentVariants}
            transition={{
              duration: prefersReducedMotion ? 0 : 0.2,
              ease: [0.25, 1, 0.5, 1],
            }}
          >
            <div className={styles.content}>{children}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default PanelSection;
