/**
 * Example: Integrating Chroma Key and Compositing Features
 * 
 * This file demonstrates how to integrate the new chroma key and compositing
 * features into your video editor workflow.
 */

import { useState, useEffect } from 'react';
import { CompositingPanel } from '../components/Compositing/CompositingPanel';
import { AppliedEffect } from '../types/effects';
import { motionTracker } from '../services/motionTrackingService';

/**
 * Example 1: Basic Chroma Key Usage
 * 
 * This shows how to add a chroma key effect to a video clip
 */
export function BasicChromaKeyExample() {
  const [chromaKeyEffect, setChromaKeyEffect] = useState<AppliedEffect>({
    id: 'chroma-key-1',
    effectType: 'chroma-key',
    enabled: true,
    parameters: {
      keyColor: '#00ff00',
      similarity: 40,
      smoothness: 8,
      spillSuppression: 15,
      edgeThickness: 0,
      edgeFeather: 1,
      choke: 0,
      matteCleanup: 0,
    },
  });

  const handleEffectUpdate = (effect: AppliedEffect) => {
    setChromaKeyEffect(effect);
    // Apply the effect to your video clip
  };

  const handleEffectRemove = () => {
    // Remove the effect from your video clip
    // eslint-disable-next-line no-console
    console.log('Effect removed');
  };

  return (
    <CompositingPanel
      chromaKeyEffect={chromaKeyEffect}
      onEffectUpdate={handleEffectUpdate}
      onEffectRemove={handleEffectRemove}
    />
  );
}

/**
 * Example 2: Multi-Layer Compositing
 * 
 * This shows how to composite multiple video layers with different blend modes
 */
export function MultiLayerCompositingExample() {
  // Example: Composite a keyed foreground over a background
  
  const foregroundEffect: AppliedEffect = {
    id: 'fg-chroma',
    effectType: 'chroma-key',
    enabled: true,
    parameters: {
      keyColor: '#00ff00',
      similarity: 45,
      smoothness: 10,
      spillSuppression: 20,
      edgeThickness: 0,
      edgeFeather: 2,
      choke: 0,
      matteCleanup: 15,
    },
  };

  const blendEffect: AppliedEffect = {
    id: 'blend-1',
    effectType: 'blend-mode',
    enabled: true,
    parameters: {
      mode: 'normal',
      opacity: 100,
    },
  };

  // In your video processing:
  // 1. Apply chroma key to foreground
  // 2. Render background layer
  // 3. Composite foreground over background using blend mode
  
  // Suppress unused variable warnings for example code
  void foregroundEffect;
  void blendEffect;
  
  return <div>Multi-layer compositing example</div>;
}

/**
 * Example 3: Motion Tracking Integration
 * 
 * This shows how to track a point and use it to position graphics
 */
export function MotionTrackingExample() {
  const [isTracking, setIsTracking] = useState(false);

  useEffect(() => {
    if (isTracking) {
      // Start tracking at current frame
      const canvas = document.querySelector('canvas'); // Your video canvas
      const ctx = canvas?.getContext('2d');
      
      if (ctx && canvas) {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const currentTime = 0; // Get from video player
        
        // Track the point
        const tracked = motionTracker.trackFrame('point-1', imageData, currentTime);
        
        if (tracked) {
          // eslint-disable-next-line no-console
          console.log('Tracked position:', tracked.x, tracked.y);
          // eslint-disable-next-line no-console
          console.log('Confidence:', tracked.confidence);
        }
      }
    }
  }, [isTracking]);

  return (
    <button onClick={() => setIsTracking(!isTracking)}>
      {isTracking ? 'Stop Tracking' : 'Start Tracking'}
    </button>
  );
}

/**
 * Example 4: Complete Green Screen Workflow
 * 
 * This demonstrates a complete workflow from keying to final composite
 */
