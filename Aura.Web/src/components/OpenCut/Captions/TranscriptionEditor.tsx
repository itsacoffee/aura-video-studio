/**
 * TranscriptionEditor Component
 *
 * Editor for refining transcription results before applying them as captions.
 * Allows segment-by-segment text editing, timing adjustments, and speaker assignment.
 */

import { makeStyles, tokens, Text, Button, Input, Tooltip } from '@fluentui/react-components';
import {
  Edit24Regular,
  Delete24Regular,
  Play24Regular,
  Checkmark24Regular,
  Dismiss24Regular,
  SplitVertical24Regular,
  Merge24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC, KeyboardEvent } from 'react';
import type { TranscriptionSegment } from '../../../services/transcriptionService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    maxHeight: '400px',
    overflow: 'auto',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  segmentList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  segment: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    transition: 'all 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground4,
      border: `1px solid ${tokens.colorBrandStroke1}`,
    },
  },
  segmentSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  segmentHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  segmentTime: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  segmentActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    opacity: 0,
    transition: 'opacity 150ms ease-out',
    '.segment:hover &, .segmentSelected &': {
      opacity: 1,
    },
  },
  segmentActionsVisible: {
    opacity: 1,
  },
  segmentText: {
    fontSize: tokens.fontSizeBase300,
    lineHeight: tokens.lineHeightBase300,
    cursor: 'pointer',
  },
  segmentTextEditing: {
    width: '100%',
  },
  segmentSpeaker: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorBrandForeground1,
    fontWeight: 500,
  },
  confidenceIndicator: {
    width: '4px',
    height: '100%',
    borderRadius: '2px',
    marginRight: tokens.spacingHorizontalS,
  },
  highConfidence: {
    backgroundColor: tokens.colorPaletteGreenBackground3,
  },
  mediumConfidence: {
    backgroundColor: tokens.colorPaletteYellowBackground3,
  },
  lowConfidence: {
    backgroundColor: tokens.colorPaletteRedBackground3,
  },
  actionButton: {
    minWidth: '28px',
    minHeight: '28px',
    padding: '2px',
  },
  bulkActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  emptyState: {
    padding: tokens.spacingVerticalXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  timeInput: {
    width: '80px',
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    fontSize: tokens.fontSizeBase200,
  },
});

/**
 * Format seconds to MM:SS.ms display
 */
function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 100);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
}

/**
 * Parse MM:SS.ms format to seconds
 */
function parseTime(timeStr: string): number {
  // Pattern: MM:SS.ms or MM:SS - anchored, simple digits
  // eslint-disable-next-line security/detect-unsafe-regex
  const match = timeStr.match(/^(\d{1,2}):(\d{2})(?:\.(\d{1,2}))?$/);
  if (!match) return 0;

  const mins = parseInt(match[1], 10);
  const secs = parseInt(match[2], 10);
  const ms = match[3] ? parseInt(match[3].padEnd(2, '0'), 10) : 0;

  return mins * 60 + secs + ms / 100;
}

/**
 * Calculate average confidence for a segment
 */
function getAverageConfidence(segment: TranscriptionSegment): number {
  if (segment.words.length === 0) return 0.5;
  const total = segment.words.reduce((sum, w) => sum + w.confidence, 0);
  return total / segment.words.length;
}

export interface TranscriptionEditorProps {
  /** Transcription segments to edit */
  segments: TranscriptionSegment[];
  /** Callback when segments are updated */
  onSegmentsChange: (segments: TranscriptionSegment[]) => void;
  /** Callback to play audio from a specific time */
  onPlaySegment?: (startTime: number, endTime: number) => void;
  /** Optional class name */
  className?: string;
}

