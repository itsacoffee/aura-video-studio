/**
 * KeyframeEditor Component
 *
 * Visual keyframe curve editor with canvas-based bezier curve visualization,
 * draggable keyframe points, handle manipulation for custom easing,
 * zoom and pan controls, and value graph with grid lines.
 */

import {
  makeStyles,
  tokens,
  Button,
  Text,
  Tooltip,
  mergeClasses,
  Slider,
} from '@fluentui/react-components';
import {
  ZoomIn16Regular,
  ZoomOut16Regular,
  ArrowResetRegular,
  Copy16Regular,
  Clipboard16Regular,
  Delete16Regular,
} from '@fluentui/react-icons';
import { useCallback, useState, useRef, useEffect, useMemo } from 'react';
import type { FC, MouseEvent as ReactMouseEvent, WheelEvent } from 'react';
import {
  useOpenCutKeyframesStore,
  type Keyframe,
  type KeyframeTrack,
} from '../../../stores/opencutKeyframes';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { EasingPresets } from './EasingPresets';
import { KeyframeDiamond } from './KeyframeDiamond';

export interface KeyframeEditorProps {
  /** The clip ID to edit keyframes for */
  clipId: string;
  /** Optional property filter - if set, only show this property */
  property?: string;
  /** Width of the editor */
  width?: number;
  /** Height of the editor */
  height?: number;
  /** Called when the editor is closed */
  onClose?: () => void;
  /** Additional class name */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  toolbarLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  toolbarRight: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  canvasContainer: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
    cursor: 'crosshair',
  },
  canvas: {
    position: 'absolute',
    top: 0,
    left: 0,
  },
  keyframeOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    pointerEvents: 'none',
  },
  keyframeMarker: {
    position: 'absolute',
    transform: 'translate(-50%, -50%)',
    pointerEvents: 'auto',
    zIndex: 10,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    pointerEvents: 'none',
    zIndex: 5,
  },
  valueLabel: {
    position: 'absolute',
    right: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground2,
    pointerEvents: 'none',
  },
  timeLabel: {
    position: 'absolute',
    bottom: tokens.spacingVerticalXS,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalXS}`,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground2,
    pointerEvents: 'none',
  },
  zoomSlider: {
    width: '100px',
  },
  trackSelector: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  trackButton: {
    minWidth: '60px',
    fontSize: tokens.fontSizeBase100,
  },
  trackButtonActive: {
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  controlButton: {
    minWidth: '28px',
    minHeight: '28px',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    color: tokens.colorNeutralForeground3,
    gap: tokens.spacingVerticalS,
  },
});

const PADDING = 40;
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 4;
const CURVE_SAMPLES = 100;
const HEADER_TOOLBAR_HEIGHT = 80;

export const KeyframeEditor: FC<KeyframeEditorProps> = ({
  clipId,
  property,
  width = 400,
  height = 200,
  onClose,
  className,
}) => {
  const styles = useStyles();
  const keyframesStore = useOpenCutKeyframesStore();
  const playbackStore = useOpenCutPlaybackStore();

  const canvasRef = useRef<HTMLCanvasElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const [zoom, setZoom] = useState(1);
  const [panX, setPanX] = useState(0);
  const [selectedProperty, setSelectedProperty] = useState<string | null>(property || null);
  const [draggingKeyframe, setDraggingKeyframe] = useState<string | null>(null);
  const dragStartRef = useRef<{ x: number; y: number; time: number; value: number } | null>(null);

  const currentTime = playbackStore.currentTime;
  const duration = playbackStore.duration;

  // Get tracks for this clip
  const tracks = useMemo(() => keyframesStore.getTracksForClip(clipId), [keyframesStore, clipId]);

  // Get active track
  const activeTrack = useMemo(() => {
    if (selectedProperty) {
      return tracks.find((t) => t.property === selectedProperty);
    }
    return tracks[0];
  }, [tracks, selectedProperty]);

  const keyframes = activeTrack?.keyframes || [];
  const selectedKeyframeIds = keyframesStore.selectedKeyframeIds;

  // Calculate canvas dimensions
  const canvasWidth = width - 2 * PADDING;
  const canvasHeight = height - 2 * PADDING;

  // Calculate value range
  const { minValue, maxValue } = useMemo(() => {
    if (keyframes.length === 0) return { minValue: 0, maxValue: 100 };
    const values = keyframes.map((k) => Number(k.value)).filter((v) => !isNaN(v));
    if (values.length === 0) return { minValue: 0, maxValue: 100 };
    const min = Math.min(...values);
    const max = Math.max(...values);
    const padding = (max - min) * 0.1 || 10;
    return { minValue: min - padding, maxValue: max + padding };
  }, [keyframes]);

  // Convert time/value to canvas coordinates
  const toCanvasX = useCallback(
    (time: number): number => {
      return PADDING + ((time / duration) * canvasWidth - panX) * zoom;
    },
    [duration, canvasWidth, panX, zoom]
  );

  const toCanvasY = useCallback(
    (value: number): number => {
      const normalized = (value - minValue) / (maxValue - minValue);
      return height - PADDING - normalized * canvasHeight;
    },
    [height, minValue, maxValue, canvasHeight]
  );

  // Convert canvas coordinates to time/value
  const fromCanvasX = useCallback(
    (x: number): number => {
      return (((x - PADDING) / zoom + panX) / canvasWidth) * duration;
    },
    [duration, canvasWidth, panX, zoom]
  );

  const fromCanvasY = useCallback(
    (y: number): number => {
      const normalized = (height - PADDING - y) / canvasHeight;
      return minValue + normalized * (maxValue - minValue);
    },
    [height, minValue, maxValue, canvasHeight]
  );

  // Draw the curve on canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, width, height);

    // Draw grid
    ctx.strokeStyle = tokens.colorNeutralStroke3;
    ctx.lineWidth = 0.5;

    // Vertical grid lines (time)
    const timeStep = duration / 10;
    for (let t = 0; t <= duration; t += timeStep) {
      const x = toCanvasX(t);
      if (x >= PADDING && x <= width - PADDING) {
        ctx.beginPath();
        ctx.moveTo(x, PADDING);
        ctx.lineTo(x, height - PADDING);
        ctx.stroke();
      }
    }

    // Horizontal grid lines (value)
    const valueStep = (maxValue - minValue) / 5;
    for (let v = minValue; v <= maxValue; v += valueStep) {
      const y = toCanvasY(v);
      ctx.beginPath();
      ctx.moveTo(PADDING, y);
      ctx.lineTo(width - PADDING, y);
      ctx.stroke();
    }

    // Draw curve if we have keyframes
    if (keyframes.length >= 2 && activeTrack?.enabled) {
      ctx.strokeStyle = tokens.colorBrandStroke1;
      ctx.lineWidth = 2;
      ctx.beginPath();

      // Sample the curve
      for (let i = 0; i <= CURVE_SAMPLES; i++) {
        const t = (i / CURVE_SAMPLES) * duration;
        const value = keyframesStore.getValueAtTime(clipId, activeTrack.property, t);
        if (typeof value === 'number') {
          const x = toCanvasX(t);
          const y = toCanvasY(value);
          if (i === 0) {
            ctx.moveTo(x, y);
          } else {
            ctx.lineTo(x, y);
          }
        }
      }
      ctx.stroke();
    }

    // Draw axis labels
    ctx.fillStyle = tokens.colorNeutralForeground3;
    ctx.font = '10px ui-monospace, SFMono-Regular, monospace';
    ctx.textAlign = 'center';

    // Time labels
    for (let t = 0; t <= duration; t += timeStep * 2) {
      const x = toCanvasX(t);
      if (x >= PADDING && x <= width - PADDING) {
        ctx.fillText(`${t.toFixed(1)}s`, x, height - 10);
      }
    }

    // Value labels
    ctx.textAlign = 'right';
    for (let v = minValue; v <= maxValue; v += valueStep) {
      const y = toCanvasY(v);
      ctx.fillText(`${v.toFixed(0)}`, PADDING - 5, y + 3);
    }
  }, [
    width,
    height,
    keyframes,
    activeTrack,
    duration,
    minValue,
    maxValue,
    toCanvasX,
    toCanvasY,
    keyframesStore,
    clipId,
    zoom,
    panX,
  ]);

  // Handle double-click to add keyframe
  const handleDoubleClick = useCallback(
    (e: ReactMouseEvent) => {
      if (!activeTrack) return;

      const rect = containerRef.current?.getBoundingClientRect();
      if (!rect) return;

      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      const time = Math.max(0, Math.min(duration, fromCanvasX(x)));
      const value = fromCanvasY(y);

      keyframesStore.addKeyframe(clipId, activeTrack.property, time, value);
    },
    [activeTrack, clipId, duration, fromCanvasX, fromCanvasY, keyframesStore]
  );

  // Handle keyframe drag
  const handleKeyframeMouseDown = useCallback(
    (keyframe: Keyframe, e: ReactMouseEvent) => {
      e.stopPropagation();
      setDraggingKeyframe(keyframe.id);
      dragStartRef.current = {
        x: e.clientX,
        y: e.clientY,
        time: keyframe.time,
        value: Number(keyframe.value),
      };

      if (!selectedKeyframeIds.includes(keyframe.id)) {
        keyframesStore.selectKeyframe(keyframe.id);
      }
    },
    [selectedKeyframeIds, keyframesStore]
  );

  // Handle dragging
  useEffect(() => {
    if (!draggingKeyframe) return;

    const handleMouseMove = (e: globalThis.MouseEvent) => {
      if (!dragStartRef.current) return;

      const deltaX = (e.clientX - dragStartRef.current.x) / zoom;
      const deltaY = -(e.clientY - dragStartRef.current.y);

      const deltaTime = (deltaX / canvasWidth) * duration;
      const deltaValue = (deltaY / canvasHeight) * (maxValue - minValue);

      const newTime = Math.max(0, Math.min(duration, dragStartRef.current.time + deltaTime));
      const newValue = dragStartRef.current.value + deltaValue;

      keyframesStore.moveKeyframe(draggingKeyframe, newTime);
      keyframesStore.updateKeyframe(draggingKeyframe, { value: newValue });
    };

    const handleMouseUp = () => {
      setDraggingKeyframe(null);
      dragStartRef.current = null;
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [
    draggingKeyframe,
    zoom,
    canvasWidth,
    canvasHeight,
    duration,
    minValue,
    maxValue,
    keyframesStore,
  ]);

  // Handle wheel for zoom
  const handleWheel = useCallback((e: WheelEvent) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.9 : 1.1;
    setZoom((z) => Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, z * delta)));
  }, []);

  // Zoom controls
  const handleZoomIn = useCallback(() => {
    setZoom((z) => Math.min(MAX_ZOOM, z * 1.25));
  }, []);

  const handleZoomOut = useCallback(() => {
    setZoom((z) => Math.max(MIN_ZOOM, z / 1.25));
  }, []);

  const handleResetZoom = useCallback(() => {
    setZoom(1);
    setPanX(0);
  }, []);

  // Clipboard operations
  const handleCopy = useCallback(() => {
    keyframesStore.copySelectedKeyframes();
  }, [keyframesStore]);

  const handlePaste = useCallback(() => {
    if (activeTrack) {
      keyframesStore.pasteKeyframes(clipId, activeTrack.property, currentTime);
    }
  }, [keyframesStore, activeTrack, clipId, currentTime]);

  const handleDelete = useCallback(() => {
    keyframesStore.deleteSelectedKeyframes();
  }, [keyframesStore]);

  // Playhead position
  const playheadX = toCanvasX(currentTime);

  if (tracks.length === 0) {
    return (
      <div className={mergeClasses(styles.container, className)} style={{ width, height }}>
        <div className={styles.emptyState}>
          <Text size={300}>No keyframes</Text>
          <Text size={200}>Add keyframes using the property controls</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={mergeClasses(styles.container, className)} style={{ width, height }}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Text weight="semibold" size={200}>
            Keyframe Editor
          </Text>
          {activeTrack && (
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              {activeTrack.property}
            </Text>
          )}
        </div>
        <div className={styles.headerRight}>
          {onClose && (
            <Button appearance="subtle" size="small" onClick={onClose}>
              Close
            </Button>
          )}
        </div>
      </div>

      {/* Toolbar */}
      <div className={styles.toolbar}>
        <div className={styles.toolbarLeft}>
          {/* Track selector */}
          <div className={styles.trackSelector}>
            {tracks.map((track) => (
              <Button
                key={track.id}
                appearance="subtle"
                size="small"
                className={mergeClasses(
                  styles.trackButton,
                  selectedProperty === track.property && styles.trackButtonActive
                )}
                onClick={() => setSelectedProperty(track.property)}
              >
                {track.property}
              </Button>
            ))}
          </div>

          {/* Easing selector for selected keyframes */}
          {selectedKeyframeIds.length > 0 && (
            <EasingPresets
              value={keyframes.find((k) => k.id === selectedKeyframeIds[0])?.easing || 'ease-out'}
              onChange={(easing) => keyframesStore.setEasingForSelected(easing)}
              size="small"
            />
          )}
        </div>

        <div className={styles.toolbarRight}>
          {/* Clipboard actions */}
          <Tooltip content="Copy (Cmd+C)" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Copy16Regular />}
              className={styles.controlButton}
              onClick={handleCopy}
              disabled={selectedKeyframeIds.length === 0}
            />
          </Tooltip>
          <Tooltip content="Paste (Cmd+V)" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Clipboard16Regular />}
              className={styles.controlButton}
              onClick={handlePaste}
              disabled={keyframesStore.copiedKeyframes.length === 0}
            />
          </Tooltip>
          <Tooltip content="Delete" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Delete16Regular />}
              className={styles.controlButton}
              onClick={handleDelete}
              disabled={selectedKeyframeIds.length === 0}
            />
          </Tooltip>

          {/* Zoom controls */}
          <Tooltip content="Zoom out" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ZoomOut16Regular />}
              className={styles.controlButton}
              onClick={handleZoomOut}
            />
          </Tooltip>
          <Slider
            className={styles.zoomSlider}
            min={MIN_ZOOM * 100}
            max={MAX_ZOOM * 100}
            value={zoom * 100}
            onChange={(_, data) => setZoom(data.value / 100)}
            size="small"
          />
          <Tooltip content="Zoom in" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ZoomIn16Regular />}
              className={styles.controlButton}
              onClick={handleZoomIn}
            />
          </Tooltip>
          <Tooltip content="Reset view" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ArrowResetRegular />}
              className={styles.controlButton}
              onClick={handleResetZoom}
            />
          </Tooltip>
        </div>
      </div>

      {/* Canvas */}
      <div
        ref={containerRef}
        className={styles.canvasContainer}
        onDoubleClick={handleDoubleClick}
        onWheel={handleWheel}
      >
        <canvas
          ref={canvasRef}
          className={styles.canvas}
          width={width}
          height={height - HEADER_TOOLBAR_HEIGHT}
        />

        {/* Keyframe markers */}
        <div className={styles.keyframeOverlay}>
          {keyframes.map((keyframe) => {
            const x = toCanvasX(keyframe.time);
            const y = toCanvasY(Number(keyframe.value));
            const isSelected = selectedKeyframeIds.includes(keyframe.id);

            if (x < PADDING || x > width - PADDING) return null;

            return (
              <div
                key={keyframe.id}
                className={styles.keyframeMarker}
                style={{
                  left: x,
                  top: y,
                  cursor: draggingKeyframe === keyframe.id ? 'grabbing' : 'grab',
                }}
              >
                <KeyframeDiamond
                  isActive
                  isSelected={isSelected}
                  size="medium"
                  onClick={() => keyframesStore.selectKeyframe(keyframe.id)}
                  onMouseDown={(e) => handleKeyframeMouseDown(keyframe, e)}
                  ariaLabel={`Keyframe: ${Number(keyframe.value).toFixed(1)} at ${keyframe.time.toFixed(2)}s`}
                />
              </div>
            );
          })}
        </div>

        {/* Playhead */}
        {playheadX >= PADDING && playheadX <= width - PADDING && (
          <div className={styles.playhead} style={{ left: playheadX }} />
        )}
      </div>
    </div>
  );
};

export default KeyframeEditor;
