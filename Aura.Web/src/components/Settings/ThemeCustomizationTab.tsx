import {
  makeStyles,
  tokens,
  Title2,
  Text,
  Button,
  Card,
  Field,
  Switch,
  Input,
  Dropdown,
  Option,
  Slider,
} from '@fluentui/react-components';
import { Save24Regular, Color24Regular, ArrowReset24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { apiUrl } from '../../config/api';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalXL,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  infoCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    marginBottom: tokens.spacingVerticalL,
  },
  colorPreview: {
    width: '40px',
    height: '40px',
    borderRadius: tokens.borderRadiusMedium,
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    cursor: 'pointer',
  },
  colorRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  presetGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  presetCard: {
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    cursor: 'pointer',
    transition: 'all 0.2s',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  colorSwatch: {
    width: '100%',
    height: '60px',
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
});

interface ThemePreset {
  name: string;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
}

const THEME_PRESETS: ThemePreset[] = [
  {
    name: 'Aura',
    primaryColor: '#00D4FF',
    secondaryColor: '#FF6B35',
    accentColor: '#0EA5E9',
  },
  {
    name: 'Default Blue',
    primaryColor: '#0078D4',
    secondaryColor: '#005A9E',
    accentColor: '#2B88D8',
  },
  {
    name: 'Purple Dream',
    primaryColor: '#6750A4',
    secondaryColor: '#4F378B',
    accentColor: '#7965AF',
  },
  {
    name: 'Teal Ocean',
    primaryColor: '#03DAC6',
    secondaryColor: '#018786',
    accentColor: '#00BFA5',
  },
  {
    name: 'Sunset Orange',
    primaryColor: '#FF6B35',
    secondaryColor: '#E85D2F',
    accentColor: '#FF8960',
  },
  {
    name: 'Forest Green',
    primaryColor: '#2E7D32',
    secondaryColor: '#1B5E20',
    accentColor: '#388E3C',
  },
  { name: 'Ruby Red', primaryColor: '#D32F2F', secondaryColor: '#B71C1C', accentColor: '#E53935' },
  {
    name: 'Deep Space',
    primaryColor: '#1A237E',
    secondaryColor: '#0D1644',
    accentColor: '#303F9F',
  },
  {
    name: 'Mint Fresh',
    primaryColor: '#00C853',
    secondaryColor: '#00A344',
    accentColor: '#00E676',
  },
];

const FONT_FAMILIES = [
  'Segoe UI',
  'Arial',
  'Helvetica',
  'Verdana',
  'Calibri',
  'Roboto',
  'Open Sans',
  'Lato',
  'Montserrat',
];

