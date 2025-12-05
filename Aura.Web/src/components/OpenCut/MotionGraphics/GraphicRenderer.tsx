/**
 * GraphicRenderer Component
 *
 * Main canvas/SVG renderer for motion graphics with layer composition,
 * animation playback engine, easing functions, and transform handling.
 */

import { makeStyles } from '@fluentui/react-components';
import { useMemo } from 'react';
import type { FC, CSSProperties } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type { AppliedGraphic, GraphicLayer } from '../../../types/motionGraphics';
import {
  applyEasing,
  updateAnimationState,
  createAnimationState,
  getAnimationStartTransform,
  lerpTransform,
  type TransformValues,
  type AnimationPhase,
} from '../../../utils/motionGraphicsAnimation';

export interface GraphicRendererProps {
  graphic: AppliedGraphic;
  currentTime: number;
  width: number;
  height: number;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    pointerEvents: 'none',
    overflow: 'hidden',
  },
  layer: {
    position: 'absolute',
    transformOrigin: 'center center',
  },
  textLayer: {
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  shapeLayer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
});

interface LayerRenderState {
  transform: TransformValues;
  opacity: number;
  blur: number;
}

function calculateLayerState(
  layer: GraphicLayer,
  phase: AnimationPhase,
  progress: number
): LayerRenderState {
  const baseTransform: TransformValues = {
    x: layer.transform.x,
    y: layer.transform.y,
    scaleX: layer.transform.scaleX,
    scaleY: layer.transform.scaleY,
    rotation: layer.transform.rotation,
    opacity: layer.transform.opacity,
  };

  const defaultState: LayerRenderState = {
    transform: baseTransform,
    opacity: layer.transform.opacity / 100,
    blur: 0,
  };

  if (phase === 'idle' || phase === 'complete') {
    return { ...defaultState, opacity: 0 };
  }

  if (phase === 'hold') {
    return defaultState;
  }

  const animation = phase === 'enter' ? layer.entryAnimation : layer.exitAnimation;
  if (!animation) {
    return phase === 'exit' ? { ...defaultState, opacity: 0 } : defaultState;
  }

  const startTransform = getAnimationStartTransform(
    animation.style,
    animation.direction || 'left',
    animation.blur,
    animation.scale,
    animation.rotation
  );

  const easedProgress = applyEasing(progress, animation.easing);

  if (phase === 'enter') {
    const interpolated = lerpTransform(startTransform, baseTransform, easedProgress, 'linear');
    return {
      transform: interpolated,
      opacity: interpolated.opacity / 100,
      blur: animation.blur ? animation.blur * (1 - easedProgress) : 0,
    };
  } else {
    // Exit - animate from base to start
    const interpolated = lerpTransform(baseTransform, startTransform, easedProgress, 'linear');
    return {
      transform: interpolated,
      opacity: interpolated.opacity / 100,
      blur: animation.blur ? animation.blur * easedProgress : 0,
    };
  }
}

function getLayerStyles(state: LayerRenderState, width: number, height: number): CSSProperties {
  const { transform, opacity, blur } = state;

  const x = (transform.x / 100) * width;
  const y = (transform.y / 100) * height;

  return {
    transform: `translate(${x}px, ${y}px) scale(${transform.scaleX / 100}, ${transform.scaleY / 100}) rotate(${transform.rotation}deg)`,
    opacity,
    filter: blur > 0 ? `blur(${blur}px)` : undefined,
  };
}

const TextLayerRenderer: FC<{
  layer: GraphicLayer;
  state: LayerRenderState;
  customValues: Record<string, string | number | boolean>;
  width: number;
  height: number;
}> = ({ layer, state, customValues, width, height }) => {
  const styles = useStyles();
  const textProps = layer.textProperties;
  if (!textProps) return null;

  // Get custom text content if available
  const textFieldId = layer.id;
  const customText = customValues[textFieldId] as string | undefined;
  const displayText = customText || textProps.content;

  // Get custom colors
  const customTextColor = customValues['textColor'] as string | undefined;
  const customAccentColor = customValues['accentColor'] as string | undefined;
  const textColor = customTextColor || customAccentColor || textProps.color;

  const layerStyles = getLayerStyles(state, width, height);

  const textStyles: CSSProperties = {
    ...layerStyles,
    fontFamily: textProps.fontFamily,
    fontSize: `${textProps.fontSize}px`,
    fontWeight: textProps.fontWeight,
    fontStyle: textProps.fontStyle,
    textAlign: textProps.textAlign,
    lineHeight: textProps.lineHeight,
    letterSpacing: `${textProps.letterSpacing}px`,
    color: textColor,
    textTransform: textProps.textTransform || 'none',
  };

  if (textProps.strokeColor && textProps.strokeWidth) {
    textStyles.WebkitTextStroke = `${textProps.strokeWidth}px ${textProps.strokeColor}`;
  }

  if (textProps.shadow) {
    textStyles.textShadow = `${textProps.shadow.offsetX}px ${textProps.shadow.offsetY}px ${textProps.shadow.blur}px ${textProps.shadow.color}`;
  }

  return (
    <div className={`${styles.layer} ${styles.textLayer}`} style={textStyles}>
      {displayText}
    </div>
  );
};

