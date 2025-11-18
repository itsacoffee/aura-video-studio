# PR #4: Developer Quick Reference

## 🚀 Quick Start

### New Components Location
```
/workspace/Aura.Web/src/components/VideoWizard/
├── RichProgressDisplay.tsx          (New)
├── CelebrationEffect.tsx            (New)
├── ScriptSyntaxHighlighter.tsx      (New)
├── PromptQualityAnalyzer.tsx        (New)
├── AnimatedStepTransition.tsx       (New)
└── ...existing files...
```

### Modified Components
```
/workspace/Aura.Web/src/components/
├── WizardProgress.tsx               (Enhanced animations)
└── VideoWizard/
    ├── VideoCreationWizard.tsx      (Added celebration, animations)
    └── steps/
        ├── BriefInput.tsx           (Added quality analyzer)
        └── StyleSelection.tsx       (Enhanced animations)
```

---

## 📦 Component Import Guide

### PromptQualityAnalyzer
```typescript
import { PromptQualityAnalyzer } from '../PromptQualityAnalyzer';

<PromptQualityAnalyzer
  prompt={data.topic}
  targetAudience={data.targetAudience}
  keyMessage={data.keyMessage}
  videoType={data.videoType}
/>
```

### RichProgressDisplay
```typescript
import { RichProgressDisplay, type ProgressStage } from './RichProgressDisplay';

const stages: ProgressStage[] = [
  { id: 'init', name: 'Initialize', description: 'Setting up...', status: 'completed' },
  { id: 'script', name: 'Generate Script', description: 'Creating narrative...', status: 'active', progress: 65 },
  { id: 'visuals', name: 'Generate Visuals', description: 'Creating images...', status: 'pending', estimatedTime: 120 },
];

<RichProgressDisplay
  stages={stages}
  currentStage="script"
  overallProgress={45}
  timeElapsed={150}
  timeRemaining={180}
  onPause={() => console.log('Paused')}
  onCancel={() => console.log('Cancelled')}
  canPause={true}
  canCancel={true}
/>
```

### ScriptSyntaxHighlighter
```typescript
import { ScriptSyntaxHighlighter } from './ScriptSyntaxHighlighter';

// Option 1: With structured scenes
<ScriptSyntaxHighlighter
  script=""
  scenes={[
    { number: 1, narration: 'Text...', visualPrompt: 'Scene description', durationSeconds: 5.5, transition: 'Fade' },
    { number: 2, narration: 'More text...', visualPrompt: 'Another scene', durationSeconds: 4.0, transition: 'Cut' },
  ]}
/>

// Option 2: Plain text
<ScriptSyntaxHighlighter script="Full script text here..." />
```

### CelebrationEffect
```typescript
import { CelebrationEffect } from './CelebrationEffect';

const [showCelebration, setShowCelebration] = useState(false);

<CelebrationEffect
  show={showCelebration}
  onComplete={() => setShowCelebration(false)}
  type="both"  // 'confetti' | 'pulse' | 'both'
  duration={3000}
/>

// Trigger it:
setShowCelebration(true);
```

### AnimatedStepTransition
```typescript
import { AnimatedStepTransition } from './AnimatedStepTransition';

<AnimatedStepTransition stepKey={currentStep}>
  {renderStepContent()}
</AnimatedStepTransition>
```

---

## 🎨 Animation Utilities

### Standard Animations

```typescript
// Add to makeStyles
const useStyles = makeStyles({
  fadeIn: {
    animation: 'fadeIn 0.5s ease',
  },
  fadeInUp: {
    animation: 'fadeInUp 0.5s ease',
  },
  slideIn: {
    animation: 'slideInRight 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
  },
  '@keyframes fadeIn': {
    '0%': { opacity: 0 },
    '100%': { opacity: 1 },
  },
  '@keyframes fadeInUp': {
    '0%': {
      opacity: 0,
      transform: 'translateY(20px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateY(0)',
    },
  },
  '@keyframes slideInRight': {
    '0%': {
      opacity: 0,
      transform: 'translateX(30px)',
    },
    '100%': {
      opacity: 1,
      transform: 'translateX(0)',
    },
  },
});
```

### Hover Effects

```typescript
const useStyles = makeStyles({
  hoverCard: {
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  hoverButton: {
    transition: 'all 0.2s ease',
    ':hover': {
      transform: 'scale(1.05)',
    },
  },
});
```

---

## 🎯 Validation Patterns

