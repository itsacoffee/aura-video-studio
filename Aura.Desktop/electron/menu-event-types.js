/**
 * Menu Event Channel Definitions
 * 
 * This file defines all valid menu event channels that can be sent from
 * menu-builder.js to the renderer process via IPC.
 * 
 * CRITICAL: Every channel in this array MUST have a corresponding exposure
 * in preload.js and a listener in useElectronMenuEvents.ts
 */

const MENU_EVENT_CHANNELS = [
  'menu:newProject',
  'menu:openProject',
  'menu:openRecentProject',
  'menu:saveProject',
  'menu:saveProjectAs',
  'menu:importVideo',
  'menu:importAudio',
  'menu:importImages',
  'menu:importDocument',
  'menu:exportVideo',
  'menu:exportTimeline',
  'menu:find',
  'menu:openPreferences',
  'menu:openProviderSettings',
  'menu:openFFmpegConfig',
  'menu:clearCache',
  'menu:viewLogs',
  'menu:runDiagnostics',
  'menu:openGettingStarted',
  'menu:showKeyboardShortcuts',
  'menu:checkForUpdates'
];

/**
 * Validate that a channel is a valid menu event channel
 * @param {string} channel - The channel to validate
 * @returns {boolean} True if channel is valid
 */
function isValidMenuEventChannel(channel) {
  return MENU_EVENT_CHANNELS.includes(channel);
}

/**
 * Get all menu event channels
 * @returns {string[]} Array of all menu event channels
 */
function getMenuEventChannels() {
  return [...MENU_EVENT_CHANNELS];
}

module.exports = {
  MENU_EVENT_CHANNELS,
  isValidMenuEventChannel,
  getMenuEventChannels
};
