import {
  makeStyles,
  tokens,
  Title1,
  Button,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Spinner,
  Text,
  Caption1,
} from '@fluentui/react-components';
import {
  ArrowClockwise24Regular,
  ArrowDownload24Regular,
  MoreVertical24Regular,
  ArrowExport24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Clock24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { SkeletonTable, ErrorState } from '../../components/Loading';
import { getExportHistory, ExportHistoryItem, startExport } from '../../services/exportService';
import { formatFileSize, formatDuration } from '../../utils/formatters';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
  },
  table: {
    width: '100%',
  },
  statusBadge: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '12px',
  },
});

export function ExportHistoryPage() {
  const styles = useStyles();
  const [history, setHistory] = useState<ExportHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reExporting, setReExporting] = useState<string | null>(null);

  const loadHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getExportHistory(undefined, 100);
      setHistory(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load export history');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadHistory();
  }, []);

  const handleReExport = async (item: ExportHistoryItem) => {
    setReExporting(item.id);
    try {
      await startExport({
        inputFile: item.inputFile,
        outputFile: item.outputFile,
        presetName: item.presetName,
      });
      // Reload history to show new export
      await loadHistory();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start re-export');
    } finally {
      setReExporting(null);
    }
  };

  const handleDownload = (outputFile: string) => {
    // In a real implementation, this would download the file
    // For now, we'll just open it or copy the path
    window.open(outputFile, '_blank');
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Completed':
        return (
          <Badge
            appearance="tint"
            color="success"
            icon={<CheckmarkCircle24Regular />}
            className={styles.statusBadge}
          >
            Completed
          </Badge>
        );
      case 'Failed':
        return (
          <Badge
            appearance="tint"
            color="danger"
            icon={<ErrorCircle24Regular />}
            className={styles.statusBadge}
          >
            Failed
          </Badge>
        );
      case 'Processing':
        return (
          <Badge
            appearance="tint"
            color="informative"
            icon={<Spinner size="tiny" />}
            className={styles.statusBadge}
          >
            Processing
          </Badge>
        );
      case 'Queued':
        return (
          <Badge
            appearance="tint"
            color="informative"
            icon={<Clock24Regular />}
            className={styles.statusBadge}
          >
            Queued
          </Badge>
        );
      default:
        return <Badge>{status}</Badge>;
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title1>Export History</Title1>
        </div>
        <SkeletonTable
          columns={[
            'Status',
            'Preset',
            'Platform',
            'Resolution',
            'Created',
            'Duration',
            'Size',
            'Actions',
          ]}
          rowCount={5}
        />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title1>Export History</Title1>
          <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={loadHistory}>
            Retry
          </Button>
        </div>
        <ErrorState message={error} />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Export History</Title1>
        <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={loadHistory}>
          Refresh
        </Button>
      </div>

      {history.length === 0 ? (
        <div className={styles.emptyState}>
          <ArrowExport24Regular fontSize={48} />
          <Text size={500}>No export history</Text>
          <Caption1>Your exported videos will appear here</Caption1>
        </div>
      ) : (
        <Table className={styles.table}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Preset</TableHeaderCell>
              <TableHeaderCell>Platform</TableHeaderCell>
              <TableHeaderCell>Resolution</TableHeaderCell>
              <TableHeaderCell>Created</TableHeaderCell>
              <TableHeaderCell>Duration</TableHeaderCell>
              <TableHeaderCell>Size</TableHeaderCell>
              <TableHeaderCell>Actions</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {history.map((item) => (
              <TableRow key={item.id}>
                <TableCell>{getStatusBadge(item.status)}</TableCell>
                <TableCell>
                  <Text weight="semibold">{item.presetName}</Text>
                  {item.errorMessage && <div className={styles.errorText}>{item.errorMessage}</div>}
                </TableCell>
                <TableCell>{item.platform || '-'}</TableCell>
                <TableCell>{item.resolution || '-'}</TableCell>
                <TableCell>
                  <Caption1>{formatDate(item.createdAt)}</Caption1>
                </TableCell>
                <TableCell>
                  {item.durationSeconds ? formatDuration(item.durationSeconds) : '-'}
                </TableCell>
                <TableCell>{item.fileSize ? formatFileSize(item.fileSize) : '-'}</TableCell>
                <TableCell>
                  <Menu>
                    <MenuTrigger>
                      <Button
                        appearance="subtle"
                        icon={<MoreVertical24Regular />}
                        aria-label="Actions"
                      />
                    </MenuTrigger>
                    <MenuPopover>
                      <MenuList>
                        {item.status === 'Completed' && (
                          <MenuItem
                            icon={<ArrowDownload24Regular />}
                            onClick={() => handleDownload(item.outputFile)}
                          >
                            Download
                          </MenuItem>
                        )}
                        <MenuItem
                          icon={
                            reExporting === item.id ? (
                              <Spinner size="tiny" />
                            ) : (
                              <ArrowExport24Regular />
                            )
                          }
                          onClick={() => handleReExport(item)}
                          disabled={reExporting === item.id}
                        >
                          Re-export
                        </MenuItem>
                      </MenuList>
                    </MenuPopover>
                  </Menu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
