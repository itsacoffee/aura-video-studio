import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Input,
  Card,
  Field,
} from '@fluentui/react-components';
import { Folder24Regular } from '@fluentui/react-icons';
import type { FileLocationsSettings } from '../../types/settings';

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
  inputWithButton: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-start',
  },
});

interface FileLocationsSettingsTabProps {
  settings: FileLocationsSettings;
  onChange: (settings: FileLocationsSettings) => void;
  onSave: () => void;
  onValidatePath: (path: string) => Promise<{ valid: boolean; message: string }>;
  hasChanges: boolean;
}

export function FileLocationsSettingsTab({
  settings,
  onChange,
  onSave,
  onValidatePath,
  hasChanges,
}: FileLocationsSettingsTabProps) {
  const styles = useStyles();

  const updateSetting = <K extends keyof FileLocationsSettings>(
    key: K,
    value: FileLocationsSettings[K]
  ) => {
    onChange({ ...settings, [key]: value });
  };

  const handleBrowse = async (_key: keyof FileLocationsSettings) => {
    // Note: File browser dialogs would need to be implemented via backend API
    // For now, this is a placeholder
    alert('File browser dialog would open here. Please enter path manually for now.');
  };

  const handleValidate = async (path: string, label: string) => {
    if (!path) {
      alert(`${label} is empty`);
      return;
    }
    const result = await onValidatePath(path);
    alert(result.message);
  };

  return (
    <Card className={styles.section}>
      <Title2>File Locations</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Configure paths for tools and directories used by the application
      </Text>

      <div className={styles.infoBox}>
        <Text weight="semibold" size={300}>
          💡 Tip
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          Leave paths empty to use system defaults or portable installation paths. Visit the
          Downloads page to automatically install and configure tools like FFmpeg.
        </Text>
      </div>

      <div className={styles.form}>
        <Field
          label="FFmpeg Path"
          hint="Path to ffmpeg executable (leave empty to use system PATH or portable installation)"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.ffmpegPath}
              onChange={(e) => updateSetting('ffmpegPath', e.target.value)}
              placeholder="C:\path\to\ffmpeg.exe or leave empty"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('ffmpegPath')}
              title="Browse for file"
            />
            <Button
              onClick={() => handleValidate(settings.ffmpegPath, 'FFmpeg path')}
              disabled={!settings.ffmpegPath}
            >
              Test
            </Button>
          </div>
        </Field>

        <Field
          label="FFprobe Path"
          hint="Path to ffprobe executable (usually in same folder as FFmpeg)"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.ffprobePath}
              onChange={(e) => updateSetting('ffprobePath', e.target.value)}
              placeholder="C:\path\to\ffprobe.exe or leave empty"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('ffprobePath')}
              title="Browse for file"
            />
          </div>
        </Field>

        <Field
          label="Output Directory"
          hint="Default directory for rendered videos (leave empty for Documents\AuraVideoStudio)"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.outputDirectory}
              onChange={(e) => updateSetting('outputDirectory', e.target.value)}
              placeholder="C:\Users\YourName\Videos\AuraOutput"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('outputDirectory')}
              title="Browse for folder"
            />
          </div>
        </Field>

        <Field
          label="Temporary Directory"
          hint="Directory for temporary files during video generation"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.tempDirectory}
              onChange={(e) => updateSetting('tempDirectory', e.target.value)}
              placeholder="Leave empty to use system temp folder"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('tempDirectory')}
              title="Browse for folder"
            />
          </div>
        </Field>

        <Field
          label="Media Library Location"
          hint="Directory where media assets are stored"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.mediaLibraryLocation}
              onChange={(e) => updateSetting('mediaLibraryLocation', e.target.value)}
              placeholder="Leave empty to use default location"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('mediaLibraryLocation')}
              title="Browse for folder"
            />
          </div>
        </Field>

        <Field
          label="Projects Directory"
          hint="Directory where project files are saved"
        >
          <div className={styles.inputWithButton}>
            <Input
              style={{ flex: 1 }}
              value={settings.projectsDirectory}
              onChange={(e) => updateSetting('projectsDirectory', e.target.value)}
              placeholder="Leave empty to use default location"
            />
            <Button
              icon={<Folder24Regular />}
              onClick={() => handleBrowse('projectsDirectory')}
              title="Browse for folder"
            />
          </div>
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
            Save File Locations
          </Button>
        </div>
      </div>
    </Card>
  );
}
