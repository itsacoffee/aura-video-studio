/**
 * WordTimingEditor Component
 *
 * Fine-grained word timing editor with waveform visualization.
 * Allows precise adjustment of word boundaries, splitting and merging words.
 */

import { makeStyles, tokens, Text, Button, Tooltip, Slider } from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Merge24Regular,
  SplitVertical24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useRef, useEffect } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import type { TranscriptionWord } from '../../../services/transcriptionService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  waveformContainer: {
    position: 'relative',
    width: '100%',
    height: '80px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    overflow: 'hidden',
    cursor: 'pointer',
  },
  waveformCanvas: {
    width: '100%',
    height: '100%',
  },
  wordBoundaries: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    pointerEvents: 'none',
  },
  wordBoundary: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorBrandStroke1,
    cursor: 'col-resize',
    pointerEvents: 'auto',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorBrandForeground1,
      width: '4px',
    },
  },
  wordLabel: {
    position: 'absolute',
    top: '4px',
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground1,
    backgroundColor: 'rgba(0,0,0,0.6)',
    padding: `2px ${tokens.spacingHorizontalXS}`,
    borderRadius: tokens.borderRadiusSmall,
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    maxWidth: '80px',
    pointerEvents: 'auto',
    cursor: 'pointer',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: 'rgba(0,0,0,0.8)',
    },
  },
  wordLabelSelected: {
    backgroundColor: tokens.colorBrandBackground,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    pointerEvents: 'none',
  },
  wordList: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    maxHeight: '120px',
    overflowY: 'auto',
  },
  wordChip: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    cursor: 'pointer',
    border: `1px solid transparent`,
    transition: 'all 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground5,
      border: `1px solid ${tokens.colorBrandStroke1}`,
    },
  },
  wordChipSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  wordTime: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  zoomControl: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    minWidth: '150px',
  },
  actionButtons: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButton: {
    minWidth: '32px',
    minHeight: '32px',
  },
  confidenceBar: {
    height: '3px',
    marginTop: '2px',
    borderRadius: '1px',
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
});

/**
 * Format seconds to SS.ms display
 */
function formatShortTime(seconds: number): string {
  const secs = Math.floor(seconds);
  const ms = Math.floor((seconds % 1) * 100);
  return `${secs}.${ms.toString().padStart(2, '0')}`;
}

export interface WordTimingEditorProps {
  /** Words to edit */
  words: TranscriptionWord[];
  /** Callback when words are updated */
  onWordsChange: (words: TranscriptionWord[]) => void;
  /** Duration of the audio segment */
  duration: number;
  /** Optional audio URL for playback */
  audioUrl?: string;
  /** Optional class name */
  className?: string;
}

