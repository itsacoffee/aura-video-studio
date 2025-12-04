/**
 * ClipWaveform Component for OpenCut Timeline
 *
 * Wrapper component that manages waveform generation and display for timeline clips.
 * Automatically loads waveform on mount and handles loading/error states.
 */

import { useEffect } from 'react';
import type { FC } from 'react';

import { useWaveformStore } from '../../../stores/opencutWaveforms';

import { WaveformDisplay } from './WaveformDisplay';

interface ClipWaveformProps {
  /** Unique media ID for caching */
  mediaId: string;
  /** URL of the audio file */
  audioUrl: string;
  /** Width of the waveform display */
  width: number;
  /** Height of the waveform display */
  height: number;
  /** Color for waveform bars (defaults to green for audio) */
  color?: string;
  /** Background color */
  backgroundColor?: string;
  /** Trim start time in seconds */
  trimStart?: number;
  /** Trim end time in seconds */
  trimEnd?: number;
  /** Total clip duration */
  clipDuration?: number;
  /** Number of samples to generate (adjusts with zoom) */
  samples?: number;
}

/**
 * ClipWaveform handles waveform generation and display for timeline audio clips
 */
export const ClipWaveform: FC<ClipWaveformProps> = ({
  mediaId,
  audioUrl,
  width,
  height,
  color = '#22C55E',
  backgroundColor = 'transparent',
  trimStart = 0,
  trimEnd = 0,
  clipDuration,
  samples = 200,
}) => {
  const { loadWaveform, getWaveform, isLoading } = useWaveformStore();

  // Load waveform on mount or when parameters change
  useEffect(() => {
    if (mediaId && audioUrl) {
      loadWaveform(mediaId, audioUrl, samples);
    }
  }, [mediaId, audioUrl, samples, loadWaveform]);

  const waveformData = getWaveform(mediaId);
  const loading = isLoading(mediaId);

  return (
    <WaveformDisplay
      waveformData={waveformData ?? null}
      width={width}
      height={height}
      color={color}
      backgroundColor={backgroundColor}
      trimStart={trimStart}
      trimEnd={trimEnd}
      clipDuration={clipDuration}
      isLoading={loading}
    />
  );
};

export default ClipWaveform;
