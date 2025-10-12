import { useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Spinner,
  MessageBar,
  MessageBarBody,
  Button,
  Card,
  Badge,
  Divider,
} from '@fluentui/react-components';
import { Folder24Regular, Globe24Regular } from '@fluentui/react-icons';
import { EngineCard } from './EngineCard';
import { useEnginesStore } from '../../state/engines';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  header: {
    marginBottom: tokens.spacingVerticalM,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
  enginesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
  instanceCard: {
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalS,
  },
  instanceRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXS,
  },
  instancePath: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    wordBreak: 'break-all',
  },
  instanceActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

export function EnginesTab() {
  const styles = useStyles();
  const { engines, instances, isLoading, error, fetchEngines, fetchInstances, openFolder, openWebUI } = useEnginesStore();

  useEffect(() => {
    fetchEngines();
    fetchInstances();
  }, []);

  if (isLoading && engines.length === 0) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading engines..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text size={600} weight="semibold">Local Engines</Text>
        <Text className={styles.subtitle}>
          Install and manage local AI engines for offline image generation and text-to-speech.
          All engines are installed to your local system without admin privileges.
        </Text>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {/* Attached Instances Section */}
      {instances.length > 0 && (
        <>
          <div className={styles.header}>
            <Text size={500} weight="semibold">Engine Instances</Text>
            <Text className={styles.subtitle}>
              Manage your engine installations (both app-managed and external)
            </Text>
          </div>
          
          <div className={styles.enginesList}>
            {instances.map((instance) => (
              <Card key={instance.id} className={styles.instanceCard}>
                <div className={styles.instanceRow}>
                  <div>
                    <Text weight="semibold">{instance.name}</Text>
                    <Badge appearance="filled" color={instance.mode === 'Managed' ? 'brand' : 'success'}>
                      {instance.mode}
                    </Badge>
                    <Badge appearance="outline" color={instance.isRunning ? 'success' : 'subtle'}>
                      {instance.status}
                    </Badge>
                  </div>
                  <div className={styles.instanceActions}>
                    <Button
                      appearance="subtle"
                      icon={<Folder24Regular />}
                      onClick={() => openFolder(instance.id)}
                    >
                      Open Folder
                    </Button>
                    {instance.port && (
                      <Button
                        appearance="subtle"
                        icon={<Globe24Regular />}
                        onClick={async () => {
                          const url = await openWebUI(instance.id);
                          window.open(url, '_blank');
                        }}
                      >
                        Open Web UI
                      </Button>
                    )}
                  </div>
                </div>
                <div>
                  <Text className={styles.instancePath}>
                    Path: {instance.installPath}
                  </Text>
                  {instance.port && (
                    <Text className={styles.instancePath}>
                      Port: {instance.port}
                    </Text>
                  )}
                  {instance.notes && (
                    <Text className={styles.instancePath}>
                      Notes: {instance.notes}
                    </Text>
                  )}
                </div>
              </Card>
            ))}
          </div>
          
          <Divider />
        </>
      )}

      {/* Available Engines Section */}
      <div className={styles.header}>
        <Text size={500} weight="semibold">Available Engines</Text>
        <Text className={styles.subtitle}>
          Install new engines or attach existing installations
        </Text>
      </div>

      {engines.length === 0 && !isLoading ? (
        <div className={styles.emptyState}>
          <Text>No engines available. Please check your connection and try again.</Text>
        </div>
      ) : (
        <div className={styles.enginesList}>
          {engines.map((engine) => (
            <EngineCard key={engine.id} engine={engine} />
          ))}
        </div>
      )}
    </div>
  );
}