### Step Validation
```typescript
const validateStep = useCallback((data: StepData): StepValidation => {
  const errors: string[] = [];

  if (!data.field1) {
    errors.push('Field 1 is required');
  }
  if (data.field2.length < 10) {
    errors.push('Field 2 must be at least 10 characters');
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}, []);

// Use it:
useEffect(() => {
  const validation = validateStep(data);
  onValidationChange(validation);
}, [data, validateStep, onValidationChange]);
```

### Prompt Quality Scoring
```typescript
// Scoring logic example from PromptQualityAnalyzer:
const wordCount = prompt.split(/\s+/).filter(w => w.length > 0).length;
const lengthScore = Math.min((wordCount / 30) * 25, 25);

const specificKeywords = ['explain', 'demonstrate', 'show', 'compare'];
const hasSpecificKeywords = specificKeywords.some(k => 
  prompt.toLowerCase().includes(k)
);
const specificityScore = hasSpecificKeywords ? 25 : 10;

const totalScore = lengthScore + specificityScore + clarityScore + actionabilityScore;
```

---

## 🎨 Fluent UI Token Reference

### Common Tokens

```typescript
// Spacing
tokens.spacingVerticalXXS    // 2px
tokens.spacingVerticalXS     // 4px
tokens.spacingVerticalS      // 8px
tokens.spacingVerticalM      // 12px
tokens.spacingVerticalL      // 16px
tokens.spacingVerticalXL     // 24px
tokens.spacingVerticalXXL    // 32px
tokens.spacingVerticalXXXL   // 40px

// Colors
tokens.colorBrandBackground           // Primary brand
tokens.colorBrandForeground1          // Brand text
tokens.colorNeutralBackground1        // Background
tokens.colorNeutralForeground1        // Text
tokens.colorPaletteGreenForeground1   // Success
tokens.colorPaletteRedForeground1     // Error
tokens.colorPaletteYellowForeground1  // Warning
tokens.colorPaletteBlueForeground1    // Info

// Shadows
tokens.shadow4     // Subtle
tokens.shadow8     // Medium
tokens.shadow16    // Strong
tokens.shadow28    // Very strong
tokens.shadow64    // Maximum

// Border Radius
tokens.borderRadiusSmall   // 2px
tokens.borderRadiusMedium  // 4px
tokens.borderRadiusLarge   // 8px
tokens.borderRadiusXLarge  // 12px

// Font
tokens.fontSizeBase200     // 12px
tokens.fontSizeBase300     // 14px
tokens.fontSizeBase400     // 16px
tokens.fontWeightRegular   // 400
tokens.fontWeightSemibold  // 600
tokens.fontWeightBold      // 700
```

---

## 🔧 Common Patterns

### Debounced Auto-Save

```typescript
const autoSaveTimeouts = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

const handleChange = useCallback((id: string, value: string) => {
  // Update local state immediately
  setData(prev => ({ ...prev, [id]: value }));

  // Clear existing timeout
  if (autoSaveTimeouts.current[id]) {
    clearTimeout(autoSaveTimeouts.current[id]);
  }

  // Set new timeout for save
  autoSaveTimeouts.current[id] = setTimeout(async () => {
    await saveToServer(id, value);
  }, 2000);
}, []);

// Cleanup on unmount
useEffect(() => {
  return () => {
    Object.values(autoSaveTimeouts.current).forEach(clearTimeout);
  };
}, []);
```

### Loading State Pattern

```typescript
const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');

const handleAction = async () => {
  setStatus('loading');
  try {
    await doSomething();
    setStatus('success');
  } catch (error) {
    setStatus('error');
  }
};

// Render based on status:
{status === 'loading' && <Spinner />}
{status === 'success' && <CheckmarkIcon />}
{status === 'error' && <ErrorIcon />}
```

### Progress Tracking

```typescript
const [progress, setProgress] = useState(0);
const [currentStage, setCurrentStage] = useState('');

const processStages = async () => {
  const stages = [
    { name: 'Stage 1', action: stage1Action },
    { name: 'Stage 2', action: stage2Action },
    { name: 'Stage 3', action: stage3Action },
  ];

  for (let i = 0; i < stages.length; i++) {
    setCurrentStage(stages[i].name);
    await stages[i].action();
    setProgress(((i + 1) / stages.length) * 100);
  }
};
```

---

## 🎵 Audio Utilities

### Success Sound (from CelebrationEffect)

