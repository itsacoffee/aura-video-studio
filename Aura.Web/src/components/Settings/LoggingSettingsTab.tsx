/**
 * Logging Settings Tab Component
 * Allows users to configure logging behavior and view log statistics
 */

import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Field,
  Switch,
  Select,
  Body1,
  Caption1,
} from '@fluentui/react-components';
import {
  Save24Regular,
  Delete24Regular,
  Document24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { loggingService, LogLevel } from '../../services/loggingService';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalXL,
  },
  stats: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  statCard: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-start',
  },
  infoIcon: {
    color: tokens.colorBrandForeground1,
    flexShrink: 0,
  },
});

export function LoggingSettingsTab() {
  const styles = useStyles();
  const [config, setConfig] = useState(loggingService.getConfig());
  const [logStats, setLogStats] = useState({
    total: 0,
    debug: 0,
    info: 0,
    warn: 0,
    error: 0,
  });
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    // Get current log statistics
    const logs = loggingService.getLogs();
    setLogStats({
      total: logs.length,
      debug: logs.filter((l) => l.level === 'debug').length,
      info: logs.filter((l) => l.level === 'info').length,
      warn: logs.filter((l) => l.level === 'warn').length,
      error: logs.filter((l) => l.level === 'error').length,
    });
  }, []);

  const handleConfigChange = (key: string, value: any) => {
    const newConfig = { ...config, [key]: value };
    setConfig(newConfig);
    setHasChanges(true);
  };

  const handleSave = () => {
    loggingService.configure(config);
    setHasChanges(false);
  };

  const handleReset = () => {
    const defaultConfig = loggingService.getConfig();
    setConfig(defaultConfig);
    setHasChanges(false);
  };

  const handleClearLogs = () => {
    if (confirm('Are you sure you want to clear all logs? This action cannot be undone.')) {
      loggingService.clearLogs();
      setLogStats({
        total: 0,
        debug: 0,
        info: 0,
        warn: 0,
        error: 0,
      });
    }
  };

  const handleExportLogs = () => {
    const logsJson = loggingService.exportLogs();
    const blob = new Blob([logsJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `aura-logs-${new Date().toISOString()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <Card className={styles.section}>
      <Title2>Logging Settings</Title2>
      <div className={styles.form}>
        <div className={styles.infoBox}>
          <Info24Regular className={styles.infoIcon} />
          <div>
            <Body1 style={{ fontWeight: 600 }}>About Logging</Body1>
            <Caption1>
              The logging system captures application events, errors, and performance metrics to
              help diagnose issues. Logs are stored locally in your browser.
            </Caption1>
          </div>
        </div>

        <Field label="Enable Console Logging">
          <Switch
            checked={config.enableConsole}
            onChange={(_, data) => handleConfigChange('enableConsole', data.checked)}
          />
          <Caption1>Display logs in the browser console (recommended for debugging)</Caption1>
        </Field>

        <Field label="Enable Log Persistence">
          <Switch
            checked={config.enablePersistence}
            onChange={(_, data) => handleConfigChange('enablePersistence', data.checked)}
          />
          <Caption1>
            Save logs to browser storage for viewing in the log viewer (Ctrl+Shift+L)
          </Caption1>
        </Field>

        <Field label="Minimum Log Level">
          <Select
            value={config.minLogLevel}
            onChange={(_, data) => handleConfigChange('minLogLevel', data.value as LogLevel)}
          >
            <option value="debug">Debug (All logs)</option>
            <option value="info">Info (Normal verbosity)</option>
            <option value="warn">Warning (Warnings and errors only)</option>
            <option value="error">Error (Errors only)</option>
          </Select>
          <Caption1>
            Only logs at or above this level will be captured. Debug logs can be verbose.
          </Caption1>
        </Field>

        <Field label="Maximum Stored Logs">
          <Select
            value={config.maxStoredLogs.toString()}
            onChange={(_, data) => handleConfigChange('maxStoredLogs', parseInt(data.value))}
          >
            <option value="100">100 logs</option>
            <option value="500">500 logs</option>
            <option value="1000">1,000 logs (default)</option>
            <option value="5000">5,000 logs</option>
            <option value="10000">10,000 logs</option>
          </Select>
          <Caption1>Maximum number of logs to keep in browser storage</Caption1>
        </Field>

        <div>
          <Title2>Log Statistics</Title2>
          <div className={styles.stats}>
            <Card className={styles.statCard}>
              <Caption1>Total Logs</Caption1>
              <Body1 style={{ fontWeight: 600 }}>{logStats.total}</Body1>
            </Card>
            <Card className={styles.statCard}>
              <Caption1>Debug</Caption1>
              <Body1 style={{ fontWeight: 600 }}>{logStats.debug}</Body1>
            </Card>
            <Card className={styles.statCard}>
              <Caption1>Info</Caption1>
              <Body1 style={{ fontWeight: 600 }}>{logStats.info}</Body1>
            </Card>
            <Card className={styles.statCard}>
              <Caption1>Warnings</Caption1>
              <Body1 style={{ fontWeight: 600 }}>{logStats.warn}</Body1>
            </Card>
            <Card className={styles.statCard}>
              <Caption1>Errors</Caption1>
              <Body1 style={{ fontWeight: 600 }}>{logStats.error}</Body1>
            </Card>
          </div>
        </div>

        {hasChanges && (
          <div
            style={{
              padding: tokens.spacingVerticalM,
              backgroundColor: tokens.colorPaletteYellowBackground2,
              borderRadius: tokens.borderRadiusMedium,
            }}
          >
            <Text>⚠️ You have unsaved changes to logging settings</Text>
          </div>
        )}

        <div className={styles.actions}>
          <Button appearance="primary" icon={<Save24Regular />} onClick={handleSave}>
            Save Settings
          </Button>
          <Button appearance="secondary" onClick={handleReset} disabled={!hasChanges}>
            Reset
          </Button>
          <Button
            appearance="secondary"
            icon={<Document24Regular />}
            onClick={handleExportLogs}
            disabled={logStats.total === 0}
          >
            Export Logs
          </Button>
          <Button
            appearance="secondary"
            icon={<Delete24Regular />}
            onClick={handleClearLogs}
            disabled={logStats.total === 0}
          >
            Clear Logs
          </Button>
        </div>
      </div>
    </Card>
  );
}
