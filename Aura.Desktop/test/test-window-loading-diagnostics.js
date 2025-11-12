/**
 * Test script for Window Loading Diagnostics
 * This validates the enhanced window loading features work correctly
 */

const fs = require('fs');
const path = require('path');
const os = require('os');

// Mock Electron app object
const mockApp = {
  getVersion: () => '1.0.0-test',
  getAppPath: () => path.join(__dirname, '..'),
  getPath: (name) => {
    if (name === 'userData') {
      return path.join(os.tmpdir(), 'aura-test-window-loading');
    }
    if (name === 'temp') {
      return os.tmpdir();
    }
    return os.tmpdir();
  },
  isPackaged: false,
  quit: () => {}
};

// Mock BrowserWindow
class MockBrowserWindow {
  constructor(options) {
    this.options = options;
    this.webContents = new MockWebContents();
    this._isDestroyed = false;
    this._eventHandlers = {};
  }

  isDestroyed() {
    return this._isDestroyed;
  }

  isMaximized() {
    return false;
  }

  getBounds() {
    return { x: 0, y: 0, width: 1920, height: 1080 };
  }

  maximize() {}
  show() {}
  hide() {}
  focus() {}
  close() { this._isDestroyed = true; }
  destroy() { this._isDestroyed = true; }

  on(event, handler) {
    if (!this._eventHandlers[event]) {
      this._eventHandlers[event] = [];
    }
    this._eventHandlers[event].push(handler);
  }

  once(event, handler) {
    this.on(event, handler);
  }

  emit(event, ...args) {
    if (this._eventHandlers[event]) {
      this._eventHandlers[event].forEach(handler => handler(...args));
    }
  }

  loadFile(filePath) {
    this.loadedPath = filePath;
    return new Promise((resolve, reject) => {
      if (fs.existsSync(filePath)) {
        setTimeout(() => {
          this.webContents.emit('did-start-loading');
          setTimeout(() => {
            this.webContents.emit('did-finish-load');
            resolve();
          }, 100);
        }, 50);
      } else {
        setTimeout(() => {
          this.webContents.emit('did-fail-load', null, -6, 'File not found', filePath, true);
          reject(new Error('File not found: ' + filePath));
        }, 50);
      }
    });
  }

  loadURL(url) {
    this.loadedURL = url;
    return Promise.resolve();
  }
}

class MockWebContents {
  constructor() {
    this._eventHandlers = {};
    this.session = {
      webRequest: {
        onHeadersReceived: () => {}
      }
    };
  }

  on(event, handler) {
    if (!this._eventHandlers[event]) {
      this._eventHandlers[event] = [];
    }
    this._eventHandlers[event].push(handler);
  }

  emit(event, ...args) {
    if (this._eventHandlers[event]) {
      this._eventHandlers[event].forEach(handler => handler(...args));
    }
  }

  getURL() {
    return 'file:///test/index.html';
  }

  executeJavaScript(script) {
    return Promise.resolve();
  }

  setWindowOpenHandler() {}
  openDevTools() {}
}

// Mock electron modules
const mockElectron = {
  BrowserWindow: MockBrowserWindow,
  screen: {
    getPrimaryDisplay: () => ({
      workAreaSize: { width: 1920, height: 1080 }
    }),
    getAllDisplays: () => [
      { bounds: { x: 0, y: 0, width: 1920, height: 1080 } }
    ]
  },
  nativeImage: {
    createFromPath: (path) => ({
      isEmpty: () => false
    }),
    createFromDataURL: (dataURL) => ({
      isEmpty: () => false
    }),
    createEmpty: () => ({
      isEmpty: () => true
    })
  },
  shell: {
    openExternal: () => Promise.resolve()
  },
  dialog: {
    showMessageBox: () => Promise.resolve({ response: 0 })
  }
};

// Mock electron-store
class MockStore {
  constructor(options) {
    this.data = options.defaults || {};
  }
  get(key, defaultValue) {
    return this.data[key] !== undefined ? this.data[key] : defaultValue;
  }
  set(key, value) {
    this.data[key] = value;
  }
}

// Mock modules
const Module = require('module');
const originalRequire = Module.prototype.require;

Module.prototype.require = function(id) {
  if (id === 'electron') {
    return mockElectron;
  }
  if (id === 'electron-store') {
    return MockStore;
  }
  return originalRequire.apply(this, arguments);
};

// Load WindowManager
const WindowManager = require('../electron/window-manager');

console.log('='.repeat(60));
console.log('Testing Window Loading Diagnostics');
console.log('='.repeat(60));

