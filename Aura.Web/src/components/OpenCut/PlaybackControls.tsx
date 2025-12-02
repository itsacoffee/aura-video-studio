/**
 * PlaybackControls Component
 *
 * Refined playback controls following Apple HIG principles.
 * Features logical grouping, proper spacing, and 44px touch targets.
 */

import { makeStyles, tokens, Button, Tooltip, Slider, Text } from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Previous24Regular,
  Next24Regular,
  Speaker224Regular,
  SpeakerMute24Regular,
  FullScreenMaximize24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useOpenCutProjectStore } from '../../stores/opencutProject';

export interface PlaybackControlsProps {
  onFullscreen?: () => void;
  onSettings?: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalXL,
    padding: `${tokens.spacingVerticalL} ${tokens.spacingHorizontalXXL}`,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground2,
    minHeight: '64px',
  },
  transportGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  timeGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    minWidth: '160px',
    justifyContent: 'center',
  },
  timeText: {
    fontFamily: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, monospace',
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightMedium,
    letterSpacing: '0.02em',
  },
  timeSeparator: {
    color: tokens.colorNeutralForeground3,
    padding: `0 ${tokens.spacingHorizontalXS}`,
  },
  volumeGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    paddingLeft: tokens.spacingHorizontalL,
    borderLeft: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  volumeSlider: {
    width: '100px',
  },
  screenGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    paddingLeft: tokens.spacingHorizontalL,
    borderLeft: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  controlButton: {
    minWidth: '44px',
    minHeight: '44px',
  },
  playButton: {
    minWidth: '48px',
    minHeight: '48px',
    borderRadius: tokens.borderRadiusCircular,
  },
});

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

export const PlaybackControls: FC<PlaybackControlsProps> = ({ onFullscreen, onSettings }) => {
  const styles = useStyles();
  const playbackStore = useOpenCutPlaybackStore();
  const projectStore = useOpenCutProjectStore();
  const fps = projectStore.activeProject?.fps || 30;

  return (
    <div className={styles.container}>
      {/* Transport Controls */}
      <div className={styles.transportGroup}>
        <Tooltip content="Previous frame (←)" relationship="label">
          <Button
            appearance="subtle"
            icon={<Previous24Regular />}
            className={styles.controlButton}
            onClick={() => playbackStore.skipBackward(1 / fps)}
            aria-label="Previous frame"
          />
        </Tooltip>

        <Tooltip
          content={playbackStore.isPlaying ? 'Pause (Space)' : 'Play (Space)'}
          relationship="label"
        >
          <Button
            appearance="primary"
            icon={playbackStore.isPlaying ? <Pause24Regular /> : <Play24Regular />}
            className={styles.playButton}
            onClick={playbackStore.toggle}
            aria-label={playbackStore.isPlaying ? 'Pause' : 'Play'}
          />
        </Tooltip>

        <Tooltip content="Next frame (→)" relationship="label">
          <Button
            appearance="subtle"
            icon={<Next24Regular />}
            className={styles.controlButton}
            onClick={() => playbackStore.skipForward(1 / fps)}
            aria-label="Next frame"
          />
        </Tooltip>
      </div>

      {/* Time Display */}
      <div className={styles.timeGroup}>
        <Text className={styles.timeText}>{formatTime(playbackStore.currentTime)}</Text>
        <Text className={styles.timeSeparator}>/</Text>
        <Text className={styles.timeText} style={{ color: tokens.colorNeutralForeground3 }}>
          {formatTime(playbackStore.duration)}
        </Text>
      </div>

      {/* Volume Controls */}
      <div className={styles.volumeGroup}>
        <Tooltip content={playbackStore.muted ? 'Unmute (M)' : 'Mute (M)'} relationship="label">
          <Button
            appearance="subtle"
            icon={
              playbackStore.muted || playbackStore.volume === 0 ? (
                <SpeakerMute24Regular />
              ) : (
                <Speaker224Regular />
              )
            }
            className={styles.controlButton}
            onClick={playbackStore.toggleMute}
            aria-label={playbackStore.muted ? 'Unmute' : 'Mute'}
          />
        </Tooltip>
        <Slider
          className={styles.volumeSlider}
          min={0}
          max={100}
          value={playbackStore.volume * 100}
          onChange={(_, data) => playbackStore.setVolume(data.value / 100)}
          aria-label="Volume"
        />
      </div>

      {/* Screen Controls */}
      <div className={styles.screenGroup}>
        {onSettings && (
          <Tooltip content="Playback settings" relationship="label">
            <Button
              appearance="subtle"
              icon={<Settings24Regular />}
              className={styles.controlButton}
              onClick={onSettings}
              aria-label="Playback settings"
            />
          </Tooltip>
        )}
        {onFullscreen && (
          <Tooltip content="Enter fullscreen (F)" relationship="label">
            <Button
              appearance="subtle"
              icon={<FullScreenMaximize24Regular />}
              className={styles.controlButton}
              onClick={onFullscreen}
              aria-label="Enter fullscreen"
            />
          </Tooltip>
        )}
      </div>
    </div>
  );
};

export default PlaybackControls;
