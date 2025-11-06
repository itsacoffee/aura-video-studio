/**
 * RunDetailsPage - Displays telemetry breakdown for a job
 */

import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Title3,
  Body1,
  Spinner,
  Button,
  DataGrid,
  DataGridHeader,
  DataGridRow,
  DataGridHeaderCell,
  DataGridBody,
  DataGridCell,
  createTableColumn,
  TableColumnDefinition,
  Badge,
} from '@fluentui/react-components';
import {
  ArrowLeft24Regular,
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { getJobTelemetry } from '@/api/telemetryClient';
import { ModelSelectionAudit } from '@/components/Jobs';
import type {
  RunTelemetryCollection,
  RunTelemetryRecord,
} from '@/types/telemetry';

export const RunDetailsPage: React.FC = () => {
  const { jobId } = useParams<{ jobId: string }>();
  const navigate = useNavigate();
  const [telemetry, setTelemetry] = useState<RunTelemetryCollection | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!jobId) {
      setError('No job ID provided');
      setLoading(false);
      return;
    }

    const loadTelemetry = async () => {
      try {
        setLoading(true);
        const data = await getJobTelemetry(jobId);
        setTelemetry(data);
      } catch (err: unknown) {
        const errorObj = err instanceof Error ? err : new Error(String(err));
        setError(errorObj.message || 'Failed to load telemetry');
      } finally {
        setLoading(false);
      }
    };

    loadTelemetry();
  }, [jobId]);

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'ok':
        return <CheckmarkCircle24Regular style={{ color: 'green' }} />;
      case 'warn':
        return <Warning24Regular style={{ color: 'orange' }} />;
      case 'error':
        return <ErrorCircle24Regular style={{ color: 'red' }} />;
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string) => {
    const color = status === 'ok' ? 'success' : status === 'warn' ? 'warning' : 'danger';
    return (
      <Badge appearance="filled" color={color}>
        {status.toUpperCase()}
      </Badge>
    );
  };

  const columns: TableColumnDefinition<RunTelemetryRecord>[] = [
    createTableColumn<RunTelemetryRecord>({
      columnId: 'stage',
      renderHeaderCell: () => 'Stage',
      renderCell: (item) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          {getStatusIcon(item.result_status)}
          <span style={{ textTransform: 'capitalize' }}>{item.stage}</span>
        </div>
      ),
    }),
    createTableColumn<RunTelemetryRecord>({
      columnId: 'provider',
      renderHeaderCell: () => 'Provider',
      renderCell: (item) => item.provider || '-',
    }),
    createTableColumn<RunTelemetryRecord>({
      columnId: 'latency',
      renderHeaderCell: () => 'Latency',
      renderCell: (item) => `${(item.latency_ms / 1000).toFixed(2)}s`,
    }),
    createTableColumn<RunTelemetryRecord>({
      columnId: 'cost',
      renderHeaderCell: () => 'Cost',
      renderCell: (item) =>
        item.cost_estimate
          ? `$${item.cost_estimate.toFixed(4)}`
          : '-',
    }),
    createTableColumn<RunTelemetryRecord>({
      columnId: 'tokens',
      renderHeaderCell: () => 'Tokens',
      renderCell: (item) =>
        item.tokens_in && item.tokens_out
          ? `${item.tokens_in + item.tokens_out}`
          : '-',
    }),
    createTableColumn<RunTelemetryRecord>({
      columnId: 'status',
      renderHeaderCell: () => 'Status',
      renderCell: (item) => getStatusBadge(item.result_status),
    }),
  ];

  if (loading) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '400px',
        }}
      >
        <Spinner label="Loading telemetry..." />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '24px' }}>
        <Button
          icon={<ArrowLeft24Regular />}
          onClick={() => navigate('/jobs')}
          style={{ marginBottom: '16px' }}
        >
          Back to Jobs
        </Button>
        <Card style={{ padding: '24px', textAlign: 'center' }}>
          <ErrorCircle24Regular style={{ fontSize: '48px', color: 'red' }} />
          <Title3 style={{ marginTop: '16px' }}>Failed to Load Telemetry</Title3>
          <Body1 style={{ marginTop: '8px', color: '#666' }}>{error}</Body1>
        </Card>
      </div>
    );
  }

  if (!telemetry) {
    return (
      <div style={{ padding: '24px' }}>
        <Button
          icon={<ArrowLeft24Regular />}
          onClick={() => navigate('/jobs')}
          style={{ marginBottom: '16px' }}
        >
          Back to Jobs
        </Button>
        <Card style={{ padding: '24px', textAlign: 'center' }}>
          <Body1>No telemetry data available</Body1>
        </Card>
      </div>
    );
  }

  const summary = telemetry.summary;

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      <Button
        icon={<ArrowLeft24Regular />}
        onClick={() => navigate('/jobs')}
        style={{ marginBottom: '16px' }}
      >
        Back to Jobs
      </Button>

      <Title3 style={{ marginBottom: '24px' }}>
        Run Telemetry: {telemetry.job_id}
      </Title3>

      {summary && (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
            gap: '16px',
            marginBottom: '24px',
          }}
        >
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Total Operations
            </Body1>
            <Title3>{summary.total_operations}</Title3>
          </Card>
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Total Cost
            </Body1>
            <Title3>
              ${summary.total_cost.toFixed(4)} {summary.currency}
            </Title3>
          </Card>
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Total Latency
            </Body1>
            <Title3>{(summary.total_latency_ms / 1000).toFixed(2)}s</Title3>
          </Card>
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Success Rate
            </Body1>
            <Title3>
              {(
                (summary.successful_operations / summary.total_operations) *
                100
              ).toFixed(1)}
              %
            </Title3>
          </Card>
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Total Tokens
            </Body1>
            <Title3>
              {summary.total_tokens_in + summary.total_tokens_out}
            </Title3>
          </Card>
          <Card style={{ padding: '16px' }}>
            <Body1 style={{ color: '#666', marginBottom: '8px' }}>
              Cache Hits
            </Body1>
            <Title3>{summary.cache_hits}</Title3>
          </Card>
        </div>
      )}

      <Card style={{ padding: '24px' }}>
        <Title3 style={{ marginBottom: '16px' }}>Stage Breakdown</Title3>
        <DataGrid
          items={telemetry.records}
          columns={columns}
          sortable
          resizableColumns
        >
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<RunTelemetryRecord>>
            {({ item, rowId }) => (
              <DataGridRow<RunTelemetryRecord> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      </Card>

      {summary?.cost_by_stage && (
        <Card style={{ padding: '24px', marginTop: '24px' }}>
          <Title3 style={{ marginBottom: '16px' }}>Cost by Stage</Title3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {Object.entries(summary.cost_by_stage).map(([stage, cost]) => (
              <div
                key={stage}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  padding: '12px',
                  backgroundColor: '#f5f5f5',
                  borderRadius: '4px',
                }}
              >
                <Body1 style={{ textTransform: 'capitalize' }}>{stage}</Body1>
                <Body1 style={{ fontWeight: 'bold' }}>
                  ${cost.toFixed(4)} {summary.currency}
                </Body1>
              </div>
            ))}
          </div>
        </Card>
      )}

      {summary?.operations_by_provider && (
        <Card style={{ padding: '24px', marginTop: '24px' }}>
          <Title3 style={{ marginBottom: '16px' }}>Operations by Provider</Title3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {Object.entries(summary.operations_by_provider).map(
              ([provider, count]) => (
                <div
                  key={provider}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '12px',
                    backgroundColor: '#f5f5f5',
                    borderRadius: '4px',
                  }}
                >
                  <Body1>{provider}</Body1>
                  <Body1 style={{ fontWeight: 'bold' }}>{count} operations</Body1>
                </div>
              )
            )}
          </div>
        </Card>
      )}

      {jobId && (
        <div style={{ marginTop: '24px' }}>
          <ModelSelectionAudit jobId={jobId} />
        </div>
      )}
    </div>
  );
};

export default RunDetailsPage;
