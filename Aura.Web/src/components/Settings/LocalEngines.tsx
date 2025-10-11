import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Input,
  Switch,
  Card,
  Field,
  Spinner,
  Badge,
  Tooltip,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
} from '@fluentui/react-components';
import {
  Play20Regular,
  Stop20Regular,
  ArrowSync20Regular,
  Folder20Regular,
  Info20Regular,
  CheckmarkCircle20Filled,
  Warning20Filled,
  ErrorCircle20Filled,
  DocumentText20Regular,
  Warning20Regular,
} from '@fluentui/react-icons';
import { useEnginesStore } from '../../state/engines';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  engineCard: {
    padding: tokens.spacingVerticalL,
  },
  engineHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  engineInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  engineActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  engineForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  statusBadge: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  helpText: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  logViewer: {
    fontFamily: 'monospace',
    fontSize: '12px',
    backgroundColor: tokens.colorNeutralBackground1,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    maxHeight: '400px',
    overflowY: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-all',
  },
  diagnosticsCard: {
    padding: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
});

interface LocalEngineConfig {
  id: string;
  name: string;
  description: string;
  defaultPort?: number;
  requiresNvidia?: boolean;
  minVRAM?: number;
}

const LOCAL_ENGINES: LocalEngineConfig[] = [
  {
    id: 'stable-diffusion',
    name: 'Stable Diffusion WebUI',
    description: 'Local image generation using Stable Diffusion. Managed mode with automatic setup.',
    defaultPort: 7860,
    requiresNvidia: true,
    minVRAM: 6,
  },
  {
    id: 'comfyui',
    name: 'ComfyUI',
    description: 'Advanced node-based interface for Stable Diffusion workflows.',
    defaultPort: 8188,
    requiresNvidia: true,
    minVRAM: 8,
  },
  {
    id: 'piper',
    name: 'Piper TTS',
    description: 'Fast, lightweight local text-to-speech engine. Works offline.',
    requiresNvidia: false,
  },
  {
    id: 'mimic3',
    name: 'Mimic3 TTS',
    description: 'Neural text-to-speech engine with high-quality voices. Works offline.',
    defaultPort: 59125,
    requiresNvidia: false,
  },
];

