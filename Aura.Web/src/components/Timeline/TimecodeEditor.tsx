/**
 * Timecode Editor Component
 * Provides frame-accurate timecode input with validation
 * Supports HH:MM:SS:FF format
 */

import { Input, makeStyles, tokens } from '@fluentui/react-components';
import { useState, useRef, useCallback, useEffect } from 'react';
import { formatTimecode, secondsToFrames, framesToSeconds } from '../../services/timelineEngine';

const useStyles = makeStyles({
  root: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  input: {
    fontFamily: 'monospace',
    width: '120px',
    fontSize: tokens.fontSizeBase300,
  },
});

interface TimecodeEditorProps {
  value: number;
  onChange: (seconds: number) => void;
  frameRate?: number;
  min?: number;
  max?: number;
  disabled?: boolean;
  placeholder?: string;
}

/**
 * Parse timecode string (HH:MM:SS:FF) to seconds
 */
function parseTimecode(timecode: string, frameRate: number = 30): number | null {
  const parts = timecode.split(':');
  if (parts.length !== 4) return null;

  const [hoursStr, minsStr, secsStr, framesStr] = parts;
  const hours = parseInt(hoursStr, 10);
  const minutes = parseInt(minsStr, 10);
  const seconds = parseInt(secsStr, 10);
  const frames = parseInt(framesStr, 10);

  if (
    isNaN(hours) ||
    isNaN(minutes) ||
    isNaN(seconds) ||
    isNaN(frames) ||
    hours < 0 ||
    minutes < 0 ||
    minutes >= 60 ||
    seconds < 0 ||
    seconds >= 60 ||
    frames < 0 ||
    frames >= frameRate
  ) {
    return null;
  }

  const totalSeconds = hours * 3600 + minutes * 60 + seconds;
  const frameSeconds = frames / frameRate;

  return totalSeconds + frameSeconds;
}

/**
 * Validate and normalize partial timecode input
 */
function normalizeTimecodeInput(input: string): string {
  let normalized = input.replace(/[^0-9:]/g, '');

  const parts = normalized.split(':');
  if (parts.length > 4) {
    normalized = parts.slice(0, 4).join(':');
  }

  return normalized;
}

export function TimecodeEditor({
  value,
  onChange,
  frameRate = 30,
  min = 0,
  max,
  disabled = false,
  placeholder = '00:00:00:00',
}: TimecodeEditorProps) {
  const styles = useStyles();
  const [inputValue, setInputValue] = useState(() => formatTimecode(value, frameRate));
  const [isEditing, setIsEditing] = useState(false);
  const [hasError, setHasError] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!isEditing) {
      setInputValue(formatTimecode(value, frameRate));
      setHasError(false);
    }
  }, [value, frameRate, isEditing]);

  const handleChange = useCallback(
    (newValue: string) => {
      const normalized = normalizeTimecodeInput(newValue);
      setInputValue(normalized);

      const parsed = parseTimecode(normalized, frameRate);
      if (parsed !== null) {
        if ((min !== undefined && parsed < min) || (max !== undefined && parsed > max)) {
          setHasError(true);
        } else {
          setHasError(false);
        }
      } else if (normalized.length > 0) {
        setHasError(true);
      }
    },
    [frameRate, min, max]
  );

  const handleBlur = useCallback(() => {
    setIsEditing(false);
    const parsed = parseTimecode(inputValue, frameRate);

    if (parsed !== null) {
      let finalValue = parsed;

      if (min !== undefined && finalValue < min) {
        finalValue = min;
      }
      if (max !== undefined && finalValue > max) {
        finalValue = max;
      }

      onChange(finalValue);
      setInputValue(formatTimecode(finalValue, frameRate));
      setHasError(false);
    } else {
      setInputValue(formatTimecode(value, frameRate));
      setHasError(false);
    }
  }, [inputValue, frameRate, value, onChange, min, max]);

  const handleFocus = useCallback(() => {
    setIsEditing(true);
    if (inputRef.current) {
      inputRef.current.select();
    }
  }, []);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLInputElement>) => {
      if (event.key === 'Enter') {
        event.currentTarget.blur();
      } else if (event.key === 'Escape') {
        setInputValue(formatTimecode(value, frameRate));
        setHasError(false);
        event.currentTarget.blur();
      } else if (event.key === 'ArrowUp') {
        event.preventDefault();
        const frames = secondsToFrames(value, frameRate);
        const newSeconds = framesToSeconds(frames + 1, frameRate);
        onChange(newSeconds);
      } else if (event.key === 'ArrowDown') {
        event.preventDefault();
        const frames = secondsToFrames(value, frameRate);
        const newSeconds = framesToSeconds(Math.max(0, frames - 1), frameRate);
        onChange(newSeconds);
      }
    },
    [value, frameRate, onChange]
  );

  return (
    <div className={styles.root}>
      <Input
        ref={inputRef}
        className={styles.input}
        value={inputValue}
        onChange={(_, data) => handleChange(data.value)}
        onBlur={handleBlur}
        onFocus={handleFocus}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        placeholder={placeholder}
        aria-label="Timecode"
        title="Enter timecode in HH:MM:SS:FF format. Use arrow keys to adjust frame by frame."
        appearance={hasError ? 'filled-darker' : undefined}
      />
    </div>
  );
}
