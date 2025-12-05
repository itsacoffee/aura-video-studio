/**
 * SpeedControls Component
 *
 * Speed adjustment panel for clip playback control.
 * Includes speed slider, reverse toggle, freeze frame picker.
 */

import {
  makeStyles,
  Text,
  Slider,
  Switch,
  Button,
  Tooltip,
  Divider,
} from '@fluentui/react-components';
import { ArrowRepeatAll24Regular } from '@fluentui/react-icons';
import { useCallback } from 'react';
import type { FC } from 'react';
import { useSpeedRampStore, type SpeedRampPreset } from '../../../stores/opencutSpeedRamp';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { openCutTokens } from '../../../styles/designTokens';

export interface SpeedControlsProps {
  clipId: string;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
    padding: openCutTokens.spacing.md,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  row: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: openCutTokens.spacing.sm,
  },
  sliderRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xxs,
  },
  sliderLabel: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  speedValue: {
    fontFamily: openCutTokens.typography.fontFamily.mono,
    fontSize: openCutTokens.typography.fontSize.sm,
  },
  presetGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, 1fr)',
    gap: openCutTokens.spacing.xs,
  },
  presetButton: {
    fontSize: openCutTokens.typography.fontSize.xs,
  },
});

const SPEED_PRESETS = [0.25, 0.5, 1, 2, 4, 8];

export const SpeedControls: FC<SpeedControlsProps> = ({ clipId, className }) => {
  const styles = useStyles();
  const { getClipById, setClipSpeed, toggleClipReverse, enableTimeRemap, setFreezeFrame } =
    useOpenCutTimelineStore();
  const { applySpeedRampPreset } = useSpeedRampStore();

  const clip = getClipById(clipId);

  const handleSpeedChange = useCallback(
    (_: unknown, data: { value: number }) => {
      setClipSpeed(clipId, data.value);
    },
    [clipId, setClipSpeed]
  );

  const handleReverseToggle = useCallback(() => {
    toggleClipReverse(clipId);
  }, [clipId, toggleClipReverse]);

  const handleTimeRemapToggle = useCallback(
    (_: unknown, data: { checked: boolean }) => {
      enableTimeRemap(clipId, data.checked);
    },
    [clipId, enableTimeRemap]
  );

  const handlePresetClick = useCallback(
    (speed: number) => {
      setClipSpeed(clipId, speed);
    },
    [clipId, setClipSpeed]
  );

  const handleRampPreset = useCallback(
    (preset: SpeedRampPreset) => {
      enableTimeRemap(clipId, true);
      applySpeedRampPreset(clipId, preset);
    },
    [clipId, enableTimeRemap, applySpeedRampPreset]
  );

  const handleClearFreezeFrame = useCallback(() => {
    setFreezeFrame(clipId, undefined);
  }, [clipId, setFreezeFrame]);

  if (!clip) return null;

  return (
    <div className={`${styles.container} ${className || ''}`}>
      <div className={styles.header}>
        <Text weight="semibold">Speed Controls</Text>
        <Tooltip content="Toggle reverse playback" relationship="label">
          <Button
            appearance={clip.reversed ? 'primary' : 'subtle'}
            icon={<ArrowRepeatAll24Regular />}
            onClick={handleReverseToggle}
            size="small"
          />
        </Tooltip>
      </div>

      <Divider />

      <div className={styles.sliderRow}>
        <div className={styles.sliderLabel}>
          <Text size={200}>Speed</Text>
          <Text className={styles.speedValue}>{clip.speed.toFixed(2)}x</Text>
        </div>
        <Slider min={0.1} max={16} step={0.1} value={clip.speed} onChange={handleSpeedChange} />
      </div>

      <div className={styles.presetGrid}>
        {SPEED_PRESETS.map((speed) => (
          <Button
            key={speed}
            appearance={clip.speed === speed ? 'primary' : 'subtle'}
            className={styles.presetButton}
            onClick={() => handlePresetClick(speed)}
            size="small"
          >
            {speed}x
          </Button>
        ))}
      </div>

      <Divider />

      <div className={styles.row}>
        <Text size={200}>Time Remapping</Text>
        <Switch checked={clip.timeRemapEnabled} onChange={handleTimeRemapToggle} />
      </div>

      {clip.timeRemapEnabled && (
        <>
          <Text size={200} weight="medium">
            Speed Ramp Presets
          </Text>
          <div className={styles.presetGrid}>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('smooth-slow-mo')}
              size="small"
            >
              Slow Mo
            </Button>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('smooth-speed-up')}
              size="small"
            >
              Speed Up
            </Button>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('dramatic-pause')}
              size="small"
            >
              Pause
            </Button>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('ramp-up-down')}
              size="small"
            >
              Ramp
            </Button>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('flash')}
              size="small"
            >
              Flash
            </Button>
            <Button
              appearance="subtle"
              className={styles.presetButton}
              onClick={() => handleRampPreset('reverse-ramp')}
              size="small"
            >
              Reverse
            </Button>
          </div>
        </>
      )}

      <Divider />

      <div className={styles.row}>
        <Text size={200}>Freeze Frame</Text>
        {clip.freezeFrameTime !== undefined ? (
          <Button appearance="subtle" onClick={handleClearFreezeFrame} size="small">
            Clear ({clip.freezeFrameTime.toFixed(2)}s)
          </Button>
        ) : (
          <Text size={200} style={{ opacity: 0.6 }}>
            Not set
          </Text>
        )}
      </div>
    </div>
  );
};

export default SpeedControls;