export function LocalEngines() {
  const styles = useStyles();
  const {
    engines,
    engineStatuses,
    fetchEngines,
    fetchEngineStatus,
    startEngine,
    stopEngine,
    isLoading,
  } = useEnginesStore();

  const [engineConfigs, setEngineConfigs] = useState<Record<string, { port?: number; autoStart: boolean }>>({});
  const [hasChanges, setHasChanges] = useState(false);
  const [diagnostics, setDiagnostics] = useState<any>(null);
  const [logs, setLogs] = useState<string>('');
  const [selectedEngineForLogs, setSelectedEngineForLogs] = useState<string | null>(null);
  const [showDiagnostics, setShowDiagnostics] = useState(false);
  const [showLogs, setShowLogs] = useState(false);

  useEffect(() => {
    fetchEngines();
    // Fetch status for all local engines
    LOCAL_ENGINES.forEach(engine => {
      fetchEngineStatus(engine.id);
    });
  }, [fetchEngines, fetchEngineStatus]);

  const updateEngineConfig = (engineId: string, key: 'port' | 'autoStart', value: number | boolean) => {
    setEngineConfigs(prev => ({
      ...prev,
      [engineId]: {
        ...prev[engineId],
        [key]: value,
      },
    }));
    setHasChanges(true);
  };

  const saveEnginePreferences = async () => {
    try {
      const response = await fetch('http://127.0.0.1:5005/api/engines/preferences', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(engineConfigs),
      });

      if (response.ok) {
        alert('Engine preferences saved successfully');
        setHasChanges(false);
      } else {
        alert('Failed to save preferences');
      }
    } catch (error) {
      console.error('Failed to save preferences:', error);
      alert('Failed to save preferences');
    }
  };

  const handleStartEngine = async (engineId: string) => {
    try {
      const config = engineConfigs[engineId];
      await startEngine(engineId, config?.port);
      await fetchEngineStatus(engineId);
    } catch (error) {
      console.error(`Failed to start ${engineId}:`, error);
      alert(`Failed to start engine. Check console for details.`);
    }
  };

  const handleStopEngine = async (engineId: string) => {
    try {
      await stopEngine(engineId);
      await fetchEngineStatus(engineId);
    } catch (error) {
      console.error(`Failed to stop ${engineId}:`, error);
    }
  };

  const handleValidateEngine = async (engineId: string) => {
    try {
      await fetchEngineStatus(engineId);
      alert('Engine status refreshed');
    } catch (error) {
      console.error(`Failed to validate ${engineId}:`, error);
    }
  };

  const handleOpenFolder = (engineId: string) => {
    const status = engineStatuses.get(engineId);
    if (status?.installedVersion) {
      // In a real application, this would open the folder in file explorer
      // For web app, we can just show the path
      alert(`Engine is installed. See Downloads page for management.`);
    } else {
      alert('Engine is not installed yet. Install from the Downloads page.');
    }
  };

  const handleRunDiagnostics = async () => {
    try {
      const response = await fetch('http://127.0.0.1:5005/api/engines/diagnostics');
      if (response.ok) {
        const data = await response.json();
        setDiagnostics(data);
        setShowDiagnostics(true);
      } else {
        alert('Failed to fetch diagnostics');
      }
    } catch (error) {
      console.error('Failed to fetch diagnostics:', error);
      alert('Failed to fetch diagnostics');
    }
  };

  const handleViewLogs = async (engineId: string) => {
    try {
      const response = await fetch(`http://127.0.0.1:5005/api/engines/logs?engineId=${engineId}`);
      if (response.ok) {
        const data = await response.json();
        setLogs(data.logs || 'No logs available');
        setSelectedEngineForLogs(engineId);
        setShowLogs(true);
      } else {
        alert('Failed to fetch logs');
      }
    } catch (error) {
      console.error('Failed to fetch logs:', error);
      alert('Failed to fetch logs');
    }
  };

  const renderEngineStatus = (engineId: string) => {
    const status = engineStatuses.get(engineId);

    if (!status) {
      return (
        <div className={styles.statusBadge}>
          <Spinner size="tiny" />
          <Text size={200}>Checking...</Text>
        </div>
      );
    }

    if (!status.isInstalled) {
      return (
        <div className={styles.statusBadge}>
          <Warning20Filled color={tokens.colorPaletteYellowForeground1} />
          <Badge appearance="outline" color="warning">Not Installed</Badge>
        </div>
      );
    }

    if (status.isRunning) {
      if (status.isHealthy) {
        return (
          <div className={styles.statusBadge}>
            <CheckmarkCircle20Filled color={tokens.colorPaletteGreenForeground1} />
            <Badge appearance="filled" color="success">Running (Healthy)</Badge>
          </div>
        );
      } else {
        return (
          <div className={styles.statusBadge}>
            <Warning20Filled color={tokens.colorPaletteYellowForeground1} />
            <Badge appearance="outline" color="warning">Running (Unreachable)</Badge>
          </div>
        );
      }
    }

    return (
      <div className={styles.statusBadge}>
        <Badge appearance="tint" color="informative">Installed</Badge>
        <Text size={200}>Ready to start</Text>
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: tokens.spacingVerticalM }}>
        <Title2>Local Engines</Title2>
        <Button 
          appearance="primary" 
          icon={<Warning20Regular />}
          onClick={handleRunDiagnostics}
        >
          Run Diagnostics
        </Button>
      </div>

      <Card className={styles.infoCard}>
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: tokens.spacingHorizontalS }}>
          <Info20Regular color={tokens.colorBrandForeground1} />
          <div>
            <Text weight="semibold">Local Engines Configuration</Text>
            <Text size={200} className={styles.helpText}>
              Configure local AI engines for offline use. Install engines from the Downloads page, 
              then configure their settings here. Engines can be started/stopped manually or set to auto-start.
            </Text>
          </div>
        </div>
      </Card>

      {LOCAL_ENGINES.map((engineConfig) => {
        const status = engineStatuses.get(engineConfig.id);
        const config = engineConfigs[engineConfig.id] || {};

        return (
          <Card key={engineConfig.id} className={styles.engineCard}>
            <div className={styles.engineHeader}>
              <div className={styles.engineInfo}>
                <div>
                  <Title3>{engineConfig.name}</Title3>
                  <Text size={200} className={styles.helpText}>
                    {engineConfig.description}
                  </Text>
                  {engineConfig.requiresNvidia && (
                    <Text size={200} className={styles.helpText} style={{ color: tokens.colorPaletteYellowForeground1 }}>
                      ⚠️ Requires NVIDIA GPU with {engineConfig.minVRAM}GB+ VRAM
                    </Text>
                  )}
                </div>
              </div>
              {renderEngineStatus(engineConfig.id)}
            </div>

            <div className={styles.engineForm}>
              {engineConfig.defaultPort && (
                <Field
                  label="Port"
                  hint={`Default: ${engineConfig.defaultPort}`}
                >
                  <Input
                    type="number"
                    value={config.port?.toString() || engineConfig.defaultPort.toString()}
                    onChange={(e) => updateEngineConfig(engineConfig.id, 'port', parseInt(e.target.value) || engineConfig.defaultPort)}
                    disabled={status?.isRunning}
                  />
                </Field>
              )}

              <Field label="Auto-start on app launch">
                <Switch
                  checked={config.autoStart || false}
                  onChange={(_, data) => updateEngineConfig(engineConfig.id, 'autoStart', data.checked)}
                />
              </Field>

              <div className={styles.engineActions}>
                {status?.isInstalled && (
                  <>
                    {status.isRunning ? (
                      <Button
                        appearance="secondary"
                        icon={<Stop20Regular />}
                        onClick={() => handleStopEngine(engineConfig.id)}
                        disabled={isLoading}
                      >
                        Stop
                      </Button>
                    ) : (
                      <Button
                        appearance="primary"
                        icon={<Play20Regular />}
                        onClick={() => handleStartEngine(engineConfig.id)}
                        disabled={isLoading}
                      >
                        Start
                      </Button>
                    )}
                  </>
                )}
                <Button
                  appearance="subtle"
                  icon={<ArrowSync20Regular />}
                  onClick={() => handleValidateEngine(engineConfig.id)}
                  disabled={isLoading}
                >
                  Validate
                </Button>
                {status?.isRunning && (
                  <Button
                    appearance="subtle"
                    icon={<DocumentText20Regular />}
                    onClick={() => handleViewLogs(engineConfig.id)}
                  >
                    View Logs
                  </Button>
                )}
                <Button
                  appearance="subtle"
                  icon={<Folder20Regular />}
                  onClick={() => handleOpenFolder(engineConfig.id)}
                >
                  Open Folder
                </Button>
              </div>
            </div>
          </Card>
        );
      })}

      {hasChanges && (
        <Card style={{ padding: tokens.spacingVerticalM, backgroundColor: tokens.colorPaletteYellowBackground1 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Text weight="semibold">⚠️ You have unsaved changes</Text>
            <Button appearance="primary" onClick={saveEnginePreferences}>
              Save Preferences
            </Button>
          </div>
        </Card>
      )}

      {/* Diagnostics Dialog */}
      <Dialog open={showDiagnostics} onOpenChange={(_, data) => setShowDiagnostics(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>System Diagnostics</DialogTitle>
            <DialogContent>
              {diagnostics && (
                <div>
                  <Text>Generated: {new Date(diagnostics.generatedAt).toLocaleString()}</Text>
                  <br />
                  <Text>Total Engines: {diagnostics.totalEngines}</Text>
                  <br />
                  <Text>Running: {diagnostics.runningEngines}</Text>
                  <br />
                  <Text>Healthy: {diagnostics.healthyEngines}</Text>
                  <br /><br />
                  <div className={styles.logViewer}>
                    {JSON.stringify(diagnostics.engines, null, 2)}
                  </div>
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowDiagnostics(false)}>Close</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Logs Dialog */}
      <Dialog open={showLogs} onOpenChange={(_, data) => setShowLogs(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Engine Logs - {selectedEngineForLogs}</DialogTitle>
            <DialogContent>
              <div className={styles.logViewer}>
                {logs}
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setShowLogs(false)}>Close</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
