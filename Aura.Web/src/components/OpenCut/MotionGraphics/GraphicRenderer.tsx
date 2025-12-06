/**
 * GraphicRenderer Component
 *
 * Canvas/SVG renderer for motion graphics with layer composition,
 * animation playback engine, and transform handling.
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { useRef, useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import type {
  AppliedGraphic,
  MotionGraphicAsset,
  GraphicLayer,
  AnimationState,
} from '../../../types/motionGraphics';
import { evaluateEasing } from '../../../utils/motionGraphicsAnimation';
import { CalloutRenderer } from './CalloutRenderer';
import { LowerThirdRenderer } from './LowerThirdRenderer';
import { SocialRenderer } from './SocialRenderer';
import { TitleRenderer } from './TitleRenderer';

export interface GraphicRendererProps {
  /** The applied graphic instance */
  graphic: AppliedGraphic;
  /** Current playback time in seconds (relative to graphic start) */
  currentTime: number;
  /** Canvas width */
  width: number;
  /** Canvas height */
  height: number;
  /** Whether the graphic is selected */
  isSelected?: boolean;
  /** Optional className */
  className?: string;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    pointerEvents: 'none',
    overflow: 'visible',
  },
  containerSelected: {
    pointerEvents: 'auto',
    outline: '2px dashed rgba(59, 130, 246, 0.6)',
    outlineOffset: '4px',
    borderRadius: '4px',
  },
});

/**
 * Calculate animation state based on current time
 */
function calculateAnimationState(
  currentTime: number,
  duration: number,
  entryDuration: number = 0.5,
  exitDuration: number = 0.4
): AnimationState {
  if (currentTime < 0) {
    return { phase: 'entry', progress: 0, elapsed: 0, isPlaying: false };
  }

  if (currentTime < entryDuration) {
    return {
      phase: 'entry',
      progress: currentTime / entryDuration,
      elapsed: currentTime,
      isPlaying: true,
    };
  }

  const holdEnd = duration - exitDuration;
  if (currentTime < holdEnd) {
    return {
      phase: 'hold',
      progress: 1,
      elapsed: currentTime,
      isPlaying: true,
    };
  }

  if (currentTime < duration) {
    const exitProgress = (currentTime - holdEnd) / exitDuration;
    return {
      phase: 'exit',
      progress: 1 - exitProgress,
      elapsed: currentTime,
      isPlaying: true,
    };
  }

  return { phase: 'exit', progress: 0, elapsed: duration, isPlaying: false };
}

/**
 * Apply transforms and opacity based on animation state
 */
function getAnimatedTransform(
  state: AnimationState,
  positionX: number,
  positionY: number,
  scale: number,
  opacity: number
): {
  x: number;
  y: number;
  scale: number;
  opacity: number;
} {
  const eased = evaluateEasing('easeOutCubic', state.progress);

  if (state.phase === 'entry') {
    // Slide in from left with fade
    return {
      x: positionX - (1 - eased) * 20,
      y: positionY,
      scale: scale * (0.9 + eased * 0.1),
      opacity: opacity * eased,
    };
  }

  if (state.phase === 'exit') {
    // Fade and slide out
    return {
      x: positionX + (1 - state.progress) * 20,
      y: positionY,
      scale: scale * (0.9 + state.progress * 0.1),
      opacity: opacity * state.progress,
    };
  }

  // Hold phase
  return { x: positionX, y: positionY, scale, opacity };
}

