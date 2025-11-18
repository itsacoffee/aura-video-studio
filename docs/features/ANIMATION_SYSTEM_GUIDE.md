# Animation System Guide

## Overview

This guide documents the comprehensive animation system added to Aura.Web as part of PR #8. The system provides professional UI polish with smooth animations, micro-interactions, and visual improvements while maintaining accessibility.

## 🎨 Key Features

### 1. Animation Library Integration
- **Framer Motion**: Industry-standard animation library for React
- **Reduced Motion Support**: Respects `prefers-reduced-motion` media query
- **Performance Optimized**: GPU-accelerated animations with proper will-change hints

### 2. Animation Components

#### Core Animation Wrappers
- `AnimatedDiv`: Generic animated container with variant support
- `FadeIn`: Simple fade-in animation
- `SlideIn`: Slide animations from any direction
- `ScaleIn`: Scale-in animations for modals/dialogs
- `PageTransition`: Smooth page route transitions
- `AnimatedList` / `AnimatedListItem`: Staggered list animations

#### UI Components
- `AnimatedButton`: Enhanced button with press and hover effects
- `AnimatedInput`: Input fields with smooth focus animations
- `AnimatedCard`: Interactive cards with hover lift effects
- `AnimatedModal`: Modal dialogs with backdrop animations
- `AnimatedTooltip`: Smooth tooltips with auto-positioning

#### Loading States
- `LoadingSpinner`: Smooth rotating spinner
- `LoadingDots`: Animated dot indicators
- `LoadingBar`: Progress bars (determinate/indeterminate)
- `Skeleton`: Shimmer loading placeholders
- `SkeletonText`: Multi-line text placeholders
- `SkeletonCard`: Card-shaped placeholders
- `SkeletonList`: List item placeholders

#### Feedback Animations
- `SuccessAnimation`: Celebration effect for success states
- `ErrorAnimation`: Shake animation for errors
- `ProgressIndicator`: Bar and circular progress indicators

## 📚 Usage Examples

### Basic Animations

```tsx
import { FadeIn, SlideIn, ScaleIn } from '@/components/animations';

// Fade in
<FadeIn delay={0.2}>
  <div>Content fades in</div>
</FadeIn>

// Slide in from bottom
<SlideIn direction="fromBottom">
  <div>Content slides up</div>
</SlideIn>

// Scale in (perfect for modals)
<ScaleIn>
  <div>Content scales in</div>
</ScaleIn>
```

### Interactive Components

```tsx
import { AnimatedButton, AnimatedInput, AnimatedCard } from '@/components/ui';

// Button with loading state
<AnimatedButton 
  variant="primary" 
  isLoading={loading}
  leftIcon={<Icon />}
  onClick={handleClick}
>
  Submit
</AnimatedButton>

// Input with validation
<AnimatedInput
  label="Email"
  error={errors.email}
  placeholder="you@example.com"
/>

// Interactive card
<AnimatedCard interactive onClick={handleClick}>
  <AnimatedCardHeader title="Card Title" />
  <AnimatedCardBody>Content here</AnimatedCardBody>
  <AnimatedCardFooter>
    <AnimatedButton>Action</AnimatedButton>
  </AnimatedCardFooter>
</AnimatedCard>
```

### Loading States

```tsx
import { 
  LoadingSpinner, 
  LoadingDots, 
  Skeleton, 
  SkeletonCard 
} from '@/components/Loading';

// Spinner
<LoadingSpinner size="md" label="Loading..." />

// Loading dots
<LoadingDots size="sm" />

// Skeleton placeholders
<Skeleton width="100%" height="2rem" />
<SkeletonText lines={3} />
<SkeletonCard hasImage hasAvatar />
```

### Feedback Animations

