/**
 * Visual test component for UI scaling
 *
 * This component provides a visual demonstration of the smart uniform UI scaling feature.
 * It displays a grid of elements at different window sizes to verify scaling behavior.
 */

import { Button, Card } from '@fluentui/react-components';
import { useState } from 'react';
import { useUIScale } from '../hooks/useUIScale';

export function UIScalingTestComponent() {
  const [mode, setMode] = useState<'fill' | 'contain'>('fill');
  const { scale, scaledWidth, scaledHeight, windowWidth, windowHeight } = useUIScale({
    mode,
    debounceDelay: 150,
  });

  return (
    <div style={{ padding: '20px', fontFamily: 'monospace' }}>
      <Card style={{ padding: '20px', marginBottom: '20px' }}>
        <h2>UI Scaling Test Component</h2>

        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'auto 1fr',
            gap: '10px',
            marginTop: '20px',
          }}
        >
          <strong>Window Size:</strong>
          <span>
            {windowWidth} × {windowHeight}
          </span>

          <strong>Scale Factor:</strong>
          <span>
            {scale.toFixed(3)} ({(scale * 100).toFixed(1)}%)
          </span>

          <strong>Scaled Dimensions:</strong>
          <span>
            {scaledWidth.toFixed(0)} × {scaledHeight.toFixed(0)}
          </span>

          <strong>Mode:</strong>
          <span>{mode}</span>
        </div>

        <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
          <Button
            appearance={mode === 'fill' ? 'primary' : 'secondary'}
            onClick={() => setMode('fill')}
          >
            Fill Mode
          </Button>
          <Button
            appearance={mode === 'contain' ? 'primary' : 'secondary'}
            onClick={() => setMode('contain')}
          >
            Contain Mode
          </Button>
        </div>
      </Card>

      <Card style={{ padding: '20px' }}>
        <h3>Visual Test Grid</h3>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(4, 1fr)',
            gap: '10px',
            marginTop: '20px',
          }}
        >
          {Array.from({ length: 12 }).map((_, i) => (
            <div
              key={i}
              style={{
                padding: '20px',
                background: `hsl(${i * 30}, 70%, 50%)`,
                color: 'white',
                textAlign: 'center',
                borderRadius: '8px',
                fontWeight: 'bold',
              }}
            >
              Box {i + 1}
            </div>
          ))}
        </div>
      </Card>

      <Card style={{ padding: '20px', marginTop: '20px' }}>
        <h3>Instructions</h3>
        <ol style={{ lineHeight: '1.8' }}>
          <li>Resize your browser window to different sizes</li>
          <li>Observe how the scale factor changes</li>
          <li>Verify all UI elements scale proportionally</li>
          <li>Test "Fill" vs "Contain" modes</li>
          <li>Verify buttons and interactive elements work at all scales</li>
        </ol>
      </Card>
    </div>
  );
}
