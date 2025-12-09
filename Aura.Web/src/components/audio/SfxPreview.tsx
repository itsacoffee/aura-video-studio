import {
  Text,
  Button,
  Slider,
  Card,
  Badge,
  makeStyles,
  tokens,
  Switch,
  Tooltip,
  Spinner,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Delete24Regular,
  Speaker224Regular,
  Info16Regular,
  Add24Regular,
  ArrowSort24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useMemo } from 'react';
import { SoundEffect, SoundEffectType } from '../../services/audioIntelligenceService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  effectList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  effectCard: {
    padding: tokens.spacingVerticalS,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  effectHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  effectInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  effectMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  effectControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    alignItems: 'center',
  },
  timeline: {
    marginTop: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
  timelineTrack: {
    position: 'relative',
    height: '40px',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    marginTop: tokens.spacingVerticalS,
    overflow: 'hidden',
  },
  timelineMarker: {
    position: 'absolute',
    height: '100%',
    minWidth: '4px',
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    '&:hover': {
      transform: 'scaleY(1.1)',
    },
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
  },
  volumeControl: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  volumeSlider: {
    width: '100px',
  },
  loadingState: {
    display: 'flex',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
  },
  filterRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
    marginBottom: tokens.spacingVerticalM,
  },
  filterButton: {
    minWidth: 'auto',
  },
});

const effectTypeColors: Record<SoundEffectType, string> = {
  [SoundEffectType.Transition]: tokens.colorPaletteBlueBorderActive,
  [SoundEffectType.Impact]: tokens.colorPaletteRedBorderActive,
  [SoundEffectType.Whoosh]: tokens.colorPaletteTealBorderActive,
  [SoundEffectType.Click]: tokens.colorPaletteGreenBorderActive,
  [SoundEffectType.UI]: tokens.colorPalettePurpleBorderActive,
  [SoundEffectType.Ambient]: tokens.colorPaletteYellowBorderActive,
  [SoundEffectType.Nature]: tokens.colorPaletteGreenBorderActive,
  [SoundEffectType.Technology]: tokens.colorPaletteBlueBorderActive,
  [SoundEffectType.Action]: tokens.colorPaletteRedBorderActive,
  [SoundEffectType.Notification]: tokens.colorPalettePurpleBorderActive,
};

interface SfxPreviewProps {
  effects: SoundEffect[];
  totalDuration: number;
  loading?: boolean;
  onEffectToggle?: (effectId: string, enabled: boolean) => void;
  onVolumeChange?: (effectId: string, volume: number) => void;
  onRemoveEffect?: (effectId: string) => void;
  onPreview?: (effectId: string) => void;
  onAddEffect?: () => void;
  onReorderEffects?: (effects: SoundEffect[]) => void;
}

