/**
 * Diagnostics Panel Component
 * Displays system diagnostics checks and allows fixing issues
 */

import {
  Card,
  CardHeader,
  CardPreview,
  Button,
  Spinner,
  Text,
  Badge,
  Title3,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  ErrorCircle24Regular,
  Warning24Regular,
  ArrowClockwise24Regular,
  Wrench24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import './DiagnosticsPanel.css';

interface DiagnosticCheck {
  name: string;
  status: 'ok' | 'warning' | 'error' | 'checking';
  message: string;
  canFix: boolean;
  fixAction?: string;
  path?: string;
  url?: string;
}

interface DiagnosticsResults {
  ffmpeg: DiagnosticCheck;
  api: DiagnosticCheck;
  providers: DiagnosticCheck;
  diskSpace: DiagnosticCheck;
  config: DiagnosticCheck;
}

export const DiagnosticsPanel: FC = () => {
  const [results, setResults] = useState<DiagnosticsResults | null>(null);
  const [loading, setLoading] = useState(false);
  const [fixingCheck, setFixingCheck] = useState<string | null>(null);

  const runDiagnostics = async () => {
    if (!window.electron) {
      return;
    }

    setLoading(true);
    try {
      const diagnostics = (await window.electron.invoke('diagnostics:runAll')) as {
        success: boolean;
        results: DiagnosticsResults;
      };
      if (diagnostics.success) {
        setResults(diagnostics.results);
      }
    } catch (error: unknown) {
      console.error('Failed to run diagnostics:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    runDiagnostics();
  }, []);

  const handleFix = async (checkName: string) => {
    if (!window.electron) {
      return;
    }

    setFixingCheck(checkName);
    try {
      const fixMethod = `diagnostics:fix${checkName.charAt(0).toUpperCase() + checkName.slice(1)}`;
      const result = (await window.electron.invoke(fixMethod)) as {
        success: boolean;
        message: string;
        requiresRestart?: boolean;
      };

      if (result.success) {
        if (result.requiresRestart) {
          alert(`${result.message}\n\nPlease restart the application.`);
        } else {
          alert(result.message);
          await runDiagnostics();
        }
      } else {
        alert(`Fix failed: ${result.message}`);
      }
    } catch (error: unknown) {
      console.error(`Failed to fix ${checkName}:`, error);
      alert(
        `Failed to fix ${checkName}: ${error instanceof Error ? error.message : String(error)}`
      );
    } finally {
      setFixingCheck(null);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'ok':
        return <CheckmarkCircle24Regular className="status-icon-ok" />;
      case 'warning':
        return <Warning24Regular className="status-icon-warning" />;
      case 'error':
        return <ErrorCircle24Regular className="status-icon-error" />;
      case 'checking':
        return <Spinner size="tiny" />;
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'ok':
        return (
          <Badge appearance="tint" color="success">
            OK
          </Badge>
        );
      case 'warning':
        return (
          <Badge appearance="tint" color="warning">
            Warning
          </Badge>
        );
      case 'error':
        return (
          <Badge appearance="tint" color="danger">
            Error
          </Badge>
        );
      case 'checking':
        return (
          <Badge appearance="tint" color="informative">
            Checking
          </Badge>
        );
      default:
        return null;
    }
  };

  const getCheckTitle = (checkName: string) => {
    const titles: Record<string, string> = {
      ffmpeg: 'FFmpeg Binary',
      api: 'API Endpoint',
      providers: 'Provider Configuration',
      diskSpace: 'Disk Space',
      config: 'Configuration File',
    };
    return titles[checkName] || checkName;
  };

  if (loading && !results) {
    return (
      <div className="diagnostics-panel">
        <div className="diagnostics-header">
          <Title3>System Diagnostics</Title3>
        </div>
        <div className="diagnostics-loading">
          <Spinner size="large" label="Running diagnostics..." />
        </div>
      </div>
    );
  }

  return (
    <div className="diagnostics-panel">
      <div className="diagnostics-header">
        <Title3>System Diagnostics</Title3>
        <Button
          appearance="secondary"
          icon={<ArrowClockwise24Regular />}
          onClick={runDiagnostics}
          disabled={loading}
        >
          Refresh
        </Button>
      </div>

      {results && (
        <div className="diagnostics-grid">
          {Object.entries(results).map(([checkName, check]) => (
            <Card key={checkName} className="diagnostic-check-card">
              <CardHeader
                image={getStatusIcon(check.status)}
                header={
                  <div className="check-header">
                    <Text weight="semibold">{getCheckTitle(checkName)}</Text>
                    {getStatusBadge(check.status)}
                  </div>
                }
              />
              <CardPreview className="check-details">
                <Text>{check.message}</Text>
                {check.path && (
                  <Text size={200} className="check-path">
                    Path: {check.path}
                  </Text>
                )}
                {check.url && (
                  <Text size={200} className="check-url">
                    URL: {check.url}
                  </Text>
                )}
                {check.fixAction && (
                  <Text size={200} className="check-fix-action">
                    Fix: {check.fixAction}
                  </Text>
                )}
              </CardPreview>
              {check.canFix && (
                <div className="check-actions">
                  <Button
                    appearance="primary"
                    icon={<Wrench24Regular />}
                    onClick={() => handleFix(checkName)}
                    disabled={fixingCheck === checkName}
                  >
                    {fixingCheck === checkName ? 'Fixing...' : 'Fix'}
                  </Button>
                </div>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

export default DiagnosticsPanel;
