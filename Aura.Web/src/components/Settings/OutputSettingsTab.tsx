import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Dropdown,
  Option,
  Slider,
  Card,
  Field,
  Switch,
  Input,
  Tooltip,
} from '@fluentui/react-components';
import { Info24Regular, Save24Regular } from '@fluentui/react-icons';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    marginBottom: tokens.spacingVerticalL,
  },
  gridLayout: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
    '@media (max-width: 768px)': {
      gridTemplateColumns: '1fr',
    },
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
  infoIcon: {
    color: tokens.colorNeutralForeground3,
    verticalAlign: 'middle',
    marginLeft: tokens.spacingHorizontalXS,
  },
});

const RESOLUTION_PRESETS = [
  { label: '720p (1280x720)', width: 1280, height: 720 },
  { label: '1080p (1920x1080)', width: 1920, height: 1080 },
  { label: '1440p (2560x1440)', width: 2560, height: 1440 },
  { label: '4K (3840x2160)', width: 3840, height: 2160 },
  { label: 'Custom', width: 0, height: 0 },
];

const ASPECT_RATIOS = [
  { label: '16:9 (Widescreen)', value: '16:9' },
  { label: '9:16 (Vertical/Shorts)', value: '9:16' },
  { label: '1:1 (Square)', value: '1:1' },
  { label: '4:3 (Classic)', value: '4:3' },
  { label: '21:9 (Ultrawide)', value: '21:9' },
];

const VIDEO_FORMATS = [
  { label: 'MP4 (Recommended)', value: 'mp4' },
  { label: 'MKV (Matroska)', value: 'mkv' },
  { label: 'MOV (QuickTime)', value: 'mov' },
  { label: 'WEBM', value: 'webm' },
];

const FRAME_RATES = [23.976, 24, 25, 29.97, 30, 50, 59.94, 60, 120];