const ShapeLayerRenderer: FC<{
  layer: GraphicLayer;
  state: LayerRenderState;
  customValues: Record<string, string | number | boolean>;
  width: number;
  height: number;
}> = ({ layer, state, customValues, width, height }) => {
  const styles = useStyles();
  const shapeProps = layer.shapeProperties;
  if (!shapeProps) return null;

  const layerStyles = getLayerStyles(state, width, height);

  // Get custom colors
  const customColor = (customValues['color'] ||
    customValues['accentColor'] ||
    customValues[`${layer.id}Color`]) as string | undefined;
  const fillColor = customColor || shapeProps.fillColor;

  let shapeElement: React.ReactNode = null;

  switch (shapeProps.shapeType) {
    case 'rectangle':
      shapeElement = (
        <div
          style={{
            width: `${shapeProps.width}px`,
            height: `${shapeProps.height}px`,
            backgroundColor: fillColor,
            borderRadius: `${shapeProps.cornerRadius || 0}px`,
            border: shapeProps.strokeColor
              ? `${shapeProps.strokeWidth || 1}px solid ${shapeProps.strokeColor}`
              : undefined,
          }}
        />
      );
      break;

    case 'circle':
      shapeElement = (
        <div
          style={{
            width: `${(shapeProps.radius || 20) * 2}px`,
            height: `${(shapeProps.radius || 20) * 2}px`,
            backgroundColor: fillColor === 'transparent' ? undefined : fillColor,
            borderRadius: '50%',
            border: shapeProps.strokeColor
              ? `${shapeProps.strokeWidth || 1}px solid ${shapeProps.strokeColor}`
              : undefined,
          }}
        />
      );
      break;

    case 'line':
      shapeElement = (
        <div
          style={{
            width: `${shapeProps.width || 100}px`,
            height: `${shapeProps.height || 2}px`,
            backgroundColor: fillColor,
            borderRadius: `${shapeProps.cornerRadius || 0}px`,
          }}
        />
      );
      break;

    default:
      shapeElement = (
        <div
          style={{
            width: `${shapeProps.width || 50}px`,
            height: `${shapeProps.height || 50}px`,
            backgroundColor: fillColor,
          }}
        />
      );
  }

  return (
    <div className={`${styles.layer} ${styles.shapeLayer}`} style={layerStyles}>
      {shapeElement}
    </div>
  );
};

export const GraphicRenderer: FC<GraphicRendererProps> = ({
  graphic,
  currentTime,
  width,
  height,
  className,
}) => {
  const styles = useStyles();
  const graphicsStore = useMotionGraphicsStore();
  const asset = graphicsStore.getAsset(graphic.assetId);

  // Calculate animation phase and progress
  const animationState = useMemo(() => {
    if (!asset) return null;

    const relativeTime = currentTime - graphic.startTime;
    const enterDuration = 0.4; // Default entry duration
    const exitDuration = 0.3; // Default exit duration
    const holdDuration = graphic.duration - enterDuration - exitDuration;

    const state = createAnimationState();
    state.startTime = 0;

    return updateAnimationState(
      state,
      relativeTime,
      enterDuration,
      Math.max(0, holdDuration),
      exitDuration
    );
  }, [asset, graphic, currentTime]);

  if (!asset || !animationState) return null;

  // Check if graphic is visible
  const relativeTime = currentTime - graphic.startTime;
  if (relativeTime < 0 || relativeTime > graphic.duration) {
    return null;
  }

  return (
    <div
      className={`${styles.container} ${className || ''}`}
      style={{
        width: `${width}px`,
        height: `${height}px`,
        left: 0,
        top: 0,
      }}
    >
      {asset.layers.map((layer) => {
        if (!layer.visible) return null;

        const layerState = calculateLayerState(
          layer,
          animationState.phase,
          animationState.progress
        );

        if (layer.type === 'text') {
          return (
            <TextLayerRenderer
              key={layer.id}
              layer={layer}
              state={layerState}
              customValues={graphic.customValues}
              width={width}
              height={height}
            />
          );
        }

        if (layer.type === 'shape') {
          return (
            <ShapeLayerRenderer
              key={layer.id}
              layer={layer}
              state={layerState}
              customValues={graphic.customValues}
              width={width}
              height={height}
            />
          );
        }

        return null;
      })}
    </div>
  );
};

export default GraphicRenderer;
