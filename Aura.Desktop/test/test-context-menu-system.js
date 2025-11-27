/**
 * Context Menu System Unit Tests
 *
 * Tests the context menu builder and handler functionality.
 * Run with: node test/test-context-menu-system.js
 */

const assert = require('assert');

// Mock Electron's Menu module for testing
const mockMenuItems = [];
let mockMenuBuiltFrom = null;

const mockMenu = {
  buildFromTemplate: (template) => {
    mockMenuBuiltFrom = template;
    return {
      popup: ({ window }) => {
        // Simulate menu popup
        mockMenuItems.push({ template, window });
      },
      items: template
    };
  }
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
const { ContextMenuBuilder } = require('../electron/context-menu-builder');

console.log('='.repeat(60));
console.log('Testing Context Menu System');
console.log('='.repeat(60));

// Mock logger
const logger = {
  debug: () => {},
  info: () => {},
  warn: (...args) => console.warn('[WARN]', ...args),
  error: (...args) => console.error('[ERROR]', ...args)
};

const builder = new ContextMenuBuilder(logger);

// Test 1: Timeline Clip Menu
console.log('\n1. Testing Timeline Clip Menu...');
const clipData = {
  clipId: 'test-123',
  clipType: 'video',
  startTime: 0,
  duration: 10,
  trackId: 'track-1',
  isLocked: false,
  hasAudio: true,
  hasClipboardData: true
};
const clipMenu = builder.build('timeline-clip', clipData, {});
assert(clipMenu, 'Timeline clip menu should be created');
assert(mockMenuBuiltFrom, 'Menu template should be set');
assert(mockMenuBuiltFrom.length > 0, 'Menu template should have items');

// Check for expected menu items
const clipLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(clipLabels.includes('Cut'), 'Should have Cut item');
assert(clipLabels.includes('Copy'), 'Should have Copy item');
assert(clipLabels.includes('Paste'), 'Should have Paste item');
assert(clipLabels.includes('Delete'), 'Should have Delete item');
assert(clipLabels.includes('Properties'), 'Should have Properties item');
console.log('✓ Timeline Clip Menu test passed');

// Test 2: Timeline Track Menu
console.log('\n2. Testing Timeline Track Menu...');
const trackData = {
  trackId: 'track-1',
  trackType: 'video',
  isLocked: false,
  isMuted: false,
  isSolo: false,
  trackIndex: 0,
  totalTracks: 3
};
const trackMenu = builder.build('timeline-track', trackData, {});
assert(trackMenu, 'Timeline track menu should be created');

const trackLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(trackLabels.includes('Add Track Above'), 'Should have Add Track Above item');
assert(trackLabels.includes('Add Track Below'), 'Should have Add Track Below item');
assert(trackLabels.includes('Lock Track'), 'Should have Lock Track item');
assert(trackLabels.includes('Delete Track'), 'Should have Delete Track item');
console.log('✓ Timeline Track Menu test passed');

// Test 3: Timeline Empty Menu
console.log('\n3. Testing Timeline Empty Menu...');
const emptyData = {
  timePosition: 5.5,
  trackId: 'track-1',
  hasClipboardData: false
};
const emptyMenu = builder.build('timeline-empty', emptyData, {});
assert(emptyMenu, 'Timeline empty menu should be created');

const emptyLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(emptyLabels.includes('Paste'), 'Should have Paste item');
assert(emptyLabels.includes('Add Marker'), 'Should have Add Marker item');
assert(emptyLabels.includes('Select All Clips'), 'Should have Select All Clips item');
console.log('✓ Timeline Empty Menu test passed');

// Test 4: Media Asset Menu
console.log('\n4. Testing Media Asset Menu...');
const assetData = {
  assetId: 'asset-456',
  assetType: 'video',
  filePath: '/test/video.mp4',
  isFavorite: false,
  tags: ['test']
};
const assetMenu = builder.build('media-asset', assetData, {});
assert(assetMenu, 'Media asset menu should be created');

const assetLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(assetLabels.includes('Add to Timeline'), 'Should have Add to Timeline item');
assert(assetLabels.includes('Preview'), 'Should have Preview item');
assert(assetLabels.includes('Rename'), 'Should have Rename item');
assert(
  assetLabels.includes('Reveal in File Explorer'),
  'Should have Reveal in File Explorer item'
);
console.log('✓ Media Asset Menu test passed');

// Test 5: AI Script Menu
console.log('\n5. Testing AI Script Menu...');
const scriptData = {
  sceneIndex: 0,
  sceneText: 'Test scene content',
  jobId: 'job-789'
};
const scriptMenu = builder.build('ai-script', scriptData, {});
assert(scriptMenu, 'AI script menu should be created');

const scriptLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(
  scriptLabels.includes('Regenerate This Scene'),
  'Should have Regenerate This Scene item'
);
assert(scriptLabels.includes('Expand Section'), 'Should have Expand Section item');
assert(scriptLabels.includes('Copy Text'), 'Should have Copy Text item');
console.log('✓ AI Script Menu test passed');

// Test 6: Job Queue Menu
console.log('\n6. Testing Job Queue Menu...');
const jobData = {
  jobId: 'job-101',
  status: 'running',
  outputPath: '/output/video.mp4'
};
const jobMenu = builder.build('job-queue', jobData, {});
assert(jobMenu, 'Job queue menu should be created');

const jobLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(jobLabels.includes('Pause Job'), 'Should have Pause Job item');
assert(jobLabels.includes('Cancel Job'), 'Should have Cancel Job item');
assert(jobLabels.includes('View Logs'), 'Should have View Logs item');
console.log('✓ Job Queue Menu test passed');

// Test 7: Preview Window Menu
console.log('\n7. Testing Preview Window Menu...');
const previewData = {
  currentTime: 10.5,
  duration: 60,
  isPlaying: false,
  zoom: 1.0
};
const previewMenu = builder.build('preview-window', previewData, {});
assert(previewMenu, 'Preview window menu should be created');

const previewLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(previewLabels.includes('Play'), 'Should have Play item when not playing');
assert(previewLabels.includes('Add Marker at Current Frame'), 'Should have Add Marker item');
assert(previewLabels.includes('Zoom'), 'Should have Zoom submenu');
console.log('✓ Preview Window Menu test passed');

// Test 8: Preview Window Menu with isPlaying=true
console.log('\n8. Testing Preview Window Menu (Playing)...');
const previewPlayingData = {
  currentTime: 10.5,
  duration: 60,
  isPlaying: true,
  zoom: 1.0
};
const previewPlayingMenu = builder.build('preview-window', previewPlayingData, {});
assert(previewPlayingMenu, 'Preview window menu should be created');

const previewPlayingLabels = mockMenuBuiltFrom
  .map((item) => item.label)
  .filter(Boolean);
assert(
  previewPlayingLabels.includes('Pause'),
  'Should have Pause item when playing'
);
console.log('✓ Preview Window Menu (Playing) test passed');

// Test 9: AI Provider Menu
console.log('\n9. Testing AI Provider Menu...');
const providerData = {
  providerId: 'openai-gpt4',
  providerType: 'llm',
  isDefault: true,
  hasFallback: true
};
const providerMenu = builder.build('ai-provider', providerData, {});
assert(providerMenu, 'AI provider menu should be created');

const providerLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(providerLabels.includes('Test Connection'), 'Should have Test Connection item');
assert(providerLabels.includes('View Usage Stats'), 'Should have View Usage Stats item');
assert(providerLabels.includes('Set as Default'), 'Should have Set as Default item');
assert(providerLabels.includes('Configure'), 'Should have Configure item');
console.log('✓ AI Provider Menu test passed');

// Test 10: Unknown Menu Type Handling
console.log('\n10. Testing Unknown Menu Type Handling...');
const unknownMenu = builder.build('unknown-type', {}, {});
assert(unknownMenu, 'Unknown menu type should return fallback menu');

const unknownLabels = mockMenuBuiltFrom.map((item) => item.label).filter(Boolean);
assert(
  unknownLabels.includes('Unknown menu type'),
  'Should have Unknown menu type fallback item'
);
console.log('✓ Unknown Menu Type Handling test passed');

// Test 11: Callback Invocation
console.log('\n11. Testing Callback Invocation...');
let callbackInvoked = false;
let callbackData = null;
const callbacks = {
  onCut: (data) => {
    callbackInvoked = true;
    callbackData = data;
  }
};

builder.build('timeline-clip', clipData, callbacks);

// Find the Cut item and trigger its click
const cutItem = mockMenuBuiltFrom.find((item) => item.label === 'Cut');
assert(cutItem, 'Should find Cut item');
assert(typeof cutItem.click === 'function', 'Cut item should have click handler');

cutItem.click();
assert(callbackInvoked, 'Callback should be invoked');
assert.deepStrictEqual(callbackData, clipData, 'Callback should receive correct data');
console.log('✓ Callback Invocation test passed');

// Test 12: Checkbox State
console.log('\n12. Testing Checkbox State...');
const lockedTrackData = {
  ...trackData,
  isLocked: true,
  isMuted: true,
  isSolo: false
};

builder.build('timeline-track', lockedTrackData, {});

const lockItem = mockMenuBuiltFrom.find((item) => item.label === 'Lock Track');
const muteItem = mockMenuBuiltFrom.find((item) => item.label === 'Mute Track');
const soloItem = mockMenuBuiltFrom.find((item) => item.label === 'Solo Track');

assert(lockItem.type === 'checkbox', 'Lock Track should be checkbox');
assert(lockItem.checked === true, 'Lock Track should be checked');
assert(muteItem.checked === true, 'Mute Track should be checked');
assert(soloItem.checked === false, 'Solo Track should not be checked');
console.log('✓ Checkbox State test passed');

// Test 13: Enabled/Disabled State
console.log('\n13. Testing Enabled/Disabled State...');
const failedJobData = {
  jobId: 'job-failed',
  status: 'failed',
  outputPath: null
};

builder.build('job-queue', failedJobData, {});

const pauseItem = mockMenuBuiltFrom.find((item) => item.label === 'Pause Job');
const resumeItem = mockMenuBuiltFrom.find((item) => item.label === 'Resume Job');
const retryItem = mockMenuBuiltFrom.find((item) => item.label === 'Retry Job');
const openOutputItem = mockMenuBuiltFrom.find(
  (item) => item.label === 'Open Output File'
);

assert(pauseItem.enabled === false, 'Pause should be disabled for failed job');
assert(resumeItem.enabled === false, 'Resume should be disabled for failed job');
assert(retryItem.enabled === true, 'Retry should be enabled for failed job');
assert(
  openOutputItem.enabled === false,
  'Open Output should be disabled for failed job'
);
console.log('✓ Enabled/Disabled State test passed');

// Test 14: Submenu Structure
console.log('\n14. Testing Submenu Structure...');
builder.build('preview-window', previewData, {});

const zoomItem = mockMenuBuiltFrom.find((item) => item.label === 'Zoom');
assert(zoomItem, 'Should find Zoom item');
assert(Array.isArray(zoomItem.submenu), 'Zoom should have submenu array');
assert(zoomItem.submenu.length === 4, 'Zoom submenu should have 4 items');

const zoomLabels = zoomItem.submenu.map((item) => item.label);
assert(zoomLabels.includes('Fit to Window'), 'Should have Fit to Window');
assert(zoomLabels.includes('50%'), 'Should have 50%');
assert(zoomLabels.includes('100%'), 'Should have 100%');
assert(zoomLabels.includes('200%'), 'Should have 200%');
console.log('✓ Submenu Structure test passed');

// Test 15: Delete Track Disabled When Only One Track
console.log('\n15. Testing Delete Track Disabled When Only One Track...');
const singleTrackData = {
  ...trackData,
  totalTracks: 1
};

builder.build('timeline-track', singleTrackData, {});

const deleteTrackItem = mockMenuBuiltFrom.find(
  (item) => item.label === 'Delete Track'
);
assert(
  deleteTrackItem.enabled === false,
  'Delete Track should be disabled when only one track'
);
console.log('✓ Delete Track Disabled When Only One Track test passed');

// Restore original require
Module.prototype.require = originalRequire;

console.log('\n' + '='.repeat(60));
console.log('ALL CONTEXT MENU SYSTEM TESTS PASSED ✓');
console.log('='.repeat(60));
console.log('Verified:');
console.log('  - Timeline Clip Menu creation');
console.log('  - Timeline Track Menu creation');
console.log('  - Timeline Empty Menu creation');
console.log('  - Media Asset Menu creation');
console.log('  - AI Script Menu creation');
console.log('  - Job Queue Menu creation');
console.log('  - Preview Window Menu creation');
console.log('  - AI Provider Menu creation');
console.log('  - Unknown menu type fallback');
console.log('  - Callback invocation');
console.log('  - Checkbox state management');
console.log('  - Enabled/disabled state management');
console.log('  - Submenu structure');
console.log('  - Conditional menu item states');
console.log('='.repeat(60));
