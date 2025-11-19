import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Spinner,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Badge,
  Field,
  Input,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  Filter24Regular,
  ArrowClockwise24Regular,
  Calendar24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, type FC } from 'react';
import { useCostTrackingStore } from '../../state/costTracking';

const useStyles = makeStyles({
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  filterSection: {
    marginBottom: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  filterRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    alignItems: 'flex-end',
  },
  filterField: {
    flex: 1,
    minWidth: '200px',
  },
  summaryCards: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalXXL,
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
  },
  summaryValue: {
    fontSize: tokens.fontSizeHero700,
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorBrandForeground1,
  },
  summaryLabel: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  tableCard: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalL,
  },
  tableContainer: {
    maxHeight: '600px',
    overflowY: 'auto',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    color: tokens.colorNeutralForeground3,
  },
  chartPlaceholder: {
    height: '300px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
});

interface CostHistoryEntry {
  timestamp: string;
  jobId: string;
  projectName?: string;
  provider: string;
  feature: string;
  cost: number;
  currency: string;
}

export const CostHistoryPage: FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(true);
  const [historyData, setHistoryData] = useState<CostHistoryEntry[]>([]);
  const [startDate, setStartDate] = useState<string>(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  );
  const [endDate, setEndDate] = useState<string>(new Date().toISOString().split('T')[0]);
  const [selectedProvider, setSelectedProvider] = useState<string>('all');
  const [selectedFeature, setSelectedFeature] = useState<string>('all');

  const { loadCurrentPeriodSpending, currentPeriodSpending } = useCostTrackingStore();

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [startDate, endDate, selectedProvider, selectedFeature]);

  useEffect(() => {
    loadCurrentPeriodSpending();
  }, [loadCurrentPeriodSpending]);

  const loadData = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        startDate,
        endDate,
      });

      if (selectedProvider !== 'all') {
        params.append('provider', selectedProvider);
      }

      if (selectedFeature !== 'all') {
        params.append('feature', selectedFeature);
      }

      const response = await fetch(`/api/cost-tracking/history?${params.toString()}`);

      if (response.ok) {
        const data = await response.json();
        setHistoryData(data.entries || []);
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to load cost history:', errorObj.message);
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async (format: 'csv' | 'json') => {
    try {
      const params = new URLSearchParams({
        startDate,
        endDate,
        format,
      });

      if (selectedProvider !== 'all') {
        params.append('provider', selectedProvider);
      }

      if (selectedFeature !== 'all') {
        params.append('feature', selectedFeature);
      }

      const response = await fetch(`/api/cost-tracking/history/export?${params.toString()}`);

      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `cost-history-${startDate}-to-${endDate}.${format}`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      }
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to export cost history:', errorObj.message);
    }
  };

  const totalCost = historyData.reduce((sum, entry) => sum + entry.cost, 0);
  const uniqueProviders = new Set(historyData.map((entry) => entry.provider)).size;
  const uniqueJobs = new Set(historyData.map((entry) => entry.jobId)).size;

  const formatCurrency = (value: number, currency: string) => {
    return `${currency} ${value.toFixed(2)}`;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>Cost Tracking History</Title1>
          <Text>View and analyze your spending across all video generation jobs</Text>
        </div>
        <div className={styles.headerActions}>
          <Button icon={<ArrowClockwise24Regular />} onClick={loadData} disabled={loading}>
            Refresh
          </Button>
          <Button
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('csv')}
            disabled={loading || historyData.length === 0}
          >
            Export CSV
          </Button>
          <Button
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('json')}
            disabled={loading || historyData.length === 0}
          >
            Export JSON
          </Button>
        </div>
      </div>

      <Card className={styles.filterSection}>
        <Title3>Filters</Title3>
        <div className={styles.filterRow}>
          <Field label="Start Date" className={styles.filterField}>
            <Input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              contentBefore={<Calendar24Regular />}
            />
          </Field>
          <Field label="End Date" className={styles.filterField}>
            <Input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              contentBefore={<Calendar24Regular />}
            />
          </Field>
          <Field label="Provider" className={styles.filterField}>
            <Dropdown
              value={selectedProvider}
              onOptionSelect={(_, data) => setSelectedProvider(data.optionValue as string)}
            >
              <Option value="all">All Providers</Option>
              <Option value="OpenAI">OpenAI</Option>
              <Option value="Anthropic">Anthropic</Option>
              <Option value="Gemini">Gemini</Option>
              <Option value="ElevenLabs">ElevenLabs</Option>
              <Option value="PlayHT">PlayHT</Option>
              <Option value="Pexels">Pexels</Option>
            </Dropdown>
          </Field>
          <Field label="Feature" className={styles.filterField}>
            <Dropdown
              value={selectedFeature}
              onOptionSelect={(_, data) => setSelectedFeature(data.optionValue as string)}
            >
              <Option value="all">All Features</Option>
              <Option value="ScriptGeneration">Script Generation</Option>
              <Option value="TextToSpeech">Text to Speech</Option>
              <Option value="ImageGeneration">Image Generation</Option>
              <Option value="VideoRendering">Video Rendering</Option>
            </Dropdown>
          </Field>
          <Button
            icon={<Filter24Regular />}
            appearance="primary"
            onClick={loadData}
            disabled={loading}
          >
            Apply
          </Button>
        </div>
      </Card>

      <div className={styles.summaryCards}>
        <Card className={styles.summaryCard}>
          <div className={styles.summaryValue}>
            {formatCurrency(totalCost, currentPeriodSpending?.currency || 'USD')}
          </div>
          <Text className={styles.summaryLabel}>Total Cost (Selected Period)</Text>
        </Card>
        <Card className={styles.summaryCard}>
          <div className={styles.summaryValue}>{uniqueJobs}</div>
          <Text className={styles.summaryLabel}>Jobs Completed</Text>
        </Card>
        <Card className={styles.summaryCard}>
          <div className={styles.summaryValue}>{uniqueProviders}</div>
          <Text className={styles.summaryLabel}>Providers Used</Text>
        </Card>
        <Card className={styles.summaryCard}>
          <div className={styles.summaryValue}>
            {formatCurrency(
              currentPeriodSpending?.totalCost || 0,
              currentPeriodSpending?.currency || 'USD'
            )}
          </div>
          <Text className={styles.summaryLabel}>Current Period Total</Text>
        </Card>
      </div>

      <Card className={styles.tableCard}>
        <Title2>Transaction History</Title2>
        {loading ? (
          <div className={styles.emptyState}>
            <Spinner label="Loading cost history..." />
          </div>
        ) : historyData.length === 0 ? (
          <div className={styles.emptyState}>
            <Text>No cost data found for the selected period</Text>
          </div>
        ) : (
          <div className={styles.tableContainer}>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Date & Time</TableHeaderCell>
                  <TableHeaderCell>Job ID</TableHeaderCell>
                  <TableHeaderCell>Project</TableHeaderCell>
                  <TableHeaderCell>Provider</TableHeaderCell>
                  <TableHeaderCell>Feature</TableHeaderCell>
                  <TableHeaderCell>Cost</TableHeaderCell>
                </TableRow>
              </TableHeader>
              <TableBody>
                {historyData.map((entry, index) => (
                  <TableRow key={`${entry.jobId}-${index}`}>
                    <TableCell>{formatDate(entry.timestamp)}</TableCell>
                    <TableCell>{entry.jobId.substring(0, 8)}...</TableCell>
                    <TableCell>{entry.projectName || 'N/A'}</TableCell>
                    <TableCell>
                      <Badge appearance="outline">{entry.provider}</Badge>
                    </TableCell>
                    <TableCell>{entry.feature}</TableCell>
                    <TableCell>{formatCurrency(entry.cost, entry.currency)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </Card>
    </div>
  );
};

export default CostHistoryPage;