export function OutputSettingsTab() {
  const styles = useStyles();
  const [modified, setModified] = useState(false);
  const [saving, setSaving] = useState(false);

  // Output settings state
  const [resolutionPreset, setResolutionPreset] = useState('1080p (1920x1080)');
  const [customWidth, setCustomWidth] = useState(1920);
  const [customHeight, setCustomHeight] = useState(1080);
  const [aspectRatio, setAspectRatio] = useState('16:9');
  const [frameRate, setFrameRate] = useState(30);
  const [videoFormat, setVideoFormat] = useState('mp4');
  const [defaultCodec, setDefaultCodec] = useState('H264');
  const [defaultQuality, setDefaultQuality] = useState(23); // CRF value
  const [enableHDR, setEnableHDR] = useState(false);
  const [defaultBitrate, setDefaultBitrate] = useState(12000); // kbps
  const [audioBitrate, setAudioBitrate] = useState(192); // kbps
  const [audioSampleRate, setAudioSampleRate] = useState(48000);
  const [enableLoudnessNorm, setEnableLoudnessNorm] = useState(true);
  const [targetLUFS, setTargetLUFS] = useState(-14);
  const [outputDirectory, setOutputDirectory] = useState('');
  const [filenameTemplate, setFilenameTemplate] = useState('{title}_{date}_{time}');

  useEffect(() => {
    fetchOutputSettings();
  }, []);

  const fetchOutputSettings = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/output'));
      if (response.ok) {
        const data = await response.json();
        // Apply loaded settings
        setResolutionPreset(data.resolutionPreset || '1080p (1920x1080)');
        setCustomWidth(data.customWidth || 1920);
        setCustomHeight(data.customHeight || 1080);
        setAspectRatio(data.aspectRatio || '16:9');
        setFrameRate(data.frameRate || 30);
        setVideoFormat(data.videoFormat || 'mp4');
        setDefaultCodec(data.defaultCodec || 'H264');
        setDefaultQuality(data.defaultQuality || 23);
        setEnableHDR(data.enableHDR || false);
        setDefaultBitrate(data.defaultBitrate || 12000);
        setAudioBitrate(data.audioBitrate || 192);
        setAudioSampleRate(data.audioSampleRate || 48000);
        setEnableLoudnessNorm(data.enableLoudnessNorm !== false);
        setTargetLUFS(data.targetLUFS || -14);
        setOutputDirectory(data.outputDirectory || '');
        setFilenameTemplate(data.filenameTemplate || '{title}_{date}_{time}');
      }
    } catch (error) {
      console.error('Error fetching output settings:', error);
    }
  };

  const saveOutputSettings = async () => {
    setSaving(true);
    try {
      const response = await fetch('/api/settings/output', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          resolutionPreset,
          customWidth,
          customHeight,
          aspectRatio,
          frameRate,
          videoFormat,
          defaultCodec,
          defaultQuality,
          enableHDR,
          defaultBitrate,
          audioBitrate,
          audioSampleRate,
          enableLoudnessNorm,
          targetLUFS,
          outputDirectory,
          filenameTemplate,
        }),
      });
      if (response.ok) {
        alert('Output settings saved successfully');
        setModified(false);
      } else {
        alert('Error saving output settings');
      }
    } catch (error) {
      console.error('Error saving output settings:', error);
      alert('Error saving output settings');
    } finally {
      setSaving(false);
    }
  };

  const handlePresetChange = (value: string) => {
    setResolutionPreset(value);
    setModified(true);
    const preset = RESOLUTION_PRESETS.find((p) => p.label === value);
    if (preset && preset.width > 0) {
      setCustomWidth(preset.width);
      setCustomHeight(preset.height);
    }
  };

  return (
    <Card className={styles.section}>
      <Title2>Output Settings</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Configure default video output parameters for rendering
      </Text>

      <Card className={styles.infoCard}>
        <Text weight="semibold" size={300}>
          💡 Tip
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          These settings define the default output parameters. You can override them per-project in
          the Render page.
        </Text>
      </Card>

      <div className={styles.form}>
        {/* Video Settings */}
        <Text size={400} weight="semibold">
          Video Settings
        </Text>

        <Field label="Resolution Preset">
          <Dropdown
            value={resolutionPreset}
            onOptionSelect={(_, data) => handlePresetChange(data.optionValue || '1080p (1920x1080)')}
            style={{ width: '100%' }}
          >
            {RESOLUTION_PRESETS.map((preset) => (
              <Option key={preset.label} value={preset.label}>
                {preset.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        {resolutionPreset === 'Custom' && (
          <div className={styles.gridLayout}>
            <Field label="Width">
              <Input
                type="number"
                value={customWidth.toString()}
                onChange={(e) => {
                  setCustomWidth(parseInt(e.target.value) || 1920);
                  setModified(true);
                }}
              />
            </Field>
            <Field label="Height">
              <Input
                type="number"
                value={customHeight.toString()}
                onChange={(e) => {
                  setCustomHeight(parseInt(e.target.value) || 1080);
                  setModified(true);
                }}
              />
            </Field>
          </div>
        )}

        <Field label="Aspect Ratio">
          <Dropdown
            value={aspectRatio}
            onOptionSelect={(_, data) => {
              setAspectRatio(data.optionValue || '16:9');
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            {ASPECT_RATIOS.map((ratio) => (
              <Option key={ratio.value} value={ratio.value}>
                {ratio.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Frame Rate (FPS)">
          <Dropdown
            value={frameRate.toString()}
            onOptionSelect={(_, data) => {
              setFrameRate(parseFloat(data.optionValue || '30'));
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            {FRAME_RATES.map((fps) => (
              <Option key={fps} value={fps.toString()} text={`${fps} FPS`}>
                {fps} FPS
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Video Format">
          <Dropdown
            value={videoFormat}
            onOptionSelect={(_, data) => {
              setVideoFormat(data.optionValue || 'mp4');
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            {VIDEO_FORMATS.map((format) => (
              <Option key={format.value} value={format.value}>
                {format.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Default Codec">
          <Dropdown
            value={defaultCodec}
            onOptionSelect={(_, data) => {
              setDefaultCodec(data.optionValue || 'H264');
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            <Option value="H264">H.264 (Most Compatible)</Option>
            <Option value="HEVC">HEVC/H.265 (Better Compression)</Option>
            <Option value="AV1">AV1 (Best Compression, RTX 40+ only)</Option>
          </Dropdown>
        </Field>

        <Field
          label={`Quality (CRF): ${defaultQuality}`}
          hint="Lower values = better quality, larger files. Recommended: 18-23"
        >
          <Slider
            min={14}
            max={35}
            step={1}
            value={defaultQuality}
            onChange={(_, data) => {
              setDefaultQuality(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field label={`Video Bitrate: ${defaultBitrate} kbps`} hint="Only used for constant bitrate mode">
          <Slider
            min={2000}
            max={50000}
            step={1000}
            value={defaultBitrate}
            onChange={(_, data) => {
              setDefaultBitrate(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field label="HDR Support">
          <Switch
            checked={enableHDR}
            onChange={(_, data) => {
              setEnableHDR(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableHDR ? 'Enabled' : 'Disabled'} - Requires high-end hardware (Tier A)
            <Tooltip
              content="HDR rendering is only available on systems with sufficient VRAM and modern GPUs"
              relationship="description"
            >
              <Info24Regular className={styles.infoIcon} />
            </Tooltip>
          </Text>
        </Field>

        {/* Audio Settings */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Audio Settings
        </Text>

        <Field label={`Audio Bitrate: ${audioBitrate} kbps`}>
          <Slider
            min={128}
            max={320}
            step={32}
            value={audioBitrate}
            onChange={(_, data) => {
              setAudioBitrate(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field label="Audio Sample Rate">
          <Dropdown
            value={audioSampleRate.toString()}
            onOptionSelect={(_, data) => {
              setAudioSampleRate(parseInt(data.optionValue || '48000'));
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            <Option value="44100">44.1 kHz</Option>
            <Option value="48000">48 kHz (Recommended)</Option>
            <Option value="96000">96 kHz (High Quality)</Option>
          </Dropdown>
        </Field>

        <Field label="Loudness Normalization">
          <Switch
            checked={enableLoudnessNorm}
            onChange={(_, data) => {
              setEnableLoudnessNorm(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {enableLoudnessNorm ? 'Enabled' : 'Disabled'} - Automatically normalize audio to target
            LUFS
          </Text>
        </Field>

        {enableLoudnessNorm && (
          <Field
            label={`Target LUFS: ${targetLUFS}`}
            hint="YouTube recommendation: -14 LUFS"
          >
            <Slider
              min={-20}
              max={-10}
              step={1}
              value={targetLUFS}
              onChange={(_, data) => {
                setTargetLUFS(data.value);
                setModified(true);
              }}
            />
          </Field>
        )}

        {/* Output Paths */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Output Paths
        </Text>

        <Field
          label="Output Directory"
          hint="Default directory for rendered videos (leave empty for default)"
        >
          <Input
            placeholder="C:\Users\YourName\Videos\AuraOutput"
            value={outputDirectory}
            onChange={(e) => {
              setOutputDirectory(e.target.value);
              setModified(true);
            }}
          />
        </Field>

        <Field
          label="Filename Template"
          hint="Use {title}, {date}, {time}, {resolution} as placeholders"
        >
          <Input
            value={filenameTemplate}
            onChange={(e) => {
              setFilenameTemplate(e.target.value);
              setModified(true);
            }}
          />
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            Example output: {filenameTemplate.replace('{title}', 'MyVideo').replace('{date}', '2025-10-24').replace('{time}', '143530').replace('{resolution}', '1080p')}
          </Text>
        </Field>

        {modified && (
          <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
            ⚠️ You have unsaved changes
          </Text>
        )}

        <div className={styles.actions}>
          <Button
            appearance="secondary"
            onClick={() => {
              fetchOutputSettings();
              setModified(false);
            }}
          >
            Reset
          </Button>
          <Button
            appearance="primary"
            icon={<Save24Regular />}
            onClick={saveOutputSettings}
            disabled={!modified || saving}
          >
            {saving ? 'Saving...' : 'Save Output Settings'}
          </Button>
        </div>
      </div>
    </Card>
  );
}
