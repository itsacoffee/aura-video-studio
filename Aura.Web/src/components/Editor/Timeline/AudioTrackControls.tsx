/**
 * Audio track controls component with mute/solo/volume
 */

import {
  makeStyles,
  tokens,
  Button,
  Slider,
  Label,
} from '@fluentui/react-components';
import {
  Speaker224Regular,
  SpeakerMute24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    width: '120px',
  },
  trackName: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalXS,
  },
  buttonRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalXXS,
    justifyContent: 'space-between',
  },
  button: {
    minWidth: 'auto',
    padding: '4px',
  },
  volumeControl: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  volumeLabel: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  vuMeter: {
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    overflow: 'hidden',
    marginTop: tokens.spacingVerticalXXS,
  },
  vuMeterBar: {
    height: '100%',
    transition: 'width 0.1s ease-out',
  },
  locked: {
    opacity: 0.5,
    pointerEvents: 'none',
  },
});

export interface AudioTrackControlsProps {
  trackName: string;
  trackType: 'narration' | 'music' | 'sfx';
  muted?: boolean;
  solo?: boolean;
  volume?: number; // 0-200 (100 = default)
  pan?: number; // -100 to 100 (0 = center)
  locked?: boolean;
  audioLevel?: number; // 0-100 for VU meter
  onMuteToggle?: () => void;
  onSoloToggle?: () => void;
  onVolumeChange?: (volume: number) => void;
  onPanChange?: (pan: number) => void;
  onLockToggle?: () => void;
}

export function AudioTrackControls({
  trackName,
  trackType,
  muted = false,
  solo = false,
  volume = 100,
  pan = 0,
  locked = false,
  audioLevel = 0,
  onMuteToggle,
  onSoloToggle,
  onVolumeChange,
  onPanChange,
  onLockToggle,
}: AudioTrackControlsProps) {
  const styles = useStyles();

  // Get VU meter color based on level
  const getVuMeterColor = (level: number): string => {
    if (level >= 90) return tokens.colorPaletteRedBackground3;
    if (level >= 70) return tokens.colorPaletteYellowBackground3;
    return tokens.colorPaletteGreenBackground3;
  };

  // Convert volume to dB display
  const volumeToDB = (vol: number): string => {
    if (vol === 0) return '-∞ dB';
    const db = 20 * Math.log10(vol / 100);
    return `${db > 0 ? '+' : ''}${db.toFixed(1)} dB`;
  };

  return (
    <div className={`${styles.container} ${locked ? styles.locked : ''}`}>
      <div className={styles.trackName}>{trackName}</div>

      {/* Mute/Solo/Lock buttons */}
      <div className={styles.buttonRow}>
        <Button
          size="small"
          appearance={muted ? 'primary' : 'subtle'}
          icon={muted ? <SpeakerMute24Regular /> : <Speaker224Regular />}
          onClick={onMuteToggle}
          disabled={locked}
          className={styles.button}
          title="Mute (M)"
        />
        <Button
          size="small"
          appearance={solo ? 'primary' : 'subtle'}
          onClick={onSoloToggle}
          disabled={locked}
          className={styles.button}
          title="Solo (S)"
        >
          S
        </Button>
        <Button
          size="small"
          appearance="subtle"
          icon={locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
          onClick={onLockToggle}
          className={styles.button}
          title="Lock track"
        />
      </div>

      {/* Volume control */}
      <div className={styles.volumeControl}>
        <Label size="small">Volume</Label>
        <Slider
          min={0}
          max={200}
          value={volume}
          onChange={(_, data) => onVolumeChange?.(data.value)}
          disabled={locked || muted}
          size="small"
        />
        <div className={styles.volumeLabel}>{volumeToDB(volume)}</div>
      </div>

      {/* Pan control */}
      <div className={styles.volumeControl}>
        <Label size="small">Pan</Label>
        <Slider
          min={-100}
          max={100}
          value={pan}
          onChange={(_, data) => onPanChange?.(data.value)}
          disabled={locked || muted}
          size="small"
        />
        <div className={styles.volumeLabel}>
          {pan === 0 ? 'Center' : pan < 0 ? `${Math.abs(pan)}% L` : `${pan}% R`}
        </div>
      </div>

      {/* VU Meter */}
      <div className={styles.vuMeter}>
        <div
          className={styles.vuMeterBar}
          style={{
            width: `${audioLevel}%`,
            backgroundColor: getVuMeterColor(audioLevel),
          }}
        />
      </div>

      {/* Track type indicator */}
      <div
        className={styles.volumeLabel}
        style={{
          marginTop: tokens.spacingVerticalXS,
          textTransform: 'uppercase',
          fontSize: tokens.fontSizeBase100,
        }}
      >
        {trackType}
      </div>
    </div>
  );
}
