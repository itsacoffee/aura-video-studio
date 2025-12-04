import {
  Text,
  Slider,
  Switch,
  Button,
  Dropdown,
  Option,
  makeStyles,
  tokens,
  Badge,
  Tooltip,
  Card,
  Spinner,
  OptionOnSelectData,
} from '@fluentui/react-components';
import {
  Speaker224Regular,
  Info16Regular,
  Settings24Regular,
  Mic24Regular,
  MusicNote224Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import React, { useCallback } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalS,
  },
  card: {
    padding: tokens.spacingVerticalM,
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  settingItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  sliderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  sliderLabel: {
    minWidth: '120px',
    fontSize: tokens.fontSizeBase300,
  },
  slider: {
    flex: 1,
  },
  valueDisplay: {
    minWidth: '50px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  presetButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap' as const,
  },
  analysisResults: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  analysisRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  loadingState: {
    display: 'flex',
    justifyContent: 'center',
    padding: tokens.spacingVerticalL,
  },
});

export type DuckingProfile = 'Aggressive' | 'Balanced' | 'Gentle' | 'Dynamic';
export type VoiceEnhancementPreset =
  | 'Light'
  | 'Standard'
  | 'Broadcast'
  | 'Podcast'
  | 'VideoNarration';
export type SfxIntensity = 'Low' | 'Medium' | 'High';

export interface AudioSettings {
  // Ducking settings
  duckingEnabled: boolean;
  duckingProfile: DuckingProfile;
  musicBaseVolume: number;

  // Voice enhancement settings
  voiceEnhancementEnabled: boolean;
  voiceEnhancementPreset: VoiceEnhancementPreset;
  noiseReductionStrength: number;
  targetLUFS: number;

  // SFX settings
  sfxEnabled: boolean;
  sfxIntensity: SfxIntensity;
  sfxVolume: number;
}

interface AudioSettingsPanelProps {
  settings: AudioSettings;
  onChange: (settings: AudioSettings) => void;
  onAnalyzeAudio?: (audioPath: string) => Promise<void>;
  isAnalyzing?: boolean;
  analysisResult?: {
    loudness: number;
    noiseFloor: number;
    hasClipping: boolean;
    recommendations: string[];
  };
}

