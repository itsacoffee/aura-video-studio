/**
 * System Requirements Checker
 * Validates system capabilities and displays warnings
 */

import {
  Badge,
  Button,
  Card,
  makeStyles,
  MessageBar,
  MessageBarBody,
  shorthands,
  Text,
  tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  DismissCircle24Regular,
  Info24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { loggingService } from '../../services/loggingService';

const useStyles = makeStyles({
  container: {
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  card: {
    maxWidth: '800px',
    ...shorthands.margin('0', 'auto'),
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  checksList: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
  checkItem: {
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
    ...shorthands.padding(tokens.spacingVerticalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorNeutralBackground2,
  },
  checkIcon: {
    flexShrink: 0,
  },
  checkContent: {
    flex: 1,
  },
  actions: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    justifyContent: 'center',
  },
});

interface SystemCheck {
  id: string;
  name: string;
  description: string;
  status: 'pass' | 'warn' | 'fail' | 'info';
  message?: string;
}

interface SystemRequirementsCheckerProps {
  onContinue?: () => void;
  showOnlyFailures?: boolean;
}

export function SystemRequirementsChecker({
  onContinue,
  showOnlyFailures = false,
}: SystemRequirementsCheckerProps) {
  const styles = useStyles();
  const [checks, setChecks] = useState<SystemCheck[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    performSystemChecks();
  }, []);

  const performSystemChecks = async () => {
    setLoading(true);
    const results: SystemCheck[] = [];

    try {
      // Check browser support
      const isSupported = 'fetch' in window && 'localStorage' in window && 'Promise' in window;
      results.push({
        id: 'browser',
        name: 'Browser Support',
        description: 'Modern browser with required APIs',
        status: isSupported ? 'pass' : 'fail',
        message: isSupported
          ? 'Your browser supports all required features'
          : 'Your browser does not support all required features. Please upgrade.',
      });

      // Check local storage
      try {
        localStorage.setItem('test', 'test');
        localStorage.removeItem('test');
        results.push({
          id: 'localStorage',
          name: 'Local Storage',
          description: 'Ability to store data locally',
          status: 'pass',
          message: 'Local storage is available',
        });
      } catch (error) {
        results.push({
          id: 'localStorage',
          name: 'Local Storage',
          description: 'Ability to store data locally',
          status: 'fail',
          message:
            'Local storage is not available. Some features may not work. Check browser privacy settings.',
        });
      }

      // Check available memory (if available)
      if ('memory' in performance && (performance as any).memory) {
        const memory = (performance as any).memory;
        const usedMemoryMB = memory.usedJSHeapSize / 1024 / 1024;
        const totalMemoryMB = memory.jsHeapSizeLimit / 1024 / 1024;
        const percentUsed = (usedMemoryMB / totalMemoryMB) * 100;

        results.push({
          id: 'memory',
          name: 'Memory Usage',
          description: 'Available JavaScript heap memory',
          status: percentUsed > 80 ? 'warn' : 'pass',
          message: `Using ${usedMemoryMB.toFixed(0)}MB of ${totalMemoryMB.toFixed(0)}MB (${percentUsed.toFixed(1)}%)`,
        });
      }

      // Check network connectivity
      const isOnline = navigator.onLine;
      results.push({
        id: 'network',
        name: 'Network Connection',
        description: 'Internet connectivity',
        status: isOnline ? 'pass' : 'warn',
        message: isOnline
          ? 'Connected to the internet'
          : 'No internet connection. Some features may not work.',
      });

      // Check WebGL support (for video rendering)
      const canvas = document.createElement('canvas');
      const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
      results.push({
        id: 'webgl',
        name: 'WebGL Support',
        description: 'Hardware-accelerated graphics',
        status: gl ? 'pass' : 'warn',
        message: gl
          ? 'WebGL is supported'
          : 'WebGL is not available. Video preview may be slower.',
      });

      // Check Web Workers support
      const hasWorkers = 'Worker' in window;
      results.push({
        id: 'workers',
        name: 'Web Workers',
        description: 'Background processing support',
        status: hasWorkers ? 'pass' : 'warn',
        message: hasWorkers
          ? 'Web Workers are supported'
          : 'Web Workers not available. Performance may be reduced.',
      });

      // Check IndexedDB
      const hasIndexedDB = 'indexedDB' in window;
      results.push({
        id: 'indexeddb',
        name: 'IndexedDB',
        description: 'Database for large data storage',
        status: hasIndexedDB ? 'pass' : 'info',
        message: hasIndexedDB
          ? 'IndexedDB is available'
          : 'IndexedDB not available. Some advanced features may be limited.',
      });

      // Check screen size
      const screenWidth = window.screen.width;
      const screenHeight = window.screen.height;
      const isSmallScreen = screenWidth < 1024 || screenHeight < 768;
      results.push({
        id: 'screen',
        name: 'Screen Resolution',
        description: 'Minimum 1024x768 recommended',
        status: isSmallScreen ? 'warn' : 'pass',
        message: `${screenWidth}x${screenHeight}${isSmallScreen ? ' - Small screen detected. Some features may be cramped.' : ''}`,
      });

      setChecks(results);

      // Log results
      loggingService.info(
        'System requirements check completed',
        'SystemRequirementsChecker',
        'performSystemChecks',
        {
          results: results.map((r) => ({ id: r.id, status: r.status })),
        }
      );
    } catch (error) {
      loggingService.error(
        'Failed to perform system checks',
        error as Error,
        'SystemRequirementsChecker',
        'performSystemChecks'
      );
    } finally {
      setLoading(false);
    }
  };

  const getIcon = (status: SystemCheck['status']) => {
    switch (status) {
      case 'pass':
        return <CheckmarkCircle24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'warn':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'fail':
        return <DismissCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      case 'info':
        return <Info24Regular style={{ color: tokens.colorNeutralForeground2 }} />;
    }
  };

  const getStatusBadge = (status: SystemCheck['status']) => {
    const props = {
      pass: { appearance: 'filled' as const, color: 'success' as const },
      warn: { appearance: 'filled' as const, color: 'warning' as const },
      fail: { appearance: 'filled' as const, color: 'danger' as const },
      info: { appearance: 'filled' as const, color: 'informative' as const },
    };
    return <Badge {...props[status]}>{status.toUpperCase()}</Badge>;
  };

  const failedChecks = checks.filter((c) => c.status === 'fail');
  const warningChecks = checks.filter((c) => c.status === 'warn');
  const hasIssues = failedChecks.length > 0 || warningChecks.length > 0;

  const visibleChecks = showOnlyFailures
    ? checks.filter((c) => c.status === 'fail' || c.status === 'warn')
    : checks;

  if (loading) {
    return (
      <div className={styles.container}>
        <Card className={styles.card}>
          <div className={styles.content}>
            <Text>Checking system requirements...</Text>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Card className={styles.card}>
        <div className={styles.content}>
          <Text size={600} weight="semibold" block>
            System Requirements Check
          </Text>

          {failedChecks.length > 0 && (
            <MessageBar intent="error">
              <MessageBarBody>
                <Text weight="semibold">
                  {failedChecks.length} critical issue(s) detected
                </Text>
                <Text size={300} block>
                  Some features may not work properly. Please address these issues.
                </Text>
              </MessageBarBody>
            </MessageBar>
          )}

          {warningChecks.length > 0 && failedChecks.length === 0 && (
            <MessageBar intent="warning">
              <MessageBarBody>
                <Text weight="semibold">{warningChecks.length} warning(s) detected</Text>
                <Text size={300} block>
                  The application will work, but some features may be limited.
                </Text>
              </MessageBarBody>
            </MessageBar>
          )}

          {!hasIssues && (
            <MessageBar intent="success">
              <MessageBarBody>
                <Text>All system requirements met! You're ready to go.</Text>
              </MessageBarBody>
            </MessageBar>
          )}

          <div className={styles.checksList}>
            {visibleChecks.map((check) => (
              <div key={check.id} className={styles.checkItem}>
                <div className={styles.checkIcon}>{getIcon(check.status)}</div>
                <div className={styles.checkContent}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                    <Text weight="semibold">{check.name}</Text>
                    {getStatusBadge(check.status)}
                  </div>
                  <Text size={300} block style={{ color: tokens.colorNeutralForeground2 }}>
                    {check.description}
                  </Text>
                  {check.message && (
                    <Text size={300} block>
                      {check.message}
                    </Text>
                  )}
                </div>
              </div>
            ))}
          </div>

          {onContinue && (
            <div className={styles.actions}>
              <Button appearance="primary" onClick={onContinue}>
                Continue Anyway
              </Button>
              <Button appearance="secondary" onClick={performSystemChecks}>
                Re-check
              </Button>
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}
