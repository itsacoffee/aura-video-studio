import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  Badge,
  makeStyles,
  tokens,
  Tab,
  TabList,
  SelectTabEvent,
  SelectTabData,
  Switch,
  Dropdown,
  Option,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
} from '@fluentui/react-components';
import {
  DataUsage24Regular,
  Money24Regular,
  Timer24Regular,
  ArrowDownload24Regular,
  Settings24Regular,
  Delete24Regular,
  DataPie24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import {
  getUsageStatistics,
  getCostStatistics,
  getPerformanceStatistics,
  getCurrentMonthBudget,
  getAnalyticsSettings,
  updateAnalyticsSettings,
  getDatabaseInfo,
  triggerCleanup,
  clearAllData,
  exportAnalyticsData,
  type UsageStatistics,
  type CostStatistics,
  type PerformanceStatistics,
  type MonthlyBudgetStatus,
  type AnalyticsSettings,
  type DatabaseInfo,
} from '../../api/analyticsClient';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1600px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXL,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  tabs: {
    marginBottom: tokens.spacingVerticalL,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalXL,
  },
  statCard: {
    padding: tokens.spacingVerticalL,
    height: '100%',
  },
  statHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  statValue: {
    fontSize: '32px',
    fontWeight: 600,
    marginBottom: tokens.spacingVerticalXS,
  },
  statSubtext: {
    color: tokens.colorNeutralForeground3,
    fontSize: '12px',
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  sectionHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  breakdownGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  breakdownItem: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  privacyBanner: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
  settingRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  progressBar: {
    width: '100%',
    height: '8px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusLarge,
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalS,
  },
  progressFill: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    transition: 'width 0.3s ease',
  },
  dateRange: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
});

type TabValue = 'overview' | 'usage' | 'costs' | 'performance' | 'settings';

