import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import {
  ArrowSync24Regular,
  CheckmarkCircle24Filled,
  ErrorCircle24Filled,
  Warning24Filled,
  ArrowDownload24Regular,
  Link24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';

interface DependencyReport {
  id: string;
  displayName: string;
  status: 'Installed' | 'Missing' | 'PartiallyInstalled';
  path?: string;
  validationOutput?: string;
  provenance?: string;
  errorMessage?: string;
}

interface RescanReport {
  success: boolean;
  scanTime: string;
  dependencies: DependencyReport[];
}

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingVerticalXL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  statusCell: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  pathText: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  lastScanText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
});

export function RescanPanel() {
  const styles = useStyles();
  const [isScanning, setIsScanning] = useState(false);
  const [report, setReport] = useState<RescanReport | null>(null);
  const [lastScanTime, setLastScanTime] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Run rescan automatically on component mount
  useEffect(() => {
    handleRescanAll();
  }, []);

  const handleRescanAll = async () => {
    setIsScanning(true);
    setError(null);

    try {
      const response = await fetch(apiUrl('/api/dependencies/rescan'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      if (response.ok) {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const data = await response.json();
          setReport(data);
          setLastScanTime(data.scanTime);
        } else {
          const text = await response.text();
          setError(
            'Server returned invalid response (expected JSON, got HTML or other format). Please check if the API server is running correctly.'
          );
          console.error('Invalid response format:', text.substring(0, 200));
        }
      } else {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          const errorData = await response.json();
          setError(errorData.error || 'Failed to rescan dependencies');
        } else {
          setError(
            `Server returned HTTP ${response.status} error. The API endpoint may not be available.`
          );
        }
      }
    } catch (err) {
      console.error('Rescan failed:', err);
      if (err instanceof Error && err.message.includes('JSON')) {
        setError(
          'Unable to parse server response. The server may have returned HTML instead of JSON. Please check the API configuration.'
        );
      } else {
        setError(err instanceof Error ? err.message : 'Failed to rescan dependencies');
      }
    } finally {
      setIsScanning(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Installed':
        return (
          <Badge appearance="filled" color="success" icon={<CheckmarkCircle24Filled />}>
            Installed
          </Badge>
        );
      case 'Missing':
        return (
          <Badge appearance="filled" color="danger" icon={<ErrorCircle24Filled />}>
            Missing
          </Badge>
        );
      case 'PartiallyInstalled':
        return (
          <Badge appearance="filled" color="warning" icon={<Warning24Filled />}>
            Partially Installed
          </Badge>
        );
      default:
        return <Badge>{status}</Badge>;
    }
  };

  const getActionButtons = (dep: DependencyReport) => {
    if (dep.status === 'Installed') {
      return null; // No action needed
    }

    const handleInstall = () => {
      // Navigate to the main downloads page where installation is handled
      window.location.href = '/downloads';
    };

    const handleAttach = () => {
      // Navigate to the main downloads page for attaching existing installations
      window.location.href = '/downloads';
    };

    return (
      <div className={styles.actions}>
        {dep.status === 'Missing' && (
          <>
            <Button
              appearance="primary"
              icon={<ArrowDownload24Regular />}
              size="small"
              onClick={handleInstall}
              title="Install this dependency automatically"
            >
              Install
            </Button>
            <Button
              appearance="secondary"
              icon={<Link24Regular />}
              size="small"
              onClick={handleAttach}
              title="Attach an existing installation"
            >
              Attach
            </Button>
          </>
        )}
        {dep.status === 'PartiallyInstalled' && (
          <Button
            appearance="primary"
            size="small"
            onClick={handleInstall}
            title="Repair this dependency"
          >
            Repair
          </Button>
        )}
      </div>
    );
  };

  const formatDateTime = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  };

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <div>
          <Title2>Dependency Scanner</Title2>
          <Text className={styles.lastScanText}>
            {lastScanTime ? `Last scan: ${formatDateTime(lastScanTime)}` : 'No scan performed yet'}
          </Text>
        </div>
        <Button
          appearance="primary"
          icon={isScanning ? <Spinner size="tiny" /> : <ArrowSync24Regular />}
          onClick={handleRescanAll}
          disabled={isScanning}
        >
          {isScanning ? 'Scanning...' : 'Rescan All Dependencies'}
        </Button>
      </div>

      {error && (
        <Text
          style={{
            color: tokens.colorPaletteRedForeground1,
            marginBottom: tokens.spacingVerticalM,
          }}
        >
          Error: {error}
        </Text>
      )}

      {report && (
        <>
          <Text style={{ marginBottom: tokens.spacingVerticalM }}>
            Found {report.dependencies.filter((d) => d.status === 'Installed').length} of{' '}
            {report.dependencies.length} dependencies installed
          </Text>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHeaderCell>Dependency</TableHeaderCell>
                <TableHeaderCell>Status</TableHeaderCell>
                <TableHeaderCell>Path / Details</TableHeaderCell>
                <TableHeaderCell>Actions</TableHeaderCell>
              </TableRow>
            </TableHeader>
            <TableBody>
              {report.dependencies.map((dep) => (
                <TableRow key={dep.id}>
                  <TableCell>
                    <div>
                      <Text weight="semibold">{dep.displayName}</Text>
                      {dep.provenance && (
                        <Text className={styles.lastScanText} block>
                          ({dep.provenance})
                        </Text>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className={styles.statusCell}>{getStatusBadge(dep.status)}</div>
                  </TableCell>
                  <TableCell>
                    {dep.path && (
                      <Text className={styles.pathText} block>
                        {dep.path}
                      </Text>
                    )}
                    {dep.validationOutput && (
                      <Text className={styles.lastScanText} block>
                        {dep.validationOutput}
                      </Text>
                    )}
                    {dep.errorMessage && (
                      <Text style={{ color: tokens.colorPaletteRedForeground1 }} block>
                        {dep.errorMessage}
                      </Text>
                    )}
                  </TableCell>
                  <TableCell>{getActionButtons(dep)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </>
      )}

      {!report && !isScanning && (
        <Text>
          Click &quot;Rescan All Dependencies&quot; to check the status of all dependencies.
        </Text>
      )}
    </Card>
  );
}
