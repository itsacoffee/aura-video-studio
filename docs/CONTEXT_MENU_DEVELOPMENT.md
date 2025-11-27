# Context Menu Development Guide

## Project Structure

```
Aura.Desktop/electron/
  context-menu-types.ts         # TypeScript type definitions
  context-menu-builder.js       # Menu builder service
  ipc-handlers/
    context-menu-handler.js     # IPC handler for context menus
  preload.js                    # Electron preload script

Aura.Web/src/
  hooks/
    useContextMenu.ts           # React hook for context menus
    useJobQueueContextMenu.ts   # Specialized hook for job queue
    useMediaAssetContextMenu.ts # Specialized hook for media assets
    useAIScriptContextMenu.ts   # Specialized hook for AI scripts
    useAIProviderContextMenu.ts # Specialized hook for AI providers
    usePreviewContextMenu.ts    # Specialized hook for preview window
  types/
    electron-context-menu.ts    # TypeScript definitions for frontend
  components/                   # Components using context menus
```

## Development Workflow

### 1. Planning

Before implementing a new context menu:

- Identify the component that needs a context menu
- Define what actions are needed
- Design the menu structure (items, separators, submenus)
- Consider keyboard shortcuts (avoid conflicts)
- Plan conditional states (enabled/disabled, checkboxes)

### 2. Implementation Order

Follow this order for implementing new context menus:

1. Add type definitions (`context-menu-types.ts`)
2. Add menu builder method (`context-menu-builder.js`)
3. Update IPC handler action map (`context-menu-handler.js`)
4. Update frontend types (`electron-context-menu.ts`)
5. Create or update React hook (`useContextMenu.ts`)
6. Integrate into component
7. Add unit tests
8. Add integration tests
9. Update documentation

### 3. Testing Strategy

#### Unit Tests

Test the menu builder logic in isolation:

```javascript
// test-context-menu-builder.test.js
const { ContextMenuBuilder } = require('../electron/context-menu-builder');

describe('ContextMenuBuilder', () => {
  let builder;
  
  beforeEach(() => {
    builder = new ContextMenuBuilder(mockLogger);
  });

  it('should build menu with correct items', () => {
    const menu = builder.build('my-menu', data, callbacks);
    expect(menu.items).toHaveLength(5);
  });
});
```

#### Integration Tests

Test React hook integration:

```tsx
// context-menu-integration.test.tsx
import { render, fireEvent } from '@testing-library/react';
import { useContextMenu } from '../hooks/useContextMenu';

describe('Context Menu Integration', () => {
  it('should call showContextMenu on right-click', () => {
    const { getByTestId } = render(<Component />);
    fireEvent.contextMenu(getByTestId('element'));
    expect(mockShowContextMenu).toHaveBeenCalled();
  });
});
```

#### E2E Tests

Test complete user workflows:

```javascript
// context-menus.spec.js
describe('Context Menus E2E', () => {
  it('should show context menu on right-click', async () => {
    await element.rightClick();
    // Verify menu appears and actions work
  });
});
```

### 4. Code Review Checklist

Before submitting a PR:

- [ ] Type definitions are complete and accurate
- [ ] Menu items have appropriate labels
- [ ] Keyboard shortcuts don't conflict with existing ones
- [ ] Disabled states are handled correctly
- [ ] Callbacks are properly registered in action map
- [ ] Error handling is in place
- [ ] Console logs are removed or set to debug level
- [ ] Documentation is updated
- [ ] Unit tests pass
- [ ] Integration tests pass

## Common Patterns

### Pattern 1: Simple Menu

Use when you have a few actions without complex logic:

```tsx
function SimpleComponent() {
  const showMenu = useContextMenu('simple-menu');
  
  useContextMenuAction('simple-menu', 'onAction', handleAction);

  return (
    <div onContextMenu={(e) => showMenu(e, { id: 'item-1' })}>
      Content
    </div>
  );
}
```

### Pattern 2: Stateful Menu

Use when menu items depend on component state:

```tsx
function StatefulComponent({ item }) {
  const showMenu = useContextMenu('stateful-menu');
  
  const handleContextMenu = useCallback((e) => {
    showMenu(e, {
      ...item,
      canDelete: canDeleteItem(item),
      isSelected: selectedItems.includes(item.id),
      clipboardHasData: clipboard.hasData(),
    });
  }, [item, showMenu, selectedItems, clipboard]);

  return <div onContextMenu={handleContextMenu}>Content</div>;
}
```

### Pattern 3: Nested Context Menus

Use when different areas need different menus:

```tsx
function ParentComponent() {
  const showParentMenu = useContextMenu('parent-menu');
  const showChildMenu = useContextMenu('child-menu');

  return (
    <div onContextMenu={(e) => showParentMenu(e, parentData)}>
      <div 
        onContextMenu={(e) => {
          e.stopPropagation(); // Prevent parent menu
          showChildMenu(e, childData);
        }}
      >
        Child content (has its own menu)
      </div>
    </div>
  );
}
```

### Pattern 4: Specialized Hook

Create specialized hooks for frequently used menus:

```tsx
// useMyFeatureContextMenu.ts
export function useMyFeatureContextMenu(callbacks: MyFeatureCallbacks) {
  const showContextMenu = useContextMenu<MyFeatureMenuData>('my-feature');

  useContextMenuAction('my-feature', 'onEdit', callbacks.onEdit);
  useContextMenuAction('my-feature', 'onDelete', callbacks.onDelete);
  useContextMenuAction('my-feature', 'onDuplicate', callbacks.onDuplicate);

  return useCallback(
    (e: React.MouseEvent, item: MyFeatureItem) => {
      showContextMenu(e, {
        itemId: item.id,
        canEdit: item.editable,
        canDelete: !item.locked,
      });
    },
    [showContextMenu]
  );
}
```

### Pattern 5: Confirmation Dialog

For destructive actions, show a confirmation:

```tsx
useContextMenuAction('my-menu', 'onDelete', useCallback(
  async (data) => {
    const confirmed = await showConfirmDialog({
      title: 'Delete Item',
      message: 'Are you sure you want to delete this item?',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
    });
    
    if (confirmed) {
      deleteItem(data.itemId);
    }
  },
  [showConfirmDialog, deleteItem]
));
```

## Best Practices

### DO:

- Use semantic menu item labels (action-oriented)
- Group related actions with separators
- Provide keyboard shortcuts for common actions
- Disable items that aren't applicable (don't hide them)
- Use checkboxes for toggleable states
- Show confirmation dialogs for destructive actions
- Log important actions for debugging (debug level)
- Memoize callbacks to prevent unnecessary re-renders
- Clean up action listeners when component unmounts

### DON'T:

- Put more than 10 items in a menu
- Use technical jargon in labels
- Create deeply nested submenus (max 2 levels)
- Forget to handle errors in callbacks
- Block the UI thread in menu actions
- Expose sensitive data in menu labels
- Forget to clean up event listeners
- Use async operations in menu item click handlers without proper error handling

## Debugging

### Enable Debug Logging

In the context menu builder, set log level:

```javascript
const logger = createLogger({ level: 'debug' });
const builder = new ContextMenuBuilder(logger);
```

### Inspect IPC Messages

Add logging in preload.js to debug IPC communication:

```javascript
contextMenu: {
  show: (type, data) => {
    console.log('[Debug] Context menu show:', type, data);
    return ipcRenderer.invoke('context-menu:show', type, data);
  }
}
```

### Test Menu Building

Run unit tests for the context menu builder:

```bash
cd Aura.Desktop
npm run test:context-menu
```

### Simulate User Actions

In tests, simulate right-clicks:

```javascript
// Using Testing Library
fireEvent.contextMenu(element);

// Using Playwright
await page.click('[data-testid="clip"]', { button: 'right' });
```

### Debug Action Callbacks

Add logging to verify callbacks are firing:

