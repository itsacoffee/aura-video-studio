import { Card, Button, Text, makeStyles, tokens, Spinner } from '@fluentui/react-components';
import { Folder24Regular, Globe24Regular, Copy24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';
import { useNotifications } from '../Notifications/Toasts';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  section: {
    padding: tokens.spacingVerticalL,
  },
  engineItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    '&:last-child': {
      borderBottom: 'none',
    },
  },
  engineInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  pathText: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    wordBreak: 'break-all',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
});

interface EngineInfo {
  id: string;
  name: string;
  installPath?: string;
  hasWebUI?: boolean;
  webUIUrl?: string;
}

export function FileLocationsSummary() {
  const styles = useStyles();
  const { showFailureToast } = useNotifications();
  const [engines, setEngines] = useState<EngineInfo[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadEngineInfo();
  }, []);

  const loadEngineInfo = async () => {
    setIsLoading(true);
    try {
      // Fetch engine instances to get install paths
      const response = await fetch(apiUrl('/api/engines/instances'));
      if (response.ok) {
        const data = await response.json();
        const engineInfo: EngineInfo[] = data.instances.map((instance: any) => ({
          id: instance.engineId,
          name: instance.engineName || instance.engineId,
          installPath: instance.installPath,
          hasWebUI: ['stable-diffusion-webui', 'comfyui', 'sd-webui'].includes(
            instance.engineId.toLowerCase()
          ),
          webUIUrl: instance.port ? `http://localhost:${instance.port}` : undefined,
        }));
        setEngines(engineInfo);
      }
    } catch (error) {
      console.error('Failed to load engine info:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenFolder = async (engineId: string) => {
    try {
      const response = await fetch(apiUrl('/api/engines/open-folder'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ engineId }),
      });

      if (!response.ok) {
        throw new Error('Failed to open folder');
      }
    } catch (error) {
      console.error('Failed to open folder:', error);
      showFailureToast({
        title: 'Cannot Open Folder',
        message: 'Failed to open folder. Please navigate manually to the path shown above.',
      });
    }
  };

  const handleOpenWebUI = async (engineId: string) => {
    try {
      const response = await fetch(apiUrl('/api/engines/open-webui'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ engineId }),
      });

      if (response.ok) {
        const data = await response.json();
        if (data.url) {
          window.open(data.url, '_blank');
        }
      }
    } catch (error) {
      console.error('Failed to open web UI:', error);
    }
  };

  const handleCopyPath = (path: string) => {
    navigator.clipboard.writeText(path);
  };

  if (isLoading) {
    return (
      <Card className={styles.section}>
        <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
          <Spinner size="small" />
          <Text>Loading installation paths...</Text>
        </div>
      </Card>
    );
  }

  if (engines.length === 0) {
    return (
      <Card className={styles.section}>
        <Text>No engines installed yet. You can install them from the Downloads page.</Text>
      </Card>
    );
  }

  return (
    <div className={styles.container}>
      <Card className={styles.section}>
        <Text weight="semibold" size={500} style={{ marginBottom: tokens.spacingVerticalM }}>
          📂 Where are my files?
        </Text>
        <Text style={{ marginBottom: tokens.spacingVerticalL }}>
          Here&apos;s where Aura stores your installed engines and tools. You can open these folders
          to add models, configure settings, or access generated files.
        </Text>

        {engines.map((engine) => (
          <div key={engine.id} className={styles.engineItem}>
            <div className={styles.engineInfo}>
              <Text weight="semibold">{engine.name}</Text>
              {engine.installPath ? (
                <>
                  <Text className={styles.pathText}>📁 {engine.installPath}</Text>
                  <div className={styles.actions}>
                    <Button
                      size="small"
                      appearance="secondary"
                      icon={<Folder24Regular />}
                      onClick={() => handleOpenFolder(engine.id)}
                    >
                      Open Folder
                    </Button>
                    {engine.hasWebUI && engine.webUIUrl && (
                      <Button
                        size="small"
                        appearance="secondary"
                        icon={<Globe24Regular />}
                        onClick={() => handleOpenWebUI(engine.id)}
                      >
                        Open Web UI
                      </Button>
                    )}
                    <Button
                      size="small"
                      appearance="subtle"
                      icon={<Copy24Regular />}
                      onClick={() => handleCopyPath(engine.installPath!)}
                    >
                      Copy Path
                    </Button>
                  </div>
                </>
              ) : (
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  Not installed or path not available
                </Text>
              )}
            </div>
          </div>
        ))}
      </Card>

      <Card style={{ padding: tokens.spacingVerticalM }}>
        <Text size={200}>
          💡 <strong>Tip:</strong> You can manage engines, add models, and configure settings from
          the <strong>Downloads</strong> page. To add your own models to Stable Diffusion, place
          them in the <code>models/Stable-diffusion</code> folder inside the SD installation
          directory.
        </Text>
      </Card>
    </div>
  );
}
