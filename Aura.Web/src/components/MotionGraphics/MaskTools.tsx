/**
 * Mask Tools Component
 * Create masks to selectively show/hide layer content
 */

import {
  makeStyles,
  tokens,
  Button,
  Label,
  Card,
  Slider,
  Divider,
} from '@fluentui/react-components';
import {
  Square24Regular,
  Circle24Regular,
  Pen24Regular,
  Checkmark24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';
import { useState, useRef, useEffect, useCallback } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
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
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  controlRow: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
});

export type MaskType = 'rectangle' | 'circle' | 'custom-path';

export interface MaskShape {
  id: string;
  type: MaskType;
  // Rectangle/Circle
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  // Custom path
  path?: Array<{ x: number; y: number }>;
  // Mask properties
  feather: number;
  expansion: number;
  opacity: number;
  inverted: boolean;
}

interface MaskToolsProps {
  onMaskCreated?: (mask: MaskShape) => void;
  canvasWidth?: number;
  canvasHeight?: number;
}

export function MaskTools({
  onMaskCreated,
  canvasWidth = 800,
  canvasHeight = 600,
}: MaskToolsProps) {
  const styles = useStyles();
  const [selectedTool, setSelectedTool] = useState<MaskType | null>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [startPoint, setStartPoint] = useState<{ x: number; y: number } | null>(null);
  const [currentMask, setCurrentMask] = useState<MaskShape | null>(null);
  const [pathPoints, setPathPoints] = useState<Array<{ x: number; y: number }>>([]);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Mask properties
  const [feather, setFeather] = useState(10);
  const [expansion, setExpansion] = useState(0);
  const [opacity, setOpacity] = useState(1);
  const [inverted] = useState(false);

  const drawMask = useCallback(
    (ctx: CanvasRenderingContext2D, mask: MaskShape) => {
      ctx.save();

      // Create mask path
      ctx.beginPath();

      if (mask.type === 'rectangle' && mask.x !== undefined && mask.y !== undefined) {
        ctx.rect(mask.x, mask.y, mask.width || 0, mask.height || 0);
      } else if (mask.type === 'circle' && mask.x !== undefined && mask.y !== undefined) {
        const centerX = mask.x + (mask.width || 0) / 2;
        const centerY = mask.y + (mask.height || 0) / 2;
        const radius = Math.min(Math.abs(mask.width || 0), Math.abs(mask.height || 0)) / 2;
        ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
      } else if (mask.type === 'custom-path' && mask.path && mask.path.length > 0) {
        ctx.moveTo(mask.path[0].x, mask.path[0].y);
        for (let i = 1; i < mask.path.length; i++) {
          ctx.lineTo(mask.path[i].x, mask.path[i].y);
        }
        ctx.closePath();
      }

      // Fill mask area with semi-transparent color
      ctx.fillStyle = inverted
        ? `rgba(255, 0, 0, ${opacity * 0.3})`
        : `rgba(0, 255, 0, ${opacity * 0.3})`;
      ctx.fill();

      // Draw mask outline
      ctx.strokeStyle = tokens.colorBrandForeground1;
      ctx.lineWidth = 2;
      ctx.stroke();

      ctx.restore();
    },
    [inverted, opacity]
  );

  const drawCustomPath = useCallback(
    (ctx: CanvasRenderingContext2D, points: Array<{ x: number; y: number }>) => {
      if (points.length === 0) return;

      ctx.strokeStyle = tokens.colorBrandForeground1;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(points[0].x, points[0].y);

      for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x, points[i].y);
      }

      ctx.stroke();

      // Draw points
      points.forEach((point) => {
        ctx.fillStyle = tokens.colorBrandBackground;
        ctx.strokeStyle = tokens.colorBrandStroke1;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(point.x, point.y, 4, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();
      });
    },
    []
  );

  const redrawCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw mask preview
    if (currentMask) {
      drawMask(ctx, currentMask);
    } else if (selectedTool === 'custom-path' && pathPoints.length > 0) {
      drawCustomPath(ctx, pathPoints);
    }
  }, [currentMask, pathPoints, selectedTool, drawMask, drawCustomPath]);

  useEffect(() => {
    redrawCanvas();
  }, [redrawCanvas]);

  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!selectedTool) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    if (selectedTool === 'custom-path') {
      // Add point to custom path
      setPathPoints([...pathPoints, { x, y }]);
    } else {
      // Start drawing rectangle/circle
      setIsDrawing(true);
      setStartPoint({ x, y });
    }
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isDrawing || !startPoint || !selectedTool || selectedTool === 'custom-path') return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    const width = x - startPoint.x;
    const height = y - startPoint.y;

    const mask: MaskShape = {
      id: `mask-${Date.now()}`,
      type: selectedTool,
      x: startPoint.x,
      y: startPoint.y,
      width,
      height,
      feather,
      expansion,
      opacity,
      inverted,
    };

    setCurrentMask(mask);
  };

  const handleMouseUp = () => {
    setIsDrawing(false);
    setStartPoint(null);
  };

  const handleConfirm = () => {
    let finalMask: MaskShape | null = null;

    if (selectedTool === 'custom-path' && pathPoints.length >= 3) {
      finalMask = {
        id: `mask-${Date.now()}`,
        type: 'custom-path',
        path: pathPoints,
        feather,
        expansion,
        opacity,
        inverted,
      };
    } else if (currentMask) {
      finalMask = currentMask;
    }

    if (finalMask) {
      onMaskCreated?.(finalMask);
      handleCancel();
    }
  };

  const handleCancel = () => {
    setCurrentMask(null);
    setPathPoints([]);
    setIsDrawing(false);
    setStartPoint(null);
    setSelectedTool(null);
  };

  return (
    <div className={styles.container}>
      <Card>
        <div style={{ padding: tokens.spacingVerticalM }}>
          <Label weight="semibold">Mask Tools</Label>
          <Divider />
          <p style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground2 }}>
            Create masks to selectively show or hide parts of your layer
          </p>
        </div>

        <div className={styles.toolbar}>
          <Button
            appearance={selectedTool === 'rectangle' ? 'primary' : 'secondary'}
            icon={<Square24Regular />}
            onClick={() => setSelectedTool('rectangle')}
          >
            Rectangle
          </Button>
          <Button
            appearance={selectedTool === 'circle' ? 'primary' : 'secondary'}
            icon={<Circle24Regular />}
            onClick={() => setSelectedTool('circle')}
          >
            Circle
          </Button>
          <Button
            appearance={selectedTool === 'custom-path' ? 'primary' : 'secondary'}
            icon={<Pen24Regular />}
            onClick={() => setSelectedTool('custom-path')}
          >
            Custom Path
          </Button>
        </div>
      </Card>

      <canvas
        ref={canvasRef}
        width={canvasWidth}
        height={canvasHeight}
        className={styles.canvas}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
      />

      <Card>
        <div className={styles.controls}>
          <div className={styles.controlRow}>
            <Label>Feather: {feather}px</Label>
            <Slider
              min={0}
              max={50}
              value={feather}
              onChange={(_, data) => setFeather(data.value)}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Expansion: {expansion}px</Label>
            <Slider
              min={-50}
              max={50}
              value={expansion}
              onChange={(_, data) => setExpansion(data.value)}
            />
          </div>

          <div className={styles.controlRow}>
            <Label>Opacity: {(opacity * 100).toFixed(0)}%</Label>
            <Slider
              min={0}
              max={1}
              step={0.01}
              value={opacity}
              onChange={(_, data) => setOpacity(data.value)}
            />
          </div>

          {selectedTool === 'custom-path' && pathPoints.length > 0 && (
            <div>
              <Label>Path Points: {pathPoints.length}</Label>
              <p
                style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground3 }}
              >
                Click to add more points. Need at least 3 points to create mask.
              </p>
            </div>
          )}

          {(currentMask || (selectedTool === 'custom-path' && pathPoints.length >= 3)) && (
            <>
              <Divider />
              <div className={styles.actions}>
                <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={handleCancel}>
                  Cancel
                </Button>
                <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleConfirm}>
                  Create Mask
                </Button>
              </div>
            </>
          )}
        </div>
      </Card>
    </div>
  );
}
