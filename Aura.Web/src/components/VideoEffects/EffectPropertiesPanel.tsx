import React from 'react';
import { VideoEffect } from '../../types/videoEffects';

interface EffectPropertiesPanelProps {
  effect: VideoEffect | null;
  onEffectChange: (effect: VideoEffect) => void;
  onClose: () => void;
  onPreview?: (effect: VideoEffect) => void;
}

/**
 * Placeholder for EffectPropertiesPanel
 * Original implementation requires @mui/material which is not installed
 */
export const EffectPropertiesPanel: React.FC<EffectPropertiesPanelProps> = ({
  effect,
  onClose,
}) => {
  return (
    <div style={{ padding: '20px', border: '1px solid #ccc' }}>
      <h3>Effect Properties Panel</h3>
      <p>Effect: {effect?.name || 'None'}</p>
      <button onClick={onClose}>Close</button>
      <p style={{ color: '#666', fontSize: '12px' }}>
        Note: Full effect editing UI requires @mui/material installation
      </p>
    </div>
  );
};
