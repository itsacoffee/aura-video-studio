import { makeStyles, tokens, Card, Text, Badge, mergeClasses } from '@fluentui/react-components';
import { Checkmark24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  platformCard: {
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    position: 'relative',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  selected: {
    borderColor: tokens.colorBrandBackground as string,
    borderWidth: '2px' as string,
    backgroundColor: tokens.colorBrandBackground2,
  },
  cardContent: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalM,
    textAlign: 'center',
  },
  platformIcon: {
    fontSize: '48px',
    marginBottom: tokens.spacingVerticalS,
  },
  checkmark: {
    position: 'absolute',
    top: tokens.spacingVerticalS,
    right: tokens.spacingHorizontalS,
    color: tokens.colorBrandForeground1,
  },
  platformName: {
    fontWeight: 600,
  },
  platformSpecs: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    width: '100%',
  },
  specBadge: {
    width: '100%',
    justifyContent: 'center',
  },
});

interface PlatformConfig {
  id: string;
  name: string;
  icon: string;
  color: string;
  recommendedResolution: string;
  aspectRatio: string;
  maxDuration?: string;
}

const PLATFORMS: PlatformConfig[] = [
  {
    id: 'youtube',
    name: 'YouTube',
    icon: '▶️',
    color: '#FF0000',
    recommendedResolution: '1920×1080',
    aspectRatio: '16:9',
  },
  {
    id: 'tiktok',
    name: 'TikTok',
    icon: '🎵',
    color: '#000000',
    recommendedResolution: '1080×1920',
    aspectRatio: '9:16',
    maxDuration: '10 min',
  },
  {
    id: 'instagram',
    name: 'Instagram',
    icon: '📷',
    color: '#E4405F',
    recommendedResolution: '1080×1920',
    aspectRatio: '9:16',
    maxDuration: '90 sec',
  },
  {
    id: 'linkedin',
    name: 'LinkedIn',
    icon: '💼',
    color: '#0077B5',
    recommendedResolution: '1920×1080',
    aspectRatio: '16:9',
    maxDuration: '10 min',
  },
  {
    id: 'twitter',
    name: 'Twitter',
    icon: '🐦',
    color: '#1DA1F2',
    recommendedResolution: '1280×720',
    aspectRatio: '16:9',
    maxDuration: '2:20',
  },
  {
    id: 'facebook',
    name: 'Facebook',
    icon: '👥',
    color: '#1877F2',
    recommendedResolution: '1280×720',
    aspectRatio: '16:9',
  },
];

export interface PlatformSelectionGridProps {
  selectedPlatforms: string[];
  onPlatformToggle: (platformId: string) => void;
}

export function PlatformSelectionGrid({
  selectedPlatforms,
  onPlatformToggle,
}: PlatformSelectionGridProps) {
  const styles = useStyles();

  return (
    <div className={styles.grid}>
      {PLATFORMS.map((platform) => {
        const isSelected = selectedPlatforms.includes(platform.id);

        return (
          <Card
            key={platform.id}
            className={mergeClasses(styles.platformCard, isSelected && styles.selected)}
            onClick={() => onPlatformToggle(platform.id)}
          >
            {isSelected && <Checkmark24Regular className={styles.checkmark} />}
            <div className={styles.cardContent}>
              <div className={styles.platformIcon} style={{ color: platform.color }}>
                {platform.icon}
              </div>
              <Text className={styles.platformName}>{platform.name}</Text>
              <div className={styles.platformSpecs}>
                <Badge size="small" appearance="outline" className={styles.specBadge}>
                  {platform.recommendedResolution}
                </Badge>
                <Badge size="small" appearance="outline" className={styles.specBadge}>
                  {platform.aspectRatio}
                </Badge>
                {platform.maxDuration && (
                  <Badge
                    size="small"
                    appearance="outline"
                    color="warning"
                    className={styles.specBadge}
                  >
                    Max: {platform.maxDuration}
                  </Badge>
                )}
              </div>
            </div>
          </Card>
        );
      })}
    </div>
  );
}
