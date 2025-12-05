/**
 * ExportSettings Component
 *
 * Detailed settings panel for video and audio export configuration.
 * Allows fine-tuning of resolution, codec, bitrate, frame rate,
 * and other export parameters.
 */

import {
  makeStyles,
  tokens,
  Text,
  Select,
  Switch,
  Button,
  Slider,
  mergeClasses,
  SpinButton,
} from '@fluentui/react-components';
import { Save24Regular } from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import {
  useExportStore,
  type ExportSettings as ExportSettingsType,
} from '../../../stores/opencutExport';
import type { VideoCodec, AudioCodec, QualityPreset } from '../../../types/opencut';

export interface ExportSettingsProps {
  className?: string;
  onSaveAsPreset?: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: 600,
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalXS,
  },
  fieldRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  fieldLabel: {
    flex: '0 0 120px',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  fieldValue: {
    flex: 1,
  },
  resolutionInputs: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  resolutionInput: {
    width: '80px',
  },
  resolutionSeparator: {
    color: tokens.colorNeutralForeground3,
  },
  sliderContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flex: 1,
  },
  sliderValue: {
    minWidth: '60px',
    textAlign: 'right',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  switchRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  saveButton: {
    marginTop: tokens.spacingVerticalM,
  },
});

const VIDEO_CODECS: { value: VideoCodec; label: string }[] = [
  { value: 'h264', label: 'H.264 (AVC)' },
  { value: 'h265', label: 'H.265 (HEVC)' },
  { value: 'vp9', label: 'VP9' },
  { value: 'av1', label: 'AV1' },
  { value: 'prores', label: 'ProRes' },
];

const AUDIO_CODECS: { value: AudioCodec; label: string }[] = [
  { value: 'aac', label: 'AAC' },
  { value: 'mp3', label: 'MP3' },
  { value: 'opus', label: 'Opus' },
  { value: 'flac', label: 'FLAC' },
  { value: 'pcm', label: 'PCM (Uncompressed)' },
];

const QUALITY_PRESETS: { value: QualityPreset; label: string }[] = [
  { value: 'draft', label: 'Draft' },
  { value: 'low', label: 'Low' },
  { value: 'medium', label: 'Medium' },
  { value: 'high', label: 'High' },
  { value: 'ultra', label: 'Ultra' },
  { value: 'custom', label: 'Custom' },
];

const FRAME_RATES = [24, 25, 30, 48, 50, 60];

const FORMATS = [
  { value: 'mp4', label: 'MP4' },
  { value: 'webm', label: 'WebM' },
  { value: 'mov', label: 'MOV' },
  { value: 'mkv', label: 'MKV' },
  { value: 'gif', label: 'GIF' },
];

