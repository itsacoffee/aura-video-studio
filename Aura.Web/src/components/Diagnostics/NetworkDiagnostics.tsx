import {
  makeStyles,
  tokens,
  Card,
  Title3,
  Subtitle2,
  Text,
  Button,
  Spinner,
  MessageBar,
  MessageBarBody,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import {
  CheckmarkCircle20Filled,
  DismissCircle20Filled,
  Warning20Filled,
  Copy20Regular,
  ArrowClockwise20Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import type { FC } from 'react';
import { apiUrl } from '../../config/api';
import { loggingService } from '../../services/loggingService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  statusCard: {
    padding: tokens.spacingVerticalL,
  },
  statusRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  statusIcon: {
    fontSize: '20px',
  },
  issuesList: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  issueItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  providerGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  providerCard: {
    padding: tokens.spacingVerticalM,
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '200px',
  },
});

interface DiagnosticsResponse {
  correlationId: string;
  timestamp: string;
  backendReachable: boolean;
  backendVersion: string;
  configuration: {
    isValid: boolean;
    errorCode?: string;
    errorMessage?: string;
    issues: string[];
  };
  ffmpeg: {
    installed: boolean;
    valid: boolean;
    version?: string;
    path?: string;
    errorCode?: string;
    errorMessage?: string;
  };
  providers: Array<{
    name: string;
    reachable: boolean;
    errorCode?: string;
    errorMessage?: string;
  }>;
  network: {
    corsConfigured: boolean;
    baseUrlConfigured: boolean;
    issues: string[];
  };
}

