import React from 'react';
import { VideoEffect } from '../../types/videoEffects';

interface EffectsTimelineProps {
  videoDuration: number;
  currentTime: number;
  effects: VideoEffect[];
  onTimeChange: (time: number) => void;
  onEffectSelect?: (effect: VideoEffect) => void;
  onEffectMove?: (effectId: string, newStartTime: number) => void;
  onEffectResize?: (effectId: string, newDuration: number) => void;
}

/**
 * Placeholder for EffectsTimeline
 * Original implementation requires @mui/material which is not installed
 */
export const EffectsTimeline: React.FC<EffectsTimelineProps> = ({
  videoDuration,
  currentTime,
  effects,
}) => {
  return (
    <div style={{ padding: '20px', border: '1px solid #ccc' }}>
      <h3>Effects Timeline</h3>
      <p>Duration: {videoDuration}s | Current: {currentTime}s</p>
      <p>Effects: {effects.length}</p>
      <p style={{ color: '#666', fontSize: '12px' }}>
        Note: Full timeline UI requires @mui/material installation
      </p>
    </div>
  );
};
