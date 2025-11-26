import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Card,
  Badge,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import {
  Flash24Regular,
  Video24Regular,
  BookOpen24Regular,
  Share24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import { VIDEO_PRESETS, type VideoPreset } from './presetData';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  presetCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    border: `2px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
      border: `2px solid ${tokens.colorBrandStroke1}`,
    },
  },
  selectedCard: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  presetHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  presetIcon: {
    fontSize: '32px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  presetDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalS,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  badges: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  selectedBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingVerticalS,
  },
  footer: {
    display: 'flex',
    justifyContent: 'center',
    marginTop: tokens.spacingVerticalL,
  },
  skipButton: {
    marginTop: tokens.spacingVerticalM,
  },
});

/**
 * Gets the icon component for a preset based on its ID.
 */
function getPresetIconComponent(presetId: string): JSX.Element {
  switch (presetId) {
    case 'quick-demo':
      return <Flash24Regular />;
    case 'youtube-short':
      return <Video24Regular />;
    case 'tutorial':
      return <BookOpen24Regular />;
    case 'social-media':
      return <Share24Regular />;
    default:
      return <Video24Regular />;
  }
}

/**
 * Formats duration in seconds to a human-readable string.
 */
function formatDuration(seconds: number): string {
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return remainingSeconds > 0 ? `${minutes}m ${remainingSeconds}s` : `${minutes}m`;
}

interface PresetSelectorProps {
  selectedPresetId: string | null;
  onSelectPreset: (preset: VideoPreset) => void;
  onSkip?: () => void;
  showSkipButton?: boolean;
}

/**
 * PresetSelector component displays available video presets and allows users to select one.
 * This is shown prominently in the first-run experience for a streamlined workflow.
 */
export const PresetSelector: FC<PresetSelectorProps> = ({
  selectedPresetId,
  onSelectPreset,
  onSkip,
  showSkipButton = false,
}) => {
  const styles = useStyles();

  const handlePresetClick = useCallback(
    (preset: VideoPreset) => {
      onSelectPreset(preset);
    },
    [onSelectPreset]
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Choose a Starting Point</Title2>
        <Text
          size={300}
          style={{ marginTop: tokens.spacingVerticalS, color: tokens.colorNeutralForeground3 }}
        >
          Select a preset to get started quickly, or customize later
        </Text>
      </div>

      <div className={styles.grid}>
        {VIDEO_PRESETS.map((preset) => (
          <Card
            key={preset.id}
            className={`${styles.presetCard} ${
              selectedPresetId === preset.id ? styles.selectedCard : ''
            }`}
            onClick={() => handlePresetClick(preset)}
          >
            {selectedPresetId === preset.id && (
              <Badge
                appearance="filled"
                color="success"
                className={styles.selectedBadge}
                icon={<Checkmark24Regular />}
              >
                Selected
              </Badge>
            )}

            <div className={styles.presetHeader}>
              <div className={styles.presetIcon}>{getPresetIconComponent(preset.id)}</div>
              <div>
                <Title3>{preset.name}</Title3>
                <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                  {preset.icon} {preset.description}
                </Text>
              </div>
            </div>

            <div className={styles.presetDetails}>
              <div className={styles.detailRow}>
                <Text size={200}>Duration</Text>
                <Text size={200} weight="semibold">
                  {formatDuration(preset.duration)}
                </Text>
              </div>
              <div className={styles.detailRow}>
                <Text size={200}>Aspect Ratio</Text>
                <Text size={200} weight="semibold">
                  {preset.aspectRatio}
                </Text>
              </div>
              <div className={styles.detailRow}>
                <Text size={200}>Resolution</Text>
                <Text size={200} weight="semibold">
                  {preset.resolution}
                </Text>
              </div>
            </div>

            <div className={styles.badges}>
              {preset.worksOffline && (
                <Tooltip content="Works without internet connection" relationship="label">
                  <Badge appearance="outline" color="success" size="small">
                    Offline
                  </Badge>
                </Tooltip>
              )}
              {!preset.requiresApiKey && (
                <Tooltip content="No API keys required" relationship="label">
                  <Badge appearance="outline" color="informative" size="small">
                    Free
                  </Badge>
                </Tooltip>
              )}
              {preset.estimatedCost === 0 && (
                <Tooltip content="No cost per video" relationship="label">
                  <Badge appearance="outline" color="success" size="small">
                    $0
                  </Badge>
                </Tooltip>
              )}
            </div>
          </Card>
        ))}
      </div>

      {showSkipButton && onSkip && (
        <div className={styles.footer}>
          <Button appearance="subtle" onClick={onSkip} className={styles.skipButton}>
            Skip and customize manually
          </Button>
        </div>
      )}
    </div>
  );
};

export default PresetSelector;
