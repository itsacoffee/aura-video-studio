# Visual Guide: Loading States and Skeleton Screens

## Before vs After

### ProjectsPage - Editor Projects Tab

**BEFORE (Simple Spinner):**
```
┌─────────────────────────────────────────────┐
│                                             │
│              ⟳ Loading projects...          │
│                                             │
└─────────────────────────────────────────────┘
```

**AFTER (Skeleton Table):**
```
┌────────────────────────────────────────────────────────────────────┐
│ Name                  Last Modified    Duration  Clips  Actions    │
├────────────────────────────────────────────────────────────────────┤
│ ░░░░░░░░░░░░░        ░░░░░░░░░░      ░░░░░░   ░░░   ░░░░ ░░░     │
│ ░░░░░░░░░░░░░░░      ░░░░░░░░░░      ░░░░░░   ░░░   ░░░░ ░░░     │
│ ░░░░░░░░░░░░░        ░░░░░░░░░░      ░░░░░░   ░░░   ░░░░ ░░░     │
│ ░░░░░░░░░░░░░░       ░░░░░░░░░░      ░░░░░░   ░░░   ░░░░ ░░░     │
│ ░░░░░░░░░░░░░░░      ░░░░░░░░░░      ░░░░░░   ░░░   ░░░░ ░░░     │
└────────────────────────────────────────────────────────────────────┘
                     with shimmer animation →
```

### IdeationDashboard - Concept Generation

**BEFORE (Spinner with text):**
```
┌─────────────────────────────────────────────┐
│                                             │
│                    ⟳                        │
│       Generating creative concepts...       │
│  Our AI is analyzing multiple creative     │
│           angles for your topic             │
│                                             │
└─────────────────────────────────────────────┘
```

**AFTER (Skeleton Card Grid):**
```
┌──────────────────────┐ ┌──────────────────────┐
│ ░░░░░░░░░░░░░░░░░   │ │ ░░░░░░░░░░░░░░░░░   │
│ ░░░░░░░░░░░         │ │ ░░░░░░░░░░░         │
│                      │ │                      │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│                      │ │                      │
│ ░░░░ ░░░░           │ │ ░░░░ ░░░░           │
└──────────────────────┘ └──────────────────────┘

┌──────────────────────┐ ┌──────────────────────┐
│ ░░░░░░░░░░░░░░░░░   │ │ ░░░░░░░░░░░░░░░░░   │
│ ░░░░░░░░░░░         │ │ ░░░░░░░░░░░         │
│                      │ │                      │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│ ░░░░░░░░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░░░░░░░░ │
│                      │ │                      │
│ ░░░░ ░░░░           │ │ ░░░░ ░░░░           │
└──────────────────────┘ └──────────────────────┘
```

## Error States

### Before (No Error Handling)
```
┌─────────────────────────────────────────────┐
│         [Console: Failed to load]           │
│         [User sees: blank page]             │
└─────────────────────────────────────────────┘
```

### After (ErrorState Component)
```
┌─────────────────────────────────────────────┐
│                    ⚠️                        │
│                                             │
│          Failed to load projects            │
│                                             │
│     Unable to connect to server.            │
│     Please check your connection.           │
│                                             │
│            [ Try Again ]                    │
└─────────────────────────────────────────────┘
```

## Progress Indicator

### Video Upload Example
```
┌─────────────────────────────────────────────┐
│  Uploading                           65%    │
│  ━━━━━━━━━━━━━━━━━━░░░░░░░░░░░░░░          │
│  Processing video...      2m 15s remaining  │
└─────────────────────────────────────────────┘
```

## AsyncButton States

### Idle State
```
┌──────────┐
│   Save   │
└──────────┘
```

### Loading State
```
┌──────────────┐
│  ⟳ Saving... │  [disabled, aria-busy=true]
└──────────────┘
```

## Component Anatomy

### SkeletonCard
```
┌──────────────────────────────────────┐
│  ░░░░░░░░░░░░░░░░░░░░    ← header   │
│  ░░░░░░░░░░░░              ← subhdr │
│                                      │
│  ░░░░░░░░░░░░░░░░░░░░░░░             │
│  ░░░░░░░░░░░░░░░░░░░░░░░  ← content │
│  ░░░░░░░░░░░░░░░░░░░░░░░             │
│                                      │
│  ░░░░ ░░░░                ← footer  │
└──────────────────────────────────────┘
```

