/**
 * Context Menu Builder Unit Tests
 *
 * Comprehensive unit tests for the ContextMenuBuilder class.
 * Tests all menu types, callback invocation, and state management.
 *
 * Run with: npm run test:context-menus
 */

const assert = require('assert');

// Mock Electron's Menu module for testing
let mockMenuBuiltFrom = null;

const mockMenu = {
  buildFromTemplate: (template) => {
    mockMenuBuiltFrom = template;
    return {
      popup: ({ window }) => {
        // Simulate menu popup
      },
      items: template,
    };
  },
};

// Override require for Menu
const Module = require('module');
const originalRequire = Module.prototype.require;
Module.prototype.require = function (id) {
  if (id === 'electron') {
    return { Menu: mockMenu };
  }
  return originalRequire.apply(this, arguments);
};

// Now require the ContextMenuBuilder
const { ContextMenuBuilder } = require('../../electron/context-menu-builder');

console.log('='.repeat(60));
console.log('Context Menu Builder Unit Tests');
console.log('='.repeat(60));

// Mock logger
const mockLogger = {
  debug: () => {},
  info: () => {},
  warn: (...args) => console.warn('[WARN]', ...args),
  error: (...args) => console.error('[ERROR]', ...args),
};

const builder = new ContextMenuBuilder(mockLogger);

// Test Suite: buildTimelineClipMenu
console.log('\n--- Testing buildTimelineClipMenu ---');

console.log('\n1. Menu should have all expected items...');
const clipData = { clipId: 'test', hasClipboardData: false };
const clipMenu = builder.buildTimelineClipMenu(clipData, {});
const clipItems = clipMenu.items;
assert(clipItems.length >= 9, `Expected at least 9 items, got ${clipItems.length}`);
console.log('✓ Menu has all expected items');

console.log('\n2. Paste should be disabled when no clipboard data...');
const pasteItem = clipItems.find((item) => item.label === 'Paste');
assert.strictEqual(pasteItem.enabled, false);
console.log('✓ Paste is disabled when no clipboard data');

console.log('\n3. Paste should be enabled when clipboard has data...');
const clipDataWithClipboard = { clipId: 'test', hasClipboardData: true };
builder.buildTimelineClipMenu(clipDataWithClipboard, {});
const pasteItemEnabled = mockMenuBuiltFrom.find((item) => item.label === 'Paste');
assert.strictEqual(pasteItemEnabled.enabled, true);
console.log('✓ Paste is enabled when clipboard has data');

console.log('\n4. Menu items should have correct accelerators...');
const cutItem = clipItems.find((item) => item.label === 'Cut');
const copyItem = clipItems.find((item) => item.label === 'Copy');
const deleteItem = clipItems.find((item) => item.label === 'Delete');
assert.strictEqual(cutItem.accelerator, 'CmdOrCtrl+X');
assert.strictEqual(copyItem.accelerator, 'CmdOrCtrl+C');
assert.strictEqual(deleteItem.accelerator, 'Delete');
console.log('✓ Menu items have correct accelerators');

// Test Suite: buildJobQueueMenu
console.log('\n--- Testing buildJobQueueMenu ---');

console.log('\n5. Pause should be enabled for running job...');
const runningJobData = { jobId: 'test', status: 'running' };
builder.buildJobQueueMenu(runningJobData, {});
const pauseItem = mockMenuBuiltFrom.find((item) => item.label === 'Pause Job');
assert.strictEqual(pauseItem.enabled, true);
console.log('✓ Pause is enabled for running job');

console.log('\n6. Resume should be enabled for paused job...');
const pausedJobData = { jobId: 'test', status: 'paused' };
builder.buildJobQueueMenu(pausedJobData, {});
const resumeItem = mockMenuBuiltFrom.find((item) => item.label === 'Resume Job');
assert.strictEqual(resumeItem.enabled, true);
console.log('✓ Resume is enabled for paused job');

console.log('\n7. Retry should be enabled for failed job...');
const failedJobData = { jobId: 'test', status: 'failed' };
builder.buildJobQueueMenu(failedJobData, {});
const retryItem = mockMenuBuiltFrom.find((item) => item.label === 'Retry Job');
assert.strictEqual(retryItem.enabled, true);
console.log('✓ Retry is enabled for failed job');

console.log('\n8. Output actions should be enabled for completed job with output...');
const completedJobData = { jobId: 'test', status: 'completed', outputPath: '/output/video.mp4' };
builder.buildJobQueueMenu(completedJobData, {});
const openItem = mockMenuBuiltFrom.find((item) => item.label === 'Open Output File');
const revealItem = mockMenuBuiltFrom.find((item) => item.label === 'Reveal Output in Explorer');
assert.strictEqual(openItem.enabled, true);
assert.strictEqual(revealItem.enabled, true);
console.log('✓ Output actions are enabled for completed job with output');