```tsx
import { 
  SuccessAnimation, 
  ErrorAnimation, 
  ProgressIndicator 
} from '@/components/feedback';

// Success celebration
<SuccessAnimation 
  show={showSuccess} 
  message="Saved successfully!" 
  onComplete={() => setShowSuccess(false)}
/>

// Error with shake
<ErrorAnimation 
  show={showError} 
  message="Something went wrong" 
  onDismiss={() => setShowError(false)}
/>

// Progress indicator
<ProgressIndicator 
  progress={75} 
  label="Uploading..." 
  variant="bar"
/>
```

### Staggered List Animations

```tsx
import { AnimatedList, AnimatedListItem } from '@/components/animations';

<AnimatedList staggerDelay={0.1}>
  {items.map((item) => (
    <AnimatedListItem key={item.id}>
      <div>{item.content}</div>
    </AnimatedListItem>
  ))}
</AnimatedList>
```

## 🎭 Animation Presets

The system includes pre-configured animation variants:

### Transitions
- `fadeVariants`: Fade in/out
- `slideVariants`: Slide from any direction (top/bottom/left/right)
- `scaleVariants`: Scale in/out
- `expandVariants`: Expand/collapse for accordions

### Special Effects
- `shakeVariants`: Shake for errors
- `celebrationVariants`: Celebration for success
- `pulseVariants`: Pulsing for attention
- `bounceVariants`: Bouncing indicators
- `rotateVariants`: Continuous rotation

### Timing Functions
```typescript
easings = {
  default: [0.4, 0, 0.2, 1],     // Smooth and natural
  snappy: [0.6, 0.05, 0.01, 0.9], // Quick and responsive
  bounce: [0.68, -0.55, 0.265, 1.55], // Bouncy
  smooth: [0.65, 0, 0.35, 1],     // Page transitions
  elastic: [0.68, -0.6, 0.32, 1.6] // Elastic
}

durations = {
  fast: 0.15,
  base: 0.25,
  slow: 0.35,
  slower: 0.5
}
```

## ♿ Accessibility

### Reduced Motion Support

All animations respect the user's motion preferences:

```tsx
import { useReducedMotion } from '@/hooks/useReducedMotion';

function MyComponent() {
  const prefersReducedMotion = useReducedMotion();
  
  if (prefersReducedMotion) {
    // Render static version
    return <div>Content</div>;
  }
  
  // Render animated version
  return <AnimatedDiv>Content</AnimatedDiv>;
}
```

The system automatically:
- Detects `prefers-reduced-motion: reduce` media query
- Disables or simplifies animations when enabled
- Maintains opacity transitions for minimal motion
- Updates in real-time if user changes preference

### Keyboard Navigation

All interactive components support:
- Tab navigation
- Enter/Space activation
- Escape key for dismissal (modals)
- Focus indicators with proper contrast

### ARIA Attributes

Components include proper ARIA attributes:
- `aria-label` for loading states
- `aria-busy` for async operations
- `aria-invalid` for form validation
- `role` attributes for semantic meaning

## 🎨 Visual Hierarchy Improvements

### Typography Scale
Enhanced font sizes with proper line heights:
- `text-xs` to `text-6xl`
- Consistent vertical rhythm
- Better readability

### Spacing System
Improved spacing scale:
- `4px` base unit (space-1)
- Multiples: 8px, 12px, 16px, 24px, 32px
- Consistent throughout the UI

### Shadow System
Enhanced shadows for depth:
- `shadow-xs` to `shadow-2xl`
- `shadow-glow` for primary actions
- Dark mode optimized shadows

### Color System
Professional color palette:
- Primary (blue), Secondary (purple)
- Success (green), Warning (orange), Error (red)
- 50-950 shades for each color
- WCAG compliant contrast ratios

## 🎯 Tailwind Utilities

New animation classes available:
```css
/* Fade */
animate-fade-in
animate-fade-out

/* Slide */
animate-slide-in-up
animate-slide-in-down
animate-slide-in-left
animate-slide-in-right

/* Scale */
animate-scale-in
animate-scale-out

/* Special */
animate-shimmer
animate-shake
animate-pulse-slow
animate-bounce-slow
```