```tsx
useContextMenuAction('my-menu', 'onAction', (data) => {
  console.log('[Debug] Action triggered:', data);
  // ... actual handler
});
```

## Performance Optimization

### Lazy Loading

Menus are only built when shown, not on component mount. This is the default behavior - no extra work needed.

### Memoization

Use `React.useCallback` for menu handlers:

```tsx
// Good - memoized, won't cause re-renders
const handleContextMenu = useCallback((e) => {
  showMenu(e, data);
}, [showMenu, data]);

// Bad - new function on every render
const handleContextMenu = (e) => showMenu(e, data);
```

### Avoid Heavy Computation

Don't compute expensive data in the context menu handler:

```tsx
// Bad - computes on every right-click
const handleContextMenu = (e) => {
  showMenu(e, {
    ...data,
    statistics: computeExpensiveStatistics(), // Slow!
  });
};

// Good - use pre-computed or cached data
const statistics = useMemo(() => computeExpensiveStatistics(), [deps]);
const handleContextMenu = (e) => {
  showMenu(e, { ...data, statistics });
};
```

### Debouncing

For frequently updating menus:

```tsx
const debouncedUpdate = useMemo(
  () => debounce(updateMenuData, 100),
  [updateMenuData]
);

useEffect(() => {
  debouncedUpdate(newData);
  return () => debouncedUpdate.cancel();
}, [newData, debouncedUpdate]);
```

## Troubleshooting Guide

### Problem: Menu appears in wrong location

**Cause:** Event coordinates not passed correctly

**Solution:** Ensure you're passing the MouseEvent to showContextMenu:

```tsx
// Good
onContextMenu={(e) => showMenu(e, data)}

// Bad - e is undefined
onContextMenu={() => showMenu(undefined, data)}
```

### Problem: Menu items are all disabled

**Cause:** Data not being passed correctly to builder

**Solution:** Verify data properties in the builder:

```javascript
buildMyMenu(data, callbacks) {
  console.log('Building menu with data:', data);
  // Check data properties
}
```

### Problem: Actions not triggering

**Cause:** Action type mismatch between handler and hook

**Solution:** Verify action types match exactly:

```javascript
// In context-menu-handler.js
const ACTION_MAP = {
  'my-menu': ['onAction', 'onOtherAction'],
};

// In component - must match exactly
useContextMenuAction('my-menu', 'onAction', handler); // ✓
useContextMenuAction('my-menu', 'on-action', handler); // ✗ Wrong
```

### Problem: Memory leak warning

**Cause:** Action listeners not cleaned up

**Solution:** The `useContextMenuAction` hook automatically cleans up on unmount. If you see warnings, ensure you're not conditionally rendering hooks:

```tsx
// Bad - conditional hook
if (someCondition) {
  useContextMenuAction('menu', 'action', handler);
}

// Good - always call, conditionally handle
useContextMenuAction('menu', 'action', (data) => {
  if (someCondition) {
    handler(data);
  }
});
```

### Problem: Multiple menus appear

**Cause:** Event bubbling to parent elements

**Solution:** Use `stopPropagation()`:

```tsx
<div onContextMenu={(e) => {
  e.stopPropagation();
  showChildMenu(e, data);
}}>
```

## Release Checklist

Before releasing context menu features:

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All E2E tests pass
- [ ] Manual testing on Windows completed
- [ ] Manual testing on macOS completed (if applicable)
- [ ] Manual testing on Linux completed (if applicable)
- [ ] Documentation is up to date
- [ ] Keyboard shortcuts documented
- [ ] Accessibility tested (keyboard navigation)
- [ ] Performance tested (menu build time < 50ms)
- [ ] Error handling tested
- [ ] Logging is appropriate (not too verbose)
- [ ] Security review completed (file paths, API keys)
- [ ] Code review approved

## Contributing

When adding new context menus:

1. Follow existing naming conventions
2. Add comprehensive tests
3. Update documentation
4. Consider accessibility
5. Test on all platforms
6. Request code review

See [CONTRIBUTING.md](/CONTRIBUTING.md) for general contribution guidelines.