export function CompleteGreenScreenWorkflow() {
  const [step, setStep] = useState<'key' | 'refine' | 'composite' | 'done'>('key');

  const workflowSteps = {
    key: {
      title: 'Step 1: Apply Chroma Key',
      description: 'Select the green screen color and adjust similarity',
      action: () => {
        // Apply initial chroma key
        // eslint-disable-next-line no-console
        console.log('Applying chroma key...');
        setStep('refine');
      },
    },
    refine: {
      title: 'Step 2: Refine Edges',
      description: 'Adjust edge thickness, feather, and spill suppression',
      action: () => {
        // Refine the key
        // eslint-disable-next-line no-console
        console.log('Refining edges...');
        setStep('composite');
      },
    },
    composite: {
      title: 'Step 3: Composite with Background',
      description: 'Add background and position your keyed subject',
      action: () => {
        // Composite layers
        // eslint-disable-next-line no-console
        console.log('Compositing layers...');
        setStep('done');
      },
    },
    done: {
      title: 'Workflow Complete!',
      description: 'Your green screen composite is ready',
      action: () => {
        // eslint-disable-next-line no-console
        console.log('Export final video');
      },
    },
  };

  const currentStep = workflowSteps[step];

  return (
    <div>
      <h2>{currentStep.title}</h2>
      <p>{currentStep.description}</p>
      <button onClick={currentStep.action}>
        {step === 'done' ? 'Export' : 'Next Step'}
      </button>
    </div>
  );
}

/**
 * Example 5: Applying Effects to Timeline Clips
 * 
 * This shows how to apply chroma key effects to timeline clips
 */
export function ApplyEffectToTimelineClip() {
  // In your timeline component:
  
  const addChromaKeyToClip = (clipId: string) => {
    const chromaKeyEffect: AppliedEffect = {
      id: `chroma-${clipId}`,
      effectType: 'chroma-key',
      enabled: true,
      parameters: {
        keyColor: '#00ff00',
        similarity: 40,
        smoothness: 8,
        spillSuppression: 15,
        edgeThickness: 0,
        edgeFeather: 1,
        choke: 0,
        matteCleanup: 0,
      },
    };

    // Add to clip's effects array
    // clip.effects = [...(clip.effects || []), chromaKeyEffect];
    void chromaKeyEffect; // Suppress unused variable warning
  };

  return (
    <button onClick={() => addChromaKeyToClip('clip-1')}>
      Add Chroma Key to Clip
    </button>
  );
}

/**
 * Example 6: Using Green Screen Presets
 * 
 * This shows how to quickly apply preset configurations
 */
export function GreenScreenPresetsExample() {
  const applyPreset = (presetType: 'studio' | 'natural' | 'lowLight' | 'uneven') => {
    const presets = {
      studio: {
        keyColor: '#00ff00',
        similarity: 40,
        smoothness: 8,
        spillSuppression: 15,
        edgeThickness: 0,
        edgeFeather: 1,
        choke: 0,
        matteCleanup: 10,
      },
      natural: {
        keyColor: '#00ff00',
        similarity: 50,
        smoothness: 12,
        spillSuppression: 20,
        edgeThickness: 0.5,
        edgeFeather: 2,
        choke: 0.5,
        matteCleanup: 15,
      },
      lowLight: {
        keyColor: '#00ff00',
        similarity: 60,
        smoothness: 15,
        spillSuppression: 25,
        edgeThickness: 1,
        edgeFeather: 3,
        choke: 1,
        matteCleanup: 20,
      },
      uneven: {
        keyColor: '#00ff00',
        similarity: 55,
        smoothness: 18,
        spillSuppression: 30,
        edgeThickness: 0,
        edgeFeather: 2.5,
        choke: 0,
        matteCleanup: 25,
      },
    };

    const preset = presets[presetType];
    // eslint-disable-next-line no-console
    console.log('Applying preset:', presetType, preset);
    
    // Apply preset parameters to your effect
  };

  return (
    <div>
      <button onClick={() => applyPreset('studio')}>Studio Lighting</button>
      <button onClick={() => applyPreset('natural')}>Natural Light</button>
      <button onClick={() => applyPreset('lowLight')}>Low Light</button>
      <button onClick={() => applyPreset('uneven')}>Uneven Lighting</button>
    </div>
  );
}
