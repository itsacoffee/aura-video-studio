/**
 * Graphics Settings Page
 * Premium Windows 11-style settings panel for graphics configuration
 */

import {
  makeStyles,
  tokens,
  Card,
  Title2,
  Title3,
  Body1,
  Caption1,
  Switch,
  RadioGroup,
  Radio,
  Button,
  Spinner,
  Badge,
  Text,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import {
  DesktopTower24Regular,
  Settings24Regular,
  Sparkle24Regular,
  Desktop24Regular,
  Accessibility24Regular,
  ArrowReset24Regular,
  Checkmark24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { FadeIn } from '../components/animations';
import { graphicsSettingsService } from '../services/graphicsSettingsService';
import type { GraphicsSettings, PerformanceProfile } from '../types/graphicsSettings';
import { createDefaultGraphicsSettings } from '../types/graphicsSettings';

const useStyles = makeStyles({
  container: {
    maxWidth: '900px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  card: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  sectionIcon: {
    color: tokens.colorBrandForeground1,
  },
  profileGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  profileCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    border: `2px solid transparent`,
    transition: 'all 200ms ease',
    ':hover': {
      border: `2px solid ${tokens.colorNeutralStroke1Hover}`,
      transform: 'translateY(-2px)',
    },
  },
  profileCardSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  profileTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  settingRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: `${tokens.spacingVerticalS} 0`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    ':last-child': {
      borderBottom: 'none',
    },
  },
  settingLabel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  gpuInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  gpuDetails: {
    flex: 1,
  },
  scalePreview: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  saveIndicator: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground3,
  },
});

const PROFILE_INFO: Record<
  PerformanceProfile,
  { title: string; description: string; icon: string }
> = {
  maximum: {
    title: 'Maximum Quality',
    description: 'All visual effects enabled for the best experience',
    icon: '✨',
  },
  balanced: {
    title: 'Balanced',
    description: 'Good visuals with better performance',
    icon: '⚖️',
  },
  powerSaver: {
    title: 'Power Saver',
    description: 'Minimal effects for best battery life',
    icon: '🔋',
  },
  custom: {
    title: 'Custom',
    description: 'Configure individual effects',
    icon: '⚙️',
  },
};

const SCALE_OPTIONS = [
  { value: 1.0, label: '100%' },
  { value: 1.25, label: '125%' },
  { value: 1.5, label: '150%' },
  { value: 1.75, label: '175%' },
  { value: 2.0, label: '200%' },
];

