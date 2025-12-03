/**
 * ClipWaveform Component
 *
 * Wrapper component for displaying waveforms in timeline clips.
 * Automatically loads waveform data and handles loading/error states.
 */

import { useEffect, type FC } from 'react';
import { useWaveformStore } from '../../../stores/opencutWaveforms';
import { openCutTokens } from '../../../styles/tokens';
import { WaveformDisplay } from './WaveformDisplay';

interface ClipWaveformProps {
  /** Unique identifier for the clip/media */
  mediaId: string;
  /** URL to the audio file */
  audioUrl: string;
  /** Width of the waveform in pixels */
  width: number;
  /** Height of the waveform in pixels */
  height: number;
  /** Type of clip (affects color) */
  clipType: 'audio' | 'video';
  /** Trim start time in seconds */
  trimStart?: number;
  /** Trim end time in seconds */
  trimEnd?: number;
  /** Actual clip duration (may differ from audio duration) */
  clipDuration?: number;
  /** Number of samples to generate (affects detail level) */
  samples?: number;
}

export const ClipWaveform: FC<ClipWaveformProps> = ({
  mediaId,
  audioUrl,
  width,
  height,
  clipType,
  trimStart = 0,
  trimEnd = 0,
  clipDuration,
  samples = 200,
}) => {
  const { loadWaveform, getWaveform, isLoading } = useWaveformStore();

  useEffect(() => {
    if (audioUrl) {
      loadWaveform(mediaId, audioUrl, samples);
    }
  }, [mediaId, audioUrl, samples, loadWaveform]);

  const waveformData = getWaveform(mediaId);
  const loading = isLoading(mediaId);

  const color = clipType === 'audio' ? openCutTokens.waveform.audio : openCutTokens.waveform.video;

  return (
    <WaveformDisplay
      waveformData={waveformData ?? null}
      width={width}
      height={height}
      color={color}
      isLoading={loading}
      trimStart={trimStart}
      trimEnd={trimEnd}
      clipDuration={clipDuration}
    />
  );
};

export default ClipWaveform;
