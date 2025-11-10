import React from 'react';
import { VideoEffect } from '../../types/videoEffects';

interface EffectsLibraryProps {
  onEffectAdd: (effect: VideoEffect) => void;
  onPresetApply?: (presetId: string) => void;
}

/**
 * Placeholder for EffectsLibrary
 * Original implementation requires @mui/material which is not installed
 */
export const EffectsLibrary: React.FC<EffectsLibraryProps> = () => {
  return (
    <div style={{ padding: '20px', border: '1px solid #ccc' }}>
      <h3>Effects Library</h3>
      <p>Browse and apply video effects</p>
      <p style={{ color: '#666', fontSize: '12px' }}>
        Note: Full effects library UI requires @mui/material installation
      </p>
    </div>
  );
};
