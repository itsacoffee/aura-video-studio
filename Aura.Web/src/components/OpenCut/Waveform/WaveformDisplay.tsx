/**
 * WaveformDisplay Component
 *
 * Canvas-based waveform visualization component.
 * Renders audio waveform peaks with support for trimming and loading states.
 */

import { makeStyles, tokens } from '@fluentui/react-components';
import { useEffect, useRef, type FC } from 'react';
import { openCutTokens } from '../../../styles/tokens';
import type { WaveformPeaksData } from '../../../services/waveformService';

interface WaveformDisplayProps {
  waveformData: WaveformPeaksData | null;
  width: number;
  height: number;
  color?: string;
  backgroundColor?: string;
  trimStart?: number;
  trimEnd?: number;
  clipDuration?: number;
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
    backgroundColor: openCutTokens.waveform.loading,
    borderRadius: openCutTokens.radius.full,
    overflow: 'hidden',
  },
  loadingProgress: {
    height: '100%',
    width: '30%',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: openCutTokens.radius.full,
    animationName: {
      '0%': { transform: 'translateX(-100%)' },
      '100%': { transform: 'translateX(400%)' },
    },
    animationDuration: '1.5s',
    animationTimingFunction: 'ease-in-out',
    animationIterationCount: 'infinite',
  },
});

export const WaveformDisplay: FC<WaveformDisplayProps> = ({
  waveformData,
  width,
  height,
  color = openCutTokens.waveform.audio,
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
    const startRatio = duration > 0 ? trimStart / duration : 0;
    const endRatio = duration > 0 ? 1 - trimEnd / duration : 1;
    const startIndex = Math.floor(startRatio * peaks.length);
    const endIndex = Math.ceil(endRatio * peaks.length);
    const visiblePeaks = peaks.slice(startIndex, endIndex);

    if (visiblePeaks.length === 0) return;

    const barWidth = width / visiblePeaks.length;
    const centerY = height / 2;

    ctx.fillStyle = color;

    visiblePeaks.forEach((peak, i) => {
      const barHeight = Math.max(peak * height * 0.8, 1);
      const x = i * barWidth;
      const y = centerY - barHeight / 2;

      // Draw rounded bars
      const radius = Math.min(barWidth / 2, 2);
      const barActualWidth = Math.max(barWidth - 1, 1);

      ctx.beginPath();
      if (typeof ctx.roundRect === 'function') {
        ctx.roundRect(x, y, barActualWidth, barHeight, radius);
      } else {
        // Fallback with proper rounded corners for older browsers
        const r = Math.min(radius, barActualWidth / 2, barHeight / 2);
        ctx.moveTo(x + r, y);
        ctx.lineTo(x + barActualWidth - r, y);
        ctx.quadraticCurveTo(x + barActualWidth, y, x + barActualWidth, y + r);
        ctx.lineTo(x + barActualWidth, y + barHeight - r);
        ctx.quadraticCurveTo(
          x + barActualWidth,
          y + barHeight,
          x + barActualWidth - r,
          y + barHeight
        );
        ctx.lineTo(x + r, y + barHeight);
        ctx.quadraticCurveTo(x, y + barHeight, x, y + barHeight - r);
        ctx.lineTo(x, y + r);
        ctx.quadraticCurveTo(x, y, x + r, y);
        ctx.closePath();
      }
      ctx.fill();
    });

    // Apply fade effect for actual duration difference
    if (actualDuration && actualDuration < duration) {
      const fadeStartX = (actualDuration / duration) * width;
      const gradient = ctx.createLinearGradient(fadeStartX - 20, 0, fadeStartX, 0);
      gradient.addColorStop(0, 'rgba(0, 0, 0, 0)');
      gradient.addColorStop(1, 'rgba(0, 0, 0, 0.5)');
      ctx.fillStyle = gradient;
      ctx.fillRect(fadeStartX - 20, 0, 20, height);
      ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
      ctx.fillRect(fadeStartX, 0, width - fadeStartX, height);
    }
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
