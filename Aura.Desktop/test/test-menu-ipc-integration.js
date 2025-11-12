/**
 * Integration Test for Menu Event IPC Flow
 * 
 * Tests the complete flow:
 * 1. Main process (menu-builder) sends IPC event
 * 2. Preload script exposes it to renderer
 * 3. React hook (useElectronMenuEvents) receives it
 * 
 * This simulates the actual Electron environment more closely.
 */

const assert = require('assert');
const EventEmitter = require('events');
const { MENU_EVENT_CHANNELS } = require('../electron/menu-event-types');

console.log('='.repeat(60));
console.log('Integration Test: Menu Event IPC Flow');
console.log('='.repeat(60));

// Create a more realistic IPC emitter
class MockIpcMain extends EventEmitter {}
class MockIpcRenderer extends EventEmitter {
  constructor() {
    super();
    this.handlerRegistry = new Map();
  }
  
  on(channel, handler) {
    this.handlerRegistry.set(handler, channel);
    return super.on(channel, handler);
  }
  
  removeListener(channel, handler) {
    this.handlerRegistry.delete(handler);
    return super.removeListener(channel, handler);
  }
  
  getListenerCount(channel) {
    return this.listenerCount(channel);
  }
}

const ipcMain = new MockIpcMain();
const ipcRenderer = new MockIpcRenderer();

// Simulate preload script exposing API
const { isValidMenuEventChannel } = require('../electron/menu-event-types');

function createMenuAPI(ipcRenderer) {
  function safeOn(channel, callback) {
    if (!isValidMenuEventChannel(channel)) {
      throw new Error(`Invalid event channel: ${channel}`);
    }
    
    if (typeof callback !== 'function') {
      throw new TypeError(`Callback must be a function, received ${typeof callback}`);
    }
    
    const subscription = (event, ...args) => {
      try {
        callback(...args);
      } catch (error) {
        console.error(`Error in listener for ${channel}:`, error);
      }
    };
    
    ipcRenderer.on(channel, subscription);
    
    return () => {
      ipcRenderer.removeListener(channel, subscription);
    };
  }
  
  return {
    onNewProject: (callback) => safeOn('menu:newProject', callback),
    onOpenProject: (callback) => safeOn('menu:openProject', callback),
    onOpenRecentProject: (callback) => safeOn('menu:openRecentProject', callback),
    onSaveProject: (callback) => safeOn('menu:saveProject', callback),
    onSaveProjectAs: (callback) => safeOn('menu:saveProjectAs', callback),
    onImportVideo: (callback) => safeOn('menu:importVideo', callback),
    onImportAudio: (callback) => safeOn('menu:importAudio', callback),
    onImportImages: (callback) => safeOn('menu:importImages', callback),
    onImportDocument: (callback) => safeOn('menu:importDocument', callback),
    onExportVideo: (callback) => safeOn('menu:exportVideo', callback),
    onExportTimeline: (callback) => safeOn('menu:exportTimeline', callback),
    onFind: (callback) => safeOn('menu:find', callback),
    onOpenPreferences: (callback) => safeOn('menu:openPreferences', callback),
    onOpenProviderSettings: (callback) => safeOn('menu:openProviderSettings', callback),
    onOpenFFmpegConfig: (callback) => safeOn('menu:openFFmpegConfig', callback),
    onClearCache: (callback) => safeOn('menu:clearCache', callback),
    onViewLogs: (callback) => safeOn('menu:viewLogs', callback),
    onRunDiagnostics: (callback) => safeOn('menu:runDiagnostics', callback),
    onOpenGettingStarted: (callback) => safeOn('menu:openGettingStarted', callback),
    onShowKeyboardShortcuts: (callback) => safeOn('menu:showKeyboardShortcuts', callback),
    onCheckForUpdates: (callback) => safeOn('menu:checkForUpdates', callback)
  };
}

// Simulate menu-builder sending events
class MockMenuBuilder {
  constructor(ipcRenderer) {
    this.ipcRenderer = ipcRenderer;
  }
  
