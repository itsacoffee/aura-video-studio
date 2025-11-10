import { makeStyles, tokens, Card, Text, Badge } from '@fluentui/react-components';
import type { FC } from 'react';

const useStyles = makeStyles({
  presetsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  presetCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    border: `2px solid ${tokens.colorNeutralStroke2}`,
    textAlign: 'center',
    position: 'relative',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
      border: `2px solid ${tokens.colorBrandStroke1}`,
    },
  },
  presetSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  presetIcon: {
    fontSize: '40px',
    marginBottom: tokens.spacingVerticalS,
  },
  presetBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingVerticalS,
  },
  presetSpecs: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginTop: tokens.spacingVerticalS,
  },
});

export interface ExportPreset {
  id: string;
  name: string;
  quality: 'low' | 'medium' | 'high' | 'ultra';
  resolution: '480p' | '720p' | '1080p' | '4k';
  format: 'mp4' | 'webm' | 'mov';
  icon: string;
  description: string;
  recommended?: string;
}

export const EXPORT_PRESETS: ExportPreset[] = [
  {
    id: 'youtube-hd',
    name: 'YouTube HD',
    quality: 'high',
    resolution: '1080p',
    format: 'mp4',
    icon: '📺',
    description: 'Optimized for YouTube',
    recommended: 'Most Popular',
  },
  {
    id: 'social-media',
    name: 'Social Media',
    quality: 'high',
    resolution: '1080p',
    format: 'mp4',
    icon: '📱',
    description: 'Instagram, TikTok, Facebook',
  },
  {
    id: 'web-optimized',
    name: 'Web Optimized',
    quality: 'medium',
    resolution: '720p',
    format: 'webm',
    icon: '🌐',
    description: 'Fast loading for websites',
  },
  {
    id: 'professional',
    name: 'Professional',
    quality: 'ultra',
    resolution: '4k',
    format: 'mov',
    icon: '🎬',
    description: 'For editing & archival',
  },
  {
    id: 'quick-preview',
    name: 'Quick Preview',
    quality: 'low',
    resolution: '480p',
    format: 'mp4',
    icon: '⚡',
    description: 'Fast generation for testing',
  },
];

interface ExportPresetsProps {
  selectedPreset?: string;
  onPresetSelect: (preset: ExportPreset) => void;
}

export const ExportPresets: FC<ExportPresetsProps> = ({ selectedPreset, onPresetSelect }) => {
  const styles = useStyles();

  return (
    <div className={styles.presetsGrid}>
      {EXPORT_PRESETS.map((preset) => (
        <Card
          key={preset.id}
          className={`${styles.presetCard} ${
            selectedPreset === preset.id ? styles.presetSelected : ''
          }`}
          onClick={() => onPresetSelect(preset)}
        >
          {preset.recommended && (
            <Badge appearance="filled" color="success" className={styles.presetBadge}>
              {preset.recommended}
            </Badge>
          )}
          {selectedPreset === preset.id && (
            <Badge appearance="filled" color="brand" className={styles.presetBadge}>
              Selected
            </Badge>
          )}
          <div className={styles.presetIcon}>{preset.icon}</div>
          <Text weight="semibold" size={300}>
            {preset.name}
          </Text>
          <Text
            size={200}
            style={{
              marginTop: tokens.spacingVerticalXS,
              color: tokens.colorNeutralForeground3,
            }}
          >
            {preset.description}
          </Text>
          <div className={styles.presetSpecs}>
            <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
              {preset.resolution} • {preset.format.toUpperCase()} • {preset.quality}
            </Text>
          </div>
        </Card>
      ))}
    </div>
  );
};
