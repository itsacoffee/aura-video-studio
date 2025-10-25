import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Field,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import type { VideoDefaultsSettings } from '../../types/settings';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalL,
  },
  twoColumn: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
  },
});

interface VideoDefaultsSettingsTabProps {
  settings: VideoDefaultsSettings;
  onChange: (settings: VideoDefaultsSettings) => void;
  onSave: () => void;
  hasChanges: boolean;
}

export function VideoDefaultsSettingsTab({
  settings,
  onChange,
  onSave,
  hasChanges,
}: VideoDefaultsSettingsTabProps) {
  const styles = useStyles();

  const updateSetting = <K extends keyof VideoDefaultsSettings>(
    key: K,
    value: VideoDefaultsSettings[K]
  ) => {
    onChange({ ...settings, [key]: value });
  };

  return (
    <Card className={styles.section}>
      <Title2>Video Defaults</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Default settings for new video projects and rendering
      </Text>

      <div className={styles.infoBox}>
        <Text weight="semibold" size={300}>
          ℹ️ About Video Defaults
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          These settings are applied to new projects. You can override them for individual projects
          during video generation.
        </Text>
      </div>

      <div className={styles.form}>
        <div className={styles.twoColumn}>
          <Field label="Default Resolution" hint="Video dimensions (width x height)">
            <Dropdown
              value={settings.defaultResolution}
              onOptionSelect={(_, data) =>
                updateSetting('defaultResolution', data.optionValue as string)
              }
            >
              <Option value="1920x1080">1920x1080 (Full HD)</Option>
              <Option value="1280x720">1280x720 (HD)</Option>
              <Option value="3840x2160">3840x2160 (4K)</Option>
              <Option value="2560x1440">2560x1440 (QHD)</Option>
              <Option value="1080x1920">1080x1920 (Vertical/Portrait)</Option>
              <Option value="1080x1080">1080x1080 (Square)</Option>
            </Dropdown>
          </Field>

          <Field label="Default Frame Rate" hint="Frames per second">
            <Dropdown
              value={settings.defaultFrameRate.toString()}
              onOptionSelect={(_, data) =>
                updateSetting('defaultFrameRate', parseInt(data.optionValue as string))
              }
            >
              <Option value="24">24 fps (Cinematic)</Option>
              <Option value="30">30 fps (Standard)</Option>
              <Option value="60">60 fps (Smooth)</Option>
              <Option value="25">25 fps (PAL)</Option>
              <Option value="29.97">29.97 fps (NTSC)</Option>
            </Dropdown>
          </Field>
        </div>

        <div className={styles.twoColumn}>
          <Field label="Default Video Codec" hint="Compression format for video">
            <Dropdown
              value={settings.defaultCodec}
              onOptionSelect={(_, data) =>
                updateSetting('defaultCodec', data.optionValue as string)
              }
            >
              <Option value="libx264">H.264 (libx264) - Best compatibility</Option>
              <Option value="libx265">H.265 (libx265) - Better compression</Option>
              <Option value="vp9">VP9 - Open source, web-friendly</Option>
              <Option value="av1">AV1 - Next-gen, high efficiency</Option>
            </Dropdown>
          </Field>

          <Field label="Default Video Bitrate" hint="Quality/file size tradeoff">
            <Dropdown
              value={settings.defaultBitrate}
              onOptionSelect={(_, data) =>
                updateSetting('defaultBitrate', data.optionValue as string)
              }
            >
              <Option value="2M">2 Mbps (Low quality)</Option>
              <Option value="5M">5 Mbps (Standard quality)</Option>
              <Option value="8M">8 Mbps (High quality)</Option>
              <Option value="12M">12 Mbps (Very high quality)</Option>
              <Option value="20M">20 Mbps (Premium quality)</Option>
            </Dropdown>
          </Field>
        </div>

        <Title2 style={{ marginTop: tokens.spacingVerticalL }}>Audio Defaults</Title2>

        <div className={styles.twoColumn}>
          <Field label="Audio Codec" hint="Compression format for audio">
            <Dropdown
              value={settings.defaultAudioCodec}
              onOptionSelect={(_, data) =>
                updateSetting('defaultAudioCodec', data.optionValue as string)
              }
            >
              <Option value="aac">AAC - Best compatibility</Option>
              <Option value="mp3">MP3 - Universal support</Option>
              <Option value="opus">Opus - High quality, efficient</Option>
              <Option value="vorbis">Vorbis - Open source</Option>
            </Dropdown>
          </Field>

          <Field label="Audio Bitrate" hint="Audio quality">
            <Dropdown
              value={settings.defaultAudioBitrate}
              onOptionSelect={(_, data) =>
                updateSetting('defaultAudioBitrate', data.optionValue as string)
              }
            >
              <Option value="128k">128 kbps (Standard)</Option>
              <Option value="192k">192 kbps (High quality)</Option>
              <Option value="256k">256 kbps (Very high quality)</Option>
              <Option value="320k">320 kbps (Maximum quality)</Option>
            </Dropdown>
          </Field>
        </div>

        <Field label="Audio Sample Rate" hint="Audio sampling frequency in Hz">
          <Dropdown
            value={settings.defaultAudioSampleRate.toString()}
            onOptionSelect={(_, data) =>
              updateSetting('defaultAudioSampleRate', parseInt(data.optionValue as string))
            }
          >
            <Option value="44100">44.1 kHz (CD quality)</Option>
            <Option value="48000">48 kHz (Professional)</Option>
            <Option value="96000">96 kHz (High-res audio)</Option>
          </Dropdown>
        </Field>

        {hasChanges && (
          <div className={styles.infoBox}>
            <Text weight="semibold" style={{ color: tokens.colorPaletteYellowForeground1 }}>
              ⚠️ You have unsaved changes
            </Text>
          </div>
        )}

        <div>
          <Button appearance="primary" onClick={onSave} disabled={!hasChanges}>
            Save Video Defaults
          </Button>
        </div>
      </div>
    </Card>
  );
}
