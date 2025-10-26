/**
 * Component for displaying and applying effect presets
 */

import {
  Card,
  CardHeader,
  CardPreview,
  Text,
  Button,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { getEffectPresets } from '../../services/templatesService';
import { EffectPreset } from '../../types/templates';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  card: {
    height: '280px',
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
  preview: {
    height: '120px',
    backgroundColor: tokens.colorNeutralBackground3,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  cardContent: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  category: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  description: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
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

export interface EffectPresetsProps {
  onApplyPreset?: (preset: EffectPreset) => void;
}

export function EffectPresets({ onApplyPreset }: EffectPresetsProps) {
  const styles = useStyles();
  const [presets, setPresets] = useState<EffectPreset[]>([]);
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
      const data = await getEffectPresets();
      setPresets(data);
    } catch (err) {
      setError('Failed to load effect presets');
      console.error('Error loading effect presets:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleApplyPreset = (preset: EffectPreset) => {
    setSelectedPreset(preset.id);
    onApplyPreset?.(preset);

    // Clear selection after a delay
    setTimeout(() => setSelectedPreset(null), 2000);
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading effect presets..." />
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
        Effect Presets
      </Text>
      <div className={styles.grid}>
        {presets.map((preset) => (
          <Card
            key={preset.id}
            className={`${styles.card} ${selectedPreset === preset.id ? styles.cardSelected : ''}`}
            onClick={() => handleApplyPreset(preset)}
          >
            <CardPreview className={styles.preview}>
              {preset.previewImage ? (
                <img
                  src={preset.previewImage}
                  alt={preset.name}
                  className={styles.previewImage}
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              ) : (
                <Text>{preset.category}</Text>
              )}
            </CardPreview>
            <CardHeader
              header={
                <div className={styles.cardContent}>
                  <Text className={styles.title}>{preset.name}</Text>
                  <Text className={styles.category}>{preset.category}</Text>
                  <Text className={styles.description}>{preset.description}</Text>
                  <Button
                    appearance={selectedPreset === preset.id ? 'primary' : 'secondary'}
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
