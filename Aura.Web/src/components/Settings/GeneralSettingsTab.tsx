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
import type { GeneralSettings, ThemeMode, StartupBehavior } from '../../types/settings';

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

interface GeneralSettingsTabProps {
  settings: GeneralSettings;
  onChange: (settings: GeneralSettings) => void;
  onSave: () => void;
  hasChanges: boolean;
}

export function GeneralSettingsTab({
  settings,
  onChange,
  onSave,
  hasChanges,
}: GeneralSettingsTabProps) {
  const styles = useStyles();

  const updateSetting = <K extends keyof GeneralSettings>(
    key: K,
    value: GeneralSettings[K]
  ) => {
    onChange({ ...settings, [key]: value });
  };

  return (
    <Card className={styles.section}>
      <Title2>General Settings</Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Configure basic application behavior and preferences
      </Text>

      <div className={styles.form}>
        <Field
          label="Default Project Save Location"
          hint="Where new projects are saved by default (leave empty for user's Documents folder)"
        >
          <Input
            value={settings.defaultProjectSaveLocation}
            onChange={(e) =>
              updateSetting('defaultProjectSaveLocation', e.target.value)
            }
            placeholder="C:\Users\YourName\Documents\AuraProjects"
          />
        </Field>

        <Field label="Autosave">
          <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalS }}>
            <Switch
              checked={settings.autosaveEnabled}
              onChange={(_, data) => updateSetting('autosaveEnabled', data.checked)}
              label={settings.autosaveEnabled ? 'Enabled' : 'Disabled'}
            />
            {settings.autosaveEnabled && (
              <Field
                label={`Autosave Interval: ${Math.floor(settings.autosaveIntervalSeconds / 60)} minutes`}
                hint="How often to automatically save your work"
              >
                <Input
                  type="number"
                  value={settings.autosaveIntervalSeconds.toString()}
                  onChange={(e) =>
                    updateSetting('autosaveIntervalSeconds', parseInt(e.target.value) || 300)
                  }
                  min="60"
                  max="3600"
                  step="60"
                />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Recommended: 300 seconds (5 minutes)
                </Text>
              </Field>
            )}
          </div>
        </Field>

        <Field label="Language & Locale" hint="Application language and regional settings">
          <Dropdown
            value={settings.language}
            onOptionSelect={(_, data) => {
              updateSetting('language', data.optionValue as string);
              updateSetting('locale', data.optionValue as string);
            }}
          >
            <Option value="en-US">English (United States)</Option>
            <Option value="en-GB">English (United Kingdom)</Option>
            <Option value="es-ES">Español (España)</Option>
            <Option value="fr-FR">Français (France)</Option>
            <Option value="de-DE">Deutsch (Deutschland)</Option>
            <Option value="ja-JP">日本語 (日本)</Option>
            <Option value="zh-CN">中文 (简体)</Option>
          </Dropdown>
        </Field>

        <Field label="Theme" hint="Choose your preferred color scheme">
          <Dropdown
            value={settings.theme}
            onOptionSelect={(_, data) => updateSetting('theme', data.optionValue as ThemeMode)}
          >
            <Option value="Light">Light</Option>
            <Option value="Dark">Dark</Option>
            <Option value="Auto">Auto (follow system)</Option>
          </Dropdown>
        </Field>

        <Field label="Startup Behavior" hint="What to show when the application starts">
          <Dropdown
            value={settings.startupBehavior}
            onOptionSelect={(_, data) =>
              updateSetting('startupBehavior', data.optionValue as StartupBehavior)
            }
          >
            <Option value="ShowDashboard">Show Dashboard</Option>
            <Option value="ShowLastProject">Open Last Project</Option>
            <Option value="ShowNewProjectDialog">New Project Dialog</Option>
          </Dropdown>
        </Field>

        <Field label="Check for Updates on Startup">
          <Switch
            checked={settings.checkForUpdatesOnStartup}
            onChange={(_, data) => updateSetting('checkForUpdatesOnStartup', data.checked)}
            label={
              settings.checkForUpdatesOnStartup
                ? 'Automatically check for updates'
                : 'Manual updates only'
            }
          />
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
            Save General Settings
          </Button>
        </div>
      </div>
    </Card>
  );
}
