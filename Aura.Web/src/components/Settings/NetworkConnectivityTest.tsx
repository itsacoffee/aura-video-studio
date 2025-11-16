import { Button, makeStyles, Spinner, Text, tokens } from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  DismissCircle24Regular,
  PlugDisconnected24Regular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import type { FC } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  testButton: {
    marginBottom: tokens.spacingVerticalM,
  },
  resultsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  resultCard: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  resultHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  successIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  failureIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
  },
  detailsText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
  },
  timestamp: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
    marginTop: tokens.spacingVerticalM,
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  errorCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground1,
    border: `1px solid ${tokens.colorPaletteRedBorder1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  warningCard: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground1,
    border: `1px solid ${tokens.colorPaletteYellowBorder1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
});

interface TestResult {
  name: string;
  success: boolean;
  statusCode?: number;
  statusText?: string;
  elapsedMs: number;
  reachable: boolean;
  errorType?: string;
  errorMessage?: string;
}

interface NetworkTestResults {
  success: boolean;
  overallStatus: string;
  timestamp: string;
  totalElapsedMs: number;
  tests: {
    google: TestResult;
    openai: TestResult;
    pexels: TestResult;
    dns: TestResult;
  };
}

export const NetworkConnectivityTest: FC = () => {
  const styles = useStyles();
  const [isLoading, setIsLoading] = useState(false);
  const [results, setResults] = useState<NetworkTestResults | null>(null);
  const [error, setError] = useState<string | null>(null);

  const runNetworkTests = async () => {
    setIsLoading(true);
    setError(null);
    setResults(null);

    try {
      const response = await fetch(apiUrl('/api/diagnostics/network-test'), {
        method: 'GET',
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = (await response.json()) as NetworkTestResults;
      setResults(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      setError(`Failed to run network tests: ${errorMessage}`);
      console.error('Network test error:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const renderTestResult = (test: TestResult, displayName: string) => {
    const Icon = test.success
      ? CheckmarkCircle24Regular
      : test.reachable === false
        ? PlugDisconnected24Regular
        : DismissCircle24Regular;

    const iconClass = test.success
      ? styles.successIcon
      : test.reachable === false
        ? styles.warningIcon
        : styles.failureIcon;

    return (
      <div key={test.name} className={styles.resultCard}>
        <div className={styles.resultHeader}>
          <Icon className={iconClass} />
          <Text weight="semibold">{displayName}</Text>
        </div>
        <div>
          <Text size={200}>
            Status: {test.success ? 'Connected' : test.reachable === false ? 'Timeout' : 'Failed'}
          </Text>
        </div>
        {test.success && (
          <div>
            <Text size={200} className={styles.detailsText}>
              Response time: {test.elapsedMs.toFixed(0)}ms
            </Text>
            {test.statusCode && (
              <Text size={200} className={styles.detailsText}>
                Status code: {test.statusCode} ({test.statusText})
              </Text>
            )}
          </div>
        )}
        {!test.success && test.errorMessage && (
          <Text
            size={200}
            className={styles.detailsText}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          >
            Error: {test.errorMessage}
          </Text>
        )}
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <Text size={300}>
        Test network connectivity to external APIs and services. This helps troubleshoot connection
        issues with OpenAI, Pexels, and general internet access.
      </Text>

      <Button
        appearance="primary"
        onClick={runNetworkTests}
        disabled={isLoading}
        className={styles.testButton}
      >
        {isLoading ? 'Running Tests...' : 'Run Network Tests'}
      </Button>

      {isLoading && (
        <div className={styles.loadingContainer}>
          <Spinner size="medium" />
          <Text>Testing network connectivity...</Text>
        </div>
      )}

      {error && (
        <div className={styles.errorCard}>
          <div className={styles.resultHeader}>
            <DismissCircle24Regular className={styles.failureIcon} />
            <Text weight="semibold">Test Failed</Text>
          </div>
          <Text size={200}>{error}</Text>
        </div>
      )}

      {results && !isLoading && (
        <>
          <div className={styles.resultsGrid}>
            {renderTestResult(results.tests.google, 'Internet Connectivity')}
            {renderTestResult(results.tests.openai, 'OpenAI API')}
            {renderTestResult(results.tests.pexels, 'Pexels API')}
            {renderTestResult(results.tests.dns, 'DNS Resolution')}
          </div>

          <Text className={styles.timestamp}>
            Last tested: {new Date(results.timestamp).toLocaleString()} | Total time:{' '}
            {results.totalElapsedMs.toFixed(0)}ms
          </Text>

          {!results.success && (
            <div className={styles.warningCard}>
              <Text
                weight="semibold"
                style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}
              >
                ⚠️ Some Tests Failed
              </Text>
              <Text size={200}>
                One or more network tests failed. Check your internet connection, firewall settings,
                and API key configuration. If the problem persists, contact support.
              </Text>
            </div>
          )}
        </>
      )}
    </div>
  );
};
