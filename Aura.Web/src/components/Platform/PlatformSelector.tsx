import React, { useState, useEffect } from 'react';
import {
  Card,
  Title3,
  Body1,
  Button,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  Circle24Regular,
} from '@fluentui/react-icons';
import type { PlatformProfile, SupportedPlatform } from '../../types/platform';
import { PLATFORM_ICONS } from '../../types/platform';
import platformService from '../../services/platform/platformService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  platformGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  platformCard: {
    cursor: 'pointer',
    transition: 'all 0.2s',
    outline: `2px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      boxShadow: tokens.shadow8,
    },
  },
  platformCardSelected: {
    outline: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  platformHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  platformIcon: {
    fontSize: '32px',
  },
  platformDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  detailRow: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
});

interface PlatformSelectorProps {
  onPlatformsSelected?: (platforms: string[]) => void;
  selectedPlatforms?: string[];
  singleSelect?: boolean;
}

export const PlatformSelector: React.FC<PlatformSelectorProps> = ({
  onPlatformsSelected,
  selectedPlatforms = [],
  singleSelect = false,
}) => {
  const styles = useStyles();
  const [platforms, setPlatforms] = useState<PlatformProfile[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set(selectedPlatforms));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadPlatforms();
  }, []);

  useEffect(() => {
    setSelected(new Set(selectedPlatforms));
  }, [selectedPlatforms]);

  const loadPlatforms = async () => {
    try {
      setLoading(true);
      const data = await platformService.getAllPlatforms();
      setPlatforms(data);
      setError(null);
    } catch (err) {
      console.error('Failed to load platforms:', err);
      setError('Failed to load platform information');
    } finally {
      setLoading(false);
    }
  };

  const handlePlatformClick = (platformId: string) => {
    const newSelected = new Set(selected);
    
    if (singleSelect) {
      newSelected.clear();
      newSelected.add(platformId);
    } else {
      if (newSelected.has(platformId)) {
        newSelected.delete(platformId);
      } else {
        newSelected.add(platformId);
      }
    }

    setSelected(newSelected);
    onPlatformsSelected?.(Array.from(newSelected));
  };

  const selectAll = () => {
    const allPlatformIds = platforms.map(p => p.platformId);
    setSelected(new Set(allPlatformIds));
    onPlatformsSelected?.(allPlatformIds);
  };

  const clearAll = () => {
    setSelected(new Set());
    onPlatformsSelected?.([]);
  };

  if (loading) {
    return <Body1>Loading platform information...</Body1>;
  }

  if (error) {
    return <Body1>Error: {error}</Body1>;
  }

  return (
    <div className={styles.container}>
      <div>
        <Title3>Select Target Platforms</Title3>
        <Body1>Choose which platforms to optimize your content for</Body1>
      </div>

      <div className={styles.platformGrid}>
        {platforms.map(platform => {
          const isSelected = selected.has(platform.platformId);
          const icon = PLATFORM_ICONS[platform.platformId as SupportedPlatform] || '📱';

          return (
            <Card
              key={platform.platformId}
              className={`${styles.platformCard} ${isSelected ? styles.platformCardSelected : ''}`}
              onClick={() => handlePlatformClick(platform.platformId)}
            >
              <div className={styles.platformHeader}>
                <span className={styles.platformIcon}>{icon}</span>
                <div style={{ flex: 1 }}>
                  <Title3>{platform.name}</Title3>
                  {isSelected ? (
                    <CheckmarkCircle24Regular primaryFill={tokens.colorBrandForeground1} />
                  ) : (
                    <Circle24Regular />
                  )}
                </div>
              </div>

              <Body1>{platform.description}</Body1>

              <div className={styles.platformDetails}>
                <div className={styles.detailRow}>
                  <span>Optimal Length:</span>
                  <span>
                    {platform.requirements.video.optimalMinDurationSeconds}-
                    {platform.requirements.video.optimalMaxDurationSeconds}s
                  </span>
                </div>
                <div className={styles.detailRow}>
                  <span>Aspect Ratio:</span>
                  <span>
                    {platform.requirements.supportedAspectRatios
                      .filter(ar => ar.isPreferred)
                      .map(ar => ar.ratio)
                      .join(', ')}
                  </span>
                </div>
                <div className={styles.detailRow}>
                  <span>Hook Duration:</span>
                  <span>{platform.bestPractices.hookDurationSeconds}s</span>
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      {!singleSelect && (
        <div className={styles.actions}>
          <Button appearance="secondary" onClick={selectAll}>
            Select All
          </Button>
          <Button appearance="secondary" onClick={clearAll}>
            Clear All
          </Button>
          <Body1>{selected.size} platform(s) selected</Body1>
        </div>
      )}
    </div>
  );
};