export const TranscriptionEditor: FC<TranscriptionEditorProps> = ({
  segments,
  onSegmentsChange,
  onPlaySegment,
  className,
}) => {
  const styles = useStyles();
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editText, setEditText] = useState('');
  const [editStartTime, setEditStartTime] = useState('');
  const [editEndTime, setEditEndTime] = useState('');

  const handleSelectSegment = useCallback((index: number) => {
    setSelectedIndex(index);
    setEditingIndex(null);
  }, []);

  const handleStartEditing = useCallback(
    (index: number) => {
      const segment = segments[index];
      setEditingIndex(index);
      setEditText(segment.text);
      setEditStartTime(formatTime(segment.startTime));
      setEditEndTime(formatTime(segment.endTime));
    },
    [segments]
  );

  const handleSaveEdit = useCallback(() => {
    if (editingIndex === null) return;

    const updatedSegments = [...segments];
    updatedSegments[editingIndex] = {
      ...updatedSegments[editingIndex],
      text: editText,
      startTime: parseTime(editStartTime),
      endTime: parseTime(editEndTime),
    };

    onSegmentsChange(updatedSegments);
    setEditingIndex(null);
  }, [editingIndex, segments, editText, editStartTime, editEndTime, onSegmentsChange]);

  const handleCancelEdit = useCallback(() => {
    setEditingIndex(null);
  }, []);

  const handleDeleteSegment = useCallback(
    (index: number) => {
      const updatedSegments = segments.filter((_, i) => i !== index);
      onSegmentsChange(updatedSegments);
      setSelectedIndex(null);
      setEditingIndex(null);
    },
    [segments, onSegmentsChange]
  );

  const handleMergeWithNext = useCallback(
    (index: number) => {
      if (index >= segments.length - 1) return;

      const current = segments[index];
      const next = segments[index + 1];

      const mergedSegment: TranscriptionSegment = {
        text: `${current.text} ${next.text}`,
        startTime: current.startTime,
        endTime: next.endTime,
        words: [...current.words, ...next.words],
        speaker: current.speaker,
      };

      const updatedSegments = [
        ...segments.slice(0, index),
        mergedSegment,
        ...segments.slice(index + 2),
      ];

      onSegmentsChange(updatedSegments);
    },
    [segments, onSegmentsChange]
  );

  const handleSplitSegment = useCallback(
    (index: number) => {
      const segment = segments[index];
      const words = segment.words;

      if (words.length < 2) return;

      const midPoint = Math.floor(words.length / 2);
      const firstWords = words.slice(0, midPoint);
      const secondWords = words.slice(midPoint);

      const firstSegment: TranscriptionSegment = {
        text: firstWords.map((w) => w.word).join(' '),
        startTime: segment.startTime,
        endTime: firstWords[firstWords.length - 1].endTime,
        words: firstWords,
        speaker: segment.speaker,
      };

      const secondSegment: TranscriptionSegment = {
        text: secondWords.map((w) => w.word).join(' '),
        startTime: secondWords[0].startTime,
        endTime: segment.endTime,
        words: secondWords,
        speaker: segment.speaker,
      };

      const updatedSegments = [
        ...segments.slice(0, index),
        firstSegment,
        secondSegment,
        ...segments.slice(index + 1),
      ];

      onSegmentsChange(updatedSegments);
    },
    [segments, onSegmentsChange]
  );

  const handlePlaySegment = useCallback(
    (index: number) => {
      const segment = segments[index];
      onPlaySegment?.(segment.startTime, segment.endTime);
    },
    [segments, onPlaySegment]
  );

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        handleSaveEdit();
      } else if (e.key === 'Escape') {
        handleCancelEdit();
      }
    },
    [handleSaveEdit, handleCancelEdit]
  );

  const getConfidenceClass = (segment: TranscriptionSegment): string => {
    const confidence = getAverageConfidence(segment);
    if (confidence >= 0.8) return styles.highConfidence;
    if (confidence >= 0.5) return styles.mediumConfidence;
    return styles.lowConfidence;
  };

  if (segments.length === 0) {
    return (
      <div className={`${styles.container} ${className ?? ''}`}>
        <div className={styles.emptyState}>
          <Text>No transcription segments available</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={`${styles.container} ${className ?? ''}`}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Edit24Regular />
          <Text weight="semibold">Edit Transcription</Text>
        </div>
        <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
          {segments.length} segments
        </Text>
      </div>

      <div className={styles.segmentList}>
        {segments.map((segment, index) => {
          const isSelected = selectedIndex === index;
          const isEditing = editingIndex === index;

          return (
            <div
              key={`${segment.startTime}-${index}`}
              className={`segment ${styles.segment} ${isSelected ? styles.segmentSelected : ''}`}
              onClick={() => handleSelectSegment(index)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  handleSelectSegment(index);
                }
              }}
            >
              <div className={styles.segmentHeader}>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                  <div className={`${styles.confidenceIndicator} ${getConfidenceClass(segment)}`} />
                  {segment.speaker && (
                    <Text className={styles.segmentSpeaker}>{segment.speaker}</Text>
                  )}
                  {isEditing ? (
                    <div className={styles.segmentTime}>
                      <Input
                        className={styles.timeInput}
                        value={editStartTime}
                        onChange={(_, data) => setEditStartTime(data.value)}
                        onKeyDown={handleKeyDown}
                        size="small"
                      />
                      <Text>→</Text>
                      <Input
                        className={styles.timeInput}
                        value={editEndTime}
                        onChange={(_, data) => setEditEndTime(data.value)}
                        onKeyDown={handleKeyDown}
                        size="small"
                      />
                    </div>
                  ) : (
                    <div className={styles.segmentTime}>
                      {formatTime(segment.startTime)} → {formatTime(segment.endTime)}
                    </div>
                  )}
                </div>

                <div
                  className={`${styles.segmentActions} ${isSelected || isEditing ? styles.segmentActionsVisible : ''}`}
                >
                  {isEditing ? (
                    <>
                      <Tooltip content="Save" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Checkmark24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleSaveEdit();
                          }}
                        />
                      </Tooltip>
                      <Tooltip content="Cancel" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Dismiss24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleCancelEdit();
                          }}
                        />
                      </Tooltip>
                    </>
                  ) : (
                    <>
                      {onPlaySegment && (
                        <Tooltip content="Play segment" relationship="label">
                          <Button
                            appearance="subtle"
                            size="small"
                            className={styles.actionButton}
                            icon={<Play24Regular />}
                            onClick={(e) => {
                              e.stopPropagation();
                              handlePlaySegment(index);
                            }}
                          />
                        </Tooltip>
                      )}
                      <Tooltip content="Edit" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Edit24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleStartEditing(index);
                          }}
                        />
                      </Tooltip>
                      <Tooltip content="Split" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<SplitVertical24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleSplitSegment(index);
                          }}
                          disabled={segment.words.length < 2}
                        />
                      </Tooltip>
                      {index < segments.length - 1 && (
                        <Tooltip content="Merge with next" relationship="label">
                          <Button
                            appearance="subtle"
                            size="small"
                            className={styles.actionButton}
                            icon={<Merge24Regular />}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleMergeWithNext(index);
                            }}
                          />
                        </Tooltip>
                      )}
                      <Tooltip content="Delete" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Delete24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteSegment(index);
                          }}
                        />
                      </Tooltip>
                    </>
                  )}
                </div>
              </div>

              {isEditing ? (
                <Input
                  className={styles.segmentTextEditing}
                  value={editText}
                  onChange={(_, data) => setEditText(data.value)}
                  onKeyDown={handleKeyDown}
                  autoFocus
                />
              ) : (
                <Text
                  className={styles.segmentText}
                  onDoubleClick={() => handleStartEditing(index)}
                >
                  {segment.text}
                </Text>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default TranscriptionEditor;
