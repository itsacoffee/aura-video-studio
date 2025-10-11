import { useState, useEffect } from 'react';
import {
  Card,
  CardHeader,
  CardPreview,
  Button,
  Text,
  Badge,
  Spinner,
  makeStyles,
  tokens,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Link,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Stop24Regular,
  ArrowDownload24Regular,
  Checkmark24Regular,
  Warning24Regular,
  Wrench24Regular,
  Delete24Regular,
  Folder24Regular,
  MoreHorizontal24Regular,
} from '@fluentui/react-icons';
import type { EngineManifestEntry, EngineStatus } from '../../types/engines';
import { useEnginesStore } from '../../state/engines';

const useStyles = makeStyles({
  card: {
    width: '100%',
    maxWidth: '800px',
    marginBottom: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  content: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  statusBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  metadata: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  messages: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalS,
  },
  message: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
});

interface EngineCardProps {
  engine: EngineManifestEntry;
}

export function EngineCard({ engine }: EngineCardProps) {
  const styles = useStyles();
  const [status, setStatus] = useState<EngineStatus | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  
  const {
    installEngine,
    verifyEngine,
    repairEngine,
    removeEngine,
    startEngine,
    stopEngine,
    fetchEngineStatus,
  } = useEnginesStore();

  useEffect(() => {
    loadStatus();
    const interval = setInterval(loadStatus, 5000); // Poll every 5 seconds
    return () => clearInterval(interval);
  }, [engine.id]);

  const loadStatus = async () => {
    try {
      await fetchEngineStatus(engine.id);
      const response = await fetch(`http://127.0.0.1:5005/api/engines/status?engineId=${engine.id}`);
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      }
    } catch (error) {
      console.error('Failed to load status:', error);
    }
  };

  const handleInstall = async () => {
    setIsProcessing(true);
    try {
      await installEngine(engine.id);
      await loadStatus();
    } catch (error) {
      console.error('Installation failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleStart = async () => {
    setIsProcessing(true);
    try {
      await startEngine(engine.id);
      await loadStatus();
    } catch (error) {
      console.error('Start failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleStop = async () => {
    setIsProcessing(true);
    try {
      await stopEngine(engine.id);
      await loadStatus();
    } catch (error) {
      console.error('Stop failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleVerify = async () => {
    setIsProcessing(true);
    try {
      const result = await verifyEngine(engine.id);
      alert(result.isValid ? 'Verification passed!' : `Verification failed: ${result.issues.join(', ')}`);
    } catch (error) {
      console.error('Verification failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRepair = async () => {
    if (!confirm(`Repair ${engine.name}? This will reinstall the engine.`)) {
      return;
    }
    setIsProcessing(true);
    try {
      await repairEngine(engine.id);
      await loadStatus();
    } catch (error) {
      console.error('Repair failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRemove = async () => {
    if (!confirm(`Remove ${engine.name}? This will delete all files.`)) {
      return;
    }
    setIsProcessing(true);
    try {
      await removeEngine(engine.id);
      await loadStatus();
    } catch (error) {
      console.error('Removal failed:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleOpenFolder = () => {
    if (engine.installPath) {
      // This would need to be implemented via an API endpoint that opens the folder
      alert(`Install path: ${engine.installPath}`);
    }
  };

  const getStatusBadge = () => {
    if (!status) {
      return <Badge appearance="outline">Unknown</Badge>;
    }

    switch (status.status) {
      case 'running':
        return (
          <Badge appearance="filled" color="success">
            Running {status.health === 'healthy' ? '✓' : '(unreachable)'}
          </Badge>
        );
      case 'installed':
        return <Badge appearance="filled" color="informative">Installed</Badge>;
      case 'not_installed':
        return <Badge appearance="outline">Not Installed</Badge>;
      default:
        return <Badge appearance="outline">Unknown</Badge>;
    }
  };

  const formatBytes = (bytes: number) => {
    const gb = bytes / (1024 ** 3);
    return gb >= 1 ? `${gb.toFixed(2)} GB` : `${(bytes / (1024 ** 2)).toFixed(2)} MB`;
  };

  const isInstalled = engine.isInstalled || status?.status !== 'not_installed';
  const isRunning = status?.isRunning || false;

  return (
    <Card className={styles.card}>
      <CardHeader
        header={
          <div className={styles.header}>
            <div className={styles.title}>
              <Text weight="semibold" size={500}>{engine.name}</Text>
              {getStatusBadge()}
            </div>
          </div>
        }
        description={
          <div>
            <Text className={styles.metadata}>
              Version: {engine.version} • Size: {formatBytes(engine.sizeBytes)}
              {engine.defaultPort && ` • Port: ${status?.port || engine.defaultPort}`}
              {engine.requiredVRAMGB && ` • Requires: ${engine.requiredVRAMGB}GB VRAM`}
            </Text>
            {engine.description && (
              <Text className={styles.metadata} block>{engine.description}</Text>
            )}
            {engine.gatingReason && (
              <Text className={styles.metadata} block style={{ color: tokens.colorPaletteYellowForeground1 }}>
                ⚠️ {engine.gatingReason}
                {engine.vramTooltip && ` • ${engine.vramTooltip}`}
              </Text>
            )}
          </div>
        }
      />
      <CardPreview className={styles.content}>
        <div className={styles.row}>
          <div className={styles.actions}>
            {!isInstalled && (
              <Button
                appearance="primary"
                icon={<ArrowDownload24Regular />}
                onClick={handleInstall}
                disabled={isProcessing || (engine.isGated && !engine.canInstall)}
                title={engine.gatingReason || undefined}
              >
                {isProcessing ? <Spinner size="tiny" /> : 'Install'}
              </Button>
            )}
            
            {isInstalled && !isRunning && (
              <Button
                appearance="primary"
                icon={<Play24Regular />}
                onClick={handleStart}
                disabled={isProcessing}
              >
                Start
              </Button>
            )}
            
            {isInstalled && isRunning && (
              <Button
                appearance="secondary"
                icon={<Stop24Regular />}
                onClick={handleStop}
                disabled={isProcessing}
              >
                Stop
              </Button>
            )}

            {isInstalled && (
              <Menu>
                <MenuTrigger disableButtonEnhancement>
                  <Button
                    appearance="subtle"
                    icon={<MoreHorizontal24Regular />}
                    disabled={isProcessing}
                  />
                </MenuTrigger>
                <MenuPopover>
                  <MenuList>
                    <MenuItem icon={<Checkmark24Regular />} onClick={handleVerify}>
                      Verify
                    </MenuItem>
                    <MenuItem icon={<Wrench24Regular />} onClick={handleRepair}>
                      Repair
                    </MenuItem>
                    <MenuItem icon={<Folder24Regular />} onClick={handleOpenFolder}>
                      Open Folder
                    </MenuItem>
                    <MenuItem icon={<Delete24Regular />} onClick={handleRemove}>
                      Remove
                    </MenuItem>
                  </MenuList>
                </MenuPopover>
              </Menu>
            )}
          </div>

          {engine.licenseUrl && (
            <Link href={engine.licenseUrl} target="_blank">
              License
            </Link>
          )}
        </div>

        {status?.messages && status.messages.length > 0 && (
          <div className={styles.messages}>
            {status.messages.map((msg, idx) => (
              <Text key={idx} className={styles.message}>
                <Warning24Regular /> {msg}
              </Text>
            ))}
          </div>
        )}

        {status?.processId && (
          <Text className={styles.metadata}>
            PID: {status.processId}
            {status.logsPath && ` • Logs: ${status.logsPath}`}
          </Text>
        )}
      </CardPreview>
    </Card>
  );
}
