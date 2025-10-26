import { makeStyles, tokens } from '@fluentui/react-components';
import {
  formatTimecode,
  formatFrameNumber,
  formatSeconds,
  calculateRulerInterval,
  TimelineDisplayMode,
  FRAME_RATE,
} from '../../services/timelineEngine';

const useStyles = makeStyles({
  ruler: {
    height: '30px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    position: 'sticky',
    top: 0,
    backgroundColor: tokens.colorNeutralBackground1,
    zIndex: 10,
    display: 'flex',
    overflow: 'hidden',
    userSelect: 'none',
  },
  rulerContent: {
    position: 'relative',
    height: '100%',
    flex: 1,
  },
  majorTick: {
    position: 'absolute',
    bottom: 0,
    width: '1px',
    height: '12px',
    backgroundColor: tokens.colorNeutralStroke1,
  },
  minorTick: {
    position: 'absolute',
    bottom: 0,
    width: '1px',
    height: '6px',
    backgroundColor: tokens.colorNeutralStroke2,
  },
  label: {
    position: 'absolute',
    top: '2px',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    transform: 'translateX(-50%)',
    whiteSpace: 'nowrap',
  },
  trackLabelSpace: {
    width: '100px',
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
    flexShrink: 0,
  },
});

interface TimelineRulerProps {
  width: number;
  pixelsPerSecond: number;
  maxTime: number;
  displayMode?: TimelineDisplayMode;
  frameRate?: number;
  onTimeClick?: (time: number) => void;
}

export function TimelineRuler({
  width,
  pixelsPerSecond,
  maxTime,
  displayMode = TimelineDisplayMode.TIMECODE,
  frameRate = FRAME_RATE,
  onTimeClick,
}: TimelineRulerProps) {
  const styles = useStyles();

  const { majorInterval, minorInterval } = calculateRulerInterval(
    pixelsPerSecond,
    displayMode,
    frameRate
  );

  const formatTime = (time: number): string => {
    switch (displayMode) {
      case TimelineDisplayMode.TIMECODE:
        return formatTimecode(time, frameRate);
      case TimelineDisplayMode.FRAMES:
        return formatFrameNumber(time, frameRate);
      case TimelineDisplayMode.SECONDS:
        return formatSeconds(time);
      default:
        return formatTimecode(time, frameRate);
    }
  };

  const renderTicks = () => {
    const ticks = [];

    // Render major ticks and labels
    for (let time = 0; time <= maxTime; time += majorInterval) {
      const left = time * pixelsPerSecond;
      ticks.push(
        <div key={`major-${time}`} className={styles.majorTick} style={{ left: `${left}px` }} />
      );
      ticks.push(
        <div key={`label-${time}`} className={styles.label} style={{ left: `${left}px` }}>
          {formatTime(time)}
        </div>
      );
    }

    // Render minor ticks
    for (let time = 0; time <= maxTime; time += minorInterval) {
      if (time % majorInterval !== 0) {
        const left = time * pixelsPerSecond;
        ticks.push(
          <div key={`minor-${time}`} className={styles.minorTick} style={{ left: `${left}px` }} />
        );
      }
    }

    return ticks;
  };

  const handleClick = (e: React.MouseEvent) => {
    if (!onTimeClick) return;
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const time = Math.max(0, x / pixelsPerSecond);
    onTimeClick(time);
  };

  return (
    <div className={styles.ruler}>
      <div className={styles.trackLabelSpace} />
      <div
        className={styles.rulerContent}
        style={{ width: `${width}px` }}
        onClick={handleClick}
        role="slider"
        aria-label="Timeline ruler"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            handleClick(e as unknown as React.MouseEvent);
          }
        }}
      >
        {renderTicks()}
      </div>
    </div>
  );
}
