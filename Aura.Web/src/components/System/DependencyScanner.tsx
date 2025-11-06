import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Button,
  Spinner,
  Badge,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  Warning24Regular,
  Dismiss24Regular,
  ArrowSyncCheckmark24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { DependencyScanResult, DependencyIssue, ScanProgressEvent } from '../../types/dependency-scan';
import { scanDependenciesStream, scanDependencies } from '../../services/dependencyScanService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
  },
  issueCard: {
    padding: tokens.spacingVerticalL,
    borderLeft: `4px solid`,
  },
  issueCardError: {
    borderLeftColor: tokens.colorPaletteRedBackground3,
  },
  issueCardWarning: {
    borderLeftColor: tokens.colorPaletteYellowBackground3,
  },
  issueCardInfo: {
    borderLeftColor: tokens.colorPaletteBlueBackground3,
  },
  issueHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
    gap: tokens.spacingHorizontalM,
  },
  issueContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    flex: 1,
  },
  issueActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
    flexWrap: 'wrap',
  },
  progressSection: {
    marginTop: tokens.spacingVerticalM,
  },
  statusIcon: {
    fontSize: '32px',
    flexShrink: 0,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

export interface DependencyScannerProps {
  /**
   * Auto-start scanning on mount
   */
  autoScan?: boolean;
  
  /**
   * Callback when scan completes
   */
  onScanComplete?: (result: DependencyScanResult) => void;
  
  /**
   * Callback when fix action is triggered
   */
  onFixAction?: (actionId: string, issue: DependencyIssue) => Promise<void>;
  
  /**
   * Show rescan button
   */
  showRescanButton?: boolean;
}

export function DependencyScanner({
  autoScan = false,
  onScanComplete,
  onFixAction,
  showRescanButton = true,
}: DependencyScannerProps) {
  const styles = useStyles();
  const [isScanning, setIsScanning] = useState(false);
  const [scanResult, setScanResult] = useState<DependencyScanResult | null>(null);
  const [progress, setProgress] = useState<string>('');
  const [percentComplete, setPercentComplete] = useState(0);
  const [eventSource, setEventSource] = useState<EventSource | null>(null);
  const [fixingActions, setFixingActions] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (autoScan) {
      handleScan();
    }
    
    return () => {
      eventSource?.close();
    };
  }, [autoScan]);

  const handleScan = async (forceRefresh = false) => {
    setIsScanning(true);
    setProgress('Starting scan...');
    setPercentComplete(0);
    setScanResult(null);

    // Close existing EventSource if any
    eventSource?.close();

    try {
      const es = scanDependenciesStream(
        forceRefresh,
        (event: ScanProgressEvent) => {
          if (event.event === 'step') {
            setProgress(event.message || '');
            setPercentComplete(event.percentComplete || 0);
          } else if (event.event === 'issue') {
            // Issues are collected in the final result
          }
        },
        async (event: ScanProgressEvent) => {
          setProgress('Scan completed');
          setPercentComplete(100);
          setIsScanning(false);

          // Fetch the full result
          const result = await scanDependencies(false);
          setScanResult(result);
          onScanComplete?.(result);
        },
        (error: Error) => {
          console.error('Scan error:', error);
          setProgress(`Error: ${error.message}`);
          setIsScanning(false);
        }
      );

      setEventSource(es);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      console.error('Scan failed:', errorMessage);
      setProgress(`Error: ${errorMessage}`);
      setIsScanning(false);
    }
  };

  const handleFixAction = async (actionId: string, issue: DependencyIssue) => {
    if (!onFixAction) return;

    setFixingActions((prev) => new Set(prev).add(actionId));

    try {
      await onFixAction(actionId, issue);
      // Rescan after fix
      await handleScan(true);
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Fix action failed';
      console.error('Fix action failed:', errorMessage);
    } finally {
      setFixingActions((prev) => {
        const next = new Set(prev);
        next.delete(actionId);
        return next;
      });
    }
  };

  const getSeverityIcon = (severity: DependencyIssue['severity']) => {
    switch (severity) {
      case 'Error':
        return (
          <Dismiss24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
      case 'Warning':
        return (
          <Warning24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteYellowForeground1 }}
          />
        );
      case 'Info':
        return (
          <Info24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteBlueForeground1 }}
          />
        );
    }
  };

  const getSeverityBadge = (severity: DependencyIssue['severity']) => {
    switch (severity) {
      case 'Error':
        return <Badge appearance="filled" color="danger">Error</Badge>;
      case 'Warning':
        return <Badge appearance="filled" color="warning">Warning</Badge>;
      case 'Info':
        return <Badge appearance="filled" color="informative">Info</Badge>;
    }
  };

  const getIssueCardClass = (severity: DependencyIssue['severity']) => {
    switch (severity) {
      case 'Error':
        return `${styles.issueCard} ${styles.issueCardError}`;
      case 'Warning':
        return `${styles.issueCard} ${styles.issueCardWarning}`;
      case 'Info':
        return `${styles.issueCard} ${styles.issueCardInfo}`;
    }
  };

  const errorCount = scanResult?.issues.filter((i) => i.severity === 'Error').length || 0;
  const warningCount = scanResult?.issues.filter((i) => i.severity === 'Warning').length || 0;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>System Validation</Title2>
        <Text>
          Checking your system for required components and configuration.
        </Text>
      </div>

      {/* Summary Card */}
      {scanResult ? (
        <Card className={styles.summaryCard}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
              {errorCount === 0 ? (
                <Checkmark24Regular
                  style={{ fontSize: '32px', color: tokens.colorPaletteGreenForeground1 }}
                />
              ) : (
                <Warning24Regular
                  style={{ fontSize: '32px', color: tokens.colorPaletteRedForeground1 }}
                />
              )}
              <div>
                <Title3>
                  {errorCount === 0
                    ? 'All Checks Passed!'
                    : `${errorCount} ${errorCount === 1 ? 'Issue' : 'Issues'} Found`}
                </Title3>
                <Text size={300}>
                  {errorCount === 0 && warningCount === 0
                    ? 'Your system is ready to create videos.'
                    : `${errorCount > 0 ? `${errorCount} error${errorCount !== 1 ? 's' : ''}` : ''}${errorCount > 0 && warningCount > 0 ? ', ' : ''}${warningCount > 0 ? `${warningCount} warning${warningCount !== 1 ? 's' : ''}` : ''}`}
                </Text>
              </div>
            </div>
            {showRescanButton && (
              <Button
                appearance="secondary"
                onClick={() => handleScan(true)}
                disabled={isScanning}
                icon={isScanning ? <Spinner size="tiny" /> : <ArrowSyncCheckmark24Regular />}
              >
                {isScanning ? 'Scanning...' : 'Rescan'}
              </Button>
            )}
          </div>
        </Card>
      ) : null}

      {/* Progress Bar */}
      {isScanning && (
        <div className={styles.progressSection}>
          <ProgressBar value={percentComplete} max={100} />
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            {progress}
          </Text>
        </div>
      )}

      {/* Issues List */}
      {scanResult && scanResult.issues.length > 0 ? (
        scanResult.issues.map((issue, index) => (
          <Card key={`${issue.id}-${index}`} className={getIssueCardClass(issue.severity)}>
            <div className={styles.issueHeader}>
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, flex: 1 }}>
                {getSeverityIcon(issue.severity)}
                <div className={styles.issueContent}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                    <Title3>{issue.title}</Title3>
                    {getSeverityBadge(issue.severity)}
                  </div>
                  <Text>{issue.description}</Text>
                  <Text weight="semibold" style={{ marginTop: tokens.spacingVerticalS }}>
                    Remediation:
                  </Text>
                  <Text size={300}>{issue.remediation}</Text>
                </div>
              </div>
            </div>

            {/* Fix Actions */}
            {(issue.actionId || issue.docsUrl) && (
              <div className={styles.issueActions}>
                {issue.actionId && onFixAction && (
                  <Button
                    appearance="primary"
                    onClick={() => handleFixAction(issue.actionId!, issue)}
                    disabled={fixingActions.has(issue.actionId)}
                  >
                    {fixingActions.has(issue.actionId) ? (
                      <>
                        <Spinner size="tiny" />
                        <span style={{ marginLeft: tokens.spacingHorizontalXS }}>Fixing...</span>
                      </>
                    ) : (
                      `Fix ${issue.title.replace(/Not Found|Missing|Unavailable|Error/gi, '').trim() || 'Issue'}`
                    )}
                  </Button>
                )}
                {issue.docsUrl && (
                  <Button
                    appearance="secondary"
                    as="a"
                    href={issue.docsUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Learn More
                  </Button>
                )}
              </div>
            )}
          </Card>
        ))
      ) : scanResult && scanResult.issues.length === 0 ? (
        <div className={styles.emptyState}>
          <Checkmark24Regular
            style={{ fontSize: '48px', color: tokens.colorPaletteGreenForeground1 }}
          />
          <Title3 style={{ marginTop: tokens.spacingVerticalM }}>
            All systems go!
          </Title3>
          <Text>No issues detected. Your system is ready to create amazing videos.</Text>
        </div>
      ) : !isScanning ? (
        <Card>
          <div style={{ textAlign: 'center', padding: tokens.spacingVerticalL }}>
            <Text>Click the button below to validate your system.</Text>
            <Button
              appearance="primary"
              onClick={() => handleScan(false)}
              style={{ marginTop: tokens.spacingVerticalM }}
            >
              Start Validation
            </Button>
          </div>
        </Card>
      ) : null}
    </div>
  );
}