```typescript
const playSuccessSound = () => {
  try {
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();

    oscillator.connect(gainNode);
    gainNode.connect(audioContext.destination);

    // C5-E5-G5 chord progression
    oscillator.frequency.setValueAtTime(523.25, audioContext.currentTime);
    oscillator.frequency.setValueAtTime(659.25, audioContext.currentTime + 0.1);
    oscillator.frequency.setValueAtTime(783.99, audioContext.currentTime + 0.2);

    gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

    oscillator.start(audioContext.currentTime);
    oscillator.stop(audioContext.currentTime + 0.5);
  } catch (error) {
    console.debug('Audio not available');
  }
};
```

---

## 🐛 Debugging Tips

### Animation Issues
```typescript
// Check if animations are playing
element.addEventListener('animationstart', () => console.log('Animation started'));
element.addEventListener('animationend', () => console.log('Animation ended'));

// Force repaint if needed
element.offsetHeight; // Triggers reflow
```

### Performance Profiling
```typescript
// Mark rendering time
performance.mark('render-start');
// ... render code ...
performance.mark('render-end');
performance.measure('render', 'render-start', 'render-end');

// Get measurements
const measures = performance.getEntriesByType('measure');
console.log(measures);
```

### React Dev Tools
```typescript
// Add displayName for better debugging
MyComponent.displayName = 'MyComponent';

// Use React.memo with custom comparison
const MyComponent = React.memo(({ data }) => {
  // component code
}, (prevProps, nextProps) => {
  // Return true if props are equal (skip re-render)
  return prevProps.data === nextProps.data;
});
```

---

## 📚 Resources

### Documentation
- [Fluent UI React](https://react.fluentui.dev/)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [React Hooks Reference](https://react.dev/reference/react)
- [Web Animations API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Animations_API)

### Tools
- [React DevTools](https://react.dev/learn/react-developer-tools)
- [Chrome DevTools](https://developer.chrome.com/docs/devtools/)
- [Lighthouse](https://developers.google.com/web/tools/lighthouse)
- [axe DevTools](https://www.deque.com/axe/devtools/)

---

## 🔥 Hot Tips

1. **Always use tokens** - Never hardcode colors or spacing
2. **Prefer transform/opacity** - For smooth, GPU-accelerated animations
3. **Use cubic-bezier** - For professional, natural motion
4. **Add loading states** - For every async operation
5. **Implement error boundaries** - Catch and handle errors gracefully
6. **Test keyboard navigation** - Before considering feature complete
7. **Check color contrast** - Use browser DevTools accessibility panel
8. **Profile performance** - Before and after changes
9. **Write meaningful commit messages** - Future you will thank you
10. **Document complex logic** - Comments save debugging time

---

## 🎯 Common Gotchas

### 1. Animation Stuttering
```typescript
// ❌ Bad - animates layout properties
transform: 'translateX(10px)'; 
left: '10px';

// ✅ Good - GPU accelerated
transform: 'translateX(10px)';
```

### 2. Memory Leaks
```typescript
// ❌ Bad - no cleanup
useEffect(() => {
  const interval = setInterval(() => {...}, 1000);
}, []);

// ✅ Good - cleanup function
useEffect(() => {
  const interval = setInterval(() => {...}, 1000);
  return () => clearInterval(interval);
}, []);
```

### 3. Infinite Re-renders
```typescript
// ❌ Bad - creates new object every render
<Component data={{ value: 'test' }} />

// ✅ Good - memoize or use state
const data = useMemo(() => ({ value: 'test' }), []);
<Component data={data} />
```

### 4. Key Prop Warnings
```typescript
// ❌ Bad - index as key (if list can reorder)
{items.map((item, index) => <Item key={index} {...item} />)}

// ✅ Good - stable unique identifier
{items.map(item => <Item key={item.id} {...item} />)}
```

### 5. Missing Dependencies
```typescript
// ❌ Bad - missing dependency
useEffect(() => {
  console.log(value);
}, []);

// ✅ Good - all dependencies included
useEffect(() => {
  console.log(value);
}, [value]);
```

---

## 🚦 Code Review Checklist

Before submitting PR:
- [ ] No console.log statements
- [ ] TypeScript errors resolved
- [ ] ESLint warnings fixed
- [ ] All imports used
- [ ] No unused variables
- [ ] PropTypes/interfaces documented
- [ ] Loading states implemented
- [ ] Error states handled
- [ ] Accessibility tested
- [ ] Responsive design verified
- [ ] Animation performance checked
- [ ] Tests written (if applicable)
- [ ] Documentation updated
- [ ] Commit messages meaningful

---

**End of Developer Quick Reference**
