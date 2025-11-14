/**
 * Pacing Settings Component
 * Configuration panel for pacing optimization parameters
 */

import {
  Card,
  Button,
  Switch,
  Slider,
  Dropdown,
  Option,
  makeStyles,
  tokens,
  Title3,
  Body1,
  Caption1,
  Field,
} from '@fluentui/react-components';
import { Settings24Regular, Save24Regular, ArrowReset24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect } from 'react';
import { PacingSettings as PacingSettingsType, PlatformPreset } from '../../types/pacing';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  sliderContainer: {
    paddingTop: tokens.spacingVerticalS,
  },
});

interface PacingSettingsProps {
  settings: PacingSettingsType;
  platforms: PlatformPreset[];
  onSettingsChange: (settings: PacingSettingsType) => void;
  onSave: () => void;
}

const DEFAULT_SETTINGS: PacingSettingsType = {
  enabled: true,
  optimizationLevel: 'Moderate',
  targetPlatform: 'YouTube',
  minConfidence: 60,
  autoApply: false,
};

export const PacingSettings: React.FC<PacingSettingsProps> = ({
  settings,
  platforms,
  onSettingsChange,
  onSave,
}) => {
  const styles = useStyles();
  const [localSettings, setLocalSettings] = useState<PacingSettingsType>(settings);

  useEffect(() => {
    setLocalSettings(settings);
  }, [settings]);

  const handleEnabledChange = (enabled: boolean) => {
    const updated = { ...localSettings, enabled };
    setLocalSettings(updated);
    onSettingsChange(updated);
  };

  const handleOptimizationLevelChange = (level: 'Conservative' | 'Moderate' | 'Aggressive') => {
    const updated = { ...localSettings, optimizationLevel: level };
    setLocalSettings(updated);
    onSettingsChange(updated);
  };

  const handlePlatformChange = (platform: string) => {
    const updated = { ...localSettings, targetPlatform: platform };
    setLocalSettings(updated);
    onSettingsChange(updated);
  };

  const handleMinConfidenceChange = (value: number) => {
    const updated = { ...localSettings, minConfidence: value };
    setLocalSettings(updated);
    onSettingsChange(updated);
  };

  const handleAutoApplyChange = (autoApply: boolean) => {
    const updated = { ...localSettings, autoApply };
    setLocalSettings(updated);
    onSettingsChange(updated);
  };

  const handleReset = () => {
    setLocalSettings(DEFAULT_SETTINGS);
    onSettingsChange(DEFAULT_SETTINGS);
  };

  const getOptimizationLevelDescription = (level: string) => {
    switch (level) {
      case 'Conservative':
        return 'Minimal changes, preserves original pacing as much as possible';
      case 'Moderate':
        return 'Balanced approach with moderate adjustments for better engagement';
      case 'Aggressive':
        return 'Maximum optimization for viewer retention and engagement';
      default:
        return '';
    }
  };

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Settings24Regular style={{ fontSize: '24px' }} />
        <Title3>Pacing Optimization Settings</Title3>
      </div>

      <div className={styles.section}>
        {/* Enable/Disable Pacing Optimization */}
        <Field label="Enable Pacing Optimization" hint="Turn pacing optimization on or off">
          <Switch
            checked={localSettings.enabled}
            onChange={(_, data) => handleEnabledChange(data.checked)}
            label={localSettings.enabled ? 'Enabled' : 'Disabled'}
          />
        </Field>

        {/* Optimization Level */}
        <div className={styles.fieldGroup}>
          <Field
            label="Optimization Level"
            hint={getOptimizationLevelDescription(localSettings.optimizationLevel)}
          >
            <Dropdown
              value={localSettings.optimizationLevel}
              selectedOptions={[localSettings.optimizationLevel]}
              onOptionSelect={(_, data) =>
                handleOptimizationLevelChange(
                  data.optionValue as PacingSettingsType['optimizationLevel']
                )
              }
              disabled={!localSettings.enabled}
            >
              <Option value="Conservative">Conservative</Option>
              <Option value="Moderate">Moderate (Recommended)</Option>
              <Option value="Aggressive">Aggressive</Option>
            </Dropdown>
          </Field>
        </div>

        {/* Platform Selector */}
        <div className={styles.fieldGroup}>
          <Field label="Target Platform" hint="Select the platform to optimize for">
            <Dropdown
              value={localSettings.targetPlatform}
              selectedOptions={[localSettings.targetPlatform]}
              onOptionSelect={(_, data) => handlePlatformChange(data.optionValue as string)}
              disabled={!localSettings.enabled}
            >
              {platforms.map((platform) => (
                <Option key={platform.name} value={platform.name}>
                  {`${platform.name} (${platform.recommendedPacing})`}
                </Option>
              ))}
            </Dropdown>
          </Field>
          {platforms.find((p) => p.name === localSettings.targetPlatform) && (
            <Caption1>
              Optimal video length:{' '}
              {Math.floor(
                platforms.find((p) => p.name === localSettings.targetPlatform)!.optimalVideoLength /
                  60
              )}
              m{' '}
              {platforms.find((p) => p.name === localSettings.targetPlatform)!.optimalVideoLength %
                60}
              s
            </Caption1>
          )}
        </div>

        {/* Minimum Confidence Threshold */}
        <div className={styles.fieldGroup}>
          <Field
            label={`Minimum Confidence Threshold: ${localSettings.minConfidence}%`}
            hint="Only apply suggestions with confidence above this threshold"
          >
            <div className={styles.sliderContainer}>
              <Slider
                value={localSettings.minConfidence}
                min={0}
                max={100}
                step={5}
                onChange={(_, data) => handleMinConfidenceChange(data.value)}
                disabled={!localSettings.enabled}
              />
            </div>
          </Field>
          <Caption1>
            {localSettings.minConfidence >= 80 && 'Very high confidence - fewer suggestions'}
            {localSettings.minConfidence >= 60 &&
              localSettings.minConfidence < 80 &&
              'Balanced confidence level (recommended)'}
            {localSettings.minConfidence >= 40 &&
              localSettings.minConfidence < 60 &&
              'Lower confidence - more suggestions'}
            {localSettings.minConfidence < 40 && 'Very low confidence - all suggestions included'}
          </Caption1>
        </div>

        {/* Auto-apply Toggle */}
        <Field
          label="Auto-apply Suggestions"
          hint="Automatically apply all high-confidence suggestions"
        >
          <Switch
            checked={localSettings.autoApply}
            onChange={(_, data) => handleAutoApplyChange(data.checked)}
            label={localSettings.autoApply ? 'Auto-apply enabled' : 'Auto-apply disabled'}
            disabled={!localSettings.enabled}
          />
        </Field>
        {localSettings.autoApply && (
          <Body1 style={{ color: tokens.colorPaletteYellowForeground1 }}>
            ⚠️ Auto-apply will immediately modify your video timeline. Review changes carefully.
          </Body1>
        )}

        {/* Action Buttons */}
        <div className={styles.actions}>
          <Button appearance="secondary" icon={<ArrowReset24Regular />} onClick={handleReset}>
            Reset to Defaults
          </Button>
          <Button appearance="primary" icon={<Save24Regular />} onClick={onSave}>
            Save Settings
          </Button>
        </div>
      </div>
    </Card>
  );
};
