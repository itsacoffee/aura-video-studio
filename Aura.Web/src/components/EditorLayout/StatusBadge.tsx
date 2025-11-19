import { makeStyles, Tooltip } from '@fluentui/react-components';
import React from 'react';
import '../../styles/editor-design-tokens.css';

const useStyles = makeStyles({
  badge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: 'var(--space-sm)',
    padding: '4px 8px',
    borderRadius: 'var(--radius-sm)',
    fontSize: 'var(--font-size-xs)',
    fontWeight: 'var(--font-weight-medium)',
    fontFamily: 'var(--font-family-mono)',
    transition: 'all var(--transition-fast)',
    userSelect: 'none',
  },
  badgeDefault: {
    backgroundColor: 'var(--color-bg-badge)',
    color: 'var(--color-text-secondary)',
  },
  badgeWarning: {
    backgroundColor: '#7f1d1d',
    color: 'var(--color-accent-danger)',
  },
  badgeSuccess: {
    backgroundColor: 'rgba(16, 185, 129, 0.15)',
    color: 'var(--color-success)',
  },
  badgeInfo: {
    backgroundColor: 'rgba(61, 130, 246, 0.15)',
    color: 'var(--color-accent-primary)',
  },
  label: {
    fontWeight: 'var(--font-weight-semibold)',
    marginRight: 'var(--space-xs)',
  },
});

export type StatusBadgeVariant = 'default' | 'warning' | 'success' | 'info';

export interface StatusBadgeProps {
  /** Badge label (e.g., "FPS", "Cache", "Proxy") */
  label: string;
  /** Badge value to display */
  value: string | number;
  /** Visual variant based on status */
  variant?: StatusBadgeVariant;
  /** Optional tooltip content */
  tooltip?: string;
  /** Click handler */
  onClick?: () => void;
  /** Additional class name */
  className?: string;
}

/**
 * StatusBadge Component
 *
 * Displays status indicators like FPS, render mode, cache status, etc.
 * Uses appropriate colors based on status (warning for dropped frames, etc.)
 *
 * Features:
 * - Monospace font for numeric values
 * - Color-coded variants (default, warning, success, info)
 * - Optional tooltip for additional details
 * - Clickable for drill-down information
 */
export function StatusBadge({
  label,
  value,
  variant = 'default',
  tooltip,
  onClick,
  className,
}: StatusBadgeProps) {
  const styles = useStyles();

  const variantClass = {
    default: styles.badgeDefault,
    warning: styles.badgeWarning,
    success: styles.badgeSuccess,
    info: styles.badgeInfo,
  }[variant];

  const badgeClassName = `${styles.badge} ${variantClass} ${className || ''}`;

  const badge = onClick ? (
    <button
      type="button"
      className={badgeClassName}
      onClick={onClick}
      style={{ cursor: 'pointer', border: 'none', background: 'transparent' }}
    >
      <span className={styles.label}>{label}</span>
      <span>{value}</span>
    </button>
  ) : (
    <div className={badgeClassName}>
      <span className={styles.label}>{label}</span>
      <span>{value}</span>
    </div>
  );

  if (tooltip) {
    return (
      <Tooltip content={tooltip} relationship="label">
        {badge}
      </Tooltip>
    );
  }

  return badge;
}
