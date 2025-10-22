import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Field,
  Switch,
  Slider,
  Dropdown,
  Option,
  Card,
  Body1,
  Caption1,
  Badge,
  Divider,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Warning24Regular,
  Delete24Regular,
  FolderOpen24Regular,
  DocumentText24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  hardwareInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  encoder: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  pathField: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  cacheInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
});

export function RenderSettings() {
  const styles = useStyles();
  const [hardwareAcceleration, setHardwareAcceleration] = useState(true);
  const [detectedGpu] = useState('NVIDIA GeForce RTX 3080');
  const [presetQuality, setPresetQuality] = useState(75);
  const [autoPreview, setAutoPreview] = useState(false);
  const [previewQuality, setPreviewQuality] = useState('720p');
  const [smartRendering, setSmartRendering] = useState(true);
  const [cacheLocation] = useState('/Users/username/.aura/render_cache');
  const [cacheSize] = useState('2.4 GB in 15 cached renders');
  const [exportDefaultLocation] = useState('/Users/username/Videos/Aura');
  const [defaultPreset, setDefaultPreset] = useState('YouTube 1080p');
  const [maxParallelExports, setMaxParallelExports] = useState(1);
  const [autoRetry, setAutoRetry] = useState(true);
  const [desktopNotifications, setDesktopNotifications] = useState(true);

  const [encoders] = useState({
    h264_nvenc: true,
    hevc_nvenc: true,
    h264_amf: false,
    h264_qsv: false,
    libx264: true,
    libx265: true,
  });
  const getQualityLabel = (value: number) => {
    if (value < 25) return 'Draft';
    if (value < 50) return 'Good';
    if (value < 75) return 'High';
    return 'Maximum';
  };

  const handleSave = () => {
    // TODO: Save settings to API
  };

  const handleClearCache = () => {
    if (
      confirm(
        'Are you sure you want to clear the render cache? This will remove all cached renders.'
      )
    ) {
      // TODO: Clear cache via API
    }
  };

  const handleBrowseCacheLocation = () => {
    // TODO: Open folder browser
  };

  const handleBrowseExportLocation = () => {
    // TODO: Open folder browser
  };

  const handleShowFFmpegLog = () => {
    // TODO: Show FFmpeg log viewer
  };

  return (
    <div className={styles.container}>
      <div className={styles.section}>
        <Title2>Hardware Acceleration</Title2>
        <Card className={styles.card}>
          <Field label="Enable GPU Encoding">
            <Switch
              checked={hardwareAcceleration}
              onChange={(_, data) => setHardwareAcceleration(data.checked)}
              label={hardwareAcceleration ? 'Enabled' : 'Disabled'}
            />
          </Field>
          <Caption1>
            Use GPU for video encoding for 5-10x faster rendering. Automatically detects available
            hardware encoders.
          </Caption1>

          {detectedGpu && (
            <div className={styles.hardwareInfo}>
              <Body1>
                <strong>Detected GPU:</strong> {detectedGpu}
              </Body1>
              <Divider />
              <Body1>
                <strong>Available Encoders:</strong>
              </Body1>
              {Object.entries(encoders).map(([encoder, available]) => (
                <div key={encoder} className={styles.encoder}>
                  <Caption1>{encoder}</Caption1>
                  {available ? (
                    <Badge color="success" icon={<CheckmarkCircle24Regular />}>
                      Available
                    </Badge>
                  ) : (
                    <Badge color="subtle" icon={<Warning24Regular />}>
                      Not available
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Default Quality Settings</Title2>
        <Card className={styles.card}>
          <Field label={`Preset Render Quality: ${getQualityLabel(presetQuality)}`}>
            <Slider
              value={presetQuality}
              onChange={(_, data) => setPresetQuality(data.value)}
              min={0}
              max={100}
              step={1}
            />
          </Field>
          <Caption1>
            Default quality level for all exports. Higher quality means slower rendering but better
            output.
          </Caption1>

          <Field label="Default Export Preset">
            <Dropdown
              value={defaultPreset}
              onOptionSelect={(_, data) => setDefaultPreset(data.optionValue as string)}
            >
              <Option value="YouTube 1080p">YouTube 1080p</Option>
              <Option value="YouTube 4K">YouTube 4K</Option>
              <Option value="Instagram Feed">Instagram Feed</Option>
              <Option value="Instagram Story">Instagram Story</Option>
              <Option value="TikTok">TikTok</Option>
              <Option value="Facebook">Facebook</Option>
              <Option value="Twitter">Twitter</Option>
              <Option value="LinkedIn">LinkedIn</Option>
            </Dropdown>
          </Field>
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Preview Generation</Title2>
        <Card className={styles.card}>
          <Field label="Automatic Preview Generation">
            <Switch
              checked={autoPreview}
              onChange={(_, data) => setAutoPreview(data.checked)}
              label={autoPreview ? 'Enabled' : 'Disabled'}
            />
          </Field>
          <Caption1>
            Automatically generate low-resolution preview after every major timeline change for
            quick review.
          </Caption1>

          {autoPreview && (
            <Field label="Preview Quality">
              <Dropdown
                value={previewQuality}
                onOptionSelect={(_, data) => setPreviewQuality(data.optionValue as string)}
              >
                <Option value="360p">360p (Fastest)</Option>
                <Option value="480p">480p</Option>
                <Option value="720p">720p (Recommended)</Option>
              </Dropdown>
            </Field>
          )}
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Smart Rendering</Title2>
        <Card className={styles.card}>
          <Field label="Enable Smart Rendering">
            <Switch
              checked={smartRendering}
              onChange={(_, data) => setSmartRendering(data.checked)}
              label={smartRendering ? 'Enabled' : 'Disabled'}
            />
          </Field>
          <Caption1>
            Only re-render modified scenes when exporting, saving significant time on minor edits.
            Caches rendered scenes for reuse.
          </Caption1>

          {smartRendering && (
            <>
              <Field label="Render Cache Location">
                <div className={styles.pathField}>
                  <Text style={{ flex: 1, padding: tokens.spacingVerticalS }}>{cacheLocation}</Text>
                  <Button
                    appearance="secondary"
                    icon={<FolderOpen24Regular />}
                    onClick={handleBrowseCacheLocation}
                  >
                    Browse
                  </Button>
                </div>
              </Field>

              <div className={styles.cacheInfo}>
                <Body1>
                  <strong>Cache Size:</strong> {cacheSize}
                </Body1>
                <div className={styles.actions}>
                  <Button
                    appearance="secondary"
                    icon={<Delete24Regular />}
                    onClick={handleClearCache}
                  >
                    Clear Cache
                  </Button>
                </div>
              </div>
            </>
          )}
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Export Settings</Title2>
        <Card className={styles.card}>
          <Field label="Default Export Location">
            <div className={styles.pathField}>
              <Text style={{ flex: 1, padding: tokens.spacingVerticalS }}>
                {exportDefaultLocation}
              </Text>
              <Button
                appearance="secondary"
                icon={<FolderOpen24Regular />}
                onClick={handleBrowseExportLocation}
              >
                Browse
              </Button>
            </div>
          </Field>
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Queue Settings</Title2>
        <Card className={styles.card}>
          <Field label="Max Parallel Exports">
            <Dropdown
              value={maxParallelExports.toString()}
              onOptionSelect={(_, data) =>
                setMaxParallelExports(parseInt(data.optionValue as string))
              }
            >
              <Option value="1">1 (Recommended)</Option>
              <Option value="2">2</Option>
              <Option value="3">3</Option>
              <Option value="4">4</Option>
            </Dropdown>
          </Field>
          <Caption1>
            Number of exports to process simultaneously. Higher values may cause resource
            exhaustion.
          </Caption1>

          <Field label="Auto-Retry Failed Exports">
            <Switch
              checked={autoRetry}
              onChange={(_, data) => setAutoRetry(data.checked)}
              label={autoRetry ? 'Enabled' : 'Disabled'}
            />
          </Field>

          <Field label="Desktop Notifications">
            <Switch
              checked={desktopNotifications}
              onChange={(_, data) => setDesktopNotifications(data.checked)}
              label={desktopNotifications ? 'Enabled' : 'Disabled'}
            />
          </Field>
          <Caption1>Show desktop notification when exports complete.</Caption1>
        </Card>
      </div>

      <div className={styles.section}>
        <Title2>Troubleshooting</Title2>
        <Card className={styles.card}>
          <Button
            appearance="secondary"
            icon={<DocumentText24Regular />}
            onClick={handleShowFFmpegLog}
          >
            Show FFmpeg Log
          </Button>
          <Caption1>View FFmpeg output logs for troubleshooting encoding issues.</Caption1>
        </Card>
      </div>

      <div className={styles.actions}>
        <Button appearance="primary" onClick={handleSave}>
          Save Settings
        </Button>
      </div>
    </div>
  );
}
