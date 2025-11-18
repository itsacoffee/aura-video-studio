/**
 * ❌ LEGACY ELECTRON ENTRY — DO NOT USE IN PRODUCTION OR DEVELOPMENT ❌
 * 
 * This file is kept ONLY as a historical reference for the original monolithic
 * architecture that existed before the modular refactoring.
 * 
 * ⚠️  CRITICAL: This file MUST NEVER be used as an entry point ⚠️
 * 
 * The canonical main entry is `electron/main.js`.
 * 
 * If you see this file being referenced by tooling or configuration,
 * TREAT THAT AS A BUG and update the config to point to `electron/main.js`.
 * 
 * This file contains an execution guard that will immediately throw an error
 * if it is ever loaded, preventing silent misconfiguration.
 * 
 * MIGRATION NOTES:
 * - The original monolithic electron.js has been refactored into a modular
 *   architecture under the electron/ directory
 * - All functionality has been split into focused, maintainable modules
 * - See electron/README.md for the current architecture
 * 
 * MODULE MAPPING (for historical reference):
 * - electron.js (monolithic) → electron/main.js (orchestrator)
 * - Window management → electron/window-manager.js
 * - Configuration → electron/app-config.js
 * - Backend process → electron/backend-service.js
 * - System tray → electron/tray-manager.js
 * - Application menu → electron/menu-builder.js
 * - Protocol handling → electron/protocol-handler.js
 * - IPC handlers → electron/ipc-handlers/*.js
 * 
 * @deprecated This file exists only for reference and must not be executed
 */

// ============================================================================
// EXECUTION GUARD - THIS MUST BE THE FIRST EXECUTABLE CODE
// ============================================================================

const errorMessage = `
╔════════════════════════════════════════════════════════════════════════════╗
║                           CONFIGURATION ERROR                              ║
╟────────────────────────────────────────────────────────────────────────────╢
║                                                                            ║
║  The legacy electron.js file was executed. This is a bug.                 ║
║                                                                            ║
║  This file MUST NOT be used as an entry point.                            ║
║                                                                            ║
║  ✓ Correct entry point: electron/main.js                                  ║
║  ✗ Incorrect entry point: electron.js (this file)                         ║
║                                                                            ║
║  REQUIRED ACTION:                                                          ║
║  Update your configuration to use "electron/main.js" as the main entry.   ║
║                                                                            ║
║  Check these locations:                                                    ║
║  - package.json "main" field (should be "electron/main.js")               ║
║  - Any npm scripts that reference "electron.js"                           ║
║  - Any electron-builder configuration                                     ║
║  - Any custom build scripts or tooling                                    ║
║                                                                            ║
║  For more information, see:                                                ║
║  - Aura.Desktop/README.md                                                  ║
║  - Aura.Desktop/electron/README.md                                         ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝
`;

console.error(errorMessage);

// Immediately throw to prevent any further execution
throw new Error(
  'FATAL: Legacy electron.js was executed. ' +
  'This file must not be used as an entry point. ' +
  'Use electron/main.js instead. ' +
  'Check your package.json "main" field and build configuration.'
);

// ============================================================================
// The code below this line is never executed due to the throw above
// It exists only for historical reference
// ============================================================================

/*
 * HISTORICAL NOTE:
 * 
 * This file originally contained a monolithic Electron main process
 * implementation of approximately 867 lines. It has been refactored into
 * a clean, modular architecture located in the electron/ directory.
 * 
 * The refactoring provides:
 * - Better separation of concerns
 * - Improved maintainability
 * - Easier testing
 * - Clearer code organization
 * - Type safety with TypeScript definitions
 * 
 * For the current implementation, see electron/main.js and related modules.
 */