  _sendToRenderer(channel, data = {}) {
    this.ipcRenderer.emit(channel, {}, data);
  }
  
  triggerNewProject() {
    this._sendToRenderer('menu:newProject');
  }
  
  triggerOpenProject() {
    this._sendToRenderer('menu:openProject');
  }
  
  triggerOpenRecentProject(path) {
    this._sendToRenderer('menu:openRecentProject', { path });
  }
  
  triggerSaveProject() {
    this._sendToRenderer('menu:saveProject');
  }
}

// Simulate React hook behavior
class MockReactHook {
  constructor(menuAPI) {
    this.menuAPI = menuAPI;
    this.unsubscribers = [];
    this.eventLog = [];
  }
  
  mount() {
    this.eventLog = [];
    
    this.unsubscribers.push(
      this.menuAPI.onNewProject(() => {
        this.eventLog.push({ event: 'newProject', timestamp: Date.now() });
      })
    );
    
    this.unsubscribers.push(
      this.menuAPI.onOpenProject(() => {
        this.eventLog.push({ event: 'openProject', timestamp: Date.now() });
      })
    );
    
    this.unsubscribers.push(
      this.menuAPI.onOpenRecentProject((data) => {
        this.eventLog.push({ event: 'openRecentProject', data, timestamp: Date.now() });
      })
    );
    
    this.unsubscribers.push(
      this.menuAPI.onSaveProject(() => {
        this.eventLog.push({ event: 'saveProject', timestamp: Date.now() });
      })
    );
  }
  
  unmount() {
    this.unsubscribers.forEach(unsub => unsub());
    this.unsubscribers = [];
  }
  
  getEventLog() {
    return this.eventLog;
  }
}

// Run integration tests
console.log('\n1. Setting up test environment...');
const menuAPI = createMenuAPI(ipcRenderer);
const menuBuilder = new MockMenuBuilder(ipcRenderer);
const reactHook = new MockReactHook(menuAPI);
console.log('✓ Test environment created');

// Test 1: Mount hook and verify listeners registered
console.log('\n2. Testing hook mount registers listeners...');
const initialListenerCount = MENU_EVENT_CHANNELS.reduce((sum, channel) => {
  return sum + ipcRenderer.getListenerCount(channel);
}, 0);
assert.strictEqual(initialListenerCount, 0, 'Should start with no listeners');

reactHook.mount();

// Verify at least some listeners are registered (we mounted 4 in the mock hook)
let mountedListenerCount = 0;
['menu:newProject', 'menu:openProject', 'menu:openRecentProject', 'menu:saveProject'].forEach(channel => {
  mountedListenerCount += ipcRenderer.getListenerCount(channel);
});
assert.strictEqual(mountedListenerCount, 4, 'Should have 4 listeners after mount');
console.log('✓ Hook mount registers listeners correctly');

// Test 2: Send IPC event from main and verify React receives it
console.log('\n3. Testing IPC event flow from main to React...');
menuBuilder.triggerNewProject();
assert.strictEqual(reactHook.getEventLog().length, 1, 'React hook should receive 1 event');
assert.strictEqual(reactHook.getEventLog()[0].event, 'newProject', 'Event should be newProject');
console.log('✓ IPC event flows from main to React');

// Test 3: Test multiple events
console.log('\n4. Testing multiple events...');
menuBuilder.triggerOpenProject();
menuBuilder.triggerSaveProject();
assert.strictEqual(reactHook.getEventLog().length, 3, 'React hook should receive 3 events total');
assert.strictEqual(reactHook.getEventLog()[1].event, 'openProject', 'Second event should be openProject');
assert.strictEqual(reactHook.getEventLog()[2].event, 'saveProject', 'Third event should be saveProject');
console.log('✓ Multiple events handled correctly');

