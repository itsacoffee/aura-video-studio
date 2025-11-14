/**
 * Motion Path Component
 * Draw and edit motion paths for object animation
 */

import {
  makeStyles,
  tokens,
  Button,
  Label,
  Card,
  Switch,
  Divider,
} from '@fluentui/react-components';
import { Delete24Regular, Checkmark24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { MotionPath, MotionPathPoint } from '../../services/animationEngine';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  canvas: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'crosshair',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  controls: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  controlRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'space-between',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
  pointList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    maxHeight: '150px',
    overflowY: 'auto',
  },
  pointItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
  },
});

interface MotionPathProps {
  onPathCreated?: (path: MotionPath) => void;
  canvasWidth?: number;
  canvasHeight?: number;
  duration?: number;
}

export function MotionPathTool({
  onPathCreated,
  canvasWidth = 800,
  canvasHeight = 600,
  duration = 10,
}: MotionPathProps) {
  const styles = useStyles();
  const [points, setPoints] = useState<MotionPathPoint[]>([]);
  const [closed, setClosed] = useState(false);
  const [autoOrient, setAutoOrient] = useState(true);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  const redrawCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (points.length === 0) return;

    // Draw path
    ctx.strokeStyle = tokens.colorBrandForeground1;
    ctx.lineWidth = 2;
    ctx.beginPath();

    points.forEach((point, index) => {
      if (index === 0) {
        ctx.moveTo(point.x, point.y);
      } else {
        // Use bezier curves if handles are defined
        const prevPoint = points[index - 1];
        if (prevPoint.handleOut && point.handleIn) {
          ctx.bezierCurveTo(
            prevPoint.x + prevPoint.handleOut.x,
            prevPoint.y + prevPoint.handleOut.y,
            point.x + point.handleIn.x,
            point.y + point.handleIn.y,
            point.x,
            point.y
          );
        } else {
          ctx.lineTo(point.x, point.y);
        }
      }
    });

    if (closed && points.length > 2) {
      ctx.closePath();
    }

    ctx.stroke();

    // Draw points
    points.forEach((point, index) => {
      // Draw point
      ctx.fillStyle = tokens.colorBrandBackground;
      ctx.strokeStyle = tokens.colorBrandStroke1;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(point.x, point.y, 5, 0, 2 * Math.PI);
      ctx.fill();
      ctx.stroke();

      // Draw point number
      ctx.fillStyle = tokens.colorNeutralForeground1;
      ctx.font = '12px sans-serif';
      ctx.fillText(`${index + 1}`, point.x + 8, point.y - 8);
    });
  }, [points, closed]);

  useEffect(() => {
    redrawCanvas();
  }, [redrawCanvas]);

  const handleCanvasClick = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    // Calculate time for this point based on its position in the sequence
    const time = points.length * (duration / 10); // Distribute points evenly

    const newPoint: MotionPathPoint = {
      x,
      y,
      time,
    };

    setPoints([...points, newPoint]);
  };

  const handleDeleteLastPoint = () => {
    if (points.length > 0) {
      setPoints(points.slice(0, -1));
    }
  };

  const handleClearPath = () => {
    setPoints([]);
  };

  const handleConfirm = () => {
    if (points.length < 2) {
      return;
    }

    const path: MotionPath = {
      id: `path-${Date.now()}`,
      points,
      closed,
      autoOrient,
    };

    onPathCreated?.(path);
    setPoints([]);
  };

  const handleCancel = () => {
    setPoints([]);
  };

  return (
    <div className={styles.container}>
      <Card>
        <Label weight="semibold">Motion Path Tool</Label>
        <Divider />
        <p style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground2 }}>
          Click on the canvas to add points to your motion path. Objects will animate along this
          path.
        </p>
      </Card>

      <canvas
        ref={canvasRef}
        width={canvasWidth}
        height={canvasHeight}
        className={styles.canvas}
        onClick={handleCanvasClick}
      />

      <Card>
        <div className={styles.controls}>
          <div className={styles.controlRow}>
            <Label>Closed Path</Label>
            <Switch checked={closed} onChange={(_, data) => setClosed(data.checked)} />
          </div>

          <div className={styles.controlRow}>
            <Label>Auto Orient</Label>
            <Switch checked={autoOrient} onChange={(_, data) => setAutoOrient(data.checked)} />
          </div>

          <Divider />

          <div>
            <Label weight="semibold">Path Points ({points.length})</Label>
            {points.length > 0 ? (
              <div className={styles.pointList}>
                {points.map((point, index) => (
                  <div key={index} className={styles.pointItem}>
                    <span>
                      Point {index + 1}: ({Math.round(point.x)}, {Math.round(point.y)}) @{' '}
                      {point.time.toFixed(2)}s
                    </span>
                  </div>
                ))}
              </div>
            ) : (
              <p
                style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 }}
              >
                No points added yet
              </p>
            )}
          </div>

          <Divider />

          <div className={styles.actions}>
            <Button
              appearance="secondary"
              icon={<Delete24Regular />}
              onClick={handleDeleteLastPoint}
              disabled={points.length === 0}
            >
              Delete Last
            </Button>
            <Button appearance="secondary" onClick={handleClearPath} disabled={points.length === 0}>
              Clear All
            </Button>
          </div>

          {points.length >= 2 && (
            <>
              <Divider />
              <div className={styles.actions}>
                <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={handleCancel}>
                  Cancel
                </Button>
                <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleConfirm}>
                  Create Path
                </Button>
              </div>
            </>
          )}
        </div>
      </Card>
    </div>
  );
}
