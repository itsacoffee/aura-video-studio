
import {
  makeStyles,
  tokens,
  Text,
  Body1Strong,
  Caption1,
  Dropdown,
  Option,
  Field,
  Switch,
  Slider,
  Card,
} from '@fluentui/react-components';

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
  fieldGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  fullWidth: {
    gridColumn: '1 / -1',
  },
});

const QUALITY_OPTIONS = [
  { key: 'draft', text: 'Draft (Fast)', description: 'Quick preview quality' },
  { key: 'good', text: 'Good', description: 'Standard quality' },
  { key: 'high', text: 'High (Recommended)', description: 'Professional quality' },
  { key: 'maximum', text: 'Maximum (Slow)', description: 'Best quality, slower' },
];

const FORMAT_OPTIONS = [
  { key: 'mp4', text: 'MP4 (H.264)', description: 'Most compatible' },
  { key: 'webm', text: 'WebM (VP9)', description: 'Web optimized' },
  { key: 'mov', text: 'MOV (H.264)', description: 'Apple devices' },
];

const RESOLUTION_PRESETS = [
  { key: 'source', text: 'Source Resolution', width: 0, height: 0 },
  { key: '4k', text: '4K (3840×2160)', width: 3840, height: 2160 },
  { key: '1080p', text: '1080p (1920×1080)', width: 1920, height: 1080 },
  { key: '720p', text: '720p (1280×720)', width: 1280, height: 720 },
  { key: '480p', text: '480p (854×480)', width: 854, height: 480 },
];

export interface ExportSettings {
  resolution: { width: number; height: number };
  quality: 'draft' | 'good' | 'high' | 'maximum';
  format: string;
  optimizeForPlatform: boolean;
  hardwareAcceleration?: boolean;
  audioBitrate?: number;
}

export interface ExportSettingsEditorProps {
  settings: ExportSettings;
  onChange: (settings: Partial<ExportSettings>) => void;
  showAdvanced?: boolean;
}

export function ExportSettingsEditor({
  settings,
  onChange,
  showAdvanced = false,
}: ExportSettingsEditorProps) {
  const styles = useStyles();

  const handleResolutionChange = (value: string) => {
    const preset = RESOLUTION_PRESETS.find((p) => p.key === value);
    if (preset) {
      onChange({
        resolution: { width: preset.width, height: preset.height },
      });
    }
  };

  const handleQualityChange = (value: string) => {
    onChange({ quality: value as ExportSettings['quality'] });
  };

  const handleFormatChange = (value: string) => {
    onChange({ format: value });
  };

  const getCurrentResolutionKey = () => {
    const { width, height } = settings.resolution;
    const preset = RESOLUTION_PRESETS.find(
      (p) => p.width === width && p.height === height
    );
    return preset?.key || 'custom';
  };

  return (
    <Card>
      <div className={styles.container} style={{ padding: tokens.spacingVerticalL }}>
        <Text>
          <Body1Strong>Export Settings</Body1Strong>
        </Text>

        <div className={styles.section}>
          <div className={styles.fieldGrid}>
            <Field label="Quality Preset">
              <Dropdown
                value={
                  QUALITY_OPTIONS.find((q) => q.key === settings.quality)?.text
                }
                onOptionSelect={(_, data) =>
                  handleQualityChange(data.optionValue as string)
                }
              >
                {QUALITY_OPTIONS.map((option) => (
                  <Option key={option.key} value={option.key} text={option.text}>
                    {option.text}
                    <Caption1> - {option.description}</Caption1>
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="Output Format">
              <Dropdown
                value={
                  FORMAT_OPTIONS.find((f) => f.key === settings.format)?.text
                }
                onOptionSelect={(_, data) =>
                  handleFormatChange(data.optionValue as string)
                }
              >
                {FORMAT_OPTIONS.map((option) => (
                  <Option key={option.key} value={option.key} text={option.text}>
                    {option.text}
                    <Caption1> - {option.description}</Caption1>
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="Resolution" className={styles.fullWidth}>
              <Dropdown
                value={
                  RESOLUTION_PRESETS.find((r) => r.key === getCurrentResolutionKey())
                    ?.text || 'Custom'
                }
                onOptionSelect={(_, data) =>
                  handleResolutionChange(data.optionValue as string)
                }
              >
                {RESOLUTION_PRESETS.map((option) => (
                  <Option key={option.key} value={option.key} text={option.text}>
                    {option.text}
                  </Option>
                ))}
              </Dropdown>
            </Field>
          </div>
        </div>

        <div className={styles.section}>
          <Field>
            <Switch
              checked={settings.optimizeForPlatform}
              onChange={(_, data) =>
                onChange({ optimizeForPlatform: data.checked })
              }
              label="Platform-specific optimization"
            />
            <Caption1>
              Automatically adjust settings for each platform&apos;s requirements
            </Caption1>
          </Field>

          {showAdvanced && (
            <Field>
              <Switch
                checked={settings.hardwareAcceleration ?? true}
                onChange={(_, data) =>
                  onChange({ hardwareAcceleration: data.checked })
                }
                label="Hardware acceleration (GPU)"
              />
              <Caption1>
                Use GPU encoding for faster exports (if available)
              </Caption1>
            </Field>
          )}
        </div>

        {showAdvanced && (
          <div className={styles.section}>
            <Field label={`Audio Bitrate: ${settings.audioBitrate || 192} kbps`}>
              <Slider
                min={64}
                max={320}
                step={64}
                value={settings.audioBitrate || 192}
                onChange={(_, data) =>
                  onChange({ audioBitrate: data.value })
                }
              />
              <Caption1>
                Higher bitrate = better audio quality but larger file size
              </Caption1>
            </Field>
          </div>
        )}
      </div>
    </Card>
  );
}
