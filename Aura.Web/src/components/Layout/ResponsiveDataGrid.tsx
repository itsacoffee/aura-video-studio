/**
 * Responsive Data Grid Component
 *
 * Hybrid table/grid component that switches between table view
 * and card view based on viewport width for Apple-level responsive design.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { useMemo, type ReactNode } from 'react';
import { useDisplayEnvironment } from '../../hooks/useDisplayEnvironment';

/**
 * Column definition for the data grid
 */
export interface DataGridColumn<T> {
  /** Property key in data object */
  key: keyof T;
  /** Header display text */
  header: string;
  /** Width: number (px), 'auto', or 'flex' */
  width?: number | 'auto' | 'flex';
  /** Minimum width in pixels */
  minWidth?: number;
  /** Priority: lower values are higher priority (always shown first) */
  priority?: number;
  /** Custom render function */
  render?: (value: T[keyof T], row: T) => ReactNode;
}

/**
 * Props for ResponsiveDataGrid component
 */
export interface ResponsiveDataGridProps<T> {
  /** Data rows to display */
  data: T[];
  /** Column definitions */
  columns: DataGridColumn<T>[];
  /** Custom card renderer for mobile view */
  cardRenderer?: (item: T, index: number) => ReactNode;
  /** Breakpoint to switch to card view (default: 768) */
  breakpoint?: number;
  /** Function to get unique key for each row (defaults to index) */
  getRowKey?: (item: T, index: number) => string | number;
  /** Additional class name */
  className?: string;
  /** Test ID for testing */
  'data-testid'?: string;
}

const useStyles = makeStyles({
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    '& th': {
      textAlign: 'left',
      padding: '12px 16px',
      fontWeight: '600',
      backgroundColor: 'var(--colorNeutralBackground2)',
      borderBottom: '1px solid var(--colorNeutralStroke1)',
    },
    '& td': {
      padding: '12px 16px',
      borderBottom: '1px solid var(--colorNeutralStroke2)',
    },
    '& tr:hover td': {
      backgroundColor: 'var(--colorNeutralBackground1Hover)',
    },
  },
  cardGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: '16px',
  },
  card: {
    padding: '16px',
    backgroundColor: 'var(--colorNeutralBackground1)',
    borderRadius: '8px',
    border: '1px solid var(--colorNeutralStroke1)',
  },
  cardField: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: '4px 0',
    '& + &': {
      borderTop: '1px solid var(--colorNeutralStroke2)',
      marginTop: '4px',
      paddingTop: '8px',
    },
  },
  cardLabel: {
    fontWeight: '500',
    color: 'var(--colorNeutralForeground2)',
  },
  cardValue: {
    color: 'var(--colorNeutralForeground1)',
  },
});

/**
 * Responsive Data Grid Component
 *
 * Displays data as a table on larger screens and switches to
 * a card-based layout on smaller screens.
 *
 * Features:
 * - Automatic table/card switching based on breakpoint
 * - Column priority for responsive column hiding
 * - Custom card renderer support
 * - Automatic default card generation from columns
 *
 * @example
 * ```tsx
 * const columns = [
 *   { key: 'name', header: 'Name', priority: 1 },
 *   { key: 'email', header: 'Email', priority: 2 },
 *   { key: 'status', header: 'Status', priority: 1, render: (v) => <Badge>{v}</Badge> },
 * ];
 *
 * <ResponsiveDataGrid
 *   data={users}
 *   columns={columns}
 *   breakpoint={768}
 * />
 * ```
 */
export function ResponsiveDataGrid<T extends Record<string, unknown>>({
  data,
  columns,
  cardRenderer,
  breakpoint = 768,
  getRowKey,
  className,
  'data-testid': testId,
}: ResponsiveDataGridProps<T>): React.ReactElement {
  const styles = useStyles();
  const display = useDisplayEnvironment();

  const useCardView = display.viewportWidth < breakpoint;

  // Helper to get row key
  const getKey = (item: T, index: number): string | number => {
    if (getRowKey) {
      return getRowKey(item, index);
    }
    // Try common ID fields
    if ('id' in item && (typeof item.id === 'string' || typeof item.id === 'number')) {
      return item.id;
    }
    if ('key' in item && (typeof item.key === 'string' || typeof item.key === 'number')) {
      return item.key;
    }
    return index;
  };

  // Calculate visible columns based on priority and available width
  const visibleColumns = useMemo(() => {
    if (useCardView) return columns;

    // Sort by priority (lower = higher priority)
    const sorted = [...columns].sort((a, b) => (a.priority ?? 99) - (b.priority ?? 99));
    const availableWidth = display.viewportWidth - 48; // Account for padding

    let usedWidth = 0;
    return sorted.filter((col) => {
      const colWidth = col.minWidth ?? 100;
      if (usedWidth + colWidth <= availableWidth) {
        usedWidth += colWidth;
        return true;
      }
      return false;
    });
  }, [columns, display.viewportWidth, useCardView]);

  // Default card renderer if none provided
  const renderCard = (item: T, index: number): ReactNode => {
    if (cardRenderer) {
      return cardRenderer(item, index);
    }

    return (
      <div className={styles.card}>
        {columns.map((col) => {
          const value = item[col.key];
          const renderedValue = col.render ? col.render(value, item) : String(value ?? '');

          return (
            <div key={String(col.key)} className={styles.cardField}>
              <span className={styles.cardLabel}>{col.header}</span>
              <span className={styles.cardValue}>{renderedValue}</span>
            </div>
          );
        })}
      </div>
    );
  };

  if (useCardView) {
    return (
      <div className={mergeClasses(styles.cardGrid, className)} data-testid={testId}>
        {data.map((item, index) => (
          <div key={getKey(item, index)}>{renderCard(item, index)}</div>
        ))}
      </div>
    );
  }

  return (
    <table className={mergeClasses(styles.table, className)} data-testid={testId}>
      <thead>
        <tr>
          {visibleColumns.map((col) => (
            <th key={String(col.key)}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {data.map((row, rowIndex) => (
          <tr key={getKey(row, rowIndex)}>
            {visibleColumns.map((col) => {
              const value = row[col.key];
              const renderedValue = col.render ? col.render(value, row) : String(value ?? '');

              return <td key={String(col.key)}>{renderedValue}</td>;
            })}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export default ResponsiveDataGrid;
