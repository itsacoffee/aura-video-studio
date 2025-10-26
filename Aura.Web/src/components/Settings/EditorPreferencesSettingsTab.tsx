import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Input,
  Switch,
  Card,
  Field,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import type { EditorPreferencesSettings } from '../../types/settings';

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

interface EditorPreferencesSettingsTabProps {
  settings: EditorPreferencesSettings;
  onChange: (settings: EditorPreferencesSettings) => void;
  onSave: () => void;
  hasChanges: boolean;
}

export function EditorPreferencesSettingsTab({
  settings,
  onChange,
  onSave,
  hasChanges,
}: EditorPreferencesSettingsTabProps) {
  const styles = useStyles();

  const updateSetting = <K extends keyof EditorPreferencesSettings>(
    key: K,
    value: EditorPreferencesSettings[K]
  ) => {
    onChange({ ...settings, [key]: value });
  };

  return (
    <Card className={styles.section}>
      <Title2>Editor Preferences</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Customize timeline editor behavior and visual preferences
      </Text>

      <div className={styles.form}>
        <Title2>Timeline Settings</Title2>

        <Field label="Timeline Snap">
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
            <Switch
              checked={settings.timelineSnapEnabled}
              onChange={(_, data) => updateSetting('timelineSnapEnabled', data.checked)}
              label={
                settings.timelineSnapEnabled ? 'Snap to grid enabled' : 'Snap to grid disabled'
              }
            />
            {settings.timelineSnapEnabled && (
              <Field
                label={`Snap Interval: ${settings.timelineSnapInterval} seconds`}
                hint="Grid interval for snapping timeline elements"
              >
                <Input
                  type="number"
                  value={settings.timelineSnapInterval.toString()}
                  onChange={(e) =>
                    updateSetting('timelineSnapInterval', parseFloat(e.target.value) || 1.0)
                  }
                  min="0.1"
                  max="10"
                  step="0.5"
                />
              </Field>
            )}
          </div>
        </Field>

        <Field label="Show Waveforms">
          <Switch
            checked={settings.showWaveforms}
            onChange={(_, data) => updateSetting('showWaveforms', data.checked)}
            label={
              settings.showWaveforms
                ? 'Display audio waveforms on timeline'
                : 'Hide audio waveforms'
            }
          />
        </Field>

        <Field label="Show Timecode">
          <Switch
            checked={settings.showTimecode}
            onChange={(_, data) => updateSetting('showTimecode', data.checked)}
            label={settings.showTimecode ? 'Display timecode on timeline' : 'Hide timecode display'}
          />
        </Field>

        <Title2 style={{ marginTop: tokens.spacingVerticalL }}>Playback & Preview</Title2>

        <Field label="Playback Quality" hint="Quality of video preview during editing">
          <Dropdown
            value={settings.playbackQuality}
            onOptionSelect={(_, data) =>
              updateSetting('playbackQuality', data.optionValue as string)
            }
          >
            <Option value="low">Low (faster performance)</Option>
            <Option value="medium">Medium (balanced)</Option>
            <Option value="high">High (better quality)</Option>
            <Option value="full">Full (native quality)</Option>
          </Dropdown>
        </Field>

        <Title2 style={{ marginTop: tokens.spacingVerticalL }}>Thumbnail Generation</Title2>

        <Field label="Generate Thumbnails">
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
            <Switch
              checked={settings.generateThumbnails}
              onChange={(_, data) => updateSetting('generateThumbnails', data.checked)}
              label={
                settings.generateThumbnails
                  ? 'Auto-generate thumbnails for video clips'
                  : 'Thumbnail generation disabled'
              }
            />
            {settings.generateThumbnails && (
              <Field
                label={`Thumbnail Interval: ${settings.thumbnailInterval} seconds`}
                hint="How often to generate thumbnails for scrubbing"
              >
                <Input
                  type="number"
                  value={settings.thumbnailInterval.toString()}
                  onChange={(e) =>
                    updateSetting('thumbnailInterval', parseInt(e.target.value) || 5)
                  }
                  min="1"
                  max="60"
                  step="1"
                />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Lower values = smoother scrubbing, but more storage needed
                </Text>
              </Field>
            )}
          </div>
        </Field>

        <Title2 style={{ marginTop: tokens.spacingVerticalL }}>Keyboard Shortcuts</Title2>

        <div className={styles.infoBox}>
          <Text weight="semibold" size={300}>
            ⌨️ Keyboard Shortcuts
          </Text>
          <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
            For detailed keyboard shortcut customization, visit the dedicated Shortcuts tab.
          </Text>
        </div>

        {hasChanges && (
          <div className={styles.infoBox}>
            <Text weight="semibold" style={{ color: tokens.colorPaletteYellowForeground1 }}>
              ⚠️ You have unsaved changes
            </Text>
          </div>
        )}

        <div>
          <Button appearance="primary" onClick={onSave} disabled={!hasChanges}>
            Save Editor Preferences
          </Button>
        </div>
      </div>
    </Card>
  );
}
