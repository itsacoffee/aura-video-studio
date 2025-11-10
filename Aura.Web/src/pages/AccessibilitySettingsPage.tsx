/**
 * Accessibility Settings Page
 * 
 * Allows users to customize accessibility settings including:
 * - High contrast mode
 * - Reduced motion
 * - Font size
 * - Enhanced focus indicators
 * - Screen reader announcements
 */

import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Switch,
  Card,
  CardHeader,
  Divider,
  Radio,
  RadioGroup,
  Button,
  shorthands,
} from '@fluentui/react-components';
import {
  Eye20Regular,
  SlideText20Regular,
  TextFont20Regular,
  Alert20Regular,
  ArrowReset20Regular,
} from '@fluentui/react-icons';
import { useAccessibility } from '../contexts/AccessibilityContext';

const useStyles = makeStyles({
  container: {
    maxWidth: '800px',
    margin: '0 auto',
    ...shorthands.padding(tokens.spacingVerticalXXL),
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  card: {
    marginBottom: tokens.spacingVerticalL,
  },
  cardContent: {
    ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalL),
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  settingRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    ...shorthands.gap(tokens.spacingHorizontalL),
  },
  settingInfo: {
    flex: 1,
  },
  settingTitle: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXXS,
    display: 'flex',
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  settingDescription: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
  },
  radioGroup: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalS),
  },
  actions: {
    display: 'flex',
    justifyContent: 'flex-end',
    ...shorthands.gap(tokens.spacingHorizontalM),
    marginTop: tokens.spacingVerticalXL,
  },
  alert: {
    backgroundColor: tokens.colorPaletteYellowBackground2,
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalL),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    marginBottom: tokens.spacingVerticalL,
    display: 'flex',
    alignItems: 'flex-start',
    ...shorthands.gap(tokens.spacingHorizontalM),
  },
  alertIcon: {
    color: tokens.colorPaletteYellowForeground1,
    flexShrink: 0,
  },
});

export function AccessibilitySettingsPage() {
  const styles = useStyles();
  const { settings, updateSettings, resetToDefaults } = useAccessibility();

  const handleReset = () => {
    if (window.confirm('Reset all accessibility settings to defaults?')) {
      resetToDefaults();
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>Accessibility Settings</Title3>
        <Text>Customize your experience to meet your accessibility needs</Text>
      </div>

      <div className={styles.alert}>
        <Alert20Regular className={styles.alertIcon} />
        <div>
          <Text weight="semibold">System Preferences Detected</Text>
          <Text block>
            We've automatically detected your system's accessibility preferences. You can adjust
            these settings to further customize your experience.
          </Text>
        </div>
      </div>

      <div className={styles.section}>
        <Card className={styles.card}>
          <CardHeader
            header={<Text weight="semibold" size={500}>Visual Accessibility</Text>}
            description="Settings for improving visual clarity and contrast"
          />
          <Divider />
          <div className={styles.cardContent}>
            <div className={styles.settingRow}>
              <div className={styles.settingInfo}>
                <div className={styles.settingTitle}>
                  <Eye20Regular />
                  High Contrast Mode
                </div>
                <Text className={styles.settingDescription}>
                  Increases contrast between text and background for better readability
                </Text>
              </div>
              <Switch
                checked={settings.highContrast}
                onChange={(_, data) => updateSettings({ highContrast: data.checked })}
                aria-label="Toggle high contrast mode"
              />
            </div>

            <Divider />

            <div className={styles.settingRow}>
              <div className={styles.settingInfo}>
                <div className={styles.settingTitle}>
                  <SlideText20Regular />
                  Enhanced Focus Indicators
                </div>
                <Text className={styles.settingDescription}>
                  Makes focus indicators more visible for keyboard navigation
                </Text>
              </div>
              <Switch
                checked={settings.focusIndicatorsEnhanced}
                onChange={(_, data) => updateSettings({ focusIndicatorsEnhanced: data.checked })}
                aria-label="Toggle enhanced focus indicators"
              />
            </div>

            <Divider />

            <div className={styles.settingRow}>
              <div className={styles.settingInfo}>
                <div className={styles.settingTitle}>
                  <TextFont20Regular />
                  Font Size
                </div>
                <Text className={styles.settingDescription}>
                  Adjust the default font size throughout the application
                </Text>
              </div>
              <div>
                <RadioGroup
                  value={settings.fontSize}
                  onChange={(_, data) => updateSettings({ fontSize: data.value as typeof settings.fontSize })}
                  className={styles.radioGroup}
                  aria-label="Font size"
                >
                  <Radio value="small" label="Small" />
                  <Radio value="medium" label="Medium (Default)" />
                  <Radio value="large" label="Large" />
                  <Radio value="x-large" label="Extra Large" />
                </RadioGroup>
              </div>
            </div>
          </div>
        </Card>

        <Card className={styles.card}>
          <CardHeader
            header={<Text weight="semibold" size={500}>Motion & Animation</Text>}
            description="Control animations and transitions"
          />
          <Divider />
          <div className={styles.cardContent}>
            <div className={styles.settingRow}>
              <div className={styles.settingInfo}>
                <div className={styles.settingTitle}>Reduced Motion</div>
                <Text className={styles.settingDescription}>
                  Minimizes animations and transitions that may cause discomfort or distraction
                </Text>
              </div>
              <Switch
                checked={settings.reducedMotion}
                onChange={(_, data) => updateSettings({ reducedMotion: data.checked })}
                aria-label="Toggle reduced motion"
              />
            </div>
          </div>
        </Card>

        <Card className={styles.card}>
          <CardHeader
            header={<Text weight="semibold" size={500}>Screen Reader</Text>}
            description="Settings for screen reader users"
          />
          <Divider />
          <div className={styles.cardContent}>
            <div className={styles.settingRow}>
              <div className={styles.settingInfo}>
                <div className={styles.settingTitle}>Screen Reader Announcements</div>
                <Text className={styles.settingDescription}>
                  Enable announcements for status changes, errors, and important updates
                </Text>
              </div>
              <Switch
                checked={settings.screenReaderAnnouncements}
                onChange={(_, data) => updateSettings({ screenReaderAnnouncements: data.checked })}
                aria-label="Toggle screen reader announcements"
              />
            </div>
          </div>
        </Card>
      </div>

      <div className={styles.actions}>
        <Button
          appearance="secondary"
          icon={<ArrowReset20Regular />}
          onClick={handleReset}
        >
          Reset to Defaults
        </Button>
      </div>

      <Divider style={{ marginTop: tokens.spacingVerticalXL, marginBottom: tokens.spacingVerticalXL }} />

      <div>
        <Text size={300} weight="semibold">Keyboard Shortcuts</Text>
        <Text block style={{ marginTop: tokens.spacingVerticalS, color: tokens.colorNeutralForeground3 }}>
          Press <strong>Ctrl+/</strong> (or <strong>Cmd+/</strong> on Mac) to view all available
          keyboard shortcuts. You can customize keyboard shortcuts in the Settings page.
        </Text>
      </div>
    </div>
  );
}
