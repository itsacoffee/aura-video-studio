import {
  makeStyles,
  tokens,
  Text,
  Card,
  Badge,
  Button,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  TableCellLayout,
  TableColumnDefinition,
  createTableColumn,
  Spinner,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Field,
  Input,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  History24Regular,
  Shield24Regular,
  Warning24Regular,
  Filter24Regular,
  ArrowDownload24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';
import apiClient from '../../services/api/apiClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  filters: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  detailsRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  detailField: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  overrideIndicator: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

interface AuditLogEntry {
  id: string;
  timestamp: string;
  contentId: string;
  policyId: string;
  userId: string;
  decision: 'Proceed' | 'Block' | 'Override' | 'ModifyAndProceed';
  decisionReason: string;
  overriddenViolations: string[];
}

interface IncidentLogViewerProps {
  showFilters?: boolean;
}

export const IncidentLogViewer: FC<IncidentLogViewerProps> = ({ showFilters = true }) => {
  const styles = useStyles();
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedLog, setSelectedLog] = useState<AuditLogEntry | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  const [filterContentId, setFilterContentId] = useState('');
  const [filterPolicyId, setFilterPolicyId] = useState('');
  const [filterDecision, setFilterDecision] = useState<string>('');

  const loadLogs = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams();
      if (filterContentId) params.append('contentId', filterContentId);
      if (filterPolicyId) params.append('policyId', filterPolicyId);

      const response = await apiClient.get<{ total: number; logs: AuditLogEntry[] }>(
        `/api/contentsafety/audit?${params.toString()}`
      );

      let filteredLogs = response.data.logs;
      if (filterDecision && filterDecision !== 'all') {
        filteredLogs = filteredLogs.filter((log: AuditLogEntry) => log.decision === filterDecision);
      }

      setLogs(filteredLogs);
    } catch (error) {
      console.error('Failed to load audit logs', error);
    } finally {
      setIsLoading(false);
    }
  }, [filterContentId, filterPolicyId, filterDecision]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  const handleViewDetails = (log: AuditLogEntry) => {
    setSelectedLog(log);
    setShowDetails(true);
  };

  const handleExportLogs = async () => {
    const csvContent = [
      [
        'ID',
        'Timestamp',
        'Content ID',
        'Policy ID',
        'User',
        'Decision',
        'Reason',
        'Overrides',
      ].join(','),
      ...logs.map((log) =>
        [
          log.id,
          new Date(log.timestamp).toLocaleString(),
          log.contentId,
          log.policyId,
          log.userId,
          log.decision,
          `"${log.decisionReason.replace(/"/g, '""')}"`,
          log.overriddenViolations.length,
        ].join(',')
      ),
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `content-safety-audit-${new Date().toISOString()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const getDecisionBadge = (decision: string) => {
    switch (decision) {
      case 'Proceed':
        return (
          <Badge appearance="filled" color="success">
            Proceed
          </Badge>
        );
      case 'Block':
        return (
          <Badge appearance="filled" color="danger">
            Block
          </Badge>
        );
      case 'Override':
        return (
          <Badge appearance="filled" color="warning">
            Override
          </Badge>
        );
      case 'ModifyAndProceed':
        return (
          <Badge appearance="filled" color="brand">
            Modified
          </Badge>
        );
      default:
        return <Badge appearance="outline">{decision}</Badge>;
    }
  };

  const columns: TableColumnDefinition<AuditLogEntry>[] = [
    createTableColumn<AuditLogEntry>({
      columnId: 'timestamp',
      renderHeaderCell: () => 'Timestamp',
      renderCell: (log) => (
        <TableCellLayout>{new Date(log.timestamp).toLocaleString()}</TableCellLayout>
      ),
    }),
    createTableColumn<AuditLogEntry>({
      columnId: 'contentId',
      renderHeaderCell: () => 'Content ID',
      renderCell: (log) => (
        <TableCellLayout>
          <Text size={200} style={{ fontFamily: 'monospace' }}>
            {log.contentId.substring(0, 8)}...
          </Text>
        </TableCellLayout>
      ),
    }),
    createTableColumn<AuditLogEntry>({
      columnId: 'decision',
      renderHeaderCell: () => 'Decision',
      renderCell: (log) => <TableCellLayout>{getDecisionBadge(log.decision)}</TableCellLayout>,
    }),
    createTableColumn<AuditLogEntry>({
      columnId: 'overrides',
      renderHeaderCell: () => 'Overrides',
      renderCell: (log) => (
        <TableCellLayout>
          {log.overriddenViolations.length > 0 ? (
            <Badge appearance="filled" color="danger">
              {log.overriddenViolations.length}
            </Badge>
          ) : (
            <Text>-</Text>
          )}
        </TableCellLayout>
      ),
    }),
    createTableColumn<AuditLogEntry>({
      columnId: 'actions',
      renderHeaderCell: () => 'Actions',
      renderCell: (log) => (
        <TableCellLayout>
          <Button appearance="subtle" size="small" onClick={() => handleViewDetails(log)}>
            Details
          </Button>
        </TableCellLayout>
      ),
    }),
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <History24Regular />
          <Text weight="semibold" size={500}>
            Safety Incident Log
          </Text>
          <Badge appearance="outline">{logs.length} entries</Badge>
        </div>
        <div className={styles.actionButtons}>
          <Button appearance="subtle" icon={<Filter24Regular />} onClick={loadLogs}>
            Refresh
          </Button>
          <Button
            appearance="subtle"
            icon={<ArrowDownload24Regular />}
            onClick={handleExportLogs}
            disabled={logs.length === 0}
          >
            Export CSV
          </Button>
        </div>
      </div>

      {showFilters && (
        <Card className={styles.card}>
          <div className={styles.filters}>
            <Field label="Content ID">
              <Input
                value={filterContentId}
                onChange={(_, data) => setFilterContentId(data.value)}
                placeholder="Filter by content ID..."
                size="small"
              />
            </Field>
            <Field label="Policy ID">
              <Input
                value={filterPolicyId}
                onChange={(_, data) => setFilterPolicyId(data.value)}
                placeholder="Filter by policy ID..."
                size="small"
              />
            </Field>
            <Field label="Decision">
              <Dropdown
                value={filterDecision || 'All'}
                onOptionSelect={(_, data) => setFilterDecision(data.optionValue as string)}
                size="small"
              >
                <Option value="all">All</Option>
                <Option value="Proceed">Proceed</Option>
                <Option value="Block">Block</Option>
                <Option value="Override">Override</Option>
                <Option value="ModifyAndProceed">Modified</Option>
              </Dropdown>
            </Field>
            <div style={{ display: 'flex', alignItems: 'flex-end' }}>
              <Button appearance="primary" onClick={loadLogs} size="small">
                Apply Filters
              </Button>
            </div>
          </div>
        </Card>
      )}

      {isLoading ? (
        <Card className={styles.card}>
          <div
            style={{
              display: 'flex',
              justifyContent: 'center',
              padding: tokens.spacingVerticalXXL,
            }}
          >
            <Spinner label="Loading audit logs..." />
          </div>
        </Card>
      ) : logs.length === 0 ? (
        <Card className={styles.card}>
          <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
            <Shield24Regular style={{ fontSize: '48px', color: tokens.colorNeutralForeground3 }} />
            <Text style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
              No audit logs found
            </Text>
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              Safety decisions will appear here once content is analyzed
            </Text>
          </div>
        </Card>
      ) : (
        <Card>
          <DataGrid items={logs} columns={columns} sortable getRowId={(item) => item.id}>
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<AuditLogEntry>>
              {({ item, rowId }) => (
                <DataGridRow<AuditLogEntry> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        </Card>
      )}

      <Dialog open={showDetails} onOpenChange={(_, data) => setShowDetails(data.open)}>
        <DialogSurface>
          <DialogTitle>Audit Log Details</DialogTitle>
          <DialogBody>
            <DialogContent>
              {selectedLog && (
                <div className={styles.detailsRow}>
                  <div className={styles.detailField}>
                    <Text weight="semibold">ID:</Text>
                    <Text style={{ fontFamily: 'monospace' }}>{selectedLog.id}</Text>
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">Timestamp:</Text>
                    <Text>{new Date(selectedLog.timestamp).toLocaleString()}</Text>
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">Content ID:</Text>
                    <Text style={{ fontFamily: 'monospace' }}>{selectedLog.contentId}</Text>
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">Policy ID:</Text>
                    <Text style={{ fontFamily: 'monospace' }}>{selectedLog.policyId}</Text>
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">User:</Text>
                    <Text>{selectedLog.userId}</Text>
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">Decision:</Text>
                    {getDecisionBadge(selectedLog.decision)}
                  </div>
                  <div className={styles.detailField}>
                    <Text weight="semibold">Reason:</Text>
                    <Text>{selectedLog.decisionReason}</Text>
                  </div>

                  {selectedLog.overriddenViolations.length > 0 && (
                    <div className={styles.overrideIndicator}>
                      <Warning24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                      <div>
                        <Text weight="semibold" style={{ display: 'block' }}>
                          {selectedLog.overriddenViolations.length} Safety Violation(s) Overridden
                        </Text>
                        <Text size={200}>
                          This user chose to proceed despite safety warnings. Advanced Mode
                          permission required.
                        </Text>
                        <ul style={{ marginTop: tokens.spacingVerticalS, paddingLeft: '20px' }}>
                          {selectedLog.overriddenViolations.map((violation, idx) => (
                            <li key={idx}>
                              <Text size={200}>{violation}</Text>
                            </li>
                          ))}
                        </ul>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="primary" onClick={() => setShowDetails(false)}>
                Close
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};
