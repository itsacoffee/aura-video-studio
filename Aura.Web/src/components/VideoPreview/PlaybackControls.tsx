/**
 * Professional Playback Controls Component
 * 
 * Provides comprehensive playback controls including:
 * - Variable speed selection
 * - Preview quality settings
 * - Loop mode controls
 * - Performance metrics display
 */

import { memo } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Menu,
  MenuTrigger,
  MenuList,
  MenuItem,
  MenuPopover,
  Text,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Previous24Regular,
  Next24Regular,
  ArrowRepeatAll24Regular,
  VideoClip24Regular,
  Gauge24Regular,
} from '@fluentui/react-icons';
import type { PlaybackSpeed, PlaybackQuality } from '../../services/playbackEngine';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
    flexWrap: 'wrap',
  },
  controlGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  separator: {
    width: '1px',
    height: '24px',
    backgroundColor: tokens.colorNeutralStroke2,
    marginLeft: tokens.spacingHorizontalS,
    marginRight: tokens.spacingHorizontalS,
  },
  metricsGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginLeft: 'auto',
  },
  metricItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'flex-start',
    gap: tokens.spacingVerticalXXS,
  },
  metricLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
  },
  metricValue: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    fontFamily: 'monospace',
  },
  activeButton: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
});

interface PlaybackControlsProps {
  isPlaying: boolean;
  playbackSpeed: PlaybackSpeed;
  quality: PlaybackQuality;
  isLooping: boolean;
  hasInOutPoints: boolean;
  droppedFrames?: number;
  currentFPS?: number;
  targetFPS?: number;
  onPlay: () => void;
  onPause: () => void;
  onStepBackward: () => void;
  onStepForward: () => void;
  onSpeedChange: (speed: PlaybackSpeed) => void;
  onQualityChange: (quality: PlaybackQuality) => void;
  onToggleLoop: () => void;
  disabled?: boolean;
}

export const PlaybackControls = memo(function PlaybackControls({
  isPlaying,
  playbackSpeed,
  quality,
  isLooping,
  hasInOutPoints,
  droppedFrames = 0,
  currentFPS = 0,
  targetFPS = 30,
  onPlay,
  onPause,
  onStepBackward,
  onStepForward,
  onSpeedChange,
  onQualityChange,
  onToggleLoop,
  disabled = false,
}: PlaybackControlsProps) {
  const styles = useStyles();

  const getSpeedLabel = (speed: PlaybackSpeed): string => {
    return `${speed * 100}%`;
  };

  const getQualityLabel = (q: PlaybackQuality): string => {
    switch (q) {
      case 'full':
        return 'Full Quality';
      case 'half':
        return 'Half (50%)';
      case 'quarter':
        return 'Quarter (25%)';
      default:
        return 'Full Quality';
    }
  };

  const getQualityIcon = (q: PlaybackQuality): string => {
    switch (q) {
      case 'full':
        return '🔷';
      case 'half':
        return '🔶';
      case 'quarter':
        return '🔸';
      default:
        return '🔷';
    }
  };

  const speedOptions: PlaybackSpeed[] = [0.25, 0.5, 1.0, 2.0, 4.0];
  const qualityOptions: PlaybackQuality[] = ['quarter', 'half', 'full'];

  const fpsStatus = currentFPS >= targetFPS * 0.9 ? 'success' : 
                    currentFPS >= targetFPS * 0.7 ? 'warning' : 'error';

  return (
    <div className={styles.container}>
      {/* Transport Controls */}
      <div className={styles.controlGroup}>
        <Tooltip content="Previous Frame (←)" relationship="label">
          <Button
            appearance="subtle"
            icon={<Previous24Regular />}
            onClick={onStepBackward}
            disabled={disabled}
            aria-label="Previous frame"
            size="small"
          />
        </Tooltip>

        <Tooltip content={isPlaying ? "Pause (Space)" : "Play (Space)"} relationship="label">
          <Button
            appearance="primary"
            icon={isPlaying ? <Pause24Regular /> : <Play24Regular />}
            onClick={isPlaying ? onPause : onPlay}
            disabled={disabled}
            aria-label={isPlaying ? 'Pause' : 'Play'}
          />
        </Tooltip>

        <Tooltip content="Next Frame (→)" relationship="label">
          <Button
            appearance="subtle"
            icon={<Next24Regular />}
            onClick={onStepForward}
            disabled={disabled}
            aria-label="Next frame"
            size="small"
          />
        </Tooltip>
      </div>

      <div className={styles.separator} />

      {/* Speed Control */}
      <div className={styles.controlGroup}>
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Tooltip content="Playback Speed" relationship="label">
              <Button
                appearance="subtle"
                icon={<Gauge24Regular />}
                disabled={disabled}
                size="small"
              >
                {getSpeedLabel(playbackSpeed)}
              </Button>
            </Tooltip>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {speedOptions.map((speed) => (
                <MenuItem
                  key={speed}
                  onClick={() => onSpeedChange(speed)}
                  icon={playbackSpeed === speed ? '✓' : undefined}
                >
                  {getSpeedLabel(speed)}
                </MenuItem>
              ))}
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>

      {/* Quality Control */}
      <div className={styles.controlGroup}>
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <Tooltip content="Preview Quality" relationship="label">
              <Button
                appearance="subtle"
                icon={<VideoClip24Regular />}
                disabled={disabled}
                size="small"
              >
                {getQualityIcon(quality)} {quality === 'full' ? 'Full' : quality === 'half' ? '50%' : '25%'}
              </Button>
            </Tooltip>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {qualityOptions.map((q) => (
                <MenuItem
                  key={q}
                  onClick={() => onQualityChange(q)}
                  icon={quality === q ? '✓' : undefined}
                >
                  {getQualityLabel(q)}
                </MenuItem>
              ))}
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>

      <div className={styles.separator} />

      {/* Loop Control */}
      <div className={styles.controlGroup}>
        <Tooltip 
          content={hasInOutPoints ? "Loop between In/Out points" : "Loop entire video"} 
          relationship="label"
        >
          <Button
            appearance={isLooping ? 'primary' : 'subtle'}
            icon={<ArrowRepeatAll24Regular />}
            onClick={onToggleLoop}
            disabled={disabled}
            aria-label="Toggle loop"
            size="small"
            className={isLooping ? styles.activeButton : undefined}
          />
        </Tooltip>
      </div>

      {/* Performance Metrics */}
      <div className={styles.metricsGroup}>
        <div className={styles.metricItem}>
          <Text className={styles.metricLabel}>FPS</Text>
          <Badge appearance={fpsStatus === 'success' ? 'filled' : 'outline'} color={
            fpsStatus === 'success' ? 'success' : 
            fpsStatus === 'warning' ? 'warning' : 'danger'
          }>
            <Text className={styles.metricValue}>
              {currentFPS}/{targetFPS}
            </Text>
          </Badge>
        </div>

        {droppedFrames > 0 && (
          <div className={styles.metricItem}>
            <Text className={styles.metricLabel}>Dropped</Text>
            <Badge appearance="outline" color="danger">
              <Text className={styles.metricValue}>{droppedFrames}</Text>
            </Badge>
          </div>
        )}
      </div>
    </div>
  );
});
