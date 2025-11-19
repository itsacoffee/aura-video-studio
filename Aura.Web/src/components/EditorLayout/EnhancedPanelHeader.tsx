import {
  makeStyles,
  Button,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
} from '@fluentui/react-components';
import { MoreHorizontal20Regular, Search20Regular } from '@fluentui/react-icons';
import React, { ReactNode } from 'react';
import '../../styles/editor-design-tokens.css';

const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    minHeight: 'var(--panel-header-height)',
    padding: '0 var(--space-md)',
    backgroundColor: 'var(--color-bg-panel-header)',
    borderBottom: '1px solid var(--color-border-subtle)',
    flexShrink: 0,
  },
  leftSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-md)',
    flex: 1,
  },
  title: {
    fontSize: 'var(--font-size-sm)',
    fontWeight: 'var(--font-weight-semibold)',
    color: 'var(--color-text-primary)',
    letterSpacing: '0.3px',
    userSelect: 'none',
  },
  subtitle: {
    fontSize: 'var(--font-size-xs)',
    color: 'var(--color-text-muted)',
    marginLeft: 'var(--space-md)',
    padding: '2px 6px',
    backgroundColor: 'var(--color-bg-badge)',
    borderRadius: 'var(--radius-sm)',
  },
  rightSection: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-sm)',
  },
  actionButton: {
    minWidth: 'var(--target-size-min)',
    minHeight: 'var(--target-size-min)',
    padding: 'var(--space-sm)',
    color: 'var(--color-text-secondary)',
    transition: 'all var(--transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--color-bg-hover)',
      color: 'var(--color-text-primary)',
    },
  },
});

export interface EnhancedPanelHeaderProps {
  /** Panel title (always visible, never truncated) */
  title: string;
  /** Optional subtitle/tag (e.g., "Clip", "Track", "Sequence") */
  subtitle?: string;
  /** Optional search button handler */
  onSearch?: () => void;
  /** Optional actions menu items */
  menuItems?: ReactNode;
  /** Custom actions to render in the right section */
  customActions?: ReactNode;
  /** Additional class name */
  className?: string;
}

/**
 * EnhancedPanelHeader Component
 *
 * Improved panel header with full title visibility, optional subtitle tag,
 * and action buttons.
 *
 * Features:
 * - Full title always visible (no truncation)
 * - Optional subtitle tag for context
 * - Search button
 * - Actions menu (...)
 * - Consistent spacing and styling
 */
export function EnhancedPanelHeader({
  title,
  subtitle,
  onSearch,
  menuItems,
  customActions,
  className,
}: EnhancedPanelHeaderProps) {
  const styles = useStyles();

  return (
    <div className={`${styles.header} ${className || ''}`}>
      <div className={styles.leftSection}>
        <span className={styles.title}>{title}</span>
        {subtitle && <span className={styles.subtitle}>{subtitle}</span>}
      </div>
      <div className={styles.rightSection}>
        {customActions}
        {onSearch && (
          <Button
            appearance="subtle"
            icon={<Search20Regular />}
            onClick={onSearch}
            className={styles.actionButton}
            aria-label="Search"
          />
        )}
        {menuItems && (
          <Menu>
            <MenuTrigger>
              <Button
                appearance="subtle"
                icon={<MoreHorizontal20Regular />}
                className={styles.actionButton}
                aria-label="More options"
              />
            </MenuTrigger>
            <MenuPopover>
              <MenuList>{menuItems}</MenuList>
            </MenuPopover>
          </Menu>
        )}
      </div>
    </div>
  );
}