export const SfxPreview: React.FC<SfxPreviewProps> = ({
  effects,
  totalDuration,
  loading = false,
  onEffectToggle,
  onVolumeChange,
  onRemoveEffect,
  onPreview,
  onAddEffect,
  onReorderEffects,
}) => {
  const styles = useStyles();
  const [selectedEffectId, setSelectedEffectId] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<SoundEffectType | null>(null);
  const [enabledEffects, setEnabledEffects] = useState<Set<string>>(
    new Set(effects.map((e) => e.effectId))
  );
  const [playingEffectId, setPlayingEffectId] = useState<string | null>(null);

  const filteredEffects = useMemo(() => {
    if (!activeFilter) return effects;
    return effects.filter((e) => e.type === activeFilter);
  }, [effects, activeFilter]);

  const effectTypes = useMemo(() => {
    const types = new Set(effects.map((e) => e.type));
    return Array.from(types);
  }, [effects]);

  const handleToggle = useCallback(
    (effectId: string, enabled: boolean) => {
      const newEnabled = new Set(enabledEffects);
      if (enabled) {
        newEnabled.add(effectId);
      } else {
        newEnabled.delete(effectId);
      }
      setEnabledEffects(newEnabled);
      onEffectToggle?.(effectId, enabled);
    },
    [enabledEffects, onEffectToggle]
  );

  const handlePreview = useCallback(
    (effectId: string) => {
      if (playingEffectId === effectId) {
        setPlayingEffectId(null);
      } else {
        setPlayingEffectId(effectId);
        onPreview?.(effectId);
        // Simulate playback ending
        setTimeout(() => setPlayingEffectId(null), 1000);
      }
    },
    [playingEffectId, onPreview]
  );

  const parseISODuration = (
    timestamp: string
  ): { hours: number; minutes: number; seconds: number } => {
    // Parse ISO 8601 duration (e.g., PT1H30M45.5S)
    let hours = 0;
    let minutes = 0;
    let seconds = 0;

    // Extract hours
    const hourMatch = timestamp.match(/(\d+)H/);
    if (hourMatch) hours = parseInt(hourMatch[1]);

    // Extract minutes
    const minMatch = timestamp.match(/(\d+)M/);
    if (minMatch) minutes = parseInt(minMatch[1]);

    // Extract seconds (may include decimals)
    const secMatch = timestamp.match(/(\d+\.?\d*)S/);
    if (secMatch) seconds = parseFloat(secMatch[1]);

    return { hours, minutes, seconds };
  };

  const formatTimestamp = (timestamp: string): string => {
    const { hours, minutes, seconds } = parseISODuration(timestamp);

    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toFixed(1).padStart(4, '0')}`;
    }
    return `${minutes}:${seconds.toFixed(1).padStart(4, '0')}`;
  };

  const getTimelinePosition = (timestamp: string): number => {
    const { hours, minutes, seconds } = parseISODuration(timestamp);
    const totalSeconds = hours * 3600 + minutes * 60 + seconds;
    return (totalSeconds / totalDuration) * 100;
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.loadingState}>
          <Spinner label="Loading sound effects..." />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text size={500} weight="semibold">
          <Speaker224Regular /> Sound Effects Preview
        </Text>
        <div className={styles.controls}>
          {onAddEffect && (
            <Button appearance="primary" icon={<Add24Regular />} onClick={onAddEffect}>
              Add Effect
            </Button>
          )}
          {onReorderEffects && (
            <Tooltip content="Reorder effects by timestamp" relationship="description">
              <Button
                appearance="secondary"
                icon={<ArrowSort24Regular />}
                onClick={() => {
                  const sorted = [...effects].sort((a, b) => {
                    const posA = getTimelinePosition(a.timestamp);
                    const posB = getTimelinePosition(b.timestamp);
                    return posA - posB;
                  });
                  onReorderEffects(sorted);
                }}
              />
            </Tooltip>
          )}
        </div>
      </div>

      {/* Filter buttons */}
      {effectTypes.length > 1 && (
        <div className={styles.filterRow}>
          <Button
            appearance={activeFilter === null ? 'primary' : 'secondary'}
            className={styles.filterButton}
            size="small"
            onClick={() => setActiveFilter(null)}
          >
            All ({effects.length})
          </Button>
          {effectTypes.map((type) => (
            <Button
              key={type}
              appearance={activeFilter === type ? 'primary' : 'secondary'}
              className={styles.filterButton}
              size="small"
              onClick={() => setActiveFilter(type)}
            >
              {type} ({effects.filter((e) => e.type === type).length})
            </Button>
          ))}
        </div>
      )}

      {/* Timeline visualization */}
      {effects.length > 0 && (
        <div className={styles.timeline}>
          <Text size={300}>Timeline</Text>
          <div className={styles.timelineTrack}>
            {effects.map((effect) => {
              const position = getTimelinePosition(effect.timestamp);
              const isEnabled = enabledEffects.has(effect.effectId);
              const color = effectTypeColors[effect.type] || tokens.colorBrandBackground;

              return (
                <Tooltip
                  key={effect.effectId}
                  content={`${effect.type}: ${effect.description} @ ${formatTimestamp(effect.timestamp)}`}
                  relationship="description"
                >
                  <div
                    className={styles.timelineMarker}
                    style={{
                      left: `${position}%`,
                      backgroundColor: isEnabled ? color : tokens.colorNeutralBackground5,
                      opacity: isEnabled ? 1 : 0.4,
                      width: '8px',
                    }}
                    onClick={() => setSelectedEffectId(effect.effectId)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        setSelectedEffectId(effect.effectId);
                      }
                    }}
                    role="button"
                    tabIndex={0}
                    aria-label={`Select ${effect.type} effect: ${effect.description}`}
                  />
                </Tooltip>
              );
            })}
          </div>
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              marginTop: tokens.spacingVerticalXS,
            }}
          >
            <Text size={200}>0:00</Text>
            <Text size={200}>
              {Math.floor(totalDuration / 60)}:{(totalDuration % 60).toFixed(0).padStart(2, '0')}
            </Text>
          </div>
        </div>
      )}

      {/* Effect list */}
      {filteredEffects.length === 0 ? (
        <div className={styles.emptyState}>
          <Speaker224Regular style={{ fontSize: '48px', color: tokens.colorNeutralForeground3 }} />
          <Text size={400} weight="semibold">
            No sound effects
          </Text>
          <Text size={300}>Sound effects will be suggested based on your script content</Text>
        </div>
      ) : (
        <div className={styles.effectList}>
          {filteredEffects.map((effect) => {
            const isEnabled = enabledEffects.has(effect.effectId);
            const isSelected = selectedEffectId === effect.effectId;
            const isPlaying = playingEffectId === effect.effectId;

            return (
              <Card
                key={effect.effectId}
                className={styles.effectCard}
                style={
                  isSelected
                    ? { borderColor: tokens.colorBrandBackground, borderWidth: '2px' }
                    : undefined
                }
                onClick={() => setSelectedEffectId(effect.effectId)}
              >
                <div className={styles.effectHeader}>
                  <div className={styles.effectInfo}>
                    <Text size={400} weight="semibold">
                      {effect.description}
                    </Text>
                    <div className={styles.effectMeta}>
                      <Badge
                        appearance="tint"
                        style={{
                          backgroundColor:
                            effectTypeColors[effect.type] || tokens.colorBrandBackground,
                          color: 'white',
                        }}
                      >
                        {effect.type}
                      </Badge>
                      <Text size={200}>@ {formatTimestamp(effect.timestamp)}</Text>
                      <Text size={200}>{formatTimestamp(effect.duration)}</Text>
                    </div>
                    <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                      {effect.purpose}
                    </Text>
                  </div>

                  <div className={styles.effectControls}>
                    <Switch
                      checked={isEnabled}
                      onChange={(_, data) => handleToggle(effect.effectId, data.checked)}
                      onClick={(e) => e.stopPropagation()}
                    />
                    <Tooltip content={isPlaying ? 'Stop' : 'Preview'} relationship="description">
                      <Button
                        appearance="subtle"
                        icon={isPlaying ? <Pause24Regular /> : <Play24Regular />}
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          handlePreview(effect.effectId);
                        }}
                      />
                    </Tooltip>
                    {onRemoveEffect && (
                      <Tooltip content="Remove" relationship="description">
                        <Button
                          appearance="subtle"
                          icon={<Delete24Regular />}
                          size="small"
                          onClick={(e) => {
                            e.stopPropagation();
                            onRemoveEffect(effect.effectId);
                          }}
                        />
                      </Tooltip>
                    )}
                  </div>
                </div>

                {isSelected && onVolumeChange && (
                  <div className={styles.volumeControl}>
                    <Text size={200}>Volume:</Text>
                    <Slider
                      className={styles.volumeSlider}
                      min={0}
                      max={100}
                      value={effect.volume}
                      onChange={(_, data) => onVolumeChange(effect.effectId, data.value)}
                    />
                    <Text size={200}>{effect.volume}%</Text>
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {/* Summary */}
      {effects.length > 0 && (
        <div style={{ marginTop: tokens.spacingVerticalM }}>
          <Text size={300} style={{ color: tokens.colorNeutralForeground3 }}>
            <Info16Regular /> {enabledEffects.size} of {effects.length} effects enabled
          </Text>
        </div>
      )}
    </div>
  );
};

export default SfxPreview;