export function GraphicsSettingsPage() {
  const styles = useStyles();
  const [settings, setSettings] = useState<GraphicsSettings>(createDefaultGraphicsSettings());
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [_originalSettings, setOriginalSettings] = useState<GraphicsSettings | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    setLoading(true);
    setErrorMessage(null);
    try {
      const loaded = await graphicsSettingsService.loadSettings();
      setSettings(loaded);
      setOriginalSettings(loaded);
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to load graphics settings', errorObj);
      setErrorMessage('Failed to load graphics settings. Using default values.');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    setErrorMessage(null);
    try {
      await graphicsSettingsService.saveSettings(settings);
      setOriginalSettings(settings);
      setHasChanges(false);
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to save graphics settings', errorObj);
      setErrorMessage('Failed to save settings. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const handleProfileChange = async (profile: PerformanceProfile) => {
    setErrorMessage(null);
    try {
      const updated = await graphicsSettingsService.applyProfile(profile);
      setSettings(updated);
      setOriginalSettings(updated);
      setHasChanges(false);
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to apply profile', errorObj);
      setErrorMessage('Failed to apply profile. Please try again.');
    }
  };

  const handleReset = async () => {
    setErrorMessage(null);
    try {
      await graphicsSettingsService.resetToDefaults();
      await loadSettings();
    } catch (error: unknown) {
      const errorObj = error instanceof Error ? error : new Error(String(error));
      console.error('Failed to reset settings', errorObj);
      setErrorMessage('Failed to reset settings. Please try again.');
    }
  };

  const updateSetting = <K extends keyof GraphicsSettings>(key: K, value: GraphicsSettings[K]) => {
    setSettings((prev) => ({ ...prev, [key]: value }));
    setHasChanges(true);
  };

  const updateEffect = (key: keyof typeof settings.effects, value: boolean) => {
    setSettings((prev) => ({
      ...prev,
      profile: 'custom',
      effects: { ...prev.effects, [key]: value },
    }));
    setHasChanges(true);
  };

  const updateScaling = (key: keyof typeof settings.scaling, value: string | number | boolean) => {
    setSettings((prev) => ({
      ...prev,
      scaling: { ...prev.scaling, [key]: value },
    }));
    setHasChanges(true);
  };

  const updateAccessibility = (key: keyof typeof settings.accessibility, value: boolean) => {
    setSettings((prev) => ({
      ...prev,
      accessibility: { ...prev.accessibility, [key]: value },
    }));
    setHasChanges(true);
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading graphics settings..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <FadeIn>
        {/* Header */}
        <div className={styles.header}>
          <Title2>Graphics Settings</Title2>
          <Body1 style={{ color: tokens.colorNeutralForeground3 }}>
            Configure visual effects, GPU acceleration, and display scaling
          </Body1>
        </div>

        {/* Error Message */}
        {errorMessage && (
          <MessageBar intent="error" style={{ marginBottom: tokens.spacingVerticalM }}>
            <MessageBarBody>{errorMessage}</MessageBarBody>
          </MessageBar>
        )}

        {/* GPU Information */}
        <Card className={styles.card}>
          <div className={styles.sectionHeader}>
            <DesktopTower24Regular className={styles.sectionIcon} />
            <Title3>GPU Information</Title3>
          </div>

          <div className={styles.gpuInfo}>
            <DesktopTower24Regular
              style={{ fontSize: '32px', color: tokens.colorBrandForeground1 }}
            />
            <div className={styles.gpuDetails}>
              <Text weight="semibold">{settings.detectedGpuName || 'No GPU detected'}</Text>
              {settings.detectedVramMB > 0 && (
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                  {settings.detectedGpuVendor} • {Math.round(settings.detectedVramMB / 1024)} GB
                  VRAM
                </Caption1>
              )}
            </div>
            <Badge
              appearance="filled"
              color={settings.gpuAccelerationEnabled ? 'success' : 'warning'}
            >
              {settings.gpuAccelerationEnabled ? 'GPU Enabled' : 'CPU Mode'}
            </Badge>
          </div>

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>GPU Acceleration</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Use graphics hardware for faster rendering
              </Caption1>
            </div>
            <Switch
              checked={settings.gpuAccelerationEnabled}
              onChange={(_, data) => updateSetting('gpuAccelerationEnabled', data.checked)}
            />
          </div>
        </Card>

        {/* Performance Profiles */}
        <Card className={styles.card}>
          <div className={styles.sectionHeader}>
            <Settings24Regular className={styles.sectionIcon} />
            <Title3>Performance Profile</Title3>
          </div>

          <div className={styles.profileGrid}>
            {(
              Object.entries(PROFILE_INFO) as [
                PerformanceProfile,
                (typeof PROFILE_INFO)['maximum'],
              ][]
            ).map(([profile, info]) => (
              <Card
                key={profile}
                className={`${styles.profileCard} ${
                  settings.profile === profile ? styles.profileCardSelected : ''
                }`}
                onClick={() => handleProfileChange(profile)}
              >
                <div className={styles.profileTitle}>
                  <span>{info.icon}</span>
                  <Text weight="semibold">{info.title}</Text>
                  {settings.profile === profile && (
                    <Checkmark24Regular style={{ color: tokens.colorBrandForeground1 }} />
                  )}
                </div>
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                  {info.description}
                </Caption1>
              </Card>
            ))}
          </div>
        </Card>

        {/* Visual Effects */}
        <Card className={styles.card}>
          <div className={styles.sectionHeader}>
            <Sparkle24Regular className={styles.sectionIcon} />
            <Title3>Visual Effects</Title3>
            {settings.profile !== 'custom' && (
              <Badge appearance="outline" size="small">
                Managed by profile
              </Badge>
            )}
          </div>

          {[
            {
              key: 'animations',
              label: 'Animations',
              desc: 'Enable smooth transitions and motion',
            },
            {
              key: 'blurEffects',
              label: 'Blur Effects',
              desc: 'Acrylic and frosted glass backgrounds',
            },
            { key: 'shadows', label: 'Shadows', desc: 'Soft drop shadows on elements' },
            {
              key: 'transparency',
              label: 'Transparency',
              desc: 'Semi-transparent panels and overlays',
            },
            {
              key: 'smoothScrolling',
              label: 'Smooth Scrolling',
              desc: 'Inertial scrolling with momentum',
            },
            {
              key: 'springPhysics',
              label: 'Spring Physics',
              desc: 'Natural, physics-based animations',
            },
            {
              key: 'parallaxEffects',
              label: 'Parallax Effects',
              desc: 'Depth effects on scroll and hover',
            },
            {
              key: 'glowEffects',
              label: 'Glow Effects',
              desc: 'Subtle glows on focus and active states',
            },
            {
              key: 'microInteractions',
              label: 'Micro-interactions',
              desc: 'Small feedback animations',
            },
            {
              key: 'staggeredAnimations',
              label: 'Staggered Animations',
              desc: 'Sequential list animations',
            },
          ].map(({ key, label, desc }) => (
            <div key={key} className={styles.settingRow}>
              <div className={styles.settingLabel}>
                <Body1>{label}</Body1>
                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>{desc}</Caption1>
              </div>
              <Switch
                checked={settings.effects[key as keyof typeof settings.effects]}
                onChange={(_, data) =>
                  updateEffect(key as keyof typeof settings.effects, data.checked)
                }
              />
            </div>
          ))}
        </Card>

        {/* Display Scaling */}
        <Card className={styles.card}>
          <div className={styles.sectionHeader}>
            <Desktop24Regular className={styles.sectionIcon} />
            <Title3>Display Scaling</Title3>
          </div>

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>Scaling Mode</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                How the application scales on your display
              </Caption1>
            </div>
            <RadioGroup
              value={settings.scaling.mode}
              onChange={(_, data) => updateScaling('mode', data.value)}
              layout="horizontal"
            >
              <Radio value="system" label="Use system setting" />
              <Radio value="manual" label="Custom scale" />
            </RadioGroup>
          </div>

          {settings.scaling.mode === 'manual' && (
            <div className={styles.settingRow}>
              <div className={styles.settingLabel}>
                <Body1>Scale Factor</Body1>
                <div className={styles.scalePreview}>
                  {SCALE_OPTIONS.map((opt) => (
                    <Button
                      key={opt.value}
                      appearance={
                        settings.scaling.manualScaleFactor === opt.value ? 'primary' : 'subtle'
                      }
                      size="small"
                      onClick={() => updateScaling('manualScaleFactor', opt.value)}
                    >
                      {opt.label}
                    </Button>
                  ))}
                </div>
              </div>
            </div>
          )}

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>Per-Monitor DPI</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Use different scales for each monitor
              </Caption1>
            </div>
            <Switch
              checked={settings.scaling.perMonitorDpiAware}
              onChange={(_, data) => updateScaling('perMonitorDpiAware', data.checked)}
            />
          </div>
        </Card>

        {/* Accessibility */}
        <Card className={styles.card}>
          <div className={styles.sectionHeader}>
            <Accessibility24Regular className={styles.sectionIcon} />
            <Title3>Accessibility</Title3>
          </div>

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>Reduce Motion</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Minimize animations for motion sensitivity
              </Caption1>
            </div>
            <Switch
              checked={settings.accessibility.reducedMotion}
              onChange={(_, data) => updateAccessibility('reducedMotion', data.checked)}
            />
          </div>

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>High Contrast</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Increase contrast for better visibility
              </Caption1>
            </div>
            <Switch
              checked={settings.accessibility.highContrast}
              onChange={(_, data) => updateAccessibility('highContrast', data.checked)}
            />
          </div>

          <div className={styles.settingRow}>
            <div className={styles.settingLabel}>
              <Body1>Focus Indicators</Body1>
              <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                Show visible focus outlines for keyboard navigation
              </Caption1>
            </div>
            <Switch
              checked={settings.accessibility.focusIndicators}
              onChange={(_, data) => updateAccessibility('focusIndicators', data.checked)}
            />
          </div>
        </Card>

        {/* Actions */}
        <div className={styles.actions}>
          <Button appearance="subtle" icon={<ArrowReset24Regular />} onClick={handleReset}>
            Reset to Defaults
          </Button>

          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            {hasChanges && (
              <div className={styles.saveIndicator}>
                <Warning24Regular />
                <Caption1>Unsaved changes</Caption1>
              </div>
            )}
            <Button
              appearance="primary"
              icon={saving ? <Spinner size="tiny" /> : <Checkmark24Regular />}
              onClick={handleSave}
              disabled={!hasChanges || saving}
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </div>
      </FadeIn>
    </div>
  );
}
