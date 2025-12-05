/**
 * CaptionEditor Component
 *
 * Caption text and timing editor:
 * - Text input with character count
 * - Start/End time inputs with timecode format
 * - Duration display
 * - Style override controls
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  Label,
  Textarea,
  Button,
  Tooltip,
} from '@fluentui/react-components';
import { Cut24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { openCutTokens } from '../../../styles/designTokens';

export interface CaptionEditorProps {
  captionId: string;
}

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  label: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground2,
  },
  textareaWrapper: {
    position: 'relative',
  },
  textarea: {
    width: '100%',
    minHeight: '80px',
    resize: 'vertical',
  },
  charCount: {
    position: 'absolute',
    bottom: tokens.spacingVerticalS,
    right: tokens.spacingHorizontalS,
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground4,
  },
  timingRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
  durationDisplay: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: openCutTokens.radius.sm,
  },
  durationLabel: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  durationValue: {
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground1,
  },
  actionsRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  timeInput: {
    width: '100%',
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
});

function formatTimecode(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 1000);
  return `${mins}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
}

function parseTimecode(timecode: string): number | null {
  // Accept formats like "1:23.456", "1:23", "83.456", "83"
  const parts = timecode.trim().split(':');
  let totalSeconds = 0;

  if (parts.length === 2) {
    // MM:SS.mmm or MM:SS
    const mins = parseInt(parts[0], 10);
    const secParts = parts[1].split('.');
    const secs = parseInt(secParts[0], 10);
    const ms = secParts[1] ? parseInt(secParts[1].padEnd(3, '0'), 10) : 0;
    if (isNaN(mins) || isNaN(secs) || isNaN(ms)) return null;
    // Validate ranges: minutes can be any positive number, seconds must be 0-59
    if (mins < 0 || secs < 0 || secs > 59 || ms < 0 || ms > 999) return null;
    totalSeconds = mins * 60 + secs + ms / 1000;
  } else if (parts.length === 1) {
    // SS.mmm or SS (accepts seconds >= 60 for convenience)
    const secParts = parts[0].split('.');
    const secs = parseInt(secParts[0], 10);
    const ms = secParts[1] ? parseInt(secParts[1].padEnd(3, '0'), 10) : 0;
    if (isNaN(secs) || isNaN(ms)) return null;
    if (secs < 0 || ms < 0 || ms > 999) return null;
    totalSeconds = secs + ms / 1000;
  } else {
    return null;
  }

  return totalSeconds >= 0 ? totalSeconds : null;
}

export const CaptionEditor: FC<CaptionEditorProps> = ({ captionId }) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();

  const caption = captionsStore.getCaptionById(captionId);

  const [text, setText] = useState(caption?.text ?? '');
  const [startTimeStr, setStartTimeStr] = useState(formatTimecode(caption?.startTime ?? 0));
  const [endTimeStr, setEndTimeStr] = useState(formatTimecode(caption?.endTime ?? 0));

  // Update local state when caption changes
  useEffect(() => {
    if (caption) {
      setText(caption.text);
      setStartTimeStr(formatTimecode(caption.startTime));
      setEndTimeStr(formatTimecode(caption.endTime));
    }
  }, [caption]);

  const handleTextChange = useCallback(
    (value: string) => {
      setText(value);
      captionsStore.updateCaption(captionId, { text: value });
    },
    [captionId, captionsStore]
  );

  const handleStartTimeBlur = useCallback(() => {
    const parsed = parseTimecode(startTimeStr);
    if (parsed !== null && caption) {
      // Ensure start time is before end time
      const newStart = Math.min(parsed, caption.endTime - 0.1);
      captionsStore.setCaptionTiming(captionId, newStart, caption.endTime);
      setStartTimeStr(formatTimecode(newStart));
    } else if (caption) {
      setStartTimeStr(formatTimecode(caption.startTime));
    }
  }, [startTimeStr, caption, captionId, captionsStore]);

  const handleEndTimeBlur = useCallback(() => {
    const parsed = parseTimecode(endTimeStr);
    if (parsed !== null && caption) {
      // Ensure end time is after start time
      const newEnd = Math.max(parsed, caption.startTime + 0.1);
      captionsStore.setCaptionTiming(captionId, caption.startTime, newEnd);
      setEndTimeStr(formatTimecode(newEnd));
    } else if (caption) {
      setEndTimeStr(formatTimecode(caption.endTime));
    }
  }, [endTimeStr, caption, captionId, captionsStore]);

  const handleSplit = useCallback(() => {
    const currentTime = playbackStore.currentTime;
    if (caption && currentTime > caption.startTime && currentTime < caption.endTime) {
      captionsStore.splitCaption(captionId, currentTime);
    }
  }, [caption, captionId, playbackStore.currentTime, captionsStore]);

  if (!caption) {
    return (
      <div className={styles.root}>
        <Text>Caption not found</Text>
      </div>
    );
  }

  const duration = caption.endTime - caption.startTime;
  const canSplit =
    playbackStore.currentTime > caption.startTime && playbackStore.currentTime < caption.endTime;

  return (
    <div className={styles.root}>
      {/* Text Field */}
      <div className={styles.field}>
        <Label className={styles.label}>Text</Label>
        <div className={styles.textareaWrapper}>
          <Textarea
            value={text}
            onChange={(_, data) => handleTextChange(data.value)}
            className={styles.textarea}
          />
          <span className={styles.charCount}>{text.length} chars</span>
        </div>
      </div>

      {/* Timing Fields */}
      <div className={styles.timingRow}>
        <div className={styles.field}>
          <Label className={styles.label}>Start Time</Label>
          <Input
            value={startTimeStr}
            onChange={(_, data) => setStartTimeStr(data.value)}
            onBlur={handleStartTimeBlur}
            className={styles.timeInput}
          />
        </div>
        <div className={styles.field}>
          <Label className={styles.label}>End Time</Label>
          <Input
            value={endTimeStr}
            onChange={(_, data) => setEndTimeStr(data.value)}
            onBlur={handleEndTimeBlur}
            className={styles.timeInput}
          />
        </div>
      </div>

      {/* Duration Display */}
      <div className={styles.durationDisplay}>
        <span className={styles.durationLabel}>Duration</span>
        <span className={styles.durationValue}>{duration.toFixed(2)}s</span>
      </div>

      {/* Actions */}
      <div className={styles.actionsRow}>
        <Tooltip content="Split at playhead" relationship="label">
          <Button
            appearance="subtle"
            size="small"
            icon={<Cut24Regular />}
            disabled={!canSplit}
            onClick={handleSplit}
          >
            Split
          </Button>
        </Tooltip>
      </div>
    </div>
  );
};

export default CaptionEditor;
