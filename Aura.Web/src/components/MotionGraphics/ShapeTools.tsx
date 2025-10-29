/**
 * Shape Tools Component
 * Drawing tools for creating vector shapes on canvas
 */

import {
  makeStyles,
  tokens,
  Button,
  Label,
  Input,
  Card,
  Divider,
} from '@fluentui/react-components';
import {
  Square24Regular,
  Circle24Regular,
  Star24Regular,
  Line24Regular,
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
    backgroundColor: tokens.colorNeutralBackground1,
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  toolButton: {
    minWidth: '40px',
  },
  canvas: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'crosshair',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  properties: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  propertyRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  propertyLabel: {
    minWidth: '100px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
});

export type ShapeType = 'rectangle' | 'circle' | 'polygon' | 'star' | 'line';

export interface Shape {
  id: string;
  type: ShapeType;
  x: number;
  y: number;
  width: number;
  height: number;
  fill: string;
  stroke: string;
  strokeWidth: number;
  // Shape-specific properties
  sides?: number; // For polygon
  points?: number; // For star
  innerRadius?: number; // For star
}

interface ShapeToolsProps {
  onShapeCreated?: (shape: Shape) => void;
  canvasWidth?: number;
  canvasHeight?: number;
}

export function ShapeTools({
  onShapeCreated,
  canvasWidth = 800,
  canvasHeight = 600,
}: ShapeToolsProps) {
  const styles = useStyles();
  const [selectedTool, setSelectedTool] = useState<ShapeType | null>(null);
  const [isDrawing, setIsDrawing] = useState(false);
  const [startPoint, setStartPoint] = useState<{ x: number; y: number } | null>(null);
  const [currentShape, setCurrentShape] = useState<Shape | null>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Shape properties
  const [fillColor, setFillColor] = useState('#3b82f6');
  const [strokeColor, setStrokeColor] = useState('#1e40af');
  const [strokeWidth, setStrokeWidth] = useState(2);
  const [sides, setSides] = useState(5);
  const [starPoints, setStarPoints] = useState(5);
  const [innerRadius, setInnerRadius] = useState(0.4);

  const drawPolygon = useCallback(
    (ctx: CanvasRenderingContext2D, x: number, y: number, radius: number, sides: number) => {
      ctx.beginPath();
      for (let i = 0; i < sides; i++) {
        const angle = (i * 2 * Math.PI) / sides - Math.PI / 2;
        const px = x + radius * Math.cos(angle);
        const py = y + radius * Math.sin(angle);
        if (i === 0) {
          ctx.moveTo(px, py);
        } else {
          ctx.lineTo(px, py);
        }
      }
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    },
    []
  );

  const drawStar = useCallback(
    (
      ctx: CanvasRenderingContext2D,
      x: number,
      y: number,
      points: number,
      outerRadius: number,
      innerRadius: number
    ) => {
      ctx.beginPath();
      for (let i = 0; i < points * 2; i++) {
        const radius = i % 2 === 0 ? outerRadius : innerRadius;
        const angle = (i * Math.PI) / points - Math.PI / 2;
        const px = x + radius * Math.cos(angle);
        const py = y + radius * Math.sin(angle);
        if (i === 0) {
          ctx.moveTo(px, py);
        } else {
          ctx.lineTo(px, py);
        }
      }
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
    },
    []
  );

  const drawShape = useCallback(
    (ctx: CanvasRenderingContext2D, shape: Shape) => {
      ctx.fillStyle = shape.fill;
      ctx.strokeStyle = shape.stroke;
      ctx.lineWidth = shape.strokeWidth;

      switch (shape.type) {
        case 'rectangle':
          ctx.fillRect(shape.x, shape.y, shape.width, shape.height);
          ctx.strokeRect(shape.x, shape.y, shape.width, shape.height);
          break;

        case 'circle': {
          const centerX = shape.x + shape.width / 2;
          const centerY = shape.y + shape.height / 2;
          const radius = Math.min(Math.abs(shape.width), Math.abs(shape.height)) / 2;
          ctx.beginPath();
          ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
          ctx.fill();
          ctx.stroke();
          break;
        }

        case 'polygon': {
          const centerX = shape.x + shape.width / 2;
          const centerY = shape.y + shape.height / 2;
          const radius = Math.min(Math.abs(shape.width), Math.abs(shape.height)) / 2;
          const numSides = shape.sides || 5;
          drawPolygon(ctx, centerX, centerY, radius, numSides);
          break;
        }

        case 'star': {
          const centerX = shape.x + shape.width / 2;
          const centerY = shape.y + shape.height / 2;
          const outerRadius = Math.min(Math.abs(shape.width), Math.abs(shape.height)) / 2;
          const numPoints = shape.points || 5;
          const innerR = (shape.innerRadius || 0.4) * outerRadius;
          drawStar(ctx, centerX, centerY, numPoints, outerRadius, innerR);
          break;
        }

        case 'line':
          ctx.beginPath();
          ctx.moveTo(shape.x, shape.y);
          ctx.lineTo(shape.x + shape.width, shape.y + shape.height);
          ctx.stroke();
          break;
      }
    },
    [drawPolygon, drawStar]
  );

  const redrawCanvas = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw current shape if exists
    if (currentShape) {
      drawShape(ctx, currentShape);
    }
  }, [currentShape, drawShape]);

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

    setIsDrawing(true);
    setStartPoint({ x, y });
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isDrawing || !startPoint || !selectedTool) return;

    const canvas = canvasRef.current;
    if (!canvas) return;

    const rect = canvas.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    const width = x - startPoint.x;
    const height = y - startPoint.y;

    const shape: Shape = {
      id: `shape-${Date.now()}`,
      type: selectedTool,
      x: startPoint.x,
      y: startPoint.y,
      width,
      height,
      fill: fillColor,
      stroke: strokeColor,
      strokeWidth,
      sides: selectedTool === 'polygon' ? sides : undefined,
      points: selectedTool === 'star' ? starPoints : undefined,
      innerRadius: selectedTool === 'star' ? innerRadius : undefined,
    };

    setCurrentShape(shape);
  };

  const handleMouseUp = () => {
    if (isDrawing && currentShape) {
      // Shape is complete
      setIsDrawing(false);
      setStartPoint(null);
    }
  };

  const handleConfirm = () => {
    if (currentShape) {
      onShapeCreated?.(currentShape);
      setCurrentShape(null);
      setSelectedTool(null);
    }
  };

  const handleCancel = () => {
    setCurrentShape(null);
    setIsDrawing(false);
    setStartPoint(null);
    setSelectedTool(null);
  };

  return (
    <div className={styles.container}>
      <Card>
        <div className={styles.toolbar}>
          <Button
            className={styles.toolButton}
            appearance={selectedTool === 'rectangle' ? 'primary' : 'secondary'}
            icon={<Square24Regular />}
            onClick={() => setSelectedTool('rectangle')}
            aria-label="Rectangle tool"
          />
          <Button
            className={styles.toolButton}
            appearance={selectedTool === 'circle' ? 'primary' : 'secondary'}
            icon={<Circle24Regular />}
            onClick={() => setSelectedTool('circle')}
            aria-label="Circle tool"
          />
          <Button
            className={styles.toolButton}
            appearance={selectedTool === 'polygon' ? 'primary' : 'secondary'}
            onClick={() => setSelectedTool('polygon')}
            aria-label="Polygon tool"
          >
            Poly
          </Button>
          <Button
            className={styles.toolButton}
            appearance={selectedTool === 'star' ? 'primary' : 'secondary'}
            icon={<Star24Regular />}
            onClick={() => setSelectedTool('star')}
            aria-label="Star tool"
          />
          <Button
            className={styles.toolButton}
            appearance={selectedTool === 'line' ? 'primary' : 'secondary'}
            icon={<Line24Regular />}
            onClick={() => setSelectedTool('line')}
            aria-label="Line tool"
          />
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
        <div className={styles.properties}>
          <Label weight="semibold">Shape Properties</Label>
          <Divider />

          <div className={styles.propertyRow}>
            <Label className={styles.propertyLabel}>Fill Color</Label>
            <input
              type="color"
              value={fillColor}
              onChange={(e) => setFillColor(e.target.value)}
              style={{ width: '100px', height: '32px' }}
            />
          </div>

          <div className={styles.propertyRow}>
            <Label className={styles.propertyLabel}>Stroke Color</Label>
            <input
              type="color"
              value={strokeColor}
              onChange={(e) => setStrokeColor(e.target.value)}
              style={{ width: '100px', height: '32px' }}
            />
          </div>

          <div className={styles.propertyRow}>
            <Label className={styles.propertyLabel}>Stroke Width</Label>
            <Input
              type="number"
              value={strokeWidth.toString()}
              onChange={(_, data) => setStrokeWidth(Number(data.value))}
              min={0}
              max={20}
            />
          </div>

          {selectedTool === 'polygon' && (
            <div className={styles.propertyRow}>
              <Label className={styles.propertyLabel}>Sides</Label>
              <Input
                type="number"
                value={sides.toString()}
                onChange={(_, data) => setSides(Number(data.value))}
                min={3}
                max={12}
              />
            </div>
          )}

          {selectedTool === 'star' && (
            <>
              <div className={styles.propertyRow}>
                <Label className={styles.propertyLabel}>Points</Label>
                <Input
                  type="number"
                  value={starPoints.toString()}
                  onChange={(_, data) => setStarPoints(Number(data.value))}
                  min={3}
                  max={12}
                />
              </div>
              <div className={styles.propertyRow}>
                <Label className={styles.propertyLabel}>Inner Radius</Label>
                <Input
                  type="number"
                  value={innerRadius.toString()}
                  onChange={(_, data) => setInnerRadius(Number(data.value))}
                  min={0.1}
                  max={0.9}
                  step={0.1}
                />
              </div>
            </>
          )}

          {currentShape && (
            <>
              <Divider />
              <div className={styles.actions}>
                <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={handleCancel}>
                  Cancel
                </Button>
                <Button appearance="primary" icon={<Checkmark24Regular />} onClick={handleConfirm}>
                  Create Shape
                </Button>
              </div>
            </>
          )}
        </div>
      </Card>
    </div>
  );
}
