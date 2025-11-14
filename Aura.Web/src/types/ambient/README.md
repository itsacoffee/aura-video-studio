# Ambient Type Declarations

This directory contains ambient TypeScript declaration files that extend global types and provide types for modules without built-in declarations.

## Files

- **window.d.ts** - Extends the Window interface with Electron API and Aura globals (AURA_IS_ELECTRON, AURA_BACKEND_URL, etc.)
- **assets.d.ts** - Module declarations for asset imports (images, audio, video, fonts)

## Purpose

Ambient declarations solve TypeScript compilation errors for:

1. Third-party modules without type definitions
2. Global extensions (window, HTMLElement, etc.)
3. Asset imports in Vite build system

## Ownership

All ambient declarations are maintained by the Aura.Web frontend team. When adding new ambient declarations:

1. Document the purpose and source
2. Keep declarations minimal and accurate
3. Update this README with the new file

## References

- TypeScript Handbook: https://www.typescriptlang.org/docs/handbook/declaration-files/introduction.html
- Vite Asset Handling: https://vitejs.dev/guide/assets.html
