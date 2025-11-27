# Context Menu System

This directory contains documentation for Aura Video Studio's context menu system.

## Overview

The context menu system provides native OS context menus throughout the application, built on Electron's Menu API.

## Related Files

| File | Location | Description |
|------|----------|-------------|
| Type Definitions | `../context-menu-types.ts` | TypeScript types for all menu types |
| Menu Builder | `../context-menu-builder.js` | Main menu builder service |
| IPC Handler | `../ipc-handlers/context-menu-handler.js` | IPC communication handler |
| Preload Script | `../preload.js` | Electron preload with contextMenu API |
| React Hook | `Aura.Web/src/hooks/useContextMenu.ts` | React integration hook |
| Frontend Types | `Aura.Web/src/types/electron-context-menu.ts` | Frontend TypeScript types |

## Quick Start

### Add a new menu type:

1. Define types in `context-menu-types.ts`
2. Add builder method in `context-menu-builder.js`
3. Update action map in `context-menu-handler.js`
4. Update frontend types in `Aura.Web/src/types/electron-context-menu.ts`
5. Create React hook in `Aura.Web/src/hooks/`
6. Use in components

## Documentation

- [Full Documentation](/docs/CONTEXT_MENUS.md)
- [Developer Guide](/docs/CONTEXT_MENU_DEVELOPMENT.md)

## Testing

Run unit tests:

```bash
npm run test:context-menu
```

Run E2E tests:

```bash
npm run test:e2e:context-menus
```

Run all context menu tests:

```bash
npm run test:all-context-menus
```

## Architecture

Context menus use Electron's native Menu API, which provides:

- OS-native appearance and behavior
- Automatic keyboard shortcut handling
- Accessibility support
- Multi-platform compatibility

### Flow Diagram

```
┌─────────────────┐
│ User Right-Click│
└────────┬────────┘
         │
┌────────▼────────┐
│ React Component │
│ (onContextMenu) │
└────────┬────────┘
         │
┌────────▼────────┐
│ useContextMenu  │
│    Hook         │
└────────┬────────┘
         │
┌────────▼────────┐
│   IPC Call      │
│ (preload.js)    │
└────────┬────────┘
         │
┌────────▼────────┐
│ Context Menu    │
│   Handler       │
└────────┬────────┘
         │
┌────────▼────────┐
│ Context Menu    │
│   Builder       │
└────────┬────────┘
         │
┌────────▼────────┐
│ Native OS Menu  │
└────────┬────────┘
         │
┌────────▼────────┐
│ User Selection  │
└────────┬────────┘
         │
┌────────▼────────┐
│  IPC Callback   │
└────────┬────────┘
         │
┌────────▼────────┐
│ React Handler   │
│ (useContext-    │
│  MenuAction)    │
└────────┬────────┘
         │
┌────────▼────────┐
│  State Update   │
└─────────────────┘
```

## Available Menu Types

1. **timeline-clip** - Right-click on timeline clips
2. **timeline-track** - Right-click on track headers
3. **timeline-empty** - Right-click on empty timeline area
4. **media-asset** - Right-click on media library assets
5. **ai-script** - Right-click on AI-generated script scenes
6. **job-queue** - Right-click on job queue items
7. **preview-window** - Right-click on video preview
8. **ai-provider** - Right-click on AI provider settings

## Contributing

When adding new context menus:

1. Follow existing naming conventions
2. Add comprehensive tests
3. Update documentation
4. Consider accessibility
5. Test on all platforms

See [CONTRIBUTING.md](/CONTRIBUTING.md) for general contribution guidelines.
