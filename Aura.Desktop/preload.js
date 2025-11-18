/**
 * ⚠️  LEGACY PRELOAD WRAPPER — DO NOT USE DIRECTLY IN NEW CONFIG ⚠️
 * 
 * The canonical preload script is `electron/preload.js`. This file exists ONLY
 * to support older configurations that cannot be immediately changed.
 * 
 * ANY DIRECT REFERENCE TO THIS FILE IN NEW CODE OR CONFIG SHOULD BE CONSIDERED A BUG.
 * 
 * This wrapper provides a safe forwarder to the canonical preload script.
 * If resolution or loading fails, it will log a clear error and throw to fail fast.
 * 
 * MIGRATION PATH:
 * - All new code should reference `electron/preload.js` directly
 * - Update any BrowserWindow configuration to use `electron/preload.js`
 * - This file will be removed in a future major version
 * 
 * @deprecated Use electron/preload.js directly
 */

const path = require('path');

// Resolve absolute path to the canonical preload script
const canonicalPreloadPath = path.join(__dirname, 'electron', 'preload.js');

// Log warning about legacy usage
console.warn('⚠️  WARNING: Loading legacy preload.js redirect');
console.warn('   The canonical preload script is: electron/preload.js');
console.warn('   This redirect exists only for backwards compatibility');
console.warn('   Please update your configuration to use electron/preload.js directly');
console.warn(`   Forwarding to: ${canonicalPreloadPath}`);

try {
  // Attempt to load the canonical preload script
  module.exports = require(canonicalPreloadPath);
  console.log('✓ Successfully forwarded to electron/preload.js');
} catch (error) {
  // Fail fast with clear error message
  const errorMessage = `
╔════════════════════════════════════════════════════════════════════════════╗
║ FATAL ERROR: Failed to load canonical preload script                      ║
╟────────────────────────────────────────────────────────────────────────────╢
║ Expected path: ${canonicalPreloadPath}
║ Error: ${error.message}
║                                                                            ║
║ This is a critical configuration error. The application cannot continue.  ║
║ Please ensure electron/preload.js exists and is not corrupted.            ║
╚════════════════════════════════════════════════════════════════════════════╝
`;
  console.error(errorMessage);
  throw new Error(`Failed to load canonical preload script: ${error.message}`);
}
