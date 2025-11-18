/**
 * Waveform Display Component
 * 
 * Renders audio waveform visualization for timeline clips
 */

import { makeStyles } from '@fluentui/react-components';
import { useEffect, useState, useRef } from 'react';
import { waveformService, type WaveformData } from '../../services/waveformService';

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    padding: '4px',
    opacity: 0.6,
    pointerEvents: 'none',
  },
  canvas: {
    width: '100%',
    height: '100%',
  },
});

interface WaveformDisplayProps {
  audioPath: string;
  width: number;
  height: number;
  color?: string;
  backgroundColor?: string;
}

export function WaveformDisplay({
  audioPath,
  width,
  height,
  color = 'rgba(255, 255, 255, 0.8)',
  backgroundColor = 'transparent',
}: WaveformDisplayProps) {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [waveformData, setWaveformData] = useState<WaveformData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const loadWaveform = async () => {
      try {
        const samples = Math.floor(width / 2);
        const data = await waveformService.generateWaveform({
          audioPath,
          targetSamples: samples,
        });
        
        if (mounted) {
          setWaveformData(data);
          setError(null);
        }
      } catch (err) {
        if (mounted) {
          console.error('Failed to load waveform:', err);
          setError(err instanceof Error ? err.message : 'Failed to load waveform');
        }
      }
    };

    if (audioPath) {
      loadWaveform();
    }

    return () => {
      mounted = false;
    };
  }, [audioPath, width]);

  useEffect(() => {
    if (!waveformData || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    canvas.width = width;
    canvas.height = height;

    ctx.fillStyle = backgroundColor;
    ctx.fillRect(0, 0, width, height);

    ctx.fillStyle = color;
    ctx.strokeStyle = color;
    ctx.lineWidth = 1;

    const data = waveformData.data;
    const barWidth = width / data.length;
    const centerY = height / 2;
    const maxHeight = height * 0.9;

    ctx.beginPath();
    for (let i = 0; i < data.length; i++) {
      const x = i * barWidth;
      const amplitude = Math.abs(data[i]);
      const barHeight = amplitude * maxHeight;
      
      const y1 = centerY - barHeight / 2;
      const y2 = centerY + barHeight / 2;
      
      ctx.fillRect(x, y1, Math.max(1, barWidth - 0.5), y2 - y1);
    }

    ctx.stroke();
  }, [waveformData, width, height, color, backgroundColor]);

  if (error) {
    return null;
  }

  return (
    <div className={styles.container}>
      <canvas ref={canvasRef} className={styles.canvas} />
    </div>
  );
}
