/**
 * Graph Editor Component
 * Visual editor for animation curves and bezier handles
 */

import { useState, useRef, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Label,
  Card,
  Select,
} from '@fluentui/react-components';
import { Keyframe } from '../../types/effects';
import { getEasingFunction } from '../../services/animationEngine';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: tokens.spacingVerticalM,
  },
  canvas: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controlGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  valueDisplay: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
});

interface GraphEditorProps {
  keyframes: Keyframe[];
  propertyName: string;
  minValue?: number;
  maxValue?: number;
  duration?: number;
  currentTime?: number;
}

export function GraphEditor({
  keyframes,
  propertyName,
  minValue = 0,
  maxValue = 100,
  duration = 10,
  currentTime = 0,
}: GraphEditorProps) {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [hoveredValue, setHoveredValue] = useState<number | null>(null);
  const [selectedCurve, setSelectedCurve] = useState<'ease-in' | 'ease-out' | 'ease-in-out' | 'linear'>('ease-in-out');

  const canvasWidth = 600;
  const canvasHeight = 300;
  const padding = 40;

  useEffect(() => {
    drawGraph();
  }, [keyframes, currentTime, selectedCurve]);

  const drawGraph = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvasWidth, canvasHeight);

    // Draw grid
    drawGrid(ctx);

    // Draw keyframes and curves
    drawKeyframesAndCurves(ctx);

    // Draw current time indicator
    drawCurrentTimeIndicator(ctx);
  };

  const drawGrid = (ctx: CanvasRenderingContext2D) => {
    ctx.strokeStyle = tokens.colorNeutralStroke2;
    ctx.lineWidth = 1;

    // Vertical grid lines (time)
    const timeStep = duration / 10;
    for (let i = 0; i <= 10; i++) {
      const x = padding + (i / 10) * (canvasWidth - 2 * padding);
      ctx.beginPath();
      ctx.moveTo(x, padding);
      ctx.lineTo(x, canvasHeight - padding);
      ctx.stroke();

      // Time labels
      ctx.fillStyle = tokens.colorNeutralForeground2;
      ctx.font = '10px sans-serif';
      ctx.fillText(`${(i * timeStep).toFixed(1)}s`, x - 10, canvasHeight - padding + 15);
    }

    // Horizontal grid lines (value)
    const valueRange = maxValue - minValue;
    for (let i = 0; i <= 5; i++) {
      const y = padding + (i / 5) * (canvasHeight - 2 * padding);
      ctx.beginPath();
      ctx.moveTo(padding, y);
      ctx.lineTo(canvasWidth - padding, y);
      ctx.stroke();

      // Value labels
      const value = maxValue - (i / 5) * valueRange;
      ctx.fillStyle = tokens.colorNeutralForeground2;
      ctx.font = '10px sans-serif';
      ctx.fillText(value.toFixed(0), 5, y + 3);
    }

    // Axis labels
    ctx.fillStyle = tokens.colorNeutralForeground1;
    ctx.font = '12px sans-serif';
    ctx.fillText('Time (s)', canvasWidth / 2 - 20, canvasHeight - 5);
    ctx.save();
    ctx.translate(15, canvasHeight / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText('Value', 0, 0);
    ctx.restore();
  };

  const drawKeyframesAndCurves = (ctx: CanvasRenderingContext2D) => {
    if (keyframes.length === 0) return;

    const sorted = [...keyframes].sort((a, b) => a.time - b.time);

    // Draw curves between keyframes
    ctx.strokeStyle = tokens.colorBrandForeground1;
    ctx.lineWidth = 2;

    for (let i = 0; i < sorted.length - 1; i++) {
      const current = sorted[i];
      const next = sorted[i + 1];

      if (typeof current.value !== 'number' || typeof next.value !== 'number') {
        continue;
      }

      const x1 = padding + (current.time / duration) * (canvasWidth - 2 * padding);
      const y1 =
        padding +
        (1 - (current.value - minValue) / (maxValue - minValue)) * (canvasHeight - 2 * padding);
      const x2 = padding + (next.time / duration) * (canvasWidth - 2 * padding);
      const y2 =
        padding +
        (1 - (next.value - minValue) / (maxValue - minValue)) * (canvasHeight - 2 * padding);

      ctx.beginPath();
      ctx.moveTo(x1, y1);

      // Draw smooth curve based on easing
      const easingFn = getEasingFunction(current);
      const steps = 50;
      for (let step = 1; step <= steps; step++) {
        const t = step / steps;
        const easedT = easingFn(t);
        const x = x1 + (x2 - x1) * t;
        const y = y1 + (y2 - y1) * easedT;
        ctx.lineTo(x, y);
      }

      ctx.stroke();
    }

    // Draw keyframe points
    sorted.forEach((keyframe) => {
      if (typeof keyframe.value !== 'number') return;

      const x = padding + (keyframe.time / duration) * (canvasWidth - 2 * padding);
      const y =
        padding +
        (1 - (keyframe.value - minValue) / (maxValue - minValue)) * (canvasHeight - 2 * padding);

      ctx.fillStyle = tokens.colorBrandBackground;
      ctx.strokeStyle = tokens.colorBrandStroke1;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(x, y, 6, 0, 2 * Math.PI);
      ctx.fill();
      ctx.stroke();
    });
  };

  const drawCurrentTimeIndicator = (ctx: CanvasRenderingContext2D) => {
    const x = padding + (currentTime / duration) * (canvasWidth - 2 * padding);

    ctx.strokeStyle = tokens.colorPaletteRedForeground3;
    ctx.lineWidth = 2;
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(x, padding);
    ctx.lineTo(x, canvasHeight - padding);
    ctx.stroke();
    ctx.setLineDash([]);
  };

  const handleCanvasMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const y = e.clientY - rect.top;

    // Calculate value at mouse position
    const value =
      maxValue - ((y - padding) / (canvasHeight - 2 * padding)) * (maxValue - minValue);

    setHoveredValue(value);
  };

  const handleCanvasMouseLeave = () => {
    setHoveredValue(null);
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.header}>
          <Label weight="semibold">Animation Curve - {propertyName}</Label>
          {hoveredValue !== null && (
            <Label className={styles.valueDisplay}>Value: {hoveredValue.toFixed(2)}</Label>
          )}
        </div>

        <canvas
          ref={canvasRef}
          width={canvasWidth}
          height={canvasHeight}
          className={styles.canvas}
          onMouseMove={handleCanvasMouseMove}
          onMouseLeave={handleCanvasMouseLeave}
        />

        <div className={styles.controls}>
          <div className={styles.controlGroup}>
            <Label>Curve Type</Label>
            <Select
              value={selectedCurve}
              onChange={(_, data) => setSelectedCurve(data.value as typeof selectedCurve)}
            >
              <option value="linear">Linear</option>
              <option value="ease-in">Ease In</option>
              <option value="ease-out">Ease Out</option>
              <option value="ease-in-out">Ease In-Out</option>
            </Select>
          </div>

          <div className={styles.controlGroup}>
            <Label>Keyframes</Label>
            <Label size="large" weight="semibold">
              {keyframes.length}
            </Label>
          </div>

          <div className={styles.controlGroup}>
            <Label>Range</Label>
            <Label size="small">
              {minValue} to {maxValue}
            </Label>
          </div>
        </div>
      </Card>
    </div>
  );
}
