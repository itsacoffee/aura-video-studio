import {
  makeStyles,
  tokens,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  skeleton: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    height: '16px',
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
  cell: {
    padding: tokens.spacingVerticalM,
  },
});

interface SkeletonTableProps {
  /**
   * Column headers
   */
  columns: string[];
  /**
   * Number of skeleton rows to render
   */
  rowCount?: number;
  /**
   * Column widths as percentages (should sum to ~100)
   */
  columnWidths?: string[];
  /**
   * ARIA label for accessibility
   */
  ariaLabel?: string;
}

/**
 * Skeleton table component for loading states
 * Displays animated placeholders that mimic a table structure
 */
export function SkeletonTable({
  columns,
  rowCount = 5,
  columnWidths,
  ariaLabel = 'Loading table data',
}: SkeletonTableProps) {
  const styles = useStyles();

  const defaultWidths = columnWidths || columns.map(() => `${Math.floor(100 / columns.length)}%`);

  return (
    <div role="status" aria-label={ariaLabel} aria-busy="true">
      <Table>
        <TableHeader>
          <TableRow>
            {columns.map((column, index) => (
              <TableHeaderCell key={index}>{column}</TableHeaderCell>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: rowCount }, (_, rowIndex) => (
            <TableRow key={rowIndex}>
              {columns.map((_, colIndex) => (
                <TableCell key={colIndex} className={styles.cell}>
                  <div
                    className={styles.skeleton}
                    style={{ width: defaultWidths[colIndex] || '100%' }}
                  />
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
