/**
 * DirectorPresetSelector - AI Director Style Selection Component
 * Allows users to select professional cinematographic presets for video generation.
 */

import React from 'react';
import { Dropdown, Option, Label, Text, tokens, makeStyles } from '@fluentui/react-components';
import type { DropdownProps } from '@fluentui/react-components';

export type DirectorPreset =
  | 'Documentary'
  | 'TikTokEnergy'
  | 'Cinematic'
  | 'Corporate'
  | 'Educational'
  | 'Storytelling'
  | 'Custom';

interface DirectorPresetSelectorProps {
  value: DirectorPreset;
  onChange: (preset: DirectorPreset) => void;
  disabled?: boolean;
}

const presetDescriptions: Record<DirectorPreset, string> = {
  Documentary: 'Steady, informative. Minimal motion, clean cuts.',
  TikTokEnergy: 'Fast-paced, dynamic. Quick cuts, high energy.',
  Cinematic: 'Dramatic, emotional. Slow reveals, epic transitions.',
  Corporate: 'Professional, clean. Subtle motion, polished.',
  Educational: 'Clear, focused. Emphasis on key points.',
  Storytelling: 'Narrative-driven. Emotion-matched pacing.',
  Custom: 'Manual control over all settings.',
};

const presetEmojis: Record<DirectorPreset, string> = {
  Documentary: '📹',
  TikTokEnergy: '⚡',
  Cinematic: '🎬',
  Corporate: '💼',
  Educational: '📚',
  Storytelling: '📖',
  Custom: '⚙️',
};

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  label: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontWeight: tokens.fontWeightSemibold,
  },
  optionContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  optionTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  optionDescription: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  dropdown: {
    width: '100%',
    minWidth: '200px',
  },
});

export const DirectorPresetSelector: React.FC<DirectorPresetSelectorProps> = ({
  value,
  onChange,
  disabled = false,
}) => {
  const styles = useStyles();

  const handleOptionSelect: DropdownProps['onOptionSelect'] = (_event, data) => {
    if (data.optionValue) {
      onChange(data.optionValue as DirectorPreset);
    }
  };

  const presets: DirectorPreset[] = [
    'Documentary',
    'TikTokEnergy',
    'Cinematic',
    'Corporate',
    'Educational',
    'Storytelling',
    'Custom',
  ];

  return (
    <div className={styles.container}>
      <Label className={styles.label} htmlFor="director-preset-select">
        🎬 AI Director Style
      </Label>
      <Dropdown
        id="director-preset-select"
        className={styles.dropdown}
        value={`${presetEmojis[value]} ${value}`}
        selectedOptions={[value]}
        onOptionSelect={handleOptionSelect}
        disabled={disabled}
        aria-label="Select AI Director preset style"
      >
        {presets.map((preset) => (
          <Option key={preset} value={preset} text={`${presetEmojis[preset]} ${preset}`}>
            <div className={styles.optionContent}>
              <div className={styles.optionTitle}>
                <span>{presetEmojis[preset]}</span>
                <Text weight="semibold">{preset}</Text>
              </div>
              <Text className={styles.optionDescription}>{presetDescriptions[preset]}</Text>
            </div>
          </Option>
        ))}
      </Dropdown>
    </div>
  );
};

export default DirectorPresetSelector;