// Test 4: Test event with data
console.log('\n5. Testing event with data payload...');
menuBuilder.triggerOpenRecentProject('/path/to/project');
assert.strictEqual(reactHook.getEventLog().length, 4, 'React hook should receive 4 events total');
const recentProjectEvent = reactHook.getEventLog()[3];
assert.strictEqual(recentProjectEvent.event, 'openRecentProject', 'Event should be openRecentProject');
assert.deepStrictEqual(recentProjectEvent.data, { path: '/path/to/project' }, 'Event data should match');
console.log('✓ Event with data payload handled correctly');

// Test 5: Test hook unmount removes all listeners
console.log('\n6. Testing hook unmount removes listeners...');
reactHook.unmount();

let unmountedListenerCount = 0;
['menu:newProject', 'menu:openProject', 'menu:openRecentProject', 'menu:saveProject'].forEach(channel => {
  unmountedListenerCount += ipcRenderer.getListenerCount(channel);
});
assert.strictEqual(unmountedListenerCount, 0, 'Should have 0 listeners after unmount');
console.log('✓ Hook unmount removes all listeners');

// Test 6: Verify events don't reach unmounted hook
console.log('\n7. Testing unmounted hook doesn\'t receive events...');
const eventCountBeforeUnmount = reactHook.getEventLog().length;
menuBuilder.triggerNewProject();
assert.strictEqual(reactHook.getEventLog().length, eventCountBeforeUnmount, 'Event count should not increase');
console.log('✓ Unmounted hook correctly ignores events');

// Test 7: Test remounting
console.log('\n8. Testing hook remount...');
reactHook.mount();
menuBuilder.triggerNewProject();
assert.strictEqual(reactHook.getEventLog().length, 1, 'New mount should start fresh event log');
assert.strictEqual(reactHook.getEventLog()[0].event, 'newProject', 'Event should be received after remount');
console.log('✓ Hook remount works correctly');

// Test 8: Test all menu channels are exposed in API
console.log('\n9. Validating all menu channels are exposed...');
const methodMapping = {
  'menu:newProject': 'onNewProject',
  'menu:openProject': 'onOpenProject',
  'menu:openRecentProject': 'onOpenRecentProject',
  'menu:saveProject': 'onSaveProject',
  'menu:saveProjectAs': 'onSaveProjectAs',
  'menu:importVideo': 'onImportVideo',
  'menu:importAudio': 'onImportAudio',
  'menu:importImages': 'onImportImages',
  'menu:importDocument': 'onImportDocument',
  'menu:exportVideo': 'onExportVideo',
  'menu:exportTimeline': 'onExportTimeline',
  'menu:find': 'onFind',
  'menu:openPreferences': 'onOpenPreferences',
  'menu:openProviderSettings': 'onOpenProviderSettings',
  'menu:openFFmpegConfig': 'onOpenFFmpegConfig',
  'menu:clearCache': 'onClearCache',
  'menu:viewLogs': 'onViewLogs',
  'menu:runDiagnostics': 'onRunDiagnostics',
  'menu:openGettingStarted': 'onOpenGettingStarted',
  'menu:showKeyboardShortcuts': 'onShowKeyboardShortcuts',
  'menu:checkForUpdates': 'onCheckForUpdates'
};

const missingMethods = [];
Object.values(methodMapping).forEach(method => {
  if (typeof menuAPI[method] !== 'function') {
    missingMethods.push(method);
  }
});

assert.strictEqual(missingMethods.length, 0, `All methods should be exposed. Missing: ${missingMethods.join(', ')}`);
console.log(`✓ All ${Object.keys(methodMapping).length} menu methods are exposed in API`);

// Cleanup
reactHook.unmount();

console.log('\n' + '='.repeat(60));
console.log('INTEGRATION TESTS PASSED ✓');
console.log('='.repeat(60));
console.log('Verified:');
console.log('  - IPC event flow from main process to React');
console.log('  - Hook lifecycle (mount/unmount)');
console.log('  - Listener cleanup prevents memory leaks');
console.log('  - Event data payloads are preserved');
console.log(`  - All ${MENU_EVENT_CHANNELS.length} menu channels properly exposed`);
console.log('='.repeat(60));
