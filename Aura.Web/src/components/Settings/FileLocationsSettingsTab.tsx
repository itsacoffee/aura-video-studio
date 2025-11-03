import { makeStyles, tokens, Title2, Text, Button, Card } from '@fluentui/react-components';
import type { FileLocationsSettings } from '../../types/settings';
import { PathSelector } from '../common/PathSelector';

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
        <PathSelector
          label="FFmpeg Path"
          value={settings.ffmpegPath}
          onChange={(value) => updateSetting('ffmpegPath', value)}
          type="file"
          fileTypes=".exe,.bat"
          placeholder="Leave empty to use system PATH or portable installation"
          helpText="Path to ffmpeg executable (leave empty to use system PATH or portable installation)"
          examplePath="C:\ffmpeg\bin\ffmpeg.exe"
          showOpenFolder={true}
          showClearButton={true}
          onValidate={async (path) => {
            const result = await onValidatePath(path);
            return { isValid: result.valid, message: result.message };
          }}
        />

        <PathSelector
          label="FFprobe Path"
          value={settings.ffprobePath}
          onChange={(value) => updateSetting('ffprobePath', value)}
          type="file"
          fileTypes=".exe,.bat"
          placeholder="Leave empty to use system PATH or portable installation"
          helpText="Path to ffprobe executable (usually in same folder as FFmpeg)"
          examplePath="C:\ffmpeg\bin\ffprobe.exe"
          showOpenFolder={true}
          showClearButton={true}
        />

        <PathSelector
          label="Output Directory"
          value={settings.outputDirectory}
          onChange={(value) => updateSetting('outputDirectory', value)}
          type="directory"
          placeholder="Leave empty to use default location"
          helpText="Default directory for rendered videos (leave empty for Documents\AuraVideoStudio)"
          examplePath="C:\Users\YourName\Videos\AuraOutput"
          showOpenFolder={true}
          showClearButton={true}
        />

        <PathSelector
          label="Temporary Directory"
          value={settings.tempDirectory}
          onChange={(value) => updateSetting('tempDirectory', value)}
          type="directory"
          placeholder="Leave empty to use system temp folder"
          helpText="Directory for temporary files during video generation"
          examplePath="C:\Temp\AuraVideoStudio"
          showOpenFolder={true}
          showClearButton={true}
        />

        <PathSelector
          label="Media Library Location"
          value={settings.mediaLibraryLocation}
          onChange={(value) => updateSetting('mediaLibraryLocation', value)}
          type="directory"
          placeholder="Leave empty to use default location"
          helpText="Directory where media assets are stored"
          examplePath="C:\Users\YourName\Documents\AuraVideoStudio\Media"
          showOpenFolder={true}
          showClearButton={true}
        />

        <PathSelector
          label="Projects Directory"
          value={settings.projectsDirectory}
          onChange={(value) => updateSetting('projectsDirectory', value)}
          type="directory"
          placeholder="Leave empty to use default location"
          helpText="Directory where project files are saved"
          examplePath="C:\Users\YourName\Documents\AuraVideoStudio\Projects"
          showOpenFolder={true}
          showClearButton={true}
        />

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
