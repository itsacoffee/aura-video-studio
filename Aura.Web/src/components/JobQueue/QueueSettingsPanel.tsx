/**
 * QueueSettingsPanel Component
 * Configuration panel for job queue settings
 */

import {
  makeStyles,
  tokens,
  Title3,
  Body1,
  Body1Strong,
  Caption1,
  Button,
  Switch,
  SpinButton,
  Card,
  Divider,
} from '@fluentui/react-components';
import { Save24Regular, ArrowReset24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { useJobQueue } from '../../hooks/useJobQueue';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXL,
    maxWidth: '800px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  settingRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  settingInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    flex: 1,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
  },
  statsCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  statsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  statItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
});

export function QueueSettingsPanel() {
  const styles = useStyles();
  const {
    configuration,
    statistics,
    updateConfiguration,
    isLoadingJobs,
  } = useJobQueue();

  const [maxConcurrentJobs, setMaxConcurrentJobs] = useState(2);
  const [isEnabled, setIsEnabled] = useState(true);
  const [hasChanges, setHasChanges] = useState(false);

  // Initialize values from configuration
  useEffect(() => {
    if (configuration) {
      setMaxConcurrentJobs(configuration.maxConcurrentJobs);
      setIsEnabled(configuration.isEnabled);
      setHasChanges(false);
    }
  }, [configuration]);

  const handleMaxConcurrentJobsChange = (value: number | null) => {
    if (value !== null && value >= 1 && value <= 10) {
      setMaxConcurrentJobs(value);
      setHasChanges(true);
    }
  };

  const handleIsEnabledChange = (checked: boolean) => {
    setIsEnabled(checked);
    setHasChanges(true);
  };

  const handleSave = async () => {
    try {
      await updateConfiguration(maxConcurrentJobs, isEnabled);
      setHasChanges(false);
    } catch (error) {
      console.error('Failed to update configuration:', error);
    }
  };

  const handleReset = () => {
    if (configuration) {
      setMaxConcurrentJobs(configuration.maxConcurrentJobs);
      setIsEnabled(configuration.isEnabled);
      setHasChanges(false);
    }
  };

  if (!configuration) {
    return (
      <div className={styles.container}>
        <Body1>Loading configuration...</Body1>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>Queue Settings</Title3>
        <Caption1>Configure how the job queue processes video generation tasks</Caption1>
      </div>

      <div className={styles.section}>
        <Body1Strong>Processing Settings</Body1Strong>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Queue Enabled</Body1>
            <Caption1>Enable or disable the job queue processing</Caption1>
          </div>
          <Switch
            checked={isEnabled}
            onChange={(_, data) => handleIsEnabledChange(data.checked)}
          />
        </div>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Max Concurrent Jobs</Body1>
            <Caption1>Maximum number of jobs that can run simultaneously (1-10)</Caption1>
          </div>
          <SpinButton
            value={maxConcurrentJobs}
            onChange={(_, data) => handleMaxConcurrentJobsChange(data.value)}
            min={1}
            max={10}
            step={1}
            style={{ width: '100px' }}
          />
        </div>

        <Divider />

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Pause on Battery</Body1>
            <Caption1>
              {configuration.pauseOnBattery ? 'Enabled' : 'Disabled'} - Automatically pause processing when on battery power
            </Caption1>
          </div>
        </div>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>CPU Throttle Threshold</Body1>
            <Caption1>{configuration.cpuThrottleThreshold}% - Pause processing if CPU usage exceeds this</Caption1>
          </div>
        </div>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Memory Throttle Threshold</Body1>
            <Caption1>{configuration.memoryThrottleThreshold}% - Pause processing if memory usage exceeds this</Caption1>
          </div>
        </div>
      </div>

      <div className={styles.section}>
        <Body1Strong>Retention Policies</Body1Strong>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Job History Retention</Body1>
            <Caption1>{configuration.jobHistoryRetentionDays} days - How long to keep completed job history</Caption1>
          </div>
        </div>

        <div className={styles.settingRow}>
          <div className={styles.settingInfo}>
            <Body1>Failed Job Retention</Body1>
            <Caption1>{configuration.failedJobRetentionDays} days - How long to keep failed jobs for debugging</Caption1>
          </div>
        </div>
      </div>

      {statistics && (
        <Card className={styles.statsCard}>
          <Body1Strong>Queue Statistics</Body1Strong>
          <div className={styles.statsGrid}>
            <div className={styles.statItem}>
              <Caption1>Total Jobs</Caption1>
              <Body1Strong>{statistics.totalJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Pending</Caption1>
              <Body1Strong>{statistics.pendingJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Processing</Caption1>
              <Body1Strong>{statistics.processingJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Completed</Caption1>
              <Body1Strong>{statistics.completedJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Failed</Caption1>
              <Body1Strong>{statistics.failedJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Cancelled</Caption1>
              <Body1Strong>{statistics.cancelledJobs}</Body1Strong>
            </div>
            <div className={styles.statItem}>
              <Caption1>Active Workers</Caption1>
              <Body1Strong>{statistics.activeWorkers}</Body1Strong>
            </div>
          </div>
        </Card>
      )}

      <div className={styles.actions}>
        <Button
          appearance="secondary"
          icon={<ArrowReset24Regular />}
          onClick={handleReset}
          disabled={!hasChanges}
        >
          Reset
        </Button>
        <Button
          appearance="primary"
          icon={<Save24Regular />}
          onClick={handleSave}
          disabled={!hasChanges || isLoadingJobs}
        >
          Save Changes
        </Button>
      </div>

      <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
        Note: Some settings like throttle thresholds and retention policies require backend configuration
        and cannot be changed from the UI.
      </Caption1>
    </div>
  );
}