export const WordTimingEditor: FC<WordTimingEditorProps> = ({
  words,
  onWordsChange,
  duration,
  audioUrl,
  className,
}) => {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);

  const [selectedWordIndex, setSelectedWordIndex] = useState<number | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [playbackTime, setPlaybackTime] = useState(0);
  const [zoom, setZoom] = useState(1);
  const [isDragging, setIsDragging] = useState(false);
  const [dragWordIndex, setDragWordIndex] = useState<number | null>(null);

  // Initialize audio element
  useEffect(() => {
    if (audioUrl) {
      audioRef.current = new Audio(audioUrl);
      audioRef.current.addEventListener('timeupdate', () => {
        setPlaybackTime(audioRef.current?.currentTime ?? 0);
      });
      audioRef.current.addEventListener('ended', () => {
        setIsPlaying(false);
      });

      return () => {
        audioRef.current?.pause();
        audioRef.current = null;
      };
    }
    return undefined;
  }, [audioUrl]);

  // Draw simple waveform visualization
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = canvas.width;
    const height = canvas.height;

    // Clear canvas
    ctx.fillStyle = tokens.colorNeutralBackground3;
    ctx.fillRect(0, 0, width, height);

    // Draw simple bars to represent audio
    ctx.fillStyle = tokens.colorNeutralForeground4;
    const barWidth = 3;
    const gap = 2;
    const centerY = height / 2;

    for (let x = 0; x < width; x += barWidth + gap) {
      // Generate pseudo-random height based on position
      const normalizedX = x / width;
      const randomFactor = Math.sin(normalizedX * 50) * 0.5 + 0.5;
      const barHeight = (height * 0.8 * randomFactor) / 2;

      ctx.fillRect(x, centerY - barHeight, barWidth, barHeight * 2);
    }

    // Draw word boundaries
    words.forEach((word) => {
      const startX = (word.startTime / duration) * width;
      const endX = (word.endTime / duration) * width;

      // Draw word region
      ctx.fillStyle = 'rgba(100, 149, 237, 0.2)';
      ctx.fillRect(startX, 0, endX - startX, height);
    });
  }, [words, duration, zoom]);

  const handlePlayPause = useCallback(() => {
    if (!audioRef.current) return;

    if (isPlaying) {
      audioRef.current.pause();
    } else {
      audioRef.current.play();
    }
    setIsPlaying(!isPlaying);
  }, [isPlaying]);

  const handlePlayWord = useCallback(() => {
    if (!audioRef.current || selectedWordIndex === null) return;

    const word = words[selectedWordIndex];
    audioRef.current.currentTime = word.startTime;
    audioRef.current.play();

    // Stop at word end
    const checkEnd = setInterval(() => {
      if (audioRef.current && audioRef.current.currentTime >= word.endTime) {
        audioRef.current.pause();
        clearInterval(checkEnd);
        setIsPlaying(false);
      }
    }, 50);

    setIsPlaying(true);
  }, [selectedWordIndex, words]);

  const handleWordClick = useCallback((index: number) => {
    setSelectedWordIndex(index);
  }, []);

  const handleWaveformClick = useCallback(
    (e: ReactMouseEvent<HTMLDivElement>) => {
      const container = containerRef.current;
      if (!container) return;

      const rect = container.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = (x / rect.width) * duration;

      // Find word at this time
      const wordIndex = words.findIndex((w) => time >= w.startTime && time <= w.endTime);

      if (wordIndex >= 0) {
        setSelectedWordIndex(wordIndex);
      }

      // Seek audio
      if (audioRef.current) {
        audioRef.current.currentTime = time;
        setPlaybackTime(time);
      }
    },
    [duration, words]
  );

  const handleBoundaryDragStart = useCallback((_index: number) => {
    setIsDragging(true);
    setDragWordIndex(_index);
  }, []);

  const handleBoundaryDrag = useCallback(
    (e: ReactMouseEvent<HTMLDivElement>) => {
      if (!isDragging || dragWordIndex === null) return;

      const container = containerRef.current;
      if (!container) return;

      const rect = container.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const newTime = Math.max(0, Math.min(duration, (x / rect.width) * duration));

      const updatedWords = [...words];
      const currentWord = updatedWords[dragWordIndex];
      const prevWord = updatedWords[dragWordIndex - 1];

      if (prevWord) {
        // Adjust end time of previous word and start time of current
        prevWord.endTime = newTime;
        currentWord.startTime = newTime;
      } else {
        // First word, adjust start time
        currentWord.startTime = newTime;
      }

      onWordsChange(updatedWords);
    },
    [isDragging, dragWordIndex, duration, words, onWordsChange]
  );

  const handleBoundaryDragEnd = useCallback(() => {
    setIsDragging(false);
    setDragWordIndex(null);
  }, []);

  const handleMergeWithNext = useCallback(() => {
    if (selectedWordIndex === null || selectedWordIndex >= words.length - 1) return;

    const current = words[selectedWordIndex];
    const next = words[selectedWordIndex + 1];

    const mergedWord: TranscriptionWord = {
      word: `${current.word} ${next.word}`,
      startTime: current.startTime,
      endTime: next.endTime,
      confidence: (current.confidence + next.confidence) / 2,
    };

    const updatedWords = [
      ...words.slice(0, selectedWordIndex),
      mergedWord,
      ...words.slice(selectedWordIndex + 2),
    ];

    onWordsChange(updatedWords);
  }, [selectedWordIndex, words, onWordsChange]);

  const handleSplitWord = useCallback(() => {
    if (selectedWordIndex === null) return;

    const word = words[selectedWordIndex];
    const wordText = word.word;
    const midPoint = Math.floor(wordText.length / 2);

    if (midPoint === 0) return;

    const firstPart = wordText.slice(0, midPoint);
    const secondPart = wordText.slice(midPoint);
    const midTime = (word.startTime + word.endTime) / 2;

    const firstWord: TranscriptionWord = {
      word: firstPart,
      startTime: word.startTime,
      endTime: midTime,
      confidence: word.confidence,
    };

    const secondWord: TranscriptionWord = {
      word: secondPart,
      startTime: midTime,
      endTime: word.endTime,
      confidence: word.confidence,
    };

    const updatedWords = [
      ...words.slice(0, selectedWordIndex),
      firstWord,
      secondWord,
      ...words.slice(selectedWordIndex + 1),
    ];

    onWordsChange(updatedWords);
  }, [selectedWordIndex, words, onWordsChange]);

  const getConfidenceClass = (confidence: number): string => {
    if (confidence >= 0.8) return styles.highConfidence;
    if (confidence >= 0.5) return styles.mediumConfidence;
    return styles.lowConfidence;
  };

  return (
    <div className={`${styles.container} ${className ?? ''}`}>
      <div className={styles.header}>
        <Text weight="semibold">Word Timing Editor</Text>
        <div className={styles.controls}>
          <div className={styles.zoomControl}>
            <Text size={200}>Zoom</Text>
            <Slider
              min={0.5}
              max={4}
              step={0.1}
              value={zoom}
              onChange={(_, data) => setZoom(data.value)}
              size="small"
            />
          </div>
          <div className={styles.actionButtons}>
            {audioUrl && (
              <Tooltip content={isPlaying ? 'Pause' : 'Play'} relationship="label">
                <Button
                  appearance="subtle"
                  className={styles.actionButton}
                  icon={isPlaying ? <Pause24Regular /> : <Play24Regular />}
                  onClick={handlePlayPause}
                />
              </Tooltip>
            )}
            <Tooltip content="Merge with next" relationship="label">
              <Button
                appearance="subtle"
                className={styles.actionButton}
                icon={<Merge24Regular />}
                onClick={handleMergeWithNext}
                disabled={selectedWordIndex === null || selectedWordIndex >= words.length - 1}
              />
            </Tooltip>
            <Tooltip content="Split word" relationship="label">
              <Button
                appearance="subtle"
                className={styles.actionButton}
                icon={<SplitVertical24Regular />}
                onClick={handleSplitWord}
                disabled={selectedWordIndex === null || words[selectedWordIndex]?.word.length < 2}
              />
            </Tooltip>
          </div>
        </div>
      </div>

      {/* Waveform with word boundaries */}
      <div
        ref={containerRef}
        className={styles.waveformContainer}
        style={{ transform: `scaleX(${zoom})`, transformOrigin: 'left' }}
        onClick={handleWaveformClick}
        onMouseMove={isDragging ? handleBoundaryDrag : undefined}
        onMouseUp={handleBoundaryDragEnd}
        onMouseLeave={handleBoundaryDragEnd}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            // Play/pause or seek functionality can be added here
          }
        }}
        role="slider"
        aria-label="Waveform editor - drag to seek"
        aria-valuemin={0}
        aria-valuemax={duration}
        aria-valuenow={0}
        tabIndex={0}
      >
        <canvas ref={canvasRef} className={styles.waveformCanvas} width={800} height={80} />

        <div className={styles.wordBoundaries}>
          {words.map((word, index) => {
            const leftPercent = (word.startTime / duration) * 100;
            const widthPercent = ((word.endTime - word.startTime) / duration) * 100;

            return (
              <div key={`${word.word}-${index}`}>
                {/* Word boundary line */}
                <div
                  className={styles.wordBoundary}
                  style={{ left: `${leftPercent}%` }}
                  onMouseDown={(e) => {
                    e.stopPropagation();
                    handleBoundaryDragStart(index);
                  }}
                  role="slider"
                  aria-label={`Adjust start time for "${word.word}"`}
                  aria-valuenow={word.startTime}
                  tabIndex={0}
                />

                {/* Word label */}
                <div
                  className={`${styles.wordLabel} ${selectedWordIndex === index ? styles.wordLabelSelected : ''}`}
                  style={{
                    left: `${leftPercent}%`,
                    maxWidth: `${widthPercent}%`,
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleWordClick(index);
                  }}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      handleWordClick(index);
                    }
                  }}
                >
                  {word.word}
                </div>
              </div>
            );
          })}

          {/* Playhead */}
          {audioUrl && (
            <div
              className={styles.playhead}
              style={{ left: `${(playbackTime / duration) * 100}%` }}
            />
          )}
        </div>
      </div>

      {/* Word list */}
      <div className={styles.wordList}>
        {words.map((word, index) => (
          <div
            key={`chip-${word.word}-${index}`}
            className={`${styles.wordChip} ${selectedWordIndex === index ? styles.wordChipSelected : ''}`}
            onClick={() => handleWordClick(index)}
            onDoubleClick={handlePlayWord}
            role="button"
            tabIndex={0}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                handleWordClick(index);
              }
            }}
          >
            <div>
              <Text>{word.word}</Text>
              <div className={`${styles.confidenceBar} ${getConfidenceClass(word.confidence)}`} />
            </div>
            <Text className={styles.wordTime}>
              {formatShortTime(word.startTime)}-{formatShortTime(word.endTime)}
            </Text>
          </div>
        ))}
      </div>
    </div>
  );
};

export default WordTimingEditor;