export const ExportSettingsPanel: FC<ExportSettingsProps> = ({ className, onSaveAsPreset }) => {
  const styles = useStyles();
  const { currentSettings, updateCurrentSetting } = useExportStore();

  const handleResolutionChange = useCallback(
    (dimension: 'width' | 'height', value: number | null) => {
      if (value === null || !currentSettings) return;
      updateCurrentSetting('resolution', {
        ...currentSettings.resolution,
        [dimension]: Math.max(1, value),
      });
    },
    [currentSettings, updateCurrentSetting]
  );

  if (!currentSettings) {
    return (
      <div className={mergeClasses(styles.container, className)}>
        <Text>No settings available. Please select a preset first.</Text>
      </div>
    );
  }

  return (
    <div className={mergeClasses(styles.container, className)}>
      {/* Video Settings */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Video Settings</Text>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Format</Text>
          <div className={styles.fieldValue}>
            <Select
              value={currentSettings.format}
              onChange={(_, data) =>
                updateCurrentSetting('format', data.value as ExportSettingsType['format'])
              }
            >
              {FORMATS.map((f) => (
                <option key={f.value} value={f.value}>
                  {f.label}
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Codec</Text>
          <div className={styles.fieldValue}>
            <Select
              value={currentSettings.videoCodec}
              onChange={(_, data) => updateCurrentSetting('videoCodec', data.value as VideoCodec)}
            >
              {VIDEO_CODECS.map((c) => (
                <option key={c.value} value={c.value}>
                  {c.label}
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Resolution</Text>
          <div className={styles.resolutionInputs}>
            <SpinButton
              className={styles.resolutionInput}
              value={currentSettings.resolution.width}
              onChange={(_, data) => handleResolutionChange('width', data.value ?? null)}
              min={1}
              max={7680}
              step={1}
            />
            <Text className={styles.resolutionSeparator}>×</Text>
            <SpinButton
              className={styles.resolutionInput}
              value={currentSettings.resolution.height}
              onChange={(_, data) => handleResolutionChange('height', data.value ?? null)}
              min={1}
              max={4320}
              step={1}
            />
          </div>
        </div>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Frame Rate</Text>
          <div className={styles.fieldValue}>
            <Select
              value={String(currentSettings.frameRate)}
              onChange={(_, data) => updateCurrentSetting('frameRate', parseInt(data.value, 10))}
            >
              {FRAME_RATES.map((rate) => (
                <option key={rate} value={rate}>
                  {rate} fps
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Video Bitrate</Text>
          <div className={styles.sliderContainer}>
            <Slider
              min={1000}
              max={50000}
              step={500}
              value={currentSettings.videoBitrate}
              onChange={(_, data) => updateCurrentSetting('videoBitrate', data.value)}
            />
            <Text className={styles.sliderValue}>
              {(currentSettings.videoBitrate / 1000).toFixed(1)} Mbps
            </Text>
          </div>
        </div>

        <div className={styles.fieldRow}>
          <Text className={styles.fieldLabel}>Quality</Text>
          <div className={styles.fieldValue}>
            <Select
              value={currentSettings.qualityPreset}
              onChange={(_, data) =>
                updateCurrentSetting('qualityPreset', data.value as QualityPreset)
              }
            >
              {QUALITY_PRESETS.map((q) => (
                <option key={q.value} value={q.value}>
                  {q.label}
                </option>
              ))}
            </Select>
          </div>
        </div>
      </div>

      {/* Audio Settings */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Audio Settings</Text>

        <div className={styles.switchRow}>
          <Text className={styles.fieldLabel}>Include Audio</Text>
          <Switch
            checked={currentSettings.includeAudio}
            onChange={(_, data) => updateCurrentSetting('includeAudio', data.checked)}
          />
        </div>

        {currentSettings.includeAudio && (
          <>
            <div className={styles.fieldRow}>
              <Text className={styles.fieldLabel}>Audio Codec</Text>
              <div className={styles.fieldValue}>
                <Select
                  value={currentSettings.audioCodec}
                  onChange={(_, data) =>
                    updateCurrentSetting('audioCodec', data.value as AudioCodec)
                  }
                >
                  {AUDIO_CODECS.map((c) => (
                    <option key={c.value} value={c.value}>
                      {c.label}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            <div className={styles.fieldRow}>
              <Text className={styles.fieldLabel}>Audio Bitrate</Text>
              <div className={styles.sliderContainer}>
                <Slider
                  min={64}
                  max={320}
                  step={32}
                  value={currentSettings.audioBitrate}
                  onChange={(_, data) => updateCurrentSetting('audioBitrate', data.value)}
                />
                <Text className={styles.sliderValue}>{currentSettings.audioBitrate} kbps</Text>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Advanced Settings */}
      <div className={styles.section}>
        <Text className={styles.sectionTitle}>Advanced</Text>

        <div className={styles.switchRow}>
          <Text className={styles.fieldLabel}>Hardware Acceleration</Text>
          <Switch
            checked={currentSettings.useHardwareAcceleration}
            onChange={(_, data) => updateCurrentSetting('useHardwareAcceleration', data.checked)}
          />
        </div>

        <div className={styles.switchRow}>
          <Text className={styles.fieldLabel}>Two-Pass Encoding</Text>
          <Switch
            checked={currentSettings.twoPass}
            onChange={(_, data) => updateCurrentSetting('twoPass', data.checked)}
          />
        </div>

        <div className={styles.switchRow}>
          <Text className={styles.fieldLabel}>Burn Captions</Text>
          <Switch
            checked={currentSettings.burnCaptions}
            onChange={(_, data) => updateCurrentSetting('burnCaptions', data.checked)}
          />
        </div>
      </div>

      {/* Save as Preset Button */}
      {onSaveAsPreset && (
        <Button
          className={styles.saveButton}
          appearance="secondary"
          icon={<Save24Regular />}
          onClick={onSaveAsPreset}
        >
          Save as Custom Preset
        </Button>
      )}
    </div>
  );
};

export default ExportSettingsPanel;