console.log('\n9. Output actions should be disabled for completed job without output...');
const completedNoOutputData = { jobId: 'test', status: 'completed', outputPath: null };
builder.buildJobQueueMenu(completedNoOutputData, {});
const openItemDisabled = mockMenuBuiltFrom.find((item) => item.label === 'Open Output File');
const revealItemDisabled = mockMenuBuiltFrom.find((item) => item.label === 'Reveal Output in Explorer');
assert.strictEqual(openItemDisabled.enabled, false);
assert.strictEqual(revealItemDisabled.enabled, false);
console.log('✓ Output actions are disabled for completed job without output');

// Test Suite: buildTimelineTrackMenu
console.log('\n--- Testing buildTimelineTrackMenu ---');

console.log('\n10. Checkbox states should reflect data...');
const trackData = {
  trackId: 'track-1',
  isLocked: true,
  isMuted: true,
  isSolo: false,
  totalTracks: 3,
};
builder.buildTimelineTrackMenu(trackData, {});
const lockItem = mockMenuBuiltFrom.find((item) => item.label === 'Lock Track');
const muteItem = mockMenuBuiltFrom.find((item) => item.label === 'Mute Track');
const soloItem = mockMenuBuiltFrom.find((item) => item.label === 'Solo Track');
assert.strictEqual(lockItem.checked, true);
assert.strictEqual(muteItem.checked, true);
assert.strictEqual(soloItem.checked, false);
console.log('✓ Checkbox states reflect data correctly');

console.log('\n11. Delete should be disabled when only one track...');
const singleTrackData = { ...trackData, totalTracks: 1 };
builder.buildTimelineTrackMenu(singleTrackData, {});
const deleteTrackItem = mockMenuBuiltFrom.find((item) => item.label === 'Delete Track');
assert.strictEqual(deleteTrackItem.enabled, false);
console.log('✓ Delete is disabled when only one track');

console.log('\n12. Delete should be enabled when multiple tracks...');
builder.buildTimelineTrackMenu(trackData, {}); // totalTracks = 3
const deleteTrackEnabled = mockMenuBuiltFrom.find((item) => item.label === 'Delete Track');
assert.strictEqual(deleteTrackEnabled.enabled, true);
console.log('✓ Delete is enabled when multiple tracks');

// Test Suite: buildPreviewWindowMenu
console.log('\n--- Testing buildPreviewWindowMenu ---');

console.log('\n13. Play label should show when not playing...');
const previewNotPlaying = { isPlaying: false, currentTime: 0, duration: 60, zoom: 1.0 };
builder.buildPreviewWindowMenu(previewNotPlaying, {});
const playItem = mockMenuBuiltFrom.find((item) => item.label === 'Play');
assert(playItem, 'Should have Play item when not playing');
console.log('✓ Play label shows when not playing');

console.log('\n14. Pause label should show when playing...');
const previewPlaying = { isPlaying: true, currentTime: 30, duration: 60, zoom: 1.0 };
builder.buildPreviewWindowMenu(previewPlaying, {});
const pauseItemPreview = mockMenuBuiltFrom.find((item) => item.label === 'Pause');
assert(pauseItemPreview, 'Should have Pause item when playing');
console.log('✓ Pause label shows when playing');

console.log('\n15. Zoom submenu should have correct items...');
const zoomItem = mockMenuBuiltFrom.find((item) => item.label === 'Zoom');
assert(zoomItem, 'Should have Zoom item');
assert(Array.isArray(zoomItem.submenu), 'Zoom should have submenu');
assert.strictEqual(zoomItem.submenu.length, 4, 'Zoom submenu should have 4 items');
const zoomLabels = zoomItem.submenu.map((item) => item.label);
assert(zoomLabels.includes('Fit to Window'));
assert(zoomLabels.includes('50%'));
assert(zoomLabels.includes('100%'));
assert(zoomLabels.includes('200%'));
console.log('✓ Zoom submenu has correct items');

// Test Suite: Unknown menu type
console.log('\n--- Testing Unknown Menu Type ---');

console.log('\n16. Unknown menu type should return fallback menu...');
const unknownMenu = builder.build('unknown-type', {}, {});
assert(unknownMenu, 'Should return a menu');
const unknownItem = mockMenuBuiltFrom.find((item) => item.label === 'Unknown menu type');
assert(unknownItem, 'Should have Unknown menu type item');
assert.strictEqual(unknownItem.enabled, false);
console.log('✓ Unknown menu type returns fallback menu');

// Test Suite: Callback invocation
console.log('\n--- Testing Callback Invocation ---');

console.log('\n17. Callback should be invoked with correct data...');
let callbackInvoked = false;
let callbackData = null;
const callbacks = {
  onCut: (data) => {
    callbackInvoked = true;
    callbackData = data;
  },
};

const testData = { clipId: 'test-123', clipType: 'video' };
builder.buildTimelineClipMenu(testData, callbacks);

const cutItemWithCallback = mockMenuBuiltFrom.find((item) => item.label === 'Cut');
assert(typeof cutItemWithCallback.click === 'function', 'Cut item should have click handler');

cutItemWithCallback.click();
assert(callbackInvoked, 'Callback should be invoked');
assert.deepStrictEqual(callbackData, testData, 'Callback should receive correct data');
console.log('✓ Callback is invoked with correct data');

console.log('\n18. Multiple callbacks should work correctly...');
let copyCalled = false;
let pasteCalled = false;
let deleteCalled = false;

