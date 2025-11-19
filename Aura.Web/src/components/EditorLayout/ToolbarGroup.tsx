import { makeStyles } from '@fluentui/react-components';
import React, { ReactNode } from 'react';
import '../../styles/editor-design-tokens.css';

const useStyles = makeStyles({
  group: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-sm)',
    padding: '0 var(--space-md)',
  },
  separator: {
    width: '1px',
    height: '20px',
    backgroundColor: 'var(--color-border-subtle)',
    margin: '0 var(--space-sm)',
  },
});

export interface ToolbarGroupProps {
  /** Child elements (buttons, controls, etc.) */
  children: ReactNode;
  /** Whether to show a separator after this group */
  showSeparator?: boolean;
  /** Additional class name */
  className?: string;
  /** Accessible label for the group */
  'aria-label'?: string;
}

/**
 * ToolbarGroup Component
 *
 * Groups related toolbar items together with consistent spacing.
 * Optionally shows a separator after the group.
 */
export function ToolbarGroup({
  children,
  showSeparator = false,
  className,
  'aria-label': ariaLabel,
}: ToolbarGroupProps) {
  const styles = useStyles();

  return (
    <>
      <div className={`${styles.group} ${className || ''}`} role="group" aria-label={ariaLabel}>
        {children}
      </div>
      {showSeparator && <div className={styles.separator} role="separator" aria-hidden="true" />}
    </>
  );
}