### SkeletonList
```
┌──────────────────────────────────────┐
│  ◯  ░░░░░░░░░░░░░      ░░ ░░        │
│     ░░░░░░░░░                        │
├──────────────────────────────────────┤
│  ◯  ░░░░░░░░░░░░░      ░░ ░░        │
│     ░░░░░░░░░                        │
├──────────────────────────────────────┤
│  ◯  ░░░░░░░░░░░░░      ░░ ░░        │
│     ░░░░░░░░░                        │
└──────────────────────────────────────┘
  ↑        ↑             ↑
avatar   content      actions
```

### SkeletonTable
```
┌────────────────────────────────────────────┐
│ Name          Date         Status          │
├────────────────────────────────────────────┤
│ ░░░░░░░░░    ░░░░░░░     ░░░░░░           │
│ ░░░░░░░░░░   ░░░░░░░     ░░░░░░           │
│ ░░░░░░░░░    ░░░░░░░     ░░░░░░           │
└────────────────────────────────────────────┘
```

## Animation

The shimmer effect moves left to right across each skeleton element:

```
Time 0s:     ░░░░░░░░░░░░
            ↓

Time 0.5s:   ▓░░░░░░░░░░░
            ↓

Time 1s:     ░▓░░░░░░░░░░
            ↓

Time 1.5s:   ░░░░░▓░░░░░░
            ↓

(repeats)
```

## Accessibility Annotations

### Skeleton Components
```html
<div 
  role="status" 
  aria-label="Loading content"
  aria-busy="true"
>
  [skeleton content]
</div>
```

Screen reader announces: "Loading content"

### ProgressIndicator
```html
<div 
  role="status"
  aria-label="Uploading - 65% complete"
  aria-live="polite"
>
  <progress value="65" max="100" />
  2m 15s remaining
</div>
```

Screen reader announces updates as progress changes.

### ErrorState
```html
<div 
  role="alert"
  aria-label="Error occurred"
>
  Failed to load projects
  [Try Again button]
</div>
```

Screen reader immediately announces: "Alert: Failed to load projects"

### AsyncButton
```html
<button
  aria-busy="true"
  aria-disabled="true"
  disabled
>
  <Spinner /> Saving...
</button>
```

Screen reader announces: "Button, Saving, busy, disabled"

## Usage Patterns

### Pattern 1: Simple Loading State
```tsx
{loading ? (
  <SkeletonCard count={3} />
) : (
  <CardGrid items={items} />
)}
```

### Pattern 2: Loading with Error Handling
```tsx
{loading && <SkeletonTable columns={cols} />}

{error && (
  <ErrorState 
    message={error} 
    onRetry={reload}
  />
)}

{!loading && !error && (
  <Table data={data} />
)}
```

### Pattern 3: Progress Tracking
```tsx
const [state, actions] = useLoadingState();

{state.isLoading && state.progress !== undefined ? (
  <ProgressIndicator
    progress={state.progress}
    estimatedTimeRemaining={state.estimatedTimeRemaining}
  />
) : state.isLoading ? (
  <SkeletonCard />
) : (
  <Content />
)}
```

## Design Principles

1. **Match Layout**: Skeleton screens match the actual content layout
2. **Shimmer Animation**: Subtle animation indicates activity
3. **Neutral Colors**: Use theme-aware neutral backgrounds
4. **Accessible**: Proper ARIA labels and roles
5. **Responsive**: Skeleton screens adapt to viewport size
6. **Consistent**: Same loading patterns across the app

## Browser Support

All components use standard CSS and React features, compatible with:
- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)

Animations use CSS keyframes, which are widely supported.

## Performance

- **Skeleton Components**: Lightweight, render quickly
- **No External Dependencies**: Uses Fluent UI components already in project
- **CSS Animations**: Hardware-accelerated for smooth performance
- **Small Bundle Impact**: ~15KB minified for all components