async function runTests() {
  let passed = 0;
  let failed = 0;

  try {
    console.log('\n1. Testing WindowManager initialization...');
    const windowManager = new WindowManager(mockApp, true);
    console.log('✓ WindowManager initialized');
    passed++;

    console.log('\n2. Testing loading state initialization...');
    const preloadPath = path.join(__dirname, '../preload.js');
    const backendPort = 5005;
    
    // Create a test HTML file
    const testHtmlPath = path.join(os.tmpdir(), 'test-index.html');
    fs.writeFileSync(testHtmlPath, '<html><body>Test</body></html>');
    
    // Create window - this should initialize loading state
    const mainWindow = windowManager.createMainWindow(backendPort, preloadPath);
    
    if (windowManager.loadingState) {
      console.log('✓ Loading state initialized');
      console.log('  Properties:', Object.keys(windowManager.loadingState));
      passed++;
    } else {
      console.error('✗ Loading state not initialized');
      failed++;
    }

    console.log('\n3. Testing event handlers registered...');
    const webContents = mainWindow.webContents;
    const expectedEvents = [
      'did-start-loading',
      'did-finish-load',
      'did-fail-load',
      'console-message',
      'crashed'
    ];
    
    let handlersFound = 0;
    for (const event of expectedEvents) {
      if (webContents._eventHandlers[event] && webContents._eventHandlers[event].length > 0) {
        console.log(`  ✓ Handler registered for '${event}'`);
        handlersFound++;
      } else {
        console.log(`  ✗ No handler for '${event}'`);
      }
    }
    
    if (handlersFound === expectedEvents.length) {
      console.log('✓ All event handlers registered');
      passed++;
    } else {
      console.error(`✗ Only ${handlersFound}/${expectedEvents.length} handlers registered`);
      failed++;
    }

    console.log('\n4. Testing did-start-loading event...');
    webContents.emit('did-start-loading');
    
    if (windowManager.loadingState.didStartLoading && windowManager.loadingState.startTime) {
      console.log('✓ did-start-loading updates state correctly');
      console.log('  Start time:', new Date(windowManager.loadingState.startTime).toISOString());
      passed++;
    } else {
      console.error('✗ did-start-loading did not update state');
      failed++;
    }

    console.log('\n5. Testing did-finish-load event...');
    webContents.emit('did-finish-load');
    
    if (windowManager.loadingState.didFinishLoad) {
      console.log('✓ did-finish-load updates state correctly');
      passed++;
    } else {
      console.error('✗ did-finish-load did not update state');
      failed++;
    }

    console.log('\n6. Testing console-message forwarding...');
    let consoleMessageCalled = false;
    const originalLog = console.log;
    console.log = function(...args) {
      const message = args.join(' ');
      if (message.includes('[Renderer:')) {
        consoleMessageCalled = true;
      }
      originalLog.apply(console, args);
    };
    
    webContents.emit('console-message', null, 1, 'Test console message', 10, 'test.js');
    console.log = originalLog;
    
    if (consoleMessageCalled) {
      console.log('✓ Console messages are forwarded to main process');
      passed++;
    } else {
      console.error('✗ Console message forwarding not working');
      failed++;
    }

    console.log('\n7. Testing _getFrontendPaths method...');
    const paths = windowManager._getFrontendPaths();
    
    if (Array.isArray(paths) && paths.length > 0) {
      console.log('✓ _getFrontendPaths returns array of paths');
      console.log(`  Found ${paths.length} paths to try`);
      paths.forEach((p, i) => console.log(`  ${i + 1}. ${p}`));
      passed++;
    } else {
      console.error('✗ _getFrontendPaths did not return valid paths');
      failed++;
    }

    console.log('\n8. Testing _collectLoadingLogs method...');
    const logs = windowManager._collectLoadingLogs();
    
    if (typeof logs === 'string' && logs.length > 0) {
      console.log('✓ _collectLoadingLogs returns diagnostic string');
      console.log('  Sample output:');
      console.log(logs.split('\n').map(line => '    ' + line).join('\n'));
      passed++;
    } else {
      console.error('✗ _collectLoadingLogs did not return valid logs');
      failed++;
    }

    console.log('\n9. Testing error page exists...');
    const errorPagePath = path.join(__dirname, '../assets/error.html');
    
    if (fs.existsSync(errorPagePath)) {
      console.log('✓ Error page exists at:', errorPagePath);
      const errorPageContent = fs.readFileSync(errorPagePath, 'utf8');
      
      if (errorPageContent.includes('Failed to Load Application') &&
          errorPageContent.includes('diagnostics') &&
          errorPageContent.includes('retryLoad')) {
        console.log('✓ Error page has expected content');
        passed++;
      } else {
        console.error('✗ Error page missing expected content');
        failed++;
      }
    } else {
      console.error('✗ Error page not found');
      failed++;
    }

    console.log('\n10. Testing failed load scenario...');
    const windowManager2 = new WindowManager(mockApp, true);
    const mainWindow2 = windowManager2.createMainWindow(backendPort, preloadPath);
    
    // Simulate failed load
    mainWindow2.webContents.emit('did-fail-load', null, -6, 'ERR_FILE_NOT_FOUND', 'file:///nonexistent.html', true);
    
    if (windowManager2.loadingState.lastError) {
      console.log('✓ Failed load is captured in state');
      console.log('  Error code:', windowManager2.loadingState.lastError.errorCode);
      console.log('  Error description:', windowManager2.loadingState.lastError.errorDescription);
      passed++;
    } else {
      console.error('✗ Failed load not captured');
      failed++;
    }

    // Cleanup
    fs.unlinkSync(testHtmlPath);

  } catch (error) {
    console.error('\n✗ Test execution error:', error);
    console.error(error.stack);
    failed++;
  }

  // Restore original require
  Module.prototype.require = originalRequire;

  console.log('\n' + '='.repeat(60));
  console.log('Test Results');
  console.log('='.repeat(60));
  console.log(`Passed: ${passed}`);
  console.log(`Failed: ${failed}`);
  console.log(`Total:  ${passed + failed}`);
  console.log('='.repeat(60));

  if (failed > 0) {
    console.log('\n✗ Some tests failed');
    process.exit(1);
  } else {
    console.log('\n✓ All tests passed!');
    process.exit(0);
  }
}

runTests().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});
