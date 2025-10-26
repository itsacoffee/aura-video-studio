import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Input,
  Button,
  Slider,
  Radio,
  RadioGroup,
  Field,
} from '@fluentui/react-components';
import { Folder24Regular, FolderOpen24Regular, Save24Regular } from '@fluentui/react-icons';
import { useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  settingsGrid: {
    display: 'grid',
    gridTemplateColumns: '1fr',
    gap: tokens.spacingVerticalL,
  },
  settingCard: {
    padding: tokens.spacingVerticalL,
  },
  settingHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  settingIcon: {
    fontSize: '32px',
    color: tokens.colorBrandBackground,
  },
  pathInput: {
    flex: 1,
  },
  pathInputContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-end',
  },
  sliderContainer: {
    marginTop: tokens.spacingVerticalM,
  },
  sliderValue: {
    display: 'flex',
    justifyContent: 'space-between',
    marginTop: tokens.spacingVerticalS,
  },
  themeOptions: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    marginTop: tokens.spacingVerticalM,
  },
  themeCard: {
    flex: 1,
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    cursor: 'pointer',
    transition: 'all 0.2s ease-in-out',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  selectedTheme: {
    outline: `2px solid ${tokens.colorBrandBackground}`,
    outlineOffset: '-2px',
  },
  themeIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalS,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    marginTop: tokens.spacingVerticalL,
  },
  summaryList: {
    listStyleType: 'none',
    padding: 0,
    margin: `${tokens.spacingVerticalM} 0`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  summaryItem: {
    display: 'flex',
    justifyContent: 'space-between',
  },
});

export interface WorkspacePreferences {
  defaultSaveLocation: string;
  cacheLocation: string;
  autosaveInterval: number; // in minutes
  theme: 'light' | 'dark' | 'auto';
}

export interface WorkspaceSetupProps {
  preferences: WorkspacePreferences;
  onPreferencesChange: (preferences: WorkspacePreferences) => void;
  onBrowseFolder: (type: 'save' | 'cache') => Promise<string | null>;
}