export const AudioSettingsPanel: React.FC<AudioSettingsPanelProps> = ({
  settings,
  onChange,
  onAnalyzeAudio,
  isAnalyzing = false,
  analysisResult,
}) => {
  const styles = useStyles();

  const updateSettings = useCallback(
    (updates: Partial<AudioSettings>) => {
      onChange({ ...settings, ...updates });
    },
    [settings, onChange]
  );

  const handleDuckingProfileChange = (_: unknown, data: OptionOnSelectData) => {
    updateSettings({ duckingProfile: data.optionValue as DuckingProfile });
  };

  const handleVoicePresetChange = (_: unknown, data: OptionOnSelectData) => {
    updateSettings({ voiceEnhancementPreset: data.optionValue as VoiceEnhancementPreset });
  };

  const handleSfxIntensityChange = (_: unknown, data: OptionOnSelectData) => {
    updateSettings({ sfxIntensity: data.optionValue as SfxIntensity });
  };

  const handleAnalyze = async () => {
    if (onAnalyzeAudio) {
      await onAnalyzeAudio('');
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text size={500} weight="semibold">
          <Settings24Regular /> Audio Settings
        </Text>
      </div>

      {/* Ducking Settings */}
      <Card className={styles.card}>
        <div className={styles.sectionHeader}>
          <Speaker224Regular />
          <Text size={400} weight="semibold">
            Intelligent Ducking
          </Text>
          <Switch
            checked={settings.duckingEnabled}
            onChange={(_, data) => updateSettings({ duckingEnabled: data.checked })}
          />
        </div>

        {settings.duckingEnabled && (
          <div className={styles.section}>
            <div className={styles.settingItem}>
              <Text size={300}>Ducking Profile</Text>
              <Dropdown
                value={settings.duckingProfile}
                selectedOptions={[settings.duckingProfile]}
                onOptionSelect={handleDuckingProfileChange}
              >
                <Option value="Aggressive">
                  Aggressive - Strong ducking for educational content
                </Option>
                <Option value="Balanced">Balanced - Good for most videos</Option>
                <Option value="Gentle">Gentle - Subtle ducking for ambient content</Option>
                <Option value="Dynamic">Dynamic - Adapts based on speech intensity</Option>
              </Dropdown>
            </div>

            <div className={styles.sliderRow}>
              <Text className={styles.sliderLabel}>Music Base Volume</Text>
              <Slider
                className={styles.slider}
                min={10}
                max={50}
                step={5}
                value={settings.musicBaseVolume * 100}
                onChange={(_, data) => updateSettings({ musicBaseVolume: data.value / 100 })}
              />
              <Text className={styles.valueDisplay}>
                {Math.round(settings.musicBaseVolume * 100)}%
              </Text>
            </div>

            <div className={styles.analysisResults}>
              <Text size={300}>
                <Tooltip
                  content="Music volume during narration will be reduced to this level"
                  relationship="description"
                >
                  <Info16Regular />
                </Tooltip>{' '}
                During speech, music will be at {Math.round(settings.musicBaseVolume * 100)}% volume
              </Text>
            </div>
          </div>
        )}
      </Card>

      {/* Voice Enhancement Settings */}
      <Card className={styles.card}>
        <div className={styles.sectionHeader}>
          <Mic24Regular />
          <Text size={400} weight="semibold">
            Voice Enhancement
          </Text>
          <Switch
            checked={settings.voiceEnhancementEnabled}
            onChange={(_, data) => updateSettings({ voiceEnhancementEnabled: data.checked })}
          />
        </div>

        {settings.voiceEnhancementEnabled && (
          <div className={styles.section}>
            <div className={styles.settingItem}>
              <Text size={300}>Enhancement Preset</Text>
              <Dropdown
                value={settings.voiceEnhancementPreset}
                selectedOptions={[settings.voiceEnhancementPreset]}
                onOptionSelect={handleVoicePresetChange}
              >
                <Option value="Light">Light - Minimal processing</Option>
                <Option value="Standard">Standard - Balanced enhancement</Option>
                <Option value="Broadcast">Broadcast - Professional grade</Option>
                <Option value="Podcast">Podcast - Optimized for spoken word</Option>
                <Option value="VideoNarration">Video Narration - Best for mixing with music</Option>
              </Dropdown>
            </div>

            <div className={styles.sliderRow}>
              <Text className={styles.sliderLabel}>Noise Reduction</Text>
              <Slider
                className={styles.slider}
                min={0}
                max={100}
                step={10}
                value={settings.noiseReductionStrength * 100}
                onChange={(_, data) => updateSettings({ noiseReductionStrength: data.value / 100 })}
              />
              <Text className={styles.valueDisplay}>
                {Math.round(settings.noiseReductionStrength * 100)}%
              </Text>
            </div>

            <div className={styles.sliderRow}>
              <Text className={styles.sliderLabel}>Target LUFS</Text>
              <Tooltip
                content="Loudness Units Full Scale - standard is -14 LUFS"
                relationship="description"
              >
                <Info16Regular />
              </Tooltip>
              <Slider
                className={styles.slider}
                min={-24}
                max={-10}
                step={1}
                value={settings.targetLUFS}
                onChange={(_, data) => updateSettings({ targetLUFS: data.value })}
              />
              <Badge appearance="tint" color="success">
                {settings.targetLUFS} LUFS
              </Badge>
            </div>

            {analysisResult && (
              <div className={styles.analysisResults}>
                <Text size={300} weight="semibold">
                  Analysis Results
                </Text>
                <div className={styles.analysisRow}>
                  <Text size={300}>Current Loudness:</Text>
                  <Badge appearance="tint">{analysisResult.loudness.toFixed(1)} LUFS</Badge>
                </div>
                <div className={styles.analysisRow}>
                  <Text size={300}>Noise Floor:</Text>
                  <Badge
                    appearance="tint"
                    color={analysisResult.noiseFloor > -45 ? 'warning' : 'success'}
                  >
                    {analysisResult.noiseFloor.toFixed(1)} dB
                  </Badge>
                </div>
                {analysisResult.hasClipping && (
                  <Badge appearance="filled" color="danger">
                    Clipping Detected
                  </Badge>
                )}
                {analysisResult.recommendations.map((rec, i) => (
                  <Text key={i} size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    • {rec}
                  </Text>
                ))}
              </div>
            )}

            {onAnalyzeAudio && (
              <Button
                appearance="secondary"
                icon={isAnalyzing ? <Spinner size="tiny" /> : <Sparkle24Regular />}
                onClick={handleAnalyze}
                disabled={isAnalyzing}
              >
                {isAnalyzing ? 'Analyzing...' : 'Analyze Audio'}
              </Button>
            )}
          </div>
        )}
      </Card>

      {/* Sound Effects Settings */}
      <Card className={styles.card}>
        <div className={styles.sectionHeader}>
          <MusicNote224Regular />
          <Text size={400} weight="semibold">
            Sound Effects
          </Text>
          <Switch
            checked={settings.sfxEnabled}
            onChange={(_, data) => updateSettings({ sfxEnabled: data.checked })}
          />
        </div>

        {settings.sfxEnabled && (
          <div className={styles.section}>
            <div className={styles.settingItem}>
              <Text size={300}>SFX Intensity</Text>
              <Dropdown
                value={settings.sfxIntensity}
                selectedOptions={[settings.sfxIntensity]}
                onOptionSelect={handleSfxIntensityChange}
              >
                <Option value="Low">Low - Subtle transitions only</Option>
                <Option value="Medium">Medium - Standard effects</Option>
                <Option value="High">High - Full sound design</Option>
              </Dropdown>
            </div>

            <div className={styles.sliderRow}>
              <Text className={styles.sliderLabel}>SFX Volume</Text>
              <Slider
                className={styles.slider}
                min={0}
                max={100}
                value={settings.sfxVolume}
                onChange={(_, data) => updateSettings({ sfxVolume: data.value })}
              />
              <Text className={styles.valueDisplay}>{settings.sfxVolume}%</Text>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
};

export default AudioSettingsPanel;
