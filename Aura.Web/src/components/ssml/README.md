# SSML Components

React components for SSML preview, editing, and timing alignment with TTS providers.

## Components

### SSMLPreview

Main component for SSML workflow with duration fitting and per-scene controls.

**Usage**:
```typescript
import { SSMLPreview } from '@/components/ssml';
import type { LineDto } from '@/types/api-v1';

const scriptLines: LineDto[] = [
  {
    sceneIndex: 0,
    text: "Welcome to our video about AI.",
    startSeconds: 0,
    durationSeconds: 3.5
  },
  {
    sceneIndex: 1,
    text: "Today we'll explore machine learning.",
    startSeconds: 3.5,
    durationSeconds: 4.2
  }
];

<SSMLPreview
  scriptLines={scriptLines}
  voiceName="Rachel"
  initialProvider="ElevenLabs"
  onSSMLGenerated={(ssmlMarkups) => {
    console.log('Generated SSML:', ssmlMarkups);
    // Pass to TTS synthesis service
  }}
/>
```

**Props**:
- `scriptLines`: Array of script lines with timing info
- `voiceName`: Voice name for TTS synthesis
- `initialProvider`: Initial TTS provider selection (default: "ElevenLabs")
- `onSSMLGenerated`: Callback when SSML is generated

### SSMLProviderSelector

Dropdown for selecting TTS provider with support badges.

**Usage**:
```typescript
import { SSMLProviderSelector } from '@/components/ssml';

<SSMLProviderSelector />
```

Uses `useSSMLEditorStore` for state management.

### SSMLTimingDisplay

Display duration fitting statistics with visual indicators.

**Usage**:
```typescript
import { SSMLTimingDisplay } from '@/components/ssml';
import type { DurationFittingStats } from '@/services/ssmlService';

const stats: DurationFittingStats = {
  segmentsAdjusted: 5,
  averageFitIterations: 2.3,
  maxFitIterations: 4,
  withinTolerancePercent: 96.5,
  averageDeviation: 0.8,
  maxDeviation: 3.2,
  targetDurationSeconds: 30.0,
  actualDurationSeconds: 29.8
};

<SSMLTimingDisplay stats={stats} />
```

### SSMLSceneEditor

Per-scene SSML editor with validation and controls.

**Usage**:
```typescript
import { SSMLSceneEditor } from '@/components/ssml';
import type { SceneSSMLState } from '@/state/ssmlEditor';

const scene: SceneSSMLState = {
  sceneIndex: 0,
  originalText: "Welcome to our video",
  ssmlMarkup: "<speak>Welcome to our video</speak>",
  userModified: false,
  estimatedDurationMs: 3500,
  targetDurationMs: 3500,
  deviationPercent: 0,
  adjustments: {
    rate: 1.0,
    pitch: 0.0,
    volume: 1.0,
    pauses: {},
    emphasis: [],
    iterations: 0
  }
};

<SSMLSceneEditor scene={scene} />
```

## State Management

Use `useSSMLEditorStore` to access and update SSML state:

```typescript
import { useSSMLEditorStore } from '@/state/ssmlEditor';

function MyComponent() {
  const {
    scenes,
    selectedSceneIndex,
    selectedProvider,
    updateScene,
    selectScene
  } = useSSMLEditorStore();

  // Update scene SSML
  const handleUpdate = () => {
    updateScene(0, {
      ssmlMarkup: '<speak><prosody rate="1.2">Updated text</prosody></speak>',
    });
  };

  return (
    <div>
      <p>Selected Provider: {selectedProvider}</p>
      <p>Total Scenes: {scenes.size}</p>
      <button onClick={handleUpdate}>Update Scene 0</button>
    </div>
  );
}
```

## API Service

Use `ssmlService` functions for backend API calls:

```typescript
import {
  planSSML,
  validateSSML,
  repairSSML,
  getSSMLConstraints
} from '@/services/ssmlService';

// Plan SSML with duration fitting
const result = await planSSML({
  scriptLines: lines,
  targetProvider: 'ElevenLabs',
  voiceSpec: {
    voiceName: 'Rachel',
    rate: 1.0,
    pitch: 0.0,
    volume: 1.0
  },
  targetDurations: { 0: 3.5, 1: 4.2 },
  durationTolerance: 0.02,
  maxFittingIterations: 10
});

// Validate SSML
const validation = await validateSSML({
  ssml: '<speak>Hello world</speak>',
  targetProvider: 'ElevenLabs'
});

if (!validation.isValid) {
  console.error('Validation errors:', validation.errors);
  
  // Auto-repair
  const repaired = await repairSSML({
    ssml: '<speak>Hello world</speak>',
    targetProvider: 'ElevenLabs'
  });
  
  if (repaired.wasRepaired) {
    console.log('Repaired SSML:', repaired.repairedSsml);
  }
}

// Get provider constraints
const constraints = await getSSMLConstraints('ElevenLabs');
console.log('Rate range:', constraints.minRate, '-', constraints.maxRate);
```

## Integration Example

Complete integration with voice configuration:

```typescript
import { useState } from 'react';
import { SSMLPreview } from '@/components/ssml';
import type { LineDto } from '@/types/api-v1';

export function VoiceConfigurationStep() {
  const [scriptLines, setScriptLines] = useState<LineDto[]>([]);
  const [voiceName, setVoiceName] = useState('Rachel');
  const [ssmlMarkups, setSSMLMarkups] = useState<string[]>([]);

  const handleSSMLGenerated = (markups: string[]) => {
    setSSMLMarkups(markups);
    console.log('SSML ready for synthesis');
  };

  const handleSynthesizeVoice = async () => {
    // Use ssmlMarkups for TTS synthesis
    for (let i = 0; i < ssmlMarkups.length; i++) {
      const audioData = await synthesizeTTS({
        text: ssmlMarkups[i],
        voiceName: voiceName,
        format: 'mp3'
      });
      // Save audio for scene i
    }
  };

  return (
    <div>
      <h2>Voice Configuration</h2>
      
      {/* Voice selection UI */}
      <select value={voiceName} onChange={(e) => setVoiceName(e.target.value)}>
        <option value="Rachel">Rachel</option>
        <option value="Josh">Josh</option>
      </select>

      {/* SSML Preview */}
      <SSMLPreview
        scriptLines={scriptLines}
        voiceName={voiceName}
        initialProvider="ElevenLabs"
        onSSMLGenerated={handleSSMLGenerated}
      />

      {/* Synthesize button */}
      <button
        onClick={handleSynthesizeVoice}
        disabled={ssmlMarkups.length === 0}
      >
        Synthesize Voice
      </button>
    </div>
  );
}
```

## Supported Providers

- **ElevenLabs**: Premium, realistic voices with full SSML support
- **PlayHT**: Premium, voice cloning with SSML support
- **Windows SAPI**: Free, Windows native with timing markers
- **Piper**: Offline, neural TTS with basic SSML
- **Mimic3**: Offline, open-source with limited SSML

## Testing

Run tests:
```bash
npm test -- src/state/__tests__/ssmlEditor.test.ts
```

19 tests covering:
- Initial state
- Scene management
- Scene updates
- Provider selection
- Validation management
- UI state toggles
- State reset

## Documentation

See [SCRIPT_REFINEMENT_GUIDE.md](../../../../SCRIPT_REFINEMENT_GUIDE.md) for complete SSML workflow documentation.