export function WorkspaceSetup({
  preferences,
  onPreferencesChange,
  onBrowseFolder,
}: WorkspaceSetupProps) {
  const styles = useStyles();
  const [isBrowsing, setIsBrowsing] = useState<'save' | 'cache' | null>(null);

  const handleBrowse = async (type: 'save' | 'cache') => {
    setIsBrowsing(type);
    try {
      const path = await onBrowseFolder(type);
      if (path) {
        onPreferencesChange({
          ...preferences,
          [type === 'save' ? 'defaultSaveLocation' : 'cacheLocation']: path,
        });
      }
    } finally {
      setIsBrowsing(null);
    }
  };

  const handleAutosaveChange = (value: number) => {
    onPreferencesChange({
      ...preferences,
      autosaveInterval: value,
    });
  };

  const handleThemeChange = (theme: 'light' | 'dark' | 'auto') => {
    onPreferencesChange({
      ...preferences,
      theme,
    });
  };

  const autosaveMinutes = preferences.autosaveInterval;
  const autosaveText =
    autosaveMinutes === 0
      ? 'Disabled'
      : `Every ${autosaveMinutes} minute${autosaveMinutes > 1 ? 's' : ''}`;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Workspace Preferences</Title2>
        <Text>
          Configure your workspace settings to match your workflow. You can change these later in
          Settings.
        </Text>
      </div>

      <div className={styles.settingsGrid}>
        {/* Default Save Location */}
        <Card className={styles.settingCard}>
          <div className={styles.settingHeader}>
            <Folder24Regular className={styles.settingIcon} />
            <div>
              <Title3>Default Save Location</Title3>
              <Text size={200}>Where your video projects will be saved by default</Text>
            </div>
          </div>

          <Field>
            <div className={styles.pathInputContainer}>
              <Input
                className={styles.pathInput}
                value={preferences.defaultSaveLocation}
                onChange={(e) =>
                  onPreferencesChange({
                    ...preferences,
                    defaultSaveLocation: e.target.value,
                  })
                }
                placeholder="C:\Users\YourName\Videos\Aura"
              />
              <Button
                appearance="secondary"
                icon={<FolderOpen24Regular />}
                onClick={() => handleBrowse('save')}
                disabled={isBrowsing !== null}
              >
                {isBrowsing === 'save' ? 'Browsing...' : 'Browse'}
              </Button>
            </div>
          </Field>
        </Card>

        {/* Cache Location */}
        <Card className={styles.settingCard}>
          <div className={styles.settingHeader}>
            <Save24Regular className={styles.settingIcon} />
            <div>
              <Title3>Cache & Temp Files Location</Title3>
              <Text size={200}>Where temporary files and cache will be stored</Text>
            </div>
          </div>

          <Field>
            <div className={styles.pathInputContainer}>
              <Input
                className={styles.pathInput}
                value={preferences.cacheLocation}
                onChange={(e) =>
                  onPreferencesChange({
                    ...preferences,
                    cacheLocation: e.target.value,
                  })
                }
                placeholder="C:\Users\YourName\AppData\Local\Aura\Cache"
              />
              <Button
                appearance="secondary"
                icon={<FolderOpen24Regular />}
                onClick={() => handleBrowse('cache')}
                disabled={isBrowsing !== null}
              >
                {isBrowsing === 'cache' ? 'Browsing...' : 'Browse'}
              </Button>
            </div>
          </Field>

          <div className={styles.infoCard} style={{ marginTop: tokens.spacingVerticalM }}>
            <Text size={200}>
              💡 Cache files help speed up your workflow but can use significant disk space. Choose
              a location with ample free space.
            </Text>
          </div>
        </Card>

        {/* Autosave Interval */}
        <Card className={styles.settingCard}>
          <div className={styles.settingHeader}>
            <div style={{ fontSize: '32px' }}>💾</div>
            <div>
              <Title3>Autosave Interval</Title3>
              <Text size={200}>How often to automatically save your work</Text>
            </div>
          </div>

          <div className={styles.sliderContainer}>
            <Slider
              min={0}
              max={10}
              step={1}
              value={autosaveMinutes}
              onChange={(_, data) => handleAutosaveChange(data.value)}
            />
            <div className={styles.sliderValue}>
              <Text size={200}>Disabled</Text>
              <Text weight="semibold">{autosaveText}</Text>
              <Text size={200}>10 minutes</Text>
            </div>
          </div>

          <div className={styles.infoCard} style={{ marginTop: tokens.spacingVerticalM }}>
            <Text size={200}>
              💡 Recommended: 2-5 minutes for the best balance between performance and data safety.
              Set to 0 to disable autosave.
            </Text>
          </div>
        </Card>

        {/* Theme Preference */}
        <Card className={styles.settingCard}>
          <div className={styles.settingHeader}>
            <div style={{ fontSize: '32px' }}>🎨</div>
            <div>
              <Title3>Theme Preference</Title3>
              <Text size={200}>Choose your preferred color scheme</Text>
            </div>
          </div>

          <RadioGroup
            value={preferences.theme}
            onChange={(_, data) => handleThemeChange(data.value as 'light' | 'dark' | 'auto')}
          >
            <div className={styles.themeOptions}>
              <Card
                className={`${styles.themeCard} ${preferences.theme === 'light' ? styles.selectedTheme : ''}`}
                onClick={() => handleThemeChange('light')}
              >
                <div className={styles.themeIcon}>☀️</div>
                <Radio value="light" label="Light" />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Bright and clean
                </Text>
              </Card>

              <Card
                className={`${styles.themeCard} ${preferences.theme === 'dark' ? styles.selectedTheme : ''}`}
                onClick={() => handleThemeChange('dark')}
              >
                <div className={styles.themeIcon}>🌙</div>
                <Radio value="dark" label="Dark" />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Easy on the eyes
                </Text>
              </Card>

              <Card
                className={`${styles.themeCard} ${preferences.theme === 'auto' ? styles.selectedTheme : ''}`}
                onClick={() => handleThemeChange('auto')}
              >
                <div className={styles.themeIcon}>🔄</div>
                <Radio value="auto" label="Auto" />
                <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
                  Follows system
                </Text>
              </Card>
            </div>
          </RadioGroup>
        </Card>
      </div>

      {/* Summary */}
      <Card className={styles.summaryCard}>
        <Title3>Configuration Summary</Title3>
        <ul className={styles.summaryList}>
          <li className={styles.summaryItem}>
            <Text weight="semibold">Projects saved to:</Text>
            <Text size={200}>{preferences.defaultSaveLocation || 'Not set'}</Text>
          </li>
          <li className={styles.summaryItem}>
            <Text weight="semibold">Cache location:</Text>
            <Text size={200}>{preferences.cacheLocation || 'Not set'}</Text>
          </li>
          <li className={styles.summaryItem}>
            <Text weight="semibold">Autosave:</Text>
            <Text size={200}>{autosaveText}</Text>
          </li>
          <li className={styles.summaryItem}>
            <Text weight="semibold">Theme:</Text>
            <Text size={200}>
              {preferences.theme.charAt(0).toUpperCase() + preferences.theme.slice(1)}
            </Text>
          </li>
        </ul>
      </Card>
    </div>
  );
}