export default function UsageAnalyticsPage() {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');
  const [loading, setLoading] = useState(true);
  const [dateRange, setDateRange] = useState<'7d' | '30d' | '90d' | 'all'>('30d');
  
  // Data states
  const [usageStats, setUsageStats] = useState<UsageStatistics | null>(null);
  const [costStats, setCostStats] = useState<CostStatistics | null>(null);
  const [perfStats, setPerfStats] = useState<PerformanceStatistics | null>(null);
  const [budgetStatus, setBudgetStatus] = useState<MonthlyBudgetStatus | null>(null);
  const [settings, setSettings] = useState<AnalyticsSettings | null>(null);
  const [dbInfo, setDbInfo] = useState<DatabaseInfo | null>(null);

  // Dialog states
  const [showClearDialog, setShowClearDialog] = useState(false);

  const getDateRange = (): { start: Date; end: Date } => {
    const end = new Date();
    const start = new Date();
    
    switch (dateRange) {
      case '7d':
        start.setDate(start.getDate() - 7);
        break;
      case '30d':
        start.setDate(start.getDate() - 30);
        break;
      case '90d':
        start.setDate(start.getDate() - 90);
        break;
      case 'all':
        start.setFullYear(2020, 0, 1); // Far past date
        break;
    }
    
    return { start, end };
  };

  const loadData = async () => {
    try {
      setLoading(true);
      const { start, end } = getDateRange();
      
      const [usage, costs, perf, budget, settingsData, dbData] = await Promise.all([
        getUsageStatistics(start, end),
        getCostStatistics(start, end),
        getPerformanceStatistics(start, end),
        getCurrentMonthBudget(),
        getAnalyticsSettings(),
        getDatabaseInfo(),
      ]);
      
      setUsageStats(usage);
      setCostStats(costs);
      setPerfStats(perf);
      setBudgetStatus(budget);
      setSettings(settingsData);
      setDbInfo(dbData);
    } catch (error) {
      console.error('Failed to load analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dateRange]);

  const handleTabSelect = (_: SelectTabEvent, data: SelectTabData) => {
    setSelectedTab(data.value as TabValue);
  };

  const handleExport = async (format: 'json' | 'csv') => {
    try {
      const { start, end } = getDateRange();
      const blob = await exportAnalyticsData(start, end, format);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `analytics-${new Date().toISOString().split('T')[0]}.${format}`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to export:', error);
    }
  };

  const handleCleanup = async () => {
    try {
      await triggerCleanup();
      await loadData();
    } catch (error) {
      console.error('Failed to cleanup:', error);
    }
  };

  const handleClearAll = async () => {
    try {
      await clearAllData();
      setShowClearDialog(false);
      await loadData();
    } catch (error) {
      console.error('Failed to clear data:', error);
    }
  };

  const handleSettingsUpdate = async (updates: Partial<AnalyticsSettings>) => {
    try {
      const updated = await updateAnalyticsSettings(updates);
      setSettings(updated);
    } catch (error) {
      console.error('Failed to update settings:', error);
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading analytics..." />
      </div>
    );
  }

  const _formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  };

  const formatDuration = (ms: number) => {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  };

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 4,
    }).format(amount);
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title1>Usage Analytics & Insights</Title1>
          <Text>Privacy-first analytics - all data stays local</Text>
        </div>
        <div className={styles.headerActions}>
          <Dropdown
            value={dateRange}
            onOptionSelect={(_, data) => setDateRange(data.optionValue as string)}
          >
            <Option value="7d">Last 7 days</Option>
            <Option value="30d">Last 30 days</Option>
            <Option value="90d">Last 90 days</Option>
            <Option value="all">All time</Option>
          </Dropdown>
          <Button
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('json')}
          >
            Export JSON
          </Button>
          <Button
            icon={<ArrowDownload24Regular />}
            onClick={() => handleExport('csv')}
          >
            Export CSV
          </Button>
        </div>
      </div>

      <div className={styles.privacyBanner}>
        <DataPie24Regular />
        <div style={{ flex: 1 }}>
          <Text weight="semibold">🔒 Privacy First</Text>
          <Text size={200} style={{ display: 'block' }}>
            All analytics data is stored locally on your machine. No data is sent to external servers.
          </Text>
        </div>
        {settings && !settings.isEnabled && (
          <Badge appearance="filled" color="warning">Analytics Disabled</Badge>
        )}
      </div>

      <TabList selectedValue={selectedTab} onTabSelect={handleTabSelect} className={styles.tabs}>
        <Tab value="overview" icon={<DataUsage24Regular />}>Overview</Tab>
        <Tab value="usage" icon={<DataUsage24Regular />}>Usage</Tab>
        <Tab value="costs" icon={<Money24Regular />}>Costs</Tab>
        <Tab value="performance" icon={<Timer24Regular />}>Performance</Tab>
        <Tab value="settings" icon={<Settings24Regular />}>Settings</Tab>
      </TabList>

      {selectedTab === 'overview' && usageStats && costStats && perfStats && budgetStatus && (
        <>
          <div className={styles.statsGrid}>
            <Card className={styles.statCard}>
              <div className={styles.statHeader}>
                <DataUsage24Regular />
                <Text weight="semibold">Total Generations</Text>
              </div>
              <div className={styles.statValue}>{usageStats.totalOperations.toLocaleString()}</div>
              <Text className={styles.statSubtext}>
                {usageStats.successRate.toFixed(1)}% success rate
              </Text>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statHeader}>
                <Money24Regular />
                <Text weight="semibold">Total Cost</Text>
              </div>
              <div className={styles.statValue}>
                {formatCurrency(costStats.totalCost, costStats.currency)}
              </div>
              <Text className={styles.statSubtext}>
                This month: {formatCurrency(budgetStatus.totalCost, budgetStatus.currency)}
              </Text>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statHeader}>
                <DataUsage24Regular />
                <Text weight="semibold">Total Tokens</Text>
              </div>
              <div className={styles.statValue}>
                {(usageStats.totalTokens / 1_000_000).toFixed(2)}M
              </div>
              <Text className={styles.statSubtext}>
                {((usageStats.totalInputTokens / usageStats.totalTokens) * 100).toFixed(0)}% input
              </Text>
            </Card>

            <Card className={styles.statCard}>
              <div className={styles.statHeader}>
                <Timer24Regular />
                <Text weight="semibold">Avg Duration</Text>
              </div>
              <div className={styles.statValue}>
                {formatDuration(perfStats.averageDurationMs)}
              </div>
              <Text className={styles.statSubtext}>
                Median: {formatDuration(perfStats.medianDurationMs)}
              </Text>
            </Card>
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title2>Budget Status</Title2>
            </div>
            <Card>
              <div style={{ padding: tokens.spacingVerticalL }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: tokens.spacingVerticalS }}>
                  <Text weight="semibold">Monthly Spending ({budgetStatus.yearMonth})</Text>
                  <Text weight="semibold">
                    {formatCurrency(budgetStatus.totalCost, budgetStatus.currency)}
                  </Text>
                </div>
                <div className={styles.progressBar}>
                  <div
                    className={styles.progressFill}
                    style={{ width: `${(budgetStatus.daysElapsed / budgetStatus.daysInMonth) * 100}%` }}
                  />
                </div>
                <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                  Day {budgetStatus.daysElapsed} of {budgetStatus.daysInMonth} • Projected: {formatCurrency(budgetStatus.projectedMonthlyTotal, budgetStatus.currency)}
                </Text>
              </div>
            </Card>
          </div>

          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <Title2>Provider Breakdown</Title2>
            </div>
            <div className={styles.breakdownGrid}>
              {Object.entries(usageStats.providerBreakdown).map(([provider, stats]) => (
                <Card key={provider}>
                  <div style={{ padding: tokens.spacingVerticalM }}>
                    <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
                      {provider}
                    </Text>
                    <Text size={200} style={{ display: 'block' }}>
                      {stats.totalOperations} operations
                    </Text>
                    <Text size={200} style={{ display: 'block' }}>
                      {((stats.totalInputTokens + stats.totalOutputTokens) / 1000).toFixed(1)}K tokens
                    </Text>
                    <Text size={200} style={{ display: 'block' }}>
                      {formatCurrency(costStats.costPerProvider[provider] || 0)}
                    </Text>
                  </div>
                </Card>
              ))}
            </div>
          </div>
        </>
      )}

      {selectedTab === 'settings' && settings && dbInfo && (
        <div className={styles.section}>
          <Card>
            <div style={{ padding: tokens.spacingVerticalL }}>
              <Title2>Analytics Settings</Title2>
              
              <div className={styles.settingRow}>
                <div>
                  <Text weight="semibold">Enable Analytics</Text>
                  <Text size={200} style={{ display: 'block' }}>
                    Track usage, costs, and performance
                  </Text>
                </div>
                <Switch
                  checked={settings.isEnabled}
                  onChange={(_, data) => handleSettingsUpdate({ isEnabled: data.checked })}
                />
              </div>

              <div className={styles.settingRow}>
                <div>
                  <Text weight="semibold">Auto Cleanup</Text>
                  <Text size={200} style={{ display: 'block' }}>
                    Automatically cleanup old data
                  </Text>
                </div>
                <Switch
                  checked={settings.autoCleanupEnabled}
                  onChange={(_, data) => handleSettingsUpdate({ autoCleanupEnabled: data.checked })}
                />
              </div>

              <div className={styles.settingRow}>
                <div>
                  <Text weight="semibold">Collect Hardware Metrics</Text>
                  <Text size={200} style={{ display: 'block' }}>
                    Track CPU, memory, and GPU usage
                  </Text>
                </div>
                <Switch
                  checked={settings.collectHardwareMetrics}
                  onChange={(_, data) => handleSettingsUpdate({ collectHardwareMetrics: data.checked })}
                />
              </div>

              <div style={{ marginTop: tokens.spacingVerticalXL }}>
                <Title3>Database Storage</Title3>
                <div style={{ marginTop: tokens.spacingVerticalM }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: tokens.spacingVerticalS }}>
                    <Text>Storage Used</Text>
                    <Text weight="semibold">
                      {dbInfo.estimatedSizeMB.toFixed(2)} MB / {dbInfo.maxSizeMB} MB
                    </Text>
                  </div>
                  <div className={styles.progressBar}>
                    <div
                      className={styles.progressFill}
                      style={{ width: `${Math.min(dbInfo.usagePercent, 100)}%` }}
                    />
                  </div>
                  <div style={{ marginTop: tokens.spacingVerticalM }}>
                    <Text size={200} style={{ display: 'block' }}>
                      {dbInfo.totalRecords.toLocaleString()} total records
                    </Text>
                    <Text size={200} style={{ display: 'block' }}>
                      {dbInfo.usageRecords.toLocaleString()} usage • {dbInfo.costRecords.toLocaleString()} cost • {dbInfo.performanceRecords.toLocaleString()} performance
                    </Text>
                  </div>
                </div>
              </div>

              <div style={{ marginTop: tokens.spacingVerticalXL, display: 'flex', gap: tokens.spacingHorizontalM }}>
                <Button onClick={handleCleanup}>Run Cleanup Now</Button>
                <Dialog open={showClearDialog} onOpenChange={(_, data) => setShowClearDialog(data.open)}>
                  <DialogTrigger>
                    <Button appearance="subtle" icon={<Delete24Regular />}>
                      Clear All Data
                    </Button>
                  </DialogTrigger>
                  <DialogSurface>
                    <DialogBody>
                      <DialogTitle>Clear All Analytics Data?</DialogTitle>
                      <DialogContent>
                        This will permanently delete all usage statistics, cost tracking, and performance metrics.
                        This action cannot be undone.
                      </DialogContent>
                      <DialogActions>
                        <DialogTrigger>
                          <Button appearance="secondary">Cancel</Button>
                        </DialogTrigger>
                        <Button appearance="primary" onClick={handleClearAll}>
                          Clear All Data
                        </Button>
                      </DialogActions>
                    </DialogBody>
                  </DialogSurface>
                </Dialog>
              </div>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
