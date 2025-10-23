import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Label,
  Input,
  Slider,
  Dropdown,
  Option,
  Button,
  Card,
} from '@fluentui/react-components';
import { useState } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  colorPreview: {
    width: '100%',
    height: '40px',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  sliderContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  sliderValue: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
});

export interface BrandKitSettings {
  watermarkPath?: string;
  watermarkPosition?: string;
  watermarkOpacity: number;
  brandColor?: string;
  accentColor?: string;
}

interface BrandKitPanelProps {
  settings: BrandKitSettings;
  onSettingsChange: (settings: BrandKitSettings) => void;
}

export function BrandKitPanel({ settings, onSettingsChange }: BrandKitPanelProps) {
  const styles = useStyles();
  const [localSettings, setLocalSettings] = useState<BrandKitSettings>(settings);

  const updateSettings = (updates: Partial<BrandKitSettings>) => {
    const newSettings = { ...localSettings, ...updates };
    setLocalSettings(newSettings);
    onSettingsChange(newSettings);
  };

  return (
    <div className={styles.container}>
      <Title3>Brand Kit</Title3>
      <Text>Customize your videos with watermarks, colors, and branding.</Text>

      <Card className={styles.card}>
        <div className={styles.section}>
          {/* Watermark Settings */}
          <div className={styles.field}>
            <Label weight="semibold">Watermark Image</Label>
            <Input
              type="text"
              placeholder="Path to watermark image (PNG/SVG)"
              value={localSettings.watermarkPath || ''}
              onChange={(e) => updateSettings({ watermarkPath: e.target.value })}
            />
            <Text size={200}>Recommended: Transparent PNG or SVG, max 200px height</Text>
          </div>

          <div className={styles.field}>
            <Label weight="semibold">Watermark Position</Label>
            <Dropdown
              placeholder="Select position"
              value={localSettings.watermarkPosition || 'bottom-right'}
              onOptionSelect={(_, data) =>
                updateSettings({ watermarkPosition: data.optionValue as string })
              }
            >
              <Option value="top-left">Top Left</Option>
              <Option value="top-right">Top Right</Option>
              <Option value="bottom-left">Bottom Left</Option>
              <Option value="bottom-right">Bottom Right</Option>
              <Option value="center">Center</Option>
            </Dropdown>
          </div>

          <div className={styles.sliderContainer}>
            <Label weight="semibold">Watermark Opacity</Label>
            <Slider
              min={0}
              max={1}
              step={0.1}
              value={localSettings.watermarkOpacity}
              onChange={(_, data) => updateSettings({ watermarkOpacity: data.value })}
            />
            <Text className={styles.sliderValue}>
              {(localSettings.watermarkOpacity * 100).toFixed(0)}%
            </Text>
          </div>

          {/* Brand Colors */}
          <div className={styles.field}>
            <Label weight="semibold">Brand Color</Label>
            <Input
              type="text"
              placeholder="#FF6B35"
              value={localSettings.brandColor || ''}
              onChange={(e) => updateSettings({ brandColor: e.target.value })}
            />
            {localSettings.brandColor && (
              <div
                className={styles.colorPreview}
                style={{ backgroundColor: localSettings.brandColor }}
              />
            )}
            <Text size={200}>Primary brand color for subtle overlays (hex format)</Text>
          </div>

          <div className={styles.field}>
            <Label weight="semibold">Accent Color</Label>
            <Input
              type="text"
              placeholder="#00D9FF"
              value={localSettings.accentColor || ''}
              onChange={(e) => updateSettings({ accentColor: e.target.value })}
            />
            {localSettings.accentColor && (
              <div
                className={styles.colorPreview}
                style={{ backgroundColor: localSettings.accentColor }}
              />
            )}
            <Text size={200}>Secondary color for highlights and text (hex format)</Text>
          </div>

          <Button
            appearance="secondary"
            onClick={() => {
              const resetSettings: BrandKitSettings = {
                watermarkOpacity: 0.7,
              };
              setLocalSettings(resetSettings);
              onSettingsChange(resetSettings);
            }}
          >
            Reset to Defaults
          </Button>
        </div>
      </Card>
    </div>
  );
}