export const NetworkDiagnostics: FC = () => {
  const styles = useStyles();
  const [diagnostics, setDiagnostics] = useState<DiagnosticsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const runDiagnostics = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      loggingService.info('Running network diagnostics', 'NetworkDiagnostics');

      const response = await fetch(apiUrl('/api/system/network/diagnostics'));

      if (!response.ok) {
        throw new Error(`Diagnostics request failed: ${response.status} ${response.statusText}`);
      }

      const data = (await response.json()) as DiagnosticsResponse;
      setDiagnostics(data);

      loggingService.info('Network diagnostics completed', 'NetworkDiagnostics', undefined, {
        correlationId: data.correlationId,
      });
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to run diagnostics';
      setError(errorMessage);

      loggingService.error(
        'Network diagnostics failed',
        err instanceof Error ? err : new Error(errorMessage),
        'NetworkDiagnostics'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void runDiagnostics();
  }, [runDiagnostics]);

  const copyToClipboard = useCallback(() => {
    if (!diagnostics) return;

    const summary = `
Aura Video Studio - Network Diagnostics
========================================
Timestamp: ${diagnostics.timestamp}
Correlation ID: ${diagnostics.correlationId}

Backend Status:
- Reachable: ${diagnostics.backendReachable ? 'Yes' : 'No'}
- Version: ${diagnostics.backendVersion}

Configuration:
- Valid: ${diagnostics.configuration.isValid ? 'Yes' : 'No'}
${diagnostics.configuration.issues.length > 0 ? `- Issues:\n  ${diagnostics.configuration.issues.join('\n  ')}` : ''}

FFmpeg:
- Installed: ${diagnostics.ffmpeg.installed ? 'Yes' : 'No'}
- Valid: ${diagnostics.ffmpeg.valid ? 'Yes' : 'No'}
${diagnostics.ffmpeg.version ? `- Version: ${diagnostics.ffmpeg.version}` : ''}
${diagnostics.ffmpeg.errorMessage ? `- Error: ${diagnostics.ffmpeg.errorMessage}` : ''}

Network:
- CORS Configured: ${diagnostics.network.corsConfigured ? 'Yes' : 'No'}
- Base URL Configured: ${diagnostics.network.baseUrlConfigured ? 'Yes' : 'No'}
${diagnostics.network.issues.length > 0 ? `- Issues:\n  ${diagnostics.network.issues.join('\n  ')}` : ''}

Providers:
${diagnostics.providers.map((p) => `- ${p.name}: ${p.reachable ? 'Reachable' : 'Unreachable'}${p.errorMessage ? ` (${p.errorMessage})` : ''}`).join('\n')}
    `.trim();

    void navigator.clipboard.writeText(summary);

    loggingService.info('Diagnostics copied to clipboard', 'NetworkDiagnostics');
  }, [diagnostics]);

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Running diagnostics..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <MessageBar intent="error">
          <MessageBarBody>
            <Text weight="semibold">Diagnostics Failed</Text>
            <Text>{error}</Text>
            <Button
              appearance="primary"
              icon={<ArrowClockwise20Regular />}
              onClick={() => void runDiagnostics()}
            >
              Retry
            </Button>
          </MessageBarBody>
        </MessageBar>
      </div>
    );
  }

  if (!diagnostics) {
    return null;
  }

  const hasIssues =
    !diagnostics.backendReachable ||
    !diagnostics.configuration.isValid ||
    !diagnostics.ffmpeg.installed ||
    !diagnostics.ffmpeg.valid ||
    diagnostics.configuration.issues.length > 0 ||
    diagnostics.network.issues.length > 0 ||
    diagnostics.providers.some((p) => !p.reachable);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title3>Network & System Diagnostics</Title3>
          <Text size={200}>Correlation ID: {diagnostics.correlationId}</Text>
        </div>
        <div className={styles.actionButtons}>
          <Button appearance="subtle" icon={<Copy20Regular />} onClick={copyToClipboard}>
            Copy Summary
          </Button>
          <Button
            appearance="primary"
            icon={<ArrowClockwise20Regular />}
            onClick={() => void runDiagnostics()}
          >
            Refresh
          </Button>
        </div>
      </div>

      {hasIssues ? (
        <MessageBar intent="warning">
          <MessageBarBody>
            Issues detected. Review the details below for recommended actions.
          </MessageBarBody>
        </MessageBar>
      ) : (
        <MessageBar intent="success">
          <MessageBarBody>All systems operational.</MessageBarBody>
        </MessageBar>
      )}

      {/* Backend Status */}
      <Card className={styles.statusCard}>
        <Subtitle2>Backend Service</Subtitle2>
        <div className={styles.statusRow}>
          {diagnostics.backendReachable ? (
            <>
              <CheckmarkCircle20Filled className={styles.statusIcon} color="green" />
              <Text>Backend is reachable (Version: {diagnostics.backendVersion})</Text>
            </>
          ) : (
            <>
              <DismissCircle20Filled className={styles.statusIcon} color="red" />
              <Text>Backend is not reachable</Text>
            </>
          )}
        </div>
      </Card>

      {/* FFmpeg Status */}
      <Card className={styles.statusCard}>
        <Subtitle2>FFmpeg</Subtitle2>
        <div className={styles.statusRow}>
          {diagnostics.ffmpeg.installed && diagnostics.ffmpeg.valid ? (
            <>
              <CheckmarkCircle20Filled className={styles.statusIcon} color="green" />
              <Text>
                FFmpeg is installed and valid
                {diagnostics.ffmpeg.version ? ` (${diagnostics.ffmpeg.version})` : ''}
              </Text>
            </>
          ) : (
            <>
              <DismissCircle20Filled className={styles.statusIcon} color="red" />
              <Text>
                FFmpeg issue: {diagnostics.ffmpeg.errorMessage || 'Not installed or invalid'}
              </Text>
            </>
          )}
        </div>
      </Card>

      {/* Configuration Status */}
      <Card className={styles.statusCard}>
        <Subtitle2>Configuration</Subtitle2>
        <div className={styles.statusRow}>
          {diagnostics.configuration.isValid ? (
            <>
              <CheckmarkCircle20Filled className={styles.statusIcon} color="green" />
              <Text>Configuration is valid</Text>
            </>
          ) : (
            <>
              <Warning20Filled className={styles.statusIcon} color="orange" />
              <Text>Configuration has issues</Text>
            </>
          )}
        </div>
        {diagnostics.configuration.issues.length > 0 && (
          <ul className={styles.issuesList}>
            {diagnostics.configuration.issues.map((issue, index) => (
              <li key={index} className={styles.issueItem}>
                <Warning20Filled color="orange" />
                <Text>{issue}</Text>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {/* Network Configuration */}
      <Card className={styles.statusCard}>
        <Subtitle2>Network Configuration</Subtitle2>
        <div className={styles.statusRow}>
          {diagnostics.network.corsConfigured ? (
            <>
              <CheckmarkCircle20Filled className={styles.statusIcon} color="green" />
              <Text>CORS is configured</Text>
            </>
          ) : (
            <>
              <Warning20Filled className={styles.statusIcon} color="orange" />
              <Text>CORS may not be configured correctly</Text>
            </>
          )}
        </div>
        <div className={styles.statusRow}>
          {diagnostics.network.baseUrlConfigured ? (
            <>
              <CheckmarkCircle20Filled className={styles.statusIcon} color="green" />
              <Text>Base URL is configured</Text>
            </>
          ) : (
            <>
              <Warning20Filled className={styles.statusIcon} color="orange" />
              <Text>Base URL may not be configured</Text>
            </>
          )}
        </div>
        {diagnostics.network.issues.length > 0 && (
          <ul className={styles.issuesList}>
            {diagnostics.network.issues.map((issue, index) => (
              <li key={index} className={styles.issueItem}>
                <Warning20Filled color="orange" />
                <Text>{issue}</Text>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {/* Provider Connectivity */}
      <Card className={styles.statusCard}>
        <Subtitle2>Provider Connectivity</Subtitle2>
        <div className={styles.providerGrid}>
          {diagnostics.providers.map((provider) => (
            <Card key={provider.name} className={styles.providerCard}>
              <div className={styles.statusRow}>
                {provider.reachable ? (
                  <CheckmarkCircle20Filled color="green" />
                ) : (
                  <DismissCircle20Filled color="red" />
                )}
                <div>
                  <Text weight="semibold">{provider.name}</Text>
                  <Text size={200}>{provider.reachable ? 'Reachable' : 'Unreachable'}</Text>
                  {provider.errorMessage && (
                    <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                      {provider.errorMessage}
                    </Text>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      </Card>

      {/* Detailed Technical Information */}
      <Accordion collapsible>
        <AccordionItem value="technical">
          <AccordionHeader>Technical Details</AccordionHeader>
          <AccordionPanel>
            <pre
              style={{
                backgroundColor: tokens.colorNeutralBackground2,
                padding: tokens.spacingVerticalM,
                borderRadius: tokens.borderRadiusMedium,
                overflow: 'auto',
              }}
            >
              {JSON.stringify(diagnostics, null, 2)}
            </pre>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </div>
  );
};