const multiCallbacks = {
  onCopy: () => { copyCalled = true; },
  onPaste: () => { pasteCalled = true; },
  onDelete: () => { deleteCalled = true; },
};

builder.buildTimelineClipMenu({ clipId: 'test', hasClipboardData: true }, multiCallbacks);

const copyItemWithCallback = mockMenuBuiltFrom.find((item) => item.label === 'Copy');
const pasteItemWithCallback = mockMenuBuiltFrom.find((item) => item.label === 'Paste');
const deleteItemWithCallback = mockMenuBuiltFrom.find((item) => item.label === 'Delete');

copyItemWithCallback.click();
pasteItemWithCallback.click();
deleteItemWithCallback.click();

assert(copyCalled, 'Copy callback should be called');
assert(pasteCalled, 'Paste callback should be called');
assert(deleteCalled, 'Delete callback should be called');
console.log('✓ Multiple callbacks work correctly');

// Test Suite: All menu types
console.log('\n--- Testing All Menu Types ---');

console.log('\n19. All menu types should build correctly...');
const menuTypes = [
  { type: 'timeline-clip', data: { clipId: 'test' } },
  { type: 'timeline-track', data: { trackId: 'track-1', totalTracks: 2 } },
  { type: 'timeline-empty', data: { timePosition: 0 } },
  { type: 'media-asset', data: { assetId: 'asset-1' } },
  { type: 'ai-script', data: { sceneIndex: 0, sceneText: 'Test' } },
  { type: 'job-queue', data: { jobId: 'job-1', status: 'running' } },
  { type: 'preview-window', data: { isPlaying: false, duration: 60 } },
  { type: 'ai-provider', data: { providerId: 'openai', isDefault: false } },
];

menuTypes.forEach(({ type, data }) => {
  const menu = builder.build(type, data, {});
  assert(menu, `Menu should be built for type: ${type}`);
  assert(mockMenuBuiltFrom.length > 0, `Menu should have items for type: ${type}`);
});
console.log('✓ All menu types build correctly');

// Test Suite: Media Asset Menu
console.log('\n--- Testing Media Asset Menu ---');

console.log('\n20. Media asset menu should have all expected items...');
const assetData = { assetId: 'asset-1', isFavorite: true };
builder.buildMediaAssetMenu(assetData, {});
const assetItems = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(assetItems.includes('Add to Timeline'));
assert(assetItems.includes('Preview'));
assert(assetItems.includes('Rename'));
assert(assetItems.includes('Add to Favorites'));
assert(assetItems.includes('Properties'));
console.log('✓ Media asset menu has all expected items');

console.log('\n21. Favorites checkbox should reflect state...');
const favoritesItem = mockMenuBuiltFrom.find((item) => item.label === 'Add to Favorites');
assert.strictEqual(favoritesItem.checked, true);
console.log('✓ Favorites checkbox reflects state');

// Test Suite: AI Script Menu
console.log('\n--- Testing AI Script Menu ---');

console.log('\n22. AI script menu should have all expected items...');
const scriptData = { sceneIndex: 0, sceneText: 'Test scene', jobId: 'job-1' };
builder.buildAIScriptMenu(scriptData, {});
const scriptItems = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(scriptItems.includes('Regenerate This Scene'));
assert(scriptItems.includes('Expand Section'));
assert(scriptItems.includes('Shorten Section'));
assert(scriptItems.includes('Generate B-Roll Suggestions'));
assert(scriptItems.includes('Copy Text'));
console.log('✓ AI script menu has all expected items');

// Test Suite: AI Provider Menu
console.log('\n--- Testing AI Provider Menu ---');

console.log('\n23. AI provider menu should have all expected items...');
const providerData = { providerId: 'openai', isDefault: true };
builder.buildAIProviderMenu(providerData, {});
const providerItems = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(providerItems.includes('Test Connection'));
assert(providerItems.includes('View Usage Stats'));
assert(providerItems.includes('Set as Default'));
assert(providerItems.includes('Configure'));
console.log('✓ AI provider menu has all expected items');

console.log('\n24. Default checkbox should reflect state...');
const defaultItem = mockMenuBuiltFrom.find((item) => item.label === 'Set as Default');
assert.strictEqual(defaultItem.checked, true);
console.log('✓ Default checkbox reflects state');

// Restore original require
Module.prototype.require = originalRequire;

console.log('\n' + '='.repeat(60));
console.log('ALL CONTEXT MENU BUILDER UNIT TESTS PASSED ✓');
console.log('='.repeat(60));
console.log('Total tests: 24');
console.log('Verified:');
console.log('  - Timeline clip menu items and states');
console.log('  - Job queue menu conditional states');
console.log('  - Timeline track menu checkboxes');
console.log('  - Preview window menu dynamic labels');
console.log('  - Zoom submenu structure');
console.log('  - Unknown menu type fallback');
console.log('  - Callback invocation with correct data');
console.log('  - All 8 menu types build correctly');
console.log('  - Media asset menu items');
console.log('  - AI script menu items');
console.log('  - AI provider menu items');
console.log('='.repeat(60));
