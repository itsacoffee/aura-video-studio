import { Card, Text, Badge, Button, makeStyles, tokens } from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';

/**
 * Result of checking a path for existence
 */
interface PathCheckResult {
  path: string;
  exists: boolean;
}

interface DiagnosticsData {
  serverPath: string | null;
  serverExists: boolean;
  openCutDirExists: boolean;
  portInUse: boolean;
  port: number;
  processRunning: boolean;
  isStarting: boolean;
  isPackaged: boolean;
  resourcesPath: string | null;
  checkedPaths: PathCheckResult[];
  enabled: boolean;
  startAttempts: number;
  maxStartAttempts: number;
  error?: string;
}

interface OpenCutDiagnosticsProps {
  serverUrl: string;
  isElectron: boolean;
  onClose: () => void;
}

const useStyles = makeStyles({
  card: {
    padding: tokens.spacingHorizontalXL,
    width: '100%',
    maxWidth: '600px',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    marginTop: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  row: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  label: {
    fontWeight: tokens.fontWeightSemibold,
    minWidth: '140px',
  },
  pathList: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingHorizontalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalS,
  },
  pathItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalXS,
  },
  footer: {
    marginTop: tokens.spacingVerticalL,
    display: 'flex',
    justifyContent: 'flex-end',
  },
  loading: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
  },
});

/**
 * Type definition for OpenCut API with diagnostics support
 */
interface OpenCutApiWithDiagnostics {
  getDiagnostics?: () => Promise<DiagnosticsData>;
}

/**
 * Get the OpenCut API from either window.aura.opencut or window.electron.opencut
 * Handles SSR and cases where the expected properties may be undefined
 */
function getOpenCutApi(): OpenCutApiWithDiagnostics | null {
  // SSR check
  if (typeof window === 'undefined') return null;

  // Try to get from window.aura.opencut first, then window.electron.opencut
  const auraOpencut = (window as { aura?: { opencut?: OpenCutApiWithDiagnostics } })?.aura?.opencut;
  if (auraOpencut) return auraOpencut;

  const electronOpencut = (window as { electron?: { opencut?: OpenCutApiWithDiagnostics } })
    ?.electron?.opencut;
  if (electronOpencut) return electronOpencut;

  return null;
}

export function OpenCutDiagnostics({ serverUrl, isElectron, onClose }: OpenCutDiagnosticsProps) {
  const styles = useStyles();
  const [diagnostics, setDiagnostics] = useState<DiagnosticsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function runDiagnostics() {
      setLoading(true);
      setError(null);

      const opencutApi = getOpenCutApi();
      if (isElectron && opencutApi?.getDiagnostics) {
        try {
          const result = await opencutApi.getDiagnostics();
          setDiagnostics(result);
        } catch (err: unknown) {
          const errorObj = err instanceof Error ? err : new Error(String(err));
          setError(errorObj.message);
        }
      } else {
        setError('Diagnostics are only available in the desktop app.');
      }

      setLoading(false);
    }
    runDiagnostics();
  }, [isElectron]);

  if (loading) {
    return (
      <Card className={styles.card}>
        <div className={styles.loading}>
          <Text>Loading diagnostics...</Text>
        </div>
      </Card>
    );
  }

  return (
    <Card className={styles.card}>
      <div className={styles.header}>
        <Text weight="semibold" size={500}>
          OpenCut Diagnostics
        </Text>
        <Button
          appearance="subtle"
          icon={<Dismiss24Regular />}
          onClick={onClose}
          aria-label="Close diagnostics"
        />
      </div>

      <div className={styles.section}>
        <div className={styles.row}>
          <Text className={styles.label}>Server URL:</Text>
          <Text>{serverUrl}</Text>
        </div>
      </div>

      <div className={styles.section}>
        <div className={styles.row}>
          <Text className={styles.label}>Environment:</Text>
          <Badge color={isElectron ? 'success' : 'warning'}>
            {isElectron ? 'Electron (Desktop)' : 'Web Browser'}
          </Badge>
        </div>
      </div>

      {error && (
        <div className={styles.section}>
          <div className={styles.row}>
            <Text className={styles.label}>Error:</Text>
            <Text color="error">{error}</Text>
          </div>
        </div>
      )}

      {diagnostics && (
        <>
          <div className={styles.section}>
            <div className={styles.row}>
              <Text className={styles.label}>Server Path:</Text>
              <Text style={{ fontFamily: 'monospace', fontSize: tokens.fontSizeBase200 }}>
                {diagnostics.serverPath || 'Not found'}
              </Text>
            </div>
            <div className={styles.row}>
              <Text className={styles.label}>Server File:</Text>
              <Badge color={diagnostics.serverExists ? 'success' : 'danger'}>
                {diagnostics.serverExists ? 'Exists' : 'Missing'}
              </Badge>
            </div>
            <div className={styles.row}>
              <Text className={styles.label}>OpenCut Directory:</Text>
              <Badge color={diagnostics.openCutDirExists ? 'success' : 'danger'}>
                {diagnostics.openCutDirExists ? 'Exists' : 'Missing'}
              </Badge>
            </div>
          </div>

          <div className={styles.section}>
            <div className={styles.row}>
              <Text className={styles.label}>Port {diagnostics.port}:</Text>
              <Badge color={diagnostics.portInUse ? 'warning' : 'success'}>
                {diagnostics.portInUse ? 'In Use' : 'Available'}
              </Badge>
            </div>
            <div className={styles.row}>
              <Text className={styles.label}>Process:</Text>
              <Badge color={diagnostics.processRunning ? 'success' : 'informative'}>
                {diagnostics.processRunning ? 'Running' : 'Not Running'}
              </Badge>
            </div>
            <div className={styles.row}>
              <Text className={styles.label}>Starting:</Text>
              <Badge color={diagnostics.isStarting ? 'warning' : 'informative'}>
                {diagnostics.isStarting ? 'Yes' : 'No'}
              </Badge>
            </div>
          </div>

          <div className={styles.section}>
            <div className={styles.row}>
              <Text className={styles.label}>Packaged Mode:</Text>
              <Badge color={diagnostics.isPackaged ? 'success' : 'informative'}>
                {diagnostics.isPackaged ? 'Yes' : 'No (Dev)'}
              </Badge>
            </div>
            <div className={styles.row}>
              <Text className={styles.label}>Start Attempts:</Text>
              <Text>
                {diagnostics.startAttempts} / {diagnostics.maxStartAttempts}
              </Text>
            </div>
          </div>

          {diagnostics.checkedPaths && diagnostics.checkedPaths.length > 0 && (
            <div className={styles.section}>
              <Text className={styles.label}>Checked Paths:</Text>
              <div className={styles.pathList}>
                {diagnostics.checkedPaths.map((item, index) => (
                  <div key={index} className={styles.pathItem}>
                    <Badge
                      size="small"
                      color={item.exists ? 'success' : 'danger'}
                      style={{ minWidth: '50px' }}
                    >
                      {item.exists ? '✓' : '✗'}
                    </Badge>
                    <Text style={{ wordBreak: 'break-all' }}>{item.path}</Text>
                  </div>
                ))}
              </div>
            </div>
          )}

          {diagnostics.resourcesPath && (
            <div className={styles.section}>
              <div className={styles.row}>
                <Text className={styles.label}>Resources Path:</Text>
              </div>
              <div className={styles.pathList}>
                <Text>{diagnostics.resourcesPath}</Text>
              </div>
            </div>
          )}
        </>
      )}

      <div className={styles.footer}>
        <Button onClick={onClose}>Close</Button>
      </div>
    </Card>
  );
}