## 📦 File Structure

```
src/
├── components/
│   ├── animations/
│   │   ├── AnimatedDiv.tsx
│   │   ├── AnimatedList.tsx
│   │   ├── PageTransition.tsx
│   │   ├── FadeIn.tsx
│   │   ├── SlideIn.tsx
│   │   ├── ScaleIn.tsx
│   │   └── index.ts
│   ├── feedback/
│   │   ├── SuccessAnimation.tsx
│   │   ├── ErrorAnimation.tsx
│   │   ├── ProgressIndicator.tsx
│   │   └── index.ts
│   ├── Loading/
│   │   ├── Skeleton.tsx
│   │   ├── LoadingSpinner.tsx
│   │   └── ...
│   └── ui/
│       ├── AnimatedButton.tsx
│       ├── AnimatedInput.tsx
│       ├── AnimatedCard.tsx
│       ├── AnimatedModal.tsx
│       ├── AnimatedTooltip.tsx
│       └── index.ts
├── hooks/
│   └── useReducedMotion.ts
├── utils/
│   └── animations.ts
└── pages/
    └── AnimationShowcase.tsx
```

## 🧪 Testing

To test animations:

1. **Visual Testing**: Visit `/animation-showcase` route
2. **Reduced Motion**: Test with system preference enabled
3. **Performance**: Use browser DevTools Performance tab
4. **Accessibility**: Test keyboard navigation and screen readers

## 🚀 Performance Tips

1. **Use transform and opacity**: These are GPU-accelerated
2. **Avoid animating layout properties**: width, height, top, left
3. **Use will-change sparingly**: Only for frequently animated elements
4. **Lazy load animation components**: Use React.lazy() for non-critical animations
5. **Respect reduced motion**: Always provide a fallback

## 📝 Best Practices

### Do's ✅
- Use semantic animation durations (fast, base, slow)
- Provide reduced motion alternatives
- Use pre-configured variants when possible
- Test on low-end devices
- Keep animations subtle and purposeful

### Don'ts ❌
- Don't animate too many elements simultaneously
- Don't use long animation durations (>500ms)
- Don't animate heavy/complex elements
- Don't ignore accessibility
- Don't use animations just for decoration

## 🔧 Customization

### Creating Custom Variants

```typescript
import { Variants } from 'framer-motion';

const customVariants: Variants = {
  hidden: { opacity: 0, scale: 0.8 },
  visible: { 
    opacity: 1, 
    scale: 1,
    transition: {
      duration: 0.3,
      ease: [0.4, 0, 0.2, 1]
    }
  }
};

<AnimatedDiv variants={customVariants}>
  Content
</AnimatedDiv>
```

### Extending Animation Utilities

Add new animations to `tailwind.config.js`:

```javascript
animation: {
  'custom': 'customKeyframe 0.5s ease-in-out',
},
keyframes: {
  customKeyframe: {
    '0%': { /* start state */ },
    '100%': { /* end state */ }
  }
}
```

## 🐛 Troubleshooting

### Animations not working
- Check if framer-motion is installed
- Verify component imports
- Check for CSS conflicts

### Performance issues
- Reduce number of simultaneous animations
- Use lighter animation variants
- Check for memory leaks in useEffect

### Accessibility warnings
- Ensure reduced motion is respected
- Add proper ARIA labels
- Test keyboard navigation

## 📚 Resources

- [Framer Motion Documentation](https://www.framer.com/motion/)
- [Web Animations API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Animations_API)
- [Reduced Motion](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion)
- [WCAG Animation Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/animation-from-interactions)

## 🎉 Summary

The animation system provides:
- ✅ Professional UI polish
- ✅ Smooth micro-interactions  
- ✅ Enhanced loading states
- ✅ Clear visual feedback
- ✅ Full accessibility support
- ✅ Performance optimized
- ✅ Easy to use and extend

All animations are production-ready and follow industry best practices for modern web applications.