export const GraphicRenderer: FC<GraphicRendererProps> = ({
  graphic,
  currentTime,
  width,
  height,
  isSelected = false,
  className,
}) => {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);
  const graphicsStore = useMotionGraphicsStore();

  const asset = useMemo(() => {
    return graphicsStore.getAsset(graphic.assetId);
  }, [graphicsStore, graphic.assetId]);

  // Calculate animation state
  const entryDuration = Number(graphic.customValues['entryDuration'] ?? 0.5);
  const exitDuration = Number(graphic.customValues['exitDuration'] ?? 0.4);
  const animationState = useMemo(() => {
    return calculateAnimationState(currentTime, graphic.duration, entryDuration, exitDuration);
  }, [currentTime, graphic.duration, entryDuration, exitDuration]);

  // Get animated transforms
  const transforms = useMemo(() => {
    return getAnimatedTransform(
      animationState,
      graphic.positionX,
      graphic.positionY,
      graphic.scale,
      graphic.opacity
    );
  }, [animationState, graphic.positionX, graphic.positionY, graphic.scale, graphic.opacity]);

  if (!asset) {
    return null;
  }

  // Select the appropriate renderer based on category
  const renderGraphicContent = () => {
    const commonProps = {
      asset,
      graphic,
      animationState,
      width,
      height,
    };

    switch (asset.category) {
      case 'lower-thirds':
        return <LowerThirdRenderer {...commonProps} />;
      case 'callouts':
        return <CalloutRenderer {...commonProps} />;
      case 'social':
        return <SocialRenderer {...commonProps} />;
      case 'titles':
        return <TitleRenderer {...commonProps} />;
      default:
        // Default SVG rendering for shapes and other categories
        return <DefaultGraphicContent {...commonProps} />;
    }
  };

  const containerStyle: React.CSSProperties = {
    left: `${transforms.x}%`,
    top: `${transforms.y}%`,
    transform: `translate(-50%, -50%) scale(${transforms.scale})`,
    opacity: transforms.opacity,
    transition: isSelected ? 'none' : 'opacity 50ms ease-out',
  };

  return (
    <div
      ref={containerRef}
      className={mergeClasses(styles.container, isSelected && styles.containerSelected, className)}
      style={containerStyle}
    >
      {renderGraphicContent()}
    </div>
  );
};

/**
 * Default graphic content renderer for shapes and generic graphics
 */
interface DefaultGraphicContentProps {
  asset: MotionGraphicAsset;
  graphic: AppliedGraphic;
  animationState: AnimationState;
  width: number;
  height: number;
}

const DefaultGraphicContent: FC<DefaultGraphicContentProps> = ({
  asset,
  graphic,
  animationState,
}) => {
  return (
    <svg width={300} height={100} viewBox="0 0 300 100" style={{ overflow: 'visible' }}>
      {asset.layers.map((layer) => (
        <LayerRenderer
          key={layer.id}
          layer={layer}
          graphic={graphic}
          animationState={animationState}
        />
      ))}
    </svg>
  );
};

/**
 * Individual layer renderer
 */
interface LayerRendererProps {
  layer: GraphicLayer;
  graphic: AppliedGraphic;
  animationState: AnimationState;
}

const LayerRenderer: FC<LayerRendererProps> = ({ layer, graphic, animationState }) => {
  const transform = layer.transform;
  const opacity = transform.opacity * animationState.progress;

  if (layer.type === 'text' && layer.textProperties) {
    const props = layer.textProperties;
    // Get customized text value if available
    const customText = graphic.customValues[layer.id.replace(/^lt-\w+-/, '')] as string | undefined;
    const text = customText ?? props.content;

    return (
      <text
        x={transform.x}
        y={transform.y}
        fill={props.color}
        fontSize={props.fontSize}
        fontFamily={props.fontFamily}
        fontWeight={props.fontWeight}
        opacity={opacity}
        textAnchor={
          props.textAlign === 'center' ? 'middle' : props.textAlign === 'right' ? 'end' : 'start'
        }
        dominantBaseline="middle"
        style={{
          textShadow: props.shadow
            ? `${props.shadow.offsetX}px ${props.shadow.offsetY}px ${props.shadow.blur}px ${props.shadow.color}`
            : undefined,
        }}
      >
        {text}
      </text>
    );
  }

  if (layer.type === 'shape' && layer.shapeProperties) {
    const props = layer.shapeProperties;

    if (props.shape === 'rectangle') {
      return (
        <rect
          x={transform.x - props.width / 2}
          y={transform.y - props.height / 2}
          width={props.width}
          height={props.height}
          fill={props.fill ?? 'none'}
          stroke={props.strokeColor}
          strokeWidth={props.strokeWidth}
          rx={props.cornerRadius}
          ry={props.cornerRadius}
          opacity={opacity}
        />
      );
    }

    if (props.shape === 'ellipse') {
      return (
        <ellipse
          cx={transform.x}
          cy={transform.y}
          rx={props.width / 2}
          ry={props.height / 2}
          fill={props.fill ?? 'none'}
          stroke={props.strokeColor}
          strokeWidth={props.strokeWidth}
          opacity={opacity}
        />
      );
    }

    if (props.shape === 'line') {
      return (
        <line
          x1={transform.x}
          y1={transform.y}
          x2={transform.x + props.width}
          y2={transform.y}
          stroke={props.fill ?? props.strokeColor ?? '#FFFFFF'}
          strokeWidth={props.strokeWidth ?? props.height}
          opacity={opacity}
        />
      );
    }
  }

  return null;
};

export default GraphicRenderer;
