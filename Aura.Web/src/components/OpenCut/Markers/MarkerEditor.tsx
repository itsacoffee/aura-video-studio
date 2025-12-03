/**
 * MarkerEditor Component
 *
 * A popover for editing marker properties including name, type, color, notes,
 * time, duration, and delete functionality.
 */

import {
  makeStyles,
  tokens,
  Input,
  Label,
  Select,
  Textarea,
  Button,
  Popover,
  PopoverTrigger,
  PopoverSurface,
  Text,
} from '@fluentui/react-components';
import { Delete24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC, ReactElement } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import type { Marker, MarkerType, MarkerColor } from '../../../types/opencut';
import { MarkerColorPicker } from './MarkerColorPicker';

export interface MarkerEditorProps {
  marker: Marker;
  trigger: ReactElement;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUpdate: (updates: Partial<Marker>) => void;
  onDelete: () => void;
}

const useStyles = makeStyles({
  surface: {
    padding: openCutTokens.spacing.md,
    minWidth: '280px',
    maxWidth: '320px',
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.sm,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: openCutTokens.spacing.xs,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xxs,
  },
  row: {
    display: 'flex',
    gap: openCutTokens.spacing.sm,
  },
  halfWidth: {
    flex: 1,
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    marginTop: openCutTokens.spacing.sm,
    paddingTop: openCutTokens.spacing.sm,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  timeInput: {
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
});

const MARKER_TYPES: { value: MarkerType; label: string }[] = [
  { value: 'standard', label: 'Standard' },
  { value: 'chapter', label: 'Chapter' },
  { value: 'todo', label: 'To-do' },
  { value: 'beat', label: 'Beat' },
];

function formatTimecode(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

function parseTimecode(timecode: string): number | null {
  const parts = timecode.split(':').map((p) => parseInt(p, 10));
  if (parts.length !== 3 || parts.some(isNaN)) return null;
  const [mins, secs, frames] = parts;
  return mins * 60 + secs + frames / 30;
}

export const MarkerEditor: FC<MarkerEditorProps> = ({
  marker,
  trigger,
  open,
  onOpenChange,
  onUpdate,
  onDelete,
}) => {
  const styles = useStyles();

  const [name, setName] = useState(marker.name);
  const [type, setType] = useState<MarkerType>(marker.type);
  const [color, setColor] = useState<MarkerColor>(marker.color);
  const [notes, setNotes] = useState(marker.notes || '');
  const [timeInput, setTimeInput] = useState(formatTimecode(marker.time));
  const [durationInput, setDurationInput] = useState(
    marker.duration ? formatTimecode(marker.duration) : ''
  );

  // Reset state when marker changes
  useEffect(() => {
    setName(marker.name);
    setType(marker.type);
    setColor(marker.color);
    setNotes(marker.notes || '');
    setTimeInput(formatTimecode(marker.time));
    setDurationInput(marker.duration ? formatTimecode(marker.duration) : '');
  }, [marker]);

  const handleNameChange = useCallback(
    (newName: string) => {
      setName(newName);
      onUpdate({ name: newName });
    },
    [onUpdate]
  );

  const handleTypeChange = useCallback(
    (newType: MarkerType) => {
      setType(newType);
      onUpdate({ type: newType });
    },
    [onUpdate]
  );

  const handleColorChange = useCallback(
    (newColor: MarkerColor) => {
      setColor(newColor);
      onUpdate({ color: newColor });
    },
    [onUpdate]
  );

  const handleNotesChange = useCallback(
    (newNotes: string) => {
      setNotes(newNotes);
      onUpdate({ notes: newNotes || undefined });
    },
    [onUpdate]
  );

  const handleTimeBlur = useCallback(() => {
    const time = parseTimecode(timeInput);
    if (time !== null && time >= 0) {
      onUpdate({ time });
    } else {
      setTimeInput(formatTimecode(marker.time));
    }
  }, [timeInput, marker.time, onUpdate]);

  const handleDurationBlur = useCallback(() => {
    if (!durationInput) {
      onUpdate({ duration: undefined });
      return;
    }
    const duration = parseTimecode(durationInput);
    if (duration !== null && duration >= 0) {
      onUpdate({ duration });
    } else {
      setDurationInput(marker.duration ? formatTimecode(marker.duration) : '');
    }
  }, [durationInput, marker.duration, onUpdate]);

  return (
    <Popover open={open} onOpenChange={(_, data) => onOpenChange(data.open)} positioning="above">
      <PopoverTrigger>{trigger}</PopoverTrigger>
      <PopoverSurface className={styles.surface}>
        <div className={styles.header}>
          <Text weight="semibold" size={400}>
            Edit Marker
          </Text>
        </div>

        {/* Name */}
        <div className={styles.field}>
          <Label htmlFor="marker-name" size="small">
            Name
          </Label>
          <Input
            id="marker-name"
            value={name}
            onChange={(_, data) => handleNameChange(data.value)}
            size="small"
          />
        </div>

        {/* Type and Color row */}
        <div className={styles.row}>
          <div className={mergeClasses(styles.field, styles.halfWidth)}>
            <Label htmlFor="marker-type" size="small">
              Type
            </Label>
            <Select
              id="marker-type"
              value={type}
              onChange={(_, data) => handleTypeChange(data.value as MarkerType)}
              size="small"
            >
              {MARKER_TYPES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </Select>
          </div>
        </div>

        {/* Color */}
        <div className={styles.field}>
          <Label size="small">Color</Label>
          <MarkerColorPicker selectedColor={color} onChange={handleColorChange} />
        </div>

        {/* Time and Duration row */}
        <div className={styles.row}>
          <div className={mergeClasses(styles.field, styles.halfWidth)}>
            <Label htmlFor="marker-time" size="small">
              Time
            </Label>
            <Input
              id="marker-time"
              value={timeInput}
              onChange={(_, data) => setTimeInput(data.value)}
              onBlur={handleTimeBlur}
              size="small"
              className={styles.timeInput}
              placeholder="00:00:00"
            />
          </div>
          <div className={mergeClasses(styles.field, styles.halfWidth)}>
            <Label htmlFor="marker-duration" size="small">
              Duration
            </Label>
            <Input
              id="marker-duration"
              value={durationInput}
              onChange={(_, data) => setDurationInput(data.value)}
              onBlur={handleDurationBlur}
              size="small"
              className={styles.timeInput}
              placeholder="Optional"
            />
          </div>
        </div>

        {/* Notes */}
        <div className={styles.field}>
          <Label htmlFor="marker-notes" size="small">
            Notes
          </Label>
          <Textarea
            id="marker-notes"
            value={notes}
            onChange={(_, data) => handleNotesChange(data.value)}
            size="small"
            resize="vertical"
            placeholder="Add notes..."
          />
        </div>

        {/* Delete button */}
        <div className={styles.footer}>
          <Button appearance="subtle" icon={<Delete24Regular />} onClick={onDelete} size="small">
            Delete Marker
          </Button>
        </div>
      </PopoverSurface>
    </Popover>
  );
};

// Helper for mergeClasses compatibility
function mergeClasses(...classes: (string | undefined | false)[]): string {
  return classes.filter(Boolean).join(' ');
}

export default MarkerEditor;
