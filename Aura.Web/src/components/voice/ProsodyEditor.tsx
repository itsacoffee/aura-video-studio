import React, { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Slider,
  Button,
  Switch,
  Card,
} from '@fluentui/react-components';
import { ArrowResetRegular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    marginBottom: tokens.spacingVerticalS,
  },
  sliderGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  sliderRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sliderHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  sliderLabel: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
  },
  sliderValue: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
    minWidth: '80px',
    textAlign: 'right',
  },
  description: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  controls: {
    display: 'flex',
    justifyContent: 'flex-end',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  presetCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      transform: 'translateY(-2px)',
    },
  },
  presetsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
});

interface ProsodySettings {
  pitchShift: number;
  rateMultiplier: number;
  emphasisLevel: number;
  volumeAdjustment: number;
  pauseDurationMultiplier: number;
}

interface ProsodyEditorProps {
  prosody?: ProsodySettings;
  onChange: (prosody: ProsodySettings) => void;
}

const defaultProsody: ProsodySettings = {
  pitchShift: 0,
  rateMultiplier: 1.0,
  emphasisLevel: 0.5,
  volumeAdjustment: 0,
  pauseDurationMultiplier: 1.0,
};

const presets = [
  {
    name: 'Natural',
    description: 'Default natural speech',
    settings: defaultProsody,
  },
  {
    name: 'Energetic',
    description: 'Faster, higher pitch, more emphasis',
    settings: {
      pitchShift: 2,
      rateMultiplier: 1.15,
      emphasisLevel: 0.75,
      volumeAdjustment: 2,
      pauseDurationMultiplier: 0.8,
    },
  },
  {
    name: 'Calm',
    description: 'Slower, lower pitch, gentle',
    settings: {
      pitchShift: -1,
      rateMultiplier: 0.9,
      emphasisLevel: 0.3,
      volumeAdjustment: -1,
      pauseDurationMultiplier: 1.2,
    },
  },
  {
    name: 'Authoritative',
    description: 'Lower pitch, steady pace',
    settings: {
      pitchShift: -2,
      rateMultiplier: 0.95,
      emphasisLevel: 0.6,
      volumeAdjustment: 1,
      pauseDurationMultiplier: 1.1,
    },
  },
];

export const ProsodyEditor: React.FC<ProsodyEditorProps> = ({
  prosody,
  onChange,
}) => {
  const styles = useStyles();
  const [settings, setSettings] = useState<ProsodySettings>(
    prosody || defaultProsody
  );

  useEffect(() => {
    if (prosody) {
      setSettings(prosody);
    }
  }, [prosody]);

  const handleChange = (field: keyof ProsodySettings, value: number) => {
    const updated = { ...settings, [field]: value };
    setSettings(updated);
    onChange(updated);
  };

  const handleReset = () => {
    setSettings(defaultProsody);
    onChange(defaultProsody);
  };

  const handlePresetSelect = (preset: typeof presets[0]) => {
    setSettings(preset.settings);
    onChange(preset.settings);
  };

  const formatPitch = (value: number) => {
    return value > 0 ? `+${value} semitones` : `${value} semitones`;
  };

  const formatRate = (value: number) => {
    return `${(value * 100).toFixed(0)}%`;
  };

  const formatVolume = (value: number) => {
    return value > 0 ? `+${value} dB` : `${value} dB`;
  };

  const formatEmphasis = (value: number) => {
    return `${(value * 100).toFixed(0)}%`;
  };

  return (
    <div className={styles.container}>
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Presets</Text>
        <div className={styles.presetsGrid}>
          {presets.map(preset => (
            <Card
              key={preset.name}
              className={styles.presetCard}
              onClick={() => handlePresetSelect(preset)}
            >
              <Text weight="semibold">{preset.name}</Text>
              <Text size={200} className={styles.description}>
                {preset.description}
              </Text>
            </Card>
          ))}
        </div>
      </div>

      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Custom Settings</Text>
        <div className={styles.sliderGroup}>
          <div className={styles.sliderRow}>
            <div className={styles.sliderHeader}>
              <Text className={styles.sliderLabel}>Pitch</Text>
              <Text className={styles.sliderValue}>
                {formatPitch(settings.pitchShift)}
              </Text>
            </div>
            <Slider
              min={-12}
              max={12}
              step={1}
              value={settings.pitchShift}
              onChange={(_, data) => handleChange('pitchShift', data.value)}
            />
            <Text className={styles.description}>
              Adjust voice pitch up or down
            </Text>
          </div>

          <div className={styles.sliderRow}>
            <div className={styles.sliderHeader}>
              <Text className={styles.sliderLabel}>Speaking Rate</Text>
              <Text className={styles.sliderValue}>
                {formatRate(settings.rateMultiplier)}
              </Text>
            </div>
            <Slider
              min={0.5}
              max={2.0}
              step={0.05}
              value={settings.rateMultiplier}
              onChange={(_, data) => handleChange('rateMultiplier', data.value)}
            />
            <Text className={styles.description}>
              Adjust speech speed (50% to 200%)
            </Text>
          </div>

          <div className={styles.sliderRow}>
            <div className={styles.sliderHeader}>
              <Text className={styles.sliderLabel}>Emphasis</Text>
              <Text className={styles.sliderValue}>
                {formatEmphasis(settings.emphasisLevel)}
              </Text>
            </div>
            <Slider
              min={0}
              max={1}
              step={0.05}
              value={settings.emphasisLevel}
              onChange={(_, data) => handleChange('emphasisLevel', data.value)}
            />
            <Text className={styles.description}>
              Adjust speech emphasis and energy
            </Text>
          </div>

          <div className={styles.sliderRow}>
            <div className={styles.sliderHeader}>
              <Text className={styles.sliderLabel}>Volume</Text>
              <Text className={styles.sliderValue}>
                {formatVolume(settings.volumeAdjustment)}
              </Text>
            </div>
            <Slider
              min={-20}
              max={20}
              step={1}
              value={settings.volumeAdjustment}
              onChange={(_, data) => handleChange('volumeAdjustment', data.value)}
            />
            <Text className={styles.description}>
              Adjust overall volume level
            </Text>
          </div>

          <div className={styles.sliderRow}>
            <div className={styles.sliderHeader}>
              <Text className={styles.sliderLabel}>Pause Duration</Text>
              <Text className={styles.sliderValue}>
                {formatRate(settings.pauseDurationMultiplier)}
              </Text>
            </div>
            <Slider
              min={0.5}
              max={2.0}
              step={0.1}
              value={settings.pauseDurationMultiplier}
              onChange={(_, data) =>
                handleChange('pauseDurationMultiplier', data.value)
              }
            />
            <Text className={styles.description}>
              Adjust duration of pauses between sentences
            </Text>
          </div>
        </div>
      </div>

      <div className={styles.controls}>
        <Button
          appearance="subtle"
          icon={<ArrowResetRegular />}
          onClick={handleReset}
        >
          Reset to Default
        </Button>
      </div>
    </div>
  );
};
