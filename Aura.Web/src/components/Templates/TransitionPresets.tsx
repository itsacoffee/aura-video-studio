/**
 * Component for displaying and applying transition presets
 */

import {
  Card,
  CardHeader,
  Text,
  Button,
  Spinner,
  makeStyles,
  tokens,
  Badge,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { getTransitionPresets } from '../../services/templatesService';
import { TransitionPreset } from '../../types/templates';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  card: {
    height: '140px',
    cursor: 'pointer',
    transition: 'transform 0.2s ease, box-shadow 0.2s ease',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  cardSelected: {
    border: `2px solid ${tokens.colorBrandBackground}`,
  },
  cardContent: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    height: '100%',
  },
  titleRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  metadata: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  applyButton: {
    marginTop: 'auto',
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
  errorContainer: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    color: tokens.colorPaletteRedForeground1,
  },
});

export interface TransitionPresetsProps {
  onApplyPreset?: (preset: TransitionPreset) => void;
}

export function TransitionPresets({ onApplyPreset }: TransitionPresetsProps) {
  const styles = useStyles();
  const [presets, setPresets] = useState<TransitionPreset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedPreset, setSelectedPreset] = useState<string | null>(null);

  useEffect(() => {
    loadPresets();
  }, []);

  const loadPresets = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getTransitionPresets();
      setPresets(data);
    } catch (err) {
      setError('Failed to load transition presets');
      console.error('Error loading transition presets:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleApplyPreset = (preset: TransitionPreset) => {
    setSelectedPreset(preset.id);
    onApplyPreset?.(preset);

    // Clear selection after a delay
    setTimeout(() => setSelectedPreset(null), 2000);
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading transition presets..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.errorContainer}>
        <Text>{error}</Text>
        <Button onClick={loadPresets}>Retry</Button>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <Text size={500} weight="semibold">
        Transition Presets
      </Text>
      <div className={styles.grid}>
        {presets.map((preset) => (
          <Card
            key={preset.id}
            className={`${styles.card} ${selectedPreset === preset.id ? styles.cardSelected : ''}`}
            onClick={() => handleApplyPreset(preset)}
          >
            <CardHeader
              header={
                <div className={styles.cardContent}>
                  <div className={styles.titleRow}>
                    <Text className={styles.title}>{preset.name}</Text>
                    {preset.direction && (
                      <Badge size="small" appearance="tint">
                        {preset.direction}
                      </Badge>
                    )}
                  </div>
                  <div className={styles.metadata}>
                    <Text>Type: {preset.type}</Text>
                    <Text>Duration: {preset.defaultDuration}s</Text>
                  </div>
                  <Button
                    appearance={selectedPreset === preset.id ? 'primary' : 'secondary'}
                    size="small"
                    className={styles.applyButton}
                    icon={selectedPreset === preset.id ? <CheckmarkCircle24Filled /> : undefined}
                  >
                    {selectedPreset === preset.id ? 'Applied!' : 'Apply'}
                  </Button>
                </div>
              }
            />
          </Card>
        ))}
      </div>
    </div>
  );
}
