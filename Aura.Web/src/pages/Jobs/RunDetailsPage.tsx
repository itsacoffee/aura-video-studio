import {
  makeStyles,
  tokens,
  Text,
  Button,
  Card,
  Spinner,
  Badge,
  DataGrid,
  DataGridBody,
  DataGridRow,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridCell,
  createTableColumn,
  TableCellLayout,
  TableColumnDefinition,
  Title3,
  Caption1,
  Body1,
} from '@fluentui/react-components';
import {
  ArrowLeft24Regular,
  CheckmarkCircle24Filled,
  Warning24Filled,
  ErrorCircle24Filled,
} from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getJobTelemetry } from '@/api/telemetryClient';
import { ModelSelectionAudit } from '@/components/Jobs';
import type { RunTelemetryCollection, RunTelemetryRecord } from '@/types/telemetry';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  backButton: {
    marginBottom: tokens.spacingVerticalM,
  },
  summaryCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  summaryCard: {
    padding: tokens.spacingVerticalM,
  },
  summaryValue: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightSemibold,
    marginTop: tokens.spacingVerticalS,
  },
  summaryLabel: {
    color: tokens.colorNeutralForeground2,
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalM,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
  errorContainer: {
    padding: tokens.spacingVerticalXL,
    textAlign: 'center',
  },
  stageBreakdownCard: {
    padding: tokens.spacingVerticalM,
  },
  costByStageList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  costItem: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  providersList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  providerItem: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface StageRow {
  stage: string;
  status: string;
  latency: string;
  cost: string;
  provider: string;
  message: string;
}

const RunDetailsPage: React.FC = () => {
  const styles = useStyles();
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const [telemetry, setTelemetry] = useState<RunTelemetryCollection | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTelemetry = async () => {
      if (!jobId) {
        setError('Job ID is required');
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const data = await getJobTelemetry(jobId);
        setTelemetry(data);
        setError(null);
      } catch (err: unknown) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        setError(errorObj.message || 'Failed to load telemetry data');
      } finally {
        setLoading(false);
      }
    };

    void loadTelemetry();
  }, [jobId]);

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'ok':
        return <CheckmarkCircle24Filled primaryFill={tokens.colorPaletteGreenForeground1} />;
      case 'warn':
        return <Warning24Filled primaryFill={tokens.colorPaletteYellowForeground1} />;
      case 'error':
        return <ErrorCircle24Filled primaryFill={tokens.colorPaletteRedForeground1} />;
      default:
        return null;
    }
  };

  const columns: TableColumnDefinition<StageRow>[] = [
    createTableColumn<StageRow>({
      columnId: 'stage',
      compare: (a, b) => a.stage.localeCompare(b.stage),
      renderHeaderCell: () => 'Stage',
      renderCell: (item) => (
        <TableCellLayout>
          <Text weight="semibold">{item.stage}</Text>
        </TableCellLayout>
      ),
    }),
    createTableColumn<StageRow>({
      columnId: 'status',
      compare: (a, b) => a.status.localeCompare(b.status),
      renderHeaderCell: () => 'Status',
      renderCell: (item) => (
        <TableCellLayout media={getStatusIcon(item.status)}>
          <Badge appearance={item.status === 'ok' ? 'filled' : 'outline'}>{item.status}</Badge>
        </TableCellLayout>
      ),
    }),
    createTableColumn<StageRow>({
      columnId: 'latency',
      compare: (a, b) => parseFloat(a.latency) - parseFloat(b.latency),
      renderHeaderCell: () => 'Latency',
      renderCell: (item) => <TableCellLayout>{item.latency}</TableCellLayout>,
    }),
    createTableColumn<StageRow>({
      columnId: 'cost',
      compare: (a, b) => parseFloat(a.cost.replace('$', '')) - parseFloat(b.cost.replace('$', '')),
      renderHeaderCell: () => 'Cost',
      renderCell: (item) => <TableCellLayout>{item.cost}</TableCellLayout>,
    }),
    createTableColumn<StageRow>({
      columnId: 'provider',
      compare: (a, b) => a.provider.localeCompare(b.provider),
      renderHeaderCell: () => 'Provider',
      renderCell: (item) => <TableCellLayout>{item.provider}</TableCellLayout>,
    }),
    createTableColumn<StageRow>({
      columnId: 'message',
      compare: (a, b) => a.message.localeCompare(b.message),
      renderHeaderCell: () => 'Message',
      renderCell: (item) => (
        <TableCellLayout>
          <Caption1>{item.message}</Caption1>
        </TableCellLayout>
      ),
    }),
  ];

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading telemetry data..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <ErrorCircle24Filled primaryFill={tokens.colorPaletteRedForeground1} />
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalM }}>
          {error}
        </Text>
        <Button
          appearance="primary"
          onClick={() => navigate('/jobs')}
          style={{ marginTop: tokens.spacingVerticalL }}
        >
          Back to Jobs
        </Button>
      </div>
    );
  }

  if (!telemetry) {
    return null;
  }

  const summary = telemetry.summary;
  const stageRows: StageRow[] = telemetry.records.map((record: RunTelemetryRecord) => ({
    stage: record.stage,
    status: record.result_status,
    latency: `${(record.latency_ms / 1000).toFixed(2)}s`,
    cost: record.cost_estimate ? `$${record.cost_estimate.toFixed(4)}` : '$0.0000',
    provider: record.provider || 'N/A',
    message: record.message || '',
  }));

  return (
    <div className={styles.container}>
      <Button
        className={styles.backButton}
        appearance="subtle"
        icon={<ArrowLeft24Regular />}
        onClick={() => navigate('/jobs')}
      >
        Back to Jobs
      </Button>

      <div className={styles.header}>
        <Title3>Run Details - {telemetry.job_id}</Title3>
      </div>

      {summary && (
        <div className={styles.summaryCards}>
          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Total Operations</Caption1>
            <div className={styles.summaryValue}>{summary.total_operations}</div>
          </Card>

          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Success Rate</Caption1>
            <div className={styles.summaryValue}>
              {summary.total_operations > 0
                ? ((summary.successful_operations / summary.total_operations) * 100).toFixed(1)
                : 0}
              %
            </div>
          </Card>

          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Total Cost</Caption1>
            <div className={styles.summaryValue}>${summary.total_cost.toFixed(4)}</div>
          </Card>

          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Total Latency</Caption1>
            <div className={styles.summaryValue}>
              {(summary.total_latency_ms / 1000).toFixed(2)}s
            </div>
          </Card>

          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Total Tokens</Caption1>
            <div className={styles.summaryValue}>
              {(summary.total_tokens_in + summary.total_tokens_out).toLocaleString()}
            </div>
          </Card>

          <Card className={styles.summaryCard}>
            <Caption1 className={styles.summaryLabel}>Cache Hits</Caption1>
            <div className={styles.summaryValue}>{summary.cache_hits}</div>
          </Card>
        </div>
      )}

      <div className={styles.section}>
        <Text size={500} weight="semibold" className={styles.sectionTitle}>
          Stage Breakdown
        </Text>
        <Card className={styles.stageBreakdownCard}>
          <DataGrid
            items={stageRows}
            columns={columns}
            sortable
            resizableColumns
            columnSizingOptions={{
              stage: {
                minWidth: 100,
                defaultWidth: 120,
              },
              status: {
                minWidth: 80,
                defaultWidth: 100,
              },
              latency: {
                minWidth: 80,
                defaultWidth: 100,
              },
              cost: {
                minWidth: 80,
                defaultWidth: 100,
              },
              provider: {
                minWidth: 100,
                defaultWidth: 150,
              },
              message: {
                minWidth: 200,
                defaultWidth: 300,
              },
            }}
          >
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<StageRow>>
              {({ item, rowId }) => (
                <DataGridRow<StageRow> key={rowId}>
                  {({ renderCell }) => <DataGridCell>{renderCell(item)}</DataGridCell>}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        </Card>
      </div>

      {summary?.cost_by_stage && Object.keys(summary.cost_by_stage).length > 0 && (
        <div className={styles.section}>
          <Text size={500} weight="semibold" className={styles.sectionTitle}>
            Cost by Stage
          </Text>
          <Card className={styles.stageBreakdownCard}>
            <div className={styles.costByStageList}>
              {Object.entries(summary.cost_by_stage)
                .sort(([, a], [, b]) => b - a)
                .map(([stage, cost]) => (
                  <div key={stage} className={styles.costItem}>
                    <Body1>{stage}</Body1>
                    <Text weight="semibold">${cost.toFixed(4)}</Text>
                  </div>
                ))}
            </div>
          </Card>
        </div>
      )}

      {/* Model Selection Audit Trail */}
      {jobId && (
        <div className={styles.section}>
          <ModelSelectionAudit jobId={jobId} />
        </div>
      )}

      {summary?.operations_by_provider &&
        Object.keys(summary.operations_by_provider).length > 0 && (
          <div className={styles.section}>
            <Text size={500} weight="semibold" className={styles.sectionTitle}>
              Operations by Provider
            </Text>
            <Card className={styles.stageBreakdownCard}>
              <div className={styles.providersList}>
                {Object.entries(summary.operations_by_provider)
                  .sort(([, a], [, b]) => b - a)
                  .map(([provider, count]) => (
                    <div key={provider} className={styles.providerItem}>
                      <Body1>{provider}</Body1>
                      <Text weight="semibold">{count} operations</Text>
                    </div>
                  ))}
              </div>
            </Card>
          </div>
        )}
    </div>
  );
};

export { RunDetailsPage };
