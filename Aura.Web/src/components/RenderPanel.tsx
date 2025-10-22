import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Card,
  Text,
  Label,
  Dropdown,
  Option,
  Slider,
  Button,
  Input,
  Switch,
  Field,
  ProgressBar,
  Divider,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Dismiss24Regular,
  Checkmark24Regular,
  ErrorCircle24Regular,
} from '@fluentui/react-icons';
import { useRenderStore } from '../state/render';

const useStyles = makeStyles({
  panel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  field: {
    flex: 1,
  },
  resolutionInputs: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  qualitySlider: {
    width: '100%',
  },
  queueItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  queueItemInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  queueActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  statusIcon: {
    fontSize: '20px',
  },
  emptyQueue: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

const FPS_OPTIONS = [23.976, 24, 25, 30, 50, 60];
const CODEC_OPTIONS = [
  { value: 'H264', label: 'H.264 (Most Compatible)' },
  { value: 'HEVC', label: 'HEVC/H.265 (Better Compression)' },
  { value: 'AV1', label: 'AV1 (Best Compression, RTX 40+ only)' },
];
const CONTAINER_OPTIONS = ['mp4', 'mkv', 'mov'];
const PRESET_OPTIONS = [
  'YouTube 1080p',
  'YouTube Shorts',
  'YouTube 4K',
  'YouTube 1440p',
  'YouTube 720p',
];

export function RenderPanel() {
  const styles = useStyles();
  const {
    settings,
    queue,
    updateSettings,
    setPreset,
    addToQueue,
    removeFromQueue,
    updateQueueItem,
  } = useRenderStore();
  const [selectedPreset, setSelectedPreset] = useState('YouTube 1080p');

  const handlePresetChange = (
    _event: unknown,
    data: { optionValue?: string; optionText?: string }
  ) => {
    if (data.optionValue) {
      setSelectedPreset(data.optionValue);
      setPreset(data.optionValue);
    }
  };

  const handleStartRender = () => {
    addToQueue(settings);
  };

  const handleCancelRender = async (id: string) => {
    try {
      await fetch(`/api/render/${id}/cancel`, { method: 'POST' });
      updateQueueItem(id, { status: 'cancelled' });
    } catch (error) {
      console.error('Error cancelling render:', error);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return (
          <Checkmark24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteGreenForeground1 }}
          />
        );
      case 'failed':
        return (
          <ErrorCircle24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          />
        );
      case 'cancelled':
        return (
          <Dismiss24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorNeutralForeground3 }}
          />
        );
      default:
        return (
          <Play24Regular
            className={styles.statusIcon}
            style={{ color: tokens.colorPaletteBlueForeground2 }}
          />
        );
    }
  };

  const formatDuration = (seconds?: number) => {
    if (!seconds) return '--:--';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className={styles.panel}>
      <Card>
        <div className={styles.section}>
          <Text size={500} weight="semibold">
            Render Settings
          </Text>

          <Field label="Preset">
            <Dropdown
              value={selectedPreset}
              onOptionSelect={handlePresetChange}
              style={{ width: '100%' }}
            >
              {PRESET_OPTIONS.map((preset) => (
                <Option key={preset} value={preset}>
                  {preset}
                </Option>
              ))}
            </Dropdown>
          </Field>

          <Divider />

          <div className={styles.row}>
            <Field label="Resolution" className={styles.field}>
              <div className={styles.resolutionInputs}>
                <Input
                  type="number"
                  value={settings.resolution.width.toString()}
                  onChange={(e) =>
                    updateSettings({
                      resolution: {
                        ...settings.resolution,
                        width: parseInt(e.target.value) || 1920,
                      },
                    })
                  }
                  style={{ width: '100px' }}
                />
                <Text>×</Text>
                <Input
                  type="number"
                  value={settings.resolution.height.toString()}
                  onChange={(e) =>
                    updateSettings({
                      resolution: {
                        ...settings.resolution,
                        height: parseInt(e.target.value) || 1080,
                      },
                    })
                  }
                  style={{ width: '100px' }}
                />
              </div>
            </Field>

            <Field label="FPS" className={styles.field}>
              <Dropdown
                value={settings.fps.toString()}
                onOptionSelect={(_e, data) =>
                  updateSettings({ fps: parseFloat(data.optionValue || '30') })
                }
                style={{ width: '100%' }}
              >
                {FPS_OPTIONS.map((fps) => (
                  <Option key={fps} value={fps.toString()} text={fps.toString()}>
                    {fps}
                  </Option>
                ))}
              </Dropdown>
            </Field>
          </div>

          <div className={styles.row}>
            <Field label="Codec" className={styles.field}>
              <Dropdown
                value={settings.codec}
                onOptionSelect={(_e, data) =>
                  updateSettings({ codec: data.optionValue as 'H264' | 'HEVC' | 'AV1' })
                }
                style={{ width: '100%' }}
              >
                {CODEC_OPTIONS.map((codec) => (
                  <Option key={codec.value} value={codec.value}>
                    {codec.label}
                  </Option>
                ))}
              </Dropdown>
            </Field>

            <Field label="Container" className={styles.field}>
              <Dropdown
                value={settings.container}
                onOptionSelect={(_e, data) =>
                  updateSettings({ container: data.optionValue as 'mp4' | 'mkv' | 'mov' })
                }
                style={{ width: '100%' }}
              >
                {CONTAINER_OPTIONS.map((container) => (
                  <Option key={container} value={container}>
                    {container.toUpperCase()}
                  </Option>
                ))}
              </Dropdown>
            </Field>
          </div>

          <Field
            label={
              <Label>
                Quality Level: {settings.qualityLevel}
                <Text
                  size={200}
                  style={{
                    color: tokens.colorNeutralForeground3,
                    marginLeft: tokens.spacingHorizontalS,
                  }}
                >
                  (0 = fastest/lower quality, 100 = slowest/highest quality)
                </Text>
              </Label>
            }
          >
            <Slider
              className={styles.qualitySlider}
              min={0}
              max={100}
              value={settings.qualityLevel}
              onChange={(_e, data) => updateSettings({ qualityLevel: data.value })}
            />
          </Field>

          <div className={styles.row}>
            <Field label="Video Bitrate (kbps)" className={styles.field}>
              <Input
                type="number"
                value={settings.videoBitrateK.toString()}
                onChange={(e) =>
                  updateSettings({ videoBitrateK: parseInt(e.target.value) || 12000 })
                }
              />
            </Field>

            <Field label="Audio Bitrate (kbps)" className={styles.field}>
              <Input
                type="number"
                value={settings.audioBitrateK.toString()}
                onChange={(e) => updateSettings({ audioBitrateK: parseInt(e.target.value) || 256 })}
              />
            </Field>
          </div>

          <Field label="Scene-Cut Keyframes">
            <Switch
              checked={settings.enableSceneCut}
              onChange={(_e, data) => updateSettings({ enableSceneCut: data.checked })}
              label={settings.enableSceneCut ? 'Enabled' : 'Disabled'}
            />
          </Field>

          <Button appearance="primary" icon={<Play24Regular />} onClick={handleStartRender}>
            Add to Render Queue
          </Button>
        </div>
      </Card>

      <Card>
        <div className={styles.section}>
          <Text size={500} weight="semibold">
            Render Queue
          </Text>

          {queue.length === 0 ? (
            <div className={styles.emptyQueue}>
              <Text>No renders in queue</Text>
            </div>
          ) : (
            queue.map((item) => (
              <div key={item.id} className={styles.queueItem}>
                {getStatusIcon(item.status)}

                <div className={styles.queueItemInfo}>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                    }}
                  >
                    <Text weight="semibold">
                      {item.settings.resolution.width}×{item.settings.resolution.height} @
                      {item.settings.fps}fps
                    </Text>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      {item.status === 'processing' && item.estimatedTimeRemaining
                        ? `ETA: ${formatDuration(item.estimatedTimeRemaining)}`
                        : item.status}
                    </Text>
                  </div>

                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    {item.settings.codec} • {item.settings.container.toUpperCase()} • Quality:{' '}
                    {item.settings.qualityLevel}
                  </Text>

                  {(item.status === 'processing' || item.status === 'queued') && (
                    <ProgressBar value={item.progress / 100} />
                  )}

                  {item.error && (
                    <Text size={200} style={{ color: tokens.colorPaletteRedForeground1 }}>
                      Error: {item.error}
                    </Text>
                  )}
                </div>

                <div className={styles.queueActions}>
                  {(item.status === 'processing' || item.status === 'queued') && (
                    <Button
                      appearance="subtle"
                      icon={<Dismiss24Regular />}
                      onClick={() => handleCancelRender(item.id)}
                    >
                      Cancel
                    </Button>
                  )}
                  {(item.status === 'completed' ||
                    item.status === 'failed' ||
                    item.status === 'cancelled') && (
                    <Button
                      appearance="subtle"
                      icon={<Dismiss24Regular />}
                      onClick={() => removeFromQueue(item.id)}
                    >
                      Remove
                    </Button>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </Card>
    </div>
  );
}
