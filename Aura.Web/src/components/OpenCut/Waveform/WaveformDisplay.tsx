/**
 * Waveform Display Component for OpenCut
 *
 * Renders audio waveform visualization using canvas for high performance.
 * Supports trim points, loading states, and custom colors.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useEffect, useRef } from 'react';
import type { FC } from 'react';

import { openCutTokens } from '../../../styles/designTokens';
import type { WaveformData } from '../../../stores/opencutWaveforms';

/** Height scale factor for waveform bars (0-1) */
const WAVEFORM_HEIGHT_SCALE = 0.8;

/** Maximum corner radius for rounded bars */
const MAX_BAR_CORNER_RADIUS = 2;

interface WaveformDisplayProps {
  /** Waveform peak data to display */
  waveformData: WaveformData | null;
  /** Width of the waveform display in pixels */
  width: number;
  /** Height of the waveform display in pixels */
  height: number;
  /** Color for waveform bars */
  color?: string;
  /** Background color */
  backgroundColor?: string;
  /** Trim start time in seconds */
  trimStart?: number;
  /** Trim end time in seconds */
  trimEnd?: number;
  /** Total clip duration for trim calculations */
  clipDuration?: number;
  /** Whether waveform is currently loading */
  isLoading?: boolean;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    overflow: 'hidden',
  },
  canvas: {
    display: 'block',
  },
  loading: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.3)',
  },
  loadingBar: {
    width: '60%',
    height: '4px',
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    borderRadius: openCutTokens.radius.full,
    overflow: 'hidden',
  },
  loadingProgress: {
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    animation: 'waveformLoading 1.5s ease-in-out infinite',
    width: '30%',
  },
});

// Add keyframes for loading animation via style injection
const loadingKeyframes = `
@keyframes waveformLoading {
  0% { transform: translateX(-100%); }
  100% { transform: translateX(400%); }
}
`;

// Inject keyframes once
if (typeof document !== 'undefined') {
  const styleId = 'waveform-loading-keyframes';
  if (!document.getElementById(styleId)) {
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = loadingKeyframes;
    document.head.appendChild(style);
  }
}

/**
 * Draw a rounded rectangle with fallback for older browsers
 */
function drawRoundedRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number
): void {
  if (typeof ctx.roundRect === 'function') {
    ctx.roundRect(x, y, width, height, radius);
  } else {
    // Fallback for browsers without roundRect support
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
  }
}

/**
 * WaveformDisplay renders audio waveform visualization on a canvas
 */
export const WaveformDisplay: FC<WaveformDisplayProps> = ({
  waveformData,
  width,
  height,
  color = '#22C55E',
  backgroundColor = 'transparent',
  trimStart = 0,
  trimEnd = 0,
  clipDuration,
  isLoading = false,
}) => {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || !waveformData) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Handle high DPI displays
    const dpr = window.devicePixelRatio || 1;
    canvas.width = width * dpr;
    canvas.height = height * dpr;
    ctx.scale(dpr, dpr);

    // Clear canvas
    ctx.fillStyle = backgroundColor;
    ctx.fillRect(0, 0, width, height);

    const { peaks, duration } = waveformData;
    const actualDuration = clipDuration ?? duration;

    // Calculate visible range based on trim
    const startRatio = actualDuration > 0 ? trimStart / actualDuration : 0;
    const endRatio = actualDuration > 0 ? 1 - trimEnd / actualDuration : 1;
    const startIndex = Math.floor(startRatio * peaks.length);
    const endIndex = Math.ceil(endRatio * peaks.length);
    const visiblePeaks = peaks.slice(startIndex, Math.max(startIndex + 1, endIndex));

    if (visiblePeaks.length === 0) return;

    const barWidth = width / visiblePeaks.length;
    const centerY = height / 2;

    ctx.fillStyle = color;

    visiblePeaks.forEach((peak, i) => {
      const barHeight = Math.max(peak * height * WAVEFORM_HEIGHT_SCALE, 1);
      const x = i * barWidth;
      const y = centerY - barHeight / 2;

      // Draw rounded bars with fallback
      const radius = Math.min(barWidth / 2, MAX_BAR_CORNER_RADIUS);
      const barW = Math.max(barWidth - 1, 1);

      ctx.beginPath();
      drawRoundedRect(ctx, x, y, barW, barHeight, radius);
      ctx.fill();
    });
  }, [waveformData, width, height, color, backgroundColor, trimStart, trimEnd, clipDuration]);

  return (
    <div className={styles.container} style={{ width, height }}>
      <canvas ref={canvasRef} className={styles.canvas} style={{ width, height }} />
      {isLoading && (
        <div className={styles.loading}>
          <div className={styles.loadingBar}>
            <div className={styles.loadingProgress} />
          </div>
        </div>
      )}
    </div>
  );
};

export default WaveformDisplay;
