/**
 * MarkerEditor Component
 *
 * Popover panel for editing marker properties including name, type,
 * color, notes, time, and duration. Provides a complete interface
 * for marker management.
 */

import {
  makeStyles,
  tokens,
  Button,
  Input,
  Textarea,
  Dropdown,
  Option,
  Field,
  Divider,
  Checkbox,
} from '@fluentui/react-components';
import { Delete24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import type { FC, ChangeEvent } from 'react';
import { useState, useCallback, useEffect } from 'react';
import type { Marker, MarkerType } from '../../../types/opencut';
import { MarkerColorPicker } from './MarkerColorPicker';

export interface MarkerEditorProps {
  marker: Marker;
  onUpdate: (updates: Partial<Marker>) => void;
  onDelete: () => void;
  onClose: () => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingHorizontalM,
    minWidth: '280px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
  },
  headerTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    flex: 1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-end',
  },
  field: {
    flex: 1,
  },
  smallField: {
    width: '100px',
  },
  timeInput: {
    fontFamily: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, monospace',
  },
  deleteButton: {
    color: tokens.colorPaletteRedForeground1,
    ':hover': {
      color: tokens.colorPaletteRedForeground2,
      backgroundColor: tokens.colorPaletteRedBackground1,
    },
  },
});

const MARKER_TYPES: { value: MarkerType; label: string }[] = [
  { value: 'standard', label: 'Standard' },
  { value: 'chapter', label: 'Chapter' },
  { value: 'todo', label: 'To-Do' },
  { value: 'beat', label: 'Beat' },
];

function formatTimeForInput(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

function parseTimeFromInput(timeString: string): number | null {
  const parts = timeString.split(':').map((p) => parseInt(p, 10));
  if (parts.length !== 3 || parts.some(isNaN)) return null;
  const [mins, secs, frames] = parts;
  return mins * 60 + secs + frames / 30;
}

export const MarkerEditor: FC<MarkerEditorProps> = ({ marker, onUpdate, onDelete, onClose }) => {
  const styles = useStyles();
  const [name, setName] = useState(marker.name);
  const [notes, setNotes] = useState(marker.notes || '');
  const [timeValue, setTimeValue] = useState(formatTimeForInput(marker.time));
  const [durationValue, setDurationValue] = useState(
    marker.duration ? formatTimeForInput(marker.duration) : ''
  );

  // Reset form when marker changes
  useEffect(() => {
    setName(marker.name);
    setNotes(marker.notes || '');
    setTimeValue(formatTimeForInput(marker.time));
    setDurationValue(marker.duration ? formatTimeForInput(marker.duration) : '');
  }, [marker]);

  const handleNameChange = useCallback(
    (value: string) => {
      setName(value);
      onUpdate({ name: value });
    },
    [onUpdate]
  );

  const handleNotesChange = useCallback(
    (value: string) => {
      setNotes(value);
      onUpdate({ notes: value || undefined });
    },
    [onUpdate]
  );

  const handleTypeChange = useCallback(
    (type: MarkerType) => {
      onUpdate({ type });
    },
    [onUpdate]
  );

  const handleTimeBlur = useCallback(() => {
    const parsed = parseTimeFromInput(timeValue);
    if (parsed !== null && parsed >= 0) {
      onUpdate({ time: parsed });
    } else {
      setTimeValue(formatTimeForInput(marker.time));
    }
  }, [timeValue, marker.time, onUpdate]);

  const handleDurationBlur = useCallback(() => {
    if (!durationValue.trim()) {
      onUpdate({ duration: undefined });
      return;
    }
    const parsed = parseTimeFromInput(durationValue);
    if (parsed !== null && parsed > 0) {
      onUpdate({ duration: parsed });
    } else {
      setDurationValue(marker.duration ? formatTimeForInput(marker.duration) : '');
    }
  }, [durationValue, marker.duration, onUpdate]);

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <span className={styles.headerTitle}>Edit Marker</span>
        <div className={styles.headerActions}>
          <Button
            appearance="subtle"
            icon={<Delete24Regular />}
            className={styles.deleteButton}
            onClick={onDelete}
            title="Delete marker"
            aria-label="Delete marker"
          />
          <Button
            appearance="subtle"
            icon={<Dismiss24Regular />}
            onClick={onClose}
            title="Close"
            aria-label="Close"
          />
        </div>
      </div>

      <Divider />

      {/* Name */}
      <Field label="Name" className={styles.field}>
        <Input
          value={name}
          onChange={(_, data) => handleNameChange(data.value)}
          placeholder="Marker name"
        />
      </Field>

      {/* Type and Color */}
      <div className={styles.row}>
        <Field label="Type" className={styles.field}>
          <Dropdown
            value={MARKER_TYPES.find((t) => t.value === marker.type)?.label || 'Standard'}
            selectedOptions={[marker.type]}
            onOptionSelect={(_, data) => handleTypeChange(data.optionValue as MarkerType)}
          >
            {MARKER_TYPES.map((type) => (
              <Option key={type.value} value={type.value}>
                {type.label}
              </Option>
            ))}
          </Dropdown>
        </Field>
        <Field label="Color">
          <MarkerColorPicker
            selectedColor={marker.color}
            onColorChange={(color) => onUpdate({ color })}
          />
        </Field>
      </div>

      {/* Time and Duration */}
      <div className={styles.row}>
        <Field label="Time" className={styles.field}>
          <Input
            value={timeValue}
            onChange={(_, data) => setTimeValue(data.value)}
            onBlur={handleTimeBlur}
            className={styles.timeInput}
            placeholder="MM:SS:FF"
          />
        </Field>
        <Field label="Duration (optional)" className={styles.smallField}>
          <Input
            value={durationValue}
            onChange={(_, data) => setDurationValue(data.value)}
            onBlur={handleDurationBlur}
            className={styles.timeInput}
            placeholder="MM:SS:FF"
          />
        </Field>
      </div>

      {/* Notes */}
      <Field label="Notes">
        <Textarea
          value={notes}
          onChange={(_, data) => handleNotesChange(data.value)}
          placeholder="Add notes..."
          resize="vertical"
          style={{ minHeight: '60px' }}
        />
      </Field>

      {/* Completion toggle for task markers */}
      {marker.type === 'todo' && (
        <Checkbox
          checked={marker.completed || false}
          onChange={(_, data) => onUpdate({ completed: data.checked === true })}
          label="Mark as completed"
        />
      )}
    </div>
  );
};

export default MarkerEditor;
