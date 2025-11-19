import { makeStyles, Tooltip } from '@fluentui/react-components';
import React, { ReactNode } from 'react';
import '../../styles/editor-design-tokens.css';

const useStyles = makeStyles({
  tab: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 'var(--space-md)',
    padding: 'var(--space-md)',
    minHeight: 'var(--target-size-comfortable)',
    cursor: 'pointer',
    backgroundColor: 'transparent',
    border: 'none',
    borderLeft: '2px solid transparent',
    color: 'var(--color-text-secondary)',
    fontSize: 'var(--font-size-sm)',
    fontWeight: 'var(--font-weight-medium)',
    fontFamily: 'var(--font-family-default)',
    transition: 'all var(--transition-fast)',
    textAlign: 'left',
    width: '100%',
    userSelect: 'none',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
      color: 'var(--color-text-primary)',
    },
    '&:focus-visible': {
      outline: 'none',
      boxShadow: 'var(--shadow-focus)',
    },
  },
  tabActive: {
    backgroundColor: 'var(--color-bg-surface-subtle)',
    borderLeftColor: 'var(--color-accent-primary)',
    color: 'var(--color-text-primary)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-surface-subtle)',
    },
  },
  tabCollapsed: {
    justifyContent: 'center',
    padding: 'var(--space-md)',
  },
  icon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '20px',
    height: '20px',
    flexShrink: 0,
    '& > svg': {
      width: '100%',
      height: '100%',
    },
  },
  label: {
    flex: 1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  labelCollapsed: {
    display: 'none',
  },
  shortcut: {
    fontSize: 'var(--font-size-xs)',
    color: 'var(--color-text-muted)',
    flexShrink: 0,
  },
});

export interface SidebarTabProps {
  /** Unique identifier for the tab */
  id: string;
  /** Display label for the tab (full text, not truncated) */
  label: string;
  /** Icon element to display */
  icon: ReactNode;
  /** Whether this tab is currently active/selected */
  isActive: boolean;
  /** Whether the sidebar is collapsed (icon-only mode) */
  isCollapsed: boolean;
  /** Optional keyboard shortcut to display */
  shortcut?: string;
  /** Click handler */
  onClick: () => void;
  /** Optional additional class name */
  className?: string;
}

/**
 * SidebarTab Component
 *
 * A tab item for vertical sidebars that displays an icon + full label.
 * Supports both expanded and collapsed states to ensure labels are never truncated.
 *
 * Key features:
 * - Icon + full text label (no truncation in default state)
 * - Expanded/collapsed modes for space efficiency
 * - Visual hierarchy with active/inactive/hover states
 * - Keyboard accessible
 * - Tooltip on hover showing full name + shortcut
 */
export function SidebarTab({
  label,
  icon,
  isActive,
  isCollapsed,
  shortcut,
  onClick,
  className,
}: SidebarTabProps) {
  const styles = useStyles();

  const tabClassName = [
    styles.tab,
    isActive && styles.tabActive,
    isCollapsed && styles.tabCollapsed,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  const labelClassName = [styles.label, isCollapsed && styles.labelCollapsed]
    .filter(Boolean)
    .join(' ');

  const tooltipContent = isCollapsed ? (shortcut ? `${label} (${shortcut})` : label) : undefined;

  const tabButton = (
    <button
      className={tabClassName}
      onClick={onClick}
      aria-label={label}
      aria-current={isActive ? 'page' : undefined}
      title={isCollapsed ? tooltipContent : undefined}
      type="button"
    >
      <span className={styles.icon}>{icon}</span>
      <span className={labelClassName}>{label}</span>
      {!isCollapsed && shortcut && <span className={styles.shortcut}>{shortcut}</span>}
    </button>
  );

  if (isCollapsed && tooltipContent) {
    return (
      <Tooltip content={tooltipContent} relationship="label" positioning="after">
        {tabButton}
      </Tooltip>
    );
  }

  return tabButton;
}
