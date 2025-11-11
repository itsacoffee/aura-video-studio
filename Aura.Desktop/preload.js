/**
 * Preload Script Entry Point
 * 
 * This file simply redirects to the actual preload script in the electron/ directory.
 * This is kept for backwards compatibility with the old structure.
 */

module.exports = require('./electron/preload');