export function ThemeCustomizationTab() {
  const styles = useStyles();
  const [modified, setModified] = useState(false);
  const [saving, setSaving] = useState(false);

  // Theme settings state
  const [autoTheme, setAutoTheme] = useState(true);
  const [darkMode, setDarkMode] = useState(false);
  const [primaryColor, setPrimaryColor] = useState('#0078D4');
  const [secondaryColor, setSecondaryColor] = useState('#005A9E');
  const [accentColor, setAccentColor] = useState('#2B88D8');
  const [selectedPreset, setSelectedPreset] = useState('Default Blue');
  const [fontFamily, setFontFamily] = useState('Segoe UI');
  const [baseFontSize, setBaseFontSize] = useState(14);
  const [borderRadius, setBorderRadius] = useState(4);
  const [animationsEnabled, setAnimationsEnabled] = useState(true);
  const [reducedMotion, setReducedMotion] = useState(false);
  const [highContrast, setHighContrast] = useState(false);

  useEffect(() => {
    fetchThemeSettings();
  }, []);

  const fetchThemeSettings = async () => {
    try {
      const response = await fetch(apiUrl('/api/settings/theme'));
      if (response.ok) {
        const data = await response.json();
        setAutoTheme(data.autoTheme !== false);
        setDarkMode(data.darkMode || false);
        setPrimaryColor(data.primaryColor || '#0078D4');
        setSecondaryColor(data.secondaryColor || '#005A9E');
        setAccentColor(data.accentColor || '#2B88D8');
        setSelectedPreset(data.selectedPreset || 'Default Blue');
        setFontFamily(data.fontFamily || 'Segoe UI');
        setBaseFontSize(data.baseFontSize || 14);
        setBorderRadius(data.borderRadius || 4);
        setAnimationsEnabled(data.animationsEnabled !== false);
        setReducedMotion(data.reducedMotion || false);
        setHighContrast(data.highContrast || false);
      }
    } catch (error) {
      console.error('Error fetching theme settings:', error);
    }
  };

  const saveThemeSettings = async () => {
    setSaving(true);
    try {
      const response = await fetch('/api/settings/theme', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          autoTheme,
          darkMode,
          primaryColor,
          secondaryColor,
          accentColor,
          selectedPreset,
          fontFamily,
          baseFontSize,
          borderRadius,
          animationsEnabled,
          reducedMotion,
          highContrast,
        }),
      });
      if (response.ok) {
        alert('Theme settings saved successfully. Refresh the page to see changes.');
        setModified(false);
        applyTheme();
      } else {
        alert('Error saving theme settings');
      }
    } catch (error) {
      console.error('Error saving theme settings:', error);
      alert('Error saving theme settings');
    } finally {
      setSaving(false);
    }
  };

  const applyTheme = () => {
    const root = document.documentElement;
    root.style.setProperty('--primary-color', primaryColor);
    root.style.setProperty('--secondary-color', secondaryColor);
    root.style.setProperty('--accent-color', accentColor);
    root.style.setProperty('--font-family', fontFamily);
    root.style.setProperty('--base-font-size', `${baseFontSize}px`);
    root.style.setProperty('--border-radius', `${borderRadius}px`);
    if (reducedMotion) {
      root.style.setProperty('--animation-duration', '0.01ms');
    }
  };

  const applyPreset = (preset: ThemePreset) => {
    setPrimaryColor(preset.primaryColor);
    setSecondaryColor(preset.secondaryColor);
    setAccentColor(preset.accentColor);
    setSelectedPreset(preset.name);
    setModified(true);
  };

  const resetToDefaults = () => {
    if (confirm('Reset theme to defaults?')) {
      const defaultPreset = THEME_PRESETS[0];
      setPrimaryColor(defaultPreset.primaryColor);
      setSecondaryColor(defaultPreset.secondaryColor);
      setAccentColor(defaultPreset.accentColor);
      setSelectedPreset(defaultPreset.name);
      setFontFamily('Segoe UI');
      setBaseFontSize(14);
      setBorderRadius(4);
      setAnimationsEnabled(true);
      setReducedMotion(false);
      setHighContrast(false);
      setModified(true);
    }
  };

  return (
    <Card className={styles.section}>
      <Title2>
        <Color24Regular
          style={{ marginRight: tokens.spacingHorizontalS, verticalAlign: 'middle' }}
        />
        Theme Customization
      </Title2>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
        Customize the visual appearance of the application
      </Text>

      <Card className={styles.infoCard}>
        <Text weight="semibold" size={300}>
          💡 Tip
        </Text>
        <Text size={200} style={{ marginTop: tokens.spacingVerticalXS }}>
          Theme changes require a page refresh to fully apply. Some changes may require restarting
          the application.
        </Text>
      </Card>

      <div className={styles.form}>
        {/* Dark Mode */}
        <Text size={400} weight="semibold">
          Theme Mode
        </Text>

        <Field label="Auto Theme">
          <Switch
            checked={autoTheme}
            onChange={(_, data) => {
              setAutoTheme(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {autoTheme ? 'Enabled' : 'Disabled'} - Follow system dark/light mode preference
          </Text>
        </Field>

        {!autoTheme && (
          <Field label="Dark Mode">
            <Switch
              checked={darkMode}
              onChange={(_, data) => {
                setDarkMode(data.checked);
                setModified(true);
              }}
            />
            <Text size={200}>{darkMode ? 'Dark' : 'Light'} mode</Text>
          </Field>
        )}

        {/* Color Presets */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Color Presets
        </Text>

        <div className={styles.presetGrid}>
          {THEME_PRESETS.map((preset) => (
            <div
              key={preset.name}
              className={styles.presetCard}
              style={
                selectedPreset === preset.name
                  ? {
                      borderColor: tokens.colorBrandStroke1,
                      backgroundColor: tokens.colorBrandBackground2,
                    }
                  : undefined
              }
              role="button"
              tabIndex={0}
              onClick={() => applyPreset(preset)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  applyPreset(preset);
                }
              }}
            >
              <div
                className={styles.colorSwatch}
                style={{
                  background: `linear-gradient(135deg, ${preset.primaryColor} 0%, ${preset.secondaryColor} 50%, ${preset.accentColor} 100%)`,
                }}
              />
              <Text weight="semibold" size={200}>
                {preset.name}
              </Text>
            </div>
          ))}
        </div>

        {/* Custom Colors */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Custom Colors
        </Text>

        <Field label="Primary Color">
          <div className={styles.colorRow}>
            <input
              type="color"
              value={primaryColor}
              onChange={(e) => {
                setPrimaryColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              className={styles.colorPreview}
            />
            <Input
              value={primaryColor}
              onChange={(e) => {
                setPrimaryColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              style={{ flex: 1 }}
            />
          </div>
        </Field>

        <Field label="Secondary Color">
          <div className={styles.colorRow}>
            <input
              type="color"
              value={secondaryColor}
              onChange={(e) => {
                setSecondaryColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              className={styles.colorPreview}
            />
            <Input
              value={secondaryColor}
              onChange={(e) => {
                setSecondaryColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              style={{ flex: 1 }}
            />
          </div>
        </Field>

        <Field label="Accent Color">
          <div className={styles.colorRow}>
            <input
              type="color"
              value={accentColor}
              onChange={(e) => {
                setAccentColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              className={styles.colorPreview}
            />
            <Input
              value={accentColor}
              onChange={(e) => {
                setAccentColor(e.target.value);
                setModified(true);
                setSelectedPreset('Custom');
              }}
              style={{ flex: 1 }}
            />
          </div>
        </Field>

        {/* Typography */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Typography
        </Text>

        <Field label="Font Family">
          <Dropdown
            value={fontFamily}
            onOptionSelect={(_, data) => {
              setFontFamily(data.optionValue || 'Segoe UI');
              setModified(true);
            }}
            style={{ width: '100%' }}
          >
            {FONT_FAMILIES.map((font) => (
              <Option key={font} value={font} text={font}>
                {font}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field label={`Base Font Size: ${baseFontSize}px`}>
          <Slider
            min={12}
            max={18}
            step={1}
            value={baseFontSize}
            onChange={(_, data) => {
              setBaseFontSize(data.value);
              setModified(true);
            }}
          />
        </Field>

        {/* Visual Effects */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Visual Effects
        </Text>

        <Field label={`Border Radius: ${borderRadius}px`} hint="Roundness of corners">
          <Slider
            min={0}
            max={16}
            step={1}
            value={borderRadius}
            onChange={(_, data) => {
              setBorderRadius(data.value);
              setModified(true);
            }}
          />
        </Field>

        <Field label="Animations">
          <Switch
            checked={animationsEnabled}
            onChange={(_, data) => {
              setAnimationsEnabled(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {animationsEnabled ? 'Enabled' : 'Disabled'} - Smooth transitions and animations
          </Text>
        </Field>

        <Field label="Reduced Motion">
          <Switch
            checked={reducedMotion}
            onChange={(_, data) => {
              setReducedMotion(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {reducedMotion ? 'Enabled' : 'Disabled'} - Minimize motion for accessibility
          </Text>
        </Field>

        {/* Accessibility */}
        <Text size={400} weight="semibold" style={{ marginTop: tokens.spacingVerticalL }}>
          Accessibility
        </Text>

        <Field label="High Contrast Mode">
          <Switch
            checked={highContrast}
            onChange={(_, data) => {
              setHighContrast(data.checked);
              setModified(true);
            }}
          />
          <Text size={200}>
            {highContrast ? 'Enabled' : 'Disabled'} - Enhanced contrast for better visibility
          </Text>
        </Field>

        {modified && (
          <Text size={200} style={{ color: tokens.colorPaletteYellowForeground1 }}>
            ⚠️ You have unsaved changes
          </Text>
        )}

        <div className={styles.actions}>
          <Button appearance="secondary" icon={<ArrowReset24Regular />} onClick={resetToDefaults}>
            Reset to Defaults
          </Button>
          <Button
            appearance="primary"
            icon={<Save24Regular />}
            onClick={saveThemeSettings}
            disabled={!modified || saving}
          >
            {saving ? 'Saving...' : 'Save Theme Settings'}
          </Button>
        </div>
      </div>
    </Card>
  );
}
