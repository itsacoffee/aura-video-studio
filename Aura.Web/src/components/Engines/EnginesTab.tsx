import { useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Spinner,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
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
});

export function EnginesTab() {
  const styles = useStyles();
  const { engines, isLoading, error, fetchEngines } = useEnginesStore();

  useEffect(() => {
    fetchEngines();
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
