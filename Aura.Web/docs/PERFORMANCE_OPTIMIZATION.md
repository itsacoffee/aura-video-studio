# React Performance Optimization Guide

This document describes the performance optimizations implemented in PR-UI-002.

## Optimizations Implemented

### 1. Re-render Prevention

#### React.memo for Component Memoization

Components wrapped with `React.memo` to prevent unnecessary re-renders:

- `EngineCard` (1243 lines) - Large component with complex state
- `ProjectCard` - List item component rendered in grids
- `ProjectListItem` - Table row component

Example:

```tsx
const EngineCardComponent = ({ engine }: EngineCardProps) => {
  // Component logic
};

export const EngineCard = memo(EngineCardComponent);
```

#### useCallback for Event Handler Stability

Event handlers in `ProjectManagement` converted to useCallback:

- `handleSelectProject` - Project selection toggle
- `handleSelectAll` - Select all projects
- `handleBulkDelete` - Bulk delete operation
- `handleDeleteProject` - Single project deletion
- `handleDuplicateProject` - Project duplication
- `handleFilterChange` - Filter updates
- `handlePageChange` - Pagination

Benefits:

- Prevents child components from re-rendering when parent updates
- Maintains referential equality for callback props
- Reduces unnecessary reconciliation work

### 2. Performance Monitoring (Development Only)

#### useWhyDidYouUpdate Hook

Tracks prop changes causing component re-renders:

```tsx
function MyComponent({ prop1, prop2 }) {
  useWhyDidYouUpdate('MyComponent', { prop1, prop2 });
  // Logs changed props to console
}
```

#### useRenderTime Hook

Measures component render performance:

```tsx
function MyComponent() {
  useRenderTime('MyComponent', 16); // Warns if > 16ms
  // Component logic
}
```

#### useMountEffect Hook

Detects unnecessary component remounts:

```tsx
function MyComponent() {
  useMountEffect('MyComponent');
  // Logs mount/unmount events
}
```

### 3. Loading States

#### SuspenseFallback Components

Standardized loading indicators for Suspense boundaries:

- `SuspenseFallback` - Default loading state (200px min height)
- `SuspenseFallbackMinimal` - Compact loading indicator
- `SuspenseFallbackFullPage` - Full-page loading (100vh)

Features:

- Consistent spinner sizes
- ARIA labels for accessibility
- Customizable messages

Example:

```tsx
<Suspense fallback={<SuspenseFallback message="Loading dashboard..." />}>
  <LazyComponent />
</Suspense>
```

### 4. Error Recovery

#### ErrorBoundaryWithRecovery Component

Enhanced error boundary with recovery options:

Features:

- Automatic retry mechanism (up to 3 attempts)
- "Go Home" navigation fallback
- Error count tracking
- Optional error details for debugging
- Reset on prop changes via `resetKeys`

Example:

```tsx
<ErrorBoundaryWithRecovery
  showErrorDetails={isDevelopment}
  resetKeys={[userId, projectId]}
  onError={(error, errorInfo) => logError(error)}
>
  <App />
</ErrorBoundaryWithRecovery>
```

## Code Splitting

### Current State

Extensive lazy loading already implemented:

- 35+ routes lazy loaded
- Critical pages eagerly loaded (Dashboard, Welcome, FirstRun, NotFound)
- Optimal bundle splitting via Vite manual chunks

### Bundle Configuration

Separate chunks for:

- `react-vendor` - React core
- `fluentui-components` - Fluent UI components
- `fluentui-icons` - Fluent UI icons (separate to avoid circular deps)
- `ffmpeg-vendor` - FFmpeg library
- `audio-vendor` - Audio visualization
- `router-vendor` - React Router
- `state-vendor` - Zustand and React Query
- `vendor` - Other dependencies

## Best Practices

### When to Use React.memo

✅ **Use for:**

- Large components (>300 lines)
- List item components rendered multiple times
- Components with expensive render logic
- Components that re-render frequently but props rarely change

❌ **Avoid for:**

- Small components (<50 lines)
- Components that always receive new props
- Components that rarely re-render anyway

### When to Use useCallback

✅ **Use for:**

- Callbacks passed to memoized child components
- Callbacks used in dependency arrays
- Event handlers in list items

❌ **Avoid for:**

- Simple inline functions in JSX
- Callbacks not passed to children
- Over-optimization (measure first!)

### Performance Testing

```tsx
// In development, use performance hooks
function MyOptimizedComponent(props) {
  useWhyDidYouUpdate('MyOptimizedComponent', props);
  useRenderTime('MyOptimizedComponent');

  // Component logic
}
```

## Metrics & Validation

### Before Optimization

- TBD: Baseline measurements needed

### After Optimization

- TBD: Post-optimization measurements needed

### How to Measure

1. Use React DevTools Profiler
2. Check render count and duration
3. Monitor bundle size with `npm run build`
4. Review performance budgets in build output

## Future Improvements

### Potential Optimizations

- [ ] Add virtualization to Jobs list pages (if needed)
- [ ] Implement skeleton loaders for data fetching
- [ ] Add service worker for offline support
- [ ] Implement request deduplication for parallel requests
- [ ] Consider React.lazy for large modal components

### Performance Testing

- [ ] Add automated performance tests
- [ ] Set up Lighthouse CI integration
- [ ] Create performance benchmarks
- [ ] Monitor Core Web Vitals

## Related Documentation

- [Vite Configuration](../vite.config.ts)
- [Bundle Analysis](../package.json) - Run `npm run build:analyze`
- [React DevTools](https://react.dev/learn/react-developer-tools)
